using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Cyberworks
{
    sealed class TinkKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<uint, byte[]> KnownKeys { get; set; }
    }

    static class TinkKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<uint, byte[]>> s_keys =
            new Lazy<IReadOnlyDictionary<uint, byte[]>> (Load);

        internal static Dictionary<uint, byte[]> CreateSchemeKeys ()
        {
            var result = new Dictionary<uint, byte[]> ();
            foreach (var item in s_keys.Value)
                result.Add (item.Key, (byte[])item.Value.Clone ());
            return result;
        }

        static IReadOnlyDictionary<uint, byte[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<TinkKeyData> ("tink-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("TINK key dataset is invalid.");
            var keys = new Dictionary<uint, byte[]> ();
            foreach (var item in data.KnownKeys)
            {
                if (item.Value == null || item.Value.Length == 0)
                    throw new InvalidDataException ("TINK key dataset contains an incomplete entry: " + item.Key);
                if (!keys.TryAdd (item.Key, (byte[])item.Value.Clone ()))
                    throw new InvalidDataException ("TINK key dataset contains a duplicate signature: " + item.Key);
            }
            return keys;
        }
    }
}
