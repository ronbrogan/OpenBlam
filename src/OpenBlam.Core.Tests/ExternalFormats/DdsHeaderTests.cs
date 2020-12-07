using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using OpenBlam.Core.ExternalFormats;
using OpenBlam.Core.Extensions;
using OpenBlam.Core.Texturing;
using System;

namespace OpenBlam.Core.Tests.Formats
{
    [TestClass]
    public class DdsHeaderTests
    {

        [TestMethod]
        public void DdsHeader_CreatesValidHeader()
        {
            var dim = 256;

            var header = DdsHeader.Create(TextureFormat.DXT45, DdsHeader.Caps.Texture, DdsHeader.Caps2.None, dim, dim, 1, 1, null, 16384);

            Assert.AreEqual(128, header.Length);

            // Manual verification, paste into 010 Editor, run DDS template
            Logger.LogMessage("0x" + BitConverter.ToString(header.ToArray()).Replace("-", string.Empty));

            Assert.AreEqual(dim, header.ReadInt32At(12), "Height should match");
            Assert.AreEqual(dim, header.ReadInt32At(16), "Width should match");

            Assert.AreEqual(32, header.ReadInt32At(76), "DDS_PIXELFORMAT struct length should be 32");
            Assert.AreEqual("DXT5", header.ReadStringFrom(84, 4), "PixelFormat should match");

            Assert.AreEqual(0, header.ReadInt32At(116), "Reserved data should be 0");
            Assert.AreEqual(0, header.ReadInt32At(120), "Reserved data should be 0");
            Assert.AreEqual(0, header.ReadInt32At(124), "Reserved data should be 0");
        }
    }
}
