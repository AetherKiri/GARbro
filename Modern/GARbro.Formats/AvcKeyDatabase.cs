using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.AVC
{
    sealed class AvcKeyData
    {
        public int SchemaVersion { get; set; }
        public List<AvcKeyRecord> KnownSchemes { get; set; }
    }

    sealed class AvcKeyRecord
    {
        public string Password { get; set; }
        public int KeyOffset { get; set; }
        public int HeaderOffset { get; set; }
    }

    static class AvcKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<AvcKeyData> s_data = new Lazy<AvcKeyData> (Load);

        internal static ArchiveScheme[] CreateSchemes ()
        {
            var result = new ArchiveScheme[s_data.Value.KnownSchemes.Count];
            for (var i = 0; i < result.Length; ++i)
            {
                var value = s_data.Value.KnownSchemes[i];
                result[i] = new ArchiveScheme {
                    Password = value.Password,
                    KeyOffset = value.KeyOffset,
                    HeaderOffset = value.HeaderOffset,
                };
            }
            return result;
        }

        static AvcKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<AvcKeyData> ("avc-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("AVC key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (item == null || string.IsNullOrEmpty (item.Password) || item.Password.Length > 256
                    || item.KeyOffset < 0 || item.KeyOffset > 0x10000
                    || item.HeaderOffset < 0 || item.HeaderOffset > 0x10000)
                    throw new InvalidDataException ("AVC key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
