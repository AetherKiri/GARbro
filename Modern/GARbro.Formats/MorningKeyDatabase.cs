using System;
using System.IO;

namespace GameRes.Formats.Morning
{
    sealed class MorningKeyData
    {
        public int SchemaVersion { get; set; }
        public byte[] DefaultKey { get; set; }
    }

    static class MorningKeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<byte[]> s_key = new Lazy<byte[]> (Load);

        internal static byte[] CreateKey ()
        {
            return (byte[])s_key.Value.Clone();
        }

        static byte[] Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<MorningKeyData> ("morning-key");
            if (data.SchemaVersion != SchemaVersion || data.DefaultKey == null
                || data.DefaultKey.Length < 2
                || (data.DefaultKey.Length & (data.DefaultKey.Length - 1)) != 0)
                throw new InvalidDataException ("Morning key dataset is invalid.");
            return data.DefaultKey;
        }
    }
}
