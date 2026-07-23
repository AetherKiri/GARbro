using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.ExHibit
{
    sealed class GyuKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, Dictionary<int, uint>> NumericKeys { get; set; }
        public Dictionary<string, Dictionary<string, uint>> StringKeys { get; set; }
    }

    static class GyuKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<GyuKeyData> s_data = new Lazy<GyuKeyData> (Load);

        internal static Dictionary<string, Dictionary<int, uint>> CreateNumericKeys ()
        {
            return CloneNumeric (s_data.Value.NumericKeys);
        }

        internal static Dictionary<string, Dictionary<string, uint>> CreateStringKeys ()
        {
            return CloneString (s_data.Value.StringKeys);
        }

        static GyuKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<GyuKeyData> ("gyu-keys");
            if (data.SchemaVersion != SchemaVersion || data.NumericKeys == null || data.StringKeys == null
                || data.NumericKeys.Count == 0 && data.StringKeys.Count == 0)
                throw new InvalidDataException ("GYU key dataset is invalid.");
            foreach (var title in data.NumericKeys)
            {
                if (string.IsNullOrWhiteSpace (title.Key) || title.Value == null || title.Value.Count == 0)
                    throw new InvalidDataException ("GYU numeric key dataset contains an incomplete title map.");
                foreach (var item in title.Value)
                    if (item.Value == 0)
                        throw new InvalidDataException ("GYU numeric key dataset contains an empty key.");
            }
            foreach (var title in data.StringKeys)
            {
                if (string.IsNullOrWhiteSpace (title.Key) || title.Value == null || title.Value.Count == 0)
                    throw new InvalidDataException ("GYU string key dataset contains an incomplete title map.");
                foreach (var item in title.Value)
                {
                    if (string.IsNullOrWhiteSpace (item.Key) || item.Value == 0)
                        throw new InvalidDataException ("GYU string key dataset contains an incomplete key.");
                }
            }
            return data;
        }

        static Dictionary<string, Dictionary<int, uint>> CloneNumeric (Dictionary<string, Dictionary<int, uint>> source)
        {
            var result = new Dictionary<string, Dictionary<int, uint>> (StringComparer.OrdinalIgnoreCase);
            foreach (var title in source)
                result.Add (title.Key, new Dictionary<int, uint> (title.Value));
            return result;
        }

        static Dictionary<string, Dictionary<string, uint>> CloneString (Dictionary<string, Dictionary<string, uint>> source)
        {
            var result = new Dictionary<string, Dictionary<string, uint>> (StringComparer.OrdinalIgnoreCase);
            foreach (var title in source)
                result.Add (title.Key, new Dictionary<string, uint> (title.Value, StringComparer.OrdinalIgnoreCase));
            return result;
        }
    }
}
