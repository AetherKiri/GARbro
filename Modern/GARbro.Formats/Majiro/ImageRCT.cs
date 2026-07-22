using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Utility;

namespace GameRes.Formats.Majiro
{
    internal sealed class RctMetaData : ImageMetaData
    {
        public int Version;
        public bool IsEncrypted;
        public int DataOffset;
        public int DataSize;
    }

    [Serializable]
    public sealed class RctScheme : ResourceScheme
    {
        public Dictionary<string, string> KnownKeys;
    }

    [Export(typeof(ImageFormat))]
    public sealed class RctFormat : ImageFormat
    {
        public override string Tag { get { return "RCT"; } }
        public override string Description { get { return "Majiro game engine RGB image format"; } }
        public override uint Signature { get { return 0x9A925A98; } }

        static readonly RctScheme DefaultScheme = new RctScheme {
            KnownKeys = RctKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("RCT scheme is data-backed and read-only."); }
        }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            var header = stream.ReadHeader (0x14);
            if (header[4] != 'T')
                return null;
            int encryption = header[5];
            if (encryption != 'C' && encryption != 'S')
                return null;
            int version = header[7] - '0';
            if (header[6] != '0' || (version != 0 && version != 1))
                return null;

            uint width = header.ToUInt32 (8);
            uint height = header.ToUInt32 (12);
            int dataSize = header.ToInt32 (16);
            int dataOffset = 0x14;
            if (version == 1)
            {
                dataOffset += 2;
                stream.ReadUInt16 ();
            }
            if (width == 0 || height == 0 || width > 0x8000 || height > 0x8000
                || dataSize < 0 || dataSize > stream.Length - dataOffset)
                return null;

            return new RctMetaData {
                Width = width,
                Height = height,
                OffsetX = 0,
                OffsetY = 0,
                BPP = 24,
                Version = version,
                IsEncrypted = encryption == 'S',
                DataOffset = dataOffset,
                DataSize = dataSize,
            };
        }

        public override ImageData Read (IBinaryStream file, ImageMetaData info)
        {
            var meta = (RctMetaData)info;
            file.Position = meta.DataOffset;
            IBinaryStream input = file;
            if (meta.IsEncrypted)
                input = OpenEncryptedStream (file, meta.DataSize, meta.FileName);
            try
            {
                using (var reader = new RctReader (input, meta))
                {
                    reader.Unpack ();
                    return ImageData.Create (meta, PixelFormats.Bgr24, null, reader.Data, (int)meta.Width * 3);
                }
            }
            finally
            {
                if (!ReferenceEquals (input, file))
                    input.Dispose ();
            }
        }

        static IBinaryStream OpenEncryptedStream (IBinaryStream file, int dataSize, string fileName)
        {
            var title = FormatCatalog.Instance.LookupGame (fileName);
            if (string.IsNullOrEmpty (title))
                title = FormatCatalog.Instance.LookupGame (fileName, @"..\*.exe");
            if (string.IsNullOrEmpty (title) || !DefaultScheme.KnownKeys.TryGetValue (title, out var password))
                throw new UnknownEncryptionScheme ();

            var data = file.ReadBytes (dataSize);
            if (data.Length != dataSize)
                throw new EndOfStreamException ();
            var key = InitDecryptionKey (password);
            for (int i = 0; i < data.Length; ++i)
                data[i] ^= key[i & 0x3FF];
            return new BinMemoryStream (data, file.Name);
        }

        static byte[] InitDecryptionKey (string password)
        {
            var bytes = Encodings.cp932.GetBytes (password);
            uint crc32 = Crc32.Compute (bytes, 0, bytes.Length);
            var table = new byte[0x400];
            for (int i = 0; i < 0x100; ++i)
            {
                uint value = crc32 ^ Crc32.Table[(i + crc32) & 0xFF];
                BitConverter.GetBytes (value).CopyTo (table, i * 4);
            }
            return table;
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotSupportedException ("RCT writing is not implemented.");
        }
    }

    internal sealed class RctReader : IDisposable
    {
        readonly IBinaryStream m_input;
        readonly int m_width;
        readonly byte[] m_data;

        internal byte[] Data { get { return m_data; } }

        internal RctReader (IBinaryStream input, RctMetaData info)
        {
            m_input = input;
            m_width = (int)info.Width;
            m_data = new byte[checked ((int)info.Width * (int)info.Height * 3)];
        }

        static readonly sbyte[] ShiftTable = new sbyte[] {
            -16, -32, -48, -64, -80, -96,
            49, 33, 17, 1, -15, -31, -47,
            50, 34, 18, 2, -14, -30, -46,
            51, 35, 19, 3, -13, -29, -45,
            36, 20, 4, -12, -28,
        };

        internal void Unpack ()
        {
            int pixelsRemaining = m_data.Length;
            int dataPos = 0;
            int eax = 0;
            while (pixelsRemaining > 0)
            {
                int count = eax * 3 + 3;
                if (count > pixelsRemaining || count != m_input.Read (m_data, dataPos, count))
                    throw new InvalidFormatException ();
                pixelsRemaining -= count;
                dataPos += count;

                while (pixelsRemaining > 0)
                {
                    eax = m_input.ReadByte ();
                    if (eax < 0)
                        throw new EndOfStreamException ();
                    if ((eax & 0x80) == 0)
                    {
                        if (eax == 0x7F)
                            eax += m_input.ReadUInt16 ();
                        break;
                    }
                    int shiftIndex = eax >> 2;
                    eax &= 3;
                    if (eax == 3)
                        eax += m_input.ReadUInt16 ();

                    count = eax * 3 + 3;
                    if (pixelsRemaining < count)
                        throw new InvalidFormatException ();
                    pixelsRemaining -= count;
                    int shift = ShiftTable[shiftIndex & 0x1F];
                    int shiftRow = shift & 0x0F;
                    shift >>= 4;
                    shiftRow *= m_width;
                    shift -= shiftRow;
                    shift *= 3;
                    if (shift >= 0 || dataPos + shift < 0)
                        throw new InvalidFormatException ();
                    Binary.CopyOverlapped (m_data, dataPos + shift, dataPos, count);
                    dataPos += count;
                }
            }
        }

        public void Dispose ()
        {
            GC.SuppressFinalize (this);
        }
    }
}
