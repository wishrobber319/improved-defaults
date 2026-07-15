using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default new colonists' schedule to Recreation at hours 6 and 21.
    //
    // Pawn_TimetableTracker's constructor builds the 24-hour default: 0-5 & 22-23 Sleep, 6-21 Anything.
    // We postfix it to switch hours 6 and 21 (the first and last "awake" hours) to Joy ("Recreation").
    // This only sets the initial times list, so it's fully editable in-game and doesn't touch existing
    // colonists - saved timetables are restored by ExposeData after construction, overwriting this.
    [HarmonyPatch(typeof(Pawn_TimetableTracker), MethodType.Constructor, new Type[] { typeof(Pawn) })]
    public static class Patch_Pawn_TimetableTracker_Ctor
    {
        public static void Postfix(Pawn_TimetableTracker __instance)
        {
            if (__instance.times == null || __instance.times.Count < 22)
            {
                return;
            }

            __instance.times[6] = TimeAssignmentDefOf.Joy;
            __instance.times[21] = TimeAssignmentDefOf.Joy;
        }
    }
}
