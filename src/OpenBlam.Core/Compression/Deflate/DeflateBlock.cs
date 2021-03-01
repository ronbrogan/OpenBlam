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

        public ushort GetNextValue()
        {
            return this.HuffmanTree.GetValue(this.Compressed);
        }

        public (ushort length, ushort distance) GetLengthAndDistance(ushort rawValue)
        {
            var length = this.HuffmanTree.GetLength(this.Compressed, rawValue);
            var distance = this.HuffmanTree.GetDistance(this.Compressed);
            return (length, distance);
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
