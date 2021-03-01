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
                var info = b.GetData().First(d => d.ToString() == "kennedy.xls");

                Thread.Sleep(2000);

                for(var i = 0; i < 100; i++)
                    b.Burnside_ByteArray(info);

                Thread.Sleep(2000);

                for (var i = 0; i < 100; i++)
                    b.Burnside_Stream(info);
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
