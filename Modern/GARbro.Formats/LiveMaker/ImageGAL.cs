using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Utility;

namespace GameRes.Formats.LiveMaker
{
    internal sealed class GalMetaData : ImageMetaData
    {
        public int Version;
        public int FrameCount;
        public bool Shuffled;
        public int Compression;
        public int DataOffset;
    }

    [Serializable]
    public sealed class GalScheme : ResourceScheme
    {
        public Dictionary<string, string> KnownKeys;
    }

    [Export(typeof(ImageFormat))]
    public sealed class GalFormat : ImageFormat
    {
        public override string Tag { get { return "GAL"; } }
        public override string Description { get { return "LiveMaker image format"; } }
        public override uint Signature { get { return 0x656C6147; } }

        static readonly GalScheme DefaultScheme = new GalScheme {
            KnownKeys = GalKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("GAL scheme is data-backed and read-only."); }
        }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            var prefix = new byte[11];
            if (prefix.Length != stream.Read (prefix, 0, prefix.Length)
                || prefix[0] != 'G' || prefix[1] != 'a' || prefix[2] != 'l' || prefix[3] != 'e')
                return null;
            int version = prefix[4] * 100 + prefix[5] * 10 + prefix[6] - 5328;
            if (version < 103 || version > 107)
                return null;
            int headerSize = LittleEndian.ToInt32 (prefix, 7);
            if (headerSize < 0x28 || headerSize > 0x100)
                return null;
            var header = new byte[headerSize];
            if (headerSize != stream.Read (header, 0, headerSize)
                || LittleEndian.ToInt32 (header, 0) != version)
                return null;
            var meta = new GalMetaData {
                Width = LittleEndian.ToUInt32 (header, 4),
                Height = LittleEndian.ToUInt32 (header, 8),
                BPP = LittleEndian.ToInt32 (header, 0xC),
                Version = version,
                FrameCount = LittleEndian.ToInt32 (header, 0x10),
                Shuffled = header[0x15] != 0,
                Compression = header[0x16],
                DataOffset = headerSize + 11,
            };
            if (meta.Width == 0 || meta.Height == 0 || meta.FrameCount < 1)
                return null;
            return meta;
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            var meta = (GalMetaData)info;
            if (meta.Shuffled)
                throw new NotSupportedException ("GAL shuffled/encrypted pixel data is not yet supported.");
            if (meta.Compression != 0)
                throw new NotSupportedException ("GAL compressed or JPEG pixel data is not yet supported.");

            stream.Position = meta.DataOffset;
            uint nameLength = stream.ReadUInt32 ();
            if (nameLength > stream.Length - stream.Position)
                throw new InvalidFormatException ();
            stream.Seek (nameLength, SeekOrigin.Current);
            stream.ReadUInt32 (); // mask
            stream.Seek (9, SeekOrigin.Current);
            int layerCount = stream.ReadInt32 ();
            if (layerCount != 1)
                throw new NotSupportedException ("GAL layered images are not yet supported.");

            int width = stream.ReadInt32 ();
            int height = stream.ReadInt32 ();
            int bpp = stream.ReadInt32 ();
            if (width <= 0 || height <= 0 || width != (int)meta.Width || height != (int)meta.Height)
                throw new InvalidFormatException ();
            if (bpp != 4 && bpp != 8 && bpp != 16 && bpp != 24 && bpp != 32)
                throw new NotSupportedException ("GAL pixel depth is not supported: " + bpp);

            BitmapPalette palette = null;
            if (bpp <= 8)
                palette = new BitmapPalette (ImageFormat.ReadColorMap (stream.AsStream, 1 << bpp));
            int stride = (width * bpp + 7) / 8;
            if (bpp >= 8)
                stride = (stride + 3) & ~3;

            stream.Seek (4 + 4 + 1 + 4 + 4 + 1, SeekOrigin.Current);
            uint layerNameLength = stream.ReadUInt32 ();
            if (layerNameLength > stream.Length - stream.Position)
                throw new InvalidFormatException ();
            stream.Seek (layerNameLength, SeekOrigin.Current);
            if (meta.Version >= 107)
                stream.ReadByte ();
            int layerSize = stream.ReadInt32 ();
            if (layerSize < 0 || layerSize > stream.Length - stream.Position)
                throw new InvalidFormatException ();
            var pixels = stream.ReadBytes (layerSize);
            if (pixels.Length != stride * height)
                throw new InvalidFormatException ();
            int alphaSize = stream.ReadInt32 ();
            if (alphaSize != 0)
                throw new NotSupportedException ("GAL alpha layers are not yet supported.");

            PixelFormat format;
            switch (bpp)
            {
            case 4: format = PixelFormats.Indexed4; break;
            case 8: format = PixelFormats.Indexed8; break;
            case 16: format = PixelFormats.Bgr565; break;
            case 24: format = PixelFormats.Bgr24; break;
            default: format = PixelFormats.Bgr32; break;
            }
            return ImageData.Create (info, format, palette, pixels, stride);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotSupportedException ("GAL writing is not implemented.");
        }
    }
}
