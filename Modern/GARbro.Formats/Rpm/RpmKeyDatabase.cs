using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Rpm
{
    sealed class RpmKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, RpmKeyRecord> KnownSchemes { get; set; }
    }

    sealed class RpmKeyRecord
    {
        public string Keyword { get; set; }
        public int NameLength { get; set; }
    }

    static class RpmKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<RpmKeyData> s_data = new Lazy<RpmKeyData> (Load);

        internal static Dictionary<string, EncryptionScheme> CreateSchemes ()
        {
            var result = new Dictionary<string, EncryptionScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var value = item.Value;
                result.Add (item.Key, new EncryptionScheme (value.Keyword, value.NameLength));
            }
            return result;
        }

        static RpmKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<RpmKeyData> ("rpm-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("RPM key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || string.IsNullOrEmpty (item.Value.Keyword) || item.Value.Keyword.Length > 256
                    || item.Value.NameLength < 4 || item.Value.NameLength > 256)
                    throw new InvalidDataException ("RPM key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
