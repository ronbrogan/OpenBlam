using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenBlam.Serialization.Materialization
{
    public static class StreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadStringFrom(this Stream data, int offset, int length)
        {
            var len = Math.Min(length, data.Length - offset);

            Span<byte> stringBytes = stackalloc byte[0];

            if(length < 512)
            {
                stringBytes = stackalloc byte[length];
            }
            else
            {
                stringBytes = new byte[length];
            }

            if (data.Position != offset)
                data.Position = offset;

            var actualRead = data.Read(stringBytes);

            var i = 0;
            for(; i < actualRead; i++)
            {
                if(stringBytes[i] == 0b0)
                {
                    break;
                }
            }

            // TODO: remove .ToArray() once we're on netstandard2.1+
            return Encoding.UTF8.GetString(stringBytes.Slice(0, i).ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf16StringFrom(this Stream data, int offset, int length)
        {
            var byteLength = length * 2;

            Span<byte> stringBytes = stackalloc byte[0];

            if (byteLength < 512)
            {
                stringBytes = stackalloc byte[byteLength];
            }
            else
            {
                stringBytes = new byte[byteLength];
            }

            if (data.Position != offset)
                data.Position = offset;

            var possibleCharCount = data.Read(stringBytes);

            var i = 0;
            for (; i < possibleCharCount; i += 2)
            {
                if (stringBytes[i] == 0b0 && stringBytes[i + 1] == 0b0)
                {
                    break;
                }
            }

            // TODO: remove .ToArray() once we're on netstandard2.1+
            return new string(MemoryMarshal.Cast<byte, char>(stringBytes.Slice(0, i)).ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByteAt(this Stream data, int offset)
        {
            if (data.Position != offset)
                data.Position = offset;

            return (byte)data.ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[2];

            if (data.Position != offset)
                data.Position = offset;

            if (data.Read(bytes) != 2)
            {
                return 0;
            }

            return MemoryMarshal.Read<short>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[4];

            if (data.Position != offset)
                data.Position = offset;

            if (data.Read(bytes) != 4)
            {
                return 0;
            }

            return MemoryMarshal.Read<int>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[2];

            if (data.Position != offset)
                data.Position = offset;

            if (data.Read(bytes) != 2)
            {
                return 0;
            }

            return MemoryMarshal.Read<ushort>(bytes);
        }

        private static byte[] buffer = new byte[16];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[4];

            if (data.Position != offset)
                data.Position = offset;

            if (data.Read(bytes) != 4)
            {
                return 0;
            }

            return MemoryMarshal.Read<uint>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVec2At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[8];

            if (data.Position != offset)
                data.Position = offset;

            if (data.Read(bytes) != 8)
            {
                return Vector2.Zero;
            }

            return MemoryMarshal.Read<Vector2>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVec3At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[12];

            if (data.Position != offset)
                data.Position = offset;

            data.Read(bytes);

            return MemoryMarshal.Read<Vector3>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ReadVec4At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[16];

            if (data.Position != offset)
                data.Position = offset;

            data.Read(bytes);

            return MemoryMarshal.Read<Vector4>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadQuaternionAt(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[16];

            if (data.Position != offset)
                data.Position = offset;

            data.Read(bytes);

            return MemoryMarshal.Read<Quaternion>(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 ReadMatrix4x4At(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[64];

            if (data.Position != offset)
                data.Position = offset;

            data.Read(bytes);

            return new Matrix4x4(
                bytes.ReadFloatAt(4 * 0), //1
                bytes.ReadFloatAt(4 * 4), //1
                bytes.ReadFloatAt(4 * 8), //1
                bytes.ReadFloatAt(4 * 12),//1
                bytes.ReadFloatAt(4 * 1), //2
                bytes.ReadFloatAt(4 * 5), //2
                bytes.ReadFloatAt(4 * 9), //2
                bytes.ReadFloatAt(4 * 13),//2
                bytes.ReadFloatAt(4 * 2), //3
                bytes.ReadFloatAt(4 * 6), //3
                bytes.ReadFloatAt(4 * 10),//3
                bytes.ReadFloatAt(4 * 14),//3
                bytes.ReadFloatAt(4 * 3), //4
                bytes.ReadFloatAt(4 * 7), //4
                bytes.ReadFloatAt(4 * 11),//4
                bytes.ReadFloatAt(4 * 15) //4
            );
        }

        public static byte[] ReadArray(this Stream data, int offset, int length)
        {
            var bytes = new byte[length];

            if (data.Position != offset)
                data.Position = offset;

            var totalRead = 0;
            var lastRead = -1;
            while (totalRead != length && lastRead != 0) {
                lastRead = data.Read(bytes, totalRead, length-totalRead);
                totalRead += lastRead;
            }

            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatAt(this Stream data, int offset)
        {
            Span<byte> bytes = stackalloc byte[4];

            if (data.Position != offset)
                data.Position = offset;

            data.Read(bytes);

            return MemoryMarshal.Read<float>(bytes);
        }
    }
}