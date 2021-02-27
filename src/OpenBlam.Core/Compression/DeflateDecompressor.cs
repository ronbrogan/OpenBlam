using OpenBlam.Core.Compression.Deflate;
using OpenBlam.Core.Exceptions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Compression
{
    internal static class DeflateConstants
    {
        public const int EndOfBlock = 256;
        public const int MaximumLookback = 1 << 15;

        public static ushort[] LengthBase = new ushort[]
        {
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51,
            59, 67, 83, 99, 115, 131, 163, 195, 227, 258
        };

        public static byte[] LengthExtraBits = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
        };

        public static ushort[] DistanceBase = new ushort[]
        {
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025,
            1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577
        };

        public static byte[] DistanceExtraBits = new byte[]
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
        };
    }

    public partial class DeflateDecompressor
    {
        public unsafe static byte[] Decompress(byte[] compressed)
        {
            var output = new DeflateOutputBuffer();

            DeflateBlock currentBlock;
            byte* buf = stackalloc byte[258];

            var bits = new BitSource(compressed);

            fixed(byte* data = compressed)
            {
                do
                {
                    currentBlock = new DeflateBlock(bits);

                    if (currentBlock.Type == BlockType.NoCompression)
                    {
                        // skip to next byte
                        bits.SkipToNextByte();

                        // Read length and nlength?
                        var length = bits.ReadBitsAsUshort(16);
                        var nlength = bits.ReadBitsAsUshort(16); // one's compliment of length

                        // copy to output
                        output.Write(data + (int)(bits.CurrentBit >> 3), length);
                        bits.ConsumeBytes(length);
                    }
                    else
                    {
                        while (true)
                        {
                            // decode literal/length value from input stream
                            var value = currentBlock.GetNextValue();

                            if (value == DeflateConstants.EndOfBlock) // end of block
                            {
                                break;
                            }

                            if(output.AbsolutePosition > 8650000 && output.AbsolutePosition < 8651000)
                            {
                                Debugger.Break();
                            }    

                            if (value < DeflateConstants.EndOfBlock)
                            {
                                //copy value(literal byte) to output stream
                                output.WriteByte((byte)value);
                            }
                            else // value = 257..285
                            {
                                var (length, distance) = currentBlock.GetLengthAndDistance(value);

                                output.WriteWindow(length, distance);
                            }
                        }
                    }
                }
                while (!currentBlock.IsFinal);
            }

            

            bits.Dispose();

            return output.ToArray();
        }
    }
}
