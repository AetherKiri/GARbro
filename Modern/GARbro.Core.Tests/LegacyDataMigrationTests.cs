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

            var cmpKeys = LegacyXp3Exporter.ExportCmpKeys (database);
            Assert.Equal (2, cmpKeys.Count);
            Assert.Equal ("AC00C467493B0633599D49B681CCDA0BB1A7E3D281E69FD8434CE7AFACBC8DDE",
                Convert.ToHexString (SHA256.HashData (cmpKeys["Summer Radish Vacation!! 2"])));
            Assert.Equal ("2C43729D693F01F9415897FEB439B08FFF8E33CE23AA55D17E5C0357E393A63C",
                Convert.ToHexString (SHA256.HashData (cmpKeys["Imouto de Ikou!"])));

            var pkgKeys = LegacyXp3Exporter.ExportPkgKeys (database);
            Assert.Single (pkgKeys);
            var pkgKey = pkgKeys["Seisai no Resonance"];
            Assert.Equal (8, pkgKey.Length);
            Assert.Equal (2705044775u, pkgKey[0]);
            Assert.Equal (4159235687u, pkgKey[^1]);

            var csafKeys = LegacyXp3Exporter.ExportCsafKeys (database);
            Assert.Single (csafKeys);
            Assert.Equal ("招子", csafKeys["Nanairo * Clip ~Saigo no Stage~"]);

            var mblKeys = LegacyXp3Exporter.ExportMblKeys (database);
            Assert.Equal (58, mblKeys.Count);
            Assert.Equal ("amai_seikatu", mblKeys["Amai Seikatsu"]);
            Assert.Equal ("", mblKeys["Candy Toys"]);

            var npkKeys = LegacyXp3Exporter.ExportNpkKeys (database);
            Assert.Equal (4, npkKeys.Count);
            Assert.All (npkKeys.Values, value => Assert.NotEmpty (value));
            Assert.All (npkKeys.Values, value => Assert.Equal (32, value.Length));
            Assert.Equal ("D73FE8D142818BDF2A6B2AED6C0170F4085A1D6D81A2A2A083DB3631E2CEFCED",
                Convert.ToHexString (SHA256.HashData (npkKeys["Sonicomi"])));

            var pckKeys = LegacyXp3Exporter.ExportPckKeys (database);
            Assert.Equal (4, pckKeys.Count);
            Assert.Equal (10, pckKeys["Boukensha no Machi o Tsukurou! 2"].Length);
            Assert.Equal (5, pckKeys["Mezase My Home! ~Niizuma o Mamore~"].Length);
            Assert.Equal ("406C8C66DEAF641974D7BB240ED9685664174AF13A5C178A61959C4D9F526D48",
                Convert.ToHexString (SHA256.HashData (pckKeys["Boukensha no Machi o Tsukurou! 2"])));

            var nsaKeys = LegacyXp3Exporter.ExportNsaKeys (database);
            var ns2Keys = LegacyXp3Exporter.ExportNsaKeys (database, "NS2");
            Assert.Equal (10, nsaKeys.Count);
            Assert.Equal (6, ns2Keys.Count);
            Assert.Equal ("AD0A5B4CDC2BE77D29B15057E73A0CA883B8152181C0F47C8C07FB8219733B89",
                Convert.ToHexString (SHA256.HashData (Encoding.UTF8.GetBytes (nsaKeys["Chou Gedou Yuusha"]))));
            Assert.Equal ("FDA31CFCA28F42D9E49B4443CC8A7A223E8B30C89743DD75595285DE60B6A2D0",
                Convert.ToHexString (SHA256.HashData (Encoding.UTF8.GetBytes (ns2Keys["Daydream Believer"]))));

            var fjsysKeys = LegacyXp3Exporter.ExportFjsysKeys (database);
            Assert.Equal (22, fjsysKeys.Count);
            Assert.Equal ("秋のうららの～あかね色商店街～",
                fjsysKeys["Aki no Urara no ~Akaneiro Shoutengai~"]);

            var intKeys = LegacyXp3Exporter.ExportIntKeys (database);
            Assert.Equal (24, intKeys.Count);
            Assert.Equal (4081182131u, intKeys["Amakano"].Key);
            Assert.Equal ("NXT-M81ERGNE", intKeys["Amakano"].Passphrase);

            var noaKeys = LegacyXp3Exporter.ExportNoaKeys (database);
            Assert.Equal (26, noaKeys.Count);
            Assert.Equal ("convini_cat", noaKeys["Konneko"]["script.noa"]);
            Assert.Equal (4, noaKeys["Koishiki Manual"].Count);

            var galKeys = LegacyXp3Exporter.ExportGalKeys (database);
            Assert.Equal (3, galKeys.Count);
            Assert.Equal ("2011", galKeys["Grope ~Yami no Naka no Kotori-tachi~"]);
            Assert.Equal ("SRuB", galKeys["Inclusion"]);

            var crzKeys = LegacyXp3Exporter.ExportCrzKeys (database);
            Assert.Equal (2, crzKeys.Count);
            Assert.All (crzKeys.Values, value => Assert.Equal (0x24, value.Length));
            Assert.Equal ("55C90ACAEE17F959B622659EEFE9769889056A20BFA5C2A49F36562300755627",
                Convert.ToHexString (SHA256.HashData (crzKeys["(C)CROWD MissYou"])));

            var actgsKeys = LegacyXp3Exporter.ExportActgsKeys (database);
            Assert.Equal (6, actgsKeys.Length);
            Assert.All (actgsKeys, value => Assert.True (value.Length >= 4));
            Assert.Equal ("E69468362728969614674D892D49CFA4B6CC2720F3E5D6C3137A614E04138674",
                Convert.ToHexString (SHA256.HashData (actgsKeys[0])));

            var adsKeys = LegacyXp3Exporter.ExportAdsKeys (database);
            Assert.Equal (2, adsKeys.Count);
            Assert.Equal (256, adsKeys["Soukan Yuugi 2"].Length);
            Assert.Equal ("D95D2856332D51E913377C58AB6B4CD39120F3E4C3CC3EB687164D6B44CEFA6B",
                Convert.ToHexString (SHA256.HashData (adsKeys["Soukan Yuugi 2"])));

            var arcgKeys = LegacyXp3Exporter.ExportArcgKeys (database);
            Assert.Single (arcgKeys);
            Assert.Equal ("\u300E\u30DE\u30DE\u3055\u3093\u30D0\u30EC\u30FC((\u4E73\u3086\u308C\u307E\u3093\u305B\u30FC))\u300F",
                arcgKeys[1522388286u]);

            var mgpkKeys = LegacyXp3Exporter.ExportMgpkKeys (database);
            Assert.Equal (4, mgpkKeys.Count);
            Assert.Equal ("5WW6Gj3Gf55GFYk=", Convert.ToBase64String (mgpkKeys["Cartagra"]));
            Assert.Equal ("omW6Gi3Gf5NGFYQ=", Convert.ToBase64String (mgpkKeys["Kara no Shoujo 2"]));

            var rctKeys = LegacyXp3Exporter.ExportRctKeys (database);
            Assert.Equal (33, rctKeys.Count);
            Assert.Equal ("\u59C9\u30CB\u30E2\u30DE\u30B1\u30BA", rctKeys["Ane ni mo Makezu"]);

            var mcgKeys = LegacyXp3Exporter.ExportMcgKeys (database);
            Assert.Equal (24, mcgKeys.Count);
            Assert.Equal ((byte)1, mcgKeys["Echo"]);

            var tinkKeys = LegacyXp3Exporter.ExportTinkKeys (database);
            Assert.Equal (2, tinkKeys.Count);
            Assert.Equal (66, tinkKeys[1735290707u].Length);
            Assert.Equal (99, tinkKeys[1802398036u].Length);
            Assert.Equal ("NDkyMzNFRDQ5MTFFNDhjNjhFQkYxRERBQ0UzQTc3NTJBOEI1MkQzRDEzQzM0ZTUwOUZCRS1FM0VGREUzRjJENjEA",
                Convert.ToBase64String (tinkKeys[1735290707u]));
            Assert.Equal ("REJCMzIwNkYtRjE3MS00ODg1LUExMzEtRUM3RkJBNkZGNDkxIENvcHlyaWdodCAyMDA0IEN5YmVyd29ya3MgIlRpbmtlckJlbGwiLiwgYWxsIHJpZ2h0cyByZXNlcnZlZC4A",
                Convert.ToBase64String (tinkKeys[1802398036u]));

            var binIdxKeys = LegacyXp3Exporter.ExportBinIdxKeys (database);
            Assert.Single (binIdxKeys);
            Assert.Equal ("GuildMaster", binIdxKeys.Keys.Single ());
            Assert.Equal (32, binIdxKeys["GuildMaster"].Key.Length);
            Assert.Equal (16, binIdxKeys["GuildMaster"].IV.Length);
            Assert.Equal ("YzZlYWhicTlzanVhd2h2ZHI5a3ZocHNtNXF2MzkzZ2E=",
                Convert.ToBase64String (binIdxKeys["GuildMaster"].Key));
            Assert.Equal ("QVJDLVBBQ0tQQVNTV09SRA==",
                Convert.ToBase64String (binIdxKeys["GuildMaster"].IV));

            var asbKeys = LegacyXp3Exporter.ExportAsbKeys (database);
            Assert.Equal (4, asbKeys.Count);
            Assert.Equal (2938115999u, asbKeys["Amaenbou"]);
            Assert.Equal (3786541434u, asbKeys["Clover Heart's"]);

            var sjDatKeys = LegacyXp3Exporter.ExportSjDatKeys (database);
            Assert.Equal (5, sjDatKeys.Count);
            Assert.All (sjDatKeys.Values, value => Assert.Equal (16, value.Length));
            Assert.Equal ("31F5D8670739A132BD30232682E3921E18BEBA80E371730B8EDA0DECBEF69CC3",
                Convert.ToHexString (SHA256.HashData (sjDatKeys["Bias {biAs+}"])));
            Assert.Equal ("ED4D6221764CDF23FFD735661B8A5513DD788D2F5DCBD748FFF079C3268A3DE2",
                Convert.ToHexString (SHA256.HashData (sjDatKeys["Giin Oyako"])));

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
