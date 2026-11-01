using System.Xml.Linq;

namespace RimWorldMod.Templates.Tests;

[Collection(TemplateCollection.Name)]
public class ContentTests(TemplateFixture template)
{
    static readonly string[] Always =
    [
        ".gitattributes",
        ".gitignore",
        "About/About.xml",
        "README.md",
        "scripts/symlink.cs",
    ];

    static readonly string[] Xml =
    [
        "Defs/ThingDefs/ExampleItem.xml",
        "Patches/Example.xml",
        "Textures/Things/Item/TestMod/ExampleItem.png",
    ];

    static readonly string[] CSharp =
    [
        "Source/TestMod.slnx",
        "Source/TestMod/ModEntry.cs",
        "Source/TestMod/TestMod.csproj",
    ];

    static readonly string[] Comps = ["Source/TestMod/Comps/CompProperties_ExampleInfo.cs"];
    static readonly string[] Harmony = ["Source/TestMod/Patches/Game_FinalizeInit_Patch.cs"];
    static readonly string[] Settings = ["Languages/English/Keyed/TestMod.xml", "Source/TestMod/Settings.cs"];

    /// <summary>Placeholders in the template that must never survive into a generated mod.</summary>
    static readonly string[] Placeholders =
    [
        "RimWorldMod", "XmlModName", "TextModName", "CSharpModName", "TextDirName",
        "XmlAuthor", "TextAuthor", "XmlPackageId", "TextPackageId", "XmlDescription", "TextDescription",
        "#if", "#else", "#endif",
    ];

    public static TheoryData<string, string[], string[][]> Layers => new()
    {
        { "default (XML + C#)", [], [Xml, CSharp, Comps] },
        { "XML only", ["--csharp", "false"], [Xml] },
        { "C# only", ["--xml", "false"], [CSharp] },
        { "Harmony implies C#", ["--xml", "false", "--csharp", "false", "--harmony"], [CSharp, Harmony] },
        { "everything", ["--harmony", "--settings"], [Xml, CSharp, Comps, Harmony, Settings] },
        { "settings need C#", ["--csharp", "false", "--settings"], [Xml] },
        { "no layers", ["--xml", "false", "--csharp", "false"], [] },
    };

    [Theory]
    [MemberData(nameof(Layers))]
    public void CreatesExactlyTheFilesForTheChosenLayers(string label, string[] options, string[][] groups)
    {
        var dir = template.Create("TestMod", options);

        var expected = Always.Concat(groups.SelectMany(g => g)).Order();
        var actual = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/'))
            .Order();
        Assert.True(expected.SequenceEqual(actual), $"{label}:\nexpected {string.Join(", ", expected)}\nactual   {string.Join(", ", actual)}");
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void LeavesNoPlaceholdersAndWritesValidXml(string label, string[] options, string[][] groups)
    {
        _ = groups;
        var dir = template.Create("TestMod", options);

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".png", StringComparison.Ordinal) || file.EndsWith("symlink.cs", StringComparison.Ordinal)) continue;
            var text = File.ReadAllText(file);
            foreach (var placeholder in Placeholders)
            {
                Assert.False(text.Contains(placeholder, StringComparison.Ordinal), $"{label}: {file} still contains {placeholder}");
            }
            if (Path.GetExtension(file) is ".xml" or ".csproj" or ".slnx")
            {
                XDocument.Load(file);
            }
        }
    }

    [Fact]
    public void AboutXmlListsHarmonyOnlyWhenChosen()
    {
        var without = XDocument.Load(Path.Combine(template.Create("TestMod"), "About", "About.xml"));
        Assert.Null(without.Root!.Element("modDependencies"));
        Assert.Null(without.Root!.Element("loadAfter"));

        var with = XDocument.Load(Path.Combine(template.Create("TestMod", "--harmony"), "About", "About.xml"));
        Assert.Equal("brrainz.harmony", with.Root!.Element("modDependencies")!.Element("li")!.Element("packageId")!.Value);
        Assert.Equal("brrainz.harmony", with.Root!.Element("loadAfter")!.Element("li")!.Value);
    }

    [Fact]
    public void LinkScriptIsCopiedVerbatim()
    {
        var dir = template.Create("TestMod");
        var original = File.ReadAllText(Path.Combine(TemplateFixture.RepoRoot, "content", "RimWorldMod", "scripts", "symlink.cs"));
        Assert.Equal(original, File.ReadAllText(Path.Combine(dir, "scripts", "symlink.cs")));
    }
}
