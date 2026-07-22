using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.FC01
{
    sealed class McgKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte> KnownKeys { get; set; }
    }

    static class McgKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, byte>> s_keys =
            new Lazy<IReadOnlyDictionary<string, byte>> (Load);

        internal static Dictionary<string, byte> CreateSchemeKeys ()
        {
            return new Dictionary<string, byte> (s_keys.Value, StringComparer.OrdinalIgnoreCase);
        }

        static IReadOnlyDictionary<string, byte> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<McgKeyData> ("mcg-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("MCG key dataset is invalid.");
            var keys = new Dictionary<string, byte> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key))
                    throw new InvalidDataException ("MCG key dataset contains an empty title.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("MCG key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
