namespace OpenBlam.Serialization.Layout
{
    /// <summary>
    /// Used to represent that the current offset is the start of the instance data.
    /// This is usually used to 'embed' data into another without any sort of guiding structure.
    /// </summary>
    public class InPlaceObjectAttribute : SerializableMemberAttribute
    {
        public InPlaceObjectAttribute(int offset) : base(offset)
        {
        }
    }
}
