using OpenBlam.Core.MapLoading;
using OpenBlam.Serialization.Layout;

namespace OpenBlam.Core.Maps
{
    [ArbitraryLength]
    public abstract class HaloMap<THeader> : IMap
    {
        protected MapStream mapStream;

        public HaloMap()
        {
        }

        [InPlaceObject(0)]
        public THeader Header { get; set; }

        /// <summary>
        /// A method that is invoked by the <see cref="MapLoader"/> after intial deserialization is complete.
        /// This can be used to manually read additional data, create indices, etc to prepare the map for use.
        /// </summary>
        /// <param name="mapStream"></param>
        public virtual void Load(MapStream mapStream)
        {
            this.mapStream = mapStream;
        }
    }
}
