using OpenBlam.Serialization.Layout;

namespace OpenBlam.Halo3.Map
{
    [FixedLength(12288)]
    public class H3MapHeader
    {
        [PrimitiveValue(4)]
        public byte Version { get; set; }

        [PrimitiveValue(24)]
        // 68-length items with floats, two of which are typically pi
        public int SomethingOffset { get; set; }

        [StringValue(288, 32)]
        public string Build { get; set; }

        [PrimitiveValue(348)]
        public int InternedStringCount { get; set; }

        [PrimitiveValue(348)]
        public int InternedStringSectionSize { get; set; }

        [PrimitiveValue(356)]
        public int InternedStringIndexOffset { get; set; }

        [PrimitiveValue(360)]
        public int InternedStringsOffset { get; set; }

        [StringValue(408, 32)]
        public string ScenarioName { get; set; }

        [StringValue(444, 32)]
        public string ScenarioPath { get; set; }

        [PrimitiveValue(704)]
        public uint TagNameCount { get; set; }

        [PrimitiveValue(708)]
        public uint TagNamesOffset { get; set; }

        [PrimitiveValue(712)]
        public uint TagNamesSectionSize { get; set; }

        [PrimitiveValue(716)]
        public uint TagNamesIndexOffset { get; set; }

        // list of int that appear to index into some string blob
        // Snowbound: 0,1,9,18,27,37,47
        [PrimitiveValue(1220)] 
        public uint Offset2 { get; set; }

        [PrimitiveValue(1232)]
        public uint Offset4 { get; set; }

        [PrimitiveValue(1236)]
        public uint Offset5 { get; set; }

        [PrimitiveValue(1244)]
        public uint Offset6 { get; set; }
    }
}
