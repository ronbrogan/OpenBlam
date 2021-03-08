using OpenBlam.Core.Collections;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe sealed class DeflateStreamOutput : DeflateOutput<Stream>, IDisposable
    {
        private static PinnedArrayPool<byte> bufferPool = PinnedArrayPool<byte>.Create();

        private readonly Stream outstream;
        private const int LookbackCopyThreshold = 2 * DeflateConstants.MaximumLookback;

        private byte[] lookbackBufferObject;
        private byte* lookbackBuffer;

        private byte[] copyBufferObject;
        private byte* copyBuffer;

        private long lookbackPosition = 0;
        public DeflateStreamOutput(Stream outstream)
        {
            this.outstream = outstream;

            this.lookbackBufferObject = bufferPool.Rent(3 * DeflateConstants.MaximumLookback, out this.lookbackBuffer);

            this.copyBufferObject = bufferPool.Rent(LookbackCopyThreshold, out this.copyBuffer);
        }

        public override void Write(Stream source, int length)
        {
            while(length > 0)
            {
                var read = Math.Min(length, LookbackCopyThreshold);
                var actualRead = source.Read(this.copyBufferObject, 0, read);

                this.outstream.Write(this.copyBufferObject, 0, actualRead);
                length -= actualRead;

                Unsafe.CopyBlock(ref this.lookbackBuffer[this.lookbackPosition], ref this.copyBuffer[0], (uint)actualRead);
                this.lookbackPosition += actualRead;
                this.MaintainBuffer();
            }
        }

        public override void WriteByte(byte value)
        {
            this.outstream.WriteByte(value);
            this.lookbackBuffer[this.lookbackPosition] = value;
            this.lookbackPosition++;
            this.MaintainBuffer();
        }

        public override void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            Debug.Assert(lookbackDistance <= this.lookbackPosition, $"Lookback distance '{lookbackDistance}' is too high");

            long chunkLength = Math.Min(lookbackDistance, lengthToWrite);
            var start = this.lookbackPosition - lookbackDistance;

            Buffer.MemoryCopy(this.lookbackBuffer + start, this.copyBuffer, chunkLength, chunkLength);

            long written = chunkLength;
            while (written < lengthToWrite)
            {
                if (chunkLength == 1)
                {
                    *(this.copyBuffer + written) = *this.copyBuffer;
                }
                else if (chunkLength < 32 && Avx2.IsSupported)
                {
                    var t = Avx2.LoadVector256(this.copyBuffer + written - chunkLength);
                    Avx2.Store(this.copyBuffer + written, t);
                }
                else
                {
                    Buffer.MemoryCopy(this.copyBuffer + written - chunkLength, this.copyBuffer + written, chunkLength, chunkLength);
                }

                written += chunkLength;
            }

            this.outstream.Write(this.copyBufferObject, 0, lengthToWrite);

            Buffer.MemoryCopy(this.copyBuffer, this.lookbackBuffer + this.lookbackPosition, lengthToWrite, lengthToWrite);
            this.lookbackPosition += lengthToWrite;

            this.MaintainBuffer();
        }

        // TODO: remove copy, use circular buffer strategy
        private void MaintainBuffer()
        {
            if(this.lookbackPosition < LookbackCopyThreshold)
            {
                return;
            }

            Buffer.MemoryCopy(
                this.lookbackBuffer + this.lookbackPosition - DeflateConstants.MaximumLookback,
                this.lookbackBuffer,
                DeflateConstants.MaximumLookback,
                DeflateConstants.MaximumLookback);

            this.lookbackPosition = DeflateConstants.MaximumLookback;
        }

        public void Dispose()
        {
            bufferPool.Return(this.copyBufferObject);
            bufferPool.Return(this.lookbackBufferObject);
        }
    }
}
