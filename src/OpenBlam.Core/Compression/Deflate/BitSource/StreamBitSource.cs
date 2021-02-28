using System.IO;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public sealed class StreamBitSource : BitSource
    {
        private readonly Stream Data;
        private byte[] buffer = new byte[8];

        public StreamBitSource(Stream data)
        {
            this.Data = data;
        }

        public override void SkipToNextByte()
        {
            base.SkipToNextByte();

            Data.Position = (int)(this.CurrentBit << 3);
        }

        protected override void EnsureBits(int need)
        {
            if (need > this.availableLocalBits)
            {
                // read bits from currentBit
                var startByte = (int)(currentBit >> 3);
                this.currentLocalBit = (int)(currentBit & 7);
                
                if(Data.Position != startByte)
                {
                    Data.Position = startByte;
                }

                var bytesRead = Data.Read(buffer);

                var accum = Unsafe.As<byte, ulong>(ref buffer[0]);

                BroadcastTo16s(accum);

                accum >>= this.currentLocalBit;

                this.localBits = accum;
                this.availableLocalBits = 64 - this.currentLocalBit;
            }
        }
    }
}
