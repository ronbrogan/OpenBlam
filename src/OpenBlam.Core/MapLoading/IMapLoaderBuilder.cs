using OpenBlam.Core.Maps;

namespace OpenBlam.Core.MapLoading
{
    public interface IMapLoaderBuilder<TMap> where TMap : IMap, new()
    {
        IMapLoaderBuilder<TMap> UseAncillaryMap(byte key, string mapName);

        MapLoader<TMap> Build();
    }
}
