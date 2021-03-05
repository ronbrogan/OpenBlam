using OpenBlam.Core.Compression.Deflate;
using System;
using System.IO;

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
            fixed (byte* data = compressed)
            {
                using var output = new DeflateOutputBuffer();
                using var bits = new BufferBitSource(data, compressed.Length);
                
                Decompress(compressed, bits, output);

                return output.ToArray();
            }
        }

        public unsafe static void Decompress(Stream compressed, Stream decompressed)
        {
            using var output = new DeflateStreamOutput(decompressed);
            using var bits = new StreamBitSource(compressed);
            
            Decompress(compressed, bits, output);
        }

        public static void Decompress<T>(T data, BitSource bits, DeflateOutput<T> output)
        {
            while(true)
            {
                using var currentBlock = new DeflateBlock(bits);

                if (currentBlock.Type == BlockType.NoCompression)
                {
                    // skip to next byte
                    bits.SkipToNextByte();

                    // Read length and nlength?
                    var length = bits.ReadBitsAsUshort(16);
                    bits.ConsumeBytes(2);

                    // copy to output
                    output.Write(data, length);
                    bits.ConsumeBytes(length);
                }
                else
                {
                    while (true)
                    {
                        // decode literal/length value from input stream
                        var value = currentBlock.GetNextValue();

                        // end of block
                        if ((value & 0x1FF) == DeflateConstants.EndOfBlock) 
                        {
                            break;
                        }

                        if (value < DeflateConstants.EndOfBlock)
                        {
                            //copy value(literal byte) to output stream
                            output.WriteByte((byte)value);
                        }
                        else // value = 257..285
                        {
                            currentBlock.GetLengthAndDistance(value, out var length, out var distance);

                            output.WriteWindow(length, distance);
                        }
                    }
                }

                if(currentBlock.IsFinal)
                {
                    break;
                }
            }
        }
    }
}
