using System.Xml.Linq;

namespace RimWorldMod.Templates.Tests;

[Collection(TemplateCollection.Name)]
public class NamingTests(TemplateFixture template)
{
    static XElement About(string dir) => XDocument.Load(Path.Combine(dir, "About", "About.xml")).Root!;

    [Theory]
    [InlineData("my-cool-mod", "My Cool Mod", "MyCoolMod")]
    [InlineData("MyCoolMod", "My Cool Mod", "MyCoolMod")]
    [InlineData("better_bridges", "Better Bridges", "BetterBridges")]
    [InlineData("7DaysToRim", "7 Days To Rim", "Mod7DaysToRim")]
    public void DerivesNamesFromTheProjectName(string projectName, string modName, string assemblyName)
    {
        var dir = template.Create(projectName, "--mod-author", "Alex Spinu");

        Assert.Equal(modName, About(dir).Element("name")!.Value);
        Assert.Equal($"AlexSpinu.{assemblyName}", About(dir).Element("packageId")!.Value);
        Assert.True(File.Exists(Path.Combine(dir, "Source", assemblyName, $"{assemblyName}.csproj")));
        Assert.Contains($"<defName>{assemblyName}_ExampleItem</defName>", File.ReadAllText(Path.Combine(dir, "Defs", "ThingDefs", "ExampleItem.xml")));
    }

    [Fact]
    public void ExplicitValuesWin()
    {
        var dir = template.Create("bridges",
            "--mod-name", "Better Bridges",
            "--mod-author", "Someone",
            "--package-id", "someone.bridges",
            "--description", "Bridges, but better.");

        var about = About(dir);
        Assert.Equal("Better Bridges", about.Element("name")!.Value);
        Assert.Equal("Someone", about.Element("author")!.Value);
        Assert.Equal("someone.bridges", about.Element("packageId")!.Value);
        Assert.Equal("Bridges, but better.", about.Element("description")!.Value);
        Assert.True(Directory.Exists(Path.Combine(dir, "Source", "BetterBridges")));
    }

    [Fact]
    public void AuthorDefaultsToTheUserName()
    {
        var dir = template.Create("Mod", new Dictionary<string, string?> { ["USER"] = "tynan", ["USERNAME"] = null });

        Assert.Equal("tynan", About(dir).Element("author")!.Value);
        Assert.Equal("Tynan.Mod", About(dir).Element("packageId")!.Value);
    }

    [Fact]
    public void EscapesValuesForEachFileType()
    {
        const string name = "Bob's <Big> & \"Bold\" Mod \\o/";
        const string description = "Uses <XML> & \"quotes\".";
        var dir = template.Create("bob", "--mod-name", name, "--mod-author", "A & B", "--description", description, "--harmony");

        var about = About(dir);
        Assert.Equal(name, about.Element("name")!.Value);
        Assert.Equal("A & B", about.Element("author")!.Value);
        Assert.Equal(description, about.Element("description")!.Value);
        Assert.Equal("AB.BobsBigBoldModo", about.Element("packageId")!.Value);

        var patch = File.ReadAllText(Path.Combine(dir, "Source", "BobsBigBoldModo", "Patches", "Game_FinalizeInit_Patch.cs"));
        Assert.Contains("\"Bob's <Big> & \\\"Bold\\\" Mod \\\\o/ is active (Harmony patch).\"", patch);
        Assert.StartsWith($"# {name}\n\n{description}\n", File.ReadAllText(Path.Combine(dir, "README.md")).ReplaceLineEndings("\n"));
    }
}
