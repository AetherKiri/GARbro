using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Entis
{
    sealed class NoaKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, Dictionary<string, string>> KnownKeys { get; set; }
    }

    static class NoaKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> s_keys =
            new Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> (Load);

        internal static Dictionary<string, Dictionary<string, string>> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, Dictionary<string, string>> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result[item.Key] = new Dictionary<string, string> (item.Value, StringComparer.OrdinalIgnoreCase);
            return result;
        }

        static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<NoaKeyData> ("noa-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("NOA key dataset is invalid.");

            var keys = new Dictionary<string, IReadOnlyDictionary<string, string>> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Count == 0)
                    throw new InvalidDataException ("NOA key dataset contains an incomplete title entry.");
                var files = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
                foreach (var file in item.Value)
                {
                    if (string.IsNullOrWhiteSpace (file.Key) || string.IsNullOrEmpty (file.Value))
                        throw new InvalidDataException ("NOA key dataset contains an incomplete archive entry.");
                    if (!files.TryAdd (file.Key, file.Value))
                        throw new InvalidDataException ("NOA key dataset contains a duplicate archive: " + file.Key);
                }
                if (!keys.TryAdd (item.Key, files))
                    throw new InvalidDataException ("NOA key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
