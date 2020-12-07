using OpenBlam.Core.Maps;
using System.Collections.Generic;
using System.IO;

namespace OpenBlam.Core.MapLoading
{
    public class MapLoaderConfig
    {
        public string MapRoot { get; set; } = Directory.GetCurrentDirectory();

        public Dictionary<byte, string> AncillaryMaps { get; set; } = new();
    }
}
