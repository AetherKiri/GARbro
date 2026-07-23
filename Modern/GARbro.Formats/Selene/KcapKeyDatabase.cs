using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Selene
{
    sealed class KcapKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, string> KnownSchemes { get; set; }
    }

    static class KcapKeyDatabase
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
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<KcapKeyData> ("kcap-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("KCAP key dataset is invalid.");

            var keys = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || string.IsNullOrWhiteSpace (item.Value))
                    throw new InvalidDataException ("KCAP key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("KCAP key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
