using System;
using System.Collections.Generic;

namespace GameRes.Formats.NScripter
{
    [Serializable]
    public class NsaScheme : ResourceScheme
    {
        public Dictionary<string, string> KnownKeys;
    }

    public class NsaOptions : ResourceOptions
    {
        public string Password { get; set; }
    }

    internal class NsaEncryptedArchive : ArcFile
    {
        public readonly byte[] Key;

        public NsaEncryptedArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, byte[] key)
            : base (arc, impl, dir)
        {
            Key = key;
        }
    }
}
