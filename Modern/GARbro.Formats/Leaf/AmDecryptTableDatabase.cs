using System;
using System.IO;

namespace GameRes.Formats.Leaf
{
    sealed class AmDecryptTableData
    {
        public int SchemaVersion { get; set; }
        public byte[] DecryptTable { get; set; }
    }

    static class AmDecryptTableDatabase
    {
        const int SchemaVersion = 1;
        const int TableLength = 0x10000;
        static readonly Lazy<byte[]> s_table = new Lazy<byte[]> (Load);

        internal static byte[] CreateTable ()
        {
            return (byte[])s_table.Value.Clone ();
        }

        static byte[] Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<AmDecryptTableData> ("am-leaf-table");
            if (data.SchemaVersion != SchemaVersion || data.DecryptTable == null
                || data.DecryptTable.Length != TableLength)
                throw new InvalidDataException ("AM/Leaf decrypt-table dataset is invalid.");
            return data.DecryptTable;
        }
    }
}
