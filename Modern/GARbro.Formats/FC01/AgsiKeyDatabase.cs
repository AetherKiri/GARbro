using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.FC01
{
    sealed class AgsiKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, Dictionary<string, byte[]>> KnownSchemes { get; set; }
    }

    static class AgsiKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<AgsiKeyData> s_data = new Lazy<AgsiKeyData> (Load);

        internal static IDictionary<string, IDictionary<string, byte[]>> CreateSchemes ()
        {
            var result = new Dictionary<string, IDictionary<string, byte[]>> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var archives = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
                foreach (var archive in item.Value)
                    archives.Add (archive.Key, (byte[])archive.Value.Clone ());
                result.Add (item.Key, archives);
            }
            return result;
        }

        static AgsiKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<AgsiKeyData> ("agsi-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("AGSI key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Count == 0)
                    throw new InvalidDataException ("AGSI key dataset contains an invalid title map.");
                foreach (var archive in item.Value)
                {
                    if (string.IsNullOrWhiteSpace (archive.Key) || archive.Value == null || archive.Value.Length != 8)
                        throw new InvalidDataException ("AGSI key dataset contains an invalid DES key.");
                }
            }
            return data;
        }
    }
}
