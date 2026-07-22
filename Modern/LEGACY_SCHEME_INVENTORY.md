# Legacy Scheme Inventory

The trusted `Formats.dat` source (database version `148`) contains `77` serialized `SchemeMap` entries representing `64` legacy `ResourceScheme` types. The offline migration command records each format tag, serialized type name, and member names without deserializing CLR objects or exposing key values:

```sh
dotnet run --project Modern/GARbro.LegacyDataMigration/GARbro.LegacyDataMigration.csproj --configuration Release -- \
  export-scheme-inventory ArcFormats/Resources/Formats.dat inventory.json
```

The first candidate for a non-XP3 data slice is ZIP:

| Tag | Legacy type | Members |
| --- | --- | --- |
| `ZIP` | `GameRes.Formats.PkWare.ZipScheme` | `KnownKeys` |

`KnownKeys` is serialized as a string-to-string dictionary. The modern ZIP implementation currently prompts for a password and does not consult this map, so exporting it before defining lookup behavior would add data without changing runtime behavior. The next slice should first define whether keys are selected by executable/title, archive name, or explicit user selection, then add a validator and an encrypted ZIP fixture.

Other simple-looking records such as `PAK/MORNING.DefaultKey` and `GAL.KnownKeys` belong to format implementations that are not yet included in the modern solution; they remain inventory-only until those ports are in scope.
