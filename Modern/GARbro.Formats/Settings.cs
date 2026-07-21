using System.IO.Compression;
using System.Text;

namespace GameRes.Formats.Properties
{
    internal sealed class Settings
    {
        public static Settings Default { get; } = new Settings();

        public int ZIPEncodingCP { get; set; } = Encoding.UTF8.CodePage;
        public CompressionLevel ZIPCompression { get; set; } = CompressionLevel.Optimal;
        public string ZIPPassword { get; set; } = string.Empty;

        public string XP3Scheme { get; set; } = string.Empty;
        public bool XP3CompressHeader { get; set; } = true;
        public bool XP3CompressContents { get; set; } = true;
        public int XP3Version { get; set; } = 1;
        public bool XP3RetainStructure { get; set; } = true;
    }
}
