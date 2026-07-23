using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.NitroPlus
{
    sealed class NpaKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, NpaKeyRecordData> KnownSchemes { get; set; }
    }

    sealed class NpaKeyRecordData
    {
        public int TitleId { get; set; }
        public uint NameKey { get; set; }
        public byte[] Order { get; set; }
    }

    static class NpaKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, EncryptionScheme>> s_keys =
            new Lazy<IReadOnlyDictionary<string, EncryptionScheme>> (Load);

        internal static Dictionary<string, EncryptionScheme> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, EncryptionScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_keys.Value)
                result.Add (item.Key, new EncryptionScheme (item.Value.TitleId, item.Value.NameKey,
                    (byte[])item.Value.Order.Clone ()));
            return result;
        }

        static IReadOnlyDictionary<string, EncryptionScheme> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<NpaKeyData> ("npa-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSchemes == null || data.KnownSchemes.Count == 0)
                throw new InvalidDataException ("NPA key dataset is invalid.");

            var keys = new Dictionary<string, EncryptionScheme> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownSchemes)
            {
                var value = item.Value;
                if (string.IsNullOrWhiteSpace (item.Key) || value == null || value.Order == null
                    || value.Order.Length == 0 || value.Order.Length > 256
                    || value.TitleId < 0 || value.TitleId > (int)NpaTitleId.HANACHIRASU)
                    throw new InvalidDataException ("NPA key dataset contains an incomplete entry.");
                if (!keys.TryAdd (item.Key, new EncryptionScheme ((NpaTitleId)value.TitleId, value.NameKey, value.Order)))
                    throw new InvalidDataException ("NPA key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
