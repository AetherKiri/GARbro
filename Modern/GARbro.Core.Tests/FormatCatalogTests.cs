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
using GameRes.Formats.Emote;
using GameRes.Formats.Leaf;
using GameRes.Formats.Ikura;
using GameRes.Formats.Lucifen;
using GameRes.Formats.ExHibit;
using GameRes.Formats.YuRis;
using GameRes.Formats.Tactics;
using GameRes.Formats.Rpm;
using GameRes.Formats.Cyberworks;
using GameRes.Formats.Fmod;
using GameRes.Formats.AVC;
using GameRes.Formats.Dac;
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
using GameRes.Formats.Sviu;
using GameRes.Formats.Pvns;
using GameRes.Formats.Selene;
using GameRes.Formats.Elf;
using GameRes.Formats.Unity;
using GameRes.Formats.AZSys;
using GameRes.Formats.Jikkenshitsu;
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
        public void Speed_dat_format_loads_migrated_key_and_decodes_rle_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<SpDatFormat> ().Single ();
            var scheme = Assert.IsType<SjDatScheme> (format.Scheme);
            Assert.Equal (5, scheme.KnownSchemes.Count);
            var key = scheme.KnownSchemes["Bias {biAs+}"];
            Assert.Equal (16, key.Length);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.dat"] = "Bias {biAs+}" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Assert.Equal (key, SpDatFormat.ResolveKey ("sample.dat"));
                using (var input = new BinaryStream (new MemoryStream (CreateSpeedFixture ()), "sample.dat"))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.NotNull (decoded);
                    Assert.Equal ((uint)4, decoded.Width);
                    Assert.Equal ((uint)1, decoded.Height);
                    var pixels = new byte[4];
                    decoded.Bitmap.CopyPixels (pixels, 4, 0);
                    Assert.Equal (new byte[] { 1, 1, 1, 1 }, pixels);
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
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

        [Fact]
        public void Fsb5_audio_format_loads_migrated_vorbis_headers ()
        {
            var format = FormatCatalog.Instance.AudioFormats.OfType<Fsb5Audio> ().Single ();
            var scheme = Assert.IsType<FmodScheme> (format.Scheme);
            Assert.Equal (161, scheme.VorbisHeaders.Count);

            var plain = scheme.VorbisHeaders[348001315u];
            Assert.Equal (3796, plain.VorbisData.Length);
            Assert.Null (plain.PatchData);
            Assert.Equal (plain.VorbisData, Fsb5Decoder.GetVorbisHeader (348001315u));

            var patched = scheme.VorbisHeaders[2939054206u];
            Assert.Equal (3832, patched.VorbisData.Length);
            Assert.Equal (3750, patched.PatchOffset);
            Assert.Equal (32, patched.PatchData.Length);
            var patchedHeader = Fsb5Decoder.GetVorbisHeader (2939054206u);
            Assert.Equal (3832, patchedHeader.Length);
            Assert.False (patchedHeader.SequenceEqual (patched.VorbisData));
            Assert.Equal (patched.PatchData,
                patchedHeader.Skip (patched.PatchOffset).Take (patched.PatchData.Length).ToArray ());
        }

        [Fact]
        public void Bin_idx_format_loads_migrated_key_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "BIN/IDX");
            var scheme = Assert.IsType<BinPackScheme> (format.Scheme);
            Assert.Single (scheme.KnownKeys);
            var key = scheme.KnownKeys["GuildMaster"];

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var binPath = Path.Combine (tempDirectory, "sample.bin");
                var idxPath = Path.Combine (tempDirectory, "sample.idx");
                CreateBinIdxFixture (binPath, idxPath, key);

                using (var view = new ArcView (binPath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated BIN/IDX fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Arc_az_format_uses_migrated_key_for_encrypted_asb_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/AZ");
            var scheme = Assert.IsType<AsbScheme> (format.Scheme);
            Assert.Equal (4, scheme.KnownKeys.Count);
            Assert.Equal (2938115999u, scheme.KnownKeys["Amaenbou"]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            var previousDirectory = Directory.GetCurrentDirectory ();
            Directory.CreateDirectory (tempDirectory);
            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.arc"] = "Amaenbou" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                Directory.SetCurrentDirectory (tempDirectory);
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                CreateAsbFixture (archivePath, scheme.KnownKeys["Amaenbou"]);

                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.asb", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated ARC/AZ fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.SetCurrentDirectory (previousDirectory);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Arc_az_encrypted_format_uses_migrated_scheme_for_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/AZ/encrypted");
            var scheme = Assert.IsType<AzEncryptedScheme> (format.Scheme);
            Assert.Equal (2, scheme.KnownSchemes.Count);
            var key = scheme.KnownSchemes["Zwei Worter"];
            Assert.Equal (3740152942u, key.IndexKey);
            Assert.Equal (key.IndexKey, key.ContentKey);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                CreateAzEncryptedFixture (archivePath, key.IndexKey, key.ContentKey.Value);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated ARC/AZ encrypted fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Arc_az_encrypted_default_scheme_derives_system_content_key ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/AZ/encrypted");
            var scheme = Assert.IsType<AzEncryptedScheme> (format.Scheme);
            var key = scheme.KnownSchemes["Default"];
            Assert.Null (key.ContentKey);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "system.arc");
                CreateAzEncryptedSystemFixture (archivePath, key.IndexKey);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir, item => item.Name == "sample.bin");
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated ARC/AZ default fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Pkz_format_loads_migrated_key_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "PKZ");
            var scheme = Assert.IsType<PkzScheme> (format.Scheme);
            Assert.Single (scheme.KnownSchemes);
            var key = scheme.KnownSchemes["Fall in Love"];

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.pkz");
                CreatePkzFixture (archivePath, key);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated PKZ fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Pbz_format_loads_migrated_keys_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "PBZ");
            var key = PbzOpener.KnownSchemes["Karen"];

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.pbz");
                CreatePbzFixture (archivePath, key.ArcKey);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated PBZ fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Kcap_format_loads_migrated_password_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "KCAP");
            var scheme = Assert.IsType<KcapScheme> (format.Scheme);
            Assert.Equal (2, scheme.KnownSchemes.Count);
            var pass = scheme.KnownSchemes["Okaa-san ga Ippai!"];

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "Okaa-san ga Ippai!.pack");
                CreateKcapFixture (archivePath, pass);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated KCAP fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Ai5win_format_loads_migrated_scheme_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/AI5WIN");
            var scheme = Assert.IsType<Ai5Scheme> (format.Scheme);
            Assert.Equal (14, scheme.KnownSchemes.Count);
            var key = scheme.KnownSchemes["Be-Yond"];
            Assert.Equal (20, key.NameLength);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                CreateAi5Fixture (archivePath, key);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated AI5WIN fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Npa_format_loads_migrated_schemes_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "NPA");
            var scheme = Assert.IsType<NpaScheme> (format.Scheme);
            Assert.Equal (26, scheme.KnownSchemes.Count);
            var key = scheme.KnownSchemes["Chaos;Head"];
            Assert.Equal (NpaTitleId.CHAOSHEAD, key.TitleId);
            Assert.Equal (2271560481u, key.NameKey);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "Chaos;Head.npa");
                CreateNpaFixture (archivePath, key);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated NPA fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Psb_format_loads_migrated_keys_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "PSB/EMOTE");
            var scheme = Assert.IsType<PsbScheme> (format.Scheme);
            Assert.Equal (13, scheme.KnownKeys.Length);
            Assert.Equal (970396437u, scheme.KnownKeys[0]);
            Assert.Equal (439510497u, scheme.KnownKeys[^1]);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.psb");
                CreatePsbFixture (archivePath);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("a", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated PSB fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Am_leaf_format_loads_migrated_table_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "AM/Leaf");
            var scheme = Assert.IsType<AmScheme> (format.Scheme);
            Assert.Equal (0x10000, scheme.DecryptTable.Length);
            Assert.Equal (new byte[] { 38, 252, 155, 73, 113, 254, 119, 99 },
                scheme.DecryptTable.Take (8).ToArray ());

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.am");
                CreateAmFixture (archivePath, scheme.DecryptTable);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.bin", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated AM/Leaf fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Lpk_format_loads_migrated_maps_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "LPK");
            var scheme = Assert.IsType<LpkScheme> (format.Scheme);
            Assert.Equal (19, scheme.KnownSchemes.Count);
            Assert.Equal (22, scheme.KnownKeys.Count);
            var encryption = scheme.KnownSchemes["Happening Love!!"];
            var fileKey = scheme.KnownKeys["Happening Love!!"]["BGM.LPK"];
            Assert.Equal (0xA5B9AC6Bu, encryption.BaseKey.Key1);
            Assert.Equal (0x9A639DE5u, encryption.BaseKey.Key2);
            Assert.Equal (0x9C24DD6Au, fileKey.Key1);
            Assert.Equal (0xDEE82BC6u, fileKey.Key2);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["BGM.LPK"] = "Happening Love!!" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "BGM.LPK");
                CreateLpkFixture (archivePath, encryption, fileKey);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("a", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated LPK fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Gyu_format_loads_migrated_maps_and_decodes_fixture ()
        {
            var format = FormatCatalog.Instance.ImageFormats.OfType<GyuFormat> ().Single ();
            var scheme = Assert.IsType<GyuMap> (format.Scheme);
            Assert.Equal (7, scheme.NumericKeys.Count);
            Assert.Equal (2, scheme.StringKeys.Count);
            var key = scheme.StringKeys["More & More"]["title"];
            Assert.Equal (3357085324u, key);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["title.gyu"] = "More & More" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            try
            {
                using (var input = new BinaryStream (new MemoryStream (CreateGyuFixture (key)), "title.gyu"))
                {
                    var decoded = ImageFormat.Read (input);
                    Assert.NotNull (decoded);
                    Assert.Equal ((uint)2, decoded.Width);
                    Assert.Equal ((uint)1, decoded.Height);
                    Assert.Equal (24, decoded.BPP);
                    var pixels = new byte[8];
                    decoded.Bitmap.CopyPixels (pixels, 8, 0);
                    Assert.Equal (new byte[] { 1, 2, 3, 4, 5, 6, 0, 0 }, pixels);
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
            }
        }

        [Fact]
        public void Ypf_format_loads_migrated_scheme_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "YPF");
            var scheme = Assert.IsType<YuRisScheme> (format.Scheme);
            Assert.Equal (82, scheme.KnownSchemes.Count);
            var encryption = scheme.KnownSchemes["Unionism Quartet"];
            Assert.Equal (24, encryption.SwapTable.Length);
            Assert.Equal ((byte)201, encryption.Key);
            Assert.Equal (4u, encryption.ExtraHeaderSize);
            Assert.Equal (2527883219u, scheme.KnownSchemes["77 ~And, Two Stars Meet Again~"].ScriptKey);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.ypf"] = "Unionism Quartet" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.ypf");
                CreateYpfFixture (archivePath, encryption);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated YPF fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Tactics_arc2_format_loads_migrated_scheme_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/Tactics/2");
            var scheme = Assert.IsType<GameRes.Formats.Tactics.SchemeMap> (format.Scheme);
            Assert.Equal (9, scheme.KnownSchemes.Count);
            var encryption = scheme.KnownSchemes["Maou no Kuse ni Namaiki da!"];
            Assert.Equal ("Puni0r4p", encryption.Password);
            Assert.False (encryption.CustomLzss);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.arc"] = "Maou no Kuse ni Namaiki da!" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                CreateTacticsFixture (archivePath, encryption);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated Tactics fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Rpm_format_loads_migrated_scheme_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "ARC/RPM");
            var scheme = Assert.IsType<GameRes.Formats.Rpm.ArcScheme> (format.Scheme);
            Assert.Equal (31, scheme.KnownSchemes.Count);
            var encryption = scheme.KnownSchemes["After..."];
            Assert.Equal ("after", encryption.Keyword);
            Assert.Equal (24, encryption.NameLength);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.arc"] = "After..." };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.arc");
                CreateRpmFixture (archivePath, encryption);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated RPM fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Data_csystem_format_loads_migrated_scheme_and_opens_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "DATA/Csystem");
            var scheme = Assert.IsType<DataSchemeMap> (format.Scheme);
            Assert.Equal (3, scheme.KnownSchemes.Count);
            Assert.Equal (0, scheme.KnownSchemes["Mujina"].ExtraHeaderSize);
            Assert.Equal (4, scheme.KnownSchemes["Sandoku Ryouran"].ExtraHeaderSize);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["Data01.dat"] = "Mujina" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "Data01.dat");
                CreateDataCsystemFixture (tempDirectory);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("0000", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated DATA fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Avc_format_loads_migrated_scheme_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "AVC");
            var scheme = Assert.IsType<AvcScheme> (format.Scheme);
            Assert.Equal (4, scheme.KnownSchemes.Length);
            var encryption = scheme.KnownSchemes[0];
            Assert.Equal ("SETSUEI-", encryption.Password);
            Assert.Equal (8, encryption.KeyOffset);
            Assert.Equal (16, encryption.HeaderOffset);

            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.dat");
                CreateAvcFixture (archivePath, encryption);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated AVC fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Dpk_format_loads_migrated_scheme_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "DPK");
            var scheme = Assert.IsType<GameRes.Formats.Dac.ArchiveScheme> (format.Scheme);
            Assert.Equal (9, scheme.KnownSchemes.Length);
            var encryption = scheme.KnownSchemes[0];
            Assert.Equal ((uint)65432, encryption.Key1);
            Assert.Equal ((uint)1139247708, encryption.Key2);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.dpk"] = "默认" };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.dpk");
                CreateDpkFixture (archivePath, encryption);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated DPK fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Agsi_format_loads_migrated_nested_key_map_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "PAK/AGSI");
            var scheme = Assert.IsType<AgsiScheme> (format.Scheme);
            Assert.Equal (10, scheme.KnownSchemes.Count);
            Assert.Equal (103, scheme.KnownSchemes.Sum (item => item.Value.Count));
            var title = "Hitsuji-tachi no Yuuutsu";
            var archiveName = "data2.pak";
            var key = scheme.KnownSchemes[title][archiveName];

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { [archiveName] = title };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, archiveName);
                CreateAgsiFixture (archivePath, key);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("Copyright.Dat", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated AGSI", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Leaf_format_loads_migrated_key_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "PAK/LEAF");
            var scheme = Assert.IsType<LeafPackScheme> (format.Scheme);
            Assert.Equal (6, scheme.KnownSchemes.Count);
            var title = "Kizuato";
            var key = scheme.KnownSchemes[title];

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.pak"] = title };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.pak");
                CreateLeafFixture (archivePath, key);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.txt", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated Leaf fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
            }
        }

        [Fact]
        public void Ikura_gdl_format_loads_migrated_secrets_and_decrypts_fixture ()
        {
            var format = FormatCatalog.Instance.Formats.OfType<ArchiveFormat> ()
                .Single (item => item.Tag == "IKURA/GDL");
            var scheme = Assert.IsType<IsfScheme> (format.Scheme);
            Assert.Equal (18, scheme.KnownSecrets.Count);
            var title = scheme.KnownSecrets.Keys.OrderBy (value => value, StringComparer.Ordinal).First ();
            var secret = scheme.KnownSecrets[title];
            Assert.Equal (2048, secret.Length);

            var gameMapField = typeof (FormatCatalog).GetField ("m_game_map",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull (gameMapField);
            var originalGameMap = (Dictionary<string, string>)gameMapField.GetValue (FormatCatalog.Instance);
            var testGameMap = new Dictionary<string, string> (originalGameMap,
                StringComparer.OrdinalIgnoreCase) { ["sample.gdl"] = title };
            gameMapField.SetValue (FormatCatalog.Instance, testGameMap);
            var tempDirectory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
            Directory.CreateDirectory (tempDirectory);
            try
            {
                var archivePath = Path.Combine (tempDirectory, "sample.gdl");
                CreateIkuraFixture (archivePath, secret);
                using (var view = new ArcView (archivePath))
                using (var arc = format.TryOpen (view))
                {
                    Assert.NotNull (arc);
                    var entry = Assert.Single (arc.Dir);
                    Assert.Equal ("sample.isf", entry.Name);
                    using (var input = new StreamReader (format.OpenEntry (arc, entry), Encoding.UTF8))
                        Assert.Equal ("migrated IKURA fixture", input.ReadToEnd ());
                }
            }
            finally
            {
                gameMapField.SetValue (FormatCatalog.Instance, originalGameMap);
                Directory.Delete (tempDirectory, true);
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
            Assert.IsType<GameRes.Formats.Morning.PakOpener> (format);
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

        static void CreateIkuraFixture (string path, byte[] secret)
        {
            var plaintext = Encoding.UTF8.GetBytes ("migrated IKURA fixture");
            var encrypted = (byte[])plaintext.Clone ();
            var decoder = new IsfDecoder (secret);
            decoder.Decode (encrypted);
            var footer = Encoding.ASCII.GetBytes ("SECRETFILTER100a");
            var payload = encrypted.Concat (footer).ToArray ();
            const int entryOffset = 0x40;
            var data = new byte[entryOffset + payload.Length];
            using (var output = new MemoryStream (data, true))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                output.Position = 4;
                writer.Write (Encoding.ASCII.GetBytes ("PX10"));
                output.Position = 8;
                writer.Write (1);
                writer.Write (0x14u);
                output.Position = 0x20;
                var name = new byte[12];
                Encoding.ASCII.GetBytes ("sample.isf").CopyTo (name, 0);
                writer.Write (name);
                writer.Write ((uint)entryOffset);
                writer.Write ((uint)payload.Length);
                output.Position = entryOffset;
                writer.Write (payload);
            }
            File.WriteAllBytes (path, data);
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

        static void CreateBinIdxFixture (string binPath, string idxPath, BinPackKey key)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated BIN/IDX fixture");
            byte[] encryptedPayload;
            using (var aes = Aes.Create ())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key.Key;
                aes.IV = key.IV;
                using (var encryptor = aes.CreateEncryptor ())
                    encryptedPayload = encryptor.TransformFinalBlock (payload, 0, payload.Length);
            }

            byte[] indexRecord;
            using (var record = new MemoryStream ())
            using (var output = new BinaryWriter (record, Encoding.UTF8, true))
            {
                output.Write ((byte)0x83);
                WriteBinIdxFieldName (output, "fileName");
                WriteBinIdxString (output, "sample.txt");
                WriteBinIdxFieldName (output, "index");
                WriteBinIdxInt32 (output, 0);
                WriteBinIdxFieldName (output, "size");
                WriteBinIdxInt32 (output, encryptedPayload.Length);
                indexRecord = record.ToArray ();
            }

            byte[] encryptedIndex;
            using (var aes = Aes.Create ())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key.Key;
                aes.IV = key.IV;
                using (var encryptor = aes.CreateEncryptor ())
                    encryptedIndex = encryptor.TransformFinalBlock (indexRecord, 0, indexRecord.Length);
            }

            using (var output = new BinaryWriter (File.Create (idxPath), Encoding.UTF8))
            {
                output.Write (encryptedIndex.Length);
                output.Write (encryptedIndex);
            }
            File.WriteAllBytes (binPath, encryptedPayload);
        }

        static void CreateAsbFixture (string path, uint key)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated ARC/AZ fixture");
            byte[] compressed;
            using (var buffer = new MemoryStream ())
            {
                using (var zlib = new ZLibStream (buffer, CompressionLevel.SmallestSize, true))
                    zlib.Write (payload, 0, payload.Length);
                compressed = buffer.ToArray ();
            }
            var body = new byte[4 + compressed.Length];
            Buffer.BlockCopy (compressed, 0, body, 4, compressed.Length);
            while ((body.Length & 3) != 0)
                Array.Resize (ref body, body.Length + 1);
            LittleEndian.Pack (Crc32.Compute (body, 4, body.Length - 4), body, 0);

            uint contentKey = key ^ (uint)payload.Length;
            contentKey ^= ((contentKey << 12) | contentKey) << 11;
            for (int i = 0; i < body.Length; i += 4)
                LittleEndian.Pack (LittleEndian.ToUInt32 (body, i) + contentKey, body, i);

            var asb = new byte[12 + body.Length];
            Buffer.BlockCopy (Encoding.ASCII.GetBytes ("ASB\x1A"), 0, asb, 0, 4);
            LittleEndian.Pack ((uint)body.Length, asb, 4);
            LittleEndian.Pack ((uint)payload.Length, asb, 8);
            Buffer.BlockCopy (body, 0, asb, 12, body.Length);

            const int entrySize = 0x40;
            var index = new byte[entrySize];
            LittleEndian.Pack (0u, index, 0);
            LittleEndian.Pack ((uint)asb.Length, index, 4);
            var name = Encoding.ASCII.GetBytes ("sample.asb");
            Buffer.BlockCopy (name, 0, index, 0x10, name.Length);

            const int controlLength = 1;
            const int compressed1Length = 0;
            int compressed2Length = 1 + index.Length;
            var packedIndex = new byte[0x14 + controlLength + compressed1Length + compressed2Length];
            LittleEndian.Pack (controlLength, packedIndex, 4);
            LittleEndian.Pack (compressed1Length, packedIndex, 8);
            LittleEndian.Pack (compressed2Length, packedIndex, 12);
            LittleEndian.Pack (entrySize, packedIndex, 0x10);
            packedIndex[0x14] = 0;
            packedIndex[0x15] = (byte)(entrySize - 1);
            Buffer.BlockCopy (index, 0, packedIndex, 0x16, index.Length);
            LittleEndian.Pack (Crc32.Compute (packedIndex, 0x14, packedIndex.Length - 0x14), packedIndex, 0);

            using (var output = new BinaryWriter (File.Create (path), Encoding.UTF8))
            {
                output.Write (Encoding.ASCII.GetBytes ("ARC\x1A"));
                output.Write (1);
                output.Write (1);
                output.Write ((uint)packedIndex.Length);
                output.Write (new byte[0x20]);
                output.Write (packedIndex);
                output.Write (asb);
            }
        }

        static void CreateAzEncryptedFixture (string path, uint indexKey, uint contentKey)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated ARC/AZ encrypted fixture");
            var record = new byte[0x30];
            LittleEndian.Pack (0u, record, 0);
            LittleEndian.Pack ((uint)payload.Length, record, 4);
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            Buffer.BlockCopy (name, 0, record, 0x10, name.Length);

            byte[] compressed;
            using (var buffer = new MemoryStream ())
            {
                using (var zlib = new ZLibStream (buffer, CompressionLevel.SmallestSize, true))
                    zlib.Write (record, 0, record.Length);
                compressed = buffer.ToArray ();
            }
            var packedIndex = new byte[4 + compressed.Length];
            Buffer.BlockCopy (compressed, 0, packedIndex, 4, compressed.Length);
            LittleEndian.Pack (Adler32.Compute (packedIndex, 4, compressed.Length), packedIndex, 0);
            XorAzEncrypted (packedIndex, 0x30, indexKey);

            var header = new byte[0x30];
            Buffer.BlockCopy (Encoding.ASCII.GetBytes ("ARC\0"), 0, header, 0, 4);
            LittleEndian.Pack (1, header, 4);
            LittleEndian.Pack (1, header, 8);
            LittleEndian.Pack ((uint)packedIndex.Length, header, 12);
            XorAzEncrypted (header, 0, indexKey);

            XorAzEncrypted (payload, 0x30 + packedIndex.Length, contentKey);
            using (var output = File.Create (path))
            {
                output.Write (header, 0, header.Length);
                output.Write (packedIndex, 0, packedIndex.Length);
                output.Write (payload, 0, payload.Length);
            }
        }

        static void CreateAzEncryptedSystemFixture (string path, uint indexKey)
        {
            var environment = Enumerable.Range (1, 16).Select (value => (byte)value).ToArray ();
            var sysenvCompressed = CompressBytes (environment);
            var sysenv = new byte[4 + sysenvCompressed.Length];
            Buffer.BlockCopy (sysenvCompressed, 0, sysenv, 4, sysenvCompressed.Length);
            LittleEndian.Pack (Adler32.Compute (sysenv, 4, sysenvCompressed.Length), sysenv, 0);

            var payload = Encoding.UTF8.GetBytes ("migrated ARC/AZ default fixture");
            var records = new byte[0x60];
            WriteAzEncryptedIndexRecord (records, 0, 0, (uint)sysenv.Length, "sysenv.tbl");
            WriteAzEncryptedIndexRecord (records, 0x30, (uint)sysenv.Length, (uint)payload.Length, "sample.bin");
            var packedIndex = new byte[4 + CompressBytes (records).Length];
            var compressedRecords = CompressBytes (records);
            Buffer.BlockCopy (compressedRecords, 0, packedIndex, 4, compressedRecords.Length);
            LittleEndian.Pack (Adler32.Compute (packedIndex, 4, compressedRecords.Length), packedIndex, 0);

            var header = new byte[0x30];
            Buffer.BlockCopy (Encoding.ASCII.GetBytes ("ARC\0"), 0, header, 0, 4);
            LittleEndian.Pack (1, header, 4);
            LittleEndian.Pack (2, header, 8);
            LittleEndian.Pack ((uint)packedIndex.Length, header, 12);
            XorAzEncrypted (header, 0, indexKey);
            XorAzEncrypted (packedIndex, 0x30, indexKey);
            XorAzEncrypted (sysenv, 0x30 + packedIndex.Length, indexKey);
            var contentKey = AzEncryptedKeyDerivation.GenerateContentKey (environment);
            XorAzEncrypted (payload, 0x30 + packedIndex.Length + sysenv.Length, contentKey);

            using (var output = File.Create (path))
            {
                output.Write (header, 0, header.Length);
                output.Write (packedIndex, 0, packedIndex.Length);
                output.Write (sysenv, 0, sysenv.Length);
                output.Write (payload, 0, payload.Length);
            }
        }

        static void CreatePkzFixture (string path, byte[] key)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated PKZ fixture");
            var index = new byte[0x0C + 0x2C];
            LittleEndian.Pack (0, index, 0);
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            Buffer.BlockCopy (name, 0, index, 0x0C, name.Length);
            LittleEndian.Pack ((uint)payload.Length, index, 0x2C);
            LittleEndian.Pack (0, index, 0x30);
            EncryptPkz (index, key);
            var encryptedPayload = (byte[])payload.Clone ();
            EncryptPkz (encryptedPayload, key);
            using (var output = File.Create (path))
            {
                output.Write (Encoding.ASCII.GetBytes ("PKZ0"), 0, 4);
                var count = new byte[4];
                LittleEndian.Pack (1, count, 0);
                output.Write (count, 0, count.Length);
                output.Write (index, 0, index.Length);
                output.Write (encryptedPayload, 0, encryptedPayload.Length);
            }
        }

        static void CreatePbzFixture (string path, byte[] key)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated PBZ fixture");
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            var entrySize = 0x18 + name.Length + 1;
            var index = new byte[entrySize];
            LittleEndian.Pack ((uint)entrySize, index, 0);
            LittleEndian.Pack ((uint)payload.Length, index, 4);
            LittleEndian.Pack (0, index, 8);
            Buffer.BlockCopy (name, 0, index, 0x18, name.Length);
            EncryptPbz (index, key);
            var encryptedPayload = (byte[])payload.Clone ();
            EncryptPbz (encryptedPayload, key);
            using (var output = File.Create (path))
            {
                output.Write (Encoding.ASCII.GetBytes ("PBZ1"), 0, 4);
                var header = new byte[12];
                LittleEndian.Pack (1, header, 0);
                LittleEndian.Pack ((uint)index.Length, header, 4);
                output.Write (header, 0, header.Length);
                output.Write (index, 0, index.Length);
                output.Write (encryptedPayload, 0, encryptedPayload.Length);
            }
        }

        static void CreateKcapFixture (string path, string pass)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated KCAP fixture");
            var keyTable = PackOpener.CreateKeyTable (pass);
            var encryptedPayload = (byte[])payload.Clone ();
            for (int i = 0; i < encryptedPayload.Length; ++i)
                encryptedPayload[i] ^= keyTable[i];
            var index = new byte[0x54];
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            Buffer.BlockCopy (name, 0, index, 0, name.Length);
            LittleEndian.Pack (0x5C, index, 0x48);
            LittleEndian.Pack ((uint)encryptedPayload.Length, index, 0x4C);
            LittleEndian.Pack (1, index, 0x50);
            using (var output = File.Create (path))
            {
                output.Write (Encoding.ASCII.GetBytes ("KCAP"), 0, 4);
                var count = new byte[4];
                LittleEndian.Pack (1, count, 0);
                output.Write (count, 0, count.Length);
                output.Write (index, 0, index.Length);
                output.Write (encryptedPayload, 0, encryptedPayload.Length);
            }
        }

        static void CreateAi5Fixture (string path, ArcIndexScheme key)
        {
            var payload = Encoding.UTF8.GetBytes ("migrated AI5WIN fixture");
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            var indexSize = key.NameLength + 8;
            var index = new byte[indexSize];
            var encryptedName = new byte[key.NameLength];
            Buffer.BlockCopy (name, 0, encryptedName, 0, name.Length);
            for (int i = 0; i < encryptedName.Length; ++i)
                encryptedName[i] ^= key.NameKey;
            Buffer.BlockCopy (encryptedName, 0, index, 0, encryptedName.Length);
            LittleEndian.Pack ((uint)payload.Length ^ key.SizeKey, index, key.NameLength);
            LittleEndian.Pack ((uint)index.Length + 4u ^ key.OffsetKey, index, 4 + key.NameLength);
            var count = new byte[4];
            LittleEndian.Pack (1, count, 0);
            using (var output = File.Create (path))
            {
                output.Write (count, 0, count.Length);
                output.Write (index, 0, index.Length);
                output.Write (payload, 0, payload.Length);
            }
        }

        static void CreateNpaFixture (string path, GameRes.Formats.NitroPlus.EncryptionScheme scheme)
        {
            var plaintext = Encoding.UTF8.GetBytes ("migrated NPA fixture");
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            const int key1 = NpaOpener.DefaultKey1;
            const int key2 = NpaOpener.DefaultKey2;
            var archiveKey = NpaOpener.GetArchiveKey (scheme.TitleId, key1, key2);
            var rawName = new byte[name.Length];
            for (var i = 0; i < name.Length; ++i)
                rawName[i] = (byte)(name[i] - NpaOpener.DecryptName (i, 0, archiveKey));

            var entryKey = (int)scheme.NameKey;
            for (var i = 0; i < rawName.Length; ++i)
                entryKey -= rawName[i];
            entryKey *= rawName.Length;
            entryKey += archiveKey;
            entryKey *= plaintext.Length;

            var table = NpaOpener.GenerateKeyTable (scheme);
            var inverse = new byte[256];
            for (var i = 0; i < inverse.Length; ++i)
                inverse[table[i]] = (byte)i;
            var encrypted = new byte[plaintext.Length];
            for (var i = 0; i < plaintext.Length; ++i)
                encrypted[i] = inverse[(plaintext[i] + entryKey + i) & 0xff];

            var indexSize = 4 + rawName.Length + 17;
            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (Encoding.ASCII.GetBytes ("NPA\x01"));
                writer.Write ((short)0);
                writer.Write ((byte)0);
                writer.Write (key1);
                writer.Write (key2);
                writer.Write (false);
                writer.Write (true);
                writer.Write (1);
                writer.Write (0);
                writer.Write (1);
                writer.Write ((long)0);
                writer.Write (indexSize);
                writer.Write (rawName.Length);
                writer.Write (rawName);
                writer.Write ((byte)2);
                writer.Write (0);
                writer.Write (0u);
                writer.Write ((uint)encrypted.Length);
                writer.Write ((uint)plaintext.Length);
                writer.Write (encrypted);
            }
        }

        static void CreatePsbFixture (string path)
        {
            var names1 = new byte[99];
            var names2 = new byte[99];
            names1[0] = 1;
            names2[98] = 0;
            names1[98] = 2;
            names2[2] = 98;
            names1[2] = 0;

            var data = new List<byte> (new byte[40]);
            var namesOffset = data.Count;
            AddPsbArray (data, names1, 1);
            AddPsbArray (data, names2, 1);
            var stringsOffset = data.Count;
            AddPsbArray (data, Array.Empty<byte> (), 1);
            var stringsDataOffset = data.Count;
            AddPsbArray (data, Array.Empty<byte> (), 1);
            var chunkOffsetsOffset = data.Count;
            AddPsbArray (data, new byte[4], 4);
            var chunkLengthsOffset = data.Count;
            var payload = Encoding.UTF8.GetBytes ("migrated PSB fixture");
            AddPsbArray (data, BitConverter.GetBytes (payload.Length), 4);
            var rootOffset = data.Count;
            data.Add (0x21);
            AddPsbArray (data, BitConverter.GetBytes (0), 4);
            AddPsbArray (data, new byte[] { 0, 0, 0, 0 }, 4);
            data.Add (0x19);
            data.Add (0);
            var chunkDataOffset = data.Count;

            PackPsbInt (data, 12, namesOffset);
            PackPsbInt (data, 16, stringsOffset);
            PackPsbInt (data, 20, stringsDataOffset);
            PackPsbInt (data, 24, chunkOffsetsOffset);
            PackPsbInt (data, 28, chunkLengthsOffset);
            PackPsbInt (data, 32, chunkDataOffset);
            PackPsbInt (data, 36, rootOffset);
            using (var output = File.Create (path))
            {
                output.Write (Encoding.ASCII.GetBytes ("PSB\0"), 0, 4);
                output.WriteByte (3);
                output.WriteByte (0);
                output.WriteByte (0);
                output.WriteByte (0);
                output.Write (data.ToArray (), 8, data.Count - 8);
                output.Write (payload, 0, payload.Length);
            }
        }

        static void AddPsbArray (List<byte> data, byte[] values, int elementSize)
        {
            data.Add (0x0D);
            data.Add ((byte)(values.Length / elementSize));
            data.Add ((byte)(0x0C + elementSize));
            data.AddRange (values);
        }

        static void PackPsbInt (List<byte> data, int offset, int value)
        {
            var bytes = BitConverter.GetBytes (value);
            for (var i = 0; i < bytes.Length; ++i)
                data[offset + i] = bytes[i];
        }

        static void CreateAmFixture (string path, byte[] table)
        {
            const byte key = 0x5A;
            var payload = Encoding.UTF8.GetBytes ("migrated AM/Leaf fixture");
            var name = Encoding.ASCII.GetBytes ("sample.bin");
            var index = new byte[name.Length + 1 + 8];
            Buffer.BlockCopy (name, 0, index, 0, name.Length);
            LittleEndian.Pack ((uint)0, index, name.Length + 1);
            LittleEndian.Pack ((uint)payload.Length, index, name.Length + 5);
            for (var i = 0; i < index.Length; ++i)
                index[i] ^= key;

            var encryptedPayload = new byte[payload.Length];
            for (var i = 0; i < payload.Length; ++i)
                encryptedPayload[i] = (byte)(payload[i] ^ table[i]);
            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (Encoding.ASCII.GetBytes ("am00"));
                writer.Write ((uint)index.Length);
                writer.Write (key);
                writer.Write (index);
                writer.Write (encryptedPayload);
            }
        }

        static void CreateLpkFixture (string path, GameRes.Formats.Lucifen.EncryptionScheme scheme,
                                      LpkOpener.Key fileKey)
        {
            var basename = Encodings.cp932.GetBytes ("BGM");
            var key1 = scheme.BaseKey.Key1;
            var key2 = scheme.BaseKey.Key2;
            for (var b = 0; b < basename.Length; ++b)
            {
                var e = basename.Length - 1 - b;
                key1 ^= basename[e];
                key2 ^= basename[b];
                key1 = Binary.RotR (key1, 7);
                key2 = Binary.RotL (key2, 7);
            }
            key1 ^= fileKey.Key1;
            key2 ^= fileKey.Key2;

            var payload = Encoding.UTF8.GetBytes ("migrated LPK fixture");
            var index = new byte[27];
            LittleEndian.Pack (1, index, 0);
            index[4] = 0;
            index[5] = 0;
            LittleEndian.Pack (8, index, 6);
            index[10] = 1;
            index[11] = (byte)'a';
            LittleEndian.Pack ((ushort)0, index, 12);
            index[14] = 1;
            index[15] = 0;
            LittleEndian.Pack ((ushort)0, index, 16);
            index[18] = 0;
            LittleEndian.Pack (8u + (uint)index.Length, index, 19);
            LittleEndian.Pack ((uint)payload.Length, index, 23);
            scheme.DecryptIndex (index, index.Length, key2);

            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (Encoding.ASCII.GetBytes ("LPK1"));
                writer.Write ((uint)(0x02000000u | (uint)index.Length) ^ key2);
                writer.Write (index);
                writer.Write (payload);
            }
        }

        static byte[] CreateGyuFixture (uint key)
        {
            var payload = new byte[] { 1, 2, 3, 4, 5, 6, 0, 0 };
            var encrypted = (byte[])payload.Clone ();
            var twister = new MersenneTwister (key);
            var swaps = new (int First, int Second)[10];
            for (var n = 0; n < 10; ++n)
            {
                var first = (int)(twister.Rand () % (uint)encrypted.Length);
                var second = (int)(twister.Rand () % (uint)encrypted.Length);
                swaps[n] = (first, second);
            }
            for (var n = swaps.Length - 1; n >= 0; --n)
            {
                var first = swaps[n].First;
                var second = swaps[n].Second;
                var temp = encrypted[first];
                encrypted[first] = encrypted[second];
                encrypted[second] = temp;
            }

            var header = new byte[0x24];
            header[0] = (byte)'G';
            header[1] = (byte)'Y';
            header[2] = (byte)'U';
            header[3] = 0x1A;
            LittleEndian.Pack ((ushort)0, header, 4);
            LittleEndian.Pack ((ushort)0x100, header, 6);
            LittleEndian.Pack (0u, header, 8);
            LittleEndian.Pack (24, header, 12);
            LittleEndian.Pack (2u, header, 16);
            LittleEndian.Pack (1u, header, 20);
            LittleEndian.Pack (8, header, 24);
            LittleEndian.Pack (0, header, 28);
            LittleEndian.Pack (0, header, 32);

            using (var output = new MemoryStream ())
            {
                output.Write (header, 0, header.Length);
                output.Write (encrypted, 0, encrypted.Length);
                return output.ToArray ();
            }
        }

        static void CreateYpfFixture (string path, YpfScheme scheme)
        {
            const uint version = 0x1F4;
            const string name = "sample.txt";
            var nameBytes = Encodings.cp932.GetBytes (name);
            var payload = Encoding.UTF8.GetBytes ("migrated YPF fixture");
            var extraSize = 0x12u + scheme.ExtraHeaderSize;
            var directorySize = 5u + extraSize + (uint)nameBytes.Length;
            var baseOffset = 0x20u + directorySize;
            var directory = new byte[(int)directorySize];
            LittleEndian.Pack (0u, directory, 0);
            var lengthCode = DecryptYpfLength (scheme.SwapTable, (byte)nameBytes.Length);
            directory[4] = (byte)~lengthCode;
            for (var i = 0; i < nameBytes.Length; ++i)
                directory[5 + i] = (byte)(nameBytes[i] ^ scheme.Key);
            var entryOffset = 5 + nameBytes.Length;
            directory[entryOffset] = 2;
            directory[entryOffset + 1] = 0;
            LittleEndian.Pack ((uint)payload.Length, directory, entryOffset + 2);
            LittleEndian.Pack ((uint)payload.Length, directory, entryOffset + 6);
            LittleEndian.Pack (baseOffset, directory, entryOffset + 10);
            LittleEndian.Pack (0u, directory, entryOffset + 14);

            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (Encoding.ASCII.GetBytes ("YPF\0"));
                writer.Write (version);
                writer.Write (1);
                writer.Write (directorySize);
                writer.Write (new byte[0x10]);
                writer.Write (directory);
                writer.Write (payload);
            }
        }

        static void CreateTacticsFixture (string path, GameRes.Formats.Tactics.ArcScheme scheme)
        {
            const string name = "sample.txt";
            var nameBytes = Encodings.cp932.GetBytes (name);
            var payload = Encoding.UTF8.GetBytes ("migrated Tactics fixture");
            var password = Encodings.cp932.GetBytes (scheme.Password);
            for (var i = 0; i < payload.Length; ++i)
                payload[i] ^= password[i % password.Length];

            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (Encoding.ASCII.GetBytes ("TACTICS_ARC_FILE"));
                writer.Write ((uint)payload.Length);
                writer.Write (0u);
                writer.Write ((uint)nameBytes.Length);
                writer.Write (0u);
                writer.Write (0u);
                writer.Write (nameBytes);
                writer.Write (payload);
                writer.Write (0u);
                writer.Write (0u);
                writer.Write (0u);
            }
        }

        static void CreateRpmFixture (string path, GameRes.Formats.Rpm.EncryptionScheme scheme)
        {
            const string name = "sample.txt";
            var nameBytes = Encodings.cp932.GetBytes (name);
            var payload = Encoding.UTF8.GetBytes ("migrated RPM fixture");
            var indexSize = scheme.NameLength + 12;
            var dataOffset = 8u + (uint)indexSize;
            var index = new byte[indexSize];
            Buffer.BlockCopy (nameBytes, 0, index, 0, nameBytes.Length);
            LittleEndian.Pack ((uint)payload.Length, index, scheme.NameLength + 4);
            LittleEndian.Pack (dataOffset, index, scheme.NameLength + 8);
            var keyword = Encoding.ASCII.GetBytes (scheme.Keyword);
            for (var i = 0; i < index.Length; ++i)
                index[i] = (byte)(index[i] - keyword[i % keyword.Length]);

            using (var output = File.Create (path))
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                writer.Write (1);
                writer.Write (0);
                writer.Write (index);
                writer.Write (payload);
            }
        }

        static void CreateDataCsystemFixture (string directory)
        {
            const int dataOffset = 0x20;
            var payload = Encoding.UTF8.GetBytes ("migrated DATA fixture");
            var toc = new byte[40];
            LittleEndian.Pack (2, toc, 0);
            LittleEndian.Pack (0, toc, 4);
            LittleEndian.Pack (0, toc, 8);
            LittleEndian.Pack (0, toc, 12);
            LittleEndian.Pack (1, toc, 16);
            LittleEndian.Pack (0, toc, 20);
            LittleEndian.Pack ((uint)payload.Length, toc, 24);
            LittleEndian.Pack (dataOffset, toc, 28);
            LittleEndian.Pack (0, toc, 32);
            LittleEndian.Pack (0, toc, 36);
            File.WriteAllBytes (Path.Combine (directory, "Data00.dat"), toc);

            var data = new byte[dataOffset + payload.Length];
            Buffer.BlockCopy (payload, 0, data, dataOffset, payload.Length);
            File.WriteAllBytes (Path.Combine (directory, "Data01.dat"), data);
        }

        static void CreateAvcFixture (string path, GameRes.Formats.AVC.ArchiveScheme scheme)
        {
            const int indexOffset = 0x80;
            const int headerOffset = 16;
            const int indexSize = 0x114;
            const int dataOffset = indexOffset + indexSize;
            const string name = "sample.txt";
            var key = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var password = Encoding.ASCII.GetBytes (scheme.Password);
            var payload = Encoding.UTF8.GetBytes ("migrated AVC fixture");
            var data = new byte[headerOffset + dataOffset + payload.Length];

            for (var i = 0; i < key.Length; ++i)
            {
                data[scheme.HeaderOffset + i] = (byte)("ARCHIVE\0"[i] ^ key[i]);
                data[scheme.KeyOffset + i] = (byte)(key[i] ^ password[i]);
            }

            var header = new byte[0x24];
            LittleEndian.Pack (indexOffset, header, 0x10);
            LittleEndian.Pack (indexSize, header, 0x14);
            LittleEndian.Pack (1, header, 0x20);
            for (var i = 0x10; i < header.Length; ++i)
                data[headerOffset + i] = (byte)(header[i] ^ key[i & 7]);

            var index = new byte[indexSize];
            var nameBytes = Encodings.cp932.GetBytes (name);
            Buffer.BlockCopy (nameBytes, 0, index, 1, nameBytes.Length);
            LittleEndian.Pack (dataOffset, index, 0x108);
            LittleEndian.Pack ((uint)payload.Length, index, 0x10C);
            for (var i = 0; i < index.Length; ++i)
                index[i] ^= key[(indexOffset + i) & 7];
            Buffer.BlockCopy (index, 0, data, headerOffset + indexOffset, index.Length);

            for (var i = 0; i < payload.Length; ++i)
                data[headerOffset + dataOffset + i] = (byte)(payload[i] ^ key[(dataOffset + i) & 7]);
            File.WriteAllBytes (path, data);
        }

        static void CreateDpkFixture (string path, DpkScheme scheme)
        {
            const int indexOffset = 16;
            const int indexLength = 32;
            const int dataOffset = indexOffset + indexLength;
            const string name = "sample.txt";
            var nameBytes = Encodings.cp932.GetBytes (name);
            var payload = Encoding.UTF8.GetBytes ("migrated DPK fixture");
            var index = new byte[indexLength];
            LittleEndian.Pack (1, index, 0);
            LittleEndian.Pack (0, index, 4);
            LittleEndian.Pack (0, index, 8);
            LittleEndian.Pack ((uint)payload.Length, index, 12);
            LittleEndian.Pack (0, index, 16);
            Buffer.BlockCopy (nameBytes, 0, index, 20, nameBytes.Length);

            var file = new byte[dataOffset + payload.Length];
            file[0] = (byte)'D';
            file[1] = (byte)'P';
            file[2] = (byte)'K';
            file[3] = 0;
            var header = new byte[8];
            LittleEndian.Pack (dataOffset, header, 0);
            for (var i = 0; i < header.Length; ++i)
                file[8 + i] = (byte)(header[i] ^ (byte)(i - 8));

            var last = file[15];
            for (var i = 0; i < index.Length; ++i)
            {
                var encrypted = (byte)(index[i] ^ (byte)(indexOffset + i + last));
                file[indexOffset + i] = encrypted;
                last = encrypted;
            }

            uint hash = 0;
            for (var i = nameBytes.Length - 1; i >= 0; --i)
                hash += scheme.Key1 + scheme.Key2 * ((uint)payload.Length + nameBytes[i]);
            var key1 = scheme.Key1;
            for (var i = 0; i < payload.Length; ++i)
            {
                var key = (byte)(key1 + (key1 >> 8));
                file[dataOffset + i] = (byte)((payload[i] + (byte)hash) ^ key);
                key1 += scheme.Key2;
            }
            File.WriteAllBytes (path, file);
        }

        static void CreateAgsiFixture (string path, byte[] key)
        {
            const int recordSize = 0x30;
            const int dataOffset = 0xC + recordSize;
            const string name = "Copyright.Dat";
            var plaintext = new byte[16];
            var text = Encoding.UTF8.GetBytes ("migrated AGSI");
            Buffer.BlockCopy (text, 0, plaintext, 0, text.Length);
            var index = new byte[recordSize];
            LittleEndian.Pack (text.Length, index, 0);
            LittleEndian.Pack (plaintext.Length, index, 4);
            LittleEndian.Pack (3, index, 8);
            LittleEndian.Pack (0, index, 12);
            Buffer.BlockCopy (Encoding.ASCII.GetBytes (name), 0, index, 16, name.Length);

            byte[] encrypted;
            using (var des = DES.Create ())
            {
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.Zeros;
                using (var encryptor = des.CreateEncryptor ())
                    encrypted = encryptor.TransformFinalBlock (plaintext, 0, plaintext.Length);
            }

            var file = new byte[dataOffset + encrypted.Length];
            Buffer.BlockCopy (Encoding.ASCII.GetBytes ("PACK"), 0, file, 0, 4);
            LittleEndian.Pack (1, file, 4);
            LittleEndian.Pack (recordSize, file, 8);
            Buffer.BlockCopy (index, 0, file, 12, index.Length);
            Buffer.BlockCopy (encrypted, 0, file, dataOffset, encrypted.Length);
            File.WriteAllBytes (path, file);
        }

        static void CreateLeafFixture (string path, byte[] key)
        {
            const int dataOffset = 0x20;
            const int recordSize = 0x18;
            var name = Encoding.ASCII.GetBytes ("sample");
            var extension = Encoding.ASCII.GetBytes ("txt");
            var payload = Encoding.UTF8.GetBytes ("migrated Leaf fixture");
            var encryptedPayload = new byte[payload.Length];
            for (var i = 0; i < payload.Length; ++i)
                encryptedPayload[i] = (byte)(payload[i] + key[i % key.Length]);

            var index = new byte[recordSize];
            Buffer.BlockCopy (name, 0, index, 0, name.Length);
            Buffer.BlockCopy (extension, 0, index, 8, extension.Length);
            LittleEndian.Pack (dataOffset, index, 0xC);
            LittleEndian.Pack ((uint)payload.Length, index, 0x10);
            var encryptedIndex = new byte[index.Length];
            for (var i = 0; i < index.Length; ++i)
                encryptedIndex[i] = (byte)(index[i] + key[i % key.Length]);

            var file = new byte[dataOffset + encryptedPayload.Length + encryptedIndex.Length];
            Buffer.BlockCopy (Encoding.ASCII.GetBytes ("LEAFPACK"), 0, file, 0, 8);
            LittleEndian.Pack ((short)1, file, 8);
            Buffer.BlockCopy (encryptedPayload, 0, file, dataOffset, encryptedPayload.Length);
            Buffer.BlockCopy (encryptedIndex, 0, file, dataOffset + encryptedPayload.Length, encryptedIndex.Length);
            File.WriteAllBytes (path, file);
        }

        static byte DecryptYpfLength (byte[] table, byte value)
        {
            var position = Array.IndexOf (table, value);
            if (position < 0)
                return value;
            return table[position ^ 1];
        }

        static void EncryptPbz (byte[] data, byte[] key)
        {
            for (int i = 0; i < data.Length; ++i)
                data[i] = (byte)((data[i] - 0x80) ^ key[i % key.Length]);
        }

        static void EncryptPkz (byte[] data, byte[] key)
        {
            for (int i = 0; i < data.Length; ++i)
                data[i] = (byte)((data[i] - 0x80) ^ key[i % key.Length]);
        }

        static void WriteAzEncryptedIndexRecord (byte[] output, int offset, uint dataOffset, uint size, string name)
        {
            LittleEndian.Pack (dataOffset, output, offset);
            LittleEndian.Pack (size, output, offset + 4);
            var bytes = Encoding.ASCII.GetBytes (name);
            Buffer.BlockCopy (bytes, 0, output, offset + 0x10, Math.Min (bytes.Length, 0x1F));
        }

        static byte[] CompressBytes (byte[] data)
        {
            using (var output = new MemoryStream ())
            {
                using (var zlib = new ZLibStream (output, CompressionLevel.SmallestSize, true))
                    zlib.Write (data, 0, data.Length);
                return output.ToArray ();
            }
        }

        static void XorAzEncrypted (byte[] data, long offset, uint key)
        {
            ulong hash = key * 0x9E370001UL;
            if ((offset & 0x3F) != 0)
                hash = Binary.RotL (hash, (int)offset);
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] ^= (byte)hash;
                hash = Binary.RotL (hash, 1);
            }
        }

        static void WriteBinIdxFieldName (BinaryWriter output, string name)
        {
            var bytes = Encoding.UTF8.GetBytes (name);
            output.Write ((byte)(0xA0 | bytes.Length));
            output.Write (bytes);
        }

        static void WriteBinIdxString (BinaryWriter output, string value)
        {
            var bytes = Encoding.UTF8.GetBytes (value);
            output.Write ((byte)(0xA0 | bytes.Length));
            output.Write (bytes);
        }

        static void WriteBinIdxInt32 (BinaryWriter output, int value)
        {
            output.Write ((byte)0xD2);
            output.Write ((byte)(value >> 24));
            output.Write ((byte)(value >> 16));
            output.Write ((byte)(value >> 8));
            output.Write ((byte)value);
        }

        static byte[] CreateSpeedFixture ()
        {
            var header = new byte[0x22];
            LittleEndian.Pack ((ushort)0, header, 0);
            header[2] = 1;
            LittleEndian.Pack ((ushort)4, header, 0x16);
            LittleEndian.Pack ((ushort)1, header, 0x18);
            LittleEndian.Pack ((ushort)2, header, 0x1E);

            var plain = new byte[] { 1, 1, 4, 0, 0, 0, 0, 0 };
            using (var output = new MemoryStream ())
            using (var writer = new BinaryWriter (output, Encoding.UTF8, true))
            {
                writer.Write (header);
                writer.Write (plain.Length);
                writer.Write (new byte[] { 0, 0, 0, 0, 255, 0, 0, 255 });
                writer.Write (plain);
                return output.ToArray ();
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
