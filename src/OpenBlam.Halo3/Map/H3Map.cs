using OpenBlam.Core.MapLoading;
using OpenBlam.Core.Maps;

namespace OpenBlam.Halo3.Map
{
    public class H3Map : HaloMap<H3MapHeader>
    {
        public H3Map()
        {
        }

        public override void Load(MapStream mapStream)
        {
            base.Load(mapStream);
        }
    }
}
