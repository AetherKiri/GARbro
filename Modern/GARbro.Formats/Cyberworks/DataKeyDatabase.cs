using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Cyberworks
{
    sealed class DataKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, int> KnownSchemes { get; set; }
    }

    static class DataKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<DataKeyData> s_data = new Lazy<DataKeyData> (Load);

        internal static Dictionary<string, DataScheme> CreateSchemes ()
        {
            var result = new Dictionary<string, DataScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
                result.Add (item.Key, new DataScheme { ExtraHeaderSize = item.Value });
            return result;
        }

        static DataKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<DataKeyData> ("data-csystem-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("DATA/Csystem key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value < 0 || item.Value > 0x1000)
                    throw new InvalidDataException ("DATA/Csystem key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
