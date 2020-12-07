using OpenBlam.Core.MapLoading;

namespace OpenBlam.Core.Maps
{
    public interface IMap
    {
        void Load(MapStream mapStream);
    }
}
