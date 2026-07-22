using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.TopCat
{
    sealed class TcdKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, int> KnownKeys { get; set; }
    }

    static class TcdKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, int>> s_keys =
            new Lazy<IReadOnlyDictionary<string, int>> (Load);

        internal static Dictionary<string, int> CreateSchemeKeys ()
        {
            return new Dictionary<string, int> (s_keys.Value, StringComparer.OrdinalIgnoreCase);
        }

        static IReadOnlyDictionary<string, int> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<TcdKeyData> ("tcd-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null)
                throw new InvalidDataException ("TCD key dataset is invalid.");

            var keys = new Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key))
                    throw new InvalidDataException ("TCD key dataset contains an incomplete entry.");
                int existing;
                if (!keys.TryGetValue (item.Key, out existing))
                    keys.Add (item.Key, item.Value);
                else if (existing != item.Value)
                    throw new InvalidDataException ("TCD key dataset contains a conflicting title: " + item.Key);
            }
            return keys;
        }
    }
}
