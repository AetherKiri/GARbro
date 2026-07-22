using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.PkWare
{
    sealed class ZipPasswordData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    static class ZipPasswordDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IReadOnlyDictionary<string, string>> s_keys =
            new Lazy<IReadOnlyDictionary<string, string>> (Load);

        internal static Dictionary<string, string> CreateSchemeKeys ()
        {
            return new Dictionary<string, string> (s_keys.Value, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool TryGet (string title, out string password)
        {
            if (string.IsNullOrWhiteSpace (title))
            {
                password = null;
                return false;
            }
            return s_keys.Value.TryGetValue (title, out password);
        }

        static IReadOnlyDictionary<string, string> Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<ZipPasswordData> ("zip-passwords");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null)
                throw new InvalidDataException ("ZIP password dataset is invalid.");

            var keys = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.KnownKeys)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || string.IsNullOrEmpty (item.Value))
                    throw new InvalidDataException ("ZIP password dataset contains an incomplete entry.");
                string existing;
                if (!keys.TryGetValue (item.Key, out existing))
                    keys.Add (item.Key, item.Value);
                else if (!string.Equals (existing, item.Value, StringComparison.Ordinal))
                    throw new InvalidDataException ("ZIP password dataset contains a conflicting title: " + item.Key);
            }
            return keys;
        }
    }
}
