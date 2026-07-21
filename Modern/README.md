# Modern port

`GARbro.Modern.sln` is the .NET 10 cross-platform port. The legacy .NET Framework solution remains unchanged while this solution grows toward feature parity.

The current milestone contains the modernized `GameRes` core, its platform-neutral bitmap compatibility surface, ZIP and standard unencrypted XP3 archive support, and the `garbro` CLI. Run it with:

```sh
dotnet run --project Modern/GARbro.Cli -- formats
dotnet run --project Modern/GARbro.Cli -- list archive.zip
dotnet run --project Modern/GARbro.Cli -- extract archive.zip --output extracted
```

Run the first regression suite with:

```sh
dotnet test GARbro.Modern.sln
```
