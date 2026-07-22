using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media;
using GameRes.Compression;
using GameRes.Utility;

namespace GameRes.Formats.Crowd
{
    internal sealed class CrzMetaData : ImageMetaData
    {
        public int HeaderSize;
    }

    [Serializable]
    public sealed class CrzScheme : ResourceScheme
    {
        public IDictionary<string, byte[]> KnownKeys;
    }

    [Export(typeof(ImageFormat))]
    public sealed class CrzFormat : ImageFormat
    {
        public override string Tag { get { return "CRZ"; } }
        public override string Description { get { return "Crowd encrypted image format"; } }
        public override uint Signature { get { return 0x44445A53; } }

        static readonly CrzScheme DefaultScheme = new CrzScheme {
            KnownKeys = CrzKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("CRZ scheme is data-backed and read-only."); }
        }

        public override ImageMetaData ReadMetaData (IBinaryStream file)
        {
            file.Position = 0xE;
            using (var lz = OpenCrzStream (file))
            {
                int maxHeaderLength = DefaultScheme.KnownKeys.Keys.Max (x => x.Length);
                var header = new byte[maxHeaderLength + 0x35];
                if (header.Length != lz.Read (header, 0, header.Length))
                    return null;
                var id = Binary.GetCString (header, 0, maxHeaderLength);
                byte[] key;
                if (!DefaultScheme.KnownKeys.TryGetValue (id, out key))
                    return null;
                int seedPos = id.Length + 1;
                int headerPos = seedPos + 0x10;
                for (int i = 0; i < 0x24; ++i)
                    header[headerPos+i] ^= (byte)(key[i] ^ header[seedPos + (i & 0xF)]);
                uint width = header.ToUInt32 (headerPos + 4);
                uint height = header.ToUInt32 (headerPos + 0x10);
                int headerSize = header.ToInt32 (headerPos + 0x18) + 0x34 + seedPos;
                if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue
                    || headerSize < seedPos + 0x10 + 0x24)
                    return null;
                return new CrzMetaData {
                    Width = width,
                    Height = height,
                    BPP = 16,
                    HeaderSize = headerSize,
                };
            }
        }

        public override ImageData Read (IBinaryStream file, ImageMetaData info)
        {
            file.Position = 0xE;
            using (var lz = OpenCrzStream (file))
            {
                var meta = (CrzMetaData)info;
                if (meta.HeaderSize < 0 || meta.HeaderSize > 0x1000000)
                    throw new InvalidFormatException ();
                var header = new byte[meta.HeaderSize];
                if (header.Length != lz.Read (header, 0, header.Length))
                    throw new InvalidFormatException ();
                int stride = checked (info.iWidth * 2);
                var pixels = new byte[checked (stride * info.iHeight)];
                if (pixels.Length != lz.Read (pixels, 0, pixels.Length))
                    throw new InvalidFormatException ();
                return ImageData.Create (info, PixelFormats.Bgr555, null, pixels, stride);
            }
        }

        LzssStream OpenCrzStream (IBinaryStream file)
        {
            var lz = new LzssStream (file.AsStream, LzssMode.Decompress, true);
            lz.Config.FrameSize = 0x1000;
            lz.Config.FrameFill = 0x20;
            lz.Config.FrameInitPos = 0x1000 - 0x10;
            return lz;
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotSupportedException ("CRZ writing is not implemented.");
        }
    }
}
