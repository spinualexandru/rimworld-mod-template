using System.Diagnostics;

namespace RimWorldMod.Templates.Tests;

[CollectionDefinition(Name)]
public sealed class TemplateCollection : ICollectionFixture<TemplateFixture>
{
    // Tests share one template install and run one at a time.
    public const string Name = "template";
}

/// <summary>
/// Packs the template, installs the package into a private template cache (so the
/// machine's own `dotnet new` setup is untouched) and creates mods from it.
/// </summary>
public sealed class TemplateFixture : IDisposable
{
    public static readonly string RepoRoot = FindRepoRoot();

    readonly string root = Directory.CreateTempSubdirectory("rimworld-mod-tests-").FullName;
    string Hive => Path.Combine(root, "hive");

    public TemplateFixture()
    {
        var packDir = Path.Combine(root, "pack");
        Dotnet.Run(RepoRoot, "pack", Path.Combine(RepoRoot, "RimWorldMod.Templates.csproj"), "-o", packDir).AssertSuccess();
        var package = Directory.GetFiles(packDir, "*.nupkg").Single();
        Dotnet.Run(root, "new", "install", package, "--debug:custom-hive", Hive).AssertSuccess();
    }

    /// <summary>A fresh, empty folder inside the fixture's temp folder.</summary>
    public string NewDir(string name)
    {
        var dir = Path.Combine(root, "work", Guid.NewGuid().ToString("N")[..8], name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Runs `dotnet new rimworld-mod` into a new folder called <paramref name="name"/>
    /// and returns that folder. Linking is off unless the options turn it on.
    /// </summary>
    public string Create(string name, params string[] options) => Create(name, null, options);

    public string Create(string name, IDictionary<string, string?>? env, params string[] options) =>
        CreateAt(NewDir(name), env, options);

    public string CreateAt(string dir, IDictionary<string, string?>? env, params string[] options)
    {
        string[] args = ["new", "rimworld-mod", "-o", dir, "--debug:custom-hive", Hive, .. options];
        if (!options.Contains("--link")) args = [.. args, "--link", "false"];
        Dotnet.Run(root, env, args).AssertSuccess();
        return dir;
    }

    /// <summary>A minimal fake RimWorld install: enough for the link script to recognise it.</summary>
    public string FakeGame()
    {
        var dir = NewDir("RimWorld");
        Directory.CreateDirectory(Path.Combine(dir, "Data", "Core"));
        Directory.CreateDirectory(Path.Combine(dir, "Mods"));
        File.WriteAllText(Path.Combine(dir, "Version.txt"), "1.7.0 rev0");
        return dir;
    }

    public void Dispose()
    {
        // Links first, so deleting the temp folder never follows one.
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).ToList())
        {
            if (new DirectoryInfo(dir).LinkTarget is not null) Directory.Delete(dir);
        }
        Directory.Delete(root, recursive: true);
    }

    static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RimWorldMod.Templates.csproj"))) return dir.FullName;
        }
        throw new InvalidOperationException("Could not find RimWorldMod.Templates.csproj above the test assembly.");
    }
}

public sealed record RunResult(int ExitCode, string Output, string Command)
{
    public RunResult AssertSuccess()
    {
        Assert.True(ExitCode == 0, $"`{Command}` exited with {ExitCode}:\n{Output}");
        return this;
    }

    public RunResult AssertFailure()
    {
        Assert.True(ExitCode != 0, $"`{Command}` should have failed:\n{Output}");
        return this;
    }
}

public static class Dotnet
{
    public static RunResult Run(string workDir, params string[] args) => Run(workDir, null, args);

    public static RunResult Run(string workDir, IDictionary<string, string?>? env, params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        // Tests choose the game folder explicitly; never inherit one from the machine.
        startInfo.Environment.Remove("RIMWORLD_DIR");
        foreach (var (key, value) in env ?? new Dictionary<string, string?>()) startInfo.Environment[key] = value;

        using var process = Process.Start(startInfo)!;
        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {string.Join(' ', args)} timed out.");
        }
        return new RunResult(process.ExitCode, stdout.Result + stderr.Result, $"dotnet {string.Join(' ', args)}");
    }
}
