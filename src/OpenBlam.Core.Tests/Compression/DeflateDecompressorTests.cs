using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenBlam.Core.Compression;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OpenBlam.Core.Tests.Compression
{
    [TestClass]
    public class DeflateDecompressorTests
    {
        private byte[] UnCompressed_1MiB;
        private byte[] Compressed_1MiB;
        private byte[] Compressed_10MiB;
        private byte[] UnCompressed_10MiB;
        private byte[] Compressed_50MiB;
        private byte[] UnCompressed_50MiB;

        public DeflateDecompressorTests()
        {
            var rand = new Random(42);

            UnCompressed_1MiB = new byte[1024 * 1024 * 1];
            Fill(UnCompressed_1MiB);
            Compressed_1MiB = Compress(UnCompressed_1MiB);

            UnCompressed_10MiB = new byte[1024 * 1024 * 10];
            Fill(UnCompressed_10MiB);
            Compressed_10MiB = Compress(UnCompressed_10MiB);

            UnCompressed_50MiB = new byte[1024 * 1024 * 50];
            Fill(UnCompressed_50MiB);
            Compressed_50MiB = Compress(UnCompressed_50MiB);
        }

        [TestMethod]
        public void DecompressString()
        {
            var data = "thisissomestringdatathatwillbeusedtotestthedecompressionofthealgorithm";
            var compressed = Compress(data);

            var decompressed = DeflateDecompressor.Decompress(compressed);

            Assert.AreEqual(data, Encoding.UTF8.GetString(decompressed));
        }

        [TestMethod]
        public void Decompress_Random1MiB()
        {
            var decompressed = DeflateDecompressor.Decompress(Compressed_1MiB);

            CollectionAssert.AreEqual(UnCompressed_1MiB, decompressed);
        }

        [TestMethod]
        public void Decompress_Random10MiB()
        {
            var decompressed = DeflateDecompressor.Decompress(Compressed_10MiB);

            CollectionAssert.AreEqual(UnCompressed_10MiB, decompressed);
        }

        [TestMethod]
        public void Decompress_Random50MiB()
        {
            var decompressed = DeflateDecompressor.Decompress(Compressed_50MiB);

            CollectionAssert.AreEqual(UnCompressed_50MiB, decompressed);
        }

        private byte[] Compress(string data)
        {
            return Compress(Encoding.UTF8.GetBytes(data));
        }

        private byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var input = new MemoryStream(data))
            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            {
                input.CopyTo(deflate);
            }

            return output.ToArray();
        }

        private void Fill(byte[] data)
        {
            var rand = new Random(42);
            var current = (byte)rand.Next(0, 255);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = current;

                if(current == 0 || i % current == 0)
                {
                    current = (byte)rand.Next(0, 255);
                }
            }
        }
    }
}
