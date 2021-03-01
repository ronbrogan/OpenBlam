using OpenBlam.Core.MapLoading;
using OpenBlam.Serialization.Layout;
using System;
using System.IO;

namespace OpenBlam.Core.Maps
{
    [ArbitraryLength]
    public abstract class HaloMap<THeader> : IMap, IDisposable
    {
        protected MapStream mapStream;
        protected Stream localStream;
        

        public HaloMap()
        {
        }

        [InPlaceObject(0)]
        public THeader Header { get; set; }

        /// <inheritdoc/>
        public virtual void Load(byte selfIdentifier, MapStream mapStream)
        {
            this.mapStream = mapStream;
            this.localStream = mapStream.GetStream(selfIdentifier);
        }

        /// <inheritdoc/>
        public virtual void UseAncillaryMap(byte identifier, IMap ancillaryMap)
        {
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.localStream?.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
