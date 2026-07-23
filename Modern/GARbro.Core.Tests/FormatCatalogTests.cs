using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GameRes;
using GameRes.Cryptography;
using GameRes.Utility;
using System.Windows.Media;
using GameRes.Formats.KiriKiri;
using GameRes.Formats.GameSystem;
using GameRes.Formats.CatSystem;
using GameRes.Formats.Entis;
using GameRes.Formats.Morning;
using GameRes.Formats.MoonhirGames;
using GameRes.Formats.PkWare;
using GameRes.Formats.Yatagarasu;
using GameRes.Formats.TopCat;
using GameRes.Formats.FamilyAdvSystem;
using GameRes.Formats.Marble;
using GameRes.Formats.NitroPlus;
using GameRes.Formats.NScripter;
using GameRes.Formats.NSystem;
using GameRes.Formats.Tamamo;
using GameRes.Formats.LiveMaker;
using GameRes.Formats.Crowd;
using GameRes.Formats.Actgs;
using GameRes.Formats.BlackRainbow;
using GameRes.Formats.Will;
using GameRes.Formats.Mg;
using GameRes.Formats.Majiro;
using GameRes.Formats.FC01;
using GameRes.Formats.Cyberworks;
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
        public void Gal_format_loads_migrated_keys_and_reads_uncompressed_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<GalFormat> ().Single ();
            var scheme = Assert.IsType<GalScheme> (format.Scheme);
            Assert.Equal (3, scheme.KnownKeys.Count);
            Assert.Equal ("2011", scheme.KnownKeys["Grope ~Yami no Naka no Kotori-tachi~"]);

            var fixture = CreateGalFixture ();
            using (var input = new BinaryStream (new MemoryStream (fixture), "sample.gal"))
            {
                var decoded = ImageFormat.Read (input);
                Assert.NotNull (decoded);
                Assert.Equal ((uint)2, decoded.Width);
                Assert.Equal ((uint)1, decoded.Height);
                var pixels = new byte[8];
                decoded.Bitmap.CopyPixels (pixels, 8, 0);
                Assert.Equal (new byte[] { 0, 0, 255, 0, 255, 0, 0, 0 }, pixels);
            }
        }

        [Fact]
        public void Crz_format_loads_migrated_keys_and_reads_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<CrzFormat> ().Single ();
            var scheme = Assert.IsType<CrzScheme> (format.Scheme);
            Assert.Equal (2, scheme.KnownKeys.Count);
            Assert.Equal (0x24, scheme.KnownKeys["(C)CROWD MissYou"].Length);

            using (var input = new BinaryStream (new MemoryStream (CreateCrzFixture ()), "sample.crz"))
            {
                var decoded = ImageFormat.Read (input);
                Assert.NotNull (decoded);
                Assert.Equal ((uint)2, decoded.Width);
                Assert.Equal ((uint)1, decoded.Height);
                var pixels = new byte[4];
                decoded.Bitmap.CopyPixels (pixels, 4, 0);
                Assert.Equal (new byte[] { 0x1F, 0x00, 0x00, 0x7C }, pixels);
            }
        }

        [Fact]
        public void Desktop_audio_preview_formats_are_available ()
        {
            var tags = FormatCatalog.Instance.AudioFormats.Select (format => format.Tag).ToArray();

            Assert.Contains ("WAV", tags);
            Assert.Contains ("OGG", tags);
            Assert.Contains ("MP3", tags);
        }

        [Fact]
        public void Tink_audio_format_loads_migrated_keys_and_decodes_header ()
        {
            var format = FormatCatalog.Instance.AudioFormats.OfType<TinkAudio> ().Single ();
            var scheme = Assert.IsType<TinkAudioScheme> (format.Scheme);
            Assert.Equal (2, scheme.KnownKeys.Count);
            var key = scheme.KnownKeys[1735290707u];
            var plaintext = Enumerable.Range (0, 28).Select (value => (byte)(value * 3 + 1)).ToArray ();
            var source = new byte[4 + plaintext.Length];
            BitConverter.GetBytes (1735290707u).CopyTo (source, 0);
            for (int i = 0; i < plaintext.Length; ++i)
                source[4 + i] = (byte)(plaintext[i] ^ key[i % key.Length]);

            using (var input = new BinaryStream (new MemoryStream (source), "sample.j0"))
            {
                Assert.True (TinkDecoder.TryDecodeHeader (input, scheme.KnownKeys, out var header));
                Assert.Equal (source.Length, header.Length);
                Assert.Equal (new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' }, header.Take (4).ToArray ());
                Assert.Equal (plaintext, header.Skip (4).ToArray ());
            }
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
        public void Actgs_dat_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "DAT/ACTGS");
            var scheme = Assert.IsType<ActressScheme> (format.Scheme);
            Assert.Equal (6, scheme.KnownKeys.Length);
            Assert.True (scheme.KnownKeys[0].Length >= 4);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.dat");
                File.WriteAllBytes (archivePath, CreateActgsFixture (scheme.KnownKeys[0]));

                VFS.ChDir (archivePath);
                Assert.Equal ("DAT/ACTGS", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("hello", input.ReadToEnd ());
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
        public void Ads_format_loads_migrated_keys_and_opens_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ADS");
            var scheme = Assert.IsType<AdsScheme> (format.Scheme);
            Assert.Equal (2, scheme.KnownKeys.Count);
            Assert.Equal (256, scheme.KnownKeys["Soukan Yuugi 2"].Length);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.ads");
                File.WriteAllBytes (archivePath, CreateAdsFixture (scheme.KnownKeys["Soukan Yuugi 2"]));

                VFS.ChDir (archivePath);
                Assert.Equal ("ADS", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("ads fixture", input.ReadToEnd ());
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
        public void Arcg_format_loads_migrated_key_and_opens_inline_index_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARCG");
            var scheme = Assert.IsType<BmiScheme> (format.Scheme);
            Assert.Single (scheme.KnownKeys);
            Assert.Equal ("\u300E\u30DE\u30DE\u3055\u3093\u30D0\u30EC\u30FC((\u4E73\u3086\u308C\u307E\u3093\u305B\u30FC))\u300F",
                scheme.KnownKeys[1522388286u]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                File.WriteAllBytes (archivePath, CreateArcgFixture ());

                VFS.ChDir (archivePath);
                Assert.Equal ("ARCG", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("arcg fixture", input.ReadToEnd ());
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
        public void Mgpk_format_loads_migrated_key_and_opens_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "MGPK");
            var scheme = Assert.IsType<MgScheme> (format.Scheme);
            Assert.Equal (4, scheme.KnownKeys.Count);
            Assert.Equal ("5WW6Gj3Gf55GFYk=", Convert.ToBase64String (scheme.KnownKeys["Cartagra"]));

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.pac"] = "Cartagra" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.pac");
                File.WriteAllBytes (archivePath, CreateMgpkFixture (scheme.KnownKeys["Cartagra"]));

                VFS.ChDir (archivePath);
                Assert.Equal ("MGPK", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("mgpk fixture", input.ReadToEnd ());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Rct_format_loads_migrated_password_and_reads_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<RctFormat> ().Single ();
            var scheme = Assert.IsType<RctScheme> (format.Scheme);
            Assert.Equal (33, scheme.KnownKeys.Count);
            Assert.Equal ("\u59C9\u30CB\u30E2\u30DE\u30B1\u30BA", scheme.KnownKeys["Ane ni mo Makezu"]);

            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.rct"] = "Ane ni mo Makezu" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                using (var input = new BinaryStream (
                    new MemoryStream (CreateRctFixture (scheme.KnownKeys["Ane ni mo Makezu"])), "sample.rct"))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.NotNull (decoded);
                    Assert.Equal ((uint)1, decoded.Width);
                    Assert.Equal ((uint)1, decoded.Height);
                    var pixels = new byte[3];
                    decoded.Bitmap.CopyPixels (pixels, 3, 0);
                    Assert.Equal (new byte[] { 0x10, 0x20, 0x30 }, pixels);
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
            }
        }

        [Fact]
        public void Mcg_format_loads_migrated_key_and_reads_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<McgFormat> ().Single ();
            var scheme = Assert.IsType<McgScheme> (format.Scheme);
            Assert.Equal (24, scheme.KnownKeys.Count);
            Assert.Equal ((byte)1, scheme.KnownKeys["Echo"]);

            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.mcg"] = "Echo" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                using (var input = new BinaryStream (
                    new MemoryStream (CreateMcgFixture (scheme.KnownKeys["Echo"])), "sample.mcg"))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.NotNull (decoded);
                    Assert.Equal ((uint)1, decoded.Width);
                    Assert.Equal ((uint)1, decoded.Height);
                    var pixels = new byte[4];
                    decoded.Bitmap.CopyPixels (pixels, 4, 0);
                    Assert.Equal (new byte[] { 0x10, 0x20, 0x30, 0 }, pixels);
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
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
        public void Moonhir_fpk_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "FPK/MOONHIR");
            Assert.IsType<FpkOpener> (format);
            var scheme = Assert.IsType<Fpk0100Scheme> (format.Scheme);
            Assert.Equal (28, scheme.KnownKeys.Length);
            Assert.Equal (0u, scheme.KnownKeys[0]);
            Assert.Equal (4064587902u, scheme.KnownKeys[^1]);

            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.fpk");
                CreateFpkFixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("FPK/MOONHIR", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated FPK fixture", input.ReadToEnd());
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
        public void GameSystem_cmp_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "CMP");
            Assert.IsType<CmpOpener> (format);
            Assert.Equal (2, CmpOpener.KnownKeys.Count);
            Assert.Equal (16, CmpOpener.KnownKeys["Summer Radish Vacation!! 2"].Length);
            Assert.Equal (16, CmpOpener.KnownKeys["Imouto de Ikou!"].Length);

            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.cmp");
                CreateCmpFixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("CMP", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated CMP fixture", input.ReadToEnd());
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
        public void Yatagarasu_pkg_format_uses_migrated_key_to_open_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "PKG/2");
            Assert.IsType<Pkg2Opener> (format);
            var scheme = Assert.IsType<PkgScheme> (format.Scheme);
            var key = scheme.KnownKeys["Seisai no Resonance"];
            Assert.Equal (8, key.Length);

            var tempDirectory = Path.Combine (Path.GetTempPath(), Path.GetRandomFileName());
            var previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.pkg");
                CreatePkgFixture (archivePath, key);

                VFS.ChDir (archivePath);
                Assert.Equal ("PKG/2", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("PKG fixture data", input.ReadToEnd());
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
        public void Csaf_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "CSAF");
            Assert.IsType<CsafOpener> (format);
            var scheme = Assert.IsType<FamilyAdvScheme> (format.Scheme);
            Assert.Single (scheme.KnownKeys);
            Assert.Equal ("招子", scheme.KnownKeys["Nanairo * Clip ~Saigo no Stage~"]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.csaf");
                CreateCsafFixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("CSAF", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated CSAF fixture", input.ReadToEnd());
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
        public void Marble_mbl_format_uses_migrated_key_for_script_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "MBL");
            Assert.IsType<MblOpener> (format);
            var scheme = Assert.IsType<MblScheme> (format.Scheme);
            Assert.Equal (58, scheme.KnownKeys.Count);
            Assert.Equal ("amai_seikatu", scheme.KnownKeys["Amai Seikatsu"]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.mbl"] = "Amai Seikatsu" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.mbl");
                CreateMblFixture (archivePath, scheme.KnownKeys["Amai Seikatsu"]);

                VFS.ChDir (archivePath);
                Assert.Equal ("MBL", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.s", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated MBL fixture", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Nitroplus_npk_format_uses_migrated_key_for_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "NPK");
            Assert.IsType<NpkOpener> (format);
            var scheme = Assert.IsType<Npk2Scheme> (format.Scheme);
            var key = scheme.KnownKeys["Sonicomi"];
            Assert.Equal (4, scheme.KnownKeys.Count);
            Assert.Equal (32, key.Length);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.npk"] = "Sonicomi" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.npk");
                CreateNpkFixture (archivePath, key);

                VFS.ChDir (archivePath);
                Assert.Equal ("NPK", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                Assert.Equal (32u, entry.Size);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated NPK fixture", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Tamamo_pck_format_uses_migrated_key_for_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "PCK/TAMAMO");
            Assert.IsType<PckOpener> (format);
            var scheme = Assert.IsType<PckScheme> (format.Scheme);
            var key = scheme.KnownKeys["Boukensha no Machi o Tsukurou! 2"];
            Assert.Equal (4, scheme.KnownKeys.Count);
            Assert.Equal (10, key.Length);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.pck"] = "Boukensha no Machi o Tsukurou! 2" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.pck");
                CreatePckFixture (archivePath, key);
                Assert.Equal ("Boukensha no Machi o Tsukurou! 2", FormatCatalog.Instance.LookupGame (archivePath));
                using (var view = new ArcView (archivePath))
                {
                    Assert.Equal (0x4B434150u, view.View.ReadUInt32 (0));
                    Assert.True (view.View.AsciiEqual (4, "_FILE001"));
                    Assert.Equal (1, view.View.ReadInt32 (0xC));
                    Assert.Equal (24u, view.View.ReadUInt32 (0x10));
                    var decodedIndex = view.View.ReadBytes (0x14, 24);
                    new Blowfish (key).Decipher (decodedIndex, decodedIndex.Length);
                    Assert.Equal ((uint)20, decodedIndex.ToUInt32 (0));
                    Assert.Equal ((uint)24, decodedIndex.ToUInt32 (15));
                    using (var parsed = format.TryOpen (view))
                        Assert.NotNull (parsed);
                }

                VFS.ChDir (archivePath);
                Assert.Equal ("PCK/TAMAMO", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated PCK fixture", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Ns2_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "NS2");
            Assert.IsType<Ns2Opener> (format);
            var scheme = Assert.IsType<NsaScheme> (format.Scheme);
            Assert.Equal (6, scheme.KnownKeys.Count);
            Assert.Contains ("Daydream Believer", scheme.KnownKeys.Keys);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.ns2");
                CreateNs2Fixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("NS2", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated NS2 fixture", input.ReadToEnd());
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
        public void Nsa_format_uses_migrated_key_for_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "NSA");
            Assert.IsType<NsaOpener> (format);
            var scheme = Assert.IsType<NsaScheme> (format.Scheme);
            var password = scheme.KnownKeys["Chou Gedou Yuusha"];
            Assert.Equal (10, scheme.KnownKeys.Count);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.nsa"] = "Chou Gedou Yuusha" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.nsa");
                CreateNsaFixture (archivePath, password);
                Assert.NotEqual (0, BitConverter.ToInt16 (File.ReadAllBytes (archivePath), 0));
                using (var view = new ArcView (archivePath))
                using (var decrypted = new EncryptedViewStream (view, Encoding.ASCII.GetBytes (password)))
                {
                    var header = new byte[30];
                    Assert.Equal (30, decrypted.Read (header, 0, header.Length));
                    Assert.Equal ((byte)0x14, header[25]);
                }
                using (var view = new ArcView (archivePath))
                using (var decrypted = new EncryptedViewStream (view, Encoding.ASCII.GetBytes (password)))
                using (var reader = new ArcView.Reader (decrypted))
                {
                    Assert.Equal (1, Binary.BigEndian (reader.ReadInt16 ()));
                    Assert.Equal (30u, Binary.BigEndian (reader.ReadUInt32 ()));
                    Assert.Equal ("sample.txt", decrypted.ReadCString ());
                    Assert.Equal ((byte)0, reader.ReadByte ());
                    Assert.Equal (0u, Binary.BigEndian (reader.ReadUInt32 ()));
                    Assert.Equal (20u, Binary.BigEndian (reader.ReadUInt32 ()));
                }
                using (var view = new ArcView (archivePath))
                using (var parsed = format.TryOpen (view))
                {
                    Assert.NotNull (parsed);
                    Assert.Equal ((uint)20, Assert.Single (parsed.Dir).Size);
                }

                VFS.ChDir (archivePath);
                Assert.Equal ("NSA", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                Assert.True (entry.Size > 0);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated NSA fixture", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Fjsys_format_uses_migrated_password_for_msd_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "FJSYS");
            Assert.IsType<FjsysOpener> (format);
            var scheme = Assert.IsType<FjsysScheme> (format.Scheme);
            var password = scheme.MsdPasswords["Aki no Urara no ~Akaneiro Shoutengai~"];
            Assert.Equal (22, scheme.MsdPasswords.Count);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.fjsys"] = "Aki no Urara no ~Akaneiro Shoutengai~" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.fjsys");
                CreateFjsysFixture (archivePath, password);

                VFS.ChDir (archivePath);
                Assert.Equal ("FJSYS", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.msd", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated FJSYS fixture", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Int_format_loads_migrated_key_map_and_opens_plain_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "INT");
            Assert.IsType<IntOpener> (format);
            var scheme = Assert.IsType<IntScheme> (format.Scheme);
            Assert.Equal (24, scheme.KnownKeys.Count);
            Assert.Equal (4081182131u, scheme.KnownKeys["Amakano"].Key);
            Assert.Equal ("NXT-M81ERGNE", scheme.KnownKeys["Amakano"].Passphrase);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.int");
                CreateIntPlainFixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("INT", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated INT fixture", input.ReadToEnd());
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
        public void Int_format_uses_migrated_key_for_encrypted_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "INT");
            var scheme = Assert.IsType<IntScheme> (format.Scheme);
            var mainKey = scheme.KnownKeys["Amakano"].Key;
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.int"] = "Amakano" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.int");
                CreateIntEncryptedFixture (archivePath, mainKey);

                VFS.ChDir (archivePath);
                Assert.Equal ("INT", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.ASCII))
                    Assert.Equal ("migrated INT ok!", input.ReadToEnd());
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Noa_format_loads_nested_key_map_and_opens_raw_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "NOA");
            Assert.IsType<NoaOpener> (format);
            var scheme = Assert.IsType<NoaScheme> (format.Scheme);
            Assert.Equal (26, scheme.KnownKeys.Count);
            Assert.Equal ("convini_cat", scheme.KnownKeys["Konneko"]["script.noa"]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.noa");
                CreateNoaRawFixture (archivePath);

                VFS.ChDir (archivePath);
                Assert.Equal ("NOA", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.UTF8))
                    Assert.Equal ("migrated NOA fixture", input.ReadToEnd());
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
        public void Noa_format_uses_nested_password_for_bshf_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat>()
                .Single (item => item.Tag == "NOA");
            var scheme = Assert.IsType<NoaScheme> (format.Scheme);
            var password = scheme.KnownKeys["Konneko"]["script.noa"];
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof(FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["script.noa"] = "Konneko" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "script.noa");
                CreateNoaBshfFixture (archivePath, password);

                VFS.ChDir (archivePath);
                Assert.Equal ("NOA", VFS.CurrentArchive.Tag);
                var entry = Assert.Single (VFS.CurrentArchive.Dir);
                Assert.Equal ("sample.txt", entry.Name);
                using (var input = new StreamReader (VFS.CurrentArchive.OpenEntry (entry), Encoding.ASCII))
                    Assert.Equal ("migrated NOA BSHF fixture", input.ReadToEnd().TrimEnd ('\0'));
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
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

        static byte[] CreateGalFixture ()
        {
            using (var output = new MemoryStream ())
            using (var writer = new BinaryWriter (output, Encoding.UTF8, true))
            {
                writer.Write (new byte[] { (byte)'G', (byte)'a', (byte)'l', (byte)'e', (byte)'1', (byte)'0', (byte)'3' });
                writer.Write (0x28);
                var header = new byte[0x28];
                BitConverter.GetBytes (103).CopyTo (header, 0);
                BitConverter.GetBytes (2u).CopyTo (header, 4);
                BitConverter.GetBytes (1u).CopyTo (header, 8);
                BitConverter.GetBytes (24).CopyTo (header, 0xC);
                BitConverter.GetBytes (1).CopyTo (header, 0x10);
                writer.Write (header);
                writer.Write (0u);
                writer.Write (0u);
                writer.Write (new byte[9]);
                writer.Write (1);
                writer.Write (2);
                writer.Write (1);
                writer.Write (24);
                writer.Write (0);
                writer.Write (0);
                writer.Write ((byte)1);
                writer.Write (-1);
                writer.Write (0xFF);
                writer.Write ((byte)0);
                writer.Write (0u);
                writer.Write (8);
                writer.Write (new byte[] { 0, 0, 255, 0, 255, 0, 0, 0 });
                writer.Write (0);
                writer.Flush ();
                return output.ToArray ();
            }
        }

        static byte[] CreateCrzFixture ()
        {
            const string id = "(C)CROWD MissYou";
            var key = Convert.FromBase64String ("Af0QJBAZz8/f15t535F2Pf9C2CDfxuQREByk2d/R32D/QYTJ");
            var idBytes = Encoding.ASCII.GetBytes (id);
            var seed = Enumerable.Range (0, 16).Select (value => (byte)value).ToArray ();
            var plainHeader = new byte[0x24];
            BitConverter.GetBytes (2u).CopyTo (plainHeader, 4);
            BitConverter.GetBytes (1u).CopyTo (plainHeader, 0x10);
            var encryptedHeader = new byte[plainHeader.Length];
            for (var i = 0; i < encryptedHeader.Length; ++i)
                encryptedHeader[i] = (byte)(plainHeader[i] ^ key[i] ^ seed[i & 0xF]);

            var unpacked = new List<byte> (idBytes.Length + 1 + seed.Length + encryptedHeader.Length + 4);
            unpacked.AddRange (idBytes);
            unpacked.Add (0);
            unpacked.AddRange (seed);
            unpacked.AddRange (encryptedHeader);
            unpacked.AddRange (new byte[] { 0x1F, 0x00, 0x00, 0x7C });

            using (var output = new MemoryStream ())
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (new byte[0xE]);
                for (var offset = 0; offset < unpacked.Count; offset += 8)
                {
                    var count = Math.Min (8, unpacked.Count - offset);
                    writer.Write ((byte)0xFF);
                    writer.Write (unpacked.GetRange (offset, count).ToArray ());
                }
                writer.Flush ();
                var result = output.ToArray ();
                result[0] = (byte)'S';
                result[1] = (byte)'Z';
                result[2] = (byte)'D';
                result[3] = (byte)'D';
                return result;
            }
        }

        static byte[] CreateActgsFixture (byte[] key)
        {
            const int firstOffset = 0x30;
            var index = new byte[0x20];
            BitConverter.GetBytes ((uint)firstOffset).CopyTo (index, 0);
            BitConverter.GetBytes (5u).CopyTo (index, 4);
            Encoding.ASCII.GetBytes ("sample.txt").CopyTo (index, 8);
            for (var i = 0; i < index.Length; ++i)
                index[i] ^= key[i % key.Length];
            var payload = Encoding.ASCII.GetBytes ("hello");
            for (var i = 0; i < payload.Length; ++i)
                payload[i] ^= key[i % key.Length];

            using (var output = new MemoryStream ())
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (1);
                writer.Write (0);
                writer.Write (0);
                writer.Write (0);
                writer.Write (index);
                writer.Write (payload);
                writer.Flush ();
                return output.ToArray ();
            }
        }

        static byte[] CreateAdsFixture (byte[] key)
        {
            var plaintext = new byte[0x4F];
            BitConverter.GetBytes (2u).CopyTo (plaintext, 0);
            BitConverter.GetBytes (1).CopyTo (plaintext, 8);
            BitConverter.GetBytes (0x20u).CopyTo (plaintext, 12);
            BitConverter.GetBytes (0u).CopyTo (plaintext, 16);
            Encoding.ASCII.GetBytes ("sample.txt").CopyTo (plaintext, 0x20);
            BitConverter.GetBytes (11u).CopyTo (plaintext, 0x40);
            Encoding.ASCII.GetBytes ("ads fixture").CopyTo (plaintext, 0x44);
            EncryptAdsBytes (plaintext, key);
            return plaintext;
        }

        static byte[] CreateArcgFixture ()
        {
            var index = new byte[0x20];
            index[0] = 1;
            BitConverter.GetBytes (0x2C).CopyTo (index, 1);
            BitConverter.GetBytes (1).CopyTo (index, 5);
            index[12] = 11;
            Encoding.ASCII.GetBytes ("sample.txt").CopyTo (index, 13);
            BitConverter.GetBytes (0x40u).CopyTo (index, 23);
            BitConverter.GetBytes (12u).CopyTo (index, 27);

            var result = new byte[0x40 + 12];
            Encoding.ASCII.GetBytes ("ARCG").CopyTo (result, 0);
            BitConverter.GetBytes (0x10000u).CopyTo (result, 4);
            BitConverter.GetBytes (0x20).CopyTo (result, 8);
            BitConverter.GetBytes (0x20).CopyTo (result, 0xC);
            BitConverter.GetBytes ((ushort)1).CopyTo (result, 0x10);
            BitConverter.GetBytes (1).CopyTo (result, 0x12);
            index.CopyTo (result, 0x20);
            Encoding.ASCII.GetBytes ("arcg fixture").CopyTo (result, 0x40);
            return result;
        }

        static byte[] CreateMgpkFixture (byte[] key)
        {
            const string name = "sample.txt";
            var plain = Encoding.UTF8.GetBytes ("mgpk fixture");
            var packed = new byte[plain.Length + 1];
            packed[0] = (byte)(plain.Length - 1);
            plain.CopyTo (packed, 1);
            EncryptMgpkBytes (packed, key);

            var result = new byte[0x3C + packed.Length];
            BitConverter.GetBytes (0x4B50474Du).CopyTo (result, 0);
            BitConverter.GetBytes (1).CopyTo (result, 4);
            BitConverter.GetBytes (1).CopyTo (result, 8);
            result[0x0C] = (byte)name.Length;
            Encoding.UTF8.GetBytes (name).CopyTo (result, 0x0D);
            BitConverter.GetBytes (0x3Cu).CopyTo (result, 0x2C);
            BitConverter.GetBytes ((uint)packed.Length).CopyTo (result, 0x30);
            packed.CopyTo (result, 0x3C);
            return result;
        }

        static void EncryptMgpkBytes (byte[] data, byte[] key)
        {
            key = (byte[])key.Clone ();
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] ^= key[i % key.Length];
                key[i % key.Length] += 27;
            }
        }

        static byte[] CreateRctFixture (string password)
        {
            var pixels = new byte[] { 0x10, 0x20, 0x30 };
            var key = CreateRctKey (password);
            for (var i = 0; i < pixels.Length; ++i)
                pixels[i] ^= key[i];

            var result = new byte[0x14 + pixels.Length];
            BitConverter.GetBytes (0x9A925A98u).CopyTo (result, 0);
            result[4] = (byte)'T';
            result[5] = (byte)'S';
            result[6] = (byte)'0';
            result[7] = (byte)'0';
            BitConverter.GetBytes (1u).CopyTo (result, 8);
            BitConverter.GetBytes (1u).CopyTo (result, 12);
            BitConverter.GetBytes (pixels.Length).CopyTo (result, 16);
            pixels.CopyTo (result, 0x14);
            return result;
        }

        static byte[] CreateRctKey (string password)
        {
            var bytes = Encodings.cp932.GetBytes (password);
            var crc = Crc32.Compute (bytes, 0, bytes.Length);
            var key = new byte[0x400];
            for (var i = 0; i < 0x100; ++i)
            {
                var value = crc ^ Crc32.Table[(int)((i + crc) & 0xFF)];
                BitConverter.GetBytes (value).CopyTo (key, i * 4);
            }
            return key;
        }

        static byte[] CreateMcgFixture (byte key)
        {
            var packed = new byte[] { 0x0F, 0x10, 0x20, 0x30, 0x00, 0x00 };
            var remaining = packed.Length - 1;
            for (var i = 0; i < packed.Length - 1; ++i)
            {
                packed[i] = Binary.RotByteR ((byte)(packed[i] ^ key), 1);
                key = (byte)(key + remaining--);
            }

            var result = new byte[0x40 + packed.Length];
            BitConverter.GetBytes (0x2047434Du).CopyTo (result, 0);
            result[4] = (byte)'1';
            result[5] = (byte)'.';
            result[6] = (byte)'0';
            result[7] = (byte)'1';
            BitConverter.GetBytes (0x40).CopyTo (result, 0x10);
            BitConverter.GetBytes (1u).CopyTo (result, 0x1C);
            BitConverter.GetBytes (1u).CopyTo (result, 0x20);
            BitConverter.GetBytes (24).CopyTo (result, 0x24);
            BitConverter.GetBytes (result.Length).CopyTo (result, 0x38);
            packed.CopyTo (result, 0x40);
            return result;
        }

        static void EncryptAdsBytes (byte[] data, byte[] key)
        {
            for (var block = 0; block < data.Length; block += 1024)
            {
                var blockNumber = block / 1024;
                var number = new byte[8];
                BitConverter.GetBytes (blockNumber).CopyTo (number, 0);
                var md5 = MD5.HashData (number);
                var sha1 = SHA1.HashData (number);
                var hmacKey = new byte[16];
                for (var i = 0; i < hmacKey.Length; ++i)
                    hmacKey[i] = (byte)(md5[i] ^ sha1[i]);
                byte[] hmac;
                using (var hash = new HMACSHA512 (hmacKey))
                    hmac = hash.ComputeHash (key);

                var map = Enumerable.Range (0, 256).ToArray ();
                byte index = 0;
                var h = 0;
                for (var i = 0; i < 256; ++i)
                {
                    if (h == hmac.Length)
                        h = 0;
                    var tmp = map[i];
                    index = (byte)(tmp + hmac[h++] + index);
                    map[i] = map[index];
                    map[index] = tmp;
                }
                var i0 = 0;
                var i1 = 0;
                for (var i = 0; i < 300; ++i)
                {
                    i0 = (i0 + 1) & 0xFF;
                    var tmp = map[i0];
                    i1 = (i1 + tmp) & 0xFF;
                    map[i0] = map[i1];
                    map[i1] = tmp;
                }
                var length = Math.Min (1024, data.Length - block);
                for (var i = 0; i < length; ++i)
                {
                    i0 = (i0 + 1) & 0xFF;
                    var tmp = map[i0];
                    i1 = (i1 + tmp) & 0xFF;
                    map[i0] = map[i1];
                    map[i1] = tmp;
                    data[block + i] ^= (byte)map[(map[i0] + tmp) & 0xFF];
                }
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

        static void CreateFpkFixture (string path)
        {
            const int indexOffset = 0x20;
            const int dataOffset = 0x40;
            var payload = Encoding.UTF8.GetBytes ("migrated FPK fixture");
            var name = new byte[12];
            Array.Copy (Encoding.ASCII.GetBytes ("sample.txt"), name, 10);

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (Encoding.ASCII.GetBytes ("FPK\0"));
                output.Write (Encoding.ASCII.GetBytes ("0100"));
                output.Write ((uint)indexOffset);
                output.Write (1);
                output.Write (new byte[indexOffset - 0x10]);
                output.Write (0u);
                output.Write ((uint)dataOffset);
                output.Write ((uint)payload.Length);
                output.Write (name);
                output.Write (new byte[dataOffset - (indexOffset + 0x18)]);
                output.Write (payload);
            }
        }

        static void CreateCmpFixture (string path)
        {
            const int dataOffset = 0x10;
            var payload = Encoding.UTF8.GetBytes ("migrated CMP fixture");
            const string name = "sample.txt";
            byte[] index;
            using (var indexStream = new MemoryStream())
            using (var indexOutput = new BinaryWriter (indexStream, Encoding.UTF8, true))
            {
                indexOutput.Write ((uint)dataOffset);
                indexOutput.Write ((byte)name.Length);
                indexOutput.Write ((byte)0);
                indexOutput.Write (0u);
                indexOutput.Write (Encoding.Unicode.GetBytes (name));
                indexOutput.Write ((uint)(dataOffset + payload.Length));
                indexOutput.Write (0u);
                indexOutput.Write ((byte)0);
                index = indexStream.ToArray();
            }

            var indexOffset = dataOffset + payload.Length;
            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (new byte[dataOffset]);
                output.Write (payload);
                output.Write (index.Length);
                output.Write ((byte)(index.Length - 1));
                output.Write (index);
                output.Write (Encoding.ASCII.GetBytes ("PACK"));
                output.Write ((uint)indexOffset);
            }
        }

        static void CreatePkgFixture (string path, uint[] key)
        {
            const int indexOffset = 8;
            const int indexSize = 0x80;
            const int dataOffset = indexOffset + indexSize;
            var payload = Encoding.UTF8.GetBytes ("PKG fixture data");
            var index = new byte[indexSize];
            using (var indexOutput = new BinaryWriter (new MemoryStream (index), Encoding.UTF8, true))
            {
                var name = new byte[0x74];
                Array.Copy (Encoding.ASCII.GetBytes ("sample.txt\0"), name, 11);
                indexOutput.Write (name);
                indexOutput.Write ((uint)payload.Length);
                indexOutput.Write ((uint)dataOffset);
                indexOutput.Write ((uint)payload.Length);
            }
            XorPkgBytes (index, key);

            var encryptedPayload = (byte[])payload.Clone();
            XorPkgWords (encryptedPayload, key, (payload.Length / 4) & 7);
            var fileLength = dataOffset + encryptedPayload.Length;
            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (key[0] ^ (uint)fileLength);
                output.Write (1u ^ key[0]);
                output.Write (index);
                output.Write (encryptedPayload);
            }
        }

        static void CreateCsafFixture (string path)
        {
            const int indexSize = 0xFE0;
            const int dataOffset = 0x2000;
            const string content = "migrated CSAF fixture";
            var nameBytes = Encoding.Unicode.GetBytes ("sample.txt");
            var namesSize = nameBytes.Length + 2 + 8;
            var index = new byte[indexSize + namesSize];
            Buffer.BlockCopy (nameBytes, 0, index, indexSize, nameBytes.Length);
            BitConverter.GetBytes ((uint)(dataOffset >> 12)).CopyTo (index, 0x10);
            BitConverter.GetBytes ((uint)Encoding.UTF8.GetByteCount (content)).CopyTo (index, 0x14);
            var payload = Encoding.UTF8.GetBytes (content);
            var file = new byte[dataOffset + payload.Length];
            Buffer.BlockCopy (index, 0, file, 0x20, index.Length);
            Buffer.BlockCopy (payload, 0, file, dataOffset, payload.Length);
            BitConverter.GetBytes (0x46415343u).CopyTo (file, 0);
            BitConverter.GetBytes (0x10000u).CopyTo (file, 4);
            BitConverter.GetBytes (1).CopyTo (file, 8);
            BitConverter.GetBytes ((uint)namesSize).CopyTo (file, 12);
            using (var md5 = MD5.Create())
                Buffer.BlockCopy (md5.ComputeHash (index), 0, file, 0x10, 0x10);
            File.WriteAllBytes (path, file);
        }

        static void CreateMblFixture (string path, string password)
        {
            const int indexOffset = 8;
            const int filenameLength = 0x10;
            const int dataOffset = 0x20;
            const string content = "migrated MBL fixture";
            var payload = Encoding.UTF8.GetBytes (content);
            var key = Encoding.ASCII.GetBytes (password);
            for (var i = 0; i < payload.Length; ++i)
                payload[i] ^= key[i % key.Length];

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (1);
                output.Write (filenameLength);
                var name = new byte[filenameLength];
                Array.Copy (Encoding.ASCII.GetBytes ("sample.s"), name, 8);
                output.Write (name);
                output.Write ((uint)dataOffset);
                output.Write ((uint)payload.Length);
                output.Write (new byte[dataOffset - (indexOffset + filenameLength + 8)]);
                output.Write (payload);
            }
        }

        static void CreateNpkFixture (string path, byte[] key)
        {
            const int headerSize = 0x20;
            const int dataOffset = 0x100;
            const string content = "migrated NPK fixture";
            var payload = Encoding.UTF8.GetBytes (content);
            var iv = Enumerable.Range (0, 0x10).Select (value => (byte)value).ToArray ();
            byte[] encryptedPayload;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor())
                    encryptedPayload = encryptor.TransformFinalBlock (payload, 0, payload.Length);
            }

            byte[] index;
            using (var indexStream = new MemoryStream())
            using (var indexOutput = new BinaryWriter (indexStream, Encoding.UTF8, true))
            {
                var name = Encoding.UTF8.GetBytes ("sample.txt");
                indexOutput.Write ((byte)0);
                indexOutput.Write ((ushort)name.Length);
                indexOutput.Write (name);
                indexOutput.Write ((uint)payload.Length);
                indexOutput.Write (new byte[0x20]);
                indexOutput.Write (1);
                indexOutput.Write ((long)dataOffset);
                indexOutput.Write ((uint)encryptedPayload.Length);
                indexOutput.Write ((uint)payload.Length);
                indexOutput.Write ((uint)payload.Length);
                index = indexStream.ToArray ();
            }
            byte[] encryptedIndex;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor())
                    encryptedIndex = encryptor.TransformFinalBlock (index, 0, index.Length);
            }

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (0x324B504Eu);
                output.Write (new byte[4]);
                output.Write (iv);
                output.Write (1);
                output.Write ((uint)encryptedIndex.Length);
                output.Write (encryptedIndex);
                output.Write (new byte[dataOffset - headerSize - encryptedIndex.Length]);
                output.Write (encryptedPayload);
            }
        }

        static void CreatePckFixture (string path, byte[] key)
        {
            const string content = "migrated PCK fixture";
            var payload = Encoding.UTF8.GetBytes (content);
            var encryptedPayload = new byte[(payload.Length + 7) & ~7];
            Buffer.BlockCopy (payload, 0, encryptedPayload, 0, payload.Length);
            var bf = new Blowfish (key);
            EncipherPckBytes (bf, encryptedPayload);

            byte[] index;
            using (var indexStream = new MemoryStream())
            using (var indexOutput = new BinaryWriter (indexStream, Encoding.UTF8, true))
            {
                indexOutput.Write ((uint)payload.Length);
                indexOutput.Write (Encoding.ASCII.GetBytes ("sample.txt\0"));
                indexOutput.Write ((uint)encryptedPayload.Length);
                index = indexStream.ToArray ();
            }
            Array.Resize (ref index, (index.Length + 7) & ~7);
            EncipherPckBytes (bf, index);

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (0x4B434150u);
                output.Write (Encoding.ASCII.GetBytes ("_FILE001"));
                output.Write (1);
                output.Write ((uint)index.Length);
                output.Write (index);
                output.Write ((byte)1);
                output.Write (encryptedPayload);
            }
        }

        static void CreateNs2Fixture (string path)
        {
            const int baseOffset = 0x20;
            var payload = Encoding.UTF8.GetBytes ("migrated NS2 fixture");
            using (var output = new BinaryWriter (File.Create (path), Encoding.GetEncoding (932)))
            {
                output.Write ((uint)baseOffset);
                output.Write (Encoding.ASCII.GetBytes ("\"sample.txt\""));
                output.Write ((uint)payload.Length);
                output.Write (new byte[baseOffset - 4 - 16]);
                output.Write (payload);
            }
        }

        static void CreateNsaFixture (string path, string password)
        {
            const int baseOffset = 30;
            var payload = Encoding.UTF8.GetBytes ("migrated NSA fixture");
            byte[] plain;
            using (var stream = new MemoryStream())
            using (var output = new BinaryWriter (stream, Encoding.UTF8, true))
            {
                output.Write (new byte[] { 0, 1 });
                output.Write (new byte[] { 0, 0, 0, baseOffset });
                output.Write (Encoding.ASCII.GetBytes ("sample.txt\0"));
                output.Write ((byte)0);
                output.Write (new byte[4]);
                output.Write (new byte[] {
                    (byte)(payload.Length >> 24), (byte)(payload.Length >> 16),
                    (byte)(payload.Length >> 8), (byte)payload.Length,
                });
                output.Write (new byte[] {
                    (byte)(payload.Length >> 24), (byte)(payload.Length >> 16),
                    (byte)(payload.Length >> 8), (byte)payload.Length,
                });
                output.Write (payload);
                plain = stream.ToArray ();
            }
            EncryptNsaBytes (plain, Encoding.ASCII.GetBytes (password));
            File.WriteAllBytes (path, plain);
        }

        static void CreateFjsysFixture (string path, string password)
        {
            const int indexOffset = 0x54;
            var name = Encoding.ASCII.GetBytes ("sample.msd\0");
            var payload = Encoding.UTF8.GetBytes ("migrated FJSYS fixture");
            var dataOffset = indexOffset + 0x10 + name.Length;
            var encrypted = EncryptMsdBytes (payload, password);
            var data = new byte[dataOffset + encrypted.Length];
            Encoding.ASCII.GetBytes ("FJSYS").CopyTo (data, 0);
            BitConverter.GetBytes ((uint)name.Length).CopyTo (data, 0xC);
            BitConverter.GetBytes (1).CopyTo (data, 0x10);
            BitConverter.GetBytes (0).CopyTo (data, indexOffset);
            BitConverter.GetBytes ((uint)encrypted.Length).CopyTo (data, indexOffset + 4);
            BitConverter.GetBytes ((long)dataOffset).CopyTo (data, indexOffset + 8);
            name.CopyTo (data, indexOffset + 0x10);
            encrypted.CopyTo (data, dataOffset);
            File.WriteAllBytes (path, data);
        }

        static void CreateIntPlainFixture (string path)
        {
            const int indexOffset = 8;
            const int nameSize = 0x20;
            var payload = Encoding.UTF8.GetBytes ("migrated INT fixture");
            var dataOffset = indexOffset + nameSize + 8;
            var data = new byte[dataOffset + payload.Length];
            BitConverter.GetBytes (0x0046494Bu).CopyTo (data, 0);
            BitConverter.GetBytes (1).CopyTo (data, 4);
            Encoding.ASCII.GetBytes ("sample.txt").CopyTo (data, indexOffset);
            BitConverter.GetBytes ((uint)dataOffset).CopyTo (data, indexOffset + nameSize);
            BitConverter.GetBytes ((uint)payload.Length).CopyTo (data, indexOffset + nameSize + 4);
            payload.CopyTo (data, dataOffset);
            File.WriteAllBytes (path, data);
        }

        static void CreateIntEncryptedFixture (string path, uint mainKey)
        {
            const uint seed = 0x12345678;
            const int firstEntryOffset = 8;
            const int secondEntryOffset = firstEntryOffset + 0x48;
            const int dataOffset = 0xA0;
            var plainPayload = Encoding.ASCII.GetBytes ("migrated INT ok!");
            var twister = new MersenneTwister (seed);
            var blowfishKey = BitConverter.GetBytes (twister.Rand ());
            var blowfish = new Blowfish (blowfishKey);
            var encryptedPayload = EncipherBlowfish (blowfish, plainPayload);
            var data = new byte[dataOffset + encryptedPayload.Length];
            BitConverter.GetBytes (0x0046494Bu).CopyTo (data, 0);
            BitConverter.GetBytes (2).CopyTo (data, 4);
            Encoding.ASCII.GetBytes ("__key__.dat\0").CopyTo (data, firstEntryOffset);
            BitConverter.GetBytes (seed).CopyTo (data, firstEntryOffset + 0x44);

            twister.SRand (mainKey + 1);
            var nameKey = twister.Rand ();
            EncipherIntName ("sample.txt", nameKey).CopyTo (data, secondEntryOffset);
            uint encodedOffset = dataOffset;
            var encodedSize = (uint)encryptedPayload.Length;
            EncipherBlowfishWords (blowfish, ref encodedOffset, ref encodedSize);
            encodedOffset -= 1;
            BitConverter.GetBytes (encodedOffset).CopyTo (data, secondEntryOffset + 0x40);
            BitConverter.GetBytes (encodedSize).CopyTo (data, secondEntryOffset + 0x44);
            encryptedPayload.CopyTo (data, dataOffset);
            File.WriteAllBytes (path, data);
        }

        static void CreateNoaRawFixture (string path)
        {
            const int rootOffset = 0x40;
            const int entryOffset = rootOffset + 0x14;
            const int nameOffset = entryOffset + 0x28;
            const int dataOffset = 0x100;
            var name = Encoding.ASCII.GetBytes ("sample.txt");
            var payload = Encoding.UTF8.GetBytes ("migrated NOA fixture");
            var data = new byte[dataOffset + 0x10 + payload.Length];
            Encoding.ASCII.GetBytes ("Entis\x1a").CopyTo (data, 0);
            BitConverter.GetBytes (0x02000400u).CopyTo (data, 8);
            Encoding.ASCII.GetBytes ("DirEntry").CopyTo (data, rootOffset);
            BitConverter.GetBytes (0x40L).CopyTo (data, rootOffset + 8);
            BitConverter.GetBytes (1).CopyTo (data, rootOffset + 0x10);
            BitConverter.GetBytes ((uint)payload.Length).CopyTo (data, entryOffset);
            BitConverter.GetBytes (0u).CopyTo (data, entryOffset + 8);
            BitConverter.GetBytes (0u).CopyTo (data, entryOffset + 0xC);
            BitConverter.GetBytes ((long)(dataOffset - rootOffset)).CopyTo (data, entryOffset + 0x10);
            BitConverter.GetBytes (0u).CopyTo (data, entryOffset + 0x20);
            BitConverter.GetBytes ((uint)name.Length).CopyTo (data, entryOffset + 0x24);
            name.CopyTo (data, nameOffset);
            BitConverter.GetBytes ((ulong)payload.Length).CopyTo (data, dataOffset + 8);
            payload.CopyTo (data, dataOffset + 0x10);
            File.WriteAllBytes (path, data);
        }

        static void CreateNoaBshfFixture (string path, string password)
        {
            const int rootOffset = 0x40;
            const int entryOffset = rootOffset + 0x14;
            const int nameOffset = entryOffset + 0x28;
            const int dataOffset = 0x100;
            var name = Encoding.ASCII.GetBytes ("sample.txt");
            var plain = new byte[32];
            Encoding.ASCII.GetBytes ("migrated NOA BSHF fixture").CopyTo (plain, 0);
            var encoded = EncodeBshfBlock (plain, password);
            var data = new byte[dataOffset + 0x10 + encoded.Length];
            Encoding.ASCII.GetBytes ("Entis\x1a").CopyTo (data, 0);
            BitConverter.GetBytes (0x02000400u).CopyTo (data, 8);
            Encoding.ASCII.GetBytes ("DirEntry").CopyTo (data, rootOffset);
            BitConverter.GetBytes (0x40L).CopyTo (data, rootOffset + 8);
            BitConverter.GetBytes (1).CopyTo (data, rootOffset + 0x10);
            BitConverter.GetBytes ((uint)plain.Length).CopyTo (data, entryOffset);
            BitConverter.GetBytes (0u).CopyTo (data, entryOffset + 8);
            BitConverter.GetBytes (0x40000000u).CopyTo (data, entryOffset + 0xC);
            BitConverter.GetBytes ((long)(dataOffset - rootOffset)).CopyTo (data, entryOffset + 0x10);
            BitConverter.GetBytes (0u).CopyTo (data, entryOffset + 0x20);
            BitConverter.GetBytes ((uint)name.Length).CopyTo (data, entryOffset + 0x24);
            name.CopyTo (data, nameOffset);
            BitConverter.GetBytes ((ulong)encoded.Length).CopyTo (data, dataOffset + 8);
            encoded.CopyTo (data, dataOffset + 0x10);
            File.WriteAllBytes (path, data);
        }

        static byte[] EncodeBshfBlock (byte[] plaintext, string password)
        {
            var passwordBytes = Encoding.ASCII.GetBytes (password ?? " ");
            var pass = new byte[Math.Max (32, passwordBytes.Length)];
            Array.Copy (passwordBytes, pass, passwordBytes.Length);
            if (passwordBytes.Length < 32)
            {
                var count = passwordBytes.Length;
                pass[count++] = 0x1B;
                for (var i = count; i < pass.Length; ++i)
                    pass[i] = (byte)(pass[i % count] + pass[i - 1]);
            }
            var source = new byte[32];
            var mask = new byte[32];
            var passIndex = 0;
            var bit = 0;
            for (var sourceBit = 0; sourceBit < 256; ++sourceBit)
            {
                bit = (bit + pass[passIndex++]) & 0xFF;
                if (passIndex >= pass.Length)
                    passIndex = 0;
                var outputByte = bit >> 3;
                var outputMask = 0x80 >> (bit & 7);
                while (mask[outputByte] == 0xFF)
                {
                    bit = (bit + 8) & 0xFF;
                    outputByte = bit >> 3;
                    outputMask = 0x80 >> (bit & 7);
                }
                while ((mask[outputByte] & outputMask) != 0)
                {
                    ++bit;
                    outputMask >>= 1;
                    if (outputMask == 0)
                    {
                        bit = (bit + 8) & 0xFF;
                        outputByte = bit >> 3;
                        outputMask = 0x80;
                    }
                }
                mask[outputByte] |= (byte)outputMask;
                if ((plaintext[outputByte] & outputMask) != 0)
                    source[sourceBit >> 3] |= (byte)(0x80 >> (sourceBit & 7));
            }
            return source.Concat (new byte[4]).ToArray ();
        }

        static byte[] EncipherBlowfish (Blowfish blowfish, byte[] plaintext)
        {
            var result = (byte[])plaintext.Clone();
            if ((result.Length & 7) != 0)
                throw new InvalidOperationException ("INT fixture payload must be block aligned.");
            for (var offset = 0; offset < result.Length; offset += 8)
            {
                var left = LittleEndian.ToUInt32 (result, offset);
                var right = LittleEndian.ToUInt32 (result, offset + 4);
                EncipherBlowfishWords (blowfish, ref left, ref right);
                LittleEndian.Pack (left, result, offset);
                LittleEndian.Pack (right, result, offset + 4);
            }
            return result;
        }

        static void EncipherBlowfishWords (Blowfish blowfish, ref uint left, ref uint right)
        {
            var method = typeof(Blowfish).GetMethod ("Encipher",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(uint).MakeByRefType (), typeof(uint).MakeByRefType () }, null);
            Assert.NotNull (method);
            var arguments = new object[] { left, right };
            method.Invoke (blowfish, arguments);
            left = (uint)arguments[0];
            right = (uint)arguments[1];
        }

        static byte[] EncipherIntName (string name, uint key)
        {
            const string alphabet = "zyxwvutsrqponmlkjihgfedcbaZYXWVUTSRQPONMLKJIHGFEDCBA";
            var result = new byte[0x40];
            var source = Encoding.ASCII.GetBytes (name);
            var shift = (byte)((key >> 24) + (key >> 16) + (key >> 8) + key);
            for (var i = 0; i < source.Length; ++i)
            {
                var index = alphabet.IndexOf ((char)source[i]);
                if (index == -1)
                    result[i] = source[i];
                else
                {
                    var decipheredIndex = alphabet.Length - 1 - index;
                    var rawIndex = (decipheredIndex + shift % alphabet.Length) % alphabet.Length;
                    result[i] = (byte)alphabet[rawIndex];
                }
                ++shift;
            }
            return result;
        }

        static byte[] EncryptMsdBytes (byte[] plaintext, string password)
        {
            var result = (byte[])plaintext.Clone();
            for (var offset = 0; offset < result.Length; offset += 0x20)
            {
                var chunkKey = Encoding.GetEncoding (932).GetBytes (password + (offset / 0x20).ToString());
                var hash = MD5.HashData (chunkKey);
                var mask = Encoding.ASCII.GetBytes (Convert.ToHexString (hash).ToLowerInvariant());
                var count = Math.Min (mask.Length, result.Length - offset);
                for (var i = 0; i < count; ++i)
                    result[offset + i] ^= mask[i];
            }
            return result;
        }

        static void EncryptNsaBytes (byte[] data, byte[] key)
        {
            for (var block = 0; block < data.Length; block += 1024)
            {
                var blockNumber = block / 1024;
                var number = new byte[8];
                BitConverter.GetBytes (blockNumber).CopyTo (number, 0);
                var md5 = MD5.HashData (number);
                var sha1 = SHA1.HashData (number);
                var hmacKey = new byte[16];
                for (var i = 0; i < hmacKey.Length; ++i)
                    hmacKey[i] = (byte)(md5[i] ^ sha1[i]);
                var hmac = new HMACSHA512 (hmacKey).ComputeHash (key);
                var map = Enumerable.Range (0, 256).ToArray ();
                byte index = 0;
                var h = 0;
                for (var i = 0; i < 256; ++i)
                {
                    if (hmac.Length == h)
                        h = 0;
                    var tmp = map[i];
                    index = (byte)(tmp + hmac[h++] + index);
                    map[i] = map[index];
                    map[index] = tmp;
                }
                var i0 = 0;
                var i1 = 0;
                for (var i = 0; i < 300; ++i)
                {
                    i0 = (i0 + 1) & 0xFF;
                    var tmp = map[i0];
                    i1 = (i1 + tmp) & 0xFF;
                    map[i0] = map[i1];
                    map[i1] = tmp;
                }
                var length = Math.Min (1024, data.Length - block);
                for (var i = 0; i < length; ++i)
                {
                    i0 = (i0 + 1) & 0xFF;
                    var tmp = map[i0];
                    i1 = (i1 + tmp) & 0xFF;
                    map[i0] = map[i1];
                    map[i1] = tmp;
                    data[block + i] ^= (byte)map[(map[i0] + tmp) & 0xFF];
                }
            }
        }

        static void EncipherPckBytes (Blowfish bf, byte[] data)
        {
            ReverseWords (data);
            bf.Encipher (data, data.Length);
            ReverseWords (data);
        }

        static void ReverseWords (byte[] data)
        {
            for (var i = 0; i < data.Length; i += 4)
            {
                var b0 = data[i];
                var b1 = data[i + 1];
                data[i] = data[i + 3];
                data[i + 1] = data[i + 2];
                data[i + 2] = b1;
                data[i + 3] = b0;
            }
        }

        static void XorPkgWords (byte[] data, uint[] key, int mask)
        {
            for (var i = 0; i < data.Length / 4; ++i)
            {
                var value = BitConverter.ToUInt32 (data, i * 4) ^ key[i & mask];
                Array.Copy (BitConverter.GetBytes (value), 0, data, i * 4, 4);
            }
        }

        static void XorPkgBytes (byte[] data, uint[] key)
        {
            var keyBytes = new byte[key.Length * sizeof(uint)];
            Buffer.BlockCopy (key, 0, keyBytes, 0, keyBytes.Length);
            for (var i = 0; i < data.Length; ++i)
                data[i] ^= keyBytes[i % keyBytes.Length];
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
