using OpenBlam.Core.Exceptions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{

    public unsafe sealed class BufferBitSource : BitSource
    {
        private readonly byte* data;
        private readonly int dataLength;
        public BufferBitSource(byte* data, int length)
        {
            this.data = data;
            this.dataLength = length;
        }

        public override void Dispose()
        {
        }

        protected override unsafe void LoadBits()
        {
            // read bits from currentBit
            var localCurrentBit = this.currentBit;

            var startByte = (int)(localCurrentBit >> 3);
            var currentBit = (int)(localCurrentBit & 7);

            var bytesAvailable = this.dataLength - startByte;

            ulong accum = 0;
            if (bytesAvailable >= 8)
            {
                accum = *(ulong*)(this.data + startByte);
            }
            else
            {
                var bytesToRead = Math.Min(8, bytesAvailable);
                for (var i = 0; i < bytesToRead; i++)
                {
                    ulong b = this.data[startByte + i];
                    accum |= (b << (i << 3));
                }
            }

            this.localBits = accum >>= currentBit;
            this.availableLocalBits = (uint)(64 - currentBit);
        }
    }
}
