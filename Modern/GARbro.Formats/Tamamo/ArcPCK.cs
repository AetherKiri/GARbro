using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using GameRes.Cryptography;
using ICSharpCode.SharpZipLib.BZip2;

namespace GameRes.Formats.Tamamo
{
    [Serializable]
    public class PckScheme : ResourceScheme
    {
        public Dictionary<string, byte[]> KnownKeys;
    }

    internal class PckArchive : ArcFile
    {
        public readonly Blowfish Encryption;

        public PckArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, byte[] key)
            : base (arc, impl, dir)
        {
            Encryption = new Blowfish (key);
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class PckOpener : ArchiveFormat
    {
        public override string Tag { get { return "PCK/TAMAMO"; } }
        public override string Description { get { return "TamamoSystem resource archive"; } }
        public override uint Signature { get { return 0x4B434150; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        public PckOpener ()
        {
            Extensions = new[] { "pck" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.View.AsciiEqual (4, "_FILE001"))
                return null;
            int count = file.View.ReadInt32 (0xC);
            if (!IsSaneCount (count))
                return null;
            var key = QueryKey (file.Name);
            if (key == null)
                return null;
            uint indexLength = file.View.ReadUInt32 (0x10);
            if (indexLength == 0 || indexLength > 16 * 1024 * 1024 || (indexLength & 7) != 0)
                return null;
            var index = file.View.ReadBytes (0x14, indexLength);
            if (index.Length != indexLength)
                return null;
            var bf = new Blowfish (key);
            bf.Decipher (index, index.Length);

            long dataOffset = 0x14 + indexLength;
            int pos = 0;
            var dir = new List<Entry> (count);
            for (int i = 0; i < count; ++i)
            {
                if (pos + 4 > index.Length)
                    return null;
                uint size = index.ToUInt32 (pos);
                pos += 4;
                int nameEnd = Array.IndexOf<byte> (index, 0, pos);
                if (nameEnd < 0)
                    return null;
                var name = Encodings.cp932.GetString (index, pos, nameEnd - pos);
                pos = nameEnd + 1;
                if (pos + 4 > index.Length)
                    return null;
                uint encryptedSize = index.ToUInt32 (pos);
                pos += 4;
                if (encryptedSize > file.MaxOffset - dataOffset - 1)
                    return null;
                var entry = FormatCatalog.Instance.Create<PackedEntry> (name);
                entry.Offset = dataOffset;
                entry.Size = encryptedSize;
                entry.UnpackedSize = size;
                entry.IsPacked = encryptedSize != size;
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
                dataOffset += encryptedSize + 1;
            }
            return new PckArchive (file, this, dir, key);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var parc = (PckArchive)arc;
            var pent = (PackedEntry)entry;
            byte dataType = arc.File.View.ReadByte (pent.Offset);
            Stream input = arc.File.CreateStream (pent.Offset + 1, pent.Size);
            if (dataType == 0)
                return input;
            input = new InputCryptoStream (input, parc.Encryption.CreateDecryptor ());
            if (dataType != 3)
                return new LimitStream (input, pent.UnpackedSize);
            return new BZip2InputStream (input);
        }

#if NET10_0_OR_GREATER
        PckScheme DefaultScheme = new PckScheme { KnownKeys = PckKeyDatabase.CreateSchemeKeys () };
#else
        PckScheme DefaultScheme = new PckScheme { KnownKeys = new Dictionary<string, byte[]> () };
#endif

        public Dictionary<string, byte[]> KnownKeys { get { return DefaultScheme.KnownKeys; } }

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { DefaultScheme = (PckScheme)value; }
        }

        byte[] QueryKey (string archiveName)
        {
            if (KnownKeys.Count == 1)
                return KnownKeys.Values.First ();
            var title = FormatCatalog.Instance.LookupGame (archiveName);
            if (!string.IsNullOrEmpty (title) && KnownKeys.TryGetValue (title, out var key))
                return key;
            return null;
        }
    }
}
