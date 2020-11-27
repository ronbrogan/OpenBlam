using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBlam.Serialization.Layout
{
    public class InternedStringAttribute : SerializableMemberAttribute
    {
        public InternedStringAttribute(int offset) : base(offset)
        {
        }
    }
}
