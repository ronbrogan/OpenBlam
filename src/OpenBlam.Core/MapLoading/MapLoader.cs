using OpenBlam.Core.Maps;
using OpenBlam.Core.Streams;
using OpenBlam.Serialization;
using System.IO;

namespace OpenBlam.Core.MapLoading
{
    /// <summary>
    /// The MapLoader class is responsible for expsosing the map data properly (decompressing, etc if required)
    /// and reading the initial map header. After basic loading, it will invoke the TMap type's methods to 
    /// finish loading the data. 
    /// </summary>
    /// <typeparam name="TMap"></typeparam>
    public class MapLoader
    {
        public static MapLoader FromConfig(MapLoaderConfig config)
        {
            return new MapLoader(config);
        }

        public static MapLoader FromRoot(string mapRoot)
        {
            return new MapLoader(new MapLoaderConfig()
            {
                MapRoot = mapRoot
            });
        }

        private readonly MapLoaderConfig config;

        /// <summary>
        /// Creates a MapLoader instance that uses the current directory and has no ancillary maps configured.
        /// This can be useful for loading only a singular map directly.
        /// </summary>
        public MapLoader()
        {
            this.config = new MapLoaderConfig();
        }

        public MapLoader(MapLoaderConfig config)
        {
            this.config = config;
        }

        public TMap Load<TMap>(string mapName) where TMap : IMap, new()
        {
            var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, mapName));
            return this.Load<TMap>(fs);
        }

        public TMap Load<TMap>(Stream mapStream) where TMap : IMap, new()
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

        private void Deserialize<TMap>(TMap scene, MapStream reader)
        {
            BlamSerializer.DeserializeInto(scene, reader.Map);
        }
    }
}
