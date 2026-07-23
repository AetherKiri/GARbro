using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.YuRis
{
    sealed class YpfKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, YpfSchemeData> KnownSchemes { get; set; }
    }

    sealed class YpfSchemeData
    {
        public byte[] SwapTable { get; set; }
        public byte Key { get; set; }
        public bool GuessKey { get; set; }
        public uint ExtraHeaderSize { get; set; }
        public uint ScriptKey { get; set; }
        public int CompressType { get; set; }
    }

    static class YpfKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<YpfKeyData> s_data = new Lazy<YpfKeyData> (Load);

        internal static Dictionary<string, YpfScheme> CreateSchemes ()
        {
            var result = new Dictionary<string, YpfScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSchemes)
            {
                var value = item.Value;
                result.Add (item.Key, new YpfScheme {
                    SwapTable = (byte[])value.SwapTable.Clone (),
                    Key = value.Key,
                    GuessKey = value.GuessKey,
                    ExtraHeaderSize = value.ExtraHeaderSize,
                    ScriptKey = value.ScriptKey,
                    CompressType = (YpfCompression)value.CompressType,
                });
            }
            return result;
        }

        static YpfKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<YpfKeyData> ("ypf-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("YPF key dataset is invalid.");
            foreach (var item in data.KnownSchemes)
            {
                var value = item.Value;
                if (string.IsNullOrWhiteSpace (item.Key) || value == null || value.SwapTable == null
                    || value.SwapTable.Length == 0 || value.SwapTable.Length > 256
                    || value.ExtraHeaderSize > 0x1000 || value.CompressType < 0 || value.CompressType > 1)
                    throw new InvalidDataException ("YPF key dataset contains an invalid scheme.");
            }
            return data;
        }
    }
}
