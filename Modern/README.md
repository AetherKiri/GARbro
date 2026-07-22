# Modern cross-platform port

`GARbro.Modern.sln` is the .NET 10 cross-platform port of GARbro. The legacy .NET Framework solution remains in the repository as the compatibility reference while format support moves to this solution incrementally.

## Supported today

- Windows, macOS, and Linux builds through .NET 10.
- Avalonia desktop browser for folders, ZIP archives, and XP3 archives.
- Desktop previews for supported images, text files, and WAV, OGG, and MP3 audio.
- Desktop extraction for one selected archive entry or the complete archive.
- CLI listing and safe extraction for ZIP and XP3.
- Standard unencrypted XP3 archives, plus explicitly selected migrated generic encryption schemes.
- PNG, JPEG, and BMP regression coverage through the platform-neutral image layer.

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

Each profile declares a name, one of `CxEncryption`, `SenrenCxCrypt`, `CabbageCxCrypt`, `NanaCxCrypt`, `RiddleCxCrypt`, `HxCrypt`, or `HxCryptLite`, and the required `cx` data. No type names are deserialized from profile input.

## Current limits

The modern runtime intentionally rejects the legacy `Formats.dat` BinaryFormatter database. Game-specific profile data is therefore not bundled yet, but Hx, Senren, and related helper implementations are available through explicit JSON profiles. See [PORTING_STATUS.md](PORTING_STATUS.md) for the full status.
