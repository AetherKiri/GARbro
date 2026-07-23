using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Formats.Strings;

namespace GameRes.Formats.Jikkenshitsu
{
    internal sealed class SpMetaData : ImageMetaData
    {
        public int Flags;
        public int Colors;
        public byte[] Key;

        public bool IsEncrypted { get { return (Flags & 8) != 0; } }
    }

    [Serializable]
    public sealed class SjDatScheme : ResourceScheme
    {
        public Dictionary<string, byte[]> KnownSchemes;
    }

    [Export(typeof(ImageFormat))]
    public sealed class SpDatFormat : ImageFormat
    {
        public override string Tag { get { return "DAT/SPEED"; } }
        public override string Description { get { return "Studio Jikkenshitsu image format"; } }
        public override uint Signature { get { return 0; } }

        public SpDatFormat ()
        {
            Signatures = new uint[] { 0x010003, 0x010007, 0x01000B, 0x010046, 0 };
        }

        static readonly SjDatScheme DefaultScheme = new SjDatScheme {
            KnownSchemes = SjDatKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("DAT/SPEED scheme is data-backed and read-only."); }
        }

        public override ImageMetaData ReadMetaData (IBinaryStream file)
        {
            var header = file.ReadHeader (0x22);
            if (header.ToInt32 (4) != 0)
                return null;
            int flags = header.ToUInt16 (0);
            if ((flags & ~0xFF) != 0 || header[2] != 1)
                return null;
            var info = new SpMetaData {
                Width = header.ToUInt16 (0x16),
                Height = header.ToUInt16 (0x18),
                BPP = 8,
                Flags = flags,
                Colors = header.ToUInt16 (0x1E),
            };
            if (info.Width == 0 || info.Width > 0x2000 || info.Height == 0 || info.Height > 0x2000
                || info.Colors > 0x100)
                return null;
            if (info.IsEncrypted)
            {
                info.Key = ResolveKey (file.Name);
                if (info.Key == null)
                    return null;
            }
            return info;
        }

        public override ImageData Read (IBinaryStream file, ImageMetaData info)
        {
            var reader = new SpReader (file, (SpMetaData)info);
            var pixels = reader.Unpack ();
            return ImageData.CreateFlipped (info, reader.Format, reader.Palette, pixels, reader.Stride);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotSupportedException ("DAT/SPEED writing is not implemented.");
        }

        internal static byte[] ResolveKey (string fileName)
        {
            var title = FormatCatalog.Instance.LookupGame (fileName);
            if (string.IsNullOrEmpty (title))
                title = FormatCatalog.Instance.LookupGame (fileName, @"..\*.exe");
            if (string.IsNullOrEmpty (title) || !DefaultScheme.KnownSchemes.TryGetValue (title, out var key))
                return null;
            return key;
        }
    }

    internal sealed class SpReader
    {
        readonly IBinaryStream m_input;
        readonly SpMetaData m_info;
        readonly byte[] m_output;
        int m_stride;

        public PixelFormat Format { get; private set; }
        public int Stride { get { return m_stride; } }
        public BitmapPalette Palette { get; private set; }

        public SpReader (IBinaryStream input, SpMetaData info)
        {
            m_input = input;
            m_info = info;
            m_output = new byte[checked (info.Width * info.Height)];
            m_stride = info.iWidth;
        }

        public byte[] Unpack ()
        {
            m_input.Position = 0x22;
            int packedSize = m_input.ReadInt32 ();
            if (packedSize < 0 || packedSize > m_input.Length - m_input.Position)
                throw new InvalidFormatException ();
            if (m_info.Colors > 0)
                Palette = ImageFormat.ReadPalette (m_input.AsStream, m_info.Colors);
            UnpackStream (m_output, packedSize);
            if ((m_info.Flags & 0xF4) == 4)
            {
                packedSize = m_input.ReadInt32 ();
                if (packedSize < 0 || packedSize > m_input.Length - m_input.Position)
                    throw new InvalidFormatException ();
                if (packedSize != 0)
                {
                    var alpha = new byte[m_output.Length >> 1];
                    UnpackStream (alpha, packedSize);
                    return ConvertToRgbA (alpha);
                }
            }
            Format = m_info.Colors > 0 ? PixelFormats.Indexed8 : PixelFormats.Gray4;
            return m_output;
        }

        void UnpackStream (byte[] output, int packedSize)
        {
            long inputPosition = m_input.Position;
            if (packedSize == 0)
            {
                if (output.Length != m_input.Read (output, 0, output.Length))
                    throw new InvalidFormatException ();
                return;
            }
            Stream input = m_input.AsStream;
            if (m_info.IsEncrypted)
            {
                input = new StreamRegion (input, inputPosition, packedSize, true);
                input = new InputCryptoStream (input, new SjTransform (m_info.Key));
            }
            try
            {
                UnpackRle (input, output);
            }
            finally
            {
                if (input != m_input.AsStream)
                    input.Dispose ();
                m_input.Position = inputPosition + packedSize;
            }
        }

        static void UnpackRle (Stream input, byte[] output)
        {
            int dst = 0;
            int state = 0;
            byte pixel = 0;
            while (dst < output.Length)
            {
                int value = input.ReadByte ();
                if (value < 0)
                    throw new InvalidFormatException ();
                if (state == 0)
                {
                    state = 1;
                    output[dst++] = (byte)value;
                }
                else if (state == 1)
                {
                    if (output[dst - 1] == value)
                    {
                        pixel = (byte)value;
                        state = 2;
                    }
                    output[dst++] = (byte)value;
                }
                else
                {
                    int count = value - 2;
                    if (count < 0 || count > output.Length - dst)
                        throw new InvalidFormatException ();
                    for (int i = 0; i < count; ++i)
                        output[dst++] = pixel;
                    state = 0;
                }
            }
        }

        byte[] ConvertToRgbA (byte[] alpha)
        {
            m_stride = checked (m_info.iWidth * 4);
            var pixels = new byte[checked (m_stride * m_info.iHeight)];
            var colors = Palette?.Colors;
            if (colors == null)
                throw new InvalidFormatException ();
            int dst = 0;
            for (int src = 0; src < m_output.Length; ++src)
            {
                var color = colors[m_output[src]];
                int alphaValue = ((alpha[src >> 1] >> ((~src & 1) << 2)) & 0xF) * 0x11;
                pixels[dst++] = color.B;
                pixels[dst++] = color.G;
                pixels[dst++] = color.R;
                pixels[dst++] = (byte)alphaValue;
            }
            Format = PixelFormats.Bgra32;
            return pixels;
        }
    }
}
