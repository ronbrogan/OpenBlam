using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe class DeflateOutputBuffer
    {
        private const int chunkSize = 1 << 18;

        private static ArrayPool<byte> outputBufferPool = ArrayPool<byte>.Shared;

        private List<GCHandle> memoryHandleList = new();
        private List<IntPtr> memoryPtrList = new();

        private int absolutePosition;
        private int currentChunkIndex;
        private int currentPosition;

        public int AbsolutePosition
        {
            get
            {
                return absolutePosition;
            }
            private set
            {
                absolutePosition = value;
                currentChunkIndex = Math.DivRem(value, chunkSize, out currentPosition);
            }
        }

        public DeflateOutputBuffer()
        {
            AllocateChunk();
        }

        public unsafe void Write(byte* data, int dataLength)
        {
            if (dataLength == 0) return;

            var written = 0;
            while (written != dataLength)
            {
                var chunkFree = chunkSize - currentPosition;

                var toWrite = Math.Min(chunkFree, dataLength - written);

                Buffer.MemoryCopy(data + written, (byte*)this.memoryPtrList[currentChunkIndex] + currentPosition, chunkFree, toWrite);
                written += toWrite;

                this.AbsolutePosition += toWrite;

                EnsureCapacity();
            }
        }

        public void WriteByte(byte value)
        {
            ((byte[])this.memoryHandleList[currentChunkIndex].Target)[currentPosition] = value;

            this.AbsolutePosition++;

            EnsureCapacity();
        }

        public unsafe void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            var windowStart = this.AbsolutePosition - lookbackDistance;
            var windowLength = Math.Min(lookbackDistance, lengthToWrite);

            var startChunkIndex = Math.DivRem(windowStart, chunkSize, out var startChunkPos);
            var endChunkIndex = Math.DivRem(windowStart + windowLength, chunkSize, out var endChunkPos);

            var splitChunkReadAt = -1;

            if(startChunkIndex != endChunkIndex)
            {
                splitChunkReadAt = chunkSize - startChunkPos;
            }

            var written = 0;
            while(written < lengthToWrite)
            {
                var toWrite = Math.Min(windowLength, lengthToWrite - written);

                var chunkToWrite = (int)Math.Min(toWrite, (uint)splitChunkReadAt);
                Write((byte*)this.memoryPtrList[startChunkIndex] + startChunkPos, chunkToWrite);
                written += chunkToWrite;

                var remainingToWrite = toWrite - chunkToWrite;
                Write((byte*)this.memoryPtrList[endChunkIndex], remainingToWrite);
                written += remainingToWrite;
            }
        }

        /// <summary>
        /// Writing will always stop at a chunk boundary and re-call EnsureCapacity
        /// This method just needs to check if the current chunk is allocated, and allocate if needed
        /// </summary>
        /// <param name="length"></param>
        private void EnsureCapacity()
        {
            var chunkIndex = this.AbsolutePosition / chunkSize;

            if (this.memoryHandleList.Count > chunkIndex)
            {
                return;
            }

            AllocateChunk();
        }

        private void AllocateChunk()
        {
            var newBuf = outputBufferPool.Rent(chunkSize);
            var handle = GCHandle.Alloc(newBuf, GCHandleType.Pinned);
            this.memoryHandleList.Add(handle);
            this.memoryPtrList.Add(handle.AddrOfPinnedObject());
        }

        public byte[] ToArray()
        {
            var output = new byte[AbsolutePosition];
            fixed (byte* outp = output)
            {
                var written = 0;
                for (int i = 0; i < memoryHandleList.Count - 1; i++)
                {
                    Buffer.MemoryCopy((byte*)this.memoryPtrList[i], outp + i * chunkSize, output.Length - written, chunkSize);
                    written += chunkSize;
                }

                var remaining = this.AbsolutePosition - written;

                Buffer.MemoryCopy((byte*)this.memoryPtrList[memoryHandleList.Count - 1], outp + written, output.Length - written, remaining);

                ReleaseResources();
            }

            return output;
        }

        private void ReleaseResources()
        {
            foreach (var handle in this.memoryHandleList)
            {
                var buf = (byte[])handle.Target;
                handle.Free();
                outputBufferPool.Return(buf);
            }
        }
    }
}
