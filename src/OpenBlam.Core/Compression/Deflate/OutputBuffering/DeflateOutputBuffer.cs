using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe sealed class DeflateOutputBuffer : DeflateOutput<byte[]>, IDisposable
    {
        // CHUNK_SIZE should never be under 65535, this way we can 
        // guarantee only one possible chunk gap when reading/writing
        private const int CHUNK_SIZE = 1 << 18;

        private static ArrayPool<byte> outputBufferPool = ArrayPool<byte>.Shared;

        private List<(GCHandle handle, long length)> memoryHandleList = new();

        private int absolutePosition = 0;
        private int currentChunkIndex = 0;
        private long currentPosition = 0;
        private byte* currentChunk;
        private byte* previousChunk;
        private long previousLength;

        public DeflateOutputBuffer()
        {
            this.currentChunk = AllocateChunk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(byte[] data, int dataLength)
        {
            fixed (byte* b = data)
                Write(b, dataLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(byte* data, long dataLength)
        {
            Debug.Assert(dataLength <= 65535);

            if (dataLength == 0) return;

            if (currentPosition + dataLength >= CHUNK_SIZE)
            {
                this.previousChunk = this.currentChunk;
                this.previousLength = this.currentPosition;
                this.memoryHandleList[this.currentChunkIndex] = (this.memoryHandleList[this.currentChunkIndex].handle, this.currentPosition);

                this.currentChunk = AllocateChunk();
                this.currentPosition = 0;
                this.currentChunkIndex++;
            }

            Buffer.MemoryCopy(data, this.currentChunk + this.currentPosition, dataLength, dataLength);
            this.currentPosition += dataLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte value)
        {
            *this.GetWriteLocation(1) = value;
            this.currentPosition++;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void WriteWindow(int lengthToWrite, int lookbackDistance)
        {
            var windowStart = this.currentPosition - lookbackDistance;
            var windowLength = Math.Min(lookbackDistance, lengthToWrite);

            long nextChunkWrite = 0;

            // By default assume that the lookback can be sourced from within current chunk
            byte* startPtr = this.currentChunk + windowStart;
            byte* endPtr = this.currentChunk;

            // Detect if we need to split reads to prior chunk
            if (windowStart < 0)
            {
                nextChunkWrite = windowLength + windowStart;
                if (nextChunkWrite < 0) nextChunkWrite = 0;

                windowStart = this.previousLength + windowStart;
                startPtr = this.previousChunk + windowStart;
            }

            do
            {
                var toWrite = Math.Min(windowLength, lengthToWrite);
                lengthToWrite -= toWrite;

                Write(startPtr, toWrite - nextChunkWrite);
                Write(endPtr, nextChunkWrite);
            } while (lengthToWrite > 0);
        }

        /// <summary>
        /// Writing must always stop at a chunk boundary and Advance the position
        /// This method will allocate the next chunk and set appropriate values
        /// </summary>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetWriteLocation(int size)
        {
            if (currentPosition + size >= CHUNK_SIZE)
            {
                this.previousChunk = this.currentChunk;
                this.previousLength = this.currentPosition;
                this.memoryHandleList[this.currentChunkIndex] = (this.memoryHandleList[this.currentChunkIndex].handle, this.currentPosition);

                this.currentChunk = AllocateChunk();
                this.currentPosition = 0;
                this.currentChunkIndex++;
            }

            return this.currentChunk + this.currentPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* AllocateChunk()
        {
            var newBuf = outputBufferPool.Rent(CHUNK_SIZE);
            var handle = GCHandle.Alloc(newBuf, GCHandleType.Pinned);
            this.memoryHandleList.Add((handle, 0));
            var ptr = handle.AddrOfPinnedObject();
            return (byte*)ptr;
        }

        public byte[] ToArray()
        {
            this.memoryHandleList[this.memoryHandleList.Count - 1] = (this.memoryHandleList[this.memoryHandleList.Count - 1].handle, this.currentPosition);

            long total = 0;
            foreach(var (_, length) in this.memoryHandleList)
            {
                total += length;
            }

            var output = new byte[total];
            long written = 0;
            fixed (byte* outp = output)
            {
                foreach(var (handle, length) in this.memoryHandleList)
                {
                    Buffer.MemoryCopy((byte*)handle.AddrOfPinnedObject(), outp + written, total - written, length);
                    written += length;
                }
            }

            return output;
        }

        public void Dispose()
        {
            lock (this.memoryHandleList)
            {
                for (var i = 0; i < this.memoryHandleList.Count; i++)
                {
                    var (handle, _) = this.memoryHandleList[i];
                    var buf = (byte[])handle.Target;
                    handle.Free();
                    outputBufferPool.Return(buf);
                }

                this.memoryHandleList = null;
            }
        }
    }
}
