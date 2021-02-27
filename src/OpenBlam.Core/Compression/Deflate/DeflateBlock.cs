namespace OpenBlam.Core.Compression.Deflate
{
    internal enum BlockType : byte
    {
        NoCompression = 0,
        FixedHuffmanCodes = 1,
        DynamicHuffmanCodes = 2,
        Reserved = 3,
    }

    internal struct DeflateBlock
    {
        public HuffmanTree HuffmanTree;
        public IBitSource Compressed;
        public bool IsFinal;
        public BlockType Type;

        public DeflateBlock(IBitSource data)
        {
            Compressed = data;

            IsFinal = data.IsSet();

            Type = (BlockType)data.ReadBitsAsUshort(2);

            if (Type == BlockType.DynamicHuffmanCodes)
            {
                HuffmanTree = new HuffmanTree(data);
            }
            else if (Type == BlockType.FixedHuffmanCodes)
            {
                HuffmanTree = HuffmanTree.Fixed;
            }
            else
            {
                HuffmanTree = null;
            }
        }

        public ushort GetNextValue()
        {
            return this.HuffmanTree.GetValue(Compressed);
        }

        public (ushort length, ushort distance) GetLengthAndDistance(ushort rawValue)
        {
            var length = this.HuffmanTree.GetLength(Compressed, rawValue);
            var distance = this.HuffmanTree.GetDistance(Compressed);
            return (length, distance);
        }
    }
}
