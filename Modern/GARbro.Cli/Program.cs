using System;
using System.IO;
using System.Linq;
using System.Text;
using GameRes;

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

            if (args.Length == 2 && args[0] == "list")
                return WithArchive (args[1], ListEntries);

            if (args.Length == 4 && args[0] == "extract" && args[2] == "--output")
                return WithArchive (args[1], archive => ExtractEntries (archive, args[3]));

            Console.Error.WriteLine ("Usage:");
            Console.Error.WriteLine ("  garbro formats");
            Console.Error.WriteLine ("  garbro list <archive>");
            Console.Error.WriteLine ("  garbro extract <archive> --output <directory>");
            return 2;
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
