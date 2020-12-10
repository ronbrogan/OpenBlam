using System.IO;
using System.Numerics;

namespace OpenBlam.Serialization.Materialization
{
    public abstract class BinaryReadableStream : Stream
    {
        public abstract byte ReadByteAt(int offset);
        public abstract short ReadInt16At(int offset);
        public abstract ushort ReadUInt16At(int offset);
        public abstract int ReadInt32At(int offset);
        public abstract uint ReadUInt32At(int offset);
        public abstract float ReadFloatAt(int offset);
        public abstract double ReadDoubleAt(int offset);
        public abstract Vector2 ReadVec2At(int offset);
        public abstract Vector3 ReadVec3At(int offset);
        public abstract Vector4 ReadVec4At(int offset);
        public abstract Quaternion ReadQuaternionAt(int offset);
    }
}
