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

All `452` unique XP3 profiles are now exported (one case-insensitive duplicate title was merged). The conversion report has no skipped profiles. The exported set includes the list-file algorithms and PureMore/Rhapsody file-name mapping parameters; archive fixture coverage remains a separate release gate.

To generate a report against a trusted local database:

```sh
dotnet run --project Modern/GARbro.LegacyDataMigration/GARbro.LegacyDataMigration.csproj --configuration Release -- \
  export-xp3 ArcFormats/Resources/Formats.dat profiles.json report.json
```

The command refuses to overwrite output files. The generated profile document is bundled under `Modern/GameData/v2/xp3/profiles.json` after schema and export-count validation. Every source profile now has a data-only modern adapter; real archive fixture coverage and human review remain before release-complete claims.

The legacy game map was merged into `Modern/GameData/v2/xp3/title-registry.json` after checking all 236 existing executable keys for conflicts. No conflicting values were found; 36 migrated profile titles now have executable bindings and can be resolved automatically by XP3 detection.
