using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Formats.Nrbf;

namespace GARbro.LegacyDataMigration
{
    internal static class Program
    {
        static int Main (string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "inspect")
                {
                    Inspect (args[1]);
                    return 0;
                }
                if (args.Length == 4 && args[0] == "export-xp3")
                {
                    ExportXp3 (args[1], args[2], args[3]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-xp3-game-map")
                {
                    ExportXp3GameMap (args[1], args[2]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-scheme-inventory")
                {
                    ExportSchemeInventory (args[1], args[2]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-zip-keys")
                {
                    ExportZipKeys (args[1], args[2]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-tcd-keys")
                {
                    ExportTcdKeys (args[1], args[2]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-morning-key")
                {
                    ExportMorningKey (args[1], args[2]);
                    return 0;
                }
                if (args.Length == 3 && args[0] == "export-fpk-keys")
                {
                    ExportFpkKeys (args[1], args[2]);
                    return 0;
                }

                Console.Error.WriteLine ("Usage: garbro-legacy-data-migration inspect <trusted-Formats.dat>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-xp3 <trusted-Formats.dat> <profiles.json> <report.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-xp3-game-map <trusted-Formats.dat> <game-map.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-scheme-inventory <trusted-Formats.dat> <inventory.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-zip-keys <trusted-Formats.dat> <keys.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-tcd-keys <trusted-Formats.dat> <keys.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-morning-key <trusted-Formats.dat> <key.json>");
                Console.Error.WriteLine ("       garbro-legacy-data-migration export-fpk-keys <trusted-Formats.dat> <keys.json>");
                return 2;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine ("Migration failed: {0}", error.Message);
                return 1;
            }
        }

        static void Inspect (string path)
        {
            var database = LegacyFormatsReader.Read (path);
            Console.WriteLine ("databaseVersion={0}", database.Version);
            Console.WriteLine ("rootType={0}", database.Root.TypeName.FullName);
            Console.WriteLine ("recordCount={0}", database.Records.Count);

            var types = database.Records.Values.OfType<ClassRecord>()
                .Where (record => record.TypeName.FullName.StartsWith ("GameRes.Formats.KiriKiri.", StringComparison.Ordinal))
                .GroupBy (record => record.TypeName.FullName)
                .OrderBy (group => group.Key, StringComparer.Ordinal);
            foreach (var type in types)
                Console.WriteLine ("kirikiriType={0}; count={1}", type.Key, type.Count());

            var hxDictionaries = database.Records.Values.OfType<ClassRecord>()
                .Where (record => string.Equals (record.TypeName.FullName, "GameRes.Formats.KiriKiri.HxCrypt", StringComparison.Ordinal))
                .Select (record => record.HasMember ("IndexKeyDict") ? record.GetRawValue ("IndexKeyDict") as ClassRecord : null)
                .Where (record => record != null)
                .GroupBy (record => string.Join (",", record.MemberNames.OrderBy (name => name, StringComparer.Ordinal)))
                .OrderBy (group => group.Key, StringComparer.Ordinal);
            foreach (var dictionary in hxDictionaries)
                Console.WriteLine ("hxIndexKeyDictionaryMembers={0}; count={1}", dictionary.Key, dictionary.Count());
        }

        static void ExportXp3 (string inputPath, string profilesPath, string reportPath)
        {
            EnsureNewFile (profilesPath);
            EnsureNewFile (reportPath);
            var profiles = LegacyXp3Exporter.Export (LegacyFormatsReader.Read (inputPath), out var report);
            WriteJson (profilesPath, profiles);
            WriteJson (reportPath, report);
            Console.WriteLine ("exportedProfiles={0}", report.ExportedProfileCount);
            Console.WriteLine ("skippedProfiles={0}", report.SkippedProfiles.Count);
        }

        static void ExportXp3GameMap (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var map = LegacyXp3Exporter.ExportGameMap (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, new Xp3GameMapDocument { GameMap = map });
            Console.WriteLine ("exportedGameBindings={0}", map.Count);
        }

        static void ExportSchemeInventory (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var inventory = LegacyXp3Exporter.ExportSchemeInventory (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, inventory);
            Console.WriteLine ("schemeCount={0}", inventory.Schemes.Count);
            Console.WriteLine ("schemeTypeCount={0}", inventory.Types.Count);
        }

        static void ExportZipKeys (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var keys = LegacyXp3Exporter.ExportZipKeys (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, new Xp3ZipKeysDocument { KnownKeys = keys });
            Console.WriteLine ("zipKeyCount={0}", keys.Count);
        }

        static void ExportTcdKeys (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var keys = LegacyXp3Exporter.ExportTcdKeys (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, new TcdKeysDocument { KnownKeys = keys });
            Console.WriteLine ("tcdKeyCount={0}", keys.Count);
        }

        static void ExportMorningKey (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var key = LegacyXp3Exporter.ExportMorningKey (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, new MorningKeyDocument { DefaultKey = key });
            Console.WriteLine ("morningKeyLength={0}", key.Length);
        }

        static void ExportFpkKeys (string inputPath, string outputPath)
        {
            EnsureNewFile (outputPath);
            var keys = LegacyXp3Exporter.ExportFpkKeys (LegacyFormatsReader.Read (inputPath));
            WriteJson (outputPath, new FpkKeysDocument { KnownKeys = keys });
            Console.WriteLine ("fpkKeyCount={0}", keys.Length);
        }

        static void EnsureNewFile (string path)
        {
            if (File.Exists (path))
                throw new IOException ("Refusing to overwrite existing file: " + path);
            var directory = Path.GetDirectoryName (Path.GetFullPath (path));
            if (!string.IsNullOrEmpty (directory))
                Directory.CreateDirectory (directory);
        }

        static void WriteJson<T> (string path, T value)
        {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
            File.WriteAllText (path, JsonSerializer.Serialize (value, options) + Environment.NewLine);
        }
    }
}
