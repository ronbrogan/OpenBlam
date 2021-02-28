using System;

namespace OpenBlam.Core.Compression.Deflate
{
    public abstract class DeflateOutput<T>
    {
        public abstract void Write(T source, int length);

        public abstract void WriteByte(byte value);

        public abstract void WriteWindow(int lengthToWrite, int lookbackDistance);
    }
}