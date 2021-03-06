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
        [StructLayout(LayoutKind.Explicit)]
        protected unsafe struct BitSourceState
        {
            [FieldOffset(0)] public uint availableLocalBits;
            [FieldOffset(4)] public ulong currentBit;
            [FieldOffset(12)] public ulong localBits;
        }

        protected readonly static PinnedArrayPool<byte> bufferPool = PinnedArrayPool<byte>.Shared;

        protected BitSourceState state = new BitSourceState();
        public ulong CurrentBit => this.state.currentBit;

        public BitSource()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet()
        {
            this.PrepBit();
            var v = (this.state.localBits & 1) == 1;
            this.Consume(1);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ulong CurrentBitValue()
        {
            this.PrepBit();
            var v = this.state.localBits & 1;
            this.Consume(1);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadBitsAsUshort(byte bits)
        {
            if (bits == 0) return 0;

            Debug.Assert(bits <= 16, "Only uint16 is supported here");

            this.PrepBits(bits);

            var mask = (1u << bits) - 1;
            var value2 = this.state.localBits & mask;

            this.Consume(bits);

            return (ushort)value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SkipToNextByte()
        {
            this.state.currentBit >>= 3;
            this.state.currentBit++;
            this.state.currentBit <<= 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBytes(int byteCount)
        {
            var bits = (byteCount << 3);

            this.state.currentBit += (uint)bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepBits(int need)
        {
            if (need > this.state.availableLocalBits)
            {
                this.LoadBits();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepBit()
        {
            if (1 > this.state.availableLocalBits)
            {
                this.LoadBits();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consume(int bits)
        {
            this.state.localBits >>= bits;
            this.state.currentBit += (uint)bits;
            this.state.availableLocalBits -= (uint)bits;
        }

        protected abstract void LoadBits();
        public abstract void Dispose();
    }
}
