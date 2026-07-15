using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default new colonists' Hostility response to Attack instead of Flee.
    //
    // Pawn_PlayerSettings initialises hostilityResponse = HostilityResponseMode.Flee in its constructor;
    // we postfix it to Attack. This only changes freshly-created pawns and stays fully editable in-game.
    // Existing saves are unaffected: ExposeData restores hostilityResponse after construction (defaulting
    // to Flee when the value was left at its default and thus not written), overwriting this for loads.
    [HarmonyPatch(typeof(Pawn_PlayerSettings), MethodType.Constructor, new Type[] { typeof(Pawn) })]
    public static class Patch_Pawn_PlayerSettings_Ctor
    {
        public static void Postfix(Pawn_PlayerSettings __instance)
        {
            __instance.hostilityResponse = HostilityResponseMode.Attack;
        }
    }
}
