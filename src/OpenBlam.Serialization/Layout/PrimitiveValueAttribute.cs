namespace OpenBlam.Serialization.Layout
{
    /// <summary>
    /// Used to represent that the current offset is a literal value
    /// </summary>
    public class PrimitiveValueAttribute : SerializableMemberAttribute
    {
        public PrimitiveValueAttribute(int offset) : base(offset)
        {
        }
    }
}
