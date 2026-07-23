using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Compression;
using GameRes.Utility;

namespace GameRes.Formats.AZSys
{
    [Serializable]
    public sealed class AsbScheme : ResourceScheme
    {
        public Dictionary<string, uint> KnownKeys;
    }

    internal sealed class AsbArchive : ArcFile
    {
        public readonly uint Key;

        public AsbArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, uint key)
            : base (arc, impl, dir)
        {
            Key = key;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public sealed class ArcOpener : ArchiveFormat
    {
        public override string Tag { get { return "ARC/AZ"; } }
        public override string Description { get { return "AZ system resource archive"; } }
        public override uint Signature { get { return 0x1A435241; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        static readonly AsbScheme DefaultScheme = new AsbScheme {
            KnownKeys = AsbKeyDatabase.CreateSchemeKeys ()
        };

        public ArcOpener ()
        {
            Extensions = new string[] { "arc" };
        }

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("ARC/AZ scheme is data-backed and read-only."); }
        }

        public override ArcFile TryOpen (ArcView file)
        {
            int extCount = file.View.ReadInt32 (4);
            int count = file.View.ReadInt32 (8);
            uint indexLength = file.View.ReadUInt32 (12);
            if (extCount < 1 || extCount > 8 || count <= 0 || count > 0xFFFFF
                || indexLength <= 0x14 || indexLength >= file.MaxOffset)
                return null;
            var packedIndex = file.View.ReadBytes (0x30, indexLength);
            if (packedIndex.Length != indexLength)
                return null;
            uint baseOffset = 0x30 + indexLength;
            uint crc = LittleEndian.ToUInt32 (packedIndex, 0);
            if (crc != Crc32.Compute (packedIndex, 0x14, packedIndex.Length - 0x14))
                throw new InvalidFormatException ("ARC/AZ index CRC32 mismatch.");
            var index = new IndexReader (packedIndex, count).Unpack ();
            int indexOffset = 0;
            bool containsScripts = false;
            var dir = new List<Entry> (count);
            for (int i = 0; i < count; ++i)
            {
                var name = Binary.GetCString (index, indexOffset + 0x10, 0x30);
                if (name.Length > 0)
                {
                    var entry = FormatCatalog.Instance.Create<Entry> (name);
                    entry.Offset = baseOffset + LittleEndian.ToUInt32 (index, indexOffset);
                    entry.Size = LittleEndian.ToUInt32 (index, indexOffset + 4);
                    if (entry.CheckPlacement (file.MaxOffset))
                    {
                        dir.Add (entry);
                        containsScripts = containsScripts || name.HasExtension (".asb");
                    }
                }
                indexOffset += 0x40;
            }
            if (dir.Count == 0)
                return null;
            if (!containsScripts)
                return new ArcFile (file, this, dir);

            var title = FormatCatalog.Instance.LookupGame (file.Name);
            if (string.IsNullOrEmpty (title))
                title = FormatCatalog.Instance.LookupGame (file.Name, @"..\*.exe");
            if (!AsbKeyDatabase.TryGet (title, out var key))
                return new ArcFile (file, this, dir);
            return new AsbArchive (file, this, dir, key);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var asbArc = arc as AsbArchive;
            if (asbArc == null || entry.Size < 20
                || !arc.File.View.AsciiEqual (entry.Offset, "ASB\x1A"))
                return arc.File.CreateStream (entry.Offset, entry.Size);
            uint packed = arc.File.View.ReadUInt32 (entry.Offset + 4);
            uint unpacked = arc.File.View.ReadUInt32 (entry.Offset + 8);
            if (12 + packed != entry.Size)
                return arc.File.CreateStream (entry.Offset, entry.Size);

            uint key = asbArc.Key ^ unpacked;
            key ^= ((key << 12) | key) << 11;
            uint first = arc.File.View.ReadUInt16 (entry.Offset + 16);
            first = (first - key) & 0xFFFF;
            if (first != 0xDA78)
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var input = arc.File.View.ReadBytes (entry.Offset + 12, packed);
            unsafe
            {
                fixed (byte* raw = input)
                {
                    uint* encoded = (uint*)raw;
                    for (int i = 0; i < input.Length / 4; ++i)
                        encoded[i] -= key;
                }
            }
            uint checksum = LittleEndian.ToUInt32 (input, 0);
            if (checksum != Crc32.Compute (input, 4, input.Length - 4))
                return arc.File.CreateStream (entry.Offset, entry.Size);
            return new ZLibStream (new MemoryStream (input, 4, input.Length - 4), CompressionMode.Decompress);
        }
    }

    internal sealed class IndexReader
    {
        readonly byte[] m_input;
        readonly byte[] m_output;
        readonly int m_controlLength;
        readonly int m_compressed1Length;
        readonly int m_compressed2Length;

        internal IndexReader (byte[] packed, int count)
        {
            m_input = packed;
            m_output = new byte[checked (count * 0x40)];
            m_controlLength = LittleEndian.ToInt32 (packed, 4);
            m_compressed1Length = LittleEndian.ToInt32 (packed, 8);
            m_compressed2Length = LittleEndian.ToInt32 (packed, 12);
            int outputLength = LittleEndian.ToInt32 (packed, 0x10);
            if (m_controlLength < 0 || m_compressed1Length < 0 || m_compressed2Length < 0
                || outputLength != m_output.Length
                || 0x14L + m_controlLength + m_compressed1Length + m_compressed2Length > packed.Length)
                throw new InvalidFormatException ("Invalid ARC/AZ compressed index.");
        }

        internal byte[] Unpack ()
        {
            int control = 0x14;
            int compressed1 = control + m_controlLength;
            int compressed2 = compressed1 + m_compressed1Length;
            int end = compressed2 + m_compressed2Length;
            int dst = 0;
            byte mask = 0x80;
            int compressed1End = compressed1 + m_compressed1Length;
            while (dst < m_output.Length)
            {
                int copyCount;
                if (control >= 0x14 + m_controlLength)
                    throw new InvalidFormatException ("Truncated ARC/AZ index control stream.");
                if ((m_input[control] & mask) != 0)
                {
                    if (compressed1 + 2 > compressed1End)
                        throw new InvalidFormatException ("Truncated ARC/AZ index back-reference.");
                    int offset = LittleEndian.ToUInt16 (m_input, compressed1);
                    compressed1 += 2;
                    copyCount = (offset >> 13) + 3;
                    offset = (offset & 0x1FFF) + 1;
                    if (offset > dst || dst + copyCount > m_output.Length)
                        throw new InvalidFormatException ("Invalid ARC/AZ index back-reference.");
                    Binary.CopyOverlapped (m_output, dst - offset, dst, copyCount);
                    dst += copyCount;
                }
                else
                {
                    if (compressed2 >= end)
                        throw new InvalidFormatException ("Truncated ARC/AZ index literal length.");
                    copyCount = m_input[compressed2++] + 1;
                    if (compressed2 + copyCount > end || dst + copyCount > m_output.Length)
                        throw new InvalidFormatException ("Invalid ARC/AZ index literal.");
                    Buffer.BlockCopy (m_input, compressed2, m_output, dst, copyCount);
                    compressed2 += copyCount;
                    dst += copyCount;
                }
                mask >>= 1;
                if (mask == 0)
                {
                    ++control;
                    mask = 0x80;
                    if (control >= 0x14 + m_controlLength && dst < m_output.Length)
                        throw new InvalidFormatException ("Truncated ARC/AZ index control stream.");
                }
            }
            if (dst != m_output.Length)
                throw new InvalidFormatException ("ARC/AZ index was not fully decoded.");
            return m_output;
        }
    }
}
