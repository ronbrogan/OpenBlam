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
            if(args.Length  == 0)
            { 
                var b = new DeflateCorpus();

                Thread.Sleep(2000);

                b.Burnside_ByteArray(b.GetData().Last());

                Thread.Sleep(2000);

                b.Burnside_Stream(b.GetData().Last());
            }
            else
            {
                var benchmarkType = args[0] switch
                {
                    "deflate-synthetic" => typeof(DeflateSynthetic),
                    "deflate-corpus" => typeof(DeflateCorpus)
                };

                BenchmarkSwitcher.FromTypes(new[] { benchmarkType }).Run(((Span<string>)args).Slice(1).ToArray());
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
