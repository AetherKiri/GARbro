using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Lucifen
{
    sealed class LpkKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, LpkSchemeData> KnownSchemes { get; set; }
        public Dictionary<string, Dictionary<string, LpkFileKeyData>> KnownKeys { get; set; }
    }

    sealed class LpkSchemeData
    {
        public LpkFileKeyData BaseKey { get; set; }
        public byte ContentXor { get; set; }
        public uint RotatePattern { get; set; }
        public bool ImportGameInit { get; set; }
    }

    sealed class LpkFileKeyData
    {
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
    }

    static class LpkKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<LpkKeyData> s_data = new Lazy<LpkKeyData> (Load);

        internal static Dictionary<string, EncryptionScheme> CreateSchemes ()
        {
            var result = new Dictionary<string, EncryptionScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var value = item.Value;
                result.Add (item.Key, new EncryptionScheme {
                    BaseKey = new LpkOpener.Key (value.BaseKey.Key1, value.BaseKey.Key2),
                    ContentXor = value.ContentXor,
                    RotatePattern = value.RotatePattern,
                    ImportGameInit = value.ImportGameInit,
                });
            }
            return result;
        }

        internal static Dictionary<string, Dictionary<string, LpkOpener.Key>> CreateFileKeys ()
        {
            var result = new Dictionary<string, Dictionary<string, LpkOpener.Key>> (StringComparer.OrdinalIgnoreCase);
            foreach (var title in s_data.Value.KnownKeys)
            {
                var fileKeys = new Dictionary<string, LpkOpener.Key> (StringComparer.OrdinalIgnoreCase);
                foreach (var item in title.Value)
                    fileKeys.Add (item.Key, new LpkOpener.Key (item.Value.Key1, item.Value.Key2));
                result.Add (title.Key, fileKeys);
            }
            return result;
        }

        static LpkKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<LpkKeyData> ("lpk-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0
                || data.KnownKeys == null)
                throw new InvalidDataException ("LPK key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                var value = item.Value;
                if (string.IsNullOrWhiteSpace (item.Key) || value == null || value.BaseKey == null
                    || (value.BaseKey.Key1 == 0 && value.BaseKey.Key2 == 0))
                    throw new InvalidDataException ("LPK key dataset contains an incomplete scheme.");
            }
            foreach (var title in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (title.Key) || title.Value == null)
                    throw new InvalidDataException ("LPK key dataset contains an incomplete title map.");
                foreach (var item in title.Value)
                {
                    if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                        || (item.Value.Key1 == 0 && item.Value.Key2 == 0))
                        throw new InvalidDataException ("LPK key dataset contains an incomplete file key.");
                }
            }
            return data;
        }
    }
}
