namespace OpenBlam.Serialization.Layout
{
    /// <summary>
    /// Used to represent that the current offset is a string literal (UTF-8/ASCII)
    /// </summary>
    public sealed class StringValueAttribute : SerializableMemberAttribute
    {
        public StringValueAttribute(int offset, int maxLength) : base(offset)
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; }
    }
}
