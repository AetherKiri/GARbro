using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.NScripter
{
    [Export(typeof(ArchiveFormat))]
    public class NsaOpener : ArchiveFormat
    {
        public override string Tag { get { return "NSA"; } }
        public override string Description { get { return "NScripter NSA archive"; } }
        public override uint Signature { get { return 0; } }
        public override bool IsHierarchic { get { return true; } }
        public override bool CanWrite { get { return false; } }

        public NsaOpener ()
        {
            Extensions = new[] { "nsa" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            bool zeroSignature = file.View.ReadInt16 (0) == 0;
            if (zeroSignature)
            {
                try
                {
                    using (var input = file.CreateStream (2))
                    {
                        var dir = ReadIndex (input);
                        if (dir != null)
                            return new ArcFile (file, this, dir);
                    }
                }
                catch
                {
                }
            }
            if (zeroSignature || !file.Name.HasExtension (".nsa"))
                return null;
            var password = QueryPassword (file.Name);
            if (string.IsNullOrEmpty (password))
                return null;
            var key = Encoding.ASCII.GetBytes (password);
            using (var input = new EncryptedViewStream (file, key))
            {
                var dir = ReadIndex (input);
                if (dir == null)
                    return null;
                return new NsaEncryptedArchive (file, this, dir, key);
            }
        }

        List<Entry> ReadIndex (Stream file)
        {
            long baseOffset = file.Position;
            using (var input = new ArcView.Reader (file))
            {
                int count = Binary.BigEndian (input.ReadInt16 ());
                if (!IsSaneCount (count))
                    return null;
                baseOffset += Binary.BigEndian (input.ReadUInt32 ());
                if (baseOffset >= file.Length || baseOffset < 15 * count)
                    return null;

                var dir = new List<Entry> (count);
                for (int i = 0; i < count; ++i)
                {
                    if (baseOffset - file.Position < 15)
                        return null;
                    var name = file.ReadCString ();
                    if (baseOffset - file.Position < 13 || name.Length == 0)
                        return null;
                    // Each NSA record starts with a compression tag followed by
                    // relative offset, packed size, and unpacked size.
                    var compressionType = input.ReadByte ();
                    if (compressionType != 0)
                        return null;
                    var entry = FormatCatalog.Instance.Create<Entry> (name);
                    entry.Offset = Binary.BigEndian (input.ReadUInt32 ()) + baseOffset;
                    entry.Size = Binary.BigEndian (input.ReadUInt32 ());
                    if (!entry.CheckPlacement (file.Length))
                        return null;
                    input.ReadUInt32 ();
                    dir.Add (entry);
                }
                return dir;
            }
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var encrypted = arc as NsaEncryptedArchive;
            if (encrypted == null)
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var input = new EncryptedViewStream (arc.File, encrypted.Key);
            return new StreamRegion (input, entry.Offset, entry.Size);
        }

#if NET10_0_OR_GREATER
        static readonly NsaScheme DefaultScheme = new NsaScheme { KnownKeys = NsaKeyDatabase.CreateSchemeKeys () };
#else
        static readonly NsaScheme DefaultScheme = new NsaScheme { KnownKeys = new Dictionary<string, string> () };
#endif

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("NSA scheme is data-backed and read-only."); }
        }

        string QueryPassword (string archiveName)
        {
            var title = FormatCatalog.Instance.LookupGame (archiveName);
            if (!string.IsNullOrEmpty (title) && DefaultScheme.KnownKeys.TryGetValue (title, out var password))
                return password;
            return null;
        }
    }
}
