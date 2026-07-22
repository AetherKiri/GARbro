using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Formats.Nrbf;

namespace GARbro.LegacyDataMigration
{
    internal sealed class LegacyFormatsDatabase
    {
        internal int Version { get; set; }
        internal ClassRecord Root { get; set; }
        internal IReadOnlyDictionary<SerializationRecordId, SerializationRecord> Records { get; set; }
    }

    internal static class LegacyFormatsReader
    {
        const string DatabaseId = "GARbroDB";

        internal static LegacyFormatsDatabase Read (string path)
        {
            if (!File.Exists (path))
                throw new FileNotFoundException ("Database file was not found.", path);

            using (var file = File.OpenRead (path))
            using (var reader = new BinaryReader (file, System.Text.Encoding.UTF8, true))
            {
                var id = new string (reader.ReadChars (DatabaseId.Length));
                if (!string.Equals (id, DatabaseId, StringComparison.Ordinal))
                    throw new InvalidDataException ("The input is not a GARbro Formats.dat file.");
                var version = reader.ReadInt32();

                using (var payload = new ZLibStream (file, CompressionMode.Decompress, false))
                {
                    var root = NrbfDecoder.Decode (payload, out var records, new PayloadOptions(), false) as ClassRecord;
                    if (root == null)
                        throw new InvalidDataException ("The legacy database root is not a class record.");
                    if (!string.Equals (root.TypeName.FullName, "GameRes.SchemeDataBase", StringComparison.Ordinal))
                        throw new InvalidDataException ("The legacy database root type is not supported: " + root.TypeName.FullName);
                    return new LegacyFormatsDatabase {
                        Version = version,
                        Root = root,
                        Records = records,
                    };
                }
            }
        }
    }
}
