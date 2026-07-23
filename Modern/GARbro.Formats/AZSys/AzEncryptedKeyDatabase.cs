using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.AZSys
{
    sealed class AzEncryptedKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, AzEncryptedKeyRecord> KnownSchemes { get; set; }
    }

    public sealed class AzEncryptedKeyRecord
    {
        public uint IndexKey { get; set; }
        public uint? ContentKey { get; set; }
    }

    static class AzEncryptedKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, AzEncryptedKeyRecord>> s_keys =
            new Lazy<IReadOnlyDictionary<string, AzEncryptedKeyRecord>> (Load);

        internal static Dictionary<string, AzEncryptedKeyRecord> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, AzEncryptedKeyRecord> (StringComparer.Ordinal);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, new AzEncryptedKeyRecord {
                    IndexKey = item.Value.IndexKey,
                    ContentKey = item.Value.ContentKey,
                });
            return result;
        }

        static IReadOnlyDictionary<string, AzEncryptedKeyRecord> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<AzEncryptedKeyData> ("az-encrypted-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("ARC/AZ/encrypted key dataset is invalid.");
            var keys = new Dictionary<string, AzEncryptedKeyRecord> (StringComparer.Ordinal);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.IndexKey == 0
                    || !keys.TryAdd (item.Key, new AzEncryptedKeyRecord {
                        IndexKey = item.Value.IndexKey,
                        ContentKey = item.Value.ContentKey,
                    }))
                    throw new InvalidDataException ("ARC/AZ/encrypted key dataset contains an invalid or duplicate entry: " + item.Key);
            }
            return keys;
        }
    }
}
