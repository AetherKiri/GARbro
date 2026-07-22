using System;
using System.IO;

namespace GameRes.Formats.Actgs
{
    sealed class ActgsKeyData
    {
        public int SchemaVersion { get; set; }
        public byte[][] KnownKeys { get; set; }
    }

    static class ActgsKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<byte[][]> s_keys = new Lazy<byte[][]> (Load);

        internal static byte[][] CreateSchemeKeys ()
        {
            var result = new byte[s_keys.Value.Length][];
            for (var i = 0; i < result.Length; ++i)
                result[i] = (byte[])s_keys.Value[i].Clone ();
            return result;
        }

        static byte[][] Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<ActgsKeyData> ("actgs-keys");
            if (data.SchemaVersion != SchemaVersion || data.KnownKeys == null || data.KnownKeys.Length == 0)
                throw new InvalidDataException ("ACTGS key dataset is invalid.");
            var keys = new byte[data.KnownKeys.Length][];
            for (var i = 0; i < keys.Length; ++i)
            {
                var key = data.KnownKeys[i];
                if (key == null || key.Length < 4)
                    throw new InvalidDataException ("ACTGS key dataset contains an incomplete key.");
                keys[i] = (byte[])key.Clone ();
            }
            return keys;
        }
    }
}
