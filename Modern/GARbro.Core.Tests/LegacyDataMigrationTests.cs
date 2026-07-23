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

            var azEncryptedKeys = LegacyXp3Exporter.ExportAzEncryptedKeys (database);
            Assert.Equal (2, azEncryptedKeys.Count);
            Assert.Equal (2916218026u, azEncryptedKeys["Default"].IndexKey);
            Assert.Null (azEncryptedKeys["Default"].ContentKey);
            Assert.Equal (3740152942u, azEncryptedKeys["Zwei Worter"].IndexKey);
            Assert.Equal (3740152942u, azEncryptedKeys["Zwei Worter"].ContentKey);

            var pkzKeys = LegacyXp3Exporter.ExportPkzKeys (database);
            Assert.Single (pkzKeys);
            Assert.Equal ("Fall in Love", pkzKeys.Keys.Single ());
            Assert.Equal (28, pkzKeys["Fall in Love"].Length);
            Assert.Equal ("8D4CCB5CC195A6476F34959ABD7AB99EB6920954C58CEB6CD59E927271B76862",
                Convert.ToHexString (SHA256.HashData (pkzKeys["Fall in Love"])));

            var pbzKeys = LegacyXp3Exporter.ExportPbzKeys (database);
            Assert.Single (pbzKeys);
            Assert.Equal (30, pbzKeys["Karen"].ArcKey.Length);
            Assert.Equal (24, pbzKeys["Karen"].ScriptKey.Length);
            Assert.Equal ("3D075AAB6F02740CB9B0694809BA2E0F9EC8586FF4610B8A61A9FBC3C7579432",
                Convert.ToHexString (SHA256.HashData (pbzKeys["Karen"].ArcKey)));
            Assert.Equal ("4745D09320AD0246662AD41A5FBF419777F47388B0A7817DA6AA2B9C6598B88C",
                Convert.ToHexString (SHA256.HashData (pbzKeys["Karen"].ScriptKey)));

            var kcapKeys = LegacyXp3Exporter.ExportKcapKeys (database);
            Assert.Equal (2, kcapKeys.Count);
            Assert.Equal ("hahadata256pasyamada2zikan", kcapKeys["Okaa-san ga Ippai!"]);
            Assert.Equal ("E854ECC2F365DAD519C97F309BAA98B5942252147E2E496529D5B81CBD50D857",
                Convert.ToHexString (SHA256.HashData (Encoding.UTF8.GetBytes (kcapKeys["Okaa-san ga Ippai!"]))));
            Assert.Equal ("71B45C275CFF9EDD9B91DC336E4F254D4774B050F671E396BE1697DA4B24FCEA",
                Convert.ToHexString (SHA256.HashData (Encoding.UTF8.GetBytes (kcapKeys["Itazura Mahjong"]))));

            var ai5Keys = LegacyXp3Exporter.ExportAi5Keys (database);
            Assert.Equal (14, ai5Keys.Count);
            Assert.Equal (20, ai5Keys["Be-Yond"].NameLength);
            Assert.Equal ((byte)85, ai5Keys["Be-Yond"].NameKey);
            Assert.Equal (2857740885u, ai5Keys["Be-Yond"].SizeKey);
            Assert.Equal (1437226410u, ai5Keys["Be-Yond"].OffsetKey);

            var npaKeys = LegacyXp3Exporter.ExportNpaKeys (database);
            Assert.Equal (26, npaKeys.Count);
            Assert.Equal (1, npaKeys["Chaos;Head"].TitleId);
            Assert.Equal (2271560481u, npaKeys["Chaos;Head"].NameKey);
            Assert.NotEmpty (npaKeys["Chaos;Head"].Order);
            Assert.Equal (22, npaKeys["Kimi to Kanojo to Kanojo no Koi"].TitleId);
            Assert.Equal (305419896u, npaKeys["Kimi to Kanojo to Kanojo no Koi"].NameKey);

            var psbKeys = LegacyXp3Exporter.ExportPsbKeys (database);
            Assert.Equal (13, psbKeys.Length);
            Assert.Equal (970396437u, psbKeys[0]);
            Assert.Equal (439510497u, psbKeys[^1]);

            var amTable = LegacyXp3Exporter.ExportAmDecryptTable (database);
            Assert.Equal (0x10000, amTable.Length);
            Assert.Equal ("A51707E734180105297E3937EAEE3A9A1CB2357E861E20B12FE2BDBACB9F9AE9",
                Convert.ToHexString (SHA256.HashData (amTable)));

            var lpk = LegacyXp3Exporter.ExportLpkKeys (database);
            Assert.Equal (19, lpk.KnownSchemes.Count);
            Assert.Equal (22, lpk.KnownKeys.Count);
            Assert.Equal (2780408939u, lpk.KnownSchemes["Happening Love!!"].BaseKey.Key1);
            Assert.Equal (2590219749u, lpk.KnownSchemes["Happening Love!!"].BaseKey.Key2);
            Assert.Equal (2619661674u, lpk.KnownKeys["Happening Love!!"]["BGM.LPK"].Key1);
            Assert.Equal (3739757510u, lpk.KnownKeys["Happening Love!!"]["BGM.LPK"].Key2);

            var gyu = LegacyXp3Exporter.ExportGyuKeys (database);
            Assert.NotEmpty (gyu.NumericKeys);
            Assert.NotEmpty (gyu.StringKeys);
            Assert.All (gyu.NumericKeys.Values, map => Assert.NotEmpty (map));
            Assert.All (gyu.StringKeys.Values, map => Assert.NotEmpty (map));

            var ypf = LegacyXp3Exporter.ExportYpfKeys (database);
            Assert.Equal (82, ypf.Count);
            Assert.All (ypf.Values, value => Assert.NotEmpty (value.SwapTable));
            Assert.Equal ((byte)201, ypf["Unionism Quartet"].Key);
            Assert.Equal (4u, ypf["Aikagi"].ExtraHeaderSize);

            var tactics = LegacyXp3Exporter.ExportTacticsKeys (database);
            Assert.Equal (9, tactics.Count);
            Assert.Equal ("Puni0r4p", tactics["Maou no Kuse ni Namaiki da!"].Password);
            Assert.False (tactics["Maou no Kuse ni Namaiki da!"].CustomLzss);

            var rpm = LegacyXp3Exporter.ExportRpmKeys (database);
            Assert.Equal (31, rpm.Count);
            Assert.Equal ("after", rpm["After..."].Keyword);
            Assert.Equal (24, rpm["After..."].NameLength);

            var data = LegacyXp3Exporter.ExportDataKeys (database);
            Assert.Equal (3, data.Count);
            Assert.Equal (0, data["Mujina"]);
            Assert.Equal (4, data["Sandoku Ryouran"]);

            var avc = LegacyXp3Exporter.ExportAvcKeys (database);
            Assert.Equal (4, avc.Count);
            Assert.Equal ("SETSUEI-", avc[0].Password);
            Assert.Equal (8, avc[0].KeyOffset);
            Assert.Equal (16, avc[0].HeaderOffset);

            var dpk = LegacyXp3Exporter.ExportDpkKeys (database);
            Assert.Equal (9, dpk.Count);
            Assert.Equal ((uint)65432, dpk[0].Key1);
            Assert.Equal ((uint)1139247708, dpk[0].Key2);
            Assert.Equal ("默认", dpk[0].Name);

            var agsi = LegacyXp3Exporter.ExportAgsiKeys (database);
            Assert.Equal (10, agsi.Count);
            Assert.Equal (103, agsi.Sum (item => item.Value.Count));
            Assert.Equal (8, agsi["Hitsuji-tachi no Yuuutsu"]["data2.pak"].Length);

            var leaf = LegacyXp3Exporter.ExportLeafKeys (database);
            Assert.Equal (6, leaf.Count);
            Assert.Equal (11, leaf["Kizuato"].Length);
            Assert.Equal (leaf["Kizuato"], leaf["Shizuku"]);

            var ikura = LegacyXp3Exporter.ExportIkuraKeys (database);
            Assert.Equal (18, ikura.Count);
            Assert.All (ikura.Values, value => Assert.Equal (2048, value.Length));

            var fsb5 = LegacyXp3Exporter.ExportFsb5Keys (database);
            Assert.Equal (161, fsb5.Count);
            Assert.Equal (3796, fsb5[348001315u].VorbisData.Length);
            Assert.Null (fsb5[348001315u].PatchData);
            Assert.Equal (3832, fsb5[2939054206u].VorbisData.Length);
            Assert.Equal (3750, fsb5[2939054206u].PatchOffset);
            Assert.Equal (32, fsb5[2939054206u].PatchData.Length);

            var cpz = LegacyXp3Exporter.ExportCpzKeys (database);
            Assert.Equal (6, cpz.Count);
            Assert.Equal (5, cpz["Hapymaher"].Version);
            Assert.Equal (24, cpz["Hapymaher"].Cpz5Secret.Length);
            Assert.Equal (3448828041u, cpz["Hapymaher"].Cpz5Secret[0]);
            Assert.Equal (1, cpz["Hapymaher"].Md5Variant);
            Assert.Equal (443810357u, cpz["Hapymaher"].DecoderFactor);
            Assert.Equal (4, cpz["Hapymaher"].DirKeyAddend.Length);
            Assert.Equal (6, cpz["Chrono Clock"].Version);
            Assert.Equal (2, cpz["Chrono Clock"].Md5Variant);
            Assert.Equal (7, cpz["Aoi Tori"].Version);
            Assert.Equal (5, cpz["Aoi Tori"].Md5Variant);

            var repi = LegacyXp3Exporter.ExportRepiKeys (database);
            Assert.Equal (11, repi.Count);
            Assert.Equal (3, repi["Period"].Length);
            Assert.Equal (3738543313u, repi["Period"][0]);
            Assert.Equal (4203158194u, repi["Period"][1]);
            Assert.Equal (2166935461u, repi["Period"][2]);
            Assert.Equal (3, repi["Quartett! Standard Edition"].Length);
            Assert.Equal (4126854913u, repi["Quartett! Standard Edition"][2]);

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
