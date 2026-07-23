# Modern port status

## Available now

- .NET 10 core builds on macOS, Windows, and Linux through CI.
- Standard BMP, JPEG, and PNG read/write paths have regression coverage.
- ZIP discovery, listing, and safe extraction are available from the CLI.
- TopCat TCD3 discovery, listing, and extraction are available through the modern port, with its three migrated key records loaded from v2 data.
- Morning PAK discovery, listing, and extraction are available through the modern port with its migrated 512-byte default key.
- Moonhir FPK discovery, listing, and extraction are available through the modern port with its migrated 28-key candidate list.
- GameSystem CMP discovery, listing, and extraction are available through the modern port with its two migrated title keys.
- Yatagarasu PKG/2 discovery, listing, and extraction are available through the modern port with its migrated eight-word title key.
- Family Adv System CSAF discovery, listing, and extraction are available through the modern port with its migrated title key map.
- Marble MBL discovery, listing, and title-keyed script extraction are available through the modern port with its migrated 58-entry key map.
- NitroPlus NPK2 discovery, listing, and AES entry extraction are available through the modern port with its migrated four-key map.
- TamamoSystem PCK discovery, listing, and Blowfish entry extraction are available through the modern port with its migrated four-key map.
- NS2 discovery, listing, and extraction are available through the modern port with its migrated six-key map; encrypted lookup uses the mapped title and the platform MD5 API.
- NScripter NSA discovery, listing, and encrypted extraction are available for uncompressed entries with its migrated ten-key map.
- NSystem FJSYS discovery, listing, and `.msd` extraction are available through the modern port with its migrated 22-entry password map.
- Frontwing INT discovery, listing, and Blowfish entry extraction are available through the modern port with its migrated 24-entry structured key map.
- Entis NOA discovery, listing, Raw extraction, and BSHF password extraction are available through the modern port with its migrated 26-title nested map.
- LiveMaker GAL metadata and uncompressed, unshuffled first-layer pixel extraction are available through the modern port with its migrated three-title key map.
- Crowd CRZ encrypted image metadata and LZSS pixel extraction are available through the modern port with its migrated two-identifier key map.
- ACTGS DAT/CG archive discovery, encrypted index lookup, and entry extraction are available through the modern port with its migrated six-key list.
- BlackRainbow ADS discovery, encrypted index lookup, and entry extraction are available through the modern port with its migrated two-title key map.
- Tanaka ARCG discovery, inline-index listing, and entry extraction are available through the modern port with its migrated signature-to-passkey map.
- MangaGamer MGPK discovery, title-keyed entry decryption, and LZF text extraction are available through the modern port with its migrated four-title key map.
- Majiro RCT encrypted RGB image reading is available through the modern port with its migrated 33-title password map.
- F&C MCG encrypted v101 RGB image reading is available through the modern port with its migrated 24-title byte-key map.
- Cyberworks TINK encrypted OGG header decoding is available through the modern port with its migrated two-signature key map.
- Unity BIN/IDX index and entry decryption are available through the modern port with its migrated AES key/IV map.
- AZ ARC index and ASB script extraction are available through the modern port with its migrated four-title key map.
- AZ encrypted ARC header/index and explicit-content-key entry extraction are available through the modern port with its migrated two-scheme map.
- Studio Jikkenshitsu DAT/SPEED image metadata and RLE pixel extraction are available through the modern port with its migrated five-title key map.
- KiriKiri XP3 creation, listing, and extraction are available for standard and explicitly selected encrypted schemes, including compressed index and content streams.
- Modern builds use platform APIs for memory mapping and register legacy code pages.

## Explicit temporary limits

- The legacy `Formats.dat` BinaryFormatter database is rejected by the modern runtime. XP3, ZIP, TCD, Morning PAK, Moonhir FPK, CMP, PKG/2, CSAF, MBL, NPK, PCK, NS2, NSA, FJSYS, INT, ARCG, MGPK, RCT, MCG, TINK, BIN/IDX, ARC/AZ, ARC/AZ/encrypted, PKZ, PBZ, KCAP, NOA, GAL, CRZ, ACTGS, ADS, and DAT/SPEED records currently use checksum-verified v2 data; remaining format-specific records still require explicit migration adapters.
- TINK header decryption is covered; a full Vorbis payload fixture and any writer remain outside this migration slice.
- MCG currently supports encrypted v100/v101 24bpp RGB reads; v200, indexed/16bpp variants, and writing remain unavailable in the modern port.
- RCT currently supports encrypted RGB reads only; writer, overlay frames, masks, and other image variants remain unavailable in the modern port.
- ARCG inline indexes use the migrated v2 key map. External `.bmx` indexes that require deriving a passkey from a Windows volume serial are not guaranteed on non-Windows platforms.
- INT archive creation and interactive password-entry UI are not included in the modern port.
- NOA ERISA and SimpleCrypt variants remain unsupported; those entries require the remaining Entis decoder slices.
- GAL compressed, JPEG, shuffled/encrypted, layered, and alpha-layer variants remain unsupported until their cross-platform decoder paths are ported.
- DAT/SPEED encrypted pixel-stream fixtures and writer coverage remain pending; the migrated reader currently covers metadata, title-key resolution, and unencrypted RLE data.
- NSA compressed entries using SPB, LZSS, or NBZ are rejected until their decompression paths are ported.
- PBZ `.scr` entries still require the legacy script-specific secondary decryption path; ordinary encrypted entries are supported.
- TIFF write support and the GX4 BinaryFormatter index reader are pending dedicated safe adapters.
- Hx, Senren, and related Cx XP3 helpers are available through bundled, checksum-verified v2 profiles or an external `--xp3-profile <file> --xp3-scheme <name>`. Migrated executable bindings are available for automatic XP3 detection; real archive fixture coverage is still pending.
- ZIP now consumes its seven migrated title-to-password entries before falling back to the interactive password prompt; an encrypted ZipCrypto fixture covers that path.
- Other archive formats remain in the legacy solution while their WPF option controls are replaced with data-driven option definitions.

## Next slice

Continue the next archive-format slice from the offline inventory, prioritizing formats already included in the modern build. The detailed work order lives in [DATABASE_MIGRATION.md](DATABASE_MIGRATION.md).
