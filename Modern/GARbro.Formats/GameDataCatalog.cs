using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameRes.Formats
{
    /// <summary>
    /// Loads reviewed, data-only game records embedded in the modern formats assembly.
    /// </summary>
    internal static class GameDataCatalog
    {
        const int SupportedSchemaVersion = 1;
        const string ManifestPath = "GameData/v2/manifest.json";

        static readonly Lazy<GameDataManifest> s_manifest = new Lazy<GameDataManifest> (LoadManifest);
        static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
        };

        internal static T LoadDataset<T> (string id)
        {
            if (string.IsNullOrWhiteSpace (id))
                throw new ArgumentException ("A game-data dataset id is required.", nameof (id));

            var dataset = s_manifest.Value.Datasets.SingleOrDefault (entry => entry.Id == id);
            if (dataset == null)
                throw new InvalidDataException ("Unknown game-data dataset: " + id);
            ValidateDataset (dataset);

            var bytes = ReadResource (dataset.Path);
            var actualHash = Convert.ToHexString (SHA256.HashData (bytes));
            if (!string.Equals (dataset.Sha256, actualHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException ("Game-data checksum mismatch: " + id);

            var value = JsonSerializer.Deserialize<T> (bytes, s_jsonOptions);
            if (value == null)
                throw new InvalidDataException ("Game-data dataset is empty: " + id);
            return value;
        }

        /// <summary>
        /// Validate every manifest-referenced embedded dataset without coupling the
        /// catalog to format-specific DTOs. CI uses this as the bundle integrity gate.
        /// </summary>
        internal static void ValidateAllDatasets ()
        {
            foreach (var dataset in s_manifest.Value.Datasets)
            {
                ValidateDataset (dataset);
                var bytes = ReadResource (dataset.Path);
                var actualHash = Convert.ToHexString (SHA256.HashData (bytes));
                if (!string.Equals (dataset.Sha256, actualHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException ("Game-data checksum mismatch: " + dataset.Id);
                using (JsonDocument.Parse (bytes))
                {
                }
            }
        }

        static GameDataManifest LoadManifest ()
        {
            var manifest = JsonSerializer.Deserialize<GameDataManifest> (ReadResource (ManifestPath), s_jsonOptions);
            if (manifest == null || manifest.Datasets == null)
                throw new InvalidDataException ("Game-data manifest is invalid.");
            if (manifest.SchemaVersion != SupportedSchemaVersion)
                throw new InvalidDataException ("Unsupported game-data schema version: " + manifest.SchemaVersion);
            if (string.IsNullOrWhiteSpace (manifest.DataVersion))
                throw new InvalidDataException ("Game-data manifest is missing its data version.");

            var ids = new HashSet<string> (StringComparer.Ordinal);
            foreach (var dataset in manifest.Datasets)
            {
                ValidateDataset (dataset);
                if (!ids.Add (dataset.Id))
                    throw new InvalidDataException ("Duplicate game-data dataset: " + dataset.Id);
            }
            return manifest;
        }

        static void ValidateDataset (GameDataDataset dataset)
        {
            if (dataset == null || string.IsNullOrWhiteSpace (dataset.Id)
                || string.IsNullOrWhiteSpace (dataset.Format) || string.IsNullOrWhiteSpace (dataset.Kind))
                throw new InvalidDataException ("Game-data dataset metadata is incomplete.");
            if (string.IsNullOrWhiteSpace (dataset.Path) || dataset.Path.StartsWith ("/", StringComparison.Ordinal)
                || dataset.Path.Contains ("..") || !dataset.Path.EndsWith (".json", StringComparison.Ordinal))
                throw new InvalidDataException ("Game-data dataset path is invalid: " + dataset?.Path);
            if (string.IsNullOrWhiteSpace (dataset.Sha256) || dataset.Sha256.Length != 64
                || dataset.Sha256.Any (value => !Uri.IsHexDigit (value)))
                throw new InvalidDataException ("Game-data dataset checksum is invalid: " + dataset.Id);
        }

        static byte[] ReadResource (string path)
        {
            var suffix = "." + path.Replace ('/', '.');
            var assembly = typeof(GameDataCatalog).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .SingleOrDefault (name => name.EndsWith (suffix, StringComparison.Ordinal));
            if (resourceName == null)
                throw new InvalidDataException ("Embedded game-data resource is missing: " + path);

            using (var input = assembly.GetManifestResourceStream (resourceName))
            using (var output = new MemoryStream())
            {
                if (input == null)
                    throw new InvalidDataException ("Unable to open game-data resource: " + path);
                input.CopyTo (output);
                return output.ToArray();
            }
        }
    }

    internal sealed class GameDataManifest
    {
        public int SchemaVersion { get; set; }
        public string DataVersion { get; set; }
        public List<GameDataDataset> Datasets { get; set; }
    }

    internal sealed class GameDataDataset
    {
        public string Id { get; set; }
        public string Format { get; set; }
        public string Kind { get; set; }
        public string Path { get; set; }
        public string Sha256 { get; set; }
    }
}
