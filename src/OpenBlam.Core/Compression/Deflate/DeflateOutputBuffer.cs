using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe class DeflateOutputBuffer
    {
        // CHUNK_SIZE should never be under 65535, this way we can 
        // guarantee only one possible chunk gap when reading/writing
        private const int CHUNK_SIZE = 1 << 18;

        private static ArrayPool<byte> outputBufferPool = ArrayPool<byte>.Shared;

        private List<GCHandle> memoryHandleList = new();
        private List<IntPtr> memoryPtrList = new();

        private int absolutePosition;
        private int currentChunkIndex;
        private int currentPosition;
        private byte* currentChunk;

        public int AbsolutePosition => absolutePosition;

        public DeflateOutputBuffer()
        {
            AllocateChunk();
            this.currentChunk = (byte*)this.memoryPtrList[this.currentChunkIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte* data, int dataLength)
        {
            Debug.Assert(dataLength <= 65535);

            while(dataLength > 0)
            {
                var chunkFree = CHUNK_SIZE - currentPosition;
                var toWrite = FastMin(chunkFree, dataLength);
                Buffer.MemoryCopy(data, this.currentChunk + currentPosition, chunkFree, toWrite);
                this.Advance(toWrite);
                dataLength -= toWrite;
                data += toWrite;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            *(this.currentChunk + currentPosition) = value;
            this.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            var windowStart = this.AbsolutePosition - lookbackDistance;
            var windowLength = FastMin(lookbackDistance, lengthToWrite);

            int startChunkIndex;
            int startChunkPos;
            int endChunkIndex;

            startChunkIndex = Math.DivRem(windowStart, CHUNK_SIZE, out startChunkPos);
            endChunkIndex = (windowStart + windowLength) / CHUNK_SIZE;

            byte* startPtr = (byte*)this.memoryPtrList[startChunkIndex] + startChunkPos;
            byte* endPtr = (byte*)0;

            var splitChunkReadAt = -1;
            if(startChunkIndex != endChunkIndex)
            {
                splitChunkReadAt = CHUNK_SIZE - startChunkPos;
                endPtr = (byte*)this.memoryPtrList[endChunkIndex];
            }

            while(lengthToWrite > 0)
            {
                var toWrite = Math.Min(windowLength, lengthToWrite);

                var chunkToWrite = (int)Math.Min(toWrite, (uint)splitChunkReadAt);
                Write(startPtr, chunkToWrite);

                var remainingToWrite = toWrite - chunkToWrite;
                Write(endPtr, remainingToWrite);

                lengthToWrite -= toWrite;
            }
        }

        /// <summary>
        /// Writing must always stop at a chunk boundary and Advance the position
        /// This method will allocate the next chunk and set appropriate values
        /// </summary>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int delta)
        {
            this.absolutePosition += delta;
            this.currentPosition += delta;

            var chunkOverflow = this.currentPosition - CHUNK_SIZE;
            if (chunkOverflow >= 0)
            {
                this.currentPosition = chunkOverflow;
                this.currentChunkIndex++;
                this.currentChunk = AllocateChunk();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* AllocateChunk()
        {
            var newBuf = outputBufferPool.Rent(CHUNK_SIZE);
            var handle = GCHandle.Alloc(newBuf, GCHandleType.Pinned);
            this.memoryHandleList.Add(handle);
            var ptr = handle.AddrOfPinnedObject();
            this.memoryPtrList.Add(ptr);
            return (byte*)ptr;
        }

        public byte[] ToArray()
        {
            var output = new byte[AbsolutePosition];
            fixed (byte* outp = output)
            {
                var written = 0;
                for (int i = 0; i < memoryHandleList.Count - 1; i++)
                {
                    Buffer.MemoryCopy((byte*)this.memoryPtrList[i], outp + i * CHUNK_SIZE, output.Length - written, CHUNK_SIZE);
                    written += CHUNK_SIZE;
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

        private static int FastMin(int a, int b)
        {
            // signed for arithmetic shift
            int mask = b - a;
            // mask < 0 means MSB is 1.
            return a + (mask & (mask >> 31));
        }
    }
}
