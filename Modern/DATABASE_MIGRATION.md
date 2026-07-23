# Game-Data Migration

This is the execution checklist for replacing the legacy `Formats.dat` BinaryFormatter object graph. The modern runtime must never deserialize that file. The v2 source data is reviewed JSON; release bundles may be compressed, but compression is not the source of truth.

## v2 Contract

- [x] Define a data-only, versioned manifest (`GameData/v2/manifest.json`).
- [x] Make each dataset identify its format, kind, relative source path, and SHA-256 digest.
- [x] Verify manifest metadata and embedded dataset hashes before deserializing data.
- [x] Move the safe XP3 title-to-algorithm subset out of a C# compressed string into reviewed JSON.
- [x] Add format-specific DTOs and semantic validators for v2 XP3 profiles. Do not deserialize CLR type names or arbitrary polymorphic objects.
- [x] Keep game identification records separate from reusable algorithm profiles in the XP3 slice.
- [x] Merge the legacy XP3 executable map into the v2 title registry and resolve migrated parameterized profiles by title.
- [ ] Define a deterministic release-bundle builder from `GameData/v2`.
- [ ] Sign downloaded release manifests or bundles before accepting updates.

## XP3 Slice

- [x] Preserve the existing generic XP3 title registry as `xp3/title-registry.json`.
- [x] Load that registry through the v2 manifest and checksum verification.
- [x] Replace the broad `Xp3SchemeProfile` DTO for new data with per-algorithm profile records while retaining legacy array JSON compatibility.
- [x] Bundle the exported Hx, Senren, and related parameterized XP3 profiles after schema and count validation.
- [ ] Review every bundled profile against a real archive fixture before release.
- [ ] Add fixture-based open/extract tests for every bundled XP3 profile.
- [ ] Document unsupported legacy XP3 algorithms and migrate them only after their implementations are ported.

Current export coverage for the checked-in database is recorded in [LEGACY_XP3_EXPORT_STATUS.md](LEGACY_XP3_EXPORT_STATUS.md). The v2 profile dataset is now bundled and validated by the modern parser; archive fixture tests are still required before release claims are made.

The migrated game map contains 1,129 executable bindings. It is kept in the title-registry dataset while the XP3 slice remains small enough to review as one unit; it can be split into its own dataset later without changing profile IDs.

## Legacy Export

- [x] Create an offline-only exporter which reads a trusted local `Formats.dat` as NRBF records without instantiating legacy CLR types. Production modern binaries may not read the file.
- [x] Give the supported XP3 parameterized algorithms explicit export adapters, including archive-specific Hx index keys.
- [x] Produce a conversion report: source version, record counts, unsupported types, and output hashes.
- [x] Add fixture-based regression tests for legacy header validation, root validation, stable XP3 export counts, and v2 profile parsing.
- [x] Add the first staged non-XP3 export adapter for the `TCD` string-to-integer key map, with stable count/value assertions.
- [ ] Give each remaining `ResourceScheme` family an explicit export adapter.
- [ ] Compare exported data against known archive fixtures before accepting a generated change.
- [ ] During transition, generate legacy `Formats.dat` from v2 source if legacy releases still need updates. Never edit both sources manually.

## Other Formats

- [x] Inventory the serialized `SchemeMap` records from the trusted legacy database (77 format entries, 64 types).
- [ ] Inventory every `ResourceScheme` implementation and classify it as scalar-key, structured-key, polymorphic-algorithm, or game-identification data.
- [x] Migrate the ZIP scalar title-to-password map as a standalone v2 dataset and use it before interactive password lookup.
- [x] Add ZIP-specific semantic validation and a ZipCrypto fixture covering migrated-password lookup before the interactive prompt.
- [x] Migrate the TCD scalar title-to-integer key map as a standalone v2 dataset and load it when the TopCat port is discovered.
- [x] Add TCD-specific semantic validation and a deterministic TCD3 listing/extraction fixture.
- [x] Migrate the Morning scalar default byte key as a standalone v2 dataset and load it in the modern PAK/MORNING port.
- [x] Add Morning key-shape validation and a deterministic encrypted-index PAK fixture.
- [x] Migrate the Moonhir FPK scalar key list as a standalone v2 dataset and load it in the modern FPK port.
- [x] Add FPK key-list validation and a deterministic index/extraction fixture.
- [x] Migrate the GameSystem CMP title-to-byte-key map as a standalone v2 dataset and load it in the modern CMP port.
- [x] Add CMP key-map validation and a deterministic compressed-index extraction fixture.
- [x] Migrate the Yatagarasu PKG/2 title-to-uint-array key map as a standalone v2 dataset and load it in the modern PKG/2 port.
- [x] Add PKG/2 key-array validation and an encrypted index/data fixture.
- [x] Migrate the CSAF title-to-string key map as a standalone v2 dataset and load it in the modern CSAF port.
- [x] Add CSAF key-map validation and a deterministic unencrypted listing/extraction fixture.
- [x] Migrate the Marble MBL title-to-string key map as a standalone v2 dataset and load it in the modern MBL port.
- [x] Add MBL key-map validation and a deterministic encrypted-script extraction fixture.
- [x] Migrate the NitroPlus NPK title-to-AES-key map as a standalone v2 dataset and load it in the modern NPK port.
- [x] Add NPK key-shape validation and a deterministic encrypted-index/entry fixture, including short-read-safe index parsing.
- [x] Migrate the TamamoSystem PCK title-to-Blowfish-key map as a standalone v2 dataset and load it in the modern PCK port.
- [x] Add PCK key-map validation and a deterministic Blowfish index/entry fixture.
- [x] Migrate the NS2 title-to-password map as a standalone v2 dataset and load it in the modern NS2 port.
- [x] Add NS2 key-map validation and a deterministic unencrypted index/extraction fixture.
- [x] Migrate the NSA title-to-password map as a standalone v2 dataset and load it in the modern NSA port.
- [x] Add NSA key-map validation and a deterministic encrypted uncompressed-index/extraction fixture.
- [ ] Port NSA SPB/LZSS/NBZ decompression before claiming support for compressed entries.
- [x] Migrate the FJSYS MSD password map as a standalone v2 dataset and load it in the modern FJSYS port.
- [x] Add FJSYS password-map validation and a deterministic encrypted `.msd` extraction fixture.
- [x] Migrate the INT structured title-to-key map as a standalone v2 dataset and load it in the modern INT port.
- [x] Add INT key-map validation and deterministic plain/encrypted index and entry fixtures.
- [x] Migrate the NOA nested title-to-archive password map as a standalone v2 dataset and load it in the modern NOA port.
- [x] Add NOA nested-map validation and deterministic Raw/BSHF entry fixtures.
- [x] Migrate the GAL title-to-key map as a standalone v2 dataset and load it in the modern GAL reader.
- [x] Add GAL key-map validation and a deterministic uncompressed, unshuffled pixel fixture.
- [ ] Port GAL compression, JPEG, shuffled/encrypted, layered, and alpha-layer variants.
- [x] Migrate the CRZ identifier-to-byte-key map as a standalone v2 dataset and load it in the modern CRZ reader.
- [x] Add CRZ key-map validation and a deterministic compressed image fixture.
- [x] Migrate the ACTGS byte-key list as a standalone v2 dataset and load it in the modern DAT/CG readers.
- [x] Add ACTGS key-list validation and a deterministic encrypted DAT index/entry fixture.
- [x] Migrate the ADS title-to-byte-key map as a standalone v2 dataset and load it in the modern ADS reader.
- [x] Add ADS key-map validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the Cyberworks OGG/TINK signature-to-byte-key map as a standalone v2 dataset and load it in the modern audio reader.
- [x] Add TINK key-map validation and deterministic header/XOR decoding coverage; full Vorbis payload fixture remains a follow-up.
- [x] Migrate the Unity BIN/IDX title-to-AES key/IV map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add BIN/IDX key/IV validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the AZ ARC title-to-uint ASB key map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add ARC/AZ key-map validation and a deterministic compressed-index/encrypted-ASB fixture.
- [x] Migrate the DAT/SPEED title-to-byte-key map as a standalone v2 dataset and load it in the modern image reader.
- [x] Add DAT/SPEED key-map validation and a deterministic RLE image fixture; encrypted payload and writer coverage remain pending.
- [x] Migrate the ARC/AZ/encrypted scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add ARC/AZ/encrypted scheme validation and a deterministic encrypted header/index/entry fixture for explicit content keys.
- [x] Port system.arc/sysenv content-key derivation for the legacy `Default` ARC/AZ/encrypted scheme.
- [x] Migrate the PKZ title-to-byte-key map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add PKZ key-map validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the PBZ ArcKey/ScriptKey scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add PBZ key-map validation and a deterministic encrypted index/entry fixture; script-specific secondary decryption remains pending.
- [x] Migrate the KCAP title-to-password map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add KCAP password-map validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the ARC/AI5WIN structured scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add ARC/AI5WIN scheme validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the NPA title-to-encryption-scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add NPA scheme validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the PSB/EMOTE candidate key list as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add PSB/EMOTE key-list validation and a deterministic listing/extraction fixture.
- [x] Migrate the AM/Leaf decrypt table as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add AM/Leaf table-shape validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the LPK scheme and title/file-key maps as a standalone v2 dataset and load them in the modern archive reader.
- [x] Add LPK scheme/file-key validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the GYU numeric/string nested key maps as a standalone v2 dataset and load them in the modern image reader.
- [x] Add GYU nested-map validation and a deterministic encrypted 24bpp image fixture.
- [x] Migrate the YPF title-to-scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add YPF scheme validation and a deterministic encrypted directory/entry fixture; Snappy entries remain explicitly unsupported.
- [x] Migrate the ARC/Tactics/2 title-to-password map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add ARC/Tactics/2 scheme validation and a deterministic encrypted archive/entry fixture.
- [x] Migrate the ARC/RPM title-to-encryption-scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add ARC/RPM scheme validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the DATA/Csystem title-to-header-size map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add DATA/Csystem scheme validation and a deterministic index/entry fixture.
- [x] Migrate the AVC scheme array as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add AVC scheme validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the DPK scheme array as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add DPK scheme validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the PAK/AGSI nested title/archive DES key map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add PAK/AGSI nested-map validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the PAK/LEAF title-to-byte key map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add PAK/LEAF key validation and a deterministic encrypted index/entry fixture.
- [x] Migrate the IKURA/GDL title-to-secret map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add IKURA secret-map validation (18 secrets, 2048 bytes each) and a deterministic encrypted script fixture.
- [x] Migrate the FSB5 Vorbis header map as a standalone v2 dataset and load it in the modern audio reader.
- [x] Add FSB5 header/patch validation and deterministic migrated-header lookup coverage.
- [x] Migrate the CPZ title-to-scheme map as a standalone v2 dataset and load it in the modern archive reader.
- [x] Add CPZ scheme validation and deterministic migrated-scheme lookup coverage; full archive fixture coverage remains a follow-up.
- [x] Migrate the ARCG signature-to-passkey map as a standalone v2 dataset and load it in the modern ARCG reader.
- [x] Add ARCG key-map validation and a deterministic inline-index/listing fixture.
- [x] Migrate the MGPK title-to-byte-key map as a standalone v2 dataset and load it in the modern MGPK reader.
- [x] Add MGPK key-map validation and a deterministic encrypted entry/LZF fixture.
- [x] Migrate the RCT title-to-password map as a standalone v2 dataset and load it in the modern RCT reader.
- [x] Add RCT password-map validation and a deterministic encrypted RGB fixture.
- [x] Migrate the MCG title-to-byte-key map as a standalone v2 dataset and load it in the modern MCG reader.
- [x] Add MCG key-map validation and a deterministic encrypted v101 RGB fixture.
- [ ] Port NOA ERISA and SimpleCrypt variants before claiming complete encrypted-format coverage.
- [ ] Remove each modern build dependency on `Formats.dat` once its data is covered by v2.

The current SchemeMap inventory and the ZIP migration boundary are described in [LEGACY_SCHEME_INVENTORY.md](LEGACY_SCHEME_INVENTORY.md). The inventory command intentionally records member names and types only; it does not expose key values.

## Completion Gates

- [x] CI validates every v2 JSON file, manifest reference, digest, duplicate ID, and semantic constraint.
- [ ] CI verifies deterministic bundle generation.
- [ ] Modern applications load only v2 data and verify updates before use.
- [ ] The legacy binary database has no remaining production reader in the modern solution.
