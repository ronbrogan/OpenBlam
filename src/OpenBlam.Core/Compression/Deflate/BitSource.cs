using OpenBlam.Core.Exceptions;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public class BitSource
    {
        public readonly byte[] Data;
        public ulong CurrentBit => currentBit;
        private ulong currentBit;

        private int availableLocalBits = 0;
        private ulong localBits;

        public BitSource(byte[] data)
        {
            this.Data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBits(int need)
        {
            if(need > this.availableLocalBits)
            {
                // read bits from currentBit
                var startByte = (int)(currentBit >> 3);

                if (startByte >= Data.Length)
                    Throw.Exception("Input data not long enough");

                ulong accum = 0;
                var bitsGathered = 0;

                for(var i = 0; i < 8; i++)
                {
                    if (startByte + i >= Data.Length)
                        break;

                    ulong b = Data[startByte + i];
                    accum |= (b << (i << 3));
                    bitsGathered += 8;
                }

                var bitsAlreadyUsed = (int)(currentBit & 7);

                bitsGathered -= bitsAlreadyUsed;
                accum >>= bitsAlreadyUsed;

                this.localBits = accum;
                this.availableLocalBits = bitsGathered;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsumeBit(byte count = 1)
        {
            currentBit += count;
            this.availableLocalBits -= count;
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
    }
}
