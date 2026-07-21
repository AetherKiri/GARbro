using System;
using System.IO;
using System.Linq;
using System.Text;
using GameRes;
using GameRes.Formats.KiriKiri;

namespace GARbro.Cli
{
    internal static class Program
    {
        private static int Main (string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length == 1 && args[0] == "formats")
            {
                foreach (var format in FormatCatalog.Instance.Formats.OrderBy (format => format.Tag))
                    Console.WriteLine ("{0,-12} {1}", format.Type, format.Tag);
                return 0;
            }

            if (args.Length == 1 && args[0] == "xp3-schemes")
            {
                foreach (var scheme in Xp3Opener.ModernSchemeNames)
                    Console.WriteLine (scheme);
                return 0;
            }

            if (args.Length == 2 && args[0] == "list")
                return WithXp3Scheme (null, null, () => WithArchive (args[1], ListEntries));

            if (args.Length == 6 && args[0] == "list" && args[2] == "--xp3-profile" && args[4] == "--xp3-scheme")
                return WithXp3Scheme (args[5], args[3], () => WithArchive (args[1], ListEntries));

            if (args.Length == 4 && args[0] == "list" && args[2] == "--xp3-scheme")
                return WithXp3Scheme (args[3], null, () => WithArchive (args[1], ListEntries));

            if (args.Length == 4 && args[0] == "extract" && args[2] == "--output")
                return WithXp3Scheme (null, null, () => WithArchive (args[1], archive => ExtractEntries (archive, args[3])));

            if (args.Length == 8 && args[0] == "extract" && args[2] == "--output" && args[4] == "--xp3-profile" && args[6] == "--xp3-scheme")
                return WithXp3Scheme (args[7], args[5], () => WithArchive (args[1], archive => ExtractEntries (archive, args[3])));

            if (args.Length == 6 && args[0] == "extract" && args[2] == "--output" && args[4] == "--xp3-scheme")
                return WithXp3Scheme (args[5], null, () => WithArchive (args[1], archive => ExtractEntries (archive, args[3])));

            Console.Error.WriteLine ("Usage:");
            Console.Error.WriteLine ("  garbro formats");
            Console.Error.WriteLine ("  garbro xp3-schemes");
            Console.Error.WriteLine ("  garbro list <archive> [--xp3-scheme <scheme>]");
            Console.Error.WriteLine ("  garbro list <archive> --xp3-profile <file> --xp3-scheme <scheme>");
            Console.Error.WriteLine ("  garbro extract <archive> --output <directory> [--xp3-scheme <scheme>]");
            Console.Error.WriteLine ("  garbro extract <archive> --output <directory> --xp3-profile <file> --xp3-scheme <scheme>");
            return 2;
        }

        private static int WithXp3Scheme (string scheme, string profilePath, Func<int> action)
        {
            if (!string.IsNullOrEmpty (profilePath))
            {
                try
                {
                    Xp3Opener.LoadModernSchemeProfiles (profilePath);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine ("Unable to load XP3 profile: {0}", error.Message);
                    return 2;
                }
            }
            if (!string.IsNullOrEmpty (scheme))
            {
                ICrypt algorithm;
                if (!Xp3Opener.TryGetScheme (scheme, out algorithm))
                {
                    Console.Error.WriteLine ("Unknown XP3 scheme: {0}", scheme);
                    Console.Error.WriteLine ("Available schemes: {0}", string.Join (", ", Xp3Opener.ModernSchemeNames));
                    return 2;
                }
            }
            var previous = Xp3Opener.ModernSchemeName;
            try
            {
                Xp3Opener.ModernSchemeName = scheme;
                return action();
            }
            finally
            {
                Xp3Opener.ModernSchemeName = previous;
            }
        }

        private static int WithArchive (string archivePath, Func<ArcFile, int> action)
        {
            if (!File.Exists (archivePath))
            {
                Console.Error.WriteLine ("Archive not found: {0}", archivePath);
                return 4;
            }
            try
            {
                VFS.ChDir (Path.GetFullPath (archivePath));
                var archive = VFS.CurrentArchive;
                if (archive == null)
                {
                    Console.Error.WriteLine ("Unsupported archive: {0}", archivePath);
                    return 3;
                }
                return action (archive);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine ("Unable to open archive: {0}", error.Message);
                return 3;
            }
        }

        private static int ListEntries (ArcFile archive)
        {
            foreach (var entry in archive.Dir.OrderBy (entry => entry.Offset))
                Console.WriteLine ("{0,12}  {1}", entry.Size, entry.Name);
            return 0;
        }

        private static int ExtractEntries (ArcFile archive, string outputDirectory)
        {
            var root = Path.GetFullPath (outputDirectory);
            Directory.CreateDirectory (root);
            var rootWithSeparator = root.EndsWith (Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;

            foreach (var entry in archive.Dir)
            {
                var relativePath = entry.Name.Replace ('/', Path.DirectorySeparatorChar).Replace ('\\', Path.DirectorySeparatorChar);
                var destination = Path.GetFullPath (Path.Combine (root, relativePath));
                if (!destination.StartsWith (rootWithSeparator, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine ("Skipped unsafe entry path: {0}", entry.Name);
                    continue;
                }
                var parent = Path.GetDirectoryName (destination);
                if (!string.IsNullOrEmpty (parent))
                    Directory.CreateDirectory (parent);
                using (var input = archive.OpenEntry (entry))
                using (var output = File.Create (destination))
                    input.CopyTo (output);
                Console.WriteLine ("Extracted {0}", entry.Name);
            }
            return 0;
        }
    }
}
