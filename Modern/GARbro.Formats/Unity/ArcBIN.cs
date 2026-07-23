using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.Unity
{
    internal sealed class BinArchive : ArcFile
    {
        public readonly Aes Encryption;

        public BinArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, Aes encryption)
            : base (arc, impl, dir)
        {
            Encryption = encryption;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
                Encryption.Dispose ();
            base.Dispose (disposing);
        }
    }

    [Serializable]
    public sealed class BinPackKey
    {
        public byte[] Key;
        public byte[] IV;
    }

    [Serializable]
    public sealed class BinPackScheme : ResourceScheme
    {
        public Dictionary<string, BinPackKey> KnownKeys;
    }

    [Export(typeof(ArchiveFormat))]
    public sealed class BinOpener : ArchiveFormat
    {
        public override string Tag { get { return "BIN/IDX"; } }
        public override string Description { get { return "Unity engine resource asset"; } }
        public override uint Signature { get { return 0; } }
        public override bool IsHierarchic { get { return true; } }
        public override bool CanWrite { get { return false; } }

        static readonly BinPackScheme DefaultScheme = new BinPackScheme {
            KnownKeys = BinIdxKeyDatabase.CreateSchemeKeys ()
        };

        public override ArcFile TryOpen (ArcView file)
        {
            if (!file.Name.HasExtension (".bin"))
                return null;
            var idxName = Path.ChangeExtension (file.Name, "idx");
            if (!VFS.FileExists (idxName))
                return null;
            var scheme = DefaultScheme.KnownKeys.Values.FirstOrDefault ();
            if (scheme == null)
                return null;

            var dir = new List<Entry> ();
            using (var idx = VFS.OpenBinaryStream (idxName))
            using (var aes = CreateCipher (scheme))
            {
                var buffer = new byte[0x100];
                var unpacker = new BinDeserializer ();
                while (idx.PeekByte () != -1)
                {
                    int length = idx.ReadInt32 ();
                    if (length <= 0)
                        return null;
                    if (length > buffer.Length)
                        buffer = new byte[length];
                    if (idx.Read (buffer, 0, length) < length)
                        return null;
                    using (var decryptor = aes.CreateDecryptor ())
                    using (var encrypted = new MemoryStream (buffer, 0, length, false))
                    using (var input = new InputCryptoStream (encrypted, decryptor))
                    using (var decrypted = new MemoryStream ())
                    {
                        input.CopyTo (decrypted);
                        decrypted.Position = 0;
                        var info = unpacker.DeserializeEntry (decrypted);
                        var filename = info["fileName"] as string;
                        if (string.IsNullOrEmpty (filename))
                            return null;
                        filename = filename.TrimStart ('/', '\\');
                        var entry = Create<Entry> (filename);
                        entry.Offset = Convert.ToInt64 (info["index"]);
                        entry.Size = Convert.ToUInt32 (info["size"]);
                        if (!entry.CheckPlacement (file.MaxOffset))
                            return null;
                        dir.Add (entry);
                    }
                }
            }
            if (dir.Count == 0)
                return null;
            return new BinArchive (file, this, dir, CreateCipher (scheme));
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var binArc = arc as BinArchive;
            if (binArc == null)
                return base.OpenEntry (arc, entry);
            return new InputCryptoStream (arc.File.CreateStream (entry.Offset, entry.Size),
                binArc.Encryption.CreateDecryptor ());
        }

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("BIN/IDX scheme is data-backed and read-only."); }
        }

        static Aes CreateCipher (BinPackKey scheme)
        {
            var aes = Aes.Create ();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = (byte[])scheme.Key.Clone ();
            aes.IV = (byte[])scheme.IV.Clone ();
            return aes;
        }
    }

    internal sealed class BinDeserializer
    {
        byte[] m_buffer = new byte[0x20];

        internal IDictionary DeserializeEntry (Stream input)
        {
            int id = input.ReadByte ();
            if (id < 0x80 || id > 0x8F)
                throw new FormatException ("Invalid BIN/IDX record header.");
            int fieldCount = id & 0xF;
            var map = new Hashtable (fieldCount);
            for (int i = 0; i < fieldCount; ++i)
            {
                id = input.ReadByte ();
                if (id < 0xA0 || id > 0xBF)
                    throw new FormatException ("Invalid BIN/IDX field header.");
                int length = id & 0x1F;
                ReadExactly (input, m_buffer, length);
                var key = Encoding.UTF8.GetString (m_buffer, 0, length);
                map[key] = ReadField (input);
            }
            return map;
        }

        object ReadField (Stream input)
        {
            int id = input.ReadByte ();
            if (id >= 0 && id < 0x80)
                return id;
            if (id >= 0xA0 && id < 0xC0)
                return ReadString (input, id & 0x1F);
            switch (id)
            {
            case 0xD0:
                return checked ((sbyte)ReadByte (input));
            case 0xD1:
                ReadExactly (input, m_buffer, 2);
                return BigEndian.ToInt16 (m_buffer, 0);
            case 0xD2:
                ReadExactly (input, m_buffer, 4);
                return BigEndian.ToInt32 (m_buffer, 0);
            case 0xDA:
                ReadExactly (input, m_buffer, 2);
                return ReadString (input, BigEndian.ToUInt16 (m_buffer, 0));
            default:
                throw new FormatException ();
            }
        }

        string ReadString (Stream input, int length)
        {
            if (length > m_buffer.Length)
                m_buffer = new byte[(length + 0xF) & ~0xF];
            ReadExactly (input, m_buffer, length);
            return Encoding.UTF8.GetString (m_buffer, 0, length);
        }

        static int ReadByte (Stream input)
        {
            int value = input.ReadByte ();
            if (value < 0)
                throw new FormatException ();
            return value;
        }

        static void ReadExactly (Stream input, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = input.Read (buffer, offset, count - offset);
                if (read <= 0)
                    throw new FormatException ();
                offset += read;
            }
        }
    }
}
