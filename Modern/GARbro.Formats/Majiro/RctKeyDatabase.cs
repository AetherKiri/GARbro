using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Majiro
{
    sealed class RctKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    static class RctKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, string>> s_keys =
            new Lazy<IReadOnlyDictionary<string, string>> (Load);

        internal static Dictionary<string, string> CreateSchemeKeys ()
        {
            return new Dictionary<string, string> (s_keys.Value, StringComparer.OrdinalIgnoreCase);
        }

        static IReadOnlyDictionary<string, string> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<RctKeyData> ("rct-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("RCT key dataset is invalid.");
            var keys = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || string.IsNullOrEmpty (item.Value))
                    throw new InvalidDataException ("RCT key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("RCT key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
