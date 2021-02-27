namespace OpenBlam.Core.Compression.Deflate
{
    public interface IBitSource
    {
        ulong CurrentBit { get; }

        byte BitValue();
        void ConsumeBit(byte count);
        void ConsumeBytes(ulong byteCount);
        byte CurrentBitValue();
        byte CurrentBitValueAs16();
        bool IsSet();
        ushort ReadBitsAsUshort(byte bits);
        void SkipToNextByte();
    }
}