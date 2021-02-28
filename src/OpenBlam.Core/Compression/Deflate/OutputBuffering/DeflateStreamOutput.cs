using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe sealed class DeflateStreamOutput : DeflateOutput<Stream>
    {
        private readonly Stream outstream;
        private const int LookbackCopyThreshold = 2 * DeflateConstants.MaximumLookback;

        private GCHandle lookbackBufferHandle;
        private byte[] lookbackBufferObject;
        private byte* lookbackBuffer;

        private GCHandle copyBufferHandle;
        private byte[] copyBufferObject;
        private byte* copyBuffer;

        private long lookbackPosition = 0;
        public DeflateStreamOutput(Stream outstream)
        {
            this.outstream = outstream;

            this.lookbackBufferObject = new byte[3 * DeflateConstants.MaximumLookback];
            this.lookbackBufferHandle = GCHandle.Alloc(this.lookbackBufferObject, GCHandleType.Pinned);
            this.lookbackBuffer = (byte*)this.lookbackBufferHandle.AddrOfPinnedObject();


            this.copyBufferObject = new byte[LookbackCopyThreshold];
            this.copyBufferHandle = GCHandle.Alloc(this.copyBufferObject, GCHandleType.Pinned);
            this.copyBuffer = (byte*)this.copyBufferHandle.AddrOfPinnedObject();
        }

        public override void Write(Stream source, int length)
        {
            while(length > 0)
            {
                var read = Math.Min(length, LookbackCopyThreshold);
                var actualRead = source.Read(this.copyBufferObject, 0, read);

                this.outstream.Write(this.copyBufferObject, 0, actualRead);
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

            long chunkLength = Math.Min(lookbackDistance, lengthToWrite);
            var start = this.lookbackPosition - lookbackDistance;

            var source = this.lookbackBuffer + start;
            long written = 0;
            while (written != lengthToWrite)
            {
                var dest = this.copyBuffer + written;
                Buffer.MemoryCopy(source, dest, chunkLength, chunkLength);
                written += chunkLength;
            }

            this.outstream.Write(this.copyBufferObject, 0, (int)written);

            Buffer.MemoryCopy(this.copyBuffer, this.lookbackBuffer + this.lookbackPosition, written, written);
            this.lookbackPosition += written;

            MaintainBuffer();
        }

        private void MaintainBuffer()
        {
            if(lookbackPosition < LookbackCopyThreshold)
            {
                return;
            }

            Buffer.MemoryCopy(
                this.lookbackBuffer + lookbackPosition - DeflateConstants.MaximumLookback,
                this.lookbackBuffer,
                DeflateConstants.MaximumLookback,
                DeflateConstants.MaximumLookback);

            lookbackPosition = DeflateConstants.MaximumLookback;
        }
    }
}
