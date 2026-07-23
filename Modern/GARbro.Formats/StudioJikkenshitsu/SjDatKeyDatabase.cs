using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Jikkenshitsu
{
    sealed class SjDatKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    static class SjDatKeyDatabase
    {
        const int SchemaVersion = 1;
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
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<SjDatKeyData> ("speed-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("DAT/SPEED key dataset is invalid.");

            var keys = new Dictionary<string, byte[]> (StringComparer.Ordinal);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || item.Value.Length == 0 || item.Value.Length > 16)
                    throw new InvalidDataException ("DAT/SPEED key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, (byte[])item.Value.Clone ()))
                    throw new InvalidDataException ("DAT/SPEED key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
