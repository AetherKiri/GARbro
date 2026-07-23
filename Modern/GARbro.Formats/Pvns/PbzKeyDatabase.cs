using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Pvns
{
    sealed class PbzKeyRecordData
    {
        public byte[] ArcKey { get; set; }
        public byte[] ScriptKey { get; set; }
    }

    sealed class PbzKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, PbzKeyRecordData> KnownSchemes { get; set; }
    }

    static class PbzKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, PbzKeys>> s_keys =
            new Lazy<IReadOnlyDictionary<string, PbzKeys>> (Load);

        internal static Dictionary<string, PbzKeys> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, PbzKeys> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, new PbzKeys {
                    ArcKey = (byte[])item.Value.ArcKey.Clone (),
                    ScriptKey = (byte[])item.Value.ScriptKey.Clone (),
                });
            return result;
        }

        static IReadOnlyDictionary<string, PbzKeys> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<PbzKeyData> ("pbz-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("PBZ key dataset is invalid.");

            var keys = new Dictionary<string, PbzKeys> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || item.Value.ArcKey == null || item.Value.ArcKey.Length == 0 || item.Value.ArcKey.Length > 256
                    || item.Value.ScriptKey == null || item.Value.ScriptKey.Length == 0 || item.Value.ScriptKey.Length > 256)
                    throw new InvalidDataException ("PBZ key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, new PbzKeys {
                    ArcKey = item.Value.ArcKey,
                    ScriptKey = item.Value.ScriptKey,
                }))
                    throw new InvalidDataException ("PBZ key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
