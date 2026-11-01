using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorldMod.Patches;

/// <summary>
/// Harmony patches change what the game's own methods do, at runtime.
///
/// - Prefix runs before the original. Return false to skip the original.
/// - Postfix runs after it, and can read or change the result via `ref T __result`.
/// - Transpiler rewrites the original's IL; use it when the above can't reach.
///
/// Special parameters: `__instance` (the object), `__result` (return value),
/// and any of the original's parameters by name. Harmony 3 checks their types,
/// so they must match the original method exactly.
///
/// This example shows a message every time a game is started or loaded.
/// </summary>
[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
public static class Game_FinalizeInit_Patch
{
    public static void Postfix()
    {
        Messages.Message("CSharpModName is active (Harmony patch).", MessageTypeDefOf.PositiveEvent, historical: false);
    }
}
