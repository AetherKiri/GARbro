using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using GameRes.Cryptography;
using GameRes.Utility;

namespace GameRes.Formats.CatSystem
{
    [Serializable]
    public class IntScheme : ResourceScheme
    {
        public Dictionary<string, IntKeyData> KnownKeys;
    }

    internal sealed class FrontwingArchive : ArcFile
    {
        public readonly Blowfish Encryption;

        public FrontwingArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, Blowfish encryption)
            : base (arc, impl, dir)
        {
            Encryption = encryption;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class IntOpener : ArchiveFormat
    {
        public override string Tag { get { return "INT"; } }
        public override string Description { get { return "Frontwing resource archive"; } }
        public override uint Signature { get { return 0x0046494B; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        static readonly byte[] NameSizes = { 0x20, 0x40 };

        public IntOpener ()
        {
            Extensions = new[] { "int" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            int count = file.View.ReadInt32 (4);
            if (!IsSaneCount (count))
                return null;
            if (file.View.AsciiEqual (8, "__key__.dat\0"))
            {
                var title = FormatCatalog.Instance.LookupGame (file.Name);
                if (string.IsNullOrEmpty (title) || !DefaultScheme.KnownKeys.TryGetValue (title, out var keyData))
                    return null;
                return OpenEncrypted (file, count, keyData.Key);
            }

            foreach (var nameLength in NameSizes)
            {
                var dir = ReadPlainIndex (file, count, nameLength);
                if (dir != null)
                    return new ArcFile (file, this, dir);
            }
            return null;
        }

        List<Entry> ReadPlainIndex (ArcView file, int count, uint nameLength)
        {
            var dir = new List<Entry> (count);
            long current = 8;
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    var name = file.View.ReadString (current, nameLength);
                    if (string.IsNullOrEmpty (name))
                        return null;
                    current += nameLength;
                    var entry = FormatCatalog.Instance.Create<Entry> (name);
                    entry.Offset = file.View.ReadUInt32 (current);
                    entry.Size = file.View.ReadUInt32 (current + 4);
                    if (entry.Offset <= current || !entry.CheckPlacement (file.MaxOffset))
                        return null;
                    dir.Add (entry);
                    current += 8;
                }
                return dir;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        ArcFile OpenEncrypted (ArcView file, int count, uint mainKey)
        {
            if (count <= 1 || file.MaxOffset < 0x50 + (long)(count - 1) * 0x48)
                return null;
            uint seed = file.View.ReadUInt32 (0x4C);
            var twister = new MersenneTwister (seed);
            var blowfishKey = BitConverter.GetBytes (twister.Rand ());
            var blowfish = new Blowfish (blowfishKey);
            var dir = new List<Entry> (count - 1);
            var nameBuffer = new byte[0x40];
            long current = 8;
            for (int i = 1; i < count; ++i)
            {
                current += 0x48;
                if (file.View.Read (current, nameBuffer, 0, 0x40) != 0x40)
                    return null;
                uint offset = file.View.ReadUInt32 (current + 0x40) + (uint)i;
                uint size = file.View.ReadUInt32 (current + 0x44);
                blowfish.Decipher (ref offset, ref size);
                twister.SRand (mainKey + (uint)i);
                var name = DecipherName (nameBuffer, twister.Rand ());
                var entry = FormatCatalog.Instance.Create<Entry> (name);
                entry.Offset = offset;
                entry.Size = size;
                if (string.IsNullOrEmpty (name) || !entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
            }
            return new FrontwingArchive (file, this, dir, blowfish);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var encrypted = arc as FrontwingArchive;
            if (encrypted == null)
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var data = arc.File.View.ReadBytes (entry.Offset, entry.Size);
            encrypted.Encryption.Decipher (data, data.Length / 8 * 8);
            return new BinMemoryStream (data, entry.Name);
        }

        static string DecipherName (byte[] name, uint key)
        {
            const string alphabet = "zyxwvutsrqponmlkjihgfedcbaZYXWVUTSRQPONMLKJIHGFEDCBA";
            int shift = (byte)((key >> 24) + (key >> 16) + (key >> 8) + key);
            int i;
            for (i = 0; i < name.Length && name[i] != 0; ++i)
            {
                int index = alphabet.IndexOf ((char)name[i]);
                if (index != -1)
                {
                    index -= shift % alphabet.Length;
                    if (index < 0)
                        index += alphabet.Length;
                    name[i] = (byte)alphabet[alphabet.Length - 1 - index];
                }
                ++shift;
            }
            return Encodings.cp932.GetString (name, 0, i);
        }

#if NET10_0_OR_GREATER
        static readonly IntScheme DefaultScheme = new IntScheme {
            KnownKeys = IntKeyDatabase.CreateSchemeKeys ()
        };
#else
        static readonly IntScheme DefaultScheme = new IntScheme {
            KnownKeys = new Dictionary<string, IntKeyData> ()
        };
#endif

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("INT scheme is data-backed and read-only."); }
        }
    }
}
