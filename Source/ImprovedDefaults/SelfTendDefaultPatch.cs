using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default new pawns to self-tend ON ("Allow self-tend"), so an injured pawn tends its own wounds
    // (at reduced efficiency) instead of waiting on a doctor.
    //
    // Pawn_PlayerSettings initialises selfTend = false in its constructor; we postfix it to true. Like
    // the hostility-response default (a second postfix on this same ctor), this only changes freshly
    // created pawns and stays fully editable in-game. Existing saves are unaffected: ExposeData restores
    // selfTend after construction (scribe default false, so a pawn that never enabled it loads back as
    // false), overwriting this for loads. Applies to every generated pawn, same breadth as the hostility
    // default - faction isn't assigned yet at ctor time, so this can't be scoped to colonists here.
    [HarmonyPatch(typeof(Pawn_PlayerSettings), MethodType.Constructor, new Type[] { typeof(Pawn) })]
    public static class Patch_Pawn_PlayerSettings_Ctor_SelfTend
    {
        public static void Postfix(Pawn_PlayerSettings __instance)
        {
            __instance.selfTend = true;
        }
    }
}
