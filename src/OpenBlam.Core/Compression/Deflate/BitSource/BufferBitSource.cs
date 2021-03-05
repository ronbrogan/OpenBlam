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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override unsafe uint LoadBits()
        {
            // read bits from currentBit
            var startByte = (int)(this.currentBit >> 3);
            var currentBit = (uint)(this.currentBit & 7);

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

            this.BroadcastTo16s(accum);

            this.localBits = accum;
            this.availableLocalBits = 64 - currentBit;

            return currentBit;
        }
    }
}
