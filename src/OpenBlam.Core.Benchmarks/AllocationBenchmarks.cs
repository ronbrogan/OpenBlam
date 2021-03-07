using BenchmarkDotNet.Attributes;
using OpenBlam.Core.Collections;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace OpenBlam.Core.Benchmarks
{
    [MemoryDiagnoser]
    public class AllocationBenchmarks
    {
        private static ArrayPool<byte> sharedArrayPool = ArrayPool<byte>.Shared;
        private static PinnedArrayPool<byte> pinnedArrayPool = PinnedArrayPool<byte>.Shared;

        [Params(32, 128, 512, 1024)]
        public int Bytes { get; set; }

        [Benchmark]
        public int New()
        {
            var a = new byte[this.Bytes];
            return a.Length;
        }

        [Benchmark]
        public int Stackalloc()
        {
            Span<byte> a = stackalloc byte[this.Bytes];
            return a.Length;
        }

        [Benchmark]
        public int ArrayPool()
        {
            var a = sharedArrayPool.Rent(this.Bytes);
            var l = a.Length;
            sharedArrayPool.Return(a, clearArray: true);
            return l;
        }

        [Benchmark]
        public int ArrayPool_Pin()
        {
            var a = sharedArrayPool.Rent(this.Bytes);
            var l = a.Length;
            var h = GCHandle.Alloc(a, GCHandleType.Pinned);
            var ptr = h.AddrOfPinnedObject();
            l = unchecked(l + (int)(((long)ptr) & 1));
            h.Free();
            sharedArrayPool.Return(a, clearArray: true);
            return l;
        }

        [Benchmark]
        public unsafe int ArrayPool_Fixed()
        {
            var a = sharedArrayPool.Rent(this.Bytes);
            var l = a.Length;

            fixed (byte* b = a)
                l = unchecked(l + (int)(((long)b) & 1));

            sharedArrayPool.Return(a, clearArray: true);
            return l;
        }

        [Benchmark]
        public int PinnedArrayPool()
        {
            var a = pinnedArrayPool.Rent(this.Bytes);
            var l = a.Length;
            pinnedArrayPool.Return(a, clear: true);
            return l;
        }
    }
}
