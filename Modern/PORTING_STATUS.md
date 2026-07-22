# Modern port status

## Available now

- .NET 10 core builds on macOS, Windows, and Linux through CI.
- Standard BMP, JPEG, and PNG read/write paths have regression coverage.
- ZIP discovery, listing, and safe extraction are available from the CLI.
- TopCat TCD3 discovery, listing, and extraction are available through the modern port, with its three migrated key records loaded from v2 data.
- Morning PAK discovery, listing, and extraction are available through the modern port with its migrated 512-byte default key.
- Moonhir FPK discovery, listing, and extraction are available through the modern port with its migrated 28-key candidate list.
- KiriKiri XP3 creation, listing, and extraction are available for standard and explicitly selected encrypted schemes, including compressed index and content streams.
- Modern builds use platform APIs for memory mapping and register legacy code pages.

## Explicit temporary limits

- The legacy `Formats.dat` BinaryFormatter database is rejected by the modern runtime. XP3, ZIP, TCD, Morning PAK, and Moonhir FPK records currently use checksum-verified v2 data; remaining format-specific records still require explicit migration adapters.
- TIFF write support and the GX4 BinaryFormatter index reader are pending dedicated safe adapters.
- Hx, Senren, and related Cx XP3 helpers are available through bundled, checksum-verified v2 profiles or an external `--xp3-profile <file> --xp3-scheme <name>`. Migrated executable bindings are available for automatic XP3 detection; real archive fixture coverage is still pending.
- ZIP now consumes its seven migrated title-to-password entries before falling back to the interactive password prompt; an encrypted ZipCrypto fixture covers that path.
- Other archive formats remain in the legacy solution while their WPF option controls are replaced with data-driven option definitions.

## Next slice

Continue the next archive-format slice from the offline inventory, prioritizing formats already included in the modern build. The detailed work order lives in [DATABASE_MIGRATION.md](DATABASE_MIGRATION.md).
