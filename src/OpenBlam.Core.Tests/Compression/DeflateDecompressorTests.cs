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
        [TestMethod]
        public void Test()
        {
            var data = "thisissomestringdatathatwillbeusedtotestthedecompressionofthealgorithm";
            var compressed = Compress(data);

            var decompressed = DeflateDecompressor.Decompress(compressed);

            Assert.IsTrue(MemoryExtensions.SequenceEqual<byte>(Encoding.UTF8.GetBytes(data), decompressed));
        }

        private byte[] Compress(string data)
        {
            return Compress(Encoding.UTF8.GetBytes(data));
        }

        private byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var input = new MemoryStream(data))
            using (var deflate = new DeflateStream(output, CompressionMode.Compress, true))
            {
                input.CopyTo(deflate);
            }

            return output.ToArray();
        }
    }
}
