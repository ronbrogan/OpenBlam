using System;

namespace OpenBlam.Core.Compression.Deflate
{
    internal enum BlockType : byte
    {
        NoCompression = 0,
        FixedHuffmanCodes = 1,
        DynamicHuffmanCodes = 2,
        Reserved = 3,
    }

    internal struct DeflateBlock : IDisposable
    {
        public HuffmanTree HuffmanTree;
        public BitSource Compressed;
        public bool IsFinal;
        public BlockType Type;

        public DeflateBlock(BitSource data)
        {
            this.Compressed = data;

            this.IsFinal = data.IsSet();

            this.Type = (BlockType)data.ReadBitsAsUshort(2);

            if (this.Type == BlockType.DynamicHuffmanCodes)
            {
                this.HuffmanTree = new HuffmanTree(data);
            }
            else if (this.Type == BlockType.FixedHuffmanCodes)
            {
                this.HuffmanTree = HuffmanTree.Fixed;
            }
            else
            {
                this.HuffmanTree = null;
            }
        }

        public uint GetNextValue()
        {
            return this.HuffmanTree.GetValue();
        }

        public void GetLengthAndDistance(uint rawValue, out int length, out int distance)
        {
            length = this.HuffmanTree.GetLength(rawValue);
            distance = this.HuffmanTree.GetDistance();
        }

        public void Dispose()
        {
            if (this.Type == BlockType.DynamicHuffmanCodes)
            {
                this.HuffmanTree?.Dispose();
            }
        }
    }
}
