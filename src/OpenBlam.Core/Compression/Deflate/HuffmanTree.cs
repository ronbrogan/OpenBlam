using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression.Deflate
{
    internal class HuffmanTree
    {
        private const int AlphabetSize = 288;
        private const int DistanceValuesSize = 33;

        public readonly ushort LiteralLengthCodeCount;
        public readonly byte DistanceCodeCount;
        public readonly byte CodeLengthCodeCount;

        private TreeNode[] literalLengthTree;
        private TreeNode[] distanceTree;

        public HuffmanTree(BitSource data)
        {
            // Read 5 bits for LiteralLengthCodeCount
            LiteralLengthCodeCount = (ushort)(data.ReadBitsAsUshort(5) + 257);

            // Read 5 bits for DistanceCodeCount
            DistanceCodeCount = (byte)(data.ReadBitsAsUshort(5) + 1);

            // Read 4 bits for CodeLengthCodeCount
            CodeLengthCodeCount = (byte)(data.ReadBitsAsUshort(4) + 4);

            // Read code length intermediate tree data
            Span<byte> mapping = stackalloc byte[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
            var codeLengthCodes = new byte[19];
            for (var i = 0; i < CodeLengthCodeCount; i++)
            {
                codeLengthCodes[mapping[i]] = (byte)data.ReadBitsAsUshort(3);
            }

            // Generate intermediate tree
            var intermediateNodes = GenerateTreeNodes(codeLengthCodes);
            var intermediateTree = new CodeLengthHuffmanTree(intermediateNodes);

            var (literalLengths, distanceLengths) = intermediateTree.ProduceLengths(
                data,
                LiteralLengthCodeCount,
                DistanceCodeCount);

            this.literalLengthTree = GenerateTreeNodes(literalLengths);
            this.distanceTree = GenerateTreeNodes(distanceLengths);
        }

        public ushort GetValue(BitSource data)
        {
            var node = this.literalLengthTree[0];
            while (true)
            {
                if (node.Branches == 0)
                {
                    return node.Value;
                }

                var branchIndex = (ushort)(node.Branches >> (data.CurrentBitValue() << 4));
                node = this.literalLengthTree[branchIndex];
            }
        }

        public ushort GetLength(BitSource data, ushort value)
        {
            Debug.Assert(value > 256, "Value must be in the 'length' range");

            var index = value - 257;

            var extraBitsVal = data.ReadBitsAsUshort(DeflateConstants.LengthExtraBits[index]);
            return (ushort)(DeflateConstants.LengthBase[index] + extraBitsVal);
        }

        public ushort GetDistance(BitSource data)
        {
            if (this.distanceTree == null)
            {
                // read 5 bits instead
                return data.ReadBitsAsUshort(5);
            }

            var node = this.distanceTree[0];
            ushort value;
            while (true)
            {
                if (node.Branches == 0)
                {
                    value = node.Value;
                    break;
                }

                var branchIndex = (ushort)(node.Branches >> (data.CurrentBitValue() << 4));
                node = this.distanceTree[branchIndex];
            }

            // do distance extra bits
            Debug.Assert(value < 30, "Value must be in 'distance' range");

            var extraBitsVal = data.ReadBitsAsUshort(DeflateConstants.DistanceExtraBits[value]);
            return (ushort)(DeflateConstants.DistanceBase[value] + extraBitsVal);
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        private static TreeNode[] GenerateTreeNodes(byte[] alphabetCodeLengths)
        {
            var codewords = GetAlphabetCodewords(alphabetCodeLengths);

            Span<TreeNode> tree = stackalloc TreeNode[(codewords.Length << 1) - 1];

            ushort nodeCount = 1; // root
            var currentNode = 0;

            for (ushort c = 0; c < codewords.Length; c++)
            {
                ushort codeword = codewords[c];

                var len = alphabetCodeLengths[c];

                if (len == 0)
                    continue;

                for (int i = 0; i < len; i++)
                {
                    var isSet = ((codeword >> (len - i - 1)) & 1) == 1;

                    ref var index = ref tree[currentNode].Left;

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
                tree[currentNode].Value = c;

                // restart at root
                currentNode = 0;
            }

            return tree.Slice(0, nodeCount).ToArray();
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        // Output array maps alphabet value (index) to the codeword (bit sequence to arrive at the value)
        internal static ushort[] GetAlphabetCodewords(byte[] alphabetCodeLengths)
        {
            var maxBitLength = 15;
            var alphabetCodes = new ushort[alphabetCodeLengths.Length];

            // 1. Count the number of codes for each code length.
            var bitLengths = new int[maxBitLength];
            for (int i = 0; i < alphabetCodeLengths.Length; i++)
            {
                var length = alphabetCodeLengths[i];

                bitLengths[length]++;
            }

            // 2. Find the numerical value of the smallest code for each code length:
            var code = 0;
            bitLengths[0] = 0;
            var nextCode = new ushort[maxBitLength + 1];
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
            for (var n = 0; n < alphabetCodeLengths.Length; n++)
            {
                len = alphabetCodeLengths[n];
                if (len != 0)
                {
                    alphabetCodes[n] = nextCode[len];
                    nextCode[len]++;
                }
            }

            return alphabetCodes;
        }

        public static HuffmanTree Fixed { get; set; } = new HuffmanTree();

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

            this.literalLengthTree = GenerateTreeNodes(fixedLengths);
            this.distanceTree = null;
        }

        private class CodeLengthHuffmanTree
        {
            private readonly TreeNode[] tree;

            public CodeLengthHuffmanTree(TreeNode[] tree)
            {
                this.tree = tree;
            }

            internal (byte[] literalLengths, byte[] distanceLengths) ProduceLengths(
                BitSource data,
                ushort literalLengthCodeCount,
                byte distanceCodeCount)
            {
                var valuesToProduce = literalLengthCodeCount + distanceCodeCount;
                var literalLengths = new byte[AlphabetSize];
                var distanceLengths = new byte[DistanceValuesSize];

                var valuesProduced = 0;
                byte lastValueProduced = 0;
                while (valuesProduced < valuesToProduce)
                {
                    var node = tree[0];
                    byte codeLength = 0;

                    while (true)
                    {
                        if (node.Branches == 0)
                        {
                            codeLength = (byte)node.Value;
                            break;
                        }

                        var branchIndex = (ushort)(node.Branches >> (data.CurrentBitValue() << 4));
                        node = tree[branchIndex];
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

                return (literalLengths, distanceLengths);

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

                void ProduceRepeat(byte value, int baseAmount, int bitsAsRepeat)
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
            [FieldOffset(0)]
            public uint Branches;

            [FieldOffset(0)]
            public ushort Left;

            [FieldOffset(2)]
            public ushort Right;

            [FieldOffset(4)]
            public ushort Value;
        }
    }
}
