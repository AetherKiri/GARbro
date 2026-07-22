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
- [ ] Remove each modern build dependency on `Formats.dat` once its data is covered by v2.

The current SchemeMap inventory and the ZIP migration boundary are described in [LEGACY_SCHEME_INVENTORY.md](LEGACY_SCHEME_INVENTORY.md). The inventory command intentionally records member names and types only; it does not expose key values.

## Completion Gates

- [x] CI validates every v2 JSON file, manifest reference, digest, duplicate ID, and semantic constraint.
- [ ] CI verifies deterministic bundle generation.
- [ ] Modern applications load only v2 data and verify updates before use.
- [ ] The legacy binary database has no remaining production reader in the modern solution.
