using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.AZSys
{
    [Serializable]
    public sealed class AzEncryptedScheme : ResourceScheme
    {
        public Dictionary<string, AzEncryptedKeyRecord> KnownSchemes;
    }

    internal sealed class AzEncryptedArchive : ArcFile
    {
        public readonly uint ContentKey;

        public AzEncryptedArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, uint contentKey)
            : base (arc, impl, dir)
        {
            ContentKey = contentKey;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public sealed class ArcEncryptedOpener : ArchiveFormat
    {
        public override string Tag { get { return "ARC/AZ/encrypted"; } }
        public override string Description { get { return "AZ system encrypted resource archive"; } }
        public override uint Signature { get { return 0; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        static readonly AzEncryptedScheme DefaultScheme = new AzEncryptedScheme {
            KnownSchemes = AzEncryptedKeyDatabase.CreateSchemeKeys ()
        };

        public ArcEncryptedOpener ()
        {
            Extensions = new[] { "arc" };
            Signatures = new uint[] { 0x53EA06EB, 0x74F98F2F };
        }

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("ARC/AZ/encrypted scheme is data-backed and read-only."); }
        }

        public override ArcFile TryOpen (ArcView file)
        {
            var encryptedHeader = file.View.ReadBytes (0, 0x30);
            if (encryptedHeader.Length != 0x30)
                return null;
            foreach (var scheme in DefaultScheme.KnownSchemes.Values)
            {
                var header = (byte[])encryptedHeader.Clone ();
                Decrypt (header, 0, scheme.IndexKey);
                if (!Binary.AsciiEqual (header, 0, "ARC\0"))
                    continue;
                try
                {
                    var arc = ReadIndex (file, header, scheme);
                    if (arc != null)
                        return arc;
                }
                catch (InvalidFormatException)
                {
                }
            }
            return null;
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var encrypted = arc as AzEncryptedArchive;
            if (encrypted == null)
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var data = arc.File.View.ReadBytes (entry.Offset, entry.Size);
            if (data.Length != entry.Size)
                throw new InvalidFormatException ("Unexpected end of ARC/AZ/encrypted entry.");
            Decrypt (data, entry.Offset, encrypted.ContentKey);
            return new BinMemoryStream (data, entry.Name);
        }

        AzEncryptedArchive ReadIndex (ArcView file, byte[] header, AzEncryptedKeyRecord scheme)
        {
            int extensionCount = LittleEndian.ToInt32 (header, 4);
            int count = LittleEndian.ToInt32 (header, 8);
            uint indexLength = LittleEndian.ToUInt32 (header, 12);
            if (extensionCount < 1 || extensionCount > 8 || !IsSaneCount (count)
                || indexLength < 8 || indexLength > file.MaxOffset - 0x30)
                return null;
            var packedIndex = file.View.ReadBytes (0x30, indexLength);
            if (packedIndex.Length != indexLength)
                return null;
            Decrypt (packedIndex, 0x30, scheme.IndexKey);
            uint checksum = LittleEndian.ToUInt32 (packedIndex, 0);
            if (checksum != Adler32.Compute (packedIndex, 4, packedIndex.Length - 4)
                && checksum != Crc32.Compute (packedIndex, 4, packedIndex.Length - 4))
                return null;

            var dir = ParseIndex (packedIndex, count, 0x30L + indexLength, file.MaxOffset);
            if (dir == null || scheme.ContentKey == null)
                return null;
            return new AzEncryptedArchive (file, this, dir, scheme.ContentKey.Value);
        }

        static List<Entry> ParseIndex (byte[] packedIndex, int count, long baseOffset, long maxOffset)
        {
            var dir = new List<Entry> (count);
            using (var compressed = new MemoryStream (packedIndex, 4, packedIndex.Length - 4, false))
            using (var input = new ZLibStream (compressed, CompressionMode.Decompress))
            using (var reader = new BinaryReader (input, Encoding.ASCII, true))
            {
                var nameBuffer = new byte[0x20];
                for (int i = 0; i < count; ++i)
                {
                    uint offset = reader.ReadUInt32 ();
                    uint size = reader.ReadUInt32 ();
                    reader.ReadUInt32 ();
                    reader.ReadInt32 ();
                    if (reader.Read (nameBuffer, 0, nameBuffer.Length) != nameBuffer.Length)
                        return null;
                    var name = Binary.GetCString (nameBuffer, 0, nameBuffer.Length);
                    if (string.IsNullOrEmpty (name))
                        return null;
                    var entry = FormatCatalog.Instance.Create<Entry> (name);
                    entry.Offset = baseOffset + offset;
                    entry.Size = size;
                    if (!entry.CheckPlacement (maxOffset))
                        return null;
                    dir.Add (entry);
                }
            }
            return dir;
        }

        static void Decrypt (byte[] data, long offset, uint key)
        {
            ulong hash = key * 0x9E370001UL;
            if ((offset & 0x3F) != 0)
                hash = Binary.RotL (hash, (int)offset);
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] ^= (byte)hash;
                hash = Binary.RotL (hash, 1);
            }
        }
    }
}
