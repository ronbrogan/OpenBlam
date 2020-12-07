namespace OpenBlam.Serialization.Layout
{
    /// <summary>
    /// Used to represent that the current offset is an identifier of an interned string
    /// </summary>
    public class InternedStringAttribute : SerializableMemberAttribute
    {
        public InternedStringAttribute(int offset) : base(offset)
        {
        }
    }
}
