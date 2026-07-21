# Modern port status

## Available now

- .NET 10 core builds on macOS, Windows, and Linux through CI.
- Standard BMP, JPEG, and PNG read/write paths have regression coverage.
- ZIP discovery, listing, and safe extraction are available from the CLI.
- KiriKiri XP3 creation, listing, and extraction are available for standard and explicitly selected encrypted schemes, including compressed index and content streams.
- Modern builds use platform APIs for memory mapping and register legacy code pages.

## Explicit temporary limits

- The legacy `Formats.dat` BinaryFormatter database is rejected by the modern runtime until the safe v2 database converter lands.
- TIFF write support and the GX4 BinaryFormatter index reader are pending dedicated safe adapters.
- Hx, Senren, and related Cx XP3 helpers are available through a safe JSON profile supplied with `--xp3-profile <file> --xp3-scheme <name>`. Game-specific profile data has not yet been bundled because the legacy `Formats.dat` database remains intentionally unreadable.
- Other archive formats remain in the legacy solution while their WPF option controls are replaced with data-driven option definitions.

## Next slice

Migrate the archive option schema, then extend XP3 with its game-specific encryption helpers before adding the next archive-format slice.
