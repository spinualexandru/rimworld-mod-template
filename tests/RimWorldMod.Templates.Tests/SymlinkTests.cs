namespace RimWorldMod.Templates.Tests;

/// <summary>
/// Runs scripts/symlink.cs from a generated mod against fake RimWorld installs.
/// Every run names its game folder, so the real game is never touched.
/// </summary>
[Collection(TemplateCollection.Name)]
public class SymlinkTests(TemplateFixture template)
{
    // Compiling the script takes a few seconds per location, so the tests share one mod.
    static string? sharedMod;
    string Mod => sharedMod ??= template.Create("SymlinkMod", "--xml", "false", "--csharp", "false");

    RunResult Script(string mod, params string[] args) => Script(mod, null, args);

    static RunResult Script(string mod, IDictionary<string, string?>? env, params string[] args) =>
        Dotnet.Run(mod, env, ["scripts/symlink.cs", .. args]);

    static string? LinkTarget(string path) => new DirectoryInfo(path).LinkTarget;

    [Fact]
    public void LinksStatusAndUnlinks()
    {
        var game = template.FakeGame();
        var link = Path.Combine(game, "Mods", "SymlinkMod");

        Assert.Contains("Linked", Script(Mod, "--game-dir", game).AssertSuccess().Output);
        Assert.Equal(Mod, LinkTarget(link));
        Assert.True(File.Exists(Path.Combine(link, "About", "About.xml")));

        Assert.Contains("Already linked", Script(Mod, "link", "--game-dir", game).AssertSuccess().Output);
        Assert.Contains("linked to this mod", Script(Mod, "status", "--game-dir", game).AssertSuccess().Output);

        Assert.Contains("Unlinked", Script(Mod, "unlink", "--game-dir", game).AssertSuccess().Output);
        Assert.False(Path.Exists(link));
        Assert.True(File.Exists(Path.Combine(Mod, "About", "About.xml")));
        Assert.Contains("not linked", Script(Mod, "status", "--game-dir", game).AssertSuccess().Output);
        Assert.Contains("Nothing to unlink", Script(Mod, "unlink", "--game-dir", game).AssertSuccess().Output);
    }

    [Fact]
    public void AcceptsTheModsFolderAndTheEnvironmentVariable()
    {
        var game = template.FakeGame();
        var mods = Path.Combine(game, "Mods");

        Script(Mod, "--game-dir", mods).AssertSuccess();
        Assert.Equal(Mod, LinkTarget(Path.Combine(mods, "SymlinkMod")));
        Script(Mod, new Dictionary<string, string?> { ["RIMWORLD_DIR"] = game }, "unlink").AssertSuccess();
        Assert.False(Path.Exists(Path.Combine(mods, "SymlinkMod")));
    }

    [Fact]
    public void UsesTheGivenFolderName()
    {
        var game = template.FakeGame();

        Script(Mod, "--game-dir", game, "--name", "Renamed").AssertSuccess();
        Assert.Equal(Mod, LinkTarget(Path.Combine(game, "Mods", "Renamed")));
    }

    [Fact]
    public void NeverTouchesARealFolder()
    {
        var game = template.FakeGame();
        var occupied = Path.Combine(game, "Mods", "SymlinkMod");
        Directory.CreateDirectory(occupied);
        File.WriteAllText(Path.Combine(occupied, "keep.txt"), "mine");

        Assert.Contains("real folder", Script(Mod, "--game-dir", game, "--force").AssertFailure().Output);
        Assert.Contains("Refusing", Script(Mod, "unlink", "--game-dir", game, "--force").AssertFailure().Output);
        Assert.Equal("mine", File.ReadAllText(Path.Combine(occupied, "keep.txt")));
        Assert.Null(LinkTarget(occupied));
    }

    [Fact]
    public void ReplacesOtherLinksOnlyWithForce()
    {
        var game = template.FakeGame();
        var other = template.NewDir("OtherMod");
        File.WriteAllText(Path.Combine(other, "keep.txt"), "theirs");
        var link = Path.Combine(game, "Mods", "SymlinkMod");
        Directory.CreateSymbolicLink(link, other);

        Assert.Contains("--force", Script(Mod, "--game-dir", game).AssertFailure().Output);
        Assert.Contains("not this mod", Script(Mod, "unlink", "--game-dir", game).AssertFailure().Output);
        Assert.Contains("another folder", Script(Mod, "status", "--game-dir", game).AssertSuccess().Output);
        Assert.Equal(other, LinkTarget(link));

        Script(Mod, "--game-dir", game, "--force").AssertSuccess();
        Assert.Equal(Mod, LinkTarget(link));
        Assert.Equal("theirs", File.ReadAllText(Path.Combine(other, "keep.txt")));
    }

    [Fact]
    public void ReplacesABrokenLinkWithForce()
    {
        var game = template.FakeGame();
        var link = Path.Combine(game, "Mods", "SymlinkMod");
        Directory.CreateSymbolicLink(link, Path.Combine(game, "gone"));

        Assert.Contains("broken link", Script(Mod, "status", "--game-dir", game).AssertSuccess().Output);
        Script(Mod, "--game-dir", game).AssertFailure();
        Script(Mod, "--game-dir", game, "--force").AssertSuccess();
        Assert.Equal(Mod, LinkTarget(link));
    }

    [Fact]
    public void SkipsModsThatAlreadyLiveInTheModsFolder()
    {
        var game = template.FakeGame();
        var mod = template.CreateAt(Path.Combine(game, "Mods", "InPlace"), null, "--xml", "false", "--csharp", "false");

        Assert.Contains("no link needed", Script(mod, "--game-dir", game).AssertSuccess().Output);
        Assert.Null(LinkTarget(mod));
    }

    [Fact]
    public void ExplainsBadInput()
    {
        Assert.Contains("doesn't look like a RimWorld install", Script(Mod, "--game-dir", template.NewDir("NotRimWorld")).AssertFailure().Output);
        Assert.Contains("Unknown argument", Script(Mod, "relink").AssertFailure().Output);
        Assert.Contains("needs a value", Script(Mod, "--game-dir").AssertFailure().Output);
        Assert.Contains("Usage:", Script(Mod, "help").AssertSuccess().Output);
    }

    [Fact]
    public void TemplateLinksAfterCreatingWhenAllowed()
    {
        var game = template.FakeGame();
        var env = new Dictionary<string, string?> { ["RIMWORLD_DIR"] = game };

        var mod = template.Create("PostAction", env, "--xml", "false", "--csharp", "false", "--link", "true", "--allow-scripts", "yes");
        Assert.Equal(mod, LinkTarget(Path.Combine(game, "Mods", "PostAction")));
    }
}
