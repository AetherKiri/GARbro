# Modern port status

## Available now

- .NET 10 core builds on macOS, Windows, and Linux through CI.
- Standard BMP, JPEG, and PNG read/write paths have regression coverage.
- ZIP discovery, listing, and safe extraction are available from the CLI.
- KiriKiri XP3 creation, listing, and extraction are available for standard and explicitly selected encrypted schemes, including compressed index and content streams.
- Modern builds use platform APIs for memory mapping and register legacy code pages.

## Explicit temporary limits

- The legacy `Formats.dat` BinaryFormatter database is rejected by the modern runtime. The first safe v2 dataset is the bundled XP3 title registry; the remaining format-specific records still require explicit migration adapters.
- TIFF write support and the GX4 BinaryFormatter index reader are pending dedicated safe adapters.
- Hx, Senren, and related Cx XP3 helpers are available through bundled, checksum-verified v2 profiles or an external `--xp3-profile <file> --xp3-scheme <name>`. Migrated executable bindings are available for automatic XP3 detection; real archive fixture coverage is still pending.
- ZIP now consumes its seven migrated title-to-password entries before falling back to the interactive password prompt; an encrypted ZIP fixture is still pending.
- Other archive formats remain in the legacy solution while their WPF option controls are replaced with data-driven option definitions.

## Next slice

Migrate parameterized XP3 profiles into v2 data, then create the offline legacy exporter and add the next archive-format slice. The detailed work order lives in [DATABASE_MIGRATION.md](DATABASE_MIGRATION.md).
