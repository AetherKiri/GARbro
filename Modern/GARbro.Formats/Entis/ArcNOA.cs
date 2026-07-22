using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.Entis
{
    internal sealed class NoaEntry : Entry
    {
        public byte[] Extra;
        public uint Encryption;
        public uint Attr;
    }

    internal sealed class NoaArchive : ArcFile
    {
        public readonly string Password;

        public NoaArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, string password)
            : base (arc, impl, dir)
        {
            Password = password;
        }
    }

    [Serializable]
    public class NoaScheme : ResourceScheme
    {
        public Dictionary<string, Dictionary<string, string>> KnownKeys;
    }

    [Export(typeof(ArchiveFormat))]
    public class NoaOpener : ArchiveFormat
    {
        public override string Tag { get { return "NOA"; } }
        public override string Description { get { return "Entis GLS engine resource archive"; } }
        public override uint Signature { get { return 0x69746E45; } }
        public override bool IsHierarchic { get { return true; } }
        public override bool CanWrite { get { return false; } }

        public NoaOpener ()
        {
            Extensions = new[] { "noa", "dat", "rsa", "arc", "emc" };
            Signatures = new[] { 0x69746E45u, 0x54534956u };
            ContainedFormats = new[] { "ERI", "EMI", "MIO", "EMS", "TXT" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.View.AsciiEqual (0, "Entis\x1a") && !file.View.AsciiEqual (0, "VIST\x1a"))
                return null;
            if (file.View.ReadUInt32 (8) != 0x02000400)
                return null;
            var reader = new IndexReader (file, Encodings.cp932);
            if (!reader.ParseRoot () || reader.Dir.Count == 0)
                return null;
            if (!reader.HasEncrypted)
                return new ArcFile (file, this, reader.Dir);

            var title = FormatCatalog.Instance.LookupGame (file.Name, @"..\*.exe");
            var archiveName = Path.GetFileName (file.Name).ToLowerInvariant ();
            if (!string.IsNullOrEmpty (title) && DefaultScheme.KnownKeys.TryGetValue (title, out var files)
                && files.TryGetValue (archiveName, out var password) && !string.IsNullOrEmpty (password))
                return new NoaArchive (file, this, reader.Dir, password);
            return new ArcFile (file, this, reader.Dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var nent = entry as NoaEntry;
            if (nent == null)
                return arc.File.CreateStream (entry.Offset, entry.Size);
            ulong size = arc.File.View.ReadUInt64 (entry.Offset + 8);
            if (size > int.MaxValue)
                throw new FileSizeException ();
            if (size <= 4)
                return Stream.Null;

            var inputStream = arc.File.CreateStream (entry.Offset + 0x10, (uint)size);
            var narc = arc as NoaArchive;
            if (nent.Encryption == EncType.Raw || narc == null || string.IsNullOrEmpty (narc.Password))
                return inputStream;
            if (nent.Encryption == EncType.BSHFCrypt)
            {
                using (inputStream)
                    return DecodeBSHF (inputStream, narc.Password);
            }
            Trace.WriteLine ("Unsupported NOA encryption: 0x" + nent.Encryption.ToString ("x8"));
            return inputStream;
        }

        static Stream DecodeBSHF (Stream input, string password)
        {
            uint outputLength = (uint)input.Length - 4;
            var decoder = new BSHFDecodeContext (0x10000);
            decoder.AttachInputFile (input);
            decoder.PrepareToDecodeBSHFCode (password);
            var output = new byte[outputLength];
            if (decoder.DecodeBSHFCodeBytes (output, outputLength) < outputLength)
                throw new EndOfStreamException ("Unexpected end of NOA encrypted stream.");
            return new MemoryStream (output, false);
        }

        static readonly NoaScheme DefaultScheme = new NoaScheme {
            KnownKeys = NoaKeyDatabase.CreateSchemeKeys ()
        };

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("NOA scheme is data-backed and read-only."); }
        }

        internal static class EncType
        {
            public const uint Raw = 0;
            public const uint ERISACode = 0x80000010;
            public const uint BSHFCrypt = 0x40000000;
        }

        sealed class IndexReader
        {
            readonly ArcView m_file;
            readonly List<Entry> m_dir = new List<Entry> ();
            readonly Encoding m_encoding;
            bool m_foundEncrypted;

            public IList<Entry> Dir { get { return m_dir; } }
            public bool HasEncrypted { get { return m_foundEncrypted; } }

            public IndexReader (ArcView file, Encoding encoding)
            {
                m_file = file;
                m_encoding = encoding;
            }

            public bool ParseRoot ()
            {
                return ParseDirEntry (0x40, "");
            }

            bool ParseDirEntry (long dirOffset, string currentDirectory)
            {
                if (!m_file.View.AsciiEqual (dirOffset, "DirEntry"))
                    return false;
                long size = m_file.View.ReadInt64 (dirOffset + 8);
                if (size <= 0 || size > int.MaxValue || (uint)size > m_file.View.Reserve (dirOffset + 8, (uint)size))
                    return false;
                long baseOffset = dirOffset;
                dirOffset += 0x10;
                int count = m_file.View.ReadInt32 (dirOffset);
                if (!ArchiveFormat.IsSaneCount (count))
                    return false;
                dirOffset += 4;
                for (int i = 0; i < count; ++i)
                {
                    var entry = new NoaEntry {
                        Size = m_file.View.ReadUInt32 (dirOffset),
                    };
                    dirOffset += 8;
                    entry.Attr = m_file.View.ReadUInt32 (dirOffset);
                    dirOffset += 4;
                    entry.Encryption = m_file.View.ReadUInt32 (dirOffset);
                    m_foundEncrypted = m_foundEncrypted || (entry.Encryption != EncType.Raw && entry.Encryption != EncType.ERISACode);
                    bool packed = entry.Encryption == EncType.ERISACode;
                    dirOffset += 4;
                    entry.Offset = baseOffset + m_file.View.ReadInt64 (dirOffset);
                    if (!packed && !entry.CheckPlacement (m_file.MaxOffset))
                        entry.Size = (uint)Math.Max (0, m_file.MaxOffset - entry.Offset);
                    dirOffset += 0x10;
                    uint extraLength = m_file.View.ReadUInt32 (dirOffset);
                    dirOffset += 4;
                    if (extraLength > 0 && (entry.Attr & 0x70) == 0)
                    {
                        entry.Extra = m_file.View.ReadBytes (dirOffset, extraLength);
                        if (entry.Extra.Length != extraLength)
                            return false;
                    }
                    dirOffset += extraLength;
                    uint nameLength = m_file.View.ReadUInt32 (dirOffset);
                    dirOffset += 4;
                    var name = m_file.View.ReadString (dirOffset, nameLength, m_encoding);
                    dirOffset += nameLength;
                    entry.Name = string.IsNullOrEmpty (currentDirectory) ? name : currentDirectory + "/" + name;
                    entry.Type = FormatCatalog.Instance.GetTypeFromName (name);
                    if (entry.Attr == 0x10)
                    {
                        if (!ParseDirEntry (entry.Offset + 0x10, entry.Name))
                            return false;
                    }
                    else if (entry.Attr == 0x20 || entry.Attr == 0x40)
                        break;
                    else
                        m_dir.Add (entry);
                }
                return true;
            }
        }
    }

    abstract class NoaBitDecodeContext
    {
        protected int m_intCount;
        protected uint m_intBuffer;
        protected readonly uint m_bufferSize;
        protected uint m_bufferCount;
        protected readonly byte[] m_buffer;
        protected int m_next;
        protected Stream m_input;

        protected NoaBitDecodeContext (uint bufferSize)
        {
            m_bufferSize = (bufferSize + 3) & ~3u;
            m_buffer = new byte[bufferSize];
        }

        public void AttachInputFile (Stream input) { m_input = input; }

        protected bool PrefetchBuffer ()
        {
            if (m_intCount == 0)
            {
                if (m_bufferCount == 0)
                {
                    m_next = 0;
                    m_bufferCount = (uint)m_input.Read (m_buffer, 0, (int)m_bufferSize);
                    if (m_bufferCount == 0)
                        return false;
                    if ((m_bufferCount & 3) != 0)
                        m_bufferCount = (m_bufferCount + 3) & ~3u;
                }
                m_intCount = 32;
                m_intBuffer = ((uint)m_buffer[m_next] << 24) | ((uint)m_buffer[m_next + 1] << 16)
                    | ((uint)m_buffer[m_next + 2] << 8) | m_buffer[m_next + 3];
                m_next += 4;
                m_bufferCount -= 4;
            }
            return true;
        }

        protected int GetBit ()
        {
            if (!PrefetchBuffer ())
                return 1;
            int value = (int)m_intBuffer >> 31;
            --m_intCount;
            m_intBuffer <<= 1;
            return value;
        }
    }

    sealed class BSHFDecodeContext : NoaBitDecodeContext
    {
        byte[] m_password = new byte[32];
        readonly byte[] m_output = new byte[32];
        readonly byte[] m_source = new byte[32];
        readonly byte[] m_mask = new byte[32];
        uint m_outputPosition = 32;
        uint m_passwordPosition;

        public BSHFDecodeContext (uint bufferSize) : base (bufferSize) { }

        public void PrepareToDecodeBSHFCode (string password)
        {
            var bytes = Encoding.ASCII.GetBytes (password ?? " ");
            int length = Math.Max (bytes.Length, 32);
            m_password = new byte[length];
            Array.Copy (bytes, m_password, bytes.Length);
            if (bytes.Length < 32)
            {
                int count = bytes.Length;
                m_password[count++] = 0x1B;
                for (int i = count; i < 32; ++i)
                    m_password[i] = (byte)(m_password[i % count] + m_password[i - 1]);
            }
            m_passwordPosition = 0;
            m_outputPosition = 32;
        }

        public uint DecodeBSHFCodeBytes (byte[] output, uint count)
        {
            uint decoded = 0;
            while (decoded < count)
            {
                if (m_outputPosition >= 32)
                {
                    for (int i = 0; i < 32; ++i)
                    {
                        if (m_bufferCount == 0)
                        {
                            m_next = 0;
                            m_bufferCount = (uint)m_input.Read (m_buffer, 0, (int)m_bufferSize);
                            if (m_bufferCount == 0)
                                return decoded;
                        }
                        m_source[i] = m_buffer[m_next++];
                        --m_bufferCount;
                    }
                    DecodeBuffer ();
                    m_outputPosition = 0;
                }
                output[decoded++] = m_output[m_outputPosition++];
            }
            return decoded;
        }

        void DecodeBuffer ()
        {
            Array.Clear (m_output, 0, m_output.Length);
            Array.Clear (m_mask, 0, m_mask.Length);
            int passLength = m_password.Length;
            if (m_passwordPosition >= passLength)
                m_passwordPosition = 0;
            int pass = (int)m_passwordPosition++;
            int bit = 0;
            for (int i = 0; i < 256; ++i)
            {
                bit = (bit + m_password[pass++]) & 0xFF;
                if (pass >= passLength)
                    pass = 0;
                int offset = bit >> 3;
                int mask = 0x80 >> (bit & 7);
                while (m_mask[offset] == 0xFF)
                {
                    bit = (bit + 8) & 0xFF;
                    offset = bit >> 3;
                }
                while ((m_mask[offset] & mask) != 0)
                {
                    ++bit;
                    mask >>= 1;
                    if (mask == 0)
                    {
                        bit = (bit + 8) & 0xFF;
                        offset = bit >> 3;
                        mask = 0x80;
                    }
                }
                m_mask[offset] |= (byte)mask;
                if ((m_source[i >> 3] & (0x80 >> (i & 7))) != 0)
                    m_output[offset] |= (byte)mask;
            }
        }
    }
}
