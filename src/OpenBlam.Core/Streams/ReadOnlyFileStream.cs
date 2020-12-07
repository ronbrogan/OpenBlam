using OpenBlam.Core.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenBlam.Core.Streams
{
    /// <summary>
    /// Wrapper over FileStream to reduce copying when reading small
    /// values in a forward manner.
    /// Is NOT thread safe
    /// </summary>
    public class ReadOnlyFileStream : Stream
    {
        internal const int BufferSize = 80000;
        private FileStream fs;
        private long fsLength;
        private byte[] buffer = new byte[BufferSize];
        /// <summary>Where data[0] is from in the stream</summary>
        private int bufferOffset = 0;
        /// <summary>Where Stream.Position (absolute) points to in buffer</summary>
        private int internalOffset = 0;
        private int position => bufferOffset + internalOffset;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => fsLength;
        public override long Position { get => position; set => EnsureRead((int)value, 0); }

        public ReadOnlyFileStream(string path)
        {
            this.fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.fsLength = fs.Length;
            this.fs.Read(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByteAt(int offset) => buffer[EnsureRead(offset, 1)];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16At(int offset)
        {
            return Unsafe.ReadUnaligned<short>(ref this.buffer[EnsureRead(offset, 2)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32At(int offset)
        {
            return Unsafe.ReadUnaligned<int>(ref this.buffer[EnsureRead(offset, 4)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16At(int offset)
        {
            return Unsafe.ReadUnaligned<ushort>(ref this.buffer[EnsureRead(offset, 2)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32At(int offset)
        {
            return Unsafe.ReadUnaligned<uint>(ref this.buffer[EnsureRead(offset, 4)]);
        }

        /// <summary>
        /// Ensures that the data is available in the buffer
        /// </summary>
        /// <param name="desiredOffset">The desired offset in the stream to read from</param>
        /// <param name="length">The amount of data that is needed</param>
        /// <returns>The offset that should be read from within the current buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EnsureRead(int desiredOffset, int length)
        {
            if (length > BufferSize) Throw.NotSupported("Desired data is too large for this API");

            // current <= desired && desired end < currentEnd
            if(this.bufferOffset <= desiredOffset && desiredOffset + length <= this.bufferOffset + BufferSize)
            {
                this.internalOffset = (desiredOffset - this.bufferOffset) + length;
                return desiredOffset - this.bufferOffset;
            }
            else
            {
                this.fs.Position = desiredOffset;
                this.bufferOffset = desiredOffset;
                this.internalOffset = length;
                this.fs.Read(this.buffer);
                return 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var capable = Math.Min(BufferSize, count);

            var start = EnsureRead(this.position, capable);
            Buffer.BlockCopy(this.buffer, start, buffer, offset, capable);
            return capable;
        }

        public ReadOnlySpan<byte> Read(int offset, int count)
        {
            return ((Span<byte>)this.buffer).Slice(EnsureRead(offset, count));
        }

        public override long Seek(long offset, SeekOrigin origin) => fs.Seek(offset, origin);
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
