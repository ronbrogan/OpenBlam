using OpenBlam.Core.Maps;
using OpenBlam.Core.Streams;
using OpenBlam.Serialization;
using System.IO;

namespace OpenBlam.Core.MapLoading
{
    public delegate void MapLoadCallback(IMap map, Stream mapStream);

    /// <summary>
    /// A delegate to allow transforming on-disk map streams into something that can be read.
    /// Common use case is for on-the-fly decompression.
    /// </summary>
    /// <param name="rawStream">The raw stream on disk. The ownership of this stream is transferred to the recipient.</param>
    /// <returns>A transformed stream</returns>
    public delegate Stream MapStreamTransform(Stream rawStream);

    /// <summary>
    /// The MapLoader class is responsible for expsosing the map data properly (decompressing, etc if required)
    /// and reading the initial map header. After basic loading, it will invoke the TMap type's methods to 
    /// finish loading the data. 
    /// </summary>
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

        /// <summary>
        /// Loads the specified file as a map.
        /// </summary>
        /// <param name="mapName">The filename to load map data from. Will be combined with the current config's MapRoot</param>
        /// <param name="loadCallback">An optional callback that will be invoked after each map is created</param>
        public TMap Load<TMap>(string mapName, MapLoadCallback loadCallback = null) where TMap : IMap, new()
        {
            var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, mapName));
            return this.Load<TMap>(fs, loadCallback);
        }

        /// <summary>
        /// Loads the specified file as a map.
        /// </summary>
        /// <param name="mapName">The filename to load map data from. Will be combined with the current config's MapRoot</param>
        /// <param name="streamTransform">A delegate to tranform the map streams into a format that can be read. Typical use case is if the maps on disk are compressed.</param>
        /// <param name="loadCallback">An optional callback that will be invoked after each map is created</param>
        public TMap Load<TMap>(string mapName, MapStreamTransform streamTransform, MapLoadCallback loadCallback = null) where TMap : IMap, new()
        {
            var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, mapName));
            return this.Load<TMap>(fs, streamTransform, loadCallback);
        }

        /// <summary>
        /// Loads the specified stream as a map.
        /// </summary>
        /// <param name="mapStream">The stream to load data from</param>
        /// <param name="loadCallback">An optional callback that will be invoked after each map is created</param>
        public TMap Load<TMap>(Stream mapStream, MapLoadCallback loadCallback = null) where TMap : IMap, new()
        {
            var reader = this.GetAggregateStream(mapStream);

            var map = this.CreateMap<TMap>(reader);

            foreach(var id in this.config.AncillaryMaps.Keys)
            {
                var anc = this.CreateMap<TMap>(reader, id);
                map.UseAncillaryMap(id, anc);
                loadCallback?.Invoke(anc, reader.GetStream(id));
            }

            loadCallback?.Invoke(map, reader.Map);

            return map;
        }

        /// <summary>
        /// Loads the specified stream as a map.
        /// </summary>
        /// <param name="mapStream">The stream to load data from. This stream will be processed by streamTransform (which will take ownership)</param>
        /// /// <param name="streamTransform">A delegate to tranform the map streams into a format that can be read. Typical use case is if the maps on disk are compressed.</param>
        /// <param name="loadCallback">An optional callback that will be invoked after each map is created</param>
        public TMap Load<TMap>(Stream mapStream, MapStreamTransform streamTransform, MapLoadCallback loadCallback = null) where TMap : IMap, new()
        {
            var reader = this.GetAggregateStream(mapStream, streamTransform);

            var map = this.CreateMap<TMap>(reader);

            foreach (var id in this.config.AncillaryMaps.Keys)
            {
                var anc = this.CreateMap<TMap>(reader, id);
                map.UseAncillaryMap(id, anc);
                loadCallback?.Invoke(anc, reader.GetStream(id));
            }

            loadCallback?.Invoke(map, reader.Map);

            return map;
        }

        private MapStream GetAggregateStream(Stream mapStream)
        {
            var stream = new MapStream(mapStream);

            foreach (var (key, map) in this.config.AncillaryMaps)
            {
                var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, map));
                stream.UseAncillaryMap(key, fs);
            }

            return stream;
        }

        private MapStream GetAggregateStream(Stream mapStream, MapStreamTransform streamTransform)
        {
            var stream = new MapStream(streamTransform(mapStream));

            foreach(var (key,map) in this.config.AncillaryMaps)
            {
                var fs = new ReadOnlyFileStream(Path.Combine(this.config.MapRoot, map));
                stream.UseAncillaryMap(key, streamTransform(fs));
            }

            return stream;
        }

        private TMap CreateMap<TMap>(MapStream reader, byte streamId = 0) where TMap : IMap, new()
        {
            var map = new TMap();
            BlamSerializer.DeserializeInto(map, reader.GetStream(streamId));
            map.Load(streamId, reader);
            return map;
        }
    }
}
