namespace RimWorldMod.Templates.Tests;

/// <summary>
/// Compiles every C# variant against the RimWorld reference assemblies.
/// Slow and needs NuGet access; skip with `dotnet test --filter Category!=Build`.
/// </summary>
[Collection(TemplateCollection.Name)]
[Trait("Category", "Build")]
public class BuildTests(TemplateFixture template)
{
    [Theory]
    [InlineData("TestMod", new[] { "--xml", "false" })]
    [InlineData("TestMod", new string[0])]
    [InlineData("TestMod", new[] { "--xml", "false", "--csharp", "false", "--harmony" })]
    [InlineData("TestMod", new[] { "--harmony", "--settings" })]
    [InlineData("BobsBoldModo", new[] { "--mod-name", "Bob's \"Bold\" Mod \\o/", "--harmony", "--settings" })]
    public void CompilesWithoutWarnings(string assemblyName, string[] options)
    {
        var dir = template.Create("TestMod", options);

        Dotnet.Run(dir, "build", "Source", "-warnaserror").AssertSuccess();

        // Only the mod's own assembly ships: no game, Harmony or .deps.json files.
        var output = Directory.GetFiles(Path.Combine(dir, "1.7", "Assemblies")).Select(Path.GetFileName).Order();
        Assert.Equal([$"{assemblyName}.dll", $"{assemblyName}.pdb"], output);
    }
}
