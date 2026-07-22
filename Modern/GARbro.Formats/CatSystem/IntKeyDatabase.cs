using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.CatSystem
{
    public sealed class IntKeyData
    {
        public uint Key { get; set; }
        public string Passphrase { get; set; }
    }

    static class IntKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, IntKeyData>> s_keys =
            new Lazy<IReadOnlyDictionary<string, IntKeyData>> (Load);

        internal static Dictionary<string, IntKeyData> CreateSchemeKeys ()
        {
            return new Dictionary<string, IntKeyData> (s_keys.Value, StringComparer.OrdinalIgnoreCase);
        }

        static IReadOnlyDictionary<string, IntKeyData> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<IntKeyDocument> ("int-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("INT key dataset is invalid.");

            var keys = new Dictionary<string, IntKeyData> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || string.IsNullOrEmpty (item.Value.Passphrase))
                    throw new InvalidDataException ("INT key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("INT key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }

    sealed class IntKeyDocument
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, IntKeyData> KnownKeys { get; set; }
    }
}
