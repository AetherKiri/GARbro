using System;
using System.Collections.Generic;
using System.IO;

namespace GameRes.Formats.Fmod
{
    sealed class Fsb5HeaderData
    {
        public byte[] VorbisData { get; set; }
        public int PatchOffset { get; set; }
        public byte[] PatchData { get; set; }
    }

    sealed class Fsb5KeyData
    {
        public int SchemaVersion { get; set; }
        public Dictionary<uint, Fsb5HeaderData> VorbisHeaders { get; set; }
    }

    static class Fsb5KeyDatabase
    {
        const int SchemaVersion = 1;
        static readonly Lazy<Fsb5KeyData> s_data = new Lazy<Fsb5KeyData> (Load);

        internal static Dictionary<uint, FmodVorbisSetup> CreateSchemeKeys ()
        {
            var result = new Dictionary<uint, FmodVorbisSetup> ();
            foreach (var item in s_data.Value.VorbisHeaders)
            {
                result.Add (item.Key, new FmodVorbisSetup {
                    VorbisData = (byte[])item.Value.VorbisData.Clone (),
                    PatchOffset = item.Value.PatchOffset,
                    PatchData = item.Value.PatchData == null ? null : (byte[])item.Value.PatchData.Clone (),
                });
            }
            return result;
        }

        static Fsb5KeyData Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<Fsb5KeyData> ("fsb5-vorbis-headers");
            if (data.SchemaVersion != SchemaVersion || data.VorbisHeaders == null || data.VorbisHeaders.Count == 0
                || data.VorbisHeaders.Count > 4096)
                throw new InvalidDataException ("FSB5 Vorbis header dataset is invalid.");
            foreach (var item in data.VorbisHeaders)
            {
                var value = item.Value;
                if (value == null || value.VorbisData == null || value.VorbisData.Length == 0
                    || value.VorbisData.Length > 1024 * 1024 || value.PatchOffset < 0)
                    throw new InvalidDataException ("FSB5 Vorbis header dataset contains an invalid header.");
                if (value.PatchData != null && (value.PatchOffset > value.VorbisData.Length
                    || value.PatchData.Length > value.VorbisData.Length - value.PatchOffset))
                    throw new InvalidDataException ("FSB5 Vorbis header dataset contains an invalid patch.");
            }
            return data;
        }
    }
}
