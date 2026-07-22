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
| `PAK/MORNING` | `GameRes.Formats.Morning.MorningScheme` | `DefaultKey` |
| `FPK/MOONHIR` | `GameRes.Formats.MoonhirGames.Fpk0100Scheme` | `KnownKeys` |
| `CMP` | `GameRes.Formats.GameSystem.CmpScheme` | `KnownKeys` |
| `PKG/2` | `GameRes.Formats.Yatagarasu.PkgScheme` | `KnownKeys` |
| `CSAF` | `GameRes.Formats.FamilyAdvSystem.FamilyAdvScheme` | `KnownKeys` |
| `MBL` | `GameRes.Formats.Marble.MblScheme` | `KnownKeys` |
| `NPK` | `GameRes.Formats.NitroPlus.Npk2Scheme` | `KnownKeys` |
| `PCK/TAMAMO` | `GameRes.Formats.Tamamo.PckScheme` | `KnownKeys` |
| `NS2` | `GameRes.Formats.NScripter.NsaScheme` | `KnownKeys` |
| `NSA` | `GameRes.Formats.NScripter.NsaScheme` | `KnownKeys` |

`KnownKeys` is serialized as a string-to-string dictionary. The modern ZIP implementation now checks the resolved executable title before prompting, while preserving the interactive fallback. `ZipPasswordDatabase` validates schema, required values, duplicate titles, and conflicting case-insensitive keys. `FormatCatalogTests.Encrypted_zip_uses_migrated_title_password_before_prompt` opens a ZipCrypto archive beside a mapped executable and confirms the migrated password is used without raising an interactive request.

Other simple-looking records such as `GAL.KnownKeys` belong to format implementations that are not yet included in the modern solution; they remain inventory-only until those ports are in scope.

The `TCD` record is safely exportable with `export-tcd-keys`, validated by fixed-count/value regression assertions, and now loaded by the modern TopCat port. `FormatCatalogTests.Tcd_format_opens_a_minimal_v3_fixture` verifies index parsing and extraction from a deterministic TCD3 archive.

The `PAK/MORNING` record is safely exportable with `export-morning-key`. Its 512-byte power-of-two key is validated by `MorningKeyDatabase`; `FormatCatalogTests.Morning_format_loads_migrated_key_and_opens_fixture` verifies encrypted index decryption and extraction.

The `FPK/MOONHIR` record is safely exportable with `export-fpk-keys`. Its 28-entry unsigned key list is validated for non-empty, unique values by `FpkKeyDatabase`; `FormatCatalogTests.Moonhir_fpk_format_loads_migrated_keys_and_opens_fixture` verifies index parsing and extraction.

The `CMP` record is safely exportable with `export-cmp-keys`. Its two 16-byte title keys are validated by `CmpKeyDatabase`; `FormatCatalogTests.GameSystem_cmp_format_loads_migrated_keys_and_opens_fixture` verifies compressed-index parsing and extraction.

The `PKG/2` record is safely exportable with `export-pkg-keys`. Its single eight-word title key is validated by `PkgKeyDatabase`; `FormatCatalogTests.Yatagarasu_pkg_format_uses_migrated_key_to_open_fixture` verifies encrypted index and data extraction.

The `CSAF` record is safely exportable with `export-csaf-keys`. Its single title-to-string key is validated by `CsafKeyDatabase`; `FormatCatalogTests.Csaf_format_loads_migrated_keys_and_opens_fixture` verifies unencrypted index parsing and extraction. The same runtime port retains CSAF's legacy AES/MD5 path for encrypted archives.

The `MBL` record is safely exportable with `export-mbl-keys`. Its 58 title-to-string records, including empty-password placeholders, are validated by `MblKeyDatabase`; `FormatCatalogTests.Marble_mbl_format_uses_migrated_key_for_script_fixture` verifies title-keyed script extraction.

The `NPK` record is safely exportable with `export-npk-keys`. Its four 32-byte title-to-AES-key records are validated by `NpkKeyDatabase`; `FormatCatalogTests.Nitroplus_npk_format_uses_migrated_key_for_fixture` verifies encrypted index and entry extraction. The modern port reads encrypted indexes with exact-fill loops because `CryptoStream` can legally return short reads on .NET 10.

The `PCK/TAMAMO` record is safely exportable with `export-pck-keys`. Its four title-to-Blowfish-key records are validated by `PckKeyDatabase`; `FormatCatalogTests.Tamamo_pck_format_uses_migrated_key_for_fixture` verifies Blowfish index and entry extraction. The modern port intentionally omits the legacy WPF texture post-processing path; archive data access remains cross-platform.

The `NS2` record is safely exportable with `export-ns2-keys`. Its six title-to-password records are validated by `Ns2KeyDatabase`; `FormatCatalogTests.Ns2_format_loads_migrated_keys_and_opens_fixture` verifies unencrypted index and extraction.

The `NSA` record is safely exportable with `export-nsa-keys`. Its ten title-to-password records are validated by `NsaKeyDatabase`; `FormatCatalogTests.Nsa_format_uses_migrated_key_for_encrypted_fixture` verifies encrypted index and uncompressed entry extraction. The modern port deliberately rejects SPB, LZSS, and NBZ records until those decompression paths are migrated.
