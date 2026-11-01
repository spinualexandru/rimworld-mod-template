using Verse;

namespace RimWorldMod;

/// <summary>
/// Saved per player in the game's Config folder. Read it through ModEntry.Settings.
/// </summary>
public class Settings : ModSettings
{
    public bool exampleToggle = true;
    public float exampleMultiplier = 1f;

    public override void ExposeData()
    {
        // The last argument is the default, used when the value was never saved.
        Scribe_Values.Look(ref exampleToggle, "exampleToggle", true);
        Scribe_Values.Look(ref exampleMultiplier, "exampleMultiplier", 1f);
        base.ExposeData();
    }
}
