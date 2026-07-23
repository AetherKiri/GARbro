# Modern cross-platform port

`GARbro.Modern.sln` is the .NET 10 cross-platform port of GARbro. The legacy .NET Framework solution remains in the repository as the compatibility reference while format support moves to this solution incrementally.

## Supported today

- Windows, macOS, and Linux builds through .NET 10.
- Avalonia desktop browser for folders, ZIP, XP3, TCD, Morning PAK, Moonhir FPK, CMP, PKG/2, CSAF, MBL, NPK, PCK, NS2, uncompressed NSA, FJSYS, INT, ARCG, MGPK, ACTGS, ADS, NOA, and explicit-key ARC/AZ/encrypted archives, plus GAL, CRZ, RCT, MCG, and DAT/SPEED image reading.
- Desktop previews for supported images, text files, and WAV, OGG, and MP3 audio.
- Desktop extraction for one selected archive entry or the complete archive.
- CLI listing and safe extraction for ZIP, XP3, TCD, Morning PAK, Moonhir FPK, CMP, PKG/2, CSAF, MBL, NPK, PCK, NS2, uncompressed NSA, FJSYS, INT, ARCG, MGPK, ACTGS, ADS, and NOA archives.
- Standard unencrypted XP3 archives, plus explicitly selected migrated generic encryption schemes.
- PNG, JPEG, BMP, GAL, CRZ, DAT/SPEED, encrypted RGB RCT, and encrypted v101 MCG regression coverage through the platform-neutral image layer.

## Run

```sh
dotnet build GARbro.Modern.sln --configuration Release
dotnet test GARbro.Modern.sln --configuration Release
dotnet run --project Modern/GARbro.Desktop/GARbro.Desktop.csproj
```

The CLI supports inspection and extraction:

```sh
dotnet run --project Modern/GARbro.Cli -- formats
dotnet run --project Modern/GARbro.Cli -- list archive.zip
dotnet run --project Modern/GARbro.Cli -- extract archive.zip --output extracted
```

## Desktop browser

Select an image, text, or audio entry to preview it. XP3 entries are presented
as a navigable directory tree. Audio plays in the desktop application through
the built-in cross-platform playback engine, with play/pause and stop controls.
Use `Extract selected` to save the current archive entry or `Extract all` to
save every file. Existing destination files are left untouched and reported as
skipped.

For encrypted XP3 archives, select a migrated scheme explicitly:

```sh
dotnet run --project Modern/GARbro.Cli -- xp3-schemes
dotnet run --project Modern/GARbro.Cli -- list encrypted.xp3 --xp3-scheme FateCrypt
dotnet run --project Modern/GARbro.Cli -- extract encrypted.xp3 --output extracted --xp3-scheme FateCrypt
```

The desktop browser prompts for a scheme when an encrypted XP3 archive is opened. Use `Detect automatically` to try the supported schemes against the archive contents; if no result is reliable, choose a scheme manually. Load a profile through the `Profiles` action when needed; its schemes appear in that dialog.

Profiles may include a `title` field. It is shown as the game name in the scheme selector, while `name` remains the stable identifier used by the CLI.

Hx and Senren family schemes use safe, data-only JSON profiles instead of the legacy binary database:

```sh
dotnet run --project Modern/GARbro.Cli -- list encrypted.xp3 --xp3-profile game-profiles.json --xp3-scheme my-game
```

The original JSON array remains accepted for compatibility. New profiles use a versioned document with an `id`, stable algorithm ID (`cx-encryption`, `senren-cx`, `cabbage-cx`, `nana-cx`, `riddle-cx`, `hx`, or `hx-lite`), and an algorithm-specific `parameters` object. No type names are deserialized from profile input, and fields for another algorithm are rejected.

## Current limits

The modern runtime intentionally rejects the legacy `Formats.dat` BinaryFormatter database. Its v2 data under `Modern/GameData/v2` now includes checksum-verified XP3, ZIP, TCD, Morning PAK, Moonhir FPK, CMP, PKG/2, CSAF, MBL, NPK, PCK, NS2, NSA, FJSYS, INT, ARCG, MGPK, RCT, MCG, TINK, BIN/IDX, ARC/AZ, ARC/AZ/encrypted, NOA, GAL, CRZ, ACTGS, ADS, and DAT/SPEED datasets alongside migrated executable bindings and parameterized Hx, Senren, and related profiles. See [DATABASE_MIGRATION.md](DATABASE_MIGRATION.md) for the execution checklist and [PORTING_STATUS.md](PORTING_STATUS.md) for the full status.
