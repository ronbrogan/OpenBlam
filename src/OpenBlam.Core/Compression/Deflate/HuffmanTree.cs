using OpenBlam.Core.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

            byte* codeLengthCodes = stackalloc byte[19];
            byte* literalLengths = stackalloc byte[AlphabetSize];
            byte* distanceLengths = stackalloc byte[DistanceValuesSize];

            for (var i = 0; i < this.CodeLengthCodeCount; i++)
            {
                codeLengthCodes[CodeLengthMapping[i]] = (byte)data.ReadBitsAsUshort(3);
            }

            // Generate intermediate tree
            uint* intermediateTree = stackalloc uint[19];
            GenerateTree(codeLengthCodes, (ushort*)intermediateTree, 19);
            CodeLengthHuffmanTree.ProduceLengths(
                data,
                (ushort*)intermediateTree,
                this.LiteralLengthCodeCount,
                this.DistanceCodeCount,
                literalLengths,
                distanceLengths);

            this.literalLengthTree = NodePool.Rent(GetTreeLength(AlphabetSize), out var lenTreePtr);
            this.literalLengthTreeAsUshort = (ushort*)lenTreePtr;
            GenerateTree(
                literalLengths,
                this.literalLengthTreeAsUshort,
                AlphabetSize,
                DeflateConstants.LengthExtraBits,
                257);

            this.distanceTree = NodePool.Rent(GetTreeLength(DistanceValuesSize), out var distTreePtr);
            this.distanceTreeAsUshort = (ushort*)distTreePtr;
            GenerateTree(
                distanceLengths,
                this.distanceTreeAsUshort,
                DistanceValuesSize,
                DeflateConstants.DistanceExtraBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetValue()
        {
            var bitsource = this.data;
            var tree = this.literalLengthTreeAsUshort;

            uint value = TraverseTree(tree, bitsource);

            return value & 0x7FFF;
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

            uint rawValue = TraverseTree(distanceTree, bitsource);

            var distanceValue = rawValue & 0x1FF;

            // do distance extra bits
            Debug.Assert(distanceValue < 30, "Value must be in 'distance' range");

            var distance = DeflateConstants.DistanceBase[distanceValue];

            if(distanceValue <= 3)
            {
                return distance;
            }

            var extraBitsVal = data.ReadBitsAsUshort((byte)((rawValue & 0x7e00) >> 9));
            return distance + extraBitsVal;
        }

        private ushort TraverseTree(ushort* tree, BitSource bitsource)
        {
            uint branch = 0;
            uint availableBits = 0;
            ulong bits = 0;

            while (branch < TreeNode.Threshold)
            {
                ulong consumed = 0;
                availableBits = (uint)bitsource.AvailableBits();
                bits = bitsource.PeekBits();

                while (availableBits > consumed && branch < TreeNode.Threshold)
                {
                    var bit = bits & 1;
                    bits >>= 1;
                    branch = tree[branch * 2 + bit];
                    consumed++;
                }

                bitsource.Consume(consumed);

                if (branch < TreeNode.Threshold)
                {
                    var bit = bitsource.CurrentBitValue();
                    branch = tree[branch * 2 + bit];
                }
            }

            return (ushort)branch;
        }

        private static int GetTreeLength(int length) => (length << 1) - 1;

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        private static void GenerateTree(byte* alphabetCodeLengths, ushort* tree, int length, byte[] extraBitsEmbed = null, int extraBitsOffset = 0)
        {
            ushort* codewords = stackalloc ushort[length];
            FillAlphabetCodewords(alphabetCodeLengths, codewords, length);

            ushort nodeCount = 1; // root

            for (int c = 0; c < length; c++)
            {
                var len = alphabetCodeLengths[c];

                if (len == 0)
                    continue;

                long currentNode = 0;
                long codeword = codewords[c];
                ushort* indexPtr = tree;

                var shift = len - 1;
                for (int i = 0; i < len; i++, shift--)
                {
                    var isSet = (codeword >> shift) & 1;
                    indexPtr = tree + (2 * currentNode) + isSet;

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
        }

        // Incoming array maps alphabet value (index) to the code length required to walk to the value
        // Output array maps alphabet value (index) to the codeword (bit sequence to arrive at the value)
        internal static void FillAlphabetCodewords(byte* alphabetCodeLengths, ushort* alphabetCodes, int length)
        {
            // 1. Count the number of codes for each code length.
            Span<ushort> bitLengths = stackalloc ushort[DeflateConstants.MaxBitLength];
            for (int i = 0; i < length; i++)
            {
                bitLengths[alphabetCodeLengths[i]]++;
            }

            // 2. Find the numerical value of the smallest code for each code length:
            var code = 0;
            bitLengths[0] = 0;

            Span<ushort> nextCode = stackalloc ushort[DeflateConstants.MaxBitLength + 1];
            for (var bits = 1; bits <= DeflateConstants.MaxBitLength; bits++)
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
            if(this.distanceTree != null)
            {
                NodePool.Return(this.distanceTree, true);
                NodePool.Return(this.literalLengthTree, true);
            }
        }

        public static HuffmanTree Fixed => fixedTree.Value;

        private static Lazy<HuffmanTree> fixedTree = new Lazy<HuffmanTree>(() => new HuffmanTree());

        private HuffmanTree()
        {
            byte* fixedLengths = stackalloc byte[288];

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

            this.literalLengthTree = NodePool.Rent(GetTreeLength(AlphabetSize), out var lenTreePtr);
            this.literalLengthTreeAsUshort = (ushort*)lenTreePtr;
            GenerateTree(
                fixedLengths,
                this.literalLengthTreeAsUshort,
                288,
                DeflateConstants.LengthExtraBits,
                257);

            this.distanceTree = null;
            this.distanceTreeAsUshort = null;
        }

        private static class CodeLengthHuffmanTree
        {
            public static unsafe void ProduceLengths(
                BitSource data,
                ushort* tree,
                ushort literalLengthCodeCount,
                byte distanceCodeCount,
                byte* literalLengths,
                byte* distanceLengths)
            {
                var valuesToProduce = literalLengthCodeCount + distanceCodeCount;

                var valuesProduced = 0;
                byte lastValueProduced = 0;
                while (valuesProduced < valuesToProduce)
                {
                    byte codeLength = 0;

                    uint branch = 0;
                    while (true)
                    {
                        var bit = data.CurrentBitValue();
                        branch = tree[branch * 2 + bit];

                        if (branch >= TreeNode.Threshold)
                        {
                            // Take lower bits
                            codeLength = (byte)branch;
                            break;
                        }
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

                    if(codeLength == 18)
                    {
                        ProduceRepeat(0, 11, bitsAsRepeat: 7);
                    }
                    else if(codeLength == 17)
                    {
                        ProduceRepeat(0, 3, bitsAsRepeat: 3);
                    }
                    else if(codeLength == 16)
                    {
                        ProduceRepeat(lastValueProduced, 3, bitsAsRepeat: 2);
                    }
                    else
                    {
                        ProduceValue(codeLength);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            public ushort Left;

            [FieldOffset(2)]
            public ushort Right;
        }
    }
}
