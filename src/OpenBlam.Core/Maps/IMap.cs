using OpenBlam.Core.MapLoading;

namespace OpenBlam.Core.Maps
{
    public interface IMap
    {
        /// <summary>
        /// Invoked after the map is initially loaded to provide the map instance with the ability
        /// to access the raw map stream(s). The map instance could be a playable or shared map,
        /// so the stream identifier of the current instance is provided. 
        /// 
        /// The current map instance will henceforth own the identified stream, and be responsible for disposal.
        /// The current map instance shall be able to reference the other streams in the MapStream instance, however
        /// those individual streams are owned by their respective map instances.
        /// </summary>
        /// <param name="selfIdentifier">The stream identifier that corresponds to the current map instance</param>
        /// <param name="mapStream">The aggregate MapStream that contains raw map data</param>
        void Load(byte selfIdentifier, MapStream mapStream);

        /// <summary>
        /// Allows the map to store local references to ancillary maps as they're created by the MapLoader
        /// </summary>
        /// <param name="identifier">The identifier of the ancillary map</param>
        /// <param name="ancillaryMap">The ancillary map instance</param>
        void UseAncillaryMap(byte identifier, IMap ancillaryMap);
    }
}
