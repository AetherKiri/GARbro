using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Crowd
{
    sealed class CrzKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    static class CrzKeyDatabase
    {
        const int SchemaVersion = 1;
        const int KeyLength = 0x24;
        static readonly Lazy<IReadOnlyDictionary<string, byte[]>> s_keys =
            new Lazy<IReadOnlyDictionary<string, byte[]>> (Load);

        internal static Dictionary<string, byte[]> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, byte[]> (StringComparer.Ordinal);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, (byte[])item.Value.Clone ());
            return result;
        }

        static IReadOnlyDictionary<string, byte[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<CrzKeyData> ("crz-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("CRZ key dataset is invalid.");
            var keys = new Dictionary<string, byte[]> (StringComparer.Ordinal);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Length != KeyLength)
                    throw new InvalidDataException ("CRZ key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, (byte[])item.Value.Clone ()))
                    throw new InvalidDataException ("CRZ key dataset contains a duplicate identifier: " + item.Key);
            }
            return keys;
        }
    }
}
