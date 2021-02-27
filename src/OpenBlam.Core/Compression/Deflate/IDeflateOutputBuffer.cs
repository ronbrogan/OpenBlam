namespace OpenBlam.Core.Compression.Deflate
{
    internal interface IDeflateOutputBuffer
    {
        unsafe void Write(byte* data, int dataLength);
        void WriteByte(byte value);
        void WriteWindow(int lengthToWrite, int lookbackDistance);
    }
}