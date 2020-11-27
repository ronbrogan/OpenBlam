﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBlam.Serialization.Metadata
{
    public class SerializationClassAttribute : Attribute
    {
        public const string SerializeMethod = "Serialize";
        public const string DeserializeMethod = "Deserialize";
        public const string DeserializeIntoMethod = "DeserializeInto";
    }
}
