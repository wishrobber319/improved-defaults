using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default the two home-area play toggles for a new colony:
    //   * autoRebuild  -> ON  : destroyed structures in the home area are auto-queued for rebuild.
    //   * autoHomeArea -> OFF : the home area does NOT auto-expand around every new construction.
    //
    // PlaySettings initialises autoRebuild = false and autoHomeArea = true in its field initialisers;
    // we postfix the constructor to flip both. Only new games are affected: on load, PlaySettings
    // .ExposeData restores each value after construction (autoRebuild scribe-default false, autoHomeArea
    // scribe-default true), so a saved game keeps whatever the player had set. Both stay fully editable
    // via the play-settings toggle row in-game.
    // Separate postfix from Patch_PlaySettings_Ctor (which sets useWorkPriorities); Harmony runs both.
    [HarmonyPatch(typeof(PlaySettings), MethodType.Constructor, new Type[0])]
    public static class Patch_PlaySettings_Ctor_HomeArea
    {
        public static void Postfix(PlaySettings __instance)
        {
            __instance.autoRebuild = true;
            __instance.autoHomeArea = false;
        }
    }
}
