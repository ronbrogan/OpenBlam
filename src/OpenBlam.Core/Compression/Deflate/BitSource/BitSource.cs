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
        public ulong availableLocalBits;
        public ulong currentBit;
        public ulong localBits;

        protected readonly static PinnedArrayPool<byte> bufferPool = PinnedArrayPool<byte>.Shared;

        public ulong CurrentBit => this.currentBit;

        public BitSource()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet()
        {
            this.PrepBits(1);
            var v = (this.localBits & 1) == 1;
            Consume(1, this);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ulong CurrentBitValue()
        {
            this.PrepBits(1);
            var v = this.localBits & 1;
            Consume(1, this);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadBitsAsUshort(byte bits)
        {
            if (bits == 0) return 0;

            Debug.Assert(bits <= 16, "Only uint16 is supported here");

            this.PrepBits(bits);

            var mask = (1u << bits) - 1;
            var value2 = this.localBits & mask;

            Consume(bits, this);

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
        public ulong AvailableBits()
        {
            return this.availableLocalBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong PeekBits()
        {
            return this.localBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBytes(int byteCount)
        {
            var bits = (byteCount << 3);

            this.currentBit += (uint)bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepBits(uint need)
        {
            if (need > this.availableLocalBits)
            {
                this.LoadBits();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(ulong bits)
        {
            Consume(bits, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Consume(ulong bits, BitSource b)
        {
            b.availableLocalBits -= bits;
            b.currentBit += bits;
            b.localBits >>= (int)bits;
        }

        protected abstract void LoadBits();
        public abstract void Dispose();
    }
}
