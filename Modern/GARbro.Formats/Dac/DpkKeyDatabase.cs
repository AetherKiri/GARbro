using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameRes.Formats.Dac
{
    sealed class DpkKeyData
    {
        public int SchemaVersion { get; set; }
        public List<DpkKeyRecord> KnownSchemes { get; set; }
    }

    sealed class DpkKeyRecord
    {
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public string Name { get; set; }
        public string OriginalTitle { get; set; }
    }

    static class DpkKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<DpkKeyData> s_data = new Lazy<DpkKeyData> (Load);

        internal static DpkScheme[] CreateSchemes ()
        {
            return s_data.Value.KnownSchemes.Select (item => new DpkScheme {
                Key1 = item.Key1,
                Key2 = item.Key2,
                Name = item.Name,
                OriginalTitle = item.OriginalTitle,
            }).ToArray ();
        }

        static DpkKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<DpkKeyData> ("dpk-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("DPK key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                if (item == null || item.Name == null || item.Name.Length > 256
                    || item.OriginalTitle != null && item.OriginalTitle.Length > 256)
                    throw new InvalidDataException ("DPK key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
