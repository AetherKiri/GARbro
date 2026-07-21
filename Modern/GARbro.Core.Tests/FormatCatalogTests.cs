using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using GameRes;
using System.Windows.Media;
using Xunit;

namespace GARbro.Core.Tests
{
    public class FormatCatalogTests
    {
        [Fact]
        public void Builtin_image_formats_are_available ()
        {
            var tags = FormatCatalog.Instance.ImageFormats.Select (format => format.Tag).ToArray();

            Assert.Contains ("PNG", tags);
            Assert.Contains ("JPEG", tags);
            Assert.Contains ("BMP", tags);
            Assert.Contains ("TGA", tags);
        }

        [Theory]
        [InlineData("PNG")]
        [InlineData("JPEG")]
        [InlineData("BMP")]
        public void Standard_image_formats_round_trip_dimensions (string tag)
        {
            var info = new ImageMetaData { Width = 2, Height = 2 };
            var pixels = new byte[] {
                0, 0, 255, 255, 0, 255, 0, 255,
                255, 0, 0, 255, 255, 255, 255, 255,
            };
            var image = ImageData.Create (info, PixelFormats.Bgra32, null, pixels, 8);
            var format = ImageFormat.FindByTag (tag);

            using (var output = new MemoryStream())
            {
                format.Write (output, image);
                output.Position = 0;
                using (var input = new BinaryStream (output, "image." + tag.ToLowerInvariant()))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.Equal ((uint)2, decoded.Width);
                    Assert.Equal ((uint)2, decoded.Height);
                }
            }
        }

        [Fact]
        public void Zip_format_is_discovered_and_lists_entries ()
        {
            var archivePath = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
            try
            {
                using (var output = File.Create (archivePath))
                using (var zip = new ZipArchive (output, ZipArchiveMode.Create))
                using (var entry = new StreamWriter (zip.CreateEntry ("nested/sample.txt").Open()))
                    entry.Write ("cross-platform");

                VFS.ChDir (archivePath);

                Assert.NotNull (VFS.CurrentArchive);
                Assert.Equal ("ZIP", VFS.CurrentArchive.Tag);
                Assert.Contains (VFS.CurrentArchive.Dir, entry => entry.Name == "nested/sample.txt");
            }
            finally
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                File.Delete (archivePath);
            }
        }

        [Fact]
        public void Xp3_format_creates_and_extracts_an_unencrypted_entry ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                File.WriteAllText ("sample.txt", "cross-platform XP3", Encoding.UTF8);
                var archivePath = Path.Combine (tempDirectory, "sample.xp3");
                var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>().Single (item => item.Tag == "XP3");
                var options = format.GetDefaultOptions();

                using (var output = File.Create (archivePath))
                    format.Create (output, new[] { new Entry { Name = "sample.txt" } }, options);

                VFS.ChDir (archivePath);
                var archive = VFS.CurrentArchive;
                var entry = Assert.Single (archive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (archive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("cross-platform XP3", input.ReadToEnd());
            }
            finally
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }
    }
}
