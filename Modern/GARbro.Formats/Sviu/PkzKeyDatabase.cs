using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Sviu
{
    sealed class PkzKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    static class PkzKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, byte[]>> s_keys =
            new Lazy<IReadOnlyDictionary<string, byte[]>> (Load);

        internal static Dictionary<string, byte[]> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, (byte[])item.Value.Clone ());
            return result;
        }

        static IReadOnlyDictionary<string, byte[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<PkzKeyData> ("pkz-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("PKZ key dataset is invalid.");

            var keys = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || item.Value.Length == 0 || item.Value.Length > 256)
                    throw new InvalidDataException ("PKZ key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("PKZ key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
