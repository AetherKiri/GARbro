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

    internal static class LegacyXp3Exporter
    {
        const string KnownSchemePairType = "[GameRes.Formats.KiriKiri.ICrypt,";

        internal static Xp3ExportDocument Export (LegacyFormatsDatabase database, out Xp3ExportReport report)
        {
            var profiles = new List<Xp3ExportProfile>();
            var skipped = new List<Xp3SkippedProfile>();
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
                else
                    profiles.Add (profile);
            }

            report = new Xp3ExportReport {
                SourceDatabaseVersion = database.Version,
                SourceKnownSchemeCount = pairs.Length,
                ExportedProfileCount = profiles.Count,
                SkippedProfiles = skipped,
            };
            return new Xp3ExportDocument { Profiles = profiles };
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

        static Xp3ExportProfile TryExportProfile (string title, ClassRecord crypt, out string reason)
        {
            reason = null;
            object parameters;
            string algorithm;
            switch (crypt.TypeName.FullName)
            {
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
