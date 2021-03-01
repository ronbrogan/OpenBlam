using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenBlam.Core.Compression.Deflate
{
    internal sealed class HuffmanTree : IDisposable
    {
        private const int AlphabetSize = 288;
        private const int DistanceValuesSize = 33;

        private readonly static ArrayPool<TreeNode> NodePool = ArrayPool<TreeNode>.Create();
        private readonly static ArrayPool<byte> LengthPool = ArrayPool<byte>.Create();
        private readonly static ArrayPool<ushort> CodewordPool = ArrayPool<ushort>.Create();

        public readonly ushort LiteralLengthCodeCount;
        public readonly byte DistanceCodeCount;
        public readonly byte CodeLengthCodeCount;

        private TreeNode[] literalLengthTree;
        private TreeNode[] distanceTree;

        public HuffmanTree(BitSource data)
        {
            // Read 5 bits for LiteralLengthCodeCount
            this.LiteralLengthCodeCount = (ushort)(data.ReadBitsAsUshort(5) + 257);

            // Read 5 bits for DistanceCodeCount
            this.DistanceCodeCount = (byte)(data.ReadBitsAsUshort(5) + 1);

            // Read 4 bits for CodeLengthCodeCount
            this.CodeLengthCodeCount = (byte)(data.ReadBitsAsUshort(4) + 4);

            // Read code length intermediate tree data
            Span<byte> mapping = stackalloc byte[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
            var codeLengthCodes = new byte[19];
            for (var i = 0; i < this.CodeLengthCodeCount; i++)
            {
                codeLengthCodes[mapping[i]] = (byte)data.ReadBitsAsUshort(3);
            }

            // Generate intermediate tree
            var intermediateNodes = GenerateTreeNodes(codeLengthCodes, codeLengthCodes.Length);
            using var intermediateTree = new CodeLengthHuffmanTree(intermediateNodes);

            var literalLengths = LengthPool.Rent(AlphabetSize);
            var distanceLengths = LengthPool.Rent(DistanceValuesSize);

            intermediateTree.ProduceLengths(
                data,
                this.LiteralLengthCodeCount,
                this.DistanceCodeCount,
                literalLengths,
                distanceLengths);

            this.literalLengthTree = GenerateTreeNodes(literalLengths, AlphabetSize);
            this.distanceTree = GenerateTreeNodes(distanceLengths, DistanceValuesSize);

            LengthPool.Return(literalLengths, true);
            LengthPool.Return(distanceLengths, true);
        }

        public ushort GetValue(BitSource data)
        {
            var node = this.literalLengthTree[0];
            while (true)
            {
                int branch = (ushort)(node.Branches >> data.CurrentBitValueAs16());

                if (branch >= TreeNode.Threshold)
                {
                    // Take lower bits
                    return (ushort)(branch & 0x7FFF);
                }

                node = this.literalLengthTree[branch];
            }
        }

        public ushort GetLength(BitSource data, ushort value)
        {
            Debug.Assert(value > 256, "Value must be in the 'length' range");

            var index = value - 257;

            var length = DeflateConstants.LengthBase[index];

            if (index <= 7)
            {
                return length;
            }

            var extraBitsVal = data.ReadBitsAsUshort(DeflateConstants.LengthExtraBits[index]);
            return (ushort)(length + extraBitsVal);
        }

        public ushort GetDistance(BitSource data)
        {
            if (this.distanceTree == null)
            {
                // read 5 bits instead
                return data.ReadBitsAsUshort(5);
            }

            var node = this.distanceTree[0];
            int value;
            while (true)
            {
                int branch = (ushort)(node.Branches >> data.CurrentBitValueAs16());

                if (branch >= TreeNode.Threshold)
                {
                    // Take lower bits
                    value = branch & 0x7FFF;
                    break;
                }

                node = this.distanceTree[branch];
            }

            // do distance extra bits
            Debug.Assert(value < 30, "Value must be in 'distance' range");

            var distance = DeflateConstants.DistanceBase[value];

            if(value <= 3)
            {
                return distance;
            }

            var extraBitsVal = data.ReadBitsAsUshort(DeflateConstants.DistanceExtraBits[value]);
            return (ushort)(distance + extraBitsVal);
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        private static TreeNode[] GenerateTreeNodes(byte[] alphabetCodeLengths, int length)
        {
            ushort[] codewords = null;

            try
            {
                codewords = CodewordPool.Rent(length);
                FillAlphabetCodewords(alphabetCodeLengths, codewords, length);

                var tree = NodePool.Rent((length << 1) - 1);

                ushort nodeCount = 1; // root
                var currentNode = 0;

                for (ushort c = 0; c < length; c++)
                {
                    var len = alphabetCodeLengths[c];

                    if (len == 0)
                        continue;

                    ushort codeword = codewords[c];

                    // rhs is not significant here, just creating a ref local
                    ref var index = ref tree[currentNode].Left;

                    var bit = 1 << len - 1;
                    for (int i = 0; i < len; i++)
                    {
                        var isSet = (codeword & bit) == bit;
                        codeword <<= 1;

                        index = ref tree[currentNode].Left;

                        if (isSet)
                        {
                            index = ref tree[currentNode].Right;
                        }

                        if (index == 0)
                        {
                            index = nodeCount;
                            nodeCount++;
                        }

                        currentNode = index;
                    }

                    // Use current node to store value
                    nodeCount--;
                    index = (ushort)(TreeNode.Threshold | c);

                    // restart at root
                    currentNode = 0;
                }

                return tree;
            }
            finally
            {
                if(codewords != null)
                    CodewordPool.Return(codewords, true);
            }
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        // Output array maps alphabet value (index) to the codeword (bit sequence to arrive at the value)
        internal static void FillAlphabetCodewords(byte[] alphabetCodeLengths, ushort[] alphabetCodes, int length)
        {
            var maxBitLength = 15;

            // 1. Count the number of codes for each code length.
            Span<int> bitLengths = stackalloc int[maxBitLength];
            for (int i = 0; i < length; i++)
            {
                bitLengths[alphabetCodeLengths[i]]++;
            }

            // 2. Find the numerical value of the smallest code for each code length:
            var code = 0;
            bitLengths[0] = 0;
            Span<ushort> nextCode = stackalloc ushort[maxBitLength + 1];
            for (var bits = 1; bits <= maxBitLength; bits++)
            {
                code = (code + bitLengths[bits - 1]) << 1;
                nextCode[bits] = (ushort)code;
            }

            // 3. Assign numerical values to all codes, using consecutive
            //  values for all codes of the same length with the base
            //  values determined at step 2. Codes that are never used
            //  (which have a bit length of zero) must not be assigned a value.
            var len = 0;
            for (var n = 0; n < length; n++)
            {
                len = alphabetCodeLengths[n];
                if (len != 0)
                {
                    alphabetCodes[n] = nextCode[len];
                    nextCode[len]++;
                }
            }
        }

        public void Dispose()
        {
            NodePool.Return(this.distanceTree, true);
            NodePool.Return(this.literalLengthTree, true);
        }

        public static HuffmanTree Fixed => fixedTree.Value;

        private static Lazy<HuffmanTree> fixedTree = new Lazy<HuffmanTree>(() => new HuffmanTree());

        private HuffmanTree()
        {
            var fixedLengths = new byte[288];

            for (var i = 0; i < 288; i++)
            {
                if (i < 144)
                {
                    fixedLengths[i] = 8;
                }
                else if (i < 256)
                {
                    fixedLengths[i] = 9;
                }
                else if (i < 280)
                {
                    fixedLengths[i] = 7;
                }
                else
                {
                    fixedLengths[i] = 8;
                }
            }

            this.literalLengthTree = GenerateTreeNodes(fixedLengths, fixedLengths.Length);
            this.distanceTree = null;
        }

        private class CodeLengthHuffmanTree : IDisposable
        {
            private readonly TreeNode[] tree;

            public CodeLengthHuffmanTree(TreeNode[] tree)
            {
                this.tree = tree;
            }

            public void Dispose()
            {
                HuffmanTree.NodePool.Return(this.tree, true);
            }

            internal void ProduceLengths(
                BitSource data,
                ushort literalLengthCodeCount,
                byte distanceCodeCount,
                byte[] literalLengths,
                byte[] distanceLengths)
            {
                var valuesToProduce = literalLengthCodeCount + distanceCodeCount;

                var valuesProduced = 0;
                byte lastValueProduced = 0;
                while (valuesProduced < valuesToProduce)
                {
                    var node = this.tree[0];
                    byte codeLength = 0;

                    while(true)
                    {
                        var branch = (ushort)(node.Branches >> data.CurrentBitValueAs16());

                        if(branch >= TreeNode.Threshold)
                        {
                            // Take lower bits
                            codeLength = (byte)branch;
                            break;
                        }

                        node = this.tree[branch];
                    }

                    //0 - 15: Represent code lengths of 0 - 15
                    //    16: Copy the previous code length 3 - 6 times.
                    //        The next 2 bits indicate repeat length
                    //              (0 = 3, ... , 3 = 6)
                    //           Example: Codes 8, 16(+2 bits 11),
                    //                     16(+2 bits 10) will expand to
                    //                     12 code lengths of 8(1 + 6 + 5)
                    //    17: Repeat a code length of 0 for 3 - 10 times.
                    //        (3 bits of length)
                    //    18: Repeat a code length of 0 for 11 - 138 times
                    //        (7 bits of length)

                    switch (codeLength)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                            ProduceValue(codeLength);
                            break;
                        case 16:
                            ProduceRepeat(lastValueProduced, 3, bitsAsRepeat: 2);
                            break;
                        case 17:
                            ProduceRepeat(0, 3, bitsAsRepeat: 3);
                            break;
                        case 18:
                            ProduceRepeat(0, 11, bitsAsRepeat: 7);
                            break;
                    }
                }

                void ProduceValue(byte value)
                {
                    if (valuesProduced < literalLengthCodeCount)
                    {
                        literalLengths[valuesProduced] = value;
                    }
                    else
                    {
                        distanceLengths[valuesProduced - literalLengthCodeCount] = value;
                    }

                    lastValueProduced = value;
                    valuesProduced++;
                }

                void ProduceRepeat(byte value, int baseAmount, byte bitsAsRepeat)
                {
                    var repeatCount = data.ReadBitsAsUshort(bitsAsRepeat);

                    for (int i = 0; i < baseAmount + repeatCount; i++)
                    {
                        ProduceValue(value);
                    }
                }
            }
        }


        [StructLayout(LayoutKind.Explicit)]
        private struct TreeNode
        {
            public const ushort Threshold = 1 << 15;

            [FieldOffset(0)]
            public uint Branches;

            [FieldOffset(0)]
            public ushort Left;

            [FieldOffset(2)]
            public ushort Right;
        }
    }
}
