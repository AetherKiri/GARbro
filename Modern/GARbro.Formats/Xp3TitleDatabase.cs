using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameRes.Formats.KiriKiri
{
    /// <summary>
    /// Safe, data-only subset of the original XP3 title registry.
    /// </summary>
    internal static class Xp3TitleDatabase
    {
        static readonly Lazy<Data> s_data = new Lazy<Data> (Load);

        internal static IEnumerable<string> SchemeTitles => s_data.Value.Schemes.Keys.OrderBy (title => title, StringComparer.OrdinalIgnoreCase);

        internal static bool TryGetGameTitle (string name, out string title)
        {
            return s_data.Value.GameMap.TryGetValue (Path.GetFileName (name), out title);
        }

        internal static bool TryGetAlgorithm (string title, out string algorithm)
        {
            return s_data.Value.Schemes.TryGetValue (title, out algorithm);
        }

        static Data Load ()
        {
            var data = GameRes.Formats.GameDataCatalog.LoadDataset<Data> ("xp3-title-registry");
            if (data.GameMap == null || data.Schemes == null)
                throw new InvalidDataException ("XP3 title registry is invalid.");
            data.GameMap = Normalize (data.GameMap, "executable name");
            data.Schemes = Normalize (data.Schemes, "title");
            return data;
        }

        static Dictionary<string, string> Normalize (IEnumerable<KeyValuePair<string, string>> source, string label)
        {
            var normalized = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (string.IsNullOrWhiteSpace (item.Key) || string.IsNullOrWhiteSpace (item.Value))
                    throw new InvalidDataException ("XP3 registry has an incomplete " + label + " record.");
                string existing;
                if (!normalized.TryGetValue (item.Key, out existing))
                    normalized.Add (item.Key, item.Value);
                else if (!string.Equals (existing, item.Value, StringComparison.Ordinal))
                    throw new InvalidDataException ("XP3 registry has conflicting " + label + ": " + item.Key);
            }
            return normalized;
        }

        internal sealed class Data
        {
            public Dictionary<string, string> GameMap { get; set; }
            public Dictionary<string, string> Schemes { get; set; }
        }
    }
}
