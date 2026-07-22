using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GARbro.LegacyDataMigration;
using GameRes.Formats.KiriKiri;
using Xunit;

namespace GARbro.Core.Tests
{
    public class LegacyDataMigrationTests
    {
        [Fact]
        public void Legacy_reader_rejects_a_file_without_the_GARbro_header ()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText (path, "not a GARbro database", Encoding.UTF8);

                Assert.Throws<InvalidDataException> (() => LegacyFormatsReader.Read (path));
            }
            finally
            {
                File.Delete (path);
            }
        }

        [Fact]
        public void Trusted_legacy_database_exports_valid_v2_xp3_profiles ()
        {
            var database = LegacyFormatsReader.Read (FixturePath);

            Assert.Equal (148, database.Version);
            Assert.Equal ("GameRes.SchemeDataBase", database.Root.TypeName.FullName);
            Assert.Equal (18510, database.Records.Count);

            var document = LegacyXp3Exporter.Export (database, out var report);

            Assert.Equal (1, document.SchemaVersion);
            Assert.Equal (453, report.SourceKnownSchemeCount);
            Assert.Equal (144, report.ExportedProfileCount);
            Assert.Equal (309, report.SkippedProfiles.Count);
            Assert.Equal (21, document.Profiles.Count (profile => profile.Algorithm == "hx"));
            Assert.DoesNotContain (report.SkippedProfiles,
                profile => profile.LegacyType == "GameRes.Formats.KiriKiri.HxCrypt");

            var gameMap = LegacyXp3Exporter.ExportGameMap (database);
            Assert.Equal (1129, gameMap.Count);
            Assert.Contains (gameMap, item => item.Key == "SenrenBanka.exe" && item.Value == "Senren＊Banka");

            var inventory = LegacyXp3Exporter.ExportSchemeInventory (database);
            Assert.Equal (148, inventory.SourceDatabaseVersion);
            Assert.Equal (77, inventory.Schemes.Count);
            Assert.Equal (64, inventory.Types.Count);
            Assert.Contains (inventory.Schemes, scheme => scheme.Tag == "ZIP"
                && scheme.LegacyType == "GameRes.Formats.PkWare.ZipScheme"
                && scheme.Members.SequenceEqual (new[] { "KnownKeys" }));

            var zipKeys = LegacyXp3Exporter.ExportZipKeys (database);
            Assert.Equal (7, zipKeys.Count);
            Assert.Equal ("trendri0da0", zipKeys["Choir"]);

            var tcdKeys = LegacyXp3Exporter.ExportTcdKeys (database);
            Assert.Equal (3, tcdKeys.Count);
            Assert.Equal (327047585, tcdKeys["Atori no Sora to Shinchuu no Tsuki"]);
            Assert.Equal (-982448593, tcdKeys["Favorite Sweet!"]);

            var morningKey = LegacyXp3Exporter.ExportMorningKey (database);
            Assert.Equal (512, morningKey.Length);
            Assert.Equal ("9CCCDA839FCC8EF6F231A407E07286F3FDE46A97E46EF0F995094AC20379247A",
                Convert.ToHexString (SHA256.HashData (morningKey)));

            var fpkKeys = LegacyXp3Exporter.ExportFpkKeys (database);
            Assert.Equal (28, fpkKeys.Length);
            Assert.Equal ((uint)0, fpkKeys[0]);
            Assert.Equal (86494307u, fpkKeys[1]);
            Assert.Equal (4064587902u, fpkKeys[^1]);

            foreach (var profile in document.Profiles)
            {
                Assert.True (Xp3Opener.TryGetScheme (profile.Id, out var scheme), profile.Id);
                Assert.NotNull (scheme);
            }

            var hxProfile = document.Profiles.First (profile => profile.Algorithm == "hx");
            Assert.True (Xp3Opener.TryGetScheme (hxProfile.Id, out var hxScheme));
            Assert.IsType<HxCrypt> (hxScheme);
        }

        static string FixturePath => Path.Combine (AppContext.BaseDirectory, "Fixtures", "Formats.dat");
    }
}
