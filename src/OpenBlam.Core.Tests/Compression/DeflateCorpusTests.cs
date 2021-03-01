using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenBlam.Core.Compression;
using System.IO;

namespace OpenBlam.Core.Tests.Compression
{
    [TestClass]
    public class DeflateCorpusTests
    {
        [TestMethod]
        public void Decompress_Alice29()
        {
            var corpusItem = new CorpusInfo("alice29.txt");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Asyoulik()
        {
            var corpusItem = new CorpusInfo("asyoulik.txt");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Cp()
        {
            var corpusItem = new CorpusInfo("cp.html");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Fields()
        {
            var corpusItem = new CorpusInfo("fields.c");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Grammar()
        {
            var corpusItem = new CorpusInfo("grammar.lsp");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Kennedy()
        {
            var corpusItem = new CorpusInfo("kennedy.xls");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Lcet10()
        {
            var corpusItem = new CorpusInfo("lcet10.txt");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Plrabn12()
        {
            var corpusItem = new CorpusInfo("plrabn12.txt");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Ptt5()
        {
            var corpusItem = new CorpusInfo("ptt5");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Sum()
        {
            var corpusItem = new CorpusInfo("sum");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void Decompress_Xargs()
        {
            var corpusItem = new CorpusInfo("xargs.1");

            var decompressed = DeflateDecompressor.Decompress(corpusItem.Compressed);

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Alice29()
        {
            var corpusItem = new CorpusInfo("alice29.txt");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Asyoulik()
        {
            var corpusItem = new CorpusInfo("asyoulik.txt");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Cp()
        {
            var corpusItem = new CorpusInfo("cp.html");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Fields()
        {
            var corpusItem = new CorpusInfo("fields.c");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Grammar()
        {
            var corpusItem = new CorpusInfo("grammar.lsp");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Kennedy()
        {
            var corpusItem = new CorpusInfo("kennedy.xls");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Lcet10()
        {
            var corpusItem = new CorpusInfo("lcet10.txt");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Plrabn12()
        {
            var corpusItem = new CorpusInfo("plrabn12.txt");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Ptt5()
        {
            var corpusItem = new CorpusInfo("ptt5");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Sum()
        {
            var corpusItem = new CorpusInfo("sum");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        [TestMethod]
        public void StreamDecompress_Xargs()
        {
            var corpusItem = new CorpusInfo("xargs.1");
            using var outStream = new MemoryStream();
            using var inStream = new MemoryStream(corpusItem.Compressed);
            DeflateDecompressor.Decompress(inStream, outStream);

            var decompressed = outStream.ToArray();

            CollectionAssert.AreEqual(corpusItem.Raw, decompressed);
        }

        public class CorpusInfo
        {
            public readonly byte[] Raw;
            public readonly byte[] Compressed;
            private readonly string filename;

            public CorpusInfo(string filename)
            {
                this.filename = filename;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Corpus", "Deflated", filename);
                this.Compressed = File.ReadAllBytes(path);

                var rawpath = Path.Combine(Directory.GetCurrentDirectory(), "Corpus", "Raw", filename);
                this.Raw = File.ReadAllBytes(rawpath);
            }

            public override string ToString() => filename;
        }
    }
}
