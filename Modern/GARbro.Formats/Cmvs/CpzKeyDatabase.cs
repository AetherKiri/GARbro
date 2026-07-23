using System;
using System.Collections.Generic;
using System.IO;
using GameRes.Formats.Cmvs;

namespace GameRes.Formats.Purple
{
    sealed class CpzSchemeData
    {
        public int Version { get; set; }
        public uint[] Cpz5Secret { get; set; }
        public int Md5Variant { get; set; }
        public uint DecoderFactor { get; set; }
        public uint EntryInitKey { get; set; }
        public uint EntrySubKey { get; set; }
        public byte EntryTailKey { get; set; }
        public byte EntryKeyPos { get; set; }
        public uint IndexSeed { get; set; }
        public uint IndexAddend { get; set; }
        public uint IndexSubtrahend { get; set; }
        public uint[] DirKeyAddend { get; set; }
    }

    sealed class CpzKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, CpzSchemeData> KnownSchemes { get; set; }
    }

    static class CpzKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<CpzKeyData> s_data = new Lazy<CpzKeyData> (Load);

        internal static Dictionary<string, CmvsScheme> CreateSchemes ()
        {
            var result = new Dictionary<string, CmvsScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var value = item.Value;
                result.Add (item.Key, new CmvsScheme {
                    Version = value.Version,
                    Cpz5Secret = (uint[])value.Cpz5Secret.Clone (),
                    Md5Variant = (Md5Variant)value.Md5Variant,
                    DecoderFactor = value.DecoderFactor,
                    EntryInitKey = value.EntryInitKey,
                    EntrySubKey = value.EntrySubKey,
                    EntryTailKey = value.EntryTailKey,
                    EntryKeyPos = value.EntryKeyPos,
                    IndexSeed = value.IndexSeed,
                    IndexAddend = value.IndexAddend,
                    IndexSubtrahend = value.IndexSubtrahend,
                    DirKeyAddend = (uint[])value.DirKeyAddend.Clone (),
                });
            }
            return result;
        }

        static CpzKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<CpzKeyData> ("cpz-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0
                || data.KnownSchemes.Count > 256)
                throw new InvalidDataException ("CPZ scheme dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                var value = item.Value;
                if (string.IsNullOrWhiteSpace (item.Key) || value == null
                    || value.Version < 5 || value.Version > 7
                    || value.Cpz5Secret == null || value.Cpz5Secret.Length != 24
                    || value.DirKeyAddend == null || value.DirKeyAddend.Length != 4
                    || value.Md5Variant < 0 || value.Md5Variant > 6)
                    throw new InvalidDataException ("CPZ scheme dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
