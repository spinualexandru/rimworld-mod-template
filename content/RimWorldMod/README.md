# TextModName

TextDescription

A RimWorld 1.7 mod by TextAuthor (`TextPackageId`).

## Layout

```
TextDirName/
├── About/
<!--#if (harmony) -->
│   └── About.xml            Name, packageId, supported versions, dependencies
<!--#else -->
│   └── About.xml            Name, packageId, supported versions
<!--#endif -->
<!--#if (xml) -->
├── Defs/                    XML definitions: what things ARE
│   └── ThingDefs/ExampleItem.xml
├── Patches/                 XML patches that edit other mods' (or vanilla) defs
├── Textures/                PNGs referenced by texPath, without the extension
<!--#endif -->
<!--#if (useSettings) -->
├── Languages/English/Keyed/ Text used from C# via "Key".Translate()
<!--#endif -->
<!--#if (useCsharp) -->
├── Source/                  C#: what things DO (not loaded by the game)
│   ├── RimWorldMod.slnx
│   └── RimWorldMod/
│       ├── ModEntry.cs      Runs when the mod loads
<!--#if (useSettings) -->
│       ├── Settings.cs      Options > Mod settings
<!--#endif -->
<!--#if (xmlAndCsharp) -->
│       ├── Comps/           C# behaviour attached to defs
<!--#endif -->
<!--#if (harmony) -->
│       ├── Patches/         Harmony patches: change what vanilla code does
<!--#endif -->
│       └── RimWorldMod.csproj
├── 1.7/Assemblies/          Build output; RimWorld loads DLLs from here
<!--#endif -->
└── scripts/symlink.cs       Links this folder into RimWorld's Mods folder
```

RimWorld loads the `1.7/` folder, then a `Common/` folder if there is one, then the root.
To support more than one game version later, add a `LoadFolders.xml`
([wiki](https://rimworldwiki.com/wiki/Modding_Tutorials/Mod_Folder_Structure)).

## Getting started

<!--#if (useCsharp) -->
1. You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
   Any editor works; for IDEs you need Visual Studio 2026 or Rider 2026+.
2. Build the assembly:

   ```sh
   dotnet build Source
   ```

   The DLL lands in `1.7/Assemblies/`. Rebuild after every C# change and restart the game.
3. Link the mod into your game (see below), start RimWorld and enable **TextModName** in the Mods menu.
<!--#if (harmony) -->
   Subscribe to [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) too, and keep it above this mod.
<!--#endif -->
<!--#else -->
1. Link the mod into your game (see below).
2. Start RimWorld and enable **TextModName** in the Mods menu.
3. Edit the XML and restart the game to see changes.
<!--#endif -->
<!--#if (xml) -->

Try it: build a crafting spot and add a "Make example item" bill.
With Development mode on (Options), the debug actions menu can also spawn the item directly.
<!--#endif -->

## Linking into RimWorld

A link makes the game load the mod straight from this folder, so there's nothing to copy after a change.

```sh
dotnet scripts/symlink.cs           # link
dotnet scripts/symlink.cs status    # where is RimWorld, is the link there?
dotnet scripts/symlink.cs unlink    # remove the link (never touches your files)
dotnet scripts/symlink.cs help      # all options
```

The script is a single C# file that needs only the .NET 10 SDK; its first run takes a few seconds to compile.
It looks for RimWorld in the usual Steam and GOG locations. If yours is elsewhere, pass
`--game-dir "/path/to/RimWorld"` or set the `RIMWORLD_DIR` environment variable.
On Windows it creates a directory junction, which needs no admin rights.

Without .NET, create the link by hand from this folder:

```sh
ln -s "$PWD" "/path/to/RimWorld/Mods/TextDirName"           # Linux, macOS
mklink /J "C:\path\to\RimWorld\Mods\TextDirName" "%CD%"     # Windows (cmd)
```

## Debugging

- The game log is `Logs/Player.log` inside RimWorld's data folder:
  - Windows: `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs`
  - Linux: `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Logs`
- Turn on Development mode in Options to get the debug log window and debug actions.
<!--#if (useCsharp) -->
- Since 1.7 the game runs on .NET 10, so you can attach your IDE's debugger to the running `RimWorld`
  process. Visual Studio may refuse because the runtime isn't signed; set the environment variable
  `VSDebugger_ValidateDotnetDebugLibSignatures=0` before starting it.
<!--#endif -->
<!--#if (harmony) -->
- Harmony 3 checks the types of `__instance`, `__result` and parameters; a mismatch rejects the patch
  with an error in the log.
<!--#endif -->

## Publishing

1. Add `About/Preview.png` (640x360 works well). It's the picture in the mod list and on the Workshop.
2. Bump `modVersion` in `About/About.xml`.
<!--#if (useCsharp) -->
3. Build in Release: `dotnet build Source -c Release`.
4. With Development mode on, open the Mods menu, select the mod and click **Upload**.
<!--#else -->
3. With Development mode on, open the Mods menu, select the mod and click **Upload**.
<!--#endif -->

## Resources

- [RimWorld wiki: modding tutorials](https://rimworldwiki.com/wiki/Modding_Tutorials)
- [RimWorld 1.7 mod updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.7_Mod_Updates)
<!--#if (useCsharp) -->
- [RimRef](https://github.com/krafs/RimRef): the game reference assemblies this project builds against
<!--#endif -->
<!--#if (harmony) -->
- [Harmony documentation](https://harmony.pardeike.net/)
<!--#endif -->
- The vanilla defs in `Data/` inside your RimWorld folder are the best examples there are.
