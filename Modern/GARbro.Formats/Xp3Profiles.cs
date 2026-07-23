using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRes.Formats.KiriKiri
{
    /// <summary>
    /// Data-only XP3 profile document. Algorithm-specific parameters live under each record's parameters object.
    /// </summary>
    public sealed class Xp3ProfileDocument
    {
        public int SchemaVersion { get; set; }
        public List<Xp3ProfileRecord> Profiles { get; set; }
    }

    public sealed class Xp3ProfileRecord
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Algorithm { get; set; }
        public JsonElement Parameters { get; set; }
    }

    public sealed class Xp3CxData
    {
        public uint Mask { get; set; }
        public uint Offset { get; set; }
        public byte[] PrologOrder { get; set; }
        public byte[] OddBranchOrder { get; set; }
        public byte[] EvenBranchOrder { get; set; }
        public uint[] ControlBlock { get; set; }
        public string TpmFileName { get; set; }
    }

    public sealed class Xp3CxParameters
    {
        public Xp3CxData Cx { get; set; }
    }

    public sealed class Xp3SeededCxParameters
    {
        public Xp3CxData Cx { get; set; }
        public uint Seed { get; set; }
    }

    public sealed class Xp3NanaCxParameters
    {
        public Xp3CxData Cx { get; set; }
        public uint Seed { get; set; }
        public uint[] YuzKey { get; set; }
    }

    public sealed class Xp3RiddleCxParameters
    {
        public Xp3CxData Cx { get; set; }
        public uint Seed { get; set; }
        public uint[] YuzKey { get; set; }
        public uint Key1 { get; set; } = 0xAAAAAAAA;
        public uint Key2 { get; set; } = 0x55555555;
    }

    public sealed class Xp3HxIndexKeyData
    {
        public byte[] Key1 { get; set; }
        public byte[] Key2 { get; set; }
    }

    public sealed class Xp3HxParameters
    {
        public Xp3CxData Cx { get; set; }
        public ulong FilterKey { get; set; }
        public int RandomType { get; set; }
        public string NamesFile { get; set; }
        public byte[] IndexKey1 { get; set; }
        public byte[] IndexKey2 { get; set; }
        public Dictionary<string, Xp3HxIndexKeyData> IndexKeys { get; set; }
    }

    public sealed class Xp3HxLiteParameters
    {
        public Xp3CxData Cx { get; set; }
        public byte[] HeaderKey { get; set; }
        public long HeaderSplitPosition { get; set; }
        public bool FileCryptFlag { get; set; }
        public int RandomType { get; set; }
    }

    public sealed class Xp3SeedParameters
    {
        public uint Seed { get; set; }
    }

    public sealed class Xp3ByteKeyParameters
    {
        public byte Key { get; set; }
    }

    public sealed class Xp3SmileParameters
    {
        public uint KeyXor { get; set; }
        public byte FirstXor { get; set; }
        public byte ZeroXor { get; set; }
    }

    public sealed class Xp3SmxParameters
    {
        public int Mask { get; set; }
        public byte[] KeySeq { get; set; }
    }

    /// <summary>
    /// Compatibility DTO for the pre-v2 profile array. New data must use Xp3ProfileDocument.
    /// </summary>
    [Obsolete ("Use Xp3ProfileDocument with algorithm-specific parameters for new profiles.")]
    public sealed class Xp3SchemeProfile
    {
        public string Name { get; set; }
        public string Title { get; set; }
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
        const int SchemaVersion = 1;

        static readonly Dictionary<string, ICrypt> s_profiles =
            new Dictionary<string, ICrypt> (StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, string> s_titles =
            new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
        static readonly JsonSerializerOptions s_jsonOptions = CreateJsonOptions();
        static readonly Lazy<bool> s_bundledLoaded = new Lazy<bool> (LoadBundled);

        public static IEnumerable<string> Names
        {
            get
            {
                EnsureBundled();
                return s_profiles.Keys.OrderBy (name => name, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Load (string path)
        {
            using (var input = File.OpenRead (path))
                Load (input);
        }

        public static void Load (Stream input)
        {
            if (input == null)
                throw new ArgumentNullException (nameof (input));
            EnsureBundled();

            string text;
            using (var reader = new StreamReader (input, Encoding.UTF8, true, 4096, true))
                text = reader.ReadToEnd();
            using (var document = JsonDocument.Parse (text))
            {
                List<LoadedProfile> profiles;
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                    profiles = LoadLegacyProfiles (text);
                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                    profiles = LoadV2Profiles (text);
                else
                    throw new InvalidDataException ("XP3 profile input must be an array or document object.");
                SetProfiles (profiles);
            }
        }

        public static bool TryGet (string name, out ICrypt scheme)
        {
            EnsureBundled();
            return s_profiles.TryGetValue (name, out scheme);
        }

        public static string GetTitle (string name)
        {
            EnsureBundled();
            string title;
            return s_titles.TryGetValue (name, out title) ? title : name;
        }

        static void EnsureBundled ()
        {
            _ = s_bundledLoaded.Value;
        }

        static bool LoadBundled ()
        {
            var document = GameRes.Formats.GameDataCatalog.LoadDataset<Xp3ProfileDocument> ("xp3-profiles");
            SetProfiles (LoadV2Document (document));
            return true;
        }

        static JsonSerializerOptions CreateJsonOptions ()
        {
            var options = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };
            options.Converters.Add (new ByteArrayConverter());
            return options;
        }

        static List<LoadedProfile> LoadV2Profiles (string text)
        {
            var document = JsonSerializer.Deserialize<Xp3ProfileDocument> (text, s_jsonOptions);
            return LoadV2Document (document);
        }

        static List<LoadedProfile> LoadV2Document (Xp3ProfileDocument document)
        {
            if (document == null || document.Profiles == null || document.Profiles.Count == 0)
                throw new InvalidDataException ("XP3 profile document contains no profiles.");
            if (document.SchemaVersion != SchemaVersion)
                throw new InvalidDataException ("Unsupported XP3 profile schema version: " + document.SchemaVersion);

            var profiles = new List<LoadedProfile> (document.Profiles.Count);
            foreach (var profile in document.Profiles)
            {
                if (profile == null || string.IsNullOrWhiteSpace (profile.Id))
                    throw new InvalidDataException ("An XP3 v2 profile is missing its id.");
                if (string.IsNullOrWhiteSpace (profile.Algorithm))
                    throw new InvalidDataException ("An XP3 v2 profile is missing its algorithm: " + profile.Id);
                if (profile.Parameters.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException ("XP3 v2 profile parameters must be an object: " + profile.Id);

                profiles.Add (new LoadedProfile {
                    Id = profile.Id,
                    Title = string.IsNullOrWhiteSpace (profile.Title) ? profile.Id : profile.Title,
                    Crypt = CreateV2 (profile),
                });
            }
            return profiles;
        }

        static List<LoadedProfile> LoadLegacyProfiles (string text)
        {
#pragma warning disable CS0618
            var legacyProfiles = JsonSerializer.Deserialize<List<Xp3SchemeProfile>> (text, s_jsonOptions);
#pragma warning restore CS0618
            if (legacyProfiles == null || legacyProfiles.Count == 0)
                throw new InvalidDataException ("XP3 profile file contains no profiles.");

            var profiles = new List<LoadedProfile> (legacyProfiles.Count);
            foreach (var profile in legacyProfiles)
            {
                if (profile == null || string.IsNullOrWhiteSpace (profile.Name))
                    throw new InvalidDataException ("An XP3 profile is missing its name.");
                profiles.Add (new LoadedProfile {
                    Id = profile.Name,
                    Title = string.IsNullOrWhiteSpace (profile.Title) ? profile.Name : profile.Title,
                    Crypt = CreateLegacy (profile),
                });
            }
            return profiles;
        }

        static void SetProfiles (IEnumerable<LoadedProfile> profiles)
        {
            foreach (var profile in profiles)
            {
                if (s_profiles.ContainsKey (profile.Id))
                    throw new InvalidDataException ("Duplicate XP3 profile: " + profile.Id);
                s_profiles.Add (profile.Id, profile.Crypt);
                s_titles.Add (profile.Id, profile.Title);
            }
        }

        static ICrypt CreateV2 (Xp3ProfileRecord profile)
        {
            switch (profile.Algorithm)
            {
            case "akabei":
                return new AkabeiCrypt (ReadParameters<Xp3SeedParameters> (profile).Seed);
            case "mado":
                return new MadoCrypt (ReadParameters<Xp3SeedParameters> (profile).Seed);
            case "xor":
                return new XorCrypt (ReadParameters<Xp3ByteKeyParameters> (profile).Key);
            case "stripe":
                return new StripeCrypt (ReadParameters<Xp3ByteKeyParameters> (profile).Key);
            case "smile":
                var smile = ReadParameters<Xp3SmileParameters> (profile);
                return new SmileCrypt (smile.KeyXor, smile.FirstXor, smile.ZeroXor);
            case "smx":
                var smx = ReadParameters<Xp3SmxParameters> (profile);
                if (smx.KeySeq == null || smx.Mask < 0 || smx.KeySeq.Length <= smx.Mask + 1)
                    throw new InvalidDataException ("XP3 SMX profile has invalid key sequence: " + profile.Id);
                return new SmxCrypt (smx.Mask, smx.KeySeq);
            case "altered-pink":
                return new AlteredPinkCrypt();
            case "applique":
                return new AppliqueCrypt();
            case "damegane":
                return new DameganeCrypt();
            case "dieselmine":
                return new DieselmineCrypt();
            case "exa":
                return new ExaCrypt();
            case "fate":
                return new FateCrypt();
            case "festival":
                return new FestivalCrypt();
            case "flying-shine":
                return new FlyingShineCrypt();
            case "haikuo":
                return new HaikuoCrypt();
            case "hash":
                return new HashCrypt();
            case "hibiki":
                return new HibikiCrypt();
            case "high-running":
                return new HighRunningCrypt();
            case "hybrid":
                return new HybridCrypt();
            case "kiss":
                return new KissCrypt();
            case "mizukake":
                return new MizukakeCrypt();
            case "natsupochi":
                return new NatsupochiCrypt();
            case "nephrite":
                return new NephriteCrypt();
            case "no-crypt":
                return new NoCrypt();
            case "okiba":
                return new OkibaCrypt();
            case "pinpoint":
                return new PinPointCrypt();
            case "poring-soft":
                return new PoringSoftCrypt();
            case "seiten":
                return new SeitenCrypt();
            case "sourire":
                return new SourireCrypt();
            case "syangrila-smart":
                return new SyangrilaSmartCrypt();
            case "tokidoki":
                return new TokidokiCrypt();
            case "yuzu":
                return new YuzuCrypt();
            case "cx-encryption":
                return new CxEncryption (CreateCx (ReadParameters<Xp3CxParameters> (profile).Cx, profile.Id));
            case "senren-cx":
                return new SenrenCxCrypt (CreateCx (ReadParameters<Xp3CxParameters> (profile).Cx, profile.Id));
            case "cabbage-cx":
                var cabbage = ReadParameters<Xp3SeededCxParameters> (profile);
                return new CabbageCxCrypt (CreateCx (cabbage.Cx, profile.Id), cabbage.Seed);
            case "nana-cx":
                var nana = ReadParameters<Xp3NanaCxParameters> (profile);
                return new NanaCxCrypt (CreateCx (nana.Cx, profile.Id), nana.Seed) { YuzKey = RequireYuzKey (nana.YuzKey, profile.Id) };
            case "riddle-cx":
                var riddle = ReadParameters<Xp3RiddleCxParameters> (profile);
                return new RiddleCxCrypt (CreateCx (riddle.Cx, profile.Id), riddle.Seed) {
                    YuzKey = RequireYuzKey (riddle.YuzKey, profile.Id),
                    m_key1 = riddle.Key1,
                    m_key2 = riddle.Key2,
                };
            case "hx":
                return CreateHx (ReadParameters<Xp3HxParameters> (profile), profile.Id);
            case "hx-lite":
                return CreateHxLite (ReadParameters<Xp3HxLiteParameters> (profile), profile.Id);
            default:
                throw new InvalidDataException ("Unsupported XP3 v2 profile algorithm: " + profile.Algorithm);
            }
        }

        static T ReadParameters<T> (Xp3ProfileRecord profile)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<T> (profile.Parameters.GetRawText(), s_jsonOptions);
                if (parameters == null)
                    throw new InvalidDataException ("XP3 v2 profile parameters are empty: " + profile.Id);
                return parameters;
            }
            catch (JsonException error)
            {
                throw new InvalidDataException ("XP3 v2 profile parameters are invalid: " + profile.Id, error);
            }
        }

        static CxScheme CreateCx (Xp3CxData data, string id)
        {
            if (data == null || data.PrologOrder == null || data.OddBranchOrder == null || data.EvenBranchOrder == null)
                throw new InvalidDataException ("XP3 profile has incomplete Cx data: " + id);
            if (data.ControlBlock == null && string.IsNullOrWhiteSpace (data.TpmFileName))
                throw new InvalidDataException ("XP3 profile needs a Cx control block or TPM filename: " + id);
            return new CxScheme {
                Mask = data.Mask,
                Offset = data.Offset,
                PrologOrder = data.PrologOrder,
                OddBranchOrder = data.OddBranchOrder,
                EvenBranchOrder = data.EvenBranchOrder,
                ControlBlock = data.ControlBlock,
                TpmFileName = data.TpmFileName,
            };
        }

        static ICrypt CreateHx (Xp3HxParameters profile, string id)
        {
            if (profile == null)
                throw new InvalidDataException ("XP3 Hx profile parameters are missing: " + id);
            ValidateKeyPair (profile.IndexKey1, profile.IndexKey2, "default index key", id, false);

            var indexKeys = new Dictionary<string, HxIndexKey> (StringComparer.OrdinalIgnoreCase);
            if (profile.IndexKeys != null)
            {
                foreach (var item in profile.IndexKeys)
                {
                    if (string.IsNullOrWhiteSpace (item.Key) || item.Value == null)
                        throw new InvalidDataException ("XP3 Hx profile has an invalid archive index key: " + id);
                    ValidateKeyPair (item.Value.Key1, item.Value.Key2, "archive index key", id, true);
                    if (!indexKeys.TryAdd (item.Key, new HxIndexKey { Key1 = item.Value.Key1, Key2 = item.Value.Key2 }))
                        throw new InvalidDataException ("XP3 Hx profile has duplicate archive index key: " + item.Key);
                }
            }
            if (profile.IndexKey1 == null && indexKeys.Count == 0)
                throw new InvalidDataException ("XP3 Hx profile needs at least one index key: " + id);

            return new HxCrypt (CreateCx (profile.Cx, id)) {
                FilterKey = profile.FilterKey,
                RandomType = profile.RandomType,
                NamesFile = profile.NamesFile,
                IndexKey1 = profile.IndexKey1,
                IndexKey2 = profile.IndexKey2,
                IndexKeyDict = indexKeys.Count == 0 ? null : indexKeys,
            };
        }

        static ICrypt CreateHxLite (Xp3HxLiteParameters profile, string id)
        {
            if (profile == null)
                throw new InvalidDataException ("XP3 HxLite profile parameters are missing: " + id);
            if (profile.HeaderKey != null && profile.HeaderKey.Length < 8)
                throw new InvalidDataException ("XP3 HxLite profile header key must be at least eight bytes: " + id);

            var crypt = new HxCryptLite (CreateCx (profile.Cx, id));
            crypt.SetHeaderDecryptParam (profile.HeaderKey, profile.HeaderSplitPosition);
            crypt.SetSingleByteCryptFlag (profile.FileCryptFlag);
            crypt.SetRandomType (profile.RandomType);
            return crypt;
        }

#pragma warning disable CS0618
        static ICrypt CreateLegacy (Xp3SchemeProfile profile)
        {
            ValidateLegacyCx (profile.Cx, profile.Name);
            switch (profile.Algorithm)
            {
            case "CxEncryption":
                return new CxEncryption (profile.Cx);
            case "SenrenCxCrypt":
                return new SenrenCxCrypt (profile.Cx);
            case "CabbageCxCrypt":
                return new CabbageCxCrypt (profile.Cx, profile.Seed);
            case "NanaCxCrypt":
                return new NanaCxCrypt (profile.Cx, profile.Seed) { YuzKey = RequireYuzKey (profile.YuzKey, profile.Name) };
            case "RiddleCxCrypt":
                return new RiddleCxCrypt (profile.Cx, profile.Seed) {
                    YuzKey = RequireYuzKey (profile.YuzKey, profile.Name),
                    m_key1 = profile.RiddleKey1,
                    m_key2 = profile.RiddleKey2,
                };
            case "HxCrypt":
                ValidateKeyPair (profile.IndexKey1, profile.IndexKey2, "default index key", profile.Name, false);
                return new HxCrypt (profile.Cx) {
                    FilterKey = profile.FilterKey,
                    RandomType = profile.RandomType,
                    NamesFile = profile.NamesFile,
                    IndexKey1 = profile.IndexKey1,
                    IndexKey2 = profile.IndexKey2,
                    IndexKeyDict = profile.IndexKeyDict,
                };
            case "HxCryptLite":
                if (profile.HeaderKey != null && profile.HeaderKey.Length < 8)
                    throw new InvalidDataException ("XP3 HxLite profile header key must be at least eight bytes: " + profile.Name);
                var lite = new HxCryptLite (profile.Cx);
                lite.SetHeaderDecryptParam (profile.HeaderKey, profile.HeaderSplitPosition);
                lite.SetSingleByteCryptFlag (profile.FileCryptFlag);
                lite.SetRandomType (profile.RandomType);
                return lite;
            default:
                throw new InvalidDataException ("Unsupported XP3 profile algorithm: " + profile.Algorithm);
            }
        }
#pragma warning restore CS0618

        static void ValidateLegacyCx (CxScheme scheme, string id)
        {
            if (scheme == null || scheme.PrologOrder == null || scheme.OddBranchOrder == null || scheme.EvenBranchOrder == null)
                throw new InvalidDataException ("XP3 profile has incomplete Cx data: " + id);
            if (scheme.ControlBlock == null && string.IsNullOrWhiteSpace (scheme.TpmFileName))
                throw new InvalidDataException ("XP3 profile needs a Cx control block or TPM filename: " + id);
        }

        static uint[] RequireYuzKey (uint[] key, string id)
        {
            if (key == null || key.Length < 6)
                throw new InvalidDataException ("XP3 profile requires six YuzKey values: " + id);
            return key;
        }

        static void ValidateKeyPair (byte[] key1, byte[] key2, string label, string id, bool required)
        {
            if (key1 == null && key2 == null && !required)
                return;
            if (key1 == null || key1.Length != 32 || key2 == null || key2.Length != 16)
                throw new InvalidDataException ("XP3 profile has an invalid " + label + ": " + id);
        }

        sealed class LoadedProfile
        {
            public string Id;
            public string Title;
            public ICrypt Crypt;
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
