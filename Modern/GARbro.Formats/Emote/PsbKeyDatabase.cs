using System;
using System.IO;
using System.Linq;

namespace GameRes.Formats.Emote
{
    sealed class PsbKeyData
    {
        public int SchemaVersion { get; set; }
        public uint[] KnownKeys { get; set; }
    }

    static class PsbKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<uint[]> s_keys = new Lazy<uint[]> (Load);

        internal static uint[] CreateKeys ()
        {
            return (uint[])s_keys.Value.Clone ();
        }

        static uint[] Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<PsbKeyData> ("psb-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null
                || data.KnownKeys.Length == 0 || data.KnownKeys.Length > 256
                || data.KnownKeys.Any (key => key == 0)
                || data.KnownKeys.Distinct ().Count () != data.KnownKeys.Length)
                throw new InvalidDataException ("PSB key dataset is invalid.");
            return data.KnownKeys;
        }
    }
}
