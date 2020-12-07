using OpenBlam.Core.Maps;
using System.Collections.Generic;

namespace OpenBlam.Core.MapLoading
{
    public static class MapLoaderBuilder
    {
        public static IMapLoaderBuilder<TMap> FromRoot<TMap>(string mapRoot) where TMap : IMap, new()
        {
            return new MapLoaderBuilderImpl<TMap>(mapRoot);
        }
    }

    internal class MapLoaderBuilderImpl<TMap> : IMapLoaderBuilder<TMap> where TMap : IMap, new()
    {
        private readonly string mapRoot;
        private Dictionary<byte, string> ancillaryMaps = new();

        internal MapLoaderBuilderImpl(string mapRoot)
        {
            this.mapRoot = mapRoot;
        }

        public IMapLoaderBuilder<TMap> UseAncillaryMap(byte key, string mapName)
        {
            ancillaryMaps.Add(key, mapName);
            return this;
        }

        public MapLoader<TMap> Build()
        {
            return new MapLoader<TMap>(new MapLoaderConfig<TMap>()
            {
                MapRoot = this.mapRoot,
                AncillaryMaps = this.ancillaryMaps
            });
        }
    }
}
