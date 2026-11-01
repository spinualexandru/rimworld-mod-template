using Verse;

namespace RimWorldMod;

/// <summary>
/// The XML side of a comp: every public field can be set from a def, like
/// &lt;message&gt; in Defs/ThingDefs/ExampleItem.xml.
/// </summary>
public class CompProperties_ExampleInfo : CompProperties
{
    public string message;

    public CompProperties_ExampleInfo()
    {
        compClass = typeof(CompExampleInfo);
    }
}

/// <summary>
/// The C# side: one instance lives on every thing whose def has the properties above.
/// </summary>
public class CompExampleInfo : ThingComp
{
    public CompProperties_ExampleInfo Props => (CompProperties_ExampleInfo)props;

    /// <summary>Adds a line to the inspect pane when the thing is selected.</summary>
    public override string CompInspectStringExtra() => Props.message;
}
