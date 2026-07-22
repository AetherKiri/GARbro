# Legacy XP3 Export Status

This is the reviewed migration inventory for the checked-in `ArcFormats/Resources/Formats.dat` database. The current source has database version `148`, `18,510` NRBF records, and `453` XP3 known-scheme bindings.

The offline `GARbro.LegacyDataMigration` tool safely reads NRBF records and exports the seven parameterized XP3 algorithms already supported by the modern runtime:

- `cx-encryption`: 113 profiles
- `senren-cx`: 1 profile
- `cabbage-cx`: 1 profile
- `nana-cx`: 4 profiles
- `riddle-cx`: 2 profiles
- `hx`: 21 profiles, including archive-specific index keys
- `hx-lite`: 2 profiles

This is `144` exported profiles. The remaining `309` profiles are deliberately retained in the conversion report until their modern algorithms and fixture coverage exist:

- `AkabeiCrypt` (19), `AlteredPinkCrypt` (1), `AppliqueCrypt` (15), `ChainReactionCrypt` (3), `ChocolatCrypt` (1)
- `DameganeCrypt` (1), `DieselmineCrypt` (15), `ExaCrypt` (1), `FateCrypt` (1), `FestivalCrypt` (1)
- `FlyingShineCrypt` (28), `HachukanoCrypt` (1), `HaikuoCrypt` (1), `HashCrypt` (101), `HibikiCrypt` (1)
- `HighRunningCrypt` (2), `HybridCrypt` (2), `KissCrypt` (1), `MadoCrypt` (7), `MizukakeCrypt` (2)
- `NatsupochiCrypt` (10), `NekoWorksCrypt` (9), `NephriteCrypt` (9), `NinkiSeiyuuCrypt` (1), `NoCrypt` (5)
- `OkibaCrypt` (1), `PinPointCrypt` (3), `PoringSoftCrypt` (13), `PuCaCrypt` (1), `PureMoreCrypt` (2)
- `RhapsodyCrypt` (1), `SeitenCrypt` (1), `SisMikoCrypt` (1), `SmileCrypt` (5), `SmxCrypt` (6)
- `SourireCrypt` (10), `StripeCrypt` (1), `SyangrilaSmartCrypt` (1), `TokidokiCrypt` (1), `XanaduCrypt` (1)
- `XorCrypt` (22), `YuzuCrypt` (1)

To generate a report against a trusted local database:

```sh
dotnet run --project Modern/GARbro.LegacyDataMigration/GARbro.LegacyDataMigration.csproj --configuration Release -- \
  export-xp3 ArcFormats/Resources/Formats.dat profiles.json report.json
```

The command refuses to overwrite output files. The generated profile document is bundled under `Modern/GameData/v2/xp3/profiles.json` after schema and export-count validation. It still requires archive fixture coverage and human review before being treated as release-complete data.
