using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public class BitSource
    {
        public readonly byte[] Data;
        private ulong CurrentBit;

        public BitSource(byte[] data)
        {
            this.Data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrent(ulong bit)
        {
            this.CurrentBit = bit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet()
        {
            var val = IsSet(CurrentBit);
            CurrentBit++;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(ulong bit) => ((Data[bit >> 3] >> (byte)(bit & 7)) & 1) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte CurrentBitValue()
        {
            var val = BitValue(CurrentBit);
            CurrentBit++;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte BitValue(ulong bit) => (byte)((Data[bit >> 3] >> (byte)(bit & 7)) & 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipToNextByte()
        {
            CurrentBit >>= 3;
            CurrentBit++;
            CurrentBit <<= 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConsumeBytes(ulong byteCount)
        {
            CurrentBit += (byteCount << 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadBitsAsUshort(int bits)
        {
            Debug.Assert(bits <= 16, "Only uint16 is supported here");

            var value = 0;
            for (var i = 0; i < bits; i++)
            {
                value |= (CurrentBitValue() << i);
            }

            return (ushort)value;
        }
    }
}
