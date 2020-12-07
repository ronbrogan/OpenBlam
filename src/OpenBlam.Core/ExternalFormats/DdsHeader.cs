using OpenBlam.Core.Extensions;
using OpenBlam.Core.Texturing;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBlam.Core.ExternalFormats
{
    public static class DdsHeader
    {
        public const int Magic = 0x20534444; // "DDS "
        public const int Length = 128;
        private static DdsFlags DefaultFlags = DdsFlags.Caps | DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat;

        public static Span<byte> Create(TextureFormat format, 
            Caps capabilities, 
            Caps2 capabilities2, 
            int width, int height, 
            int? depth, int? mipMapCount, 
            int? pitch, int? linearSize)
        {
            Span<byte> data = new byte[128];
            data.WriteInt32At(0, Magic);
            data.WriteInt32At(4, Length - 4); // Remove 4 from length for magic size
            var flags = DefaultFlags;
            if (depth.HasValue)
                flags |= DdsFlags.Depth;
            if (mipMapCount.HasValue)
                flags |= DdsFlags.MipMapCount;
            if (pitch.HasValue)
                flags |= DdsFlags.Pitch;
            if (linearSize.HasValue)
                flags |= DdsFlags.LinearSize;

            data.WriteInt32At(8, (int)flags);
            data.WriteInt32At(12, height);
            data.WriteInt32At(16, width);
            data.WriteInt32At(20, (pitch ?? linearSize).GetValueOrDefault());
            data.WriteInt32At(24, depth.GetValueOrDefault());
            data.WriteInt32At(28, mipMapCount.GetValueOrDefault() == 0 ? 1 : mipMapCount.GetValueOrDefault());
            
            // 44 bytes of reserved space

            DdsPixelFormat.Write(format, data.Slice(76));

            if (flags.HasFlag(DdsFlags.MipMapCount))
                capabilities |= Caps.MipMap;
            data.WriteInt32At(108, (int)capabilities);
            data.WriteInt32At(112, (int)capabilities2);
            data.WriteInt32At(116, 0); // Caps3 (unused)
            data.WriteInt32At(120, 0); // Caps4 (unused)
            data.WriteInt32At(124, 0); // Reserved

            return data;
        }

        [Flags]
        private enum DdsFlags
        {
            Caps = 0x1,
            Height = 0x2,
            Width = 0x4,
            Pitch = 0x8,
            PixelFormat = 0x1000,
            MipMapCount = 0x20000,
            LinearSize = 0x80000,
            Depth = 0x800000
        }

        [Flags]
        public enum Caps
        {
            Complex = 0x8,
            MipMap = 0x400000,
            Texture = 0x1000
        }

        [Flags]
        public enum Caps2
        {
            None = 0x0,
            Cubemap = 0x200,
            CubemapPositiveX = 0x400,
            CubemapNegativeX = 0x800,
            CubemapPositiveY = 0x1000,
            CubemapNegativeY = 0x2000,
            CubemapPositiveZ = 0x4000,
            CubemapNegativeZ = 0x8000,
            Volume = 0x200000
        }

        private class DdsPixelFormat
        {
            public static void Write(TextureFormat format, Span<byte> data)
            {
                data.WriteInt32At(0, 32);
                data.WriteInt32At(4, (int)FlagLookup[format]);
                data.WriteInt32At(8, BitConverter.ToInt32(Encoding.ASCII.GetBytes(FourCCLookup[format])));
                data.WriteInt32At(12, BppLookup[format]);

                var masks = RgbaMaskLookup[format];
                data.WriteUInt32At(16, masks.Item1);
                data.WriteUInt32At(20, masks.Item2);
                data.WriteUInt32At(24, masks.Item3);
                data.WriteUInt32At(28, masks.Item4);
            }

            private static Dictionary<TextureFormat, PixelFormatFlags> FlagLookup = new Dictionary<TextureFormat, PixelFormatFlags>
            {
                { TextureFormat.A8, PixelFormatFlags.Alpha },
                { TextureFormat.L8, PixelFormatFlags.Luminance },
                { TextureFormat.A8L8, PixelFormatFlags.Luminance | PixelFormatFlags.AlphaPixels},
                { TextureFormat.R5G6B5, PixelFormatFlags.UncompressedRGB },
                { TextureFormat.U8V8, PixelFormatFlags.BumpDuDv },
                { TextureFormat.A4R4G4B4, PixelFormatFlags.UncompressedRGB | PixelFormatFlags.AlphaPixels },
                { TextureFormat.R8G8B8, PixelFormatFlags.UncompressedRGB },
                { TextureFormat.A8R8G8B8, PixelFormatFlags.UncompressedRGB | PixelFormatFlags.AlphaPixels },
                { TextureFormat.DXT1, PixelFormatFlags.CompressedRGB  },
                { TextureFormat.DXT23,PixelFormatFlags.CompressedRGB  },
                { TextureFormat.DXT45,PixelFormatFlags.CompressedRGB  },
            };

            private static Dictionary<TextureFormat, string> FourCCLookup = new Dictionary<TextureFormat, string>
            { 
                { TextureFormat.A8, "\0\0\0\0"},
                { TextureFormat.L8, "\0\0\0\0"},
                { TextureFormat.A8L8, "\0\0\0\0"},
                { TextureFormat.U8V8, "\0\0\0\0" },
                { TextureFormat.R5G6B5, "\0\0\0\0" },
                { TextureFormat.A4R4G4B4, "\0\0\0\0"},
                { TextureFormat.R8G8B8, "\0\0\0\0"},
                { TextureFormat.A8R8G8B8, "\0\0\0\0"},
                { TextureFormat.DXT1, "DXT1" },
                { TextureFormat.DXT23, "DXT3" },
                { TextureFormat.DXT45, "DXT5" },
            };

            private static Dictionary<TextureFormat, int> BppLookup = new Dictionary<TextureFormat, int>
            {
                { TextureFormat.A8, 8},
                { TextureFormat.L8, 8},
                { TextureFormat.A8L8, 16},
                { TextureFormat.U8V8, 16 },
                { TextureFormat.R5G6B5, 16 },
                { TextureFormat.A4R4G4B4, 16},
                { TextureFormat.R8G8B8, 32},
                { TextureFormat.A8R8G8B8, 32},
                { TextureFormat.DXT1, 0 },
                { TextureFormat.DXT23, 0 },
                { TextureFormat.DXT45, 0 },
            };

            private static Dictionary<TextureFormat, (uint, uint, uint, uint)> RgbaMaskLookup = new Dictionary<TextureFormat, (uint, uint, uint, uint)>
            {
                { TextureFormat.A8, (0x00, 0x00, 0x00, 0xff)},
                { TextureFormat.L8, (0xff, 0x00, 0x00, 0x00)},
                { TextureFormat.A8L8, (0x00ff, 0x0000, 0x0000, 0xff00)},
                { TextureFormat.U8V8, (0x00ff, 0xff00, 0x0000, 0x0000)},
                { TextureFormat.R5G6B5, (0x0000f800, 0x000007e0, 0x0000001f, 0x00000000) },
                { TextureFormat.A4R4G4B4, (0x00000f00, 0x000000f0, 0x0000000f, 0x0000f000)},
                { TextureFormat.R8G8B8, (0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000)},
                { TextureFormat.A8R8G8B8, (0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000)},
                { TextureFormat.DXT1, (0, 0, 0, 0) },
                { TextureFormat.DXT23, (0, 0, 0, 0) },
                { TextureFormat.DXT45, (0, 0, 0, 0) },
            };

            [Flags]
            private enum PixelFormatFlags 
            {
                AlphaPixels = 0x1,
                Alpha = 0x2,
                CompressedRGB = 0x4, // Dictates that FourCC is provided
                UncompressedRGB = 0x40, // Count and maks contain data
                Yuv = 0x200, // Count and maks contain data
                Luminance = 0x2000,
                BumpDuDv = 0x00080000
            }
        }
    }
}
