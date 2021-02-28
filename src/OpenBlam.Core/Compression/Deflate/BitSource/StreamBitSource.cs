using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public sealed class StreamBitSource : BitSource
    {
        private readonly Stream Data;
        public StreamBitSource(Stream data)
        {
            this.Data = data;
        }

        protected override void EnsureBits(int need)
        {
            if (need > this.availableLocalBits)
            {
                // read bits from currentBit
                var startByte = (int)(currentBit >> 3);
                this.currentLocalBit = (int)(currentBit & 7);
                Data.Position = startByte;

                Span<byte> bytes = stackalloc byte[8];
                var bytesRead = Data.Read(bytes);

                var accum = Unsafe.As<byte, ulong>(ref bytes[0]);

                BroadcastTo16s(accum);

                accum >>= this.currentLocalBit;

                this.localBits = accum;
                this.availableLocalBits = 64 - this.currentLocalBit;
            }
        }
    }
}
