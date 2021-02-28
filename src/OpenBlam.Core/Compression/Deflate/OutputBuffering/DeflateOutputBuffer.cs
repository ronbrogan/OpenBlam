using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe sealed class DeflateOutputBuffer : DeflateOutput<byte[]>, IDisposable
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

        public DeflateOutputBuffer()
        {
            AllocateChunk();
            this.currentChunk = (byte*)this.memoryPtrList[this.currentChunkIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(byte[] data, int dataLength)
        {
            fixed (byte* b = data)
                Write(b, dataLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(byte* data, int dataLength)
        {
            Debug.Assert(dataLength <= 65535);

            while (dataLength > 0)
            {
                var chunkFree = CHUNK_SIZE - currentPosition;
                var toWrite = Math.Min(chunkFree, dataLength);
                Buffer.MemoryCopy((byte*)data, this.currentChunk + currentPosition, chunkFree, toWrite);
                this.Advance(toWrite);
                dataLength -= toWrite;
                data += toWrite;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte value)
        {
            *(this.currentChunk + currentPosition) = value;
            this.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            var windowStart = this.absolutePosition - lookbackDistance;
            var windowLength = Math.Min(lookbackDistance, lengthToWrite);

            var startChunkIndex = Math.DivRem(windowStart, CHUNK_SIZE, out var startChunkPos);

            byte* startPtr = (byte*)this.memoryPtrList[startChunkIndex] + startChunkPos;
            byte* endPtr = (byte*)0;

            var nextChunkWrite = (startChunkPos + windowLength) - CHUNK_SIZE;
            if (nextChunkWrite <= 0)
            {
                nextChunkWrite = 0;
            }
            else
            {
                endPtr = (byte*)this.memoryPtrList[startChunkIndex + 1]; 
            }

            do
            {
                var toWrite = Math.Min(windowLength, lengthToWrite);

                Write(startPtr, toWrite - nextChunkWrite);
                Write(endPtr, nextChunkWrite);

                lengthToWrite -= toWrite;
            } while (lengthToWrite > 0);
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

            var newPos = this.currentPosition + delta;
            var chunkOverflow = newPos - CHUNK_SIZE;
            if (chunkOverflow < 0)
            {
                this.currentPosition = newPos;
                return;
            }

            this.currentPosition = chunkOverflow;
            this.currentChunkIndex++;
            this.currentChunk = AllocateChunk();
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
            var output = new byte[absolutePosition];
            fixed (byte* outp = output)
            {
                var written = 0;
                for (int i = 0; i < memoryHandleList.Count - 1; i++)
                {
                    Buffer.MemoryCopy((byte*)this.memoryPtrList[i], outp + i * CHUNK_SIZE, output.Length - written, CHUNK_SIZE);
                    written += CHUNK_SIZE;
                }

                var remaining = this.absolutePosition - written;
                Buffer.MemoryCopy((byte*)this.memoryPtrList[memoryHandleList.Count - 1], outp + written, output.Length - written, remaining);
            }

            return output;
        }

        public void Dispose()
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            lock (this.memoryHandleList)
                foreach (var handle in this.memoryHandleList)
                {
                    var buf = (byte[])handle.Target;
                    handle.Free();
                    outputBufferPool.Return(buf);
                    this.memoryHandleList.Remove(handle);
                }
        }
    }
}
