using OpenBlam.Core.Maps;

namespace OpenBlam.Core.MapLoading
{
    public interface IMapLoaderBuilder
    {
        IMapLoaderBuilder UseAncillaryMap(byte key, string mapName);

        MapLoader Build();
    }
}
