#if (harmony)
using HarmonyLib;
#endif
#if (useSettings)
using UnityEngine;
#endif
using Verse;

namespace RimWorldMod;

/// <summary>
/// Entry point. RimWorld creates one instance of every <see cref="Mod"/> subclass
/// while loading mods, before any defs are loaded.
/// Code that needs defs belongs in a static constructor of a class marked
/// [StaticConstructorOnStartup] instead; those run once the game has finished loading.
/// </summary>
public class ModEntry : Mod
{
#if (useSettings)
    public static Settings Settings { get; private set; }

#endif
    public ModEntry(ModContentPack content) : base(content)
    {
#if (useSettings)
        Settings = GetSettings<Settings>();
#endif
#if (harmony)
        // Applies every [HarmonyPatch] class in this assembly (see Patches/).
        new Harmony(content.PackageIdPlayerFacing).PatchAll(typeof(ModEntry).Assembly);
#endif
        Log.Message($"[{content.Name}] Loaded.");
    }
#if (useSettings)

    /// <summary>Name of this mod's entry under Options > Mod settings.</summary>
    public override string SettingsCategory() => Content.Name;

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled(
            "RimWorldMod_ExampleToggle".Translate(),
            ref Settings.exampleToggle,
            "RimWorldMod_ExampleToggleTooltip".Translate());
        listing.Gap();
        listing.Label("RimWorldMod_ExampleMultiplier".Translate(Settings.exampleMultiplier.ToStringPercent()));
        Settings.exampleMultiplier = listing.Slider(Settings.exampleMultiplier, 0.1f, 5f);
        listing.End();
    }
#endif
}
