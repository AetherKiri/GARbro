using System;
using System.Collections.Generic;
using System.Formats.Nrbf;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GARbro.LegacyDataMigration
{
    internal sealed class Xp3ExportDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<Xp3ExportProfile> Profiles { get; set; }
    }

    internal sealed class Xp3ExportProfile
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Algorithm { get; set; }
        public object Parameters { get; set; }
    }

    internal sealed class Xp3ExportReport
    {
        public int SourceDatabaseVersion { get; set; }
        public int SourceKnownSchemeCount { get; set; }
        public int ExportedProfileCount { get; set; }
        public int DuplicateProfileCount { get; set; }
        public List<Xp3SkippedProfile> SkippedProfiles { get; set; }
    }

    internal sealed class Xp3GameMapDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> GameMap { get; set; }
    }

    internal sealed class LegacySchemeInventoryDocument
    {
        public int SourceDatabaseVersion { get; set; }
        public List<LegacySchemeInventoryEntry> Schemes { get; set; }
        public List<LegacySchemeTypeSummary> Types { get; set; }
    }

    internal sealed class LegacySchemeInventoryEntry
    {
        public string Tag { get; set; }
        public string LegacyType { get; set; }
        public List<string> Members { get; set; }
    }

    internal sealed class LegacySchemeTypeSummary
    {
        public string LegacyType { get; set; }
        public int Count { get; set; }
    }

    internal sealed class Xp3ZipKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class TcdKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, int> KnownKeys { get; set; }
    }

    internal sealed class MorningKeyDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public byte[] DefaultKey { get; set; }
    }

    internal sealed class FpkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public uint[] KnownKeys { get; set; }
    }

    internal sealed class CmpKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class PkgKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, uint[]> KnownKeys { get; set; }
    }

    internal sealed class RepiKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, uint[]> KnownSchemes { get; set; }
    }

    internal sealed class CsafKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class MblKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class NpkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class PckKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class NsaKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class FjsysKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> MsdPasswords { get; set; }
    }

    internal sealed class IntKeyRecord
    {
        public uint Key { get; set; }
        public string Passphrase { get; set; }
    }

    internal sealed class IntKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, IntKeyRecord> KnownKeys { get; set; }
    }

    internal sealed class NoaKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, Dictionary<string, string>> KnownKeys { get; set; }
    }

    internal sealed class GalKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class CrzKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class ActgsKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public byte[][] KnownKeys { get; set; }
    }

    internal sealed class AdsKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class ArcgKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<uint, string> KnownKeys { get; set; }
    }

    internal sealed class MgpkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownKeys { get; set; }
    }

    internal sealed class RctKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownKeys { get; set; }
    }

    internal sealed class McgKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte> KnownKeys { get; set; }
    }

    internal sealed class TinkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<uint, byte[]> KnownKeys { get; set; }
    }

    internal sealed class BinIdxKeyRecord
    {
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }
    }

    internal sealed class BinIdxKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, BinIdxKeyRecord> KnownKeys { get; set; }
    }

    internal sealed class AsbKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, uint> KnownKeys { get; set; }
    }

    internal sealed class SjDatKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    internal sealed class AzEncryptedKeyRecord
    {
        public uint IndexKey { get; set; }
        public uint? ContentKey { get; set; }
    }

    internal sealed class AzEncryptedKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, AzEncryptedKeyRecord> KnownSchemes { get; set; }
    }

    internal sealed class PkzKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    internal sealed class PbzKeyRecord
    {
        public byte[] ArcKey { get; set; }
        public byte[] ScriptKey { get; set; }
    }

    internal sealed class PbzKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, PbzKeyRecord> KnownSchemes { get; set; }
    }

    internal sealed class KcapKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, string> KnownSchemes { get; set; }
    }

    internal sealed class Ai5KeyRecord
    {
        public int NameLength { get; set; }
        public byte NameKey { get; set; }
        public uint SizeKey { get; set; }
        public uint OffsetKey { get; set; }
    }

    internal sealed class Ai5KeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, Ai5KeyRecord> KnownSchemes { get; set; }
    }

    internal sealed class NpaKeyRecord
    {
        public int TitleId { get; set; }
        public uint NameKey { get; set; }
        public byte[] Order { get; set; }
    }

    internal sealed class NpaKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, NpaKeyRecord> KnownSchemes { get; set; }
    }

    internal sealed class PsbKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public uint[] KnownKeys { get; set; }
    }

    internal sealed class AmDecryptTableDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public byte[] DecryptTable { get; set; }
    }

    internal sealed class LpkKeyRecord
    {
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
    }

    internal sealed class LpkSchemeRecord
    {
        public LpkKeyRecord BaseKey { get; set; }
        public byte ContentXor { get; set; }
        public uint RotatePattern { get; set; }
        public bool ImportGameInit { get; set; }
    }

    internal sealed class LpkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, LpkSchemeRecord> KnownSchemes { get; set; }
        public Dictionary<string, Dictionary<string, LpkKeyRecord>> KnownKeys { get; set; }
    }

    internal sealed class GyuKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, Dictionary<int, uint>> NumericKeys { get; set; }
        public Dictionary<string, Dictionary<string, uint>> StringKeys { get; set; }
    }

    internal sealed class YpfSchemeRecord
    {
        public byte[] SwapTable { get; set; }
        public byte Key { get; set; }
        public bool GuessKey { get; set; }
        public uint ExtraHeaderSize { get; set; }
        public uint ScriptKey { get; set; }
        public int CompressType { get; set; }
    }

    internal sealed class YpfKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, YpfSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class TacticsSchemeRecord
    {
        public string Password { get; set; }
        public bool CustomLzss { get; set; }
    }

    internal sealed class TacticsKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, TacticsSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class RpmSchemeRecord
    {
        public string Keyword { get; set; }
        public int NameLength { get; set; }
    }

    internal sealed class RpmKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, RpmSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class DataKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, int> KnownSchemes { get; set; }
    }

    internal sealed class AvcSchemeRecord
    {
        public string Password { get; set; }
        public int KeyOffset { get; set; }
        public int HeaderOffset { get; set; }
    }

    internal sealed class AvcKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<AvcSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class DpkSchemeRecord
    {
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
        public string Name { get; set; }
        public string OriginalTitle { get; set; }
    }

    internal sealed class DpkKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<DpkSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class AgsiKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, Dictionary<string, byte[]>> KnownSchemes { get; set; }
    }

    internal sealed class LeafKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownSchemes { get; set; }
    }

    internal sealed class IkuraKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, byte[]> KnownSecrets { get; set; }
    }

    internal sealed class Fsb5HeaderRecord
    {
        public byte[] VorbisData { get; set; }
        public int PatchOffset { get; set; }
        public byte[] PatchData { get; set; }
    }

    internal sealed class Fsb5KeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<uint, Fsb5HeaderRecord> VorbisHeaders { get; set; }
    }

    internal sealed class CpzSchemeRecord
    {
        public int Version { get; set; }
        public uint[] Cpz5Secret { get; set; }
        public int Md5Variant { get; set; }
        public uint DecoderFactor { get; set; }
        public uint EntryInitKey { get; set; }
        public uint EntrySubKey { get; set; }
        public byte EntryTailKey { get; set; }
        public byte EntryKeyPos { get; set; }
        public uint IndexSeed { get; set; }
        public uint IndexAddend { get; set; }
        public uint IndexSubtrahend { get; set; }
        public uint[] DirKeyAddend { get; set; }
    }

    internal sealed class CpzKeysDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, CpzSchemeRecord> KnownSchemes { get; set; }
    }

    internal sealed class Xp3SkippedProfile
    {
        public string Title { get; set; }
        public string LegacyType { get; set; }
        public string Reason { get; set; }
    }

    internal sealed class Xp3ExportCxData
    {
        public uint Mask { get; set; }
        public uint Offset { get; set; }
        public byte[] PrologOrder { get; set; }
        public byte[] OddBranchOrder { get; set; }
        public byte[] EvenBranchOrder { get; set; }
        public uint[] ControlBlock { get; set; }
        public string TpmFileName { get; set; }
    }

    internal sealed class Xp3ExportCxParameters { public Xp3ExportCxData Cx { get; set; } }

    internal sealed class Xp3ExportSeededCxParameters
    {
        public Xp3ExportCxData Cx { get; set; }
        public uint Seed { get; set; }
    }

    internal sealed class Xp3ExportNanaCxParameters
    {
        public Xp3ExportCxData Cx { get; set; }
        public uint Seed { get; set; }
        public uint[] YuzKey { get; set; }
    }

    internal sealed class Xp3ExportRiddleCxParameters
    {
        public Xp3ExportCxData Cx { get; set; }
        public uint Seed { get; set; }
        public uint[] YuzKey { get; set; }
        public uint Key1 { get; set; }
        public uint Key2 { get; set; }
    }

    internal sealed class Xp3ExportHxParameters
    {
        public Xp3ExportCxData Cx { get; set; }
        public ulong FilterKey { get; set; }
        public int RandomType { get; set; }
        public string NamesFile { get; set; }
        public byte[] IndexKey1 { get; set; }
        public byte[] IndexKey2 { get; set; }
        public Dictionary<string, Xp3ExportHxIndexKey> IndexKeys { get; set; }
    }

    internal sealed class Xp3ExportHxIndexKey
    {
        public byte[] Key1 { get; set; }
        public byte[] Key2 { get; set; }
    }

    internal sealed class Xp3ExportHxLiteParameters
    {
        public Xp3ExportCxData Cx { get; set; }
        public byte[] HeaderKey { get; set; }
        public long HeaderSplitPosition { get; set; }
        public bool FileCryptFlag { get; set; }
        public int RandomType { get; set; }
    }

    internal sealed class Xp3ExportSeedParameters
    {
        public uint Seed { get; set; }
    }

    internal sealed class Xp3ExportByteKeyParameters
    {
        public byte Key { get; set; }
    }

    internal sealed class Xp3ExportSmileParameters
    {
        public uint KeyXor { get; set; }
        public byte FirstXor { get; set; }
        public byte ZeroXor { get; set; }
    }

    internal sealed class Xp3ExportSmxParameters
    {
        public int Mask { get; set; }
        public byte[] KeySeq { get; set; }
    }

    internal static class LegacyXp3Exporter
    {
        const string KnownSchemePairType = "[GameRes.Formats.KiriKiri.ICrypt,";

        static readonly Dictionary<string, string> SimpleAlgorithms = new Dictionary<string, string> (StringComparer.Ordinal) {
            { "GameRes.Formats.KiriKiri.AlteredPinkCrypt", "altered-pink" },
            { "GameRes.Formats.KiriKiri.AppliqueCrypt", "applique" },
            { "GameRes.Formats.KiriKiri.DameganeCrypt", "damegane" },
            { "GameRes.Formats.KiriKiri.DieselmineCrypt", "dieselmine" },
            { "GameRes.Formats.KiriKiri.ExaCrypt", "exa" },
            { "GameRes.Formats.KiriKiri.FateCrypt", "fate" },
            { "GameRes.Formats.KiriKiri.FestivalCrypt", "festival" },
            { "GameRes.Formats.KiriKiri.FlyingShineCrypt", "flying-shine" },
            { "GameRes.Formats.KiriKiri.HaikuoCrypt", "haikuo" },
            { "GameRes.Formats.KiriKiri.HashCrypt", "hash" },
            { "GameRes.Formats.KiriKiri.HibikiCrypt", "hibiki" },
            { "GameRes.Formats.KiriKiri.HighRunningCrypt", "high-running" },
            { "GameRes.Formats.KiriKiri.HybridCrypt", "hybrid" },
            { "GameRes.Formats.KiriKiri.KissCrypt", "kiss" },
            { "GameRes.Formats.KiriKiri.MizukakeCrypt", "mizukake" },
            { "GameRes.Formats.KiriKiri.NatsupochiCrypt", "natsupochi" },
            { "GameRes.Formats.KiriKiri.NephriteCrypt", "nephrite" },
            { "GameRes.Formats.KiriKiri.NoCrypt", "no-crypt" },
            { "GameRes.Formats.KiriKiri.OkibaCrypt", "okiba" },
            { "GameRes.Formats.KiriKiri.PinPointCrypt", "pinpoint" },
            { "GameRes.Formats.KiriKiri.PoringSoftCrypt", "poring-soft" },
            { "GameRes.Formats.KiriKiri.SeitenCrypt", "seiten" },
            { "GameRes.Formats.KiriKiri.SourireCrypt", "sourire" },
            { "GameRes.Formats.KiriKiri.SyangrilaSmartCrypt", "syangrila-smart" },
            { "GameRes.Formats.KiriKiri.TokidokiCrypt", "tokidoki" },
            { "GameRes.Formats.KiriKiri.YuzuCrypt", "yuzu" },
        };

        internal static Xp3ExportDocument Export (LegacyFormatsDatabase database, out Xp3ExportReport report)
        {
            var profiles = new Dictionary<string, Xp3ExportProfile> (StringComparer.OrdinalIgnoreCase);
            var skipped = new List<Xp3SkippedProfile>();
            int duplicateCount = 0;
            var pairs = database.Records.Values.OfType<ClassRecord>()
                .Where (record => record.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal)
                    && record.TypeName.FullName.IndexOf (KnownSchemePairType, StringComparison.Ordinal) >= 0)
                .Select (record => new {
                    Title = record.GetRawValue ("key") as string,
                    Crypt = record.GetRawValue ("value") as ClassRecord,
                })
                .OrderBy (item => item.Title, StringComparer.Ordinal)
                .ToArray();

            foreach (var pair in pairs)
            {
                if (string.IsNullOrWhiteSpace (pair.Title) || pair.Crypt == null)
                    throw new InvalidDataException ("Legacy XP3 known-scheme record is incomplete.");
                string reason;
                var profile = TryExportProfile (pair.Title, pair.Crypt, out reason);
                if (profile == null)
                {
                    skipped.Add (new Xp3SkippedProfile {
                        Title = pair.Title,
                        LegacyType = pair.Crypt.TypeName.FullName,
                        Reason = reason,
                    });
                }
                else if (profiles.TryGetValue (profile.Id, out var existing))
                {
                    if (!string.Equals (existing.Algorithm, profile.Algorithm, StringComparison.Ordinal)
                        || existing.Parameters.GetType() != profile.Parameters.GetType())
                        throw new InvalidDataException ("Legacy XP3 profiles collide case-insensitively with different algorithms: " + pair.Title);
                    ++duplicateCount;
                }
                else
                    profiles.Add (profile.Id, profile);
            }

            report = new Xp3ExportReport {
                SourceDatabaseVersion = database.Version,
                SourceKnownSchemeCount = pairs.Length,
                ExportedProfileCount = profiles.Count,
                DuplicateProfileCount = duplicateCount,
                SkippedProfiles = skipped,
            };
            return new Xp3ExportDocument {
                Profiles = profiles.Values.OrderBy (profile => profile.Id, StringComparer.Ordinal).ToList(),
            };
        }

        internal static Dictionary<string, string> ExportGameMap (LegacyFormatsDatabase database)
        {
            if (database == null || database.Root == null || !database.Root.HasMember ("GameMap"))
                throw new InvalidDataException ("Legacy database has no game map.");
            var dictionary = database.Root.GetRawValue ("GameMap") as ClassRecord;
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy game map is not a dictionary.");
            if (!dictionary.HasMember ("KeyValuePairs"))
                return new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException ("Legacy game map has invalid entries.");

            var result = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy game map contains an invalid entry.");
                var executable = ReadRaw (entry, "key") as string;
                var title = ReadRaw (entry, "value") as string;
                if (string.IsNullOrWhiteSpace (executable) || string.IsNullOrWhiteSpace (title))
                    throw new InvalidDataException ("Legacy game map contains an incomplete entry.");
                if (!result.TryAdd (executable, title))
                    throw new InvalidDataException ("Legacy game map contains a duplicate executable: " + executable);
            }
            return result;
        }

        internal static LegacySchemeInventoryDocument ExportSchemeInventory (LegacyFormatsDatabase database)
        {
            if (database == null || database.Root == null || !database.Root.HasMember ("SchemeMap"))
                throw new InvalidDataException ("Legacy database has no scheme map.");
            var dictionary = database.Root.GetRawValue ("SchemeMap") as ClassRecord;
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy scheme map is not a dictionary.");
            var entries = new List<LegacySchemeInventoryEntry>();
            if (dictionary.HasMember ("KeyValuePairs"))
            {
                var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
                if (rawPairs != null)
                {
                    var pairs = rawPairs as ArrayRecord;
                    if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                        throw new InvalidDataException ("Legacy scheme map has invalid entries.");
                    foreach (var record in GetRecordArray (pairs))
                    {
                        var entry = record as ClassRecord;
                        if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                            throw new InvalidDataException ("Legacy scheme map contains an invalid entry.");
                        var tag = ReadRaw (entry, "key") as string;
                        var scheme = ReadRaw (entry, "value") as ClassRecord;
                        if (string.IsNullOrWhiteSpace (tag) || scheme == null)
                            throw new InvalidDataException ("Legacy scheme map contains an incomplete entry.");
                        entries.Add (new LegacySchemeInventoryEntry {
                            Tag = tag,
                            LegacyType = scheme.TypeName.FullName,
                            Members = scheme.MemberNames.OrderBy (name => name, StringComparer.Ordinal).ToList(),
                        });
                    }
                }
            }
            entries = entries.OrderBy (entry => entry.Tag, StringComparer.OrdinalIgnoreCase).ToList();
            var duplicate = entries.GroupBy (entry => entry.Tag, StringComparer.OrdinalIgnoreCase).FirstOrDefault (group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidDataException ("Legacy scheme map contains a duplicate format tag: " + duplicate.Key);
            var types = entries.GroupBy (entry => entry.LegacyType, StringComparer.Ordinal)
                .Select (group => new LegacySchemeTypeSummary { LegacyType = group.Key, Count = group.Count() })
                .OrderBy (entry => entry.LegacyType, StringComparer.Ordinal)
                .ToList();
            return new LegacySchemeInventoryDocument {
                SourceDatabaseVersion = database.Version,
                Schemes = entries,
                Types = types,
            };
        }

        internal static Dictionary<string, string> ExportZipKeys (LegacyFormatsDatabase database)
        {
            var zipScheme = FindScheme (database, "ZIP");
            if (zipScheme == null || !zipScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy ZIP scheme has no KnownKeys member.");
            var dictionary = ReadRaw (zipScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy ZIP key map");
        }

        internal static Dictionary<string, int> ExportTcdKeys (LegacyFormatsDatabase database)
        {
            var tcdScheme = FindScheme (database, "TCD");
            if (tcdScheme == null || !tcdScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy TCD scheme has no KnownKeys member.");
            var dictionary = ReadRaw (tcdScheme, "KnownKeys") as ClassRecord;
            return ReadIntDictionary (dictionary, "Legacy TCD key map");
        }

        internal static byte[] ExportMorningKey (LegacyFormatsDatabase database)
        {
            var morningScheme = FindScheme (database, "PAK/MORNING");
            if (morningScheme == null || !morningScheme.HasMember ("DefaultKey"))
                throw new InvalidDataException ("Legacy Morning scheme has no DefaultKey member.");
            return ReadRequiredArray<byte> (morningScheme, "DefaultKey");
        }

        internal static uint[] ExportFpkKeys (LegacyFormatsDatabase database)
        {
            var fpkScheme = FindScheme (database, "FPK/MOONHIR");
            if (fpkScheme == null || !fpkScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy FPK scheme has no KnownKeys member.");
            return ReadRequiredArray<uint> (fpkScheme, "KnownKeys");
        }

        internal static Dictionary<string, byte[]> ExportCmpKeys (LegacyFormatsDatabase database)
        {
            var cmpScheme = FindScheme (database, "CMP");
            if (cmpScheme == null || !cmpScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy CMP scheme has no KnownKeys member.");
            var dictionary = ReadRaw (cmpScheme, "KnownKeys") as ClassRecord;
            return ReadByteDictionary (dictionary, "Legacy CMP key map");
        }

        internal static Dictionary<string, uint[]> ExportPkgKeys (LegacyFormatsDatabase database)
        {
            var pkgScheme = FindScheme (database, "PKG/2");
            if (pkgScheme == null || !pkgScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy PKG/2 scheme has no KnownKeys member.");
            var dictionary = ReadRaw (pkgScheme, "KnownKeys") as ClassRecord;
            return ReadUIntArrayDictionary (dictionary, "Legacy PKG/2 key map");
        }

        internal static Dictionary<string, uint[]> ExportRepiKeys (LegacyFormatsDatabase database)
        {
            var repiScheme = FindScheme (database, "DAT/RepiPack");
            if (repiScheme == null || !repiScheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy DAT/RepiPack scheme has no KnownSchemes member.");
            var dictionary = ReadRaw (repiScheme, "KnownSchemes") as ClassRecord;
            return ReadUIntArrayDictionary (dictionary, "Legacy DAT/RepiPack scheme map");
        }

        internal static Dictionary<string, string> ExportCsafKeys (LegacyFormatsDatabase database)
        {
            var csafScheme = FindScheme (database, "CSAF");
            if (csafScheme == null || !csafScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy CSAF scheme has no KnownKeys member.");
            var dictionary = ReadRaw (csafScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy CSAF key map");
        }

        internal static Dictionary<string, string> ExportMblKeys (LegacyFormatsDatabase database)
        {
            var mblScheme = FindScheme (database, "MBL");
            if (mblScheme == null || !mblScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy MBL scheme has no KnownKeys member.");
            var dictionary = ReadRaw (mblScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy MBL key map");
        }

        internal static Dictionary<string, byte[]> ExportNpkKeys (LegacyFormatsDatabase database)
        {
            var npkScheme = FindScheme (database, "NPK");
            if (npkScheme == null || !npkScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy NPK scheme has no KnownKeys member.");
            var dictionary = ReadRaw (npkScheme, "KnownKeys") as ClassRecord;
            return ReadByteDictionary (dictionary, "Legacy NPK key map");
        }

        internal static Dictionary<string, byte[]> ExportPckKeys (LegacyFormatsDatabase database)
        {
            var pckScheme = FindScheme (database, "PCK/TAMAMO");
            if (pckScheme == null || !pckScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy PCK/TAMAMO scheme has no KnownKeys member.");
            var dictionary = ReadRaw (pckScheme, "KnownKeys") as ClassRecord;
            return ReadByteDictionary (dictionary, "Legacy PCK/TAMAMO key map");
        }

        internal static Dictionary<string, string> ExportNsaKeys (LegacyFormatsDatabase database, string tag = "NSA")
        {
            var nsaScheme = FindScheme (database, tag);
            if (nsaScheme == null || !nsaScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy " + tag + " scheme has no KnownKeys member.");
            var dictionary = ReadRaw (nsaScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy " + tag + " key map");
        }

        internal static Dictionary<string, string> ExportFjsysKeys (LegacyFormatsDatabase database)
        {
            var fjsysScheme = FindScheme (database, "FJSYS");
            if (fjsysScheme == null || !fjsysScheme.HasMember ("MsdPasswords"))
                throw new InvalidDataException ("Legacy FJSYS scheme has no MsdPasswords member.");
            var dictionary = ReadRaw (fjsysScheme, "MsdPasswords") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy FJSYS password map");
        }

        internal static Dictionary<string, IntKeyRecord> ExportIntKeys (LegacyFormatsDatabase database)
        {
            var intScheme = FindScheme (database, "INT");
            if (intScheme == null || !intScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy INT scheme has no KnownKeys member.");
            var dictionary = ReadRaw (intScheme, "KnownKeys") as ClassRecord;
            return ReadIntKeyDataDictionary (dictionary, "Legacy INT key map");
        }

        internal static Dictionary<string, Dictionary<string, string>> ExportNoaKeys (LegacyFormatsDatabase database)
        {
            var noaScheme = FindScheme (database, "NOA");
            if (noaScheme == null || !noaScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy NOA scheme has no KnownKeys member.");
            var dictionary = ReadRaw (noaScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionaryMap (dictionary, "Legacy NOA key map");
        }

        internal static Dictionary<string, string> ExportGalKeys (LegacyFormatsDatabase database)
        {
            var galScheme = FindScheme (database, "GAL");
            if (galScheme == null || !galScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy GAL scheme has no KnownKeys member.");
            var dictionary = ReadRaw (galScheme, "KnownKeys") as ClassRecord;
            return ReadStringDictionary (dictionary, "Legacy GAL key map");
        }

        internal static Dictionary<string, byte[]> ExportCrzKeys (LegacyFormatsDatabase database)
        {
            var crzScheme = FindScheme (database, "CRZ");
            if (crzScheme == null || !crzScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy CRZ scheme has no KnownKeys member.");
            var dictionary = ReadRaw (crzScheme, "KnownKeys") as ClassRecord;
            return ReadByteDictionary (dictionary, "Legacy CRZ key map");
        }

        internal static byte[][] ExportActgsKeys (LegacyFormatsDatabase database)
        {
            var actgsScheme = FindScheme (database, "DAT/ACTGS");
            if (actgsScheme == null || !actgsScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy ACTGS scheme has no KnownKeys member.");
            var raw = ReadRaw (actgsScheme, "KnownKeys") as ArrayRecord;
            if (raw == null || raw.Rank != 1 || raw.Lengths[0] > 4096)
                throw new InvalidDataException ("Legacy ACTGS key list is invalid.");
            byte[][] values;
            try
            {
                var array = raw.GetArray (typeof (byte[][]), false);
                var records = array as ArrayRecord[];
                if (records == null)
                    throw new InvalidDataException ("returned " + (array == null ? "null" : array.GetType ().FullName));
                values = new byte[records.Length][];
                for (var i = 0; i < records.Length; ++i)
                {
                    var key = records[i] as SZArrayRecord<byte>;
                    if (key == null)
                        throw new InvalidDataException ("element " + i + " is " + records[i]?.GetType ().FullName);
                    values[i] = key.GetArray (false);
                }
            }
            catch (Exception error)
            {
                throw new InvalidDataException ("Legacy ACTGS key list cannot be read: " + error.Message, error);
            }
            if (values == null)
                throw new InvalidDataException ("Legacy ACTGS key list has an invalid element type.");
            foreach (var key in values)
            {
                if (key == null || key.Length == 0)
                    throw new InvalidDataException ("Legacy ACTGS key list contains an empty key.");
            }
            return values;
        }

        internal static Dictionary<string, byte[]> ExportAdsKeys (LegacyFormatsDatabase database)
        {
            var adsScheme = FindScheme (database, "ADS");
            if (adsScheme == null || !adsScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy ADS scheme has no KnownKeys member.");
            var dictionary = ReadRaw (adsScheme, "KnownKeys") as ClassRecord;
            return ReadByteDictionary (dictionary, "Legacy ADS key map");
        }

        internal static Dictionary<uint, string> ExportArcgKeys (LegacyFormatsDatabase database)
        {
            var arcgScheme = FindScheme (database, "ARCG");
            if (arcgScheme == null || !arcgScheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy ARCG scheme has no KnownKeys member.");
            var dictionary = ReadRaw (arcgScheme, "KnownKeys") as ClassRecord;
            return ReadUIntStringDictionary (dictionary, "Legacy ARCG key map");
        }

        internal static Dictionary<string, byte[]> ExportMgpkKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "MGPK");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy MGPK scheme has no KnownKeys member.");
            return ReadByteDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy MGPK key map");
        }

        internal static Dictionary<string, string> ExportRctKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "RCT");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy RCT scheme has no KnownKeys member.");
            return ReadStringDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy RCT key map");
        }

        internal static Dictionary<string, byte> ExportMcgKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "MCG");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy MCG scheme has no KnownKeys member.");
            return ReadStringByteDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy MCG key map");
        }

        internal static Dictionary<uint, byte[]> ExportTinkKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "OGG/TINK");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy OGG/TINK scheme has no KnownKeys member.");
            return ReadUIntByteDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy OGG/TINK key map");
        }

        internal static Dictionary<string, BinIdxKeyRecord> ExportBinIdxKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "BIN/IDX");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy BIN/IDX scheme has no KnownKeys member.");
            return ReadBinIdxDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy BIN/IDX key map");
        }

        internal static Dictionary<string, uint> ExportAsbKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "ARC/AZ");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy ARC/AZ scheme has no KnownKeys member.");
            return ReadStringUIntDictionary (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy ARC/AZ key map");
        }

        internal static Dictionary<string, byte[]> ExportSjDatKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "DAT/SPEED");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy DAT/SPEED scheme has no KnownSchemes member.");
            return ReadByteDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy DAT/SPEED key map");
        }

        internal static Dictionary<string, AzEncryptedKeyRecord> ExportAzEncryptedKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "ARC/AZ/encrypted");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy ARC/AZ/encrypted scheme has no KnownSchemes member.");
            return ReadAzEncryptedDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy ARC/AZ/encrypted key map");
        }

        internal static Dictionary<string, byte[]> ExportPkzKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "PKZ");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy PKZ scheme has no KnownSchemes member.");
            return ReadByteDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy PKZ key map");
        }

        internal static Dictionary<string, PbzKeyRecord> ExportPbzKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "PBZ");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy PBZ scheme has no KnownSchemes member.");
            return ReadPbzDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy PBZ key map");
        }

        internal static Dictionary<string, string> ExportKcapKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "KCAP");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy KCAP scheme has no KnownSchemes member.");
            return ReadStringDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy KCAP key map");
        }

        internal static Dictionary<string, Ai5KeyRecord> ExportAi5Keys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "ARC/AI5WIN");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy ARC/AI5WIN scheme has no KnownSchemes member.");
            return ReadAi5Dictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy ARC/AI5WIN key map");
        }

        internal static Dictionary<string, NpaKeyRecord> ExportNpaKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "NPA");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy NPA scheme has no KnownSchemes member.");
            return ReadNpaDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord, "Legacy NPA key map");
        }

        internal static uint[] ExportPsbKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "PSB/EMOTE");
            if (scheme == null || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy PSB/EMOTE scheme has no KnownKeys member.");
            var keys = ReadRaw (scheme, "KnownKeys") as SZArrayRecord<uint>;
            if (keys == null || keys.Length == 0 || keys.Length > 256)
                throw new InvalidDataException ("Legacy PSB/EMOTE key list is empty or invalid.");
            var result = keys.GetArray (false);
            if (result.Distinct ().Count () != result.Length)
                throw new InvalidDataException ("Legacy PSB/EMOTE key list contains duplicates.");
            return result;
        }

        internal static byte[] ExportAmDecryptTable (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "AM/Leaf");
            if (scheme == null || !scheme.HasMember ("DecryptTable"))
                throw new InvalidDataException ("Legacy AM/Leaf scheme has no DecryptTable member.");
            var table = ReadRaw (scheme, "DecryptTable") as SZArrayRecord<byte>;
            if (table == null || table.Length == 0 || table.Length > 0x10000)
                throw new InvalidDataException ("Legacy AM/Leaf decrypt table is empty or invalid.");
            return table.GetArray (false);
        }

        internal static LpkKeysDocument ExportLpkKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "LPK");
            if (scheme == null || !scheme.HasMember ("KnownSchemes") || !scheme.HasMember ("KnownKeys"))
                throw new InvalidDataException ("Legacy LPK scheme is incomplete.");
            var knownSchemes = ReadLpkSchemeDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy LPK scheme map");
            var knownKeys = ReadLpkKeyMap (ReadRaw (scheme, "KnownKeys") as ClassRecord, "Legacy LPK file-key map");
            if (knownSchemes.Count == 0)
                throw new InvalidDataException ("Legacy LPK scheme map is empty.");
            return new LpkKeysDocument {
                KnownSchemes = knownSchemes,
                KnownKeys = knownKeys,
            };
        }

        internal static GyuKeysDocument ExportGyuKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "GYU");
            if (scheme == null || !scheme.HasMember ("NumericKeys") || !scheme.HasMember ("StringKeys"))
                throw new InvalidDataException ("Legacy GYU scheme is incomplete.");
            var numeric = ReadIntUIntDictionaryMap (ReadRaw (scheme, "NumericKeys") as ClassRecord,
                "Legacy GYU numeric key map");
            var strings = ReadStringUIntDictionaryMap (ReadRaw (scheme, "StringKeys") as ClassRecord,
                "Legacy GYU string key map");
            if (numeric.Count == 0 && strings.Count == 0)
                throw new InvalidDataException ("Legacy GYU key maps are empty.");
            return new GyuKeysDocument {
                NumericKeys = numeric,
                StringKeys = strings,
            };
        }

        internal static Dictionary<string, YpfSchemeRecord> ExportYpfKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "YPF");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy YPF scheme has no KnownSchemes member.");
            var result = ReadYpfDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy YPF scheme map");
            if (result.Count == 0)
                throw new InvalidDataException ("Legacy YPF scheme map is empty.");
            return result;
        }

        internal static Dictionary<string, TacticsSchemeRecord> ExportTacticsKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "ARC/Tactics/2");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy Tactics scheme has no KnownSchemes member.");
            var result = ReadTacticsDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy Tactics scheme map");
            if (result.Count == 0)
                throw new InvalidDataException ("Legacy Tactics scheme map is empty.");
            return result;
        }

        internal static Dictionary<string, RpmSchemeRecord> ExportRpmKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "ARC/RPM");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy ARC/RPM scheme has no KnownSchemes member.");
            var result = ReadRpmDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy ARC/RPM scheme map");
            if (result.Count == 0)
                throw new InvalidDataException ("Legacy ARC/RPM scheme map is empty.");
            return result;
        }

        internal static Dictionary<string, int> ExportDataKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "DATA/Csystem");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy DATA/Csystem scheme has no KnownSchemes member.");
            var result = ReadDataDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy DATA/Csystem scheme map");
            if (result.Count == 0)
                throw new InvalidDataException ("Legacy DATA/Csystem scheme map is empty.");
            return result;
        }

        internal static List<AvcSchemeRecord> ExportAvcKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "AVC");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy AVC scheme has no KnownSchemes member.");
            var array = ReadRaw (scheme, "KnownSchemes") as ArrayRecord;
            if (array == null || array.Rank != 1 || array.Lengths[0] == 0 || array.Lengths[0] > 256)
                throw new InvalidDataException ("Legacy AVC scheme array is invalid.");
            var result = new List<AvcSchemeRecord> (array.Lengths[0]);
            foreach (var record in GetRecordArray (array))
            {
                var value = record as ClassRecord;
                if (value == null)
                    throw new InvalidDataException ("Legacy AVC scheme array contains an invalid entry.");
                var exported = new AvcSchemeRecord {
                    Password = ReadRequired<string> (value, "Password"),
                    KeyOffset = ReadRequired<int> (value, "KeyOffset"),
                    HeaderOffset = ReadRequired<int> (value, "HeaderOffset"),
                };
                if (exported.Password == null || exported.Password.Length > 256
                    || exported.KeyOffset < 0 || exported.KeyOffset > 0x10000
                    || exported.HeaderOffset < 0 || exported.HeaderOffset > 0x10000)
                    throw new InvalidDataException ("Legacy AVC scheme contains invalid offsets.");
                result.Add (exported);
            }
            return result;
        }

        internal static List<DpkSchemeRecord> ExportDpkKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "DPK");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy DPK scheme has no KnownSchemes member.");
            var array = ReadRaw (scheme, "KnownSchemes") as ArrayRecord;
            if (array == null || array.Rank != 1 || array.Lengths[0] == 0 || array.Lengths[0] > 256)
                throw new InvalidDataException ("Legacy DPK scheme array is invalid.");
            var result = new List<DpkSchemeRecord> (array.Lengths[0]);
            foreach (var record in GetRecordArray (array))
            {
                var value = record as ClassRecord;
                if (value == null)
                    throw new InvalidDataException ("Legacy DPK scheme array contains an invalid entry.");
                var exported = new DpkSchemeRecord {
                    Key1 = ReadRequired<uint> (value, "Key1", "<Key1>k__BackingField"),
                    Key2 = ReadRequired<uint> (value, "Key2", "<Key2>k__BackingField"),
                    Name = ReadOptional<string> (value, "Name", "<Name>k__BackingField"),
                    OriginalTitle = ReadOptional<string> (value, "OriginalTitle", "<OriginalTitle>k__BackingField"),
                };
                if (exported.Name != null && exported.Name.Length > 256
                    || exported.OriginalTitle != null && exported.OriginalTitle.Length > 256)
                    throw new InvalidDataException ("Legacy DPK scheme contains an invalid title.");
                result.Add (exported);
            }
            return result;
        }

        internal static Dictionary<string, Dictionary<string, byte[]>> ExportAgsiKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "PAK/AGSI");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy AGSI scheme has no KnownSchemes member.");
            return ReadByteDictionaryMap (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy AGSI title/archive key map");
        }

        internal static Dictionary<string, byte[]> ExportLeafKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "PAK/LEAF");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy Leaf scheme has no KnownSchemes member.");
            return ReadByteDictionary (ReadRaw (scheme, "KnownSchemes") as ClassRecord,
                "Legacy Leaf key map");
        }

        internal static Dictionary<string, byte[]> ExportIkuraKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "IKURA/GDL");
            if (scheme == null || !scheme.HasMember ("KnownSecrets"))
                throw new InvalidDataException ("Legacy IKURA scheme has no KnownSecrets member.");
            return ReadByteDictionary (ReadRaw (scheme, "KnownSecrets") as ClassRecord,
                "Legacy IKURA secret map");
        }

        internal static Dictionary<uint, Fsb5HeaderRecord> ExportFsb5Keys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "FSB5");
            if (scheme == null || !scheme.HasMember ("VorbisHeaders"))
                throw new InvalidDataException ("Legacy FSB5 scheme has no VorbisHeaders member.");
            var dictionary = ReadRaw (scheme, "VorbisHeaders") as ClassRecord;
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy FSB5 Vorbis header map is not a dictionary.");
            var result = new Dictionary<uint, Fsb5HeaderRecord> ();
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 4096)
                throw new InvalidDataException ("Legacy FSB5 Vorbis header map has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy FSB5 Vorbis header map contains an invalid entry.");
                var key = ReadRaw (entry, "key");
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (!(key is uint) || value == null)
                    throw new InvalidDataException ("Legacy FSB5 Vorbis header map contains an incomplete entry.");
                var exported = new Fsb5HeaderRecord {
                    VorbisData = ReadRequiredArray<byte> (value, "VorbisData"),
                    PatchOffset = ReadRequired<int> (value, "PatchOffset"),
                    PatchData = ReadOptionalArray<byte> (value, "PatchData"),
                };
                if (exported.VorbisData.Length == 0 || exported.VorbisData.Length > 1024 * 1024
                    || exported.PatchOffset < 0
                    || exported.PatchData != null && (exported.PatchOffset > exported.VorbisData.Length
                        || exported.PatchData.Length > exported.VorbisData.Length - exported.PatchOffset))
                    throw new InvalidDataException ("Legacy FSB5 Vorbis header map contains an invalid header.");
                if (!result.TryAdd ((uint)key, exported))
                    throw new InvalidDataException ("Legacy FSB5 Vorbis header map contains a duplicate signature: " + key);
            }
            if (result.Count == 0)
                throw new InvalidDataException ("Legacy FSB5 Vorbis header map is empty.");
            return result;
        }

        internal static Dictionary<string, CpzSchemeRecord> ExportCpzKeys (LegacyFormatsDatabase database)
        {
            var scheme = FindScheme (database, "CPZ");
            if (scheme == null || !scheme.HasMember ("KnownSchemes"))
                throw new InvalidDataException ("Legacy CPZ scheme has no KnownSchemes member.");
            var dictionary = ReadRaw (scheme, "KnownSchemes") as ClassRecord;
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy CPZ scheme map is not a dictionary.");
            var result = new Dictionary<string, CpzSchemeRecord> (StringComparer.Ordinal);
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] == 0 || pairs.Lengths[0] > 256)
                throw new InvalidDataException ("Legacy CPZ scheme map has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy CPZ scheme map contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException ("Legacy CPZ scheme map contains an incomplete entry.");
                var exported = new CpzSchemeRecord {
                    Version = ReadRequired<int> (value, "Version"),
                    Cpz5Secret = ReadRequiredArray<uint> (value, "Cpz5Secret"),
                    Md5Variant = ReadEnumInt (ReadRaw (value, "Md5Variant"), "Legacy CPZ Md5Variant"),
                    DecoderFactor = ReadRequired<uint> (value, "DecoderFactor"),
                    EntryInitKey = ReadRequired<uint> (value, "EntryInitKey"),
                    EntrySubKey = ReadRequired<uint> (value, "EntrySubKey"),
                    EntryTailKey = ReadRequired<byte> (value, "EntryTailKey"),
                    EntryKeyPos = ReadRequired<byte> (value, "EntryKeyPos"),
                    IndexSeed = ReadRequired<uint> (value, "IndexSeed"),
                    IndexAddend = ReadRequired<uint> (value, "IndexAddend"),
                    IndexSubtrahend = ReadRequired<uint> (value, "IndexSubtrahend"),
                    DirKeyAddend = ReadRequiredArray<uint> (value, "DirKeyAddend"),
                };
                if (exported.Version < 5 || exported.Version > 7
                    || exported.Cpz5Secret.Length != 24 || exported.DirKeyAddend.Length != 4
                    || exported.Md5Variant < 0 || exported.Md5Variant > 6)
                    throw new InvalidDataException ("Legacy CPZ scheme contains invalid parameters: " + title);
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException ("Legacy CPZ scheme map contains a duplicate title: " + title);
            }
            return result;
        }

        static Xp3ExportProfile TryExportProfile (string title, ClassRecord crypt, out string reason)
        {
            reason = null;
            object parameters;
            string algorithm;
            if (SimpleAlgorithms.TryGetValue (crypt.TypeName.FullName, out algorithm))
            {
                parameters = new Dictionary<string, object>();
            }
            else
            switch (crypt.TypeName.FullName)
            {
            case "GameRes.Formats.KiriKiri.AkabeiCrypt":
                algorithm = "akabei";
                parameters = new Xp3ExportSeedParameters {
                    Seed = ReadRequired<uint> (crypt, "m_seed"),
                };
                break;
            case "GameRes.Formats.KiriKiri.MadoCrypt":
                algorithm = "mado";
                parameters = new Xp3ExportSeedParameters {
                    Seed = ReadRequired<uint> (crypt, "AkabeiCrypt+m_seed"),
                };
                break;
            case "GameRes.Formats.KiriKiri.XorCrypt":
                algorithm = "xor";
                parameters = new Xp3ExportByteKeyParameters {
                    Key = ReadRequired<byte> (crypt, "m_key"),
                };
                break;
            case "GameRes.Formats.KiriKiri.StripeCrypt":
                algorithm = "stripe";
                parameters = new Xp3ExportByteKeyParameters {
                    Key = ReadRequired<byte> (crypt, "m_key"),
                };
                break;
            case "GameRes.Formats.KiriKiri.SmileCrypt":
                algorithm = "smile";
                parameters = new Xp3ExportSmileParameters {
                    KeyXor = ReadRequired<uint> (crypt, "m_key_xor"),
                    FirstXor = ReadRequired<byte> (crypt, "m_first_xor"),
                    ZeroXor = ReadRequired<byte> (crypt, "m_zero_xor"),
                };
                break;
            case "GameRes.Formats.KiriKiri.SmxCrypt":
                algorithm = "smx";
                var smxMask = ReadRequired<int> (crypt, "Mask");
                var smxKeySeq = ReadRequiredArray<byte> (crypt, "KeySeq");
                if (smxMask < 0 || smxKeySeq.Length <= smxMask + 1)
                    throw new InvalidDataException ("Legacy SMX profile has invalid key sequence: " + title);
                parameters = new Xp3ExportSmxParameters {
                    Mask = smxMask,
                    KeySeq = smxKeySeq,
                };
                break;
            case "GameRes.Formats.KiriKiri.CxEncryption":
                algorithm = "cx-encryption";
                parameters = new Xp3ExportCxParameters { Cx = ReadCx (crypt) };
                break;
            case "GameRes.Formats.KiriKiri.SenrenCxCrypt":
                algorithm = "senren-cx";
                parameters = new Xp3ExportCxParameters { Cx = ReadCx (crypt) };
                break;
            case "GameRes.Formats.KiriKiri.CabbageCxCrypt":
                algorithm = "cabbage-cx";
                parameters = new Xp3ExportSeededCxParameters {
                    Cx = ReadCx (crypt),
                    Seed = ReadRequired<uint> (crypt, "m_random_seed", "CabbageCxCrypt+m_random_seed"),
                };
                break;
            case "GameRes.Formats.KiriKiri.NanaCxCrypt":
                algorithm = "nana-cx";
                parameters = new Xp3ExportNanaCxParameters {
                    Cx = ReadCx (crypt),
                    Seed = ReadRequired<uint> (crypt, "CabbageCxCrypt+m_random_seed", "m_random_seed"),
                    YuzKey = ReadRequiredArray<uint> (crypt, "YuzKey"),
                };
                break;
            case "GameRes.Formats.KiriKiri.RiddleCxCrypt":
                algorithm = "riddle-cx";
                parameters = new Xp3ExportRiddleCxParameters {
                    Cx = ReadCx (crypt),
                    Seed = ReadRequired<uint> (crypt, "CabbageCxCrypt+m_random_seed", "m_random_seed"),
                    YuzKey = ReadRequiredArray<uint> (crypt, "YuzKey"),
                    Key1 = ReadRequired<uint> (crypt, "m_key1"),
                    Key2 = ReadRequired<uint> (crypt, "m_key2"),
                };
                break;
            case "GameRes.Formats.KiriKiri.HxCrypt":
                algorithm = "hx";
                var indexKeys = ReadHxIndexKeys (crypt);
                var defaultIndexKey1 = ReadOptionalArray<byte> (crypt, "IndexKey1");
                var defaultIndexKey2 = ReadOptionalArray<byte> (crypt, "IndexKey2");
                if ((defaultIndexKey1 == null) != (defaultIndexKey2 == null))
                    throw new InvalidDataException ("Legacy Hx record has an incomplete default index key.");
                if (defaultIndexKey1 == null && (indexKeys == null || indexKeys.Count == 0))
                    throw new InvalidDataException ("Legacy Hx record has no index key.");
                if (defaultIndexKey1 != null)
                    ValidateHxIndexKey (new Xp3ExportHxIndexKey { Key1 = defaultIndexKey1, Key2 = defaultIndexKey2 }, title);
                parameters = new Xp3ExportHxParameters {
                    Cx = ReadCx (crypt),
                    FilterKey = ReadRequired<ulong> (crypt, "FilterKey"),
                    RandomType = ReadRequired<int> (crypt, "RandomType"),
                    NamesFile = ReadOptional<string> (crypt, "NamesFile"),
                    IndexKey1 = defaultIndexKey1,
                    IndexKey2 = defaultIndexKey2,
                    IndexKeys = indexKeys,
                };
                break;
            case "GameRes.Formats.KiriKiri.HxCryptLite":
                algorithm = "hx-lite";
                parameters = new Xp3ExportHxLiteParameters {
                    Cx = ReadCx (crypt),
                    HeaderKey = ReadOptionalArray<byte> (crypt, "mHeaderKey"),
                    HeaderSplitPosition = ReadRequired<long> (crypt, "mHeaderSplitPosition"),
                    FileCryptFlag = ReadRequired<bool> (crypt, "mFileCryptFlag"),
                    RandomType = ReadRequired<int> (crypt, "mRandomType"),
                };
                break;
            default:
                reason = "The modern runtime has no matching parameterized XP3 algorithm adapter.";
                return null;
            }
            return new Xp3ExportProfile {
                Id = title,
                Title = title,
                Algorithm = algorithm,
                Parameters = parameters,
            };
        }

        static Xp3ExportCxData ReadCx (ClassRecord record)
        {
            return new Xp3ExportCxData {
                Mask = ReadRequired<uint> (record, "m_mask", "CxEncryption+m_mask", "SenrenCxCrypt+m_mask", "CabbageCxCrypt+m_mask", "NanaCxCrypt+m_mask"),
                Offset = ReadRequired<uint> (record, "m_offset", "CxEncryption+m_offset", "SenrenCxCrypt+m_offset", "CabbageCxCrypt+m_offset", "NanaCxCrypt+m_offset"),
                PrologOrder = ReadRequiredArray<byte> (record, "PrologOrder", "CxEncryption+PrologOrder", "SenrenCxCrypt+PrologOrder", "CabbageCxCrypt+PrologOrder", "NanaCxCrypt+PrologOrder"),
                OddBranchOrder = ReadRequiredArray<byte> (record, "OddBranchOrder", "CxEncryption+OddBranchOrder", "SenrenCxCrypt+OddBranchOrder", "CabbageCxCrypt+OddBranchOrder", "NanaCxCrypt+OddBranchOrder"),
                EvenBranchOrder = ReadRequiredArray<byte> (record, "EvenBranchOrder", "CxEncryption+EvenBranchOrder", "SenrenCxCrypt+EvenBranchOrder", "CabbageCxCrypt+EvenBranchOrder", "NanaCxCrypt+EvenBranchOrder"),
                ControlBlock = ReadOptionalArray<uint> (record, "ControlBlock", "CxEncryption+ControlBlock", "SenrenCxCrypt+ControlBlock", "CabbageCxCrypt+ControlBlock", "NanaCxCrypt+ControlBlock"),
                TpmFileName = ReadOptional<string> (record, "TpmFileName", "CxEncryption+TpmFileName", "SenrenCxCrypt+TpmFileName", "CabbageCxCrypt+TpmFileName", "NanaCxCrypt+TpmFileName"),
            };
        }

        static object ReadRaw (ClassRecord record, params string[] names)
        {
            foreach (var name in names)
            {
                if (record.HasMember (name))
                    return record.GetRawValue (name);
            }
            return null;
        }

        static int ReadEnumInt (object value, string label)
        {
            if (value is int number)
                return number;
            if (value == null)
                throw new InvalidDataException (label + " contains a missing enum value.");

            var type = value.GetType();
            var getRawValue = type.GetMethod ("GetRawValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof (string) }, null);
            if (getRawValue != null)
            {
                var raw = getRawValue.Invoke (value, new object[] { "value__" });
                if (raw is int enumNumber)
                    return enumNumber;
            }
            throw new InvalidDataException (label + " contains an invalid enum value: " + type.FullName);
        }

        static ClassRecord FindScheme (LegacyFormatsDatabase database, string tag)
        {
            if (database == null || database.Root == null || !database.Root.HasMember ("SchemeMap"))
                throw new InvalidDataException ("Legacy database has no scheme map.");
            var dictionary = database.Root.GetRawValue ("SchemeMap") as ClassRecord;
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy scheme map is not a dictionary.");
            if (!dictionary.HasMember ("KeyValuePairs"))
                return null;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return null;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException ("Legacy scheme map has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy scheme map contains an invalid entry.");
                if (string.Equals (ReadRaw (entry, "key") as string, tag, StringComparison.OrdinalIgnoreCase))
                    return ReadRaw (entry, "value") as ClassRecord;
            }
            return null;
        }

        static Dictionary<string, string> ReadStringDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, string> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as string;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, value))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, int> ReadIntDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, int> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value");
                if (string.IsNullOrWhiteSpace (key) || !(value is int))
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, (int)value))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<uint, string> ReadUIntStringDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<uint, string> ();
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key");
                var value = ReadRaw (entry, "value") as string;
                if (!(key is uint) || string.IsNullOrWhiteSpace (value))
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd ((uint)key, value))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, byte[]> ReadByteDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, byte[]> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as SZArrayRecord<byte>;
                if (string.IsNullOrWhiteSpace (key) || value == null || value.Length == 0)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, value.GetArray (false)))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, AzEncryptedKeyRecord> ReadAzEncryptedDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, AzEncryptedKeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var indexKey = ReadRequired<uint> (value, "IndexKey");
                var contentRaw = ReadRaw (value, "ContentKey");
                uint? contentKey = null;
                if (contentRaw != null)
                {
                    if (!(contentRaw is uint))
                        throw new InvalidDataException (label + " contains an invalid ContentKey for " + key);
                    contentKey = (uint)contentRaw;
                }
                if (indexKey == 0 || !result.TryAdd (key, new AzEncryptedKeyRecord {
                    IndexKey = indexKey,
                    ContentKey = contentKey,
                }))
                    throw new InvalidDataException (label + " contains a duplicate or empty key: " + key);
            }
            return result;
        }

        static Dictionary<string, PbzKeyRecord> ReadPbzDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, PbzKeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new PbzKeyRecord {
                    ArcKey = ReadRequiredArray<byte> (value, "ArcKey"),
                    ScriptKey = ReadRequiredArray<byte> (value, "ScriptKey"),
                };
                if (exported.ArcKey.Length > 256 || exported.ScriptKey.Length > 256)
                    throw new InvalidDataException (label + " contains an oversized key: " + key);
                if (!result.TryAdd (key, exported))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, Ai5KeyRecord> ReadAi5Dictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Ai5KeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new Ai5KeyRecord {
                    NameLength = ReadRequired<int> (value, "NameLength"),
                    NameKey = ReadRequired<byte> (value, "NameKey"),
                    SizeKey = ReadRequired<uint> (value, "SizeKey"),
                    OffsetKey = ReadRequired<uint> (value, "OffsetKey"),
                };
                if (exported.NameLength <= 0 || exported.NameLength > 0x100)
                    throw new InvalidDataException (label + " contains an invalid name length: " + key);
                if (!result.TryAdd (key, exported))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, NpaKeyRecord> ReadNpaDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, NpaKeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var titleId = ReadRaw (value, "TitleId");
                var exported = new NpaKeyRecord {
                    TitleId = ReadEnumInt (titleId, label + ": " + key),
                    NameKey = ReadRequired<uint> (value, "NameKey"),
                    Order = ReadRequiredArray<byte> (value, "Order"),
                };
                if (exported.Order.Length == 0 || exported.Order.Length > 256)
                    throw new InvalidDataException (label + " contains an invalid order: " + key);
                if (!result.TryAdd (key, exported))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, LpkSchemeRecord> ReadLpkSchemeDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, LpkSchemeRecord> (StringComparer.Ordinal);
            var pairs = GetDictionaryPairs (dictionary, label);
            foreach (var record in pairs)
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var baseKey = ReadRaw (value, "BaseKey") as ClassRecord;
                if (baseKey == null)
                    throw new InvalidDataException (label + " contains a scheme without BaseKey: " + title);
                var exported = new LpkSchemeRecord {
                    BaseKey = ReadLpkKeyRecord (baseKey, label + ": " + title),
                    ContentXor = ReadRequired<byte> (value, "ContentXor"),
                    RotatePattern = ReadRequired<uint> (value, "RotatePattern"),
                    ImportGameInit = ReadRequired<bool> (value, "ImportGameInit"),
                };
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, Dictionary<string, LpkKeyRecord>> ReadLpkKeyMap (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Dictionary<string, LpkKeyRecord>> (StringComparer.Ordinal);
            var pairs = GetDictionaryPairs (dictionary, label);
            foreach (var record in pairs)
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var fileKeys = new Dictionary<string, LpkKeyRecord> (StringComparer.Ordinal);
                foreach (var fileRecord in GetDictionaryPairs (value, label + ": " + title))
                {
                    var fileEntry = fileRecord as ClassRecord;
                    if (fileEntry == null || !fileEntry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                        throw new InvalidDataException (label + " contains an invalid file-key entry.");
                    var fileName = ReadRaw (fileEntry, "key") as string;
                    var fileValue = ReadRaw (fileEntry, "value") as ClassRecord;
                    if (string.IsNullOrWhiteSpace (fileName) || fileValue == null)
                        throw new InvalidDataException (label + " contains an incomplete file-key entry.");
                    if (!fileKeys.TryAdd (fileName, ReadLpkKeyRecord (fileValue, label + ": " + title + ": " + fileName)))
                        throw new InvalidDataException (label + " contains a duplicate file key: " + title + "/" + fileName);
                }
                if (!result.TryAdd (title, fileKeys))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static LpkKeyRecord ReadLpkKeyRecord (ClassRecord record, string label)
        {
            var result = new LpkKeyRecord {
                Key1 = ReadRequired<uint> (record, "Key1"),
                Key2 = ReadRequired<uint> (record, "Key2"),
            };
            if (result.Key1 == 0 && result.Key2 == 0)
                throw new InvalidDataException (label + " contains an all-zero key.");
            return result;
        }

        static Array GetDictionaryPairs (ClassRecord dictionary, string label)
        {
            if (!dictionary.HasMember ("KeyValuePairs"))
                return Array.Empty<SerializationRecord> ();
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return Array.Empty<SerializationRecord> ();
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            return GetRecordArray (pairs);
        }

        static Dictionary<string, byte> ReadStringByteDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, byte> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value");
                if (string.IsNullOrWhiteSpace (key) || !(value is byte))
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, (byte)value))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<uint, byte[]> ReadUIntByteDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<uint, byte[]> ();
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key");
                var value = ReadRaw (entry, "value") as SZArrayRecord<byte>;
                if (!(key is uint) || value == null || value.Length == 0)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd ((uint)key, value.GetArray (false)))
                    throw new InvalidDataException (label + " contains a duplicate signature: " + key);
            }
            return result;
        }

        static Dictionary<string, BinIdxKeyRecord> ReadBinIdxDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, BinIdxKeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new BinIdxKeyRecord {
                    Key = ReadRequiredArray<byte> (value, "Key"),
                    IV = ReadRequiredArray<byte> (value, "IV"),
                };
                if ((exported.Key.Length != 16 && exported.Key.Length != 24 && exported.Key.Length != 32)
                    || exported.IV.Length != 16)
                    throw new InvalidDataException (label + " contains an invalid key/IV pair: " + title);
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, uint> ReadStringUIntDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, uint> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value");
                if (string.IsNullOrWhiteSpace (title) || !(value is uint))
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (title, (uint)value))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<int, uint> ReadIntUIntDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<int, uint> ();
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key");
                var value = ReadRaw (entry, "value");
                if (!(key is int) || !(value is uint))
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd ((int)key, (uint)value))
                    throw new InvalidDataException (label + " contains a duplicate numeric key: " + key);
            }
            return result;
        }

        static Dictionary<string, Dictionary<string, uint>> ReadStringUIntDictionaryMap (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Dictionary<string, uint>> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, ReadStringUIntDictionary (value, label + ": " + key)))
                    throw new InvalidDataException (label + " contains a duplicate title: " + key);
            }
            return result;
        }

        static Dictionary<string, Dictionary<int, uint>> ReadIntUIntDictionaryMap (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Dictionary<int, uint>> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, ReadIntUIntDictionary (value, label + ": " + key)))
                    throw new InvalidDataException (label + " contains a duplicate title: " + key);
            }
            return result;
        }

        static Dictionary<string, YpfSchemeRecord> ReadYpfDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, YpfSchemeRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var swapTable = ReadRaw (value, "SwapTable") as SZArrayRecord<byte>;
                if (swapTable == null || swapTable.Length == 0 || swapTable.Length > 256)
                    throw new InvalidDataException (label + " contains an invalid swap table: " + title);
                var exported = new YpfSchemeRecord {
                    SwapTable = swapTable.GetArray (false),
                    Key = ReadRequired<byte> (value, "Key"),
                    GuessKey = ReadRequired<bool> (value, "GuessKey"),
                    ExtraHeaderSize = ReadRequired<uint> (value, "ExtraHeaderSize"),
                    ScriptKey = ReadRequired<uint> (value, "ScriptKey"),
                    CompressType = ReadEnumInt (ReadRaw (value, "CompressType"), label + ": " + title),
                };
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, TacticsSchemeRecord> ReadTacticsDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, TacticsSchemeRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new TacticsSchemeRecord {
                    Password = ReadRequired<string> (value, "Password"),
                    CustomLzss = ReadRequired<bool> (value, "CustomLzss"),
                };
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, RpmSchemeRecord> ReadRpmDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, RpmSchemeRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new RpmSchemeRecord {
                    Keyword = ReadRequired<string> (value, "Keyword"),
                    NameLength = ReadRequired<int> (value, "NameLength"),
                };
                if (string.IsNullOrEmpty (exported.Keyword) || exported.Keyword.Length > 256
                    || exported.NameLength < 4 || exported.NameLength > 256)
                    throw new InvalidDataException (label + " contains an invalid scheme: " + title);
                if (!result.TryAdd (title, exported))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, int> ReadDataDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, int> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var title = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (title) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var extraHeaderSize = ReadRequired<int> (value, "ExtraHeaderSize");
                if (extraHeaderSize < 0 || extraHeaderSize > 0x1000)
                    throw new InvalidDataException (label + " contains an invalid header size: " + title);
                if (!result.TryAdd (title, extraHeaderSize))
                    throw new InvalidDataException (label + " contains a duplicate title: " + title);
            }
            return result;
        }

        static Dictionary<string, uint[]> ReadUIntArrayDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, uint[]> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as SZArrayRecord<uint>;
                if (string.IsNullOrWhiteSpace (key) || value == null || value.Length == 0)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, value.GetArray (false)))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, IntKeyRecord> ReadIntKeyDataDictionary (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, IntKeyRecord> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                var exported = new IntKeyRecord {
                    Key = ReadRequired<uint> (value, "Key"),
                    Passphrase = ReadRequired<string> (value, "Passphrase"),
                };
                if (string.IsNullOrEmpty (exported.Passphrase))
                    throw new InvalidDataException (label + " contains an empty passphrase: " + key);
                if (!result.TryAdd (key, exported))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, Dictionary<string, string>> ReadStringDictionaryMap (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Dictionary<string, string>> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, ReadStringDictionary (value, label + ": " + key)))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static Dictionary<string, Dictionary<string, byte[]>> ReadByteDictionaryMap (ClassRecord dictionary, string label)
        {
            if (dictionary == null || !dictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException (label + " is not a dictionary.");
            var result = new Dictionary<string, Dictionary<string, byte[]>> (StringComparer.Ordinal);
            if (!dictionary.HasMember ("KeyValuePairs"))
                return result;
            var rawPairs = dictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return result;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 100000)
                throw new InvalidDataException (label + " has invalid entries.");
            foreach (var record in GetRecordArray (pairs))
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException (label + " contains an invalid entry.");
                var key = ReadRaw (entry, "key") as string;
                var value = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (key) || value == null)
                    throw new InvalidDataException (label + " contains an incomplete entry.");
                if (!result.TryAdd (key, ReadByteDictionary (value, label + ": " + key)))
                    throw new InvalidDataException (label + " contains a duplicate key: " + key);
            }
            return result;
        }

        static T ReadRequired<T> (ClassRecord record, params string[] names)
        {
            var value = ReadRaw (record, names);
            if (value is T typed)
                return typed;
            throw new InvalidDataException ("Legacy XP3 record is missing or has invalid member: " + string.Join (",", names));
        }

        static T ReadOptional<T> (ClassRecord record, params string[] names) where T : class
        {
            var value = ReadRaw (record, names);
            if (value == null)
                return null;
            var typed = value as T;
            if (typed == null)
                throw new InvalidDataException ("Legacy XP3 record has invalid member: " + string.Join (",", names));
            return typed;
        }

        static T[] ReadRequiredArray<T> (ClassRecord record, params string[] names)
        {
            var values = ReadOptionalArray<T> (record, names);
            if (values == null)
                throw new InvalidDataException ("Legacy XP3 record is missing array member: " + string.Join (",", names));
            return values;
        }

        static T[] ReadOptionalArray<T> (ClassRecord record, params string[] names)
        {
            var value = ReadRaw (record, names);
            if (value == null)
                return null;
            var array = value as SZArrayRecord<T>;
            if (array == null || array.Length > 1024 * 1024)
                throw new InvalidDataException ("Legacy XP3 record has invalid array member: " + string.Join (",", names));
            return array.GetArray (false);
        }

        static Dictionary<string, Xp3ExportHxIndexKey> ReadHxIndexKeys (ClassRecord crypt)
        {
            var serializedDictionary = ReadRaw (crypt, "IndexKeyDict") as ClassRecord;
            if (serializedDictionary == null)
                return null;
            if (!serializedDictionary.TypeName.FullName.StartsWith ("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal))
                throw new InvalidDataException ("Legacy Hx index-key member is not a dictionary.");

            // Dictionary<TKey, TValue> implements ISerializable and stores its logical contents
            // as KeyValuePairs. Reading this array preserves the association for each archive.
            if (!serializedDictionary.HasMember ("KeyValuePairs"))
                return null;
            var rawPairs = serializedDictionary.GetRawValue ("KeyValuePairs");
            if (rawPairs == null)
                return null;
            var pairs = rawPairs as ArrayRecord;
            if (pairs == null || pairs.Rank != 1 || pairs.Lengths[0] > 4096)
                throw new InvalidDataException ("Legacy Hx index-key dictionary has invalid entries.");

            var result = new Dictionary<string, Xp3ExportHxIndexKey> (StringComparer.OrdinalIgnoreCase);
            var entries = GetRecordArray (pairs);
            foreach (var record in entries)
            {
                var entry = record as ClassRecord;
                if (entry == null || !entry.TypeName.FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy Hx index-key dictionary contains an invalid entry.");
                var archiveName = ReadRaw (entry, "key") as string;
                var key = ReadRaw (entry, "value") as ClassRecord;
                if (string.IsNullOrWhiteSpace (archiveName) || key == null
                    || !string.Equals (key.TypeName.FullName, "GameRes.Formats.KiriKiri.HxIndexKey", StringComparison.Ordinal))
                    throw new InvalidDataException ("Legacy Hx index-key dictionary contains an invalid key.");

                var exported = new Xp3ExportHxIndexKey {
                    Key1 = ReadRequiredArray<byte> (key, "Key1"),
                    Key2 = ReadRequiredArray<byte> (key, "Key2"),
                };
                ValidateHxIndexKey (exported, archiveName);
                if (!result.TryAdd (archiveName, exported))
                    throw new InvalidDataException ("Legacy Hx index-key dictionary contains a duplicate archive: " + archiveName);
            }
            return result;
        }

        static void ValidateHxIndexKey (Xp3ExportHxIndexKey key, string archiveName)
        {
            if (key.Key1 == null || key.Key1.Length != 32 || key.Key2 == null || key.Key2.Length != 16)
                throw new InvalidDataException ("Legacy Hx index-key dictionary has invalid key lengths: " + archiveName);
        }

        static Array GetRecordArray (ArrayRecord array)
        {
            var arrayType = array.GetType();
            if (arrayType.Assembly != typeof (ArrayRecord).Assembly)
                throw new InvalidDataException ("Legacy Hx index-key array has an invalid record type.");
            var getArray = arrayType.GetMethod ("GetArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof (bool) }, null);
            if (getArray == null || !typeof (Array).IsAssignableFrom (getArray.ReturnType))
                throw new InvalidDataException ("Legacy Hx index-key array cannot be read.");
            try
            {
                return getArray.Invoke (array, new object[] { false }) as Array
                    ?? throw new InvalidDataException ("Legacy Hx index-key array cannot be read.");
            }
            catch (TargetInvocationException error)
            {
                throw new InvalidDataException ("Legacy Hx index-key array cannot be read.", error.InnerException ?? error);
            }
        }
    }
}
