using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.MoonhirGames
{
    sealed class FpkKeyData
    {
        public int SchemaVersion { get; set; }
        public uint[] KnownKeys { get; set; }
    }

    static class FpkKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<uint[]> s_keys = new Lazy<uint[]> (Load);

        internal static uint[] CreateSchemeKeys ()
        {
            return (uint[])s_keys.Value.Clone();
        }

        static uint[] Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<FpkKeyData> ("fpk-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Length == 0)
                throw new InvalidDataException ("FPK key dataset is invalid.");
            var keys = new HashSet<uint>();
            foreach (var key in data.KnownKeys)
            {
                if (!keys.Add (key))
                    throw new InvalidDataException ("FPK key dataset contains a duplicate key: " + key);
            }
            return data.KnownKeys;
        }
    }
}
