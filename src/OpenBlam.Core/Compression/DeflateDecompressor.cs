using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenBlam.Core.Compression
{
    internal static class DeflateConstants
    {
        public const int EndOfBlock = 256;
    }

    public class DeflateDecompressor
    {
        public static byte[] Decompress(byte[] compressed)
        {
            if (BitConverter.IsLittleEndian == false) throw new NotSupportedException("Big Endian bad?");

            using var output = new MemoryStream();

            ulong currentBit = 0;
            Block currentBlock;

            do
            {
                currentBlock = new Block(compressed, currentBit);

                if(currentBlock.Type == BlockType.NoCompression)
                {
                    // skip to next byte
                    currentBit >>= 3;
                    currentBit++;
                    
                    // Read length and nlength?
                    var length = BitConverter.ToUInt16(compressed, (int)currentBit);
                    currentBit += 2;

                    var nlength = BitConverter.ToUInt16(compressed, (int)currentBit); // one's compliment of length
                    currentBit += 2;

                    // copy to output
                    output.Write(compressed, (int)currentBit, length);
                    currentBit += length;

                    // Restore to bit-mode
                    currentBit <<= 3;
                }
                else
                {
                    while(true)
                    {
                        // decode literal/length value from input stream
                        var value = currentBlock.GetNextValue();

                        if(value == DeflateConstants.EndOfBlock) // end of block
                        {
                            break;
                        }

                        if(value < 256)
                        {
                            //copy value(literal byte) to output stream
                        }
                        else // value = 257..285
                        {
                            // decode distance from input stream

                            // move backwards distance bytes in the output stream
                            // copy length bytes from this position to the output stream
                        }
                    }
                }
            }
            while (!currentBlock.IsFinal);

            return output.ToArray();
        }

        public struct Block
        {
            public HuffmanTree HuffmanTree;
            public byte[] Compressed;
            public ulong CurrentBit;
            public bool IsFinal;
            public BlockType Type;

            public Block(byte[] compressed, ulong start)
            {
                Compressed = compressed;
                var partialByte = compressed[start >> 3];
                var bitOffset = (byte)(start & 7);
                IsFinal = ((partialByte >> (bitOffset)) & 1) == 1;
                Type = (BlockType)((partialByte >> (1 - bitOffset)) & 3);

                CurrentBit = start + 3;

                if(Type == BlockType.DynamicHuffmanCodes)
                {
                    HuffmanTree = new HuffmanTree(compressed, ref CurrentBit);
                }
                else if(Type == BlockType.FixedHuffmanCodes)
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
                return this.HuffmanTree.GetValue(ref CurrentBit);
            }
        }

        public enum BlockType : byte
        {
            NoCompression = 0,
            FixedHuffmanCodes = 1,
            DynamicHuffmanCodes = 2,
            Reserved = 3,
        }

        public class HuffmanTree
        {
            private const int AlphabetSize = 288;
            private const int DistanceValuesSize = 33;

            public readonly ushort LiteralLengthCodeCount;
            public readonly byte DistanceCodeCount;
            public readonly byte CodeLengthCodeCount;

            private TreeNode[] tree;

            public HuffmanTree(byte[] compressed, ref ulong currentBit)
            {
                // Read 5 bits for LiteralLengthCodeCount
                for (var i = 0; i < 5; i++)
                {
                    LiteralLengthCodeCount >>= 1;

                    if (((compressed[currentBit >> 3] >> (byte)(currentBit & 7)) & 1) == 1)
                    {
                        LiteralLengthCodeCount |= 16;
                    }

                    currentBit++;
                }

                LiteralLengthCodeCount += 257;

                // Read 5 bits for DistanceCodeCount
                for (var i = 0; i < 5; i++)
                {
                    DistanceCodeCount >>= 1;

                    if (((compressed[currentBit >> 3] >> (byte)(currentBit & 7)) & 1) == 1)
                    {
                        DistanceCodeCount |= 16;
                    }

                    currentBit++;
                }

                DistanceCodeCount += 1;

                // Read 4 bits for CodeLengthCodeCount
                for (var i = 0; i < 4; i++)
                {
                    CodeLengthCodeCount >>= 1;

                    if (((compressed[currentBit >> 3] >> (byte)(currentBit & 7)) & 1) == 1)
                    {
                        CodeLengthCodeCount |= 8;
                    }

                    currentBit++;
                }

                CodeLengthCodeCount += 4;

                // Read code length intermediate tree data
                Span<byte> mapping = stackalloc byte[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
                var codeLengthCodes = new byte[19];
                for(var i = 0; i < CodeLengthCodeCount; i++)
                {
                    byte codeLength = 0;

                    for (var j = 0; j < 3; j++)
                    {
                        codeLength >>= 1;

                        if (((compressed[currentBit >> 3] >> (byte)(currentBit & 7)) & 1) == 1)
                        {
                            codeLength |= 4;
                        }

                        currentBit++;
                    }

                    codeLengthCodes[mapping[i]] = codeLength;
                }

                // Generate intermediate tree
                var intermediateNodes = GenerateTreeNodes(codeLengthCodes);
                var intermediateTree = new CodeLengthHuffmanTree(intermediateNodes);

                var (literalLengths, distanceLengths) = intermediateTree.ProduceLengths(
                    compressed, 
                    ref currentBit,
                    LiteralLengthCodeCount, 
                    DistanceCodeCount);
            }

            public ushort GetValue(ref ulong start)
            {
                return 0;
            }

            // Incoming array maps alphabet value (index) to the code length required to walk to the value
            private static TreeNode[] GenerateTreeNodes(byte[] alphabetCodeLengths)
            {
                var codewords = GetAlphabetCodewords(alphabetCodeLengths);
                var maxLength = alphabetCodeLengths.Max();

                Span<TreeNode> tree = stackalloc TreeNode[1 << (maxLength - 1)];

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
                        var isSet = ((codeword >> (len-i-1)) & 1) == 1;

                        // Resize tree if necessary
                        if(nodeCount + 1 >= tree.Length)
                        {
                            Span<TreeNode> newTree = stackalloc TreeNode[tree.Length + 32];

                            tree.CopyTo(newTree);

                            tree = newTree;
                        }

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

                return tree.ToArray();
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

                // TODO: build from lengths
            }

            private class CodeLengthHuffmanTree
            {
                private readonly TreeNode[] tree;

                public CodeLengthHuffmanTree(TreeNode[] tree)
                {
                    this.tree = tree;
                }

                internal (byte[] literalLengths, byte[] distanceLengths) ProduceLengths(
                    byte[] compressed, 
                    ref ulong currentBit,
                    ushort literalLengthCodeCount, 
                    byte distanceCodeCount)
                {
                    var valuesToProduce = literalLengthCodeCount + distanceCodeCount;
                    var literalLengths = new byte[AlphabetSize];
                    var distanceLengths = new byte[DistanceValuesSize];

                    var bitStream = "";

                    var valuesProduced = 0;
                    byte lastValueProduced = 0;
                    while(valuesProduced < valuesToProduce)
                    {
                        var node = tree[0];
                        byte codeLength = 0;

                        while(true)
                        {
                            if(node.Branches == 0)
                            {
                                codeLength = (byte)node.Value;
                                break;
                            }

                            var nextBit = IsSet(currentBit);
                            bitStream += nextBit ? "1" : "0";
                            node = nextBit ? tree[node.Right] : tree[node.Left];
                            currentBit++;
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
                                ProduceRepeat(lastValueProduced, 3, bitsAsRepeat: 2, ref currentBit);
                                break;
                            case 17:
                                ProduceRepeat(0, 3, bitsAsRepeat: 3, ref currentBit);
                                break;
                            case 18:
                                ProduceRepeat(0, 11, bitsAsRepeat: 7, ref currentBit);
                                break;
                        }
                    }

                    return (literalLengths, distanceLengths);

                    void ProduceValue(byte value)
                    {
                        if(valuesProduced <= literalLengthCodeCount)
                        {
                            literalLengths[valuesProduced] = value;
                        }
                        else
                        {
                            distanceLengths[valuesProduced- literalLengthCodeCount] = value;
                        }

                        lastValueProduced = value;
                        valuesProduced++;
                    }

                    void ProduceRepeat(byte value, int baseAmount, int bitsAsRepeat, ref ulong currentBit)
                    {
                        var repeatCount = 0;
                        var setBit = 1 << bitsAsRepeat - 1;
                        for(var i = 0; i < bitsAsRepeat; i++)
                        {
                            repeatCount >>= 1;

                            if (IsSet(currentBit))
                            {
                                repeatCount |= setBit;
                            }

                            currentBit++;
                        }

                        for (int i = 0; i < baseAmount + repeatCount; i++)
                        {
                            ProduceValue(value);
                        }
                    }

                    bool IsSet(ulong currentBit) => ((compressed[currentBit >> 3] >> (byte)(currentBit & 7)) & 1) == 1;
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
}
