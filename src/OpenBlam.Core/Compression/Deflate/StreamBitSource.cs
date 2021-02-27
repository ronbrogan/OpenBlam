using OpenBlam.Core.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public class StreamBitSource : IBitSource
    {
        private readonly Stream Data;

        public ulong CurrentBit => currentBit;
        private ulong currentBit;

        private int availableLocalBits = 0;
        private int currentLocalBit = 0;
        private ulong localBits;

        public StreamBitSource(Stream data)
        {
            this.Data = data;
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
            var val = BitValue() << 4;
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

        private void EnsureBits(int need)
        {
            if (need > this.availableLocalBits)
            {
                // read bits from currentBit
                var startByte = (int)(currentBit >> 3);
                this.currentLocalBit = (int)(currentBit & 7);
                Data.Position = startByte;

                ulong accum = 0;
                Span<byte> bytes = stackalloc byte[8];
                var bytesRead = Data.Read(bytes);

                if (bytesRead == 8)
                {
                    accum = Unsafe.As<byte, ulong>(ref bytes[0]);
                }
                else
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        ulong b = bytes[i];
                        accum |= (b << (i << 3));
                    }
                }

                accum >>= this.currentLocalBit;

                this.localBits = accum;
                this.availableLocalBits = (bytesRead * 8) - this.currentLocalBit;
            }
        }
    }
}
