using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace GameRes.Formats.Marble
{
    public class MblArchive : ArcFile
    {
        public readonly byte[] Key;

        public MblArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, string password)
            : base (arc, impl, dir)
        {
            Key = Encodings.cp932.GetBytes (password);
        }
    }

    [Serializable]
    public class MblScheme : ResourceScheme
    {
        public Dictionary<string, string> KnownKeys;
    }

    [Export(typeof(ArchiveFormat))]
    public class MblOpener : ArchiveFormat
    {
        public override string Tag { get { return "MBL"; } }
        public override string Description { get { return "Marble engine resource archive"; } }
        public override uint Signature { get { return 0; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        public MblOpener ()
        {
            Extensions = new[] { "mbl", "dns" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            int count = file.View.ReadInt32 (0);
            if (!IsSaneCount (count))
                return null;
            uint filenameLength = file.View.ReadUInt32 (4);
            ArcFile arc = null;
            if (filenameLength > 0 && filenameLength <= 0xFF)
                arc = ReadIndex (file, count, filenameLength, 8);
            if (arc == null)
                arc = ReadIndex (file, count, 0x10, 4);
            if (arc == null)
                arc = ReadIndex (file, count, 0x38, 4);
            return arc;
        }

        ArcFile ReadIndex (ArcView file, int count, uint filenameLength, uint indexOffset)
        {
            uint indexSize = (8u + filenameLength) * (uint)count;
            if (indexSize > file.View.Reserve (indexOffset, indexSize))
                return null;
            try
            {
                bool containsScripts = Path.GetFileNameWithoutExtension (file.Name)
                    .EndsWith ("_data", StringComparison.OrdinalIgnoreCase);
                var dir = new List<Entry> (count);
                for (int i = 0; i < count; ++i)
                {
                    string name = file.View.ReadString (indexOffset, filenameLength);
                    if (name.Length == 0)
                        break;
                    if (filenameLength - name.Length > 1)
                    {
                        string extension = file.View.ReadString (indexOffset + (uint)name.Length + 1,
                            filenameLength - (uint)name.Length - 1);
                        if (extension.Length != 0)
                            name = Path.ChangeExtension (name, extension);
                    }
                    name = name.ToLowerInvariant ();
                    indexOffset += filenameLength;
                    uint offset = file.View.ReadUInt32 (indexOffset);
                    if (containsScripts || name.EndsWith (".s", StringComparison.OrdinalIgnoreCase))
                    {
                        containsScripts = true;
                    }
                    var entry = new Entry { Name = name };
                    entry.Offset = offset;
                    entry.Size = file.View.ReadUInt32 (indexOffset + 4);
                    if (offset < indexSize || !entry.CheckPlacement (file.MaxOffset))
                        return null;
                    if (name.EndsWith (".s", StringComparison.OrdinalIgnoreCase))
                        entry.Type = "script";
                    else
                        entry.Type = FormatCatalog.Instance.GetTypeFromName (name);
                    dir.Add (entry);
                    indexOffset += 8;
                }
                if (dir.Count == 0 || (dir.Count == 1 && count > 1))
                    return null;
                string password = containsScripts ? QueryPassPhrase (file.Name) : null;
                return string.IsNullOrEmpty (password)
                    ? new ArcFile (file, this, dir)
                    : new MblArchive (file, this, dir, password);
            }
            catch
            {
                return null;
            }
        }

#if NET10_0_OR_GREATER
        static MblScheme DefaultScheme = new MblScheme { KnownKeys = MblKeyDatabase.CreateSchemeKeys () };
#else
        static MblScheme DefaultScheme = new MblScheme { KnownKeys = new Dictionary<string, string> () };
#endif

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { DefaultScheme = (MblScheme)value; }
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            if (!string.Equals (entry.Type, "script", StringComparison.Ordinal))
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var data = arc.File.View.ReadBytes (entry.Offset, entry.Size);
            var marc = arc as MblArchive;
            if (marc == null || marc.Key == null)
            {
                for (int i = 0; i < data.Length; ++i)
                    data[i] = (byte)-data[i];
            }
            else if (marc.Key.Length > 0)
            {
                for (int i = 0; i < data.Length; ++i)
                    data[i] ^= marc.Key[i % marc.Key.Length];
            }
            return new BinMemoryStream (data, entry.Name);
        }

        string QueryPassPhrase (string archiveName)
        {
            var title = FormatCatalog.Instance.LookupGame (archiveName);
            if (!string.IsNullOrEmpty (title) && DefaultScheme.KnownKeys.TryGetValue (title, out var password))
                return password;
            return null;
        }
    }
}
