using OpenBlam.Core.Collections;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace OpenBlam.Core.Compression.Deflate
{

    public unsafe abstract class BitSource : IDisposable
    {
        protected readonly static PinnedArrayPool<byte> bufferPool = PinnedArrayPool<byte>.Shared;

        protected uint currentLocalBit = 0;
        protected uint availableLocalBits = 0;
        private byte* localBitsAsBytesPtr;
        protected ulong currentBit;
        protected ulong localBits;
        protected byte[] localBitsAsBytes;
        public ulong CurrentBit => this.currentBit;

        public BitSource()
        {
            this.localBitsAsBytes = bufferPool.Rent(64, out this.localBitsAsBytesPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet()
        {
            var localBit = this.PrepBit();
            return *(this.localBitsAsBytesPtr + localBit) == 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte CurrentBitValueAs16()
        {
            var localBit = this.PrepBit();
            return *(this.localBitsAsBytesPtr + localBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadBitsAsUshort(byte bits)
        {
            if (bits == 0) return 0;

            Debug.Assert(bits <= 16, "Only uint16 is supported here");

            var localBit = (int)this.PrepBits(bits);

            var mask = (1u << bits) - 1;
            var value2 = (this.localBits >> localBit) & mask;

            return (ushort)value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SkipToNextByte()
        {
            this.currentBit >>= 3;
            this.currentBit++;
            this.currentBit <<= 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBytes(ulong byteCount)
        {
            var bits = (uint)(byteCount << 3);

            this.currentBit += bits;
            this.currentLocalBit += bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint PrepBits(uint need)
        {
            var localBit = this.currentLocalBit;

            if (need > this.availableLocalBits - localBit)
            {
                localBit = this.LoadBits();
            }

            this.currentBit += need;
            this.currentLocalBit = localBit + need;

            return localBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint PrepBit()
        {
            var localBit = this.currentLocalBit;

            if (1 > this.availableLocalBits - localBit)
            {
                localBit = this.LoadBits();
            }

            this.currentBit++;
            this.currentLocalBit = localBit + 1;

            return localBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract uint LoadBits();

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
        Vector256<byte> Zero = Vector256.Create((byte)0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void BroadcastTo16s(ulong value)
        {
            if (Avx2.IsSupported)
            {
                var a = (uint)value;
                var b = (uint)(value >> 32);

                var y = Vector256.Create(a);
                var y2 = Vector256.Create(b);

                var z = Avx2.Shuffle(y.AsByte(), this.ByteExpansionMask);
                var z2 = Avx2.Shuffle(y2.AsByte(), this.ByteExpansionMask);

                z = Avx2.And(z, this.RelevantBitMask);
                z2 = Avx2.And(z2, this.RelevantBitMask);

                z = Avx2.CompareEqual(z, Zero);
                z2 = Avx2.CompareEqual(z2, Zero);

                z = Avx2.Shuffle(this.Sixteens, z);
                z2 = Avx2.Shuffle(this.Sixteens, z2);

                Avx2.Store(this.localBitsAsBytesPtr, z);
                Avx2.Store(this.localBitsAsBytesPtr + 32, z2);
            }
            else
            {
                for (var i = 0; i < 64; i++)
                {
                    this.localBitsAsBytes[i] = (byte)(((value >> i) & 1) * 16);
                }
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if(this.localBitsAsBytes != null)
                    {
                        bufferPool.Return(this.localBitsAsBytes);
                        this.localBitsAsBytes = null;
                    }
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
