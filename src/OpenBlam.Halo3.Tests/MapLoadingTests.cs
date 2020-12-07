﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenBlam.Core.MapLoading;
using OpenBlam.Halo3.Map;

namespace OpenBlam.Halo3.Tests
{
    [TestClass]
    public class MapLoadingTests
    {
        [TestMethod, TestCategory("RequiresMaps")]
        public void Map_Loads()
        {
            var loader = MapLoader.FromRoot<H3Map>(@"C:\Program Files\ModifiableWindowsApps\HaloMCC\halo3\maps");

            var map = loader.Load(@"010_jungle.map");

            Assert.IsNotNull(map);
            Assert.IsNotNull(map.Header);
        }
    }
}
