namespace OpenBlam.Serialization.Layout
{

    /// <summary>
    /// Used to represent that the current offset is a pointer to the instance data
    /// </summary>
    public class ReferenceValueAttribute : SerializableMemberAttribute
    {
        public ReferenceValueAttribute(int offset) : base(offset)
        {
        }
    }
}
