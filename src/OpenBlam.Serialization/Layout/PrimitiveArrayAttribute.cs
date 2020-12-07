namespace OpenBlam.Serialization.Layout
{

    /// <summary>
    /// Used to represent that the current offset is an array of literal values
    /// </summary>
    public sealed class PrimitiveArrayAttribute : SerializableMemberAttribute
    {
        public PrimitiveArrayAttribute(int offset, int count) : base(offset)
        {
            Count = count;
        }

        public int Count { get; }
    }
}
