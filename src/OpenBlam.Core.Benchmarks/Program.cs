using BenchmarkDotNet.Running;
using System;

namespace OpenBlam.Core.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<DeflateBenchmarks>();

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
