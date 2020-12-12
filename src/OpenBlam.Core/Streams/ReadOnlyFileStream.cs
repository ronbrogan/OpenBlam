using OpenBlam.Core.Exceptions;
using OpenBlam.Serialization.Materialization;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Streams
{
    /// <summary>
    /// Wrapper over FileStream to reduce copying when reading small
    /// values in a forward manner.
    /// Is NOT thread safe
    /// </summary>
    public class ReadOnlyFileStream : BinaryReadableStream
    {
        internal const int BufferSize = 80000;
        private FileStream fs;
        private long fsLength;
        private byte[] buffer = new byte[BufferSize];
        /// <summary>Where data[0] is from in the stream</summary>
        private long bufferOffset = 0;
        /// <summary>Where Stream.Position (absolute) points to in buffer</summary>
        private long internalOffset = 0;

        private int validBufferDataLength = 0;

        private long position => bufferOffset + internalOffset;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => fsLength;
        public override long Position { get => position; set => EnsureRead(value, 0); }

        public ReadOnlyFileStream(string path)
        {
            this.fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.fsLength = fs.Length;
            this.fs.Read(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte ReadByteAt(int offset) => buffer[EnsureRead(offset, 1)];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override short ReadInt16At(int offset)
        {
            return Unsafe.ReadUnaligned<short>(ref this.buffer[EnsureRead(offset, 2)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort ReadUInt16At(int offset)
        {
            return Unsafe.ReadUnaligned<ushort>(ref this.buffer[EnsureRead(offset, 2)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int ReadInt32At(int offset)
        {
            return Unsafe.ReadUnaligned<int>(ref this.buffer[EnsureRead(offset, 4)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint ReadUInt32At(int offset)
        {
            return Unsafe.ReadUnaligned<uint>(ref this.buffer[EnsureRead(offset, 4)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ReadFloatAt(int offset)
        {
            return Unsafe.ReadUnaligned<float>(ref this.buffer[EnsureRead(offset, 4)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double ReadDoubleAt(int offset)
        {
            return Unsafe.ReadUnaligned<double>(ref this.buffer[EnsureRead(offset, 8)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Vector2 ReadVec2At(int offset)
        {
            return Unsafe.ReadUnaligned<Vector2>(ref this.buffer[EnsureRead(offset, 8)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Vector3 ReadVec3At(int offset)
        {
            return Unsafe.ReadUnaligned<Vector3>(ref this.buffer[EnsureRead(offset, 12)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Vector4 ReadVec4At(int offset)
        {
            return Unsafe.ReadUnaligned<Vector4>(ref this.buffer[EnsureRead(offset, 16)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Quaternion ReadQuaternionAt(int offset)
        {
            return Unsafe.ReadUnaligned<Quaternion>(ref this.buffer[EnsureRead(offset, 16)]);
        }

        /// <summary>
        /// Ensures that the data is available in the buffer
        /// </summary>
        /// <param name="desiredOffset">The desired offset in the stream to read from</param>
        /// <param name="length">The amount of data that is needed</param>
        /// <returns>The offset that should be read from within the current buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EnsureRead(long desiredOffset, int length)
        {
            if (length > BufferSize) Throw.NotSupported("Desired data is too large for this API");

            // current <= desired && desired end < currentEnd
            if(this.bufferOffset <= desiredOffset && desiredOffset + length <= this.bufferOffset + this.validBufferDataLength)
            {
                this.internalOffset = (desiredOffset - this.bufferOffset) + length;
                return (int)(desiredOffset - this.bufferOffset);
            }
            else
            {
                this.fs.Position = desiredOffset;
                this.bufferOffset = desiredOffset;
                this.internalOffset = length;
                this.validBufferDataLength = this.fs.Read(this.buffer);
                return 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Handle larger than internal buffer reads separately
            if(count > this.validBufferDataLength)
            {
                this.fs.Position = this.position;
                var amountRead = fs.Read(buffer, offset, count);

                // Update internal buffer, prep for next read
                this.bufferOffset = this.position + amountRead;
                this.internalOffset = 0;
                this.validBufferDataLength = this.fs.Read(this.buffer);
                return amountRead;
            }

            var start = EnsureRead(this.position, count);
            var availableData = Math.Min(this.validBufferDataLength, count);
            Buffer.BlockCopy(this.buffer, start, buffer, offset, availableData);
            return count;
        }

        public ReadOnlySpan<byte> Read(int offset, int count)
        {
            return ((Span<byte>)this.buffer).Slice(EnsureRead(offset, count));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPos = fs.Seek(offset, origin);
            EnsureRead(newPos, 0);
            return newPos;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if(disposing)
                this.fs?.Dispose();

            base.Dispose(disposing);
        }
    }
}
