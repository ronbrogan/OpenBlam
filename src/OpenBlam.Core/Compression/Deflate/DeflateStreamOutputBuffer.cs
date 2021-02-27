using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal class DeflateStreamOutputBuffer
    {
        private const int LookbackCopyThreshold = 2 * DeflateConstants.MaximumLookback;

        private readonly Stream outstream;
        private byte[] lookbackBuffer = new byte[3 * DeflateConstants.MaximumLookback];
        private int lookbackPosition = 0;
        private byte[] copyBuffer = new byte[DeflateConstants.MaximumLookback];

        public DeflateStreamOutputBuffer(Stream outstream)
        {
            this.outstream = outstream;
        }

        public unsafe void Write(byte* data, int dataLength)
        {
            ref var source = ref Unsafe.AsRef<byte>(data);
            var span = MemoryMarshal.CreateSpan(ref source, dataLength);
            outstream.Write(span);

            Unsafe.CopyBlock(ref lookbackBuffer[lookbackPosition], ref source, (uint)dataLength);
            lookbackPosition += dataLength;
            MaintainBuffer();
        }

        public void Write(Stream souce, int length)
        {
            while(length > 0)
            {
                var read = Math.Min(length, DeflateConstants.MaximumLookback);
                var actualRead = souce.Read(copyBuffer, 0, read);
                this.outstream.Write(copyBuffer, 0, actualRead);
                length -= actualRead;
            }
        }

        public void WriteByte(byte value)
        {
            outstream.WriteByte(value);
            lookbackBuffer[lookbackPosition] = value;
            lookbackPosition++;
            MaintainBuffer();
        }

        public void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            Debug.Assert(lookbackDistance < lookbackPosition);

            var chunkLength = Math.Min(lookbackDistance, lengthToWrite);
            var start = this.lookbackPosition - lookbackDistance;

            while(lengthToWrite > 0)
            {
                this.outstream.Write(this.lookbackBuffer, start, chunkLength);
                lengthToWrite -= chunkLength;

                Array.Copy(this.lookbackBuffer, start, this.lookbackBuffer, this.lookbackPosition, chunkLength);
                this.lookbackPosition += chunkLength;
            }

            MaintainBuffer();
        }

        private void MaintainBuffer()
        {
            if(lookbackPosition < LookbackCopyThreshold)
            {
                return;
            }

            Array.Copy(
                lookbackBuffer, lookbackPosition - DeflateConstants.MaximumLookback,
                lookbackBuffer, 0,
                DeflateConstants.MaximumLookback);

            lookbackPosition = DeflateConstants.MaximumLookback;
        }
    }
}
