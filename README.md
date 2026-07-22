# GARbro

GARbro is a resource browser and extractor for visual novels.

## Project status

This repository now has two development paths:

- **Legacy Windows application**: the established .NET Framework/WPF application and the reference for broad format compatibility.
- **Modern cross-platform application**: the active .NET 10 port for Windows, macOS, and Linux. It uses Avalonia for the desktop shell and is being migrated format by format.

The modern port is currently focused on reliable archive access rather than full legacy format parity. Its first supported archive formats are ZIP and KiriKiri XP3. In the desktop browser, selecting a supported image, text file, or WAV, OGG, or MP3 entry opens its preview. XP3 contents are shown as a navigable directory tree, and audio plays inside the application. Archives can be extracted either entry by entry or in full; existing destination files are skipped.

## Modern quick start

Install the .NET 10 SDK, then build and test the cross-platform solution:

```sh
dotnet build GARbro.Modern.sln --configuration Release
dotnet test GARbro.Modern.sln --configuration Release
```

Run the desktop browser:

```sh
dotnet run --project Modern/GARbro.Desktop/GARbro.Desktop.csproj
```

Run the command-line tool:

```sh
dotnet run --project Modern/GARbro.Cli -- formats
dotnet run --project Modern/GARbro.Cli -- list archive.zip
dotnet run --project Modern/GARbro.Cli -- extract archive.zip --output extracted
```

## XP3 support

The modern runtime can create, list, preview, and extract standard XP3 archives, including compressed index and content streams. It also supports an explicit set of migrated generic encryption schemes.

```sh
dotnet run --project Modern/GARbro.Cli -- xp3-schemes
dotnet run --project Modern/GARbro.Cli -- list encrypted.xp3 --xp3-scheme FateCrypt
dotnet run --project Modern/GARbro.Cli -- extract encrypted.xp3 --output extracted --xp3-scheme FateCrypt
```

The desktop application prompts for an XP3 scheme when an encrypted archive is opened. Use `Detect automatically` to try the supported schemes against the archive contents; if no result is reliable, choose a scheme manually. Load a JSON profile through `Profiles` when needed; its schemes appear in that dialog.

Profiles may include a `title` field. It is shown as the game name in the scheme selector, while `name` remains the stable identifier used by the CLI.

Hx and Senren family variants can use safe JSON profiles with the CLI:

```sh
dotnet run --project Modern/GARbro.Cli -- list encrypted.xp3 --xp3-profile game-profiles.json --xp3-scheme my-game
```

The port deliberately does not load the legacy `Formats.dat` BinaryFormatter database. A checksum-verified v2 XP3 title registry and parameterized profile dataset are bundled under `Modern/GameData/v2`; archive fixture coverage and automatic game-to-profile bindings are still being migrated. See [Modern/DATABASE_MIGRATION.md](Modern/DATABASE_MIGRATION.md) for the staged plan.

See [Modern/PORTING_STATUS.md](Modern/PORTING_STATUS.md) for the current format and platform boundary.

## Legacy application

The legacy application requires .NET Framework 4.6 or newer and remains the recommended option when a game relies on a format not yet migrated to the modern runtime.

[Supported formats](https://morkt.github.io/GARbro/supported.html) | [Latest legacy release](https://github.com/morkt/GARbro/releases)

## License and credits

Written by [morkt](https://github.com/morkt/GARbro) under the [MIT License](LICENSE).

Korean translation by [mireado](https://github.com/mireado) and [overworks](https://github.com/overworks).

Simplified Chinese translation by [elasticblitz](https://github.com/elasticblitz), [PeratX](https://github.com/PeratX), and [taroxd](https://github.com/taroxd).

Japanese translation by [haniwa55](https://github.com/haniwa55).
