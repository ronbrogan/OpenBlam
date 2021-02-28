using BenchmarkDotNet.Running;
using System;
using System.Linq;
using System.Threading;

namespace OpenBlam.Core.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "profile")
            {
                var b = new DeflateBenchmarks();

                Thread.Sleep(2000);

                b.DeflateDecompressor_Decompress(b.GetData().Last());

                Thread.Sleep(2000);

                b.DeflateDecompressor_DecompressStream(b.GetData().Last());
            }
            else
            {
                BenchmarkRunner.Run<DeflateBenchmarks>();
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
