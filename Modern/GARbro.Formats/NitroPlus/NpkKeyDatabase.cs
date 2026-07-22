using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.NitroPlus
{
    sealed class NpkKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    static class NpkKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, byte[]>> s_keys =
            new Lazy<IReadOnlyDictionary<string, byte[]>> (Load);

        internal static Dictionary<string, byte[]> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, (byte[])item.Value.Clone());
            return result;
        }

        static IReadOnlyDictionary<string, byte[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<NpkKeyData> ("npk-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("NPK key dataset is invalid.");

            var keys = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Length != 32)
                    throw new InvalidDataException ("NPK key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("NPK key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
