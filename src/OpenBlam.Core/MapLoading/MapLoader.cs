using OpenBlam.Core.Maps;
using OpenBlam.Core.Streams;
using OpenBlam.Serialization;
using System.IO;

namespace OpenBlam.Core.MapLoading
{
    public static class MapLoader
    {
        public static MapLoader<TMap> FromConfig<TMap>(MapLoaderConfig<TMap> config) where TMap : IMap, new()
        {
            return new MapLoader<TMap>(config);
        }

        public static MapLoader<TMap> FromRoot<TMap>(string mapRoot) where TMap : IMap, new()
        {
            return new MapLoader<TMap>(new MapLoaderConfig<TMap>()
            {
                MapRoot = mapRoot
            });
        }
    }

    /// <summary>
    /// The MapLoader class is responsible for expsosing the map data properly (decompressing, etc if required)
    /// and reading the initial map header. After basic loading, it will invoke the TMap type's methods to 
    /// finish loading the data. 
    /// </summary>
    /// <typeparam name="TMap"></typeparam>
    public class MapLoader<TMap> where TMap: IMap, new()
    {
        private readonly MapLoaderConfig<TMap> config;

        /// <summary>
        /// Creates a MapLoader instance that uses the current directory and has no ancillary maps configured.
        /// This can be useful for loading only a singular map directly.
        /// </summary>
        public MapLoader()
        {
            this.config = new MapLoaderConfig<TMap>();
        }

        public MapLoader(MapLoaderConfig<TMap> config)
        {
            this.config = config;
        }

        public TMap Load(string mapName)
        {
            var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, mapName));
            return this.Load(fs);
        }

        public TMap Load(Stream mapStream)
        {
            var reader = GetAggregateStream(mapStream);

            var map = new TMap();

            this.Deserialize(map, reader);

            map.Load(reader);

            return map;
        }

        private MapStream GetAggregateStream(Stream mapStream)
        {
            var stream = new MapStream(mapStream);

            foreach(var (key,map) in this.config.AncillaryMaps)
            {
                var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, map));
                stream.UseAncillaryMap(key, fs);
            }

            return stream;
        }

        private void Deserialize(TMap scene, MapStream reader)
        {
            BlamSerializer.DeserializeInto(scene, reader.Map);
        }
    }
}
