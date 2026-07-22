using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using GameRes;
using System.Windows.Media;
using GameRes.Formats.KiriKiri;
using GameRes.Formats.Morning;
using GameRes.Formats.PkWare;
using GameRes.Formats.TopCat;
using ICSharpCode.SharpZipLib.Zip;
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

        [Fact]
        public void Desktop_audio_preview_formats_are_available ()
        {
            var tags = FormatCatalog.Instance.AudioFormats.Select (format => format.Tag).ToArray();

            Assert.Contains ("WAV", tags);
            Assert.Contains ("OGG", tags);
            Assert.Contains ("MP3", tags);
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
        public void Tcd_format_is_discovered_with_migrated_key_map ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "TCD");

            Assert.IsType<TcdOpener> (format);
            Assert.Equal (3, TcdOpener.KnownKeys.Count);
            Assert.Equal (327047585, TcdOpener.KnownKeys["Atori no Sora to Shinchuu no Tsuki"]);
            Assert.Equal (-982448593, TcdOpener.KnownKeys["Favorite Sweet!"]);
            Assert.Equal (-987080510, TcdOpener.KnownKeys["Nanapuri"]);
        }

        [Fact]
        public void Tcd_format_opens_a_minimal_v3_fixture ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.tcd");
                CreateTcd3Fixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("TCD", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("dir/sample.WAV", entry.Name.Replace ('\\', '/'));
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated TCD fixture", input.ReadToEnd());
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
        public void Morning_format_loads_migrated_key_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "PAK/MORNING");
            Assert.IsType<PakOpener> (format);
            var scheme = Assert.IsType<MorningScheme> (format.Scheme);
            Assert.Equal (512, scheme.DefaultKey.Length);

            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.pak");
                CreateMorningFixture (archivePath, scheme.DefaultKey);

                VFS.ChDir (archivePath);
                Assert.Equal ("PAK/MORNING", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated Morning fixture", input.ReadToEnd());
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
                using (var binary = archive.OpenBinaryEntry (entry))
                {
                    Assert.True (binary.CanSeek);
                    using var input = new StreamReader (binary.AsStream, Encoding.UTF8, true, 1024, true);
                    Assert.Equal ("cross-platform XP3", input.ReadToEnd());
                }
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
        public void Xp3_image_entry_decodes_after_yuzu_decryption ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            var previousScheme = Xp3Opener.ModernSchemeName;
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var metadata = new ImageMetaData { Width = 2, Height = 2 };
                var pixels = new byte[] {
                    0, 0, 255, 255, 0, 255, 0, 255,
                    255, 0, 0, 255, 255, 255, 255, 255,
                };
                var image = ImageData.Create (metadata, PixelFormats.Bgra32, null, pixels, 8);
                using (var output = File.Create ("sample.png"))
                    ImageFormat.FindByTag ("PNG").Write (output, image);

                var archivePath = Path.Combine (tempDirectory, "sample.xp3");
                var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>().Single (item => item.Tag == "XP3");
                var options = new Xp3Options { Version = 1, Scheme = new YuzuCrypt(), CompressIndex = true, CompressContents = true };
                using (var output = File.Create (archivePath))
                    format.Create (output, new[] { new Entry { Name = "images/sample.png" } }, options);

                Xp3Opener.ModernSchemeName = "YuzuCrypt";
                VFS.ChDir (archivePath);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                using (var input = VFS.CurrentArchive.OpenBinaryEntry (entry))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.NotNull (decoded);
                    Assert.Equal ((uint)2, decoded.Width);
                    Assert.Equal ((uint)2, decoded.Height);
                }
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

        [Fact]
        public void Xp3_v2_profiles_use_algorithm_specific_parameters ()
        {
            const string profiles = @"{
              ""schemaVersion"": 1,
              ""profiles"": [
                {
                  ""id"": ""v2-hx-lite-test"",
                  ""algorithm"": ""hx-lite"",
                  ""parameters"": {
                    ""cx"": {
                      ""mask"": 0,
                      ""offset"": 0,
                      ""prologOrder"": [],
                      ""oddBranchOrder"": [],
                      ""evenBranchOrder"": [],
                      ""controlBlock"": []
                    },
                    ""randomType"": 0,
                    ""fileCryptFlag"": false
                  }
                },
                {
                  ""id"": ""v2-senren-test"",
                  ""title"": ""Senren Banka v2"",
                  ""algorithm"": ""senren-cx"",
                  ""parameters"": {
                    ""cx"": {
                      ""mask"": 0,
                      ""offset"": 0,
                      ""prologOrder"": [],
                      ""oddBranchOrder"": [],
                      ""evenBranchOrder"": [],
                      ""controlBlock"": []
                    }
                  }
                }
              ]
            }";
            using (var input = new MemoryStream (Encoding.UTF8.GetBytes (profiles)))
                Xp3SchemeProfiles.Load (input);

            Assert.True (Xp3Opener.TryGetScheme ("v2-hx-lite-test", out var hx));
            Assert.IsType<HxCryptLite> (hx);
            Assert.True (Xp3Opener.TryGetScheme ("v2-senren-test", out var senren));
            Assert.IsType<SenrenCxCrypt> (senren);
            Assert.Equal ("Senren Banka v2", Xp3Opener.GetModernSchemeDisplayName ("v2-senren-test"));
        }

        [Fact]
        public void Xp3_v2_profiles_reject_parameters_for_another_algorithm ()
        {
            const string profiles = @"{
              ""schemaVersion"": 1,
              ""profiles"": [
                {
                  ""id"": ""invalid-profile"",
                  ""algorithm"": ""senren-cx"",
                  ""parameters"": {
                    ""cx"": {
                      ""mask"": 0,
                      ""offset"": 0,
                      ""prologOrder"": [],
                      ""oddBranchOrder"": [],
                      ""evenBranchOrder"": [],
                      ""controlBlock"": []
                    },
                    ""seed"": 1
                  }
                }
              ]
            }";

            using (var input = new MemoryStream (Encoding.UTF8.GetBytes (profiles)))
                Assert.Throws<InvalidDataException> (() => Xp3SchemeProfiles.Load (input));
        }

        [Fact]
        public void Bundled_xp3_title_registry_loads_from_verified_v2_game_data ()
        {
            Assert.True (Xp3Opener.TryGetScheme ("Fate/stay night", out var scheme));
            Assert.IsType<FateCrypt> (scheme);
            Assert.Contains ("Fate/stay night", Xp3Opener.ModernSchemeNames);
        }

        [Fact]
        public void Bundled_xp3_game_map_resolves_a_parameterized_profile ()
        {
            Assert.True (Xp3TitleDatabase.TryGetGameTitle ("SenrenBanka.exe", out var title));
            Assert.Equal ("Senren＊Banka", title);
            Assert.True (Xp3Opener.TryGetScheme (title, out var scheme));
            Assert.IsType<SenrenCxCrypt> (scheme);
        }

        [Fact]
        public void Bundled_zip_password_map_resolves_known_title_keys ()
        {
            Assert.True (ZipOpener.TryGetKnownPassword ("Choir", out var password));
            Assert.Equal ("trendri0da0", password);
            Assert.False (ZipOpener.TryGetKnownPassword ("unknown-zip-title", out _));
        }

        [Fact]
        public void Encrypted_zip_uses_migrated_title_password_before_prompt ()
        {
            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            var prompted = false;
            var parametersHandler = new ParametersRequestEventHandler ((sender, args) => {
                prompted = true;
                args.Options = new ZipOptions { Password = "wrong-password" };
                args.InputResult = true;
            });
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                File.WriteAllText ("Choir.exe", string.Empty);
                var archivePath = Path.Combine (tempDirectory, "sample.zip");
                CreateEncryptedZip (archivePath, "trendri0da0");
                var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull (gameMapField);
                var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
                var testGameMap = new Dictionary<string, string> (originalGameMap,
                    StringComparer.OrdinalIgnoreCase) { ["Choir.exe"] = "Choir" };
                gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
                FormatCatalog.Instance.ParametersRequest += parametersHandler;
                try
                {
                    VFS.ChDir (archivePath);
                    var entry = Assert.Single (VFS.CurrentArchive.Dir);
                    using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                        Assert.Equal ("migrated ZIP password", input.ReadToEnd());
                }
                finally
                {
                    FormatCatalog.Instance.ParametersRequest -= parametersHandler;
                    gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                }
                Assert.False (prompted);
            }
            finally
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        static void CreateEncryptedZip (string path, string password)
        {
            using (var output = File.Create (path))
            using (var zip = new ZipOutputStream (output))
            {
                zip.Password = password;
                var entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry ("sample.txt");
                zip.PutNextEntry (entry);
                var data = Encoding.UTF8.GetBytes ("migrated ZIP password");
                zip.Write (data, 0, data.Length);
                zip.CloseEntry();
            }
        }

        static void CreateTcd3Fixture (string path)
        {
            const byte sectionKey = 1;
            const int sectionCount = 5;
            const int headerSize = 8 + sectionCount * 0x20;
            const int indexOffset = headerSize;
            var directoryName = EncodeTcdName ("dir", 4, sectionKey);
            var fileName = EncodeTcdName ("sample", 7, sectionKey);
            var payload = Encoding.UTF8.GetBytes ("migrated TCD fixture");
            var dataOffset = indexOffset + directoryName.Length + 0x10 + fileName.Length + 8;

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (Encoding.ASCII.GetBytes ("TCD3"));
                output.Write (1);
                for (var i = 0; i < sectionCount; ++i)
                {
                    if (i != 4)
                    {
                        output.Write (new byte[0x20]);
                        continue;
                    }
                    output.Write ((uint)payload.Length);
                    output.Write ((uint)indexOffset);
                    output.Write (1);
                    output.Write (directoryName.Length);
                    output.Write (1);
                    output.Write (fileName.Length);
                    output.Write (new byte[8]);
                }

                output.Write (directoryName);
                output.Write (1);
                output.Write (0);
                output.Write (0);
                output.Write (0);
                output.Write (fileName);
                output.Write ((uint)dataOffset);
                output.Write ((uint)(dataOffset + payload.Length));
                output.Write (payload);
            }
        }

        static byte[] EncodeTcdName (string name, int length, byte key)
        {
            var result = new byte[length];
            var bytes = Encoding.ASCII.GetBytes (name);
            Array.Copy (bytes, result, Math.Min (bytes.Length, length - 1));
            for (var i = 0; i < result.Length; ++i)
                result[i] += key;
            return result;
        }

        static void CreateMorningFixture (string path, byte[] key)
        {
            const int indexSize = 0x200;
            const int dataOffset = 8 + indexSize;
            var payload = Encoding.UTF8.GetBytes ("migrated Morning fixture");
            var index = new byte[indexSize];
            using (var indexOutput = new BinaryWriter (new MemoryStream (index), Encoding.UTF8, true))
            {
                indexOutput.Write (1);
                indexOutput.Write (16);
                indexOutput.Write (dataOffset);
                indexOutput.Write (payload.Length);
                indexOutput.Write (Encoding.ASCII.GetBytes ("sample.txt\0"));
            }
            for (var i = 0; i < index.Length; ++i)
                index[i] ^= key[i & (key.Length - 1)];

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (0x58668F8Bu);
                output.Write (1u);
                output.Write (index);
                output.Write (payload);
            }
        }

        [Fact]
        public void Bundled_xp3_profiles_survive_external_profile_loading ()
        {
            Assert.True (Xp3Opener.TryGetScheme ("11eyes", out var bundled));
            Assert.IsType<CxEncryption> (bundled);

            const string profiles = @"{
              ""schemaVersion"": 1,
              ""profiles"": [
                {
                  ""id"": ""external-merge-test"",
                  ""algorithm"": ""senren-cx"",
                  ""parameters"": {
                    ""cx"": {
                      ""mask"": 0,
                      ""offset"": 0,
                      ""prologOrder"": [],
                      ""oddBranchOrder"": [],
                      ""evenBranchOrder"": [],
                      ""controlBlock"": []
                    }
                  }
                }
              ]
            }";
            using (var input = new MemoryStream (Encoding.UTF8.GetBytes (profiles)))
                Xp3SchemeProfiles.Load (input);

            Assert.True (Xp3Opener.TryGetScheme ("external-merge-test", out var external));
            Assert.IsType<SenrenCxCrypt> (external);
            Assert.True (Xp3Opener.TryGetScheme ("11eyes", out _));
        }

        [Fact]
        public void Modern_formats_assembly_does_not_embed_legacy_formats_database ()
        {
            var resources = typeof(Xp3Opener).Assembly.GetManifestResourceNames();

            Assert.DoesNotContain (resources, name => name.EndsWith ("Formats.dat", System.StringComparison.Ordinal));
        }
    }
}
