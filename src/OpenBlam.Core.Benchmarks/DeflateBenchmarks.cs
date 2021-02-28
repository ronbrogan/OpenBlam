using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OpenBlam.Core.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace OpenBlam.Core.Benchmarks
{
    [MemoryDiagnoser]
    public class DeflateBenchmarks
    {
        private byte[] UnCompressed_1MiB;
        private byte[] Compressed_1MiB;
        private byte[] Compressed_10MiB;
        private byte[] UnCompressed_10MiB;
        private byte[] Compressed_50MiB;
        private byte[] UnCompressed_50MiB;

        
        public DeflateBenchmarks()
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

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] SIOC_DeflateStream_Decompress(CompressedSize input)
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
        public byte[] SharpZipLib_Decompress(CompressedSize input)
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
        public byte[] DeflateDecompressor_Decompress(CompressedSize input)
        {
            return DeflateDecompressor.Decompress(input.Data);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetData))]
        public byte[] DeflateDecompressor_DecompressStream(CompressedSize input)
        {
            using var decompressed = new MemoryStream();
            using var data = new MemoryStream(input.Data);
            DeflateDecompressor.Decompress(data, decompressed);

            return decompressed.ToArray();
        }

        public IEnumerable<CompressedSize> GetData()
        {
            yield return new CompressedSize(Compressed_1MiB, 1);
            yield return new CompressedSize(Compressed_10MiB, 10);
            yield return new CompressedSize(Compressed_50MiB, 50);
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

        private void Fill(byte[] data)
        {
            var rand = new Random(42);
            var current = (byte)rand.Next(0, 255);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = current;

                if (current == 0 || i % current == 0)
                {
                    current = (byte)rand.Next(0, 255);
                }
            }
        }

        public class CompressedSize
        {
            public readonly byte[] Data;
            private readonly int size;

            public CompressedSize(byte[] data, int size)
            {
                this.Data = data;
                this.size = size;
            }

            public override string ToString() => $"{this.size} MiB";
        }
    }
}
