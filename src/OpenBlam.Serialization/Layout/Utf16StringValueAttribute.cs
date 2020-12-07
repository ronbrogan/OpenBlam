namespace OpenBlam.Serialization.Layout
{

    /// <summary>
    /// Used to represent that the current offset is a string literal (UTF-16)
    /// </summary>
    public sealed class Utf16StringValueAttribute : SerializableMemberAttribute
    {
        /// <param name="maxLength">Max length in characters, each character is 2 bytes</param>
        public Utf16StringValueAttribute(int offset, int maxLength) : base(offset)
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; }
    }
}
