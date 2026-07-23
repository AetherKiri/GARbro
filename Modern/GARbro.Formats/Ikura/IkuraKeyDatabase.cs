using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Ikura
{
    sealed class IkuraKeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, byte[]> KnownSecrets { get; set; }
    }

    static class IkuraKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<IkuraKeyData> s_data = new Lazy<IkuraKeyData> (Load);

        internal static Dictionary<string, byte[]> CreateSecrets ()
        {
            var result = new Dictionary<string, byte[]> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in s_data.Value.KnownSecrets)
                result.Add (item.Key, (byte[])item.Value.Clone ());
            return result;
        }

        static IkuraKeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<IkuraKeyData> ("ikura-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownSecrets == null || data.KnownSecrets.Count == 0)
                throw new InvalidDataException ("IKURA key dataset is invalid.");
            foreach (var item in data.KnownSecrets)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null || item.Value.Length != 2048)
                    throw new InvalidDataException ("IKURA key dataset contains an invalid secret.");
            }
            return data;
        }
    }
}
