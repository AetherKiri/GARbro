using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Leaf
{
    sealed class LeafKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    static class LeafKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<LeafKeyData> s_data = new Lazy<LeafKeyData> (Load);

        internal static IDictionary<string, byte[]> CreateSchemes ()
        {
            var result = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
                result.Add (item.Key, (byte[])item.Value.Clone ());
            return result;
        }

        static LeafKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<LeafKeyData> ("leaf-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("Leaf key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Length == 0)
                    throw new InvalidDataException ("Leaf key dataset contains an invalid key.");
            }
            return data;
        }
    }
}
