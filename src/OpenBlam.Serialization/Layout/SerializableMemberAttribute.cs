using System;

namespace OpenBlam.Serialization.Layout
{
    public abstract class SerializableMemberAttribute : Attribute
    {
        public SerializableMemberAttribute(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}
