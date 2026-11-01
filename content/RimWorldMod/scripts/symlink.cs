// Links this mod folder into RimWorld's Mods folder, so the game loads it
// straight from your working copy. Unlinking only ever removes the link,
// never your files.
//
//   dotnet scripts/symlink.cs [link|unlink|status|help] [options]
//
// This is a single-file C# program: it needs the .NET 10 SDK and nothing else.
// The first run compiles it, which takes a few seconds.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

var modRoot = Paths.Real(Path.Combine(Paths.ScriptDirectory(), ".."));
var help = $"""
    Usage: dotnet scripts/symlink.cs [command] [options]

    Commands:
      link     Link this mod into RimWorld's Mods folder (default)
      unlink   Remove that link (only ever removes the link itself)
      status   Show where RimWorld is and whether this mod is linked
      help     Show this help

    Options:
      --game-dir <path>  RimWorld install folder (or its Mods folder).
                         Defaults to $RIMWORLD_DIR, then auto-detection.
      --name <name>      Folder name inside Mods (default: "{Path.GetFileName(modRoot)}")
      --force            Replace a link that points somewhere else

    """;

string? command = null, gameDir = null, name = null;
var force = false;
for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    switch (arg)
    {
        case "--game-dir" or "--name":
            if (i + 1 >= args.Length) return Fail($"{arg} needs a value.");
            if (arg == "--game-dir") gameDir = args[++i];
            else name = args[++i];
            break;
        case "--force":
            force = true;
            break;
        case "-h" or "--help":
            command = "help";
            break;
        case "link" or "unlink" or "status" or "help" when command is null:
            command = arg;
            break;
        default:
            return Fail($"Unknown argument \"{arg}\".\n\n{help}");
    }
}

command ??= "link";
if (command == "help")
{
    Console.Write(help);
    return 0;
}
if (!File.Exists(Path.Combine(modRoot, "About", "About.xml")))
    return Fail($"{modRoot} doesn't look like a mod folder (no About/About.xml).");

try
{
    var modsDir = Game.FindModsDir(gameDir);
    var linkPath = Path.Combine(modsDir, name ?? Path.GetFileName(modRoot));
    Console.WriteLine(command switch
    {
        "link" => ModLink.Create(modsDir, linkPath, modRoot, force),
        "unlink" => ModLink.Remove(linkPath, modRoot, force),
        _ => ModLink.Describe(modsDir, linkPath, modRoot),
    });
    return 0;
}
catch (Exception e) when (e is LinkException or IOException or UnauthorizedAccessException)
{
    return Fail(e.Message);
}

static int Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    return 1;
}

/// <summary>Finds the RimWorld install and its Mods folder.</summary>
static class Game
{
    /// <summary>Uses --game-dir, then $RIMWORLD_DIR, then the usual install locations, then asks.</summary>
    public static string FindModsDir(string? gameDir)
    {
        if (string.IsNullOrEmpty(gameDir)) gameDir = Environment.GetEnvironmentVariable("RIMWORLD_DIR");
        if (string.IsNullOrEmpty(gameDir))
        {
            var installs = FindInstalls();
            if (installs.Count > 1)
                Console.WriteLine($"Found {installs.Count} RimWorld installs, using {installs[0]}. Pass --game-dir to pick another.");
            gameDir = installs.FirstOrDefault() ?? AskForGameDir();
        }
        if (string.IsNullOrEmpty(gameDir))
            throw new LinkException("Could not find RimWorld. Pass --game-dir <path> or set the RIMWORLD_DIR environment variable.");

        return ResolveModsDir(gameDir)
            ?? throw new LinkException($"{gameDir} doesn't look like a RimWorld install folder (expected Data/Core or Version.txt inside).");
    }

    /// <summary>Accepts the install folder or its Mods folder; returns the Mods folder, or null.</summary>
    static string? ResolveModsDir(string dir)
    {
        var full = Path.GetFullPath(ExpandHome(dir));
        if (IsRimWorldDir(full))
        {
            var macMods = Path.Combine(full, "RimWorldMac.app", "Mods");
            return Directory.Exists(macMods) ? macMods : Path.Combine(full, "Mods");
        }
        var trimmed = Path.TrimEndingDirectorySeparator(full);
        if (Path.GetFileName(trimmed).Equals("Mods", StringComparison.OrdinalIgnoreCase)
            && IsRimWorldDir(Path.GetDirectoryName(trimmed)))
            return trimmed;
        return null;
    }

    static bool IsRimWorldDir(string? dir) =>
        !string.IsNullOrEmpty(dir)
        && (Directory.Exists(Path.Combine(dir, "Data", "Core"))
            || File.Exists(Path.Combine(dir, "Version.txt"))
            || Directory.Exists(Path.Combine(dir, "RimWorldMac.app")));

    /// <summary>Every RimWorld install in the usual Steam and GOG locations.</summary>
    static List<string> FindInstalls()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = SteamLibraries().Select(library => Path.Combine(library, "steamapps", "common", "RimWorld")).ToList();
        if (OperatingSystem.IsWindows())
        {
            var drive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
            candidates.Add(Path.Combine(drive + @"\", "GOG Games", "RimWorld"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games", "RimWorld"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add("/Applications/RimWorld");
            candidates.Add(Path.Combine(home, "Applications", "RimWorld"));
        }
        else
        {
            candidates.Add(Path.Combine(home, "GOG Games", "RimWorld"));
            candidates.Add(Path.Combine(home, "Games", "RimWorld"));
        }

        var found = new List<string>();
        foreach (var dir in candidates.Where(IsRimWorldDir))
        {
            var real = Paths.Real(dir);
            if (!found.Any(f => Paths.Same(f, real))) found.Add(real);
        }
        return found;
    }

    static IEnumerable<string> SteamRoots()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsWindows())
        {
            if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string steamPath)
                yield return Path.GetFullPath(steamPath);
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return Path.Combine(home, "Library", "Application Support", "Steam");
        }
        else
        {
            yield return Path.Combine(home, ".local", "share", "Steam");
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "root");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam");
            yield return Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam");
        }
    }

    /// <summary>Steam roots plus the extra library folders listed in libraryfolders.vdf.</summary>
    static IEnumerable<string> SteamLibraries()
    {
        foreach (var root in SteamRoots().Where(Directory.Exists))
        {
            yield return root;
            foreach (var vdf in new[] { Path.Combine(root, "steamapps", "libraryfolders.vdf"), Path.Combine(root, "config", "libraryfolders.vdf") })
            {
                if (!File.Exists(vdf)) continue;
                foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    yield return match.Groups[1].Value.Replace(@"\\", @"\");
            }
        }
    }

    static string? AskForGameDir()
    {
        if (Console.IsInputRedirected) return null;
        Console.Write("Could not find RimWorld. Path to your RimWorld install folder: ");
        var answer = Console.ReadLine()?.Trim().Trim('"', '\'');
        return string.IsNullOrEmpty(answer) ? null : answer;
    }

    static string ExpandHome(string path) =>
        path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith(@"~\", StringComparison.Ordinal)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..]
            : path;
}

/// <summary>Creates, removes and inspects the link. Never deletes real folders.</summary>
static class ModLink
{
    enum State { Missing, Occupied, Linked, Foreign, Dangling }

    public static string Create(string modsDir, string linkPath, string modRoot, bool force)
    {
        if (Paths.Same(Paths.Real(Path.GetDirectoryName(modRoot)!), Paths.Real(modsDir)))
            return $"{modRoot} already lives in the Mods folder; no link needed.";

        var (state, target) = Inspect(linkPath, modRoot);
        switch (state)
        {
            case State.Linked:
                return $"Already linked: {linkPath} -> {modRoot}";
            case State.Occupied:
                throw new LinkException(
                    $"{linkPath} already exists and is a real folder, not a link. "
                    + "Move it out of the way, or pass --name to use a different folder name.");
            case State.Foreign or State.Dangling when !force:
                throw new LinkException($"{linkPath} is already a link to {target}. Pass --force to replace it.");
            case State.Foreign or State.Dangling:
                DeleteLink(linkPath);
                break;
        }

        Directory.CreateDirectory(modsDir);
        if (OperatingSystem.IsWindows())
            CreateJunction(linkPath, modRoot);
        else
            Directory.CreateSymbolicLink(linkPath, modRoot);
        return $"Linked {linkPath} -> {modRoot}";
    }

    public static string Remove(string linkPath, string modRoot, bool force)
    {
        var (state, target) = Inspect(linkPath, modRoot);
        if (state == State.Missing) return $"Nothing to unlink: {linkPath} does not exist.";
        if (state == State.Occupied) throw new LinkException($"{linkPath} is a real folder, not a link. Refusing to delete it.");
        if (state != State.Linked && !force)
            throw new LinkException($"{linkPath} links to {target}, not this mod. Pass --force to remove it anyway.");

        DeleteLink(linkPath);
        return $"Unlinked {linkPath}";
    }

    public static string Describe(string modsDir, string linkPath, string modRoot)
    {
        var (state, target) = Inspect(linkPath, modRoot);
        var description = state switch
        {
            State.Missing => "not linked",
            State.Linked => "linked to this mod",
            State.Foreign => $"linked to another folder ({target})",
            State.Dangling => $"broken link to {target}",
            _ => "a real folder (not a link)",
        };
        return $"RimWorld mods folder: {modsDir}\n{linkPath}: {description}";
    }

    static (State State, string? Target) Inspect(string linkPath, string modRoot)
    {
        var target = Paths.LinkTarget(linkPath);
        if (target is null)
            return (Path.Exists(linkPath) ? State.Occupied : State.Missing, null);
        if (Paths.Same(target, modRoot) || Paths.Same(Paths.Real(target), modRoot))
            return (State.Linked, target);
        return (Directory.Exists(target) ? State.Foreign : State.Dangling, target);
    }

    /// <summary>Removes only the link itself, never the folder it points to.</summary>
    static void DeleteLink(string linkPath)
    {
        // A junction is removed like an empty folder; a symlink like a file.
        if (OperatingSystem.IsWindows())
            Directory.Delete(linkPath);
        else
            File.Delete(linkPath);
    }

    /// <summary>Junctions work without admin rights or Developer Mode, unlike symlinks.</summary>
    static void CreateJunction(string linkPath, string target)
    {
        var startInfo = new ProcessStartInfo("cmd.exe", $"/d /c mklink /J \"{linkPath}\" \"{target}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(startInfo)!;
        process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new LinkException($"mklink failed: {error.Trim()}");
    }
}

static class Paths
{
    public static string ScriptDirectory([CallerFilePath] string sourceFile = "") =>
        AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Path.GetDirectoryName(sourceFile)!;

    /// <summary>The target of a symlink or junction, as an absolute path; null if it isn't a link.</summary>
    public static string? LinkTarget(string path)
    {
        string? target;
        try
        {
            target = new DirectoryInfo(path).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        if (target is null) return null;
        if (target.StartsWith(@"\\?\", StringComparison.Ordinal) || target.StartsWith(@"\??\", StringComparison.Ordinal)) target = target[4..];
        return Path.GetFullPath(target, Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    /// <summary>The absolute path with every link along the way resolved, like realpath.</summary>
    public static string Real(string path, int depth = 0)
    {
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        foreach (var part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (depth < 40 && LinkTarget(current) is { } target) current = Real(target, depth + 1);
        }
        return current;
    }

    public static bool Same(string a, string b) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}

sealed class LinkException(string message) : Exception(message);
