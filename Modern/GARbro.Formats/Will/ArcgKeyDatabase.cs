using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Will
{
    sealed class ArcgKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<uint, string> KnownKeys { get; set; }
    }

    static class ArcgKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<uint, string>> s_keys =
            new Lazy<IReadOnlyDictionary<uint, string>> (Load);

        internal static Dictionary<uint, string> CreateSchemeKeys ()
        {
            return new Dictionary<uint, string> (s_keys.Value);
        }

        static IReadOnlyDictionary<uint, string> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<ArcgKeyData> ("arcg-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("ARCG key dataset is invalid.");
            var keys = new Dictionary<uint, string> ();
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrEmpty (item.Value))
                    throw new InvalidDataException ("ARCG key dataset contains an empty passkey.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("ARCG key dataset contains a duplicate signature: " + item.Key);
            }
            return keys;
        }
    }
}
