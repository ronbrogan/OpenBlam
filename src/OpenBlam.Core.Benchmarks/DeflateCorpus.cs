using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OpenBlam.Core.Compression;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace OpenBlam.Core.Benchmarks
{
    [MemoryDiagnoser]
    public class DeflateCorpus
    {      
        public DeflateCorpus()
        {
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] SIOC_DeflateStream(CorpusInfo input)
        {
            using var decompressed = new MemoryStream();

            using (var data = new MemoryStream(input.Data))
            using (var deflate = new DeflateStream(data, CompressionMode.Decompress))
            {
                deflate.CopyTo(decompressed);
            }

            return decompressed.ToArray();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] SharpZipLib(CorpusInfo input)
        {
            var inf = new Inflater(true);
            using var decompressed = new MemoryStream();

            using (var data = new MemoryStream(input.Data))
            using (var decompressor = new InflaterInputStream(data, inf))
            {
                decompressor.CopyTo(decompressed);
            }

            return decompressed.ToArray();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] Burnside_ByteArray(CorpusInfo input)
        {
            return DeflateDecompressor.Decompress(input.Data);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] Burnside_Stream(CorpusInfo input)
        {
            using var decompressed = new MemoryStream();
            using var data = new MemoryStream(input.Data);
            DeflateDecompressor.Decompress(data, decompressed);

            return decompressed.ToArray();
        }

        public IEnumerable<CorpusInfo> GetData()
        {
            yield return new CorpusInfo("alice29.txt");
            yield return new CorpusInfo("asyoulik.txt");
            yield return new CorpusInfo("cp.html");
            yield return new CorpusInfo("fields.c");
            yield return new CorpusInfo("grammar.lsp");
            yield return new CorpusInfo("kennedy.xls");
            yield return new CorpusInfo("lcet10.txt");
            yield return new CorpusInfo("plrabn12.txt");
            yield return new CorpusInfo("ptt5");
            yield return new CorpusInfo("sum");
            yield return new CorpusInfo("xargs.1");
        }

        public class CorpusInfo
        {
            public readonly byte[] Data;
            private readonly string filename;

            public CorpusInfo(string filename)
            {
                this.filename = filename;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Corpus", "Deflated", filename);
                this.Data = File.ReadAllBytes(path);
            }

            public override string ToString() => filename;
        }
    }
}
