using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Unity
{
    sealed class BinIdxKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, BinIdxKeyRecord> KnownKeys { get; set; }
    }

    sealed class BinIdxKeyRecord
    {
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }
    }

    static class BinIdxKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, BinIdxKeyRecord>> s_keys =
            new Lazy<IReadOnlyDictionary<string, BinIdxKeyRecord>> (Load);

        internal static Dictionary<string, BinPackKey> CreateSchemeKeys ()
        {
            var result = new Dictionary<string, BinPackKey> (StringComparer.Ordinal);
            foreach (var item in s_keys.Value)
            {
                result.Add (item.Key, new BinPackKey {
                    Key = (byte[])item.Value.Key.Clone (),
                    IV = (byte[])item.Value.IV.Clone (),
                });
            }
            return result;
        }

        static IReadOnlyDictionary<string, BinIdxKeyRecord> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<BinIdxKeyData> ("bin-idx-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Count == 0)
                throw new InvalidDataException ("BIN/IDX key dataset is invalid.");
            var keys = new Dictionary<string, BinIdxKeyRecord> (StringComparer.Ordinal);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null
                    || item.Value.Key == null || item.Value.IV == null
                    || (item.Value.Key.Length != 16 && item.Value.Key.Length != 24 && item.Value.Key.Length != 32)
                    || item.Value.IV.Length != 16)
                    throw new InvalidDataException ("BIN/IDX key dataset contains an incomplete entry: " + item.Key);
                if (!keys.TryAdd (item.Key, new BinIdxKeyRecord {
                    Key = (byte[])item.Value.Key.Clone (),
                    IV = (byte[])item.Value.IV.Clone (),
                }))
                    throw new InvalidDataException ("BIN/IDX key dataset contains a duplicate title: " + item.Key);
            }
            return keys;
        }
    }
}
