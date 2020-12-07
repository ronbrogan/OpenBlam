using System;
using System.Collections.Generic;
using System.IO;

namespace OpenBlam.Core.MapLoading
{
    public class MapStream
    {
        private readonly Stream map;
        private List<Stream> ancillaryMaps = new List<Stream>();

        public MapStream(Stream map)
        {
            this.map = map;
        }

        public void UseAncillaryMap(byte key, Stream map)
        {
            if(key == 0)
            {
                throw new ArgumentException(nameof(key), "Ancillary maps can only be index 1 or greater");
            }

            ancillaryMaps.Capacity = key;

            ancillaryMaps[key] = map;
        }

        public Stream Map => this.map;

        public Stream GetStream(byte key)
        {
            if(key == 0)
            {
                return this.map;
            }
            else
            {
                return this.ancillaryMaps[key];
            }
        }
    }
}
