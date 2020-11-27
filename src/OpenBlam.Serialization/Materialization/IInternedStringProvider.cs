namespace OpenBlam.Serialization.Materialization
{
    public interface IInternedStringProvider
    {
        int IndexOffset { get; }
        int DataOffset { get; }
    }
}
