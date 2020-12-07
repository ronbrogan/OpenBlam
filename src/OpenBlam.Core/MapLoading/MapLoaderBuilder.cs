using OpenBlam.Core.Maps;
using System.Collections.Generic;

namespace OpenBlam.Core.MapLoading
{
    public static class MapLoaderBuilder
    {
        public static IMapLoaderBuilder FromRoot(string mapRoot)
        {
            return new MapLoaderBuilderImpl(mapRoot);
        }
    }

    internal class MapLoaderBuilderImpl : IMapLoaderBuilder
    {
        private readonly string mapRoot;
        private Dictionary<byte, string> ancillaryMaps = new();

        internal MapLoaderBuilderImpl(string mapRoot)
        {
            this.mapRoot = mapRoot;
        }

        public IMapLoaderBuilder UseAncillaryMap(byte key, string mapName)
        {
            ancillaryMaps.Add(key, mapName);
            return this;
        }

        public MapLoader Build()
        {
            return new MapLoader(new MapLoaderConfig()
            {
                MapRoot = this.mapRoot,
                AncillaryMaps = this.ancillaryMaps
            });
        }
    }
}
