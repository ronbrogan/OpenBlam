using OpenBlam.Core.Collections;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenBlam.Core.Compression.Deflate
{
    internal unsafe sealed class HuffmanTree : IDisposable
    {
        private const int AlphabetSize = 288;
        private const int DistanceValuesSize = 33;
        private readonly static byte[] CodeLengthMapping = new byte[] 
        { 
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 
        };

        private readonly static PinnedArrayPool<TreeNode> NodePool = PinnedArrayPool<TreeNode>.Create();
        private readonly static ArrayPool<byte> LengthPool = ArrayPool<byte>.Create();
        private readonly static ArrayPool<ushort> CodewordPool = ArrayPool<ushort>.Create();

        public readonly ushort LiteralLengthCodeCount;
        public readonly byte DistanceCodeCount;
        public readonly byte CodeLengthCodeCount;

        private readonly BitSource data;
        private ushort* literalLengthTreeAsUshort;
        private ushort* distanceTreeAsUshort;
        private TreeNode[] literalLengthTree;
        private TreeNode[] distanceTree;

        public HuffmanTree(BitSource data)
        {
            this.data = data;

            // Read 5 bits for LiteralLengthCodeCount
            this.LiteralLengthCodeCount = (ushort)(data.ReadBitsAsUshort(5) + 257);

            // Read 5 bits for DistanceCodeCount
            this.DistanceCodeCount = (byte)(data.ReadBitsAsUshort(5) + 1);

            // Read 4 bits for CodeLengthCodeCount
            this.CodeLengthCodeCount = (byte)(data.ReadBitsAsUshort(4) + 4);

            // Read code length intermediate tree data
            
            var codeLengthCodes = LengthPool.Rent(19);
            var literalLengths = LengthPool.Rent(AlphabetSize);
            var distanceLengths = LengthPool.Rent(DistanceValuesSize);

            try
            {
                for (var i = 0; i < this.CodeLengthCodeCount; i++)
                {
                    codeLengthCodes[CodeLengthMapping[i]] = (byte)data.ReadBitsAsUshort(3);
                }

                // Generate intermediate tree
                var intermediateNodes = GenerateTreeNodes(codeLengthCodes, 19, out _);
                using var intermediateTree = new CodeLengthHuffmanTree(intermediateNodes);

                intermediateTree.ProduceLengths(
                    data,
                    this.LiteralLengthCodeCount,
                    this.DistanceCodeCount,
                    literalLengths,
                    distanceLengths);

                this.literalLengthTree = GenerateTreeNodes(
                    literalLengths,
                    AlphabetSize,
                    out this.literalLengthTreeAsUshort,
                    DeflateConstants.LengthExtraBits,
                    257);

                this.distanceTree = GenerateTreeNodes(
                    distanceLengths,
                    DistanceValuesSize,
                    out this.distanceTreeAsUshort,
                    DeflateConstants.DistanceExtraBits);
            }
            finally
            {
                LengthPool.Return(codeLengthCodes, true);
                LengthPool.Return(literalLengths, true);
                LengthPool.Return(distanceLengths, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetValue()
        {
            var bitsource = this.data;
            var tree = this.literalLengthTreeAsUshort;

            uint branch = 0;
            while (true)
            {
                var bit = bitsource.CurrentBitValue();
                branch = *(tree + branch * 2 + bit);

                if (branch >= TreeNode.Threshold)
                {
                    // Take lower bits
                    return branch & 0x7FFF;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLength(uint value)
        {
            var index = (value & 0x1FF) - 257;

            var length = DeflateConstants.LengthBase[index];

            if (index <= 7)
            {
                return length;
            }

            var extraBitsVal = data.ReadBitsAsUshort((byte)((value & 0x7e00) >> 9));
            return length + extraBitsVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDistance()
        {
            var bitsource = this.data;
            var distanceTree = this.distanceTreeAsUshort;

            if (distanceTree == null)
            {
                // read 5 bits instead
                return data.ReadBitsAsUshort(5);
            }

            uint value = 0;
            uint branch = 0;
            while (true)
            {
                var bit = bitsource.CurrentBitValue();
                branch = *(distanceTree + branch * 2 + bit);

                if (branch >= TreeNode.Threshold)
                {
                    // Take lower bits
                    value = branch & 0x7FFF;
                    break;
                }
            }

            var distanceValue = value & 0x1FF;

            // do distance extra bits
            Debug.Assert(distanceValue < 30, "Value must be in 'distance' range");

            var distance = DeflateConstants.DistanceBase[distanceValue];

            if(distanceValue <= 3)
            {
                return distance;
            }

            var extraBitsVal = data.ReadBitsAsUshort((byte)((value & 0x7e00) >> 9));
            return distance + extraBitsVal;
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        private static TreeNode[] GenerateTreeNodes(byte[] alphabetCodeLengths, int length, out ushort* treePointer, byte[] extraBitsEmbed = null, int extraBitsOffset = 0)
        {
            ushort[] codewords = null;

            try
            {
                codewords = CodewordPool.Rent(length);
                FillAlphabetCodewords(alphabetCodeLengths, codewords, length);

                var tree = NodePool.Rent((length << 1) - 1, out var treeNodePointer);
                treePointer = (ushort*)treeNodePointer;

                ushort nodeCount = 1; // root

                for (int c = length - 1; c >= 0; c--)
                {
                    var len = alphabetCodeLengths[c];

                    if (len == 0)
                        continue;

                    long currentNode = 0;
                    long codeword = codewords[c];
                    ushort* indexPtr = (ushort*)0;

                    var shift = len - 1;
                    for (int i = 0; i < len; i++, shift--)
                    {
                        var isSet = (codeword >> shift) & 1;

                        indexPtr = ((ushort*)treePointer) + (2 * currentNode) + isSet;

                        if (*indexPtr == 0)
                        {
                            *indexPtr = nodeCount++;
                        }

                        currentNode = *indexPtr;
                    }

                    // Use current node to store value
                    nodeCount--;
                    *indexPtr = (ushort)(TreeNode.Threshold | c);

                    if(c >= extraBitsOffset && extraBitsEmbed != null)
                    {
                        *indexPtr |= (ushort)(extraBitsEmbed[c - extraBitsOffset] << 9);
                    }
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

            this.literalLengthTree = GenerateTreeNodes(fixedLengths, fixedLengths.Length, out this.literalLengthTreeAsUshort);
            
            this.distanceTree = null;
            this.distanceTreeAsUshort = null;
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
                    var tree = this.tree;

                    var node = tree[0];
                    byte codeLength = 0;

                    while(true)
                    {
                        var branch = (ushort)(node.Branches >> ((byte)data.CurrentBitValue() << 4));

                        if(branch >= TreeNode.Threshold)
                        {
                            // Take lower bits
                            codeLength = (byte)branch;
                            break;
                        }

                        node = tree[branch];
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
