using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace OpenBlam.Core.Compression.Deflate
{
    public unsafe abstract class BitSource : IDisposable
    {
        public ulong CurrentBit => currentBit;
        protected ulong currentBit;

        protected int availableLocalBits = 0;
        protected int currentLocalBit = 0;
        protected ulong localBits;
        protected byte[] localBitsAsBytes = new byte[64];
        protected byte* localBitsAsBytesPtr;
        protected GCHandle localBitsAsBytesHandle;

        public BitSource()
        {
            localBitsAsBytesHandle = GCHandle.Alloc(localBitsAsBytes, GCHandleType.Pinned);
            localBitsAsBytesPtr = (byte*)localBitsAsBytesHandle.AddrOfPinnedObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsumeBit()
        {
            this.currentBit++;
            this.availableLocalBits--;
            this.currentLocalBit++;
            this.localBits >>= 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBit(byte count)
        {
            this.currentBit += count;
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
            var val = *(this.localBitsAsBytesPtr + this.currentLocalBit);
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
            var bits = (int)(byteCount << 3);

            currentBit += (uint)bits;
            this.availableLocalBits -= bits;
            this.currentLocalBit += bits;
            this.localBits >>= bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte BitValue()
        {
            return (byte)(this.localBits & 1);
        }

        protected abstract void EnsureBits(int need);

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
        private bool disposedValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void BroadcastTo16s(ulong value)
        {
            if (Avx2.IsSupported)
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

                Avx2.Store(localBitsAsBytesPtr, z);
                Avx2.Store(localBitsAsBytesPtr + 32, z2);
            }
            else
            {
                for (var i = 0; i < 64; i++)
                {
                    this.localBitsAsBytes[i] = (byte)(((value >> i) & 1) * 16);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.localBitsAsBytesHandle.Free();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
