using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Yatagarasu
{
    sealed class PkgKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, uint[]> KnownKeys { get; set; }
    }

    static class PkgKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, uint[]>> s_keys =
            new Lazy<IReadOnlyDictionary<string, uint[]>> (Load);

        internal static Dictionary<string, uint[]> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, uint[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, (uint[])item.Value.Clone());
            return result;
        }

        static IReadOnlyDictionary<string, uint[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<PkgKeyData> ("pkg-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("PKG key dataset is invalid.");

            var keys = new Dictionary<string, uint[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || item.Value.Length == 0 || item.Value.Length > 1024)
                    throw new InvalidDataException ("PKG key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("PKG key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
