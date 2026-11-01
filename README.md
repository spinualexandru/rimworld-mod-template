# RimWorld mod template

A `dotnet new` template for RimWorld 1.7 mods. Pick the layers your mod needs, and it links the mod
into your game so RimWorld loads it straight from your working folder.

```sh
dotnet new install RimWorldMod.Templates
dotnet new rimworld-mod -n MyMod
```

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0); C# mods for 1.7 need it anyway.
Until the package is on NuGet, install it from a clone: `dotnet new install path/to/content/RimWorldMod`.

In Visual Studio or Rider, the template shows up as **RimWorld Mod** in the New Project dialog, with
the options below as fields and checkboxes.

## Options

| Option | Default |
|---|---|
| `-n`, `--name` | Name of the project folder; the mod name is derived from it |
| `--mod-name` | Project name split into words: `my-cool-mod` and `MyCoolMod` both become `My Cool Mod` |
| `--mod-author` | Your user name |
| `--package-id` | `Author.ModName` |
| `--description` | `A RimWorld mod.` |
| `--xml` | `true` |
| `--csharp` | `true` |
| `--harmony` | `false` (turns on the C# layer as well) |
| `--settings` | `false` (only with C#) |
| `--link` | `true` |

`dotnet new rimworld-mod --help` lists them too. Bool options take `true`/`false`, and on their own mean
`true`: `--harmony --settings --xml false`.

The package ID must be letters, digits and dots, with at least one dot and at most 60 characters.
`dotnet new` can't validate input, so RimWorld complains about a bad one when it loads the mod.

## The three layers

- **XML Definitions** (`--xml`) declare what things *are*: items, weapons, buildings, recipes,
  research, stats. You get an example item with a texture and a crafting recipe, plus an XML patch
  example.
- **C# Assembly** (`--csharp`) declares what things *do*: comps, AI, events, UI, simulation logic.
  You get an SDK-style `net10.0` project that builds against
  [RimRef](https://github.com/krafs/RimRef) reference assemblies, so it builds without the game
  installed and outputs to `1.7/Assemblies/`.
  With XML too, the example item gets a C# comp, which shows how the two layers connect.
- **Harmony Patches** (`--harmony`) change what the game's existing code does, at runtime. It adds the
  C# layer, a `brrainz.harmony` dependency in `About.xml`, a compile-only Harmony 3 reference and an
  example postfix patch.

## Linking into the game

Every generated mod includes `scripts/symlink.cs`, a single-file C# program (`dotnet run app.cs`, new
in .NET 10). With `--link` on, `dotnet new` asks whether to run it right after creating the mod:

```
Template is configured to run the following action:
Description: Link the mod into RimWorld's Mods folder, so the game loads it straight from this folder.
Actual command: dotnet scripts/symlink.cs
Do you want to run this action [Y(yes)|N(no)]?
```

Answering no ends `dotnet new` with exit code 105, but the mod is created all the same.
You can run the script yourself at any time:

```sh
dotnet scripts/symlink.cs           # link
dotnet scripts/symlink.cs status
dotnet scripts/symlink.cs unlink    # only ever removes the link
```

It finds RimWorld through Steam (including extra library folders, Flatpak and Snap) and the usual GOG
paths, or uses `--game-dir <path>` or `$RIMWORLD_DIR`, and otherwise asks. On Windows it creates a
junction, which needs no admin rights. It never deletes or replaces a real folder, and leaves links to
other folders alone unless you pass `--force`.

In scripts and CI, pass `--link false` or `--allow-scripts yes`: without a terminal, `dotnet new` keeps
repeating its question.

## How the template works

[`content/RimWorldMod`](https://github.com/spinualexandru/rimworld-mod-template/tree/main/content/RimWorldMod) is a complete mod with every layer turned on.
[`.template.config/template.json`](https://github.com/spinualexandru/rimworld-mod-template/blob/main/content/RimWorldMod/.template.config/template.json) defines the options
and what they do:

- Files that belong to a layer you didn't pick are left out (`sources.modifiers`).
- Optional parts of a file sit between `#if (harmony)` … `#endif` in C#, and between
  `<!--#if (harmony) -->` … `<!--#endif -->` in XML and Markdown.
- Placeholders get replaced: `RimWorldMod` (also in file names) becomes the C# identifier,
  `XmlModName`/`TextModName`/`CSharpModName` become the mod name escaped for XML, as-is, or for a C# string,
  and likewise for the author, package ID and description.
- `scripts/` is copied as-is.

[`dotnetcli.host.json`](https://github.com/spinualexandru/rimworld-mod-template/blob/main/content/RimWorldMod/.template.config/dotnetcli.host.json) sets the command-line
option names; [`ide.host.json`](https://github.com/spinualexandru/rimworld-mod-template/blob/main/content/RimWorldMod/.template.config/ide.host.json) makes the options
visible in Visual Studio.

## Development

```sh
dotnet test tests/RimWorldMod.Templates.Tests                          # everything (needs NuGet access)
dotnet test tests/RimWorldMod.Templates.Tests --filter Category!=Build # skip compiling the C# variants
dotnet pack -o out                                                     # build the template package
```

The tests pack the template and install it into a throwaway template cache, so they don't touch your
own `dotnet new` setup. They check the files of every layer combination, escaping and naming, and
compile each C# variant with warnings as errors. They also run the link script against fake RimWorld
folders, so your real game is never touched.

To try the template by hand:

```sh
dotnet new install ./content/RimWorldMod --force   # after each change
dotnet new rimworld-mod -n TestMod -o /tmp/TestMod
dotnet new uninstall ./content/RimWorldMod
```

## RimWorld 1.7 notes

1.7 moved from Unity to Ludeon's own Waylith engine on .NET 10. For modders, that means:

- Target `net10.0`. You need Visual Studio 2026 or Rider 2026+ if you use an IDE.
- Reference `Krafs.Rimworld.Ref` 1.7 instead of Unity DLLs. `using UnityEngine;` still works through the
  game's compatibility stubs.
- Harmony 3 is required. It checks the types of `__instance`, `__result` and parameters strictly.
- No Doorstop needed for debugging: attach your IDE to the running game.

See the wiki's [RimWorld 1.7 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.7_Mod_Updates).
