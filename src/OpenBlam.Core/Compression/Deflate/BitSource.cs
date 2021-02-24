using OpenBlam.Core.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace OpenBlam.Core.Compression.Deflate
{
    public class BitSource
    {
        public readonly byte[] Data;
        public ulong CurrentBit => currentBit;
        private ulong currentBit;

        private int availableLocalBits = 0;
        private int currentLocalBit = 0;
        private ulong localBits;
        private byte[] localBitsAsBytes = new byte[64];
        private IntPtr localBitsAsBytesPtr = IntPtr.Zero;
        private GCHandle localBitsAsBytesHandle;

        public BitSource(byte[] data)
        {
            this.Data = data;
            localBitsAsBytesHandle = GCHandle.Alloc(localBitsAsBytes, GCHandleType.Pinned);
            localBitsAsBytesPtr = localBitsAsBytesHandle.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            this.localBitsAsBytesHandle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void EnsureBits(int need)
        {
            if(need > this.availableLocalBits)
            {
                // read bits from currentBit
                var startByte = (int)(currentBit >> 3);
                this.currentLocalBit = (int)(currentBit & 7);

                if (startByte >= Data.Length)
                    Throw.Exception("Input data not long enough");

                ulong accum = 0;
                var bytesToRead = Math.Min(8, Data.Length - startByte);

                if(bytesToRead == 8)
                {
                    accum = BitConverter.ToUInt64(Data, startByte);
                }
                else
                {
                    for (var i = 0; i < bytesToRead; i++)
                    {
                        ulong b = Data[startByte + i];
                        accum |= (b << (i << 3));
                    }
                }

                BroadcastTo16s(accum);

                accum >>= this.currentLocalBit;

                this.localBits = accum;
                this.availableLocalBits = (bytesToRead * 8) - this.currentLocalBit;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsumeBit(byte count = 1)
        {
            currentBit += count;
            this.availableLocalBits -= count;
            this.currentLocalBit += count;
            this.localBits >>= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet()
        {
            EnsureBits(1);
            var val = BitValue() == 1;
            ConsumeBit();
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte CurrentBitValue()
        {
            EnsureBits(1);
            var val = BitValue();
            ConsumeBit();
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte CurrentBitValueAs16()
        {
            EnsureBits(1);
            var val = *(((byte*)this.localBitsAsBytesPtr) + this.currentLocalBit);
            //var val = (this.localBits & 1) << 4;
            ConsumeBit();
            return (byte)val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadBitsAsUshort(byte bits)
        {
            if (bits == 0) return 0;

            Debug.Assert(bits <= 16, "Only uint16 is supported here");

            EnsureBits(bits);

            var mask = ulong.MaxValue >> (64 - bits);
            var value2 = this.localBits & mask;

            ConsumeBit(bits);

            return (ushort)value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipToNextByte()
        {
            currentBit >>= 3;
            currentBit++;
            currentBit <<= 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBytes(ulong byteCount)
        {
            currentBit += (byteCount << 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte BitValue()
        {
            return (byte)(this.localBits & 1);
        }

        Vector256<byte> ByteExpansionMask = Vector256.Create(
                (byte)0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01,
                0x02, 0x02, 0x02, 0x02,
                0x02, 0x02, 0x02, 0x02,
                0x03, 0x03, 0x03, 0x03,
                0x03, 0x03, 0x03, 0x03
            );

        Vector256<byte> RelevantBitMask = Vector256.Create(
            0x01, 0x02, 0x04, 0x08,
            0x10, 0x20, 0x40, 0x80,
            0x01, 0x02, 0x04, 0x08,
            0x10, 0x20, 0x40, 0x80,
            0x01, 0x02, 0x04, 0x08,
            0x10, 0x20, 0x40, 0x80,
            0x01, 0x02, 0x04, 0x08,
            0x10, 0x20, 0x40, 0x80
        );

        Vector256<byte> Sixteens = Vector256.Create((byte)16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void BroadcastTo16s(ulong value)
        {
            if(Avx2.IsSupported)
            {
                var zero = Vector256.Create((byte)0);

                var a = (uint)value;
                var b = (uint)(value >> 32);

                var y = Vector256.Create(a);
                var y2 = Vector256.Create(b);

                var z = Avx2.Shuffle(y.AsByte(), ByteExpansionMask);
                var z2 = Avx2.Shuffle(y2.AsByte(), ByteExpansionMask);

                z = Avx2.And(z, RelevantBitMask);
                z2 = Avx2.And(z2, RelevantBitMask);

                z = Avx2.CompareEqual(z, zero);
                z2 = Avx2.CompareEqual(z2, zero);

                z = Avx2.Shuffle(Sixteens, z);
                z2 = Avx2.Shuffle(Sixteens, z2);

                byte* l = (byte*)localBitsAsBytesPtr;
                
                Avx2.Store(l, z);
                Avx2.Store(l+32, z2);
            }
            else
            {
                for(var i = 0; i < 64; i++)
                {
                    this.localBitsAsBytes[i] = (byte)(((value >> i) & 1) * 16);
                }
            }
        }
    }
}
