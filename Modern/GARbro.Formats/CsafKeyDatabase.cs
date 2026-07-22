using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.FamilyAdvSystem
{
    sealed class CsafKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    static class CsafKeyDatabase
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
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<CsafKeyData> ("csaf-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("CSAF key dataset is invalid.");

            var keys = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || string.IsNullOrWhiteSpace (item.Value))
                    throw new InvalidDataException ("CSAF key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("CSAF key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
