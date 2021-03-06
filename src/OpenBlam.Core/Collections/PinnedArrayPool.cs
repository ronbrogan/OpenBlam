using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenBlam.Core.Collections
{
    public class PinnedArrayPool<T> where T: unmanaged
    {
        private const int DefaultMaxArrayLength = 1024 * 1024;

        public static PinnedArrayPool<T> sharedPool = new PinnedArrayPool<T>(DefaultMaxArrayLength);
        public static PinnedArrayPool<T> Shared => sharedPool;

        private Bucket[] buckets;

        public static PinnedArrayPool<T> Create() => new PinnedArrayPool<T>();

        public PinnedArrayPool(int maxArrayLength = DefaultMaxArrayLength)
        {
            int maxBuckets = SelectBucketIndex(maxArrayLength);
            var buckets = new Bucket[maxBuckets + 1];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new Bucket(GetMaxSizeForBucket(i));
            }

            this.buckets = buckets;
        }

        public unsafe T[] Rent(int minimumSize)
        {
            var bucket = buckets[SelectBucketIndex(minimumSize)];

            return bucket.Rent();
        }

        public unsafe T[] Rent(int minimumSize, out T* pointer)
        {
            var bucket = buckets[SelectBucketIndex(minimumSize)];

            var arr = bucket.Rent();

            fixed(T* p = arr)
            {
                pointer = p;
            }

            return arr;
        }

        public void Return(T[] array, bool clear = true)
        {
            var bucket = buckets[SelectBucketIndex(array.Length)];
            bucket.Return(array, clear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SelectBucketIndex(int bufferSize)
        {
            Debug.Assert(bufferSize >= 0);

            // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
            // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
            // are combined, and the index is slid down by 3 to compensate.
            // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
            // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
            return BitOperations.Log2((uint)bufferSize - 1 | 15) - 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaxSizeForBucket(int binIndex)
        {
            int maxSize = 16 << binIndex;
            Debug.Assert(maxSize >= 0);
            return maxSize;
        }

        private class Bucket
        {
            private T[]?[] items;
            private int index = -1;

            private int bufferSize;

            public Bucket(int bufferSize)
            {
                this.items = new T[]?[100];
                this.bufferSize = bufferSize;
            }

            public T[] Rent()
            {
                while(true)
                {
                    var curIndex = index;
                    if (curIndex >= 0)
                    {
                        var orig = Interlocked.Exchange(ref index, curIndex-1);

                        // If we got what we expected, take the array
                        if (orig == curIndex)
                        {
                            var buf = this.items[orig];
                            if (buf == null)
                            {
                                return this.Allocate();
                            }
                            else
                            {
                                this.items[orig] = null;
                                return buf;
                            }
                        }
                    }
                    else
                    {
                        return this.Allocate();
                    }
                }
            }

            public void Return(T[] buffer, bool clear)
            {
                if(buffer.Length != this.bufferSize)
                {
                    throw new Exception("Array doesn't belong to this pool");
                }

                if (clear)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                }

                while(true)
                {
                    var curIndex = index;
                    if (curIndex < this.items.Length-1)
                    {
                        var orig = Interlocked.Exchange(ref index, curIndex+1);

                        // If we got what we expected, store the array
                        if (orig == curIndex)
                        {
                            this.items[curIndex+1] = buffer;
                            return;
                        }
                    }
                    else
                    {
                        // If we're full, throw away the array
                        return;
                    }
                }
            }
            private T[] Allocate()
            {
                return GC.AllocateArray<T>(this.bufferSize, pinned: true);
            }
        }
    }
}
