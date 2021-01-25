using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenBlam.Serialization.Materialization
{
    [System.Runtime.CompilerServices.CompilerGenerated]
    public static class SpanByteExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStringFromNullTerminated(this Span<byte> data)
        {
            var current = 0;
            while (true)
            {
                if (current == data.Length || data[current] == 0b0)
                {
                    break;
                }

                current++;
            }

            // TODO: remove .ToArray() once we're on netstandard2.1+
            return Encoding.UTF8.GetString(data.Slice(0, current).ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUtf16StringFromNullTerminated(this Span<byte> data)
        {
            var i = 0;
            for (; i < data.Length; i+=2)
            {
                if (data[i] == 0b0 && data[i+1] == 0b0)
                {
                    break;
                }
            }

            // TODO: remove .ToArray() once we're on netstandard2.1+
            return new string(MemoryMarshal.Cast<byte, char>(data.Slice(0, i)).ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadStringFrom(this Span<byte> data, int offset, int length)
        {
            var len = Math.Min(length, data.Length - offset);

            return data.Slice(offset, len).ToStringFromNullTerminated();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf16StringFrom(this Span<byte> data, int offset, int length)
        {
            var len = Math.Min(length * 2, data.Length - offset);

            return data.Slice(offset, len).ToUtf16StringFromNullTerminated();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByteAt(this Span<byte> data, int offset)
        {
            return data[offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16At(this Span<byte> data, int offset)
        {
            if (offset + 2 > data.Length)
            {
                return 0;
            }

            return MemoryMarshal.Read<short>(data.Slice(offset, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32At(this Span<byte> data, int offset)
        {
            if (offset + 4 > data.Length)
            {
                return 0;
            }

            return MemoryMarshal.Read<int>(data.Slice(offset, 4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16At(this Span<byte> data, int offset)
        {
            if (offset + 2 > data.Length)
            {
                return 0;
            }

            return MemoryMarshal.Read<ushort>(data.Slice(offset, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32At(this Span<byte> data, int offset)
        {
            if (offset + 4 > data.Length)
            {
                return 0;
            }

            return MemoryMarshal.Read<uint>(data.Slice(offset, 4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVec2At(this Span<byte> data, int offset)
        {
            if (offset + 8 > data.Length)
            {
                return Vector2.Zero;
            }

            return MemoryMarshal.Read<Vector2>(data.Slice(offset, 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVec3At(this Span<byte> data, int offset)
        {
            if (offset + 12 > data.Length)
            {
                return Vector3.Zero;
            }

            return MemoryMarshal.Read<Vector3>(data.Slice(offset, 12));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ReadVec4At(this Span<byte> data, int offset)
        {
            if (offset + 16 > data.Length)
            {
                return Vector4.Zero;
            }

            return MemoryMarshal.Read<Vector4>(data.Slice(offset, 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadQuaternionAt(this Span<byte> data, int offset)
        {
            if (offset + 16 > data.Length)
            {
                return Quaternion.Identity;
            }

            return MemoryMarshal.Read<Quaternion>(data.Slice(offset, 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 ReadMatrix4x4At(this Span<byte> data, int offset)
        {
            var matrixBytes = data.Slice(offset, 64);

            return new Matrix4x4(
                matrixBytes.ReadFloatAt(4 * 0),
                matrixBytes.ReadFloatAt(4 * 1),
                matrixBytes.ReadFloatAt(4 * 2),
                matrixBytes.ReadFloatAt(4 * 3),
                matrixBytes.ReadFloatAt(4 * 4),
                matrixBytes.ReadFloatAt(4 * 5),
                matrixBytes.ReadFloatAt(4 * 6),
                matrixBytes.ReadFloatAt(4 * 7),
                matrixBytes.ReadFloatAt(4 * 8),
                matrixBytes.ReadFloatAt(4 * 9),
                matrixBytes.ReadFloatAt(4 * 10),
                matrixBytes.ReadFloatAt(4 * 11),
                matrixBytes.ReadFloatAt(4 * 12),
                matrixBytes.ReadFloatAt(4 * 13),
                matrixBytes.ReadFloatAt(4 * 14),
                matrixBytes.ReadFloatAt(4 * 15)
            );
        }

        public static byte[] ReadArray(this Span<byte> data, int offset, int length)
        {
            return data.Slice(offset, length).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatAt(this Span<byte> data, int offset)
        {
            if (offset + 4 > data.Length)
            {
                return 0;
            }

            return MemoryMarshal.Read<float>(data.Slice(offset, 4));
        }
    }
}