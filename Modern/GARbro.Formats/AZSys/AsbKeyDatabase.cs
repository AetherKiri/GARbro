using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.AZSys
{
    sealed class AsbKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, uint> KnownKeys { get; set; }
    }

    static class AsbKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, uint>> s_keys =
            new Lazy<IReadOnlyDictionary<string, uint>> (Load);

        internal static Dictionary<string, uint> CreateSchemeKeys ()
        {
            return new Dictionary<string, uint> (s_keys.Value, StringComparer.Ordinal);
        }

        internal static bool TryGet (string title, out uint key)
        {
            if (string.IsNullOrWhiteSpace (title))
            {
                key = 0;
                return false;
            }
            return s_keys.Value.TryGetValue (title, out key);
        }

        static IReadOnlyDictionary<string, uint> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<AsbKeyData> ("asb-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("ARC/AZ key dataset is invalid.");
            var keys = new Dictionary<string, uint> (StringComparer.Ordinal);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == 0)
                    throw new InvalidDataException ("ARC/AZ key dataset contains an incomplete entry: " + item.Key);
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("ARC/AZ key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
