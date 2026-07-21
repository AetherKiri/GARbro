using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRes.Formats.KiriKiri
{
    /// <summary>
    /// Safe, data-only replacement for XP3 scheme records formerly loaded from Formats.dat.
    /// </summary>
    public sealed class Xp3SchemeProfile
    {
        public string Name { get; set; }
        public string Algorithm { get; set; }
        public CxScheme Cx { get; set; }
        public uint Seed { get; set; }
        public uint FilterKey { get; set; }
        public int RandomType { get; set; }
        public string NamesFile { get; set; }
        public byte[] IndexKey1 { get; set; }
        public byte[] IndexKey2 { get; set; }
        public Dictionary<string, HxIndexKey> IndexKeyDict { get; set; }
        public byte[] HeaderKey { get; set; }
        public long HeaderSplitPosition { get; set; }
        public bool FileCryptFlag { get; set; }
        public uint[] YuzKey { get; set; }
        public uint RiddleKey1 { get; set; } = 0xAAAAAAAA;
        public uint RiddleKey2 { get; set; } = 0x55555555;
    }

    public static class Xp3SchemeProfiles
    {
        static readonly Dictionary<string, ICrypt> s_profiles =
            new Dictionary<string, ICrypt> (StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<string> Names => s_profiles.Keys.OrderBy (name => name, StringComparer.OrdinalIgnoreCase);

        public static void Load (string path)
        {
            using (var input = File.OpenRead (path))
                Load (input);
        }

        public static void Load (Stream input)
        {
            var options = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
            options.Converters.Add (new ByteArrayConverter());
            var profiles = JsonSerializer.Deserialize<List<Xp3SchemeProfile>> (input, options);
            if (profiles == null || profiles.Count == 0)
                throw new InvalidDataException ("XP3 profile file contains no profiles.");

            var loaded = new Dictionary<string, ICrypt> (StringComparer.OrdinalIgnoreCase);
            foreach (var profile in profiles)
            {
                if (string.IsNullOrWhiteSpace (profile.Name))
                    throw new InvalidDataException ("An XP3 profile is missing its name.");
                if (!loaded.TryAdd (profile.Name, Create (profile)))
                    throw new InvalidDataException ("Duplicate XP3 profile: " + profile.Name);
            }
            s_profiles.Clear();
            foreach (var item in loaded)
                s_profiles.Add (item.Key, item.Value);
        }

        public static bool TryGet (string name, out ICrypt scheme)
        {
            return s_profiles.TryGetValue (name, out scheme);
        }

        static ICrypt Create (Xp3SchemeProfile profile)
        {
            if (profile.Cx == null)
                throw new InvalidDataException ("XP3 profile requires a Cx scheme: " + profile.Name);
            if (profile.Cx.ControlBlock == null || profile.Cx.PrologOrder == null
                || profile.Cx.OddBranchOrder == null || profile.Cx.EvenBranchOrder == null)
                throw new InvalidDataException ("XP3 profile has incomplete Cx data: " + profile.Name);

            switch (profile.Algorithm)
            {
            case "CxEncryption":
                return new CxEncryption (profile.Cx);
            case "SenrenCxCrypt":
                return new SenrenCxCrypt (profile.Cx);
            case "CabbageCxCrypt":
                return new CabbageCxCrypt (profile.Cx, profile.Seed);
            case "NanaCxCrypt":
                return new NanaCxCrypt (profile.Cx, profile.Seed) { YuzKey = RequireYuzKey (profile) };
            case "RiddleCxCrypt":
                return new RiddleCxCrypt (profile.Cx, profile.Seed) {
                    YuzKey = RequireYuzKey (profile),
                    m_key1 = profile.RiddleKey1,
                    m_key2 = profile.RiddleKey2,
                };
            case "HxCrypt":
                return new HxCrypt (profile.Cx) {
                    FilterKey = profile.FilterKey,
                    RandomType = profile.RandomType,
                    NamesFile = profile.NamesFile,
                    IndexKey1 = profile.IndexKey1,
                    IndexKey2 = profile.IndexKey2,
                    IndexKeyDict = profile.IndexKeyDict,
                };
            case "HxCryptLite":
                var lite = new HxCryptLite (profile.Cx);
                lite.SetHeaderDecryptParam (profile.HeaderKey, profile.HeaderSplitPosition);
                lite.SetSingleByteCryptFlag (profile.FileCryptFlag);
                lite.SetRandomType (profile.RandomType);
                return lite;
            default:
                throw new InvalidDataException ("Unsupported XP3 profile algorithm: " + profile.Algorithm);
            }
        }

        static uint[] RequireYuzKey (Xp3SchemeProfile profile)
        {
            if (profile.YuzKey == null || profile.YuzKey.Length < 6)
                throw new InvalidDataException ("XP3 profile requires six YuzKey values: " + profile.Name);
            return profile.YuzKey;
        }

        sealed class ByteArrayConverter : JsonConverter<byte[]>
        {
            public override byte[] Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                    return reader.GetBytesFromBase64();
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException ("Expected a byte array or Base64 string.");

                var values = new List<byte>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.Number || !reader.TryGetByte (out var value))
                        throw new JsonException ("Byte array values must be integers from 0 to 255.");
                    values.Add (value);
                }
                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException ("Unterminated byte array.");
                return values.ToArray();
            }

            public override void Write (Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                foreach (var item in value)
                    writer.WriteNumberValue (item);
                writer.WriteEndArray();
            }
        }
    }
}
