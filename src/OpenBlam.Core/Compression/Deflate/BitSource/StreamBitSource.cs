using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    public sealed class StreamBitSource : BitSource, IDisposable
    {
        private readonly Stream Data;
        private byte[] buffer ;

        public StreamBitSource(Stream data)
        {
            this.Data = data;
            this.buffer = bufferPool.Rent(8);
            Array.Clear(this.buffer, 0, 8);
        }

        public override void SkipToNextByte()
        {
            base.SkipToNextByte();

            this.Data.Position = (int)(this.CurrentBit << 3);
        }

        protected override void LoadBits()
        {
            // read bits from currentBit
            var localCurrentBit = this.state.currentBit;
            var startByte = (int)(localCurrentBit >> 3);
            var currentBit = (int)(localCurrentBit & 7);

            if (this.Data.Position != startByte)
            {
                this.Data.Position = startByte;
            }

            var bytesRead = this.Data.Read(this.buffer);

            var accum = Unsafe.As<byte, ulong>(ref this.buffer[0]);

            this.state.localBits = accum >>= currentBit;
            this.state.availableLocalBits = (uint)(64 - currentBit);
        }

        public override void Dispose()
        {
            if(this.buffer != null)
            {
                bufferPool.Return(this.buffer);
                this.buffer = null;
            }
        }
    }
}
