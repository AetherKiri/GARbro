# Legacy Scheme Inventory

The trusted `Formats.dat` source (database version `148`) contains `77` serialized `SchemeMap` entries representing `64` legacy `ResourceScheme` types. The offline migration command records each format tag, serialized type name, and member names without deserializing CLR objects or exposing key values:

```sh
dotnet run --project Modern/GARbro.LegacyDataMigration/GARbro.LegacyDataMigration.csproj --configuration Release -- \
  export-scheme-inventory ArcFormats/Resources/Formats.dat inventory.json
```

The first non-XP3 data slice is ZIP. Its seven title-to-password records are now in `Modern/GameData/v2/zip/passwords.json` and are checksum-verified through the v2 manifest.

| Tag | Legacy type | Members |
| --- | --- | --- |
| `ZIP` | `GameRes.Formats.PkWare.ZipScheme` | `KnownKeys` |
| `TCD` | `GameRes.Formats.TopCat.TcdScheme` | `KnownKeys` |

`KnownKeys` is serialized as a string-to-string dictionary. The modern ZIP implementation now checks the resolved executable title before prompting, while preserving the interactive fallback. `ZipPasswordDatabase` validates schema, required values, duplicate titles, and conflicting case-insensitive keys. `FormatCatalogTests.Encrypted_zip_uses_migrated_title_password_before_prompt` opens a ZipCrypto archive beside a mapped executable and confirms the migrated password is used without raising an interactive request.

Other simple-looking records such as `PAK/MORNING.DefaultKey` and `GAL.KnownKeys` belong to format implementations that are not yet included in the modern solution; they remain inventory-only until those ports are in scope.

The `TCD` record is safely exportable with `export-tcd-keys`, validated by fixed-count/value regression assertions, and now loaded by the modern TopCat port. `FormatCatalogTests.Tcd_format_opens_a_minimal_v3_fixture` verifies index parsing and extraction from a deterministic TCD3 archive.
