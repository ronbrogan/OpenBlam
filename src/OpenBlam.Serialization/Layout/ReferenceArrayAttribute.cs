namespace OpenBlam.Serialization.Layout
{
    /// <summary>
    /// Used to represent that the current offset is a count and pointer to the instance data as an array of contiguous blocks
    /// </summary>
    public class ReferenceArrayAttribute : SerializableMemberAttribute
    {
        public ReferenceArrayAttribute(int offset) : base(offset)
        {
        }
    }
}
