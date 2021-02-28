using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal sealed class DeflateStreamOutput : DeflateOutput<Stream>
    {
        private readonly Stream outstream;
        private const int LookbackCopyThreshold = 2 * DeflateConstants.MaximumLookback;
        private byte[] lookbackBuffer = new byte[3 * DeflateConstants.MaximumLookback];
        private int lookbackPosition = 0;
        private byte[] copyBuffer = new byte[LookbackCopyThreshold];

        public DeflateStreamOutput(Stream outstream)
        {
            this.outstream = outstream;
        }

        public override void Write(Stream source, int length)
        {
            while(length > 0)
            {
                var read = Math.Min(length, LookbackCopyThreshold);
                var actualRead = source.Read(copyBuffer, 0, read);

                this.outstream.Write(copyBuffer, 0, actualRead);
                length -= actualRead;

                Unsafe.CopyBlock(ref lookbackBuffer[lookbackPosition], ref copyBuffer[0], (uint)actualRead);
                lookbackPosition += actualRead;
                MaintainBuffer();
            }
        }

        public override void WriteByte(byte value)
        {
            outstream.WriteByte(value);
            lookbackBuffer[lookbackPosition] = value;
            lookbackPosition++;
            MaintainBuffer();
        }

        public override void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            Debug.Assert(lookbackDistance < lookbackPosition);

            var chunkLength = Math.Min(lookbackDistance, lengthToWrite);
            var start = this.lookbackPosition - lookbackDistance;

            var written = 0;
            while(written != lengthToWrite)
            {
                Array.Copy(this.lookbackBuffer, start, copyBuffer, written, chunkLength);
                written += chunkLength;
            }

            this.outstream.Write(this.copyBuffer, 0, written);
            Array.Copy(this.copyBuffer, 0, this.lookbackBuffer, this.lookbackPosition, written);
            this.lookbackPosition += written;

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
