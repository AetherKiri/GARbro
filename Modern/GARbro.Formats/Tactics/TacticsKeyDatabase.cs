using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Tactics
{
    sealed class TacticsKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, TacticsKeyRecord> KnownSchemes { get; set; }
    }

    sealed class TacticsKeyRecord
    {
        public string Password { get; set; }
        public bool CustomLzss { get; set; }
    }

    static class TacticsKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<TacticsKeyData> s_data = new Lazy<TacticsKeyData> (Load);

        internal static Dictionary<string, ArcScheme> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, ArcScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var value = item.Value;
                result.Add (item.Key, new ArcScheme (value.Password, value.CustomLzss));
            }
            return result;
        }

        static TacticsKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<TacticsKeyData> ("tactics-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("Tactics key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || string.IsNullOrEmpty (item.Value.Password) || item.Value.Password.Length > 256)
                    throw new InvalidDataException ("Tactics key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
