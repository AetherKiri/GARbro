using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using GameRes;
using System.Windows.Media;
using GameRes.Formats.KiriKiri;
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

        [Fact]
        public void Xp3_format_opens_an_archive_with_an_explicit_modern_scheme ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            var previousScheme = Xp3Opener.ModernSchemeName;
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                File.WriteAllText ("sample.txt", "encrypted XP3", Encoding.UTF8);
                var archivePath = Path.Combine (tempDirectory, "encrypted.xp3");
                var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>().Single (item => item.Tag == "XP3");
                var options = new Xp3Options {
                    Version = 1,
                    Scheme = new FateCrypt(),
                    CompressIndex = true,
                    CompressContents = true,
                    RetainDirs = true,
                };

                using (var output = File.Create (archivePath))
                    format.Create (output, new[] { new Entry { Name = "sample.txt" } }, options);

                Xp3Opener.ModernSchemeName = "FateCrypt";
                VFS.ChDir (archivePath);
                var archive = VFS.CurrentArchive;
                var entry = Assert.Single (archive.Dir);
                using (var input = new StreamReader (archive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("encrypted XP3", input.ReadToEnd());
            }
            finally
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Xp3Opener.ModernSchemeName = previousScheme;
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Xp3_format_automatically_detects_a_scheme_when_requested ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            var previousScheme = Xp3Opener.ModernSchemeName;
            Directory.CreateDirectory (tempDirectory);
            var requested = false;
            var parametersHandler = new ParametersRequestEventHandler ((sender, args) => {
                requested = true;
                Assert.IsType<Xp3Opener> (sender);
                args.Options = new Xp3Options { AutoDetect = true };
                args.InputResult = true;
            });
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                File.WriteAllText ("sample.txt", "queried XP3", Encoding.UTF8);
                var archivePath = Path.Combine (tempDirectory, "queried.xp3");
                var format = (Xp3Opener)FormatCatalog.Instance.Formats.OfType<ArchiveFormat>().Single (item => item.Tag == "XP3");
                var options = new Xp3Options {
                    Version = 1,
                    Scheme = new FateCrypt(),
                    CompressIndex = true,
                    CompressContents = true,
                    RetainDirs = true,
                };

                using (var output = File.Create (archivePath))
                    format.Create (output, new[] { new Entry { Name = "sample.txt" } }, options, null);

                var previousForceEncryptionQuery = format.ForceEncryptionQuery;
                format.ForceEncryptionQuery = true;
                Xp3Opener.ModernSchemeName = null;
                FormatCatalog.Instance.ParametersRequest += parametersHandler;
                try
                {
                    VFS.ChDir (archivePath);
                    var entry = Assert.Single (VFS.CurrentArchive.Dir);
                    using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                        Assert.Equal ("queried XP3", input.ReadToEnd());
                }
                finally
                {
                    FormatCatalog.Instance.ParametersRequest -= parametersHandler;
                    format.ForceEncryptionQuery = previousForceEncryptionQuery;
                }

                Assert.True (requested);
            }
            finally
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Xp3Opener.ModernSchemeName = previousScheme;
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Xp3_profiles_load_hx_and_senren_schemes_without_binaryformatter ()
        {
            const string profiles = @"[
              {
                ""name"": ""hx-lite-test"",
                ""algorithm"": ""HxCryptLite"",
                ""cx"": {
                  ""mask"": 0,
                  ""offset"": 0,
                  ""prologOrder"": [],
                  ""oddBranchOrder"": [],
                  ""evenBranchOrder"": [],
                  ""controlBlock"": []
                }
              },
              {
                ""name"": ""senren-test"",
                ""title"": ""Senren Banka"",
                ""algorithm"": ""SenrenCxCrypt"",
                ""cx"": {
                  ""mask"": 0,
                  ""offset"": 0,
                  ""prologOrder"": [],
                  ""oddBranchOrder"": [],
                  ""evenBranchOrder"": [],
                  ""controlBlock"": []
                }
              }
            ]";
            using (var input = new MemoryStream (Encoding.UTF8.GetBytes (profiles)))
                Xp3SchemeProfiles.Load (input);

            Assert.True (Xp3Opener.TryGetScheme ("hx-lite-test", out var hx));
            Assert.IsType<HxCryptLite> (hx);
            Assert.True (Xp3Opener.TryGetScheme ("senren-test", out var senren));
            Assert.IsType<SenrenCxCrypt> (senren);
            Assert.Equal ("Senren Banka", Xp3Opener.GetModernSchemeDisplayName ("senren-test"));
        }
    }
}
