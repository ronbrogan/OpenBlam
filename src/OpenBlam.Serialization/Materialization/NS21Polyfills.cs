using System;
using System.IO;

namespace OpenBlam.Serialization.Materialization
{
    internal static class StreamPolyfills
    {
        public static int Read(this Stream stream, Span<byte> data)
        {
            var arr = new byte[data.Length];

            var actual = stream.Read(arr, 0, data.Length);

            // We'll make two attempts to read the desired data
            if(actual != data.Length)
            {
                actual += stream.Read(arr, actual, data.Length - actual);
            }

            new Span<byte>(arr, 0, actual).CopyTo(data);

            return actual;
        }
    }
}
