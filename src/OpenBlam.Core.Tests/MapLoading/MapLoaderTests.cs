using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenBlam.Core.MapLoading;
using OpenBlam.Core.Maps;
using OpenBlam.Serialization.Layout;
using System;
using System.IO;

namespace OpenBlam.Core.Tests.MapLoading
{
    [TestClass]
    public class MapLoaderTests
    {
        [TestMethod]
        public void Loader_IsCreated()
        {
            var loader = MapLoaderBuilder.FromRoot<UnitTestMap>(@"D:\testroot").Build();
            Assert.IsNotNull(loader, "Construction from builder should succeed");

            var loader2 = MapLoader.FromRoot<UnitTestMap>(@"D:\testroot");
            Assert.IsNotNull(loader2, "Construction from root should succeed");

            var config = new MapLoaderConfig<UnitTestMap>()
            {
                MapRoot = @"D:\testroot"
            };
            var loader3 = MapLoader.FromConfig(config);
            Assert.IsNotNull(loader3, "Construction from config should succeed");
        }

        [TestMethod]
        public void Loader_FailsOnNonSerializable()
        {
            var loader = new MapLoader<UnitTestMap>();

            Assert.ThrowsException<NotSupportedException>(() =>
            {
                var map = loader.Load(new MemoryStream());
            });
        }

        [TestMethod]
        public void Loader_InvokesLoadMethod()
        {
            var loader = new MapLoader<DeserializableUnitTestMap>();
            var map = loader.Load(new MemoryStream());
            Assert.IsTrue(map.DoneLoading);
        }
    }

    public class UnitTestMap : IMap
    {
        public void Load(MapStream mapStream)
        {
        }
    }

    [SerializableType]
    public class DeserializableUnitTestMap : IMap
    {
        public bool DoneLoading = false;

        public void Load(MapStream mapStream)
        {
            DoneLoading = true;
        }
    }
}
