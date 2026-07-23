using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Elf
{
    sealed class Ai5KeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, Ai5KeyRecordData> KnownSchemes { get; set; }
    }

    sealed class Ai5KeyRecordData
    {
        public int NameLength { get; set; }
        public byte NameKey { get; set; }
        public uint SizeKey { get; set; }
        public uint OffsetKey { get; set; }
    }

    static class Ai5KeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, ArcIndexScheme>> s_keys =
            new Lazy<IReadOnlyDictionary<string, ArcIndexScheme>> (Load);

        internal static Dictionary<string, ArcIndexScheme> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, ArcIndexScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, new ArcIndexScheme {
                    NameLength = item.Value.NameLength,
                    NameKey = item.Value.NameKey,
                    SizeKey = item.Value.SizeKey,
                    OffsetKey = item.Value.OffsetKey,
                });
            return result;
        }

        static IReadOnlyDictionary<string, ArcIndexScheme> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<Ai5KeyData> ("ai5-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("AI5WIN key dataset is invalid.");

            var keys = new Dictionary<string, ArcIndexScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                var value = item.Value;
                if (string.IsNullOrWhiteSpace (item.Key) || value == null
                    || value.NameLength <= 0 || value.NameLength > 0x100)
                    throw new InvalidDataException ("AI5WIN key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, new ArcIndexScheme {
                    NameLength = value.NameLength,
                    NameKey = value.NameKey,
                    SizeKey = value.SizeKey,
                    OffsetKey = value.OffsetKey,
                }))
                    throw new InvalidDataException ("AI5WIN key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
