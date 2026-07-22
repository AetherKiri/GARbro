using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Utility;

namespace GameRes.Formats.FC01
{
    internal sealed class McgMetaData : ImageMetaData
    {
        public int DataOffset;
        public int PackedSize;
        public int Version;
        public int ChannelsCount;
    }

    [Serializable]
    public sealed class McgScheme : ResourceScheme
    {
        public Dictionary<string, byte> KnownKeys;
    }

    [Export(typeof(ImageFormat))]
    public sealed class McgFormat : ImageFormat
    {
        public override string Tag { get { return "MCG"; } }
        public override string Description { get { return "F&C Co. image format"; } }
        public override uint Signature { get { return 0x2047434D; } } // 'MCG'

        static readonly McgScheme DefaultScheme = new McgScheme {
            KnownKeys = McgKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("MCG scheme is data-backed and read-only."); }
        }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            var header = stream.ReadHeader (0x40);
            if (header[5] != '.')
                return null;
            int version = header[4] * 100 + header[6] * 10 + header[7] - 0x14D0;
            if (version != 100 && version != 101)
                throw new NotSupportedException ("MCG variant is not supported: " + version);
            int headerSize = header.ToInt32 (0x10);
            int bpp = header.ToInt32 (0x24);
            int packedSize = header.ToInt32 (0x38);
            if (headerSize < 0x40 || bpp != 24 || packedSize < headerSize
                || packedSize > stream.Length || header.ToUInt32 (0x1C) == 0 || header.ToUInt32 (0x20) == 0)
                return null;
            return new McgMetaData {
                Width = header.ToUInt32 (0x1C),
                Height = header.ToUInt32 (0x20),
                OffsetX = header.ToInt32 (0x14),
                OffsetY = header.ToInt32 (0x18),
                BPP = bpp,
                DataOffset = headerSize,
                PackedSize = packedSize,
                Version = version,
                ChannelsCount = header.ToInt32 (0x34),
            };
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            var meta = (McgMetaData)info;
            byte key = 0;
            if (meta.Version == 100 || meta.Version == 101)
            {
                var title = FormatCatalog.Instance.LookupGame (stream.Name);
                if (string.IsNullOrEmpty (title))
                    title = FormatCatalog.Instance.LookupGame (stream.Name, @"..\*.exe");
                if (string.IsNullOrEmpty (title) || !DefaultScheme.KnownKeys.TryGetValue (title, out key))
                    throw new UnknownEncryptionScheme ();
            }
            var reader = new McgDecoder (stream, meta, key);
            reader.Unpack ();
            return ImageData.Create (info, PixelFormats.Bgr24, null, reader.Data, reader.Stride);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotSupportedException ("MCG writing is not implemented.");
        }
    }

    internal sealed class McgDecoder
    {
        readonly IBinaryStream m_file;
        readonly McgMetaData m_info;
        readonly int m_height;
        readonly int m_width;
        byte[] m_input;

        internal byte[] Data { get; private set; }
        internal int Stride { get; private set; }

        internal McgDecoder (IBinaryStream input, McgMetaData info, byte key)
        {
            m_file = input;
            m_info = info;
            m_width = checked ((int)info.Width);
            m_height = checked ((int)info.Height);
            Stride = (m_width * 3 + 3) & ~3;
            Key = key;
        }

        byte Key { get; set; }

        internal void Unpack ()
        {
            m_file.Position = m_info.DataOffset;
            int inputSize = m_info.PackedSize - m_info.DataOffset;
            m_input = m_file.ReadBytes (inputSize);
            if (m_input.Length != inputSize)
                throw new InvalidFormatException ("Unexpected end of MCG data.");
            Decrypt (m_input, 0, Math.Max (0, m_input.Length - 1), Key);
            using (var input = new BinMemoryStream (m_input))
            using (var reader = new McgLzssReader (input, m_input.Length, Stride * m_height))
            {
                reader.Unpack ();
                if (input.Position < input.Length - 1)
                    throw new InvalidFormatException ("MCG data was not fully decoded.");
                Data = reader.Data;
            }
        }

        static void Decrypt (byte[] data, int index, int length, int key)
        {
            while (length > 0)
            {
                var value = data[index];
                data[index++] = (byte)(Binary.RotByteL (value, 1) ^ key);
                key += length--;
            }
        }
    }

    internal sealed class McgLzssReader : IDisposable
    {
        readonly IBinaryStream m_input;
        readonly byte[] m_output;
        readonly int m_size;

        internal byte[] Data { get { return m_output; } }

        internal McgLzssReader (IBinaryStream input, int inputLength, int outputLength)
        {
            m_input = input;
            m_size = inputLength;
            m_output = new byte[outputLength];
        }

        internal void Unpack ()
        {
            int dst = 0;
            var frame = new byte[0x1000];
            int framePos = 0xFEE;
            int remaining = m_size;
            while (remaining > 0 && dst < m_output.Length)
            {
                int control = m_input.ReadUInt8 ();
                --remaining;
                for (int bit = 1; remaining > 0 && bit != 0x100; bit <<= 1)
                {
                    if (dst >= m_output.Length)
                        return;
                    if ((control & bit) != 0)
                    {
                        byte value = m_input.ReadUInt8 ();
                        --remaining;
                        frame[framePos++] = value;
                        framePos &= 0xFFF;
                        m_output[dst++] = value;
                    }
                    else
                    {
                        if (remaining < 2)
                            throw new InvalidFormatException ();
                        int offset = m_input.ReadUInt16 ();
                        remaining -= 2;
                        int count = (offset >> 12) + 3;
                        for (; count != 0 && dst < m_output.Length; --count)
                        {
                            offset &= 0xFFF;
                            byte value = frame[offset++];
                            frame[framePos++] = value;
                            framePos &= 0xFFF;
                            m_output[dst++] = value;
                        }
                    }
                }
            }
            if (dst != m_output.Length)
                throw new InvalidFormatException ("MCG LZSS stream ended early.");
        }

        public void Dispose ()
        {
        }
    }
}
