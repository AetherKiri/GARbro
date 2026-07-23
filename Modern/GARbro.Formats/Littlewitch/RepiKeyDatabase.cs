using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Littlewitch
{
    sealed class RepiKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, uint[]> KnownSchemes { get; set; }
    }

    static class RepiKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, uint[]>> s_schemes =
            new Lazy<IReadOnlyDictionary<string, uint[]>> (Load);

        internal static Dictionary<string, uint[]> CreateSchemes ()
        {
            var result = new Dictionary<string, uint[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_schemes.Value)
                result.Add (item.Key, (uint[])item.Value.Clone ());
            return result;
        }

        static IReadOnlyDictionary<string, uint[]> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<RepiKeyData> ("repi-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("RepiPack key dataset is invalid.");

            var schemes = new Dictionary<string, uint[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Length != 3)
                    throw new InvalidDataException ("RepiPack key dataset contains an invalid scheme.");
                if (item.Value[0] == 0)
                    throw new InvalidDataException ("RepiPack key dataset contains an invalid archive key.");
                if (!schemes.TryAdd (item.Key, item.Value))
                    throw new InvalidDataException ("RepiPack key dataset contains a duplicate title: " + item.Key);
            }
            return schemes;
        }
    }
}
