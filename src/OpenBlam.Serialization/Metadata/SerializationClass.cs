using System;

namespace OpenBlam.Serialization.Metadata
{
    public class SerializationClassAttribute : Attribute
    {
        public const string SerializeMethod = "Serialize";
        public const string DeserializeMethod = "Deserialize";
        public const string DeserializeIntoMethod = "DeserializeInto";
    }
}
