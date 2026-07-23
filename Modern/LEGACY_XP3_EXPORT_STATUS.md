# Legacy XP3 Export Status

This is the reviewed migration inventory for the checked-in `ArcFormats/Resources/Formats.dat` database. The current source has database version `148`, `18,510` NRBF records, `453` XP3 known-scheme bindings, and `1,129` executable-to-title game bindings.

The offline `GARbro.LegacyDataMigration` tool safely reads NRBF records and exports the parameterized XP3 algorithms and simple hash-based algorithms already supported by the modern runtime:

- `cx-encryption`: 113 profiles
- `senren-cx`: 1 profile
- `cabbage-cx`: 1 profile
- `nana-cx`: 4 profiles
- `riddle-cx`: 2 profiles
- `hx`: 21 profiles, including archive-specific index keys
- `hx-lite`: 2 profiles
- simple algorithms: 228 source records, producing 227 unique profiles across 26 algorithm classes

This is `371` unique exported profiles (one case-insensitive duplicate title was merged). The remaining `81` profiles are deliberately retained in the conversion report until their parameter schemas and fixture coverage exist:

- `AkabeiCrypt` (19), `ChainReactionCrypt` (3), `ChocolatCrypt` (1), `HachukanoCrypt` (1), `MadoCrypt` (7), `NekoWorksCrypt` (9)
- `NinkiSeiyuuCrypt` (1), `PuCaCrypt` (1), `PureMoreCrypt` (2), `RhapsodyCrypt` (1), `SisMikoCrypt` (1)
- `SmileCrypt` (5), `SmxCrypt` (6), `StripeCrypt` (1), `XanaduCrypt` (1), `XorCrypt` (22)

To generate a report against a trusted local database:

```sh
dotnet run --project Modern/GARbro.LegacyDataMigration/GARbro.LegacyDataMigration.csproj --configuration Release -- \
  export-xp3 ArcFormats/Resources/Formats.dat profiles.json report.json
```

The command refuses to overwrite output files. The generated profile document is bundled under `Modern/GameData/v2/xp3/profiles.json` after schema and export-count validation. Simple algorithm profiles now load through the modern factory map; parameterized profiles and archive fixture coverage still require follow-up before being treated as release-complete data.

The legacy game map was merged into `Modern/GameData/v2/xp3/title-registry.json` after checking all 236 existing executable keys for conflicts. No conflicting values were found; 36 migrated profile titles now have executable bindings and can be resolved automatically by XP3 detection.
