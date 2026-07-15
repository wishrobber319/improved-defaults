using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default "Manual priorities" (numbered 1-4 work priorities) to ON.
    //
    // PlaySettings.useWorkPriorities defaults to false. We set it true in the PlaySettings constructor so
    // new games start with manual priorities enabled. On load, ExposeData restores the saved value after
    // construction (defaulting to false when it was left off and thus not written), so existing saves are
    // unchanged.
    [HarmonyPatch(typeof(PlaySettings), MethodType.Constructor, new Type[0])]
    public static class Patch_PlaySettings_Ctor
    {
        public static void Postfix(PlaySettings __instance)
        {
            __instance.useWorkPriorities = true;
        }
    }

    // Shared work-priority defaults, matched by labelShort (the label shown in the work tab) so they stay
    // correct across mods that rename/replace work types - e.g. FSF Complex Jobs relabels vanilla Hauling
    // to "Maintenance" and adds its own "haul".
    //   - Flat: fixed priority regardless of the pawn.
    //   - Passion-scaled: priority depends on the pawn's passion for that work's relevant skills -
    //     burning (Major) -> 2, interested (Minor) -> 3, none -> 4.
    // Anything in neither set keeps whatever priority it already had.
    public static class WorkPriorityDefaults
    {
        // Fixed priorities, keyed by lower-cased WorkTypeDef.labelShort.
        private static readonly Dictionary<string, int> TargetPriorities = new Dictionary<string, int>
        {
            { "firefight", 2 },
            { "patient", 1 },
            { "bed rest", 3 },
            { "haul+", 1 },      // HaulingUrgent (Allow Tool)
            { "basic", 3 },
            { "rearm", 3 },
            { "production", 3 },
            { "smelt", 3 },
            { "stone cut", 3 },
            { "transport", 3 },
            { "mortuary", 3 },
            { "maintenance", 3 }, // FSF-relabeled vanilla Hauling
            { "deliver", 3 },
            { "haul", 3 },        // FSFHauling / regular hauling
            { "clean", 3 },
            { "dark study", 0 },  // disabled
            { "training", 4 },
        };

        // Passion-scaled work types (lower-cased labelShort): burning -> 2, interested -> 3, none -> 4.
        private static readonly HashSet<string> PassionScaledWork = new HashSet<string>
        {
            "nurse", "childcare", "magic", "warden", "manage", "handle",
            "train", "cook", "butcher", "hunt", "fish", "repair", "deconstruct", "construct",
            "plant cut", "harvest", "grow", "mine", "drill", "drugs", "craft", "tailor", "smith",
            "machining", "manufacturing", "fabrication", "art", "refine", "research", "scan",
        };

        // Like passion-scaled, but with no passion the work is left disabled instead of set to 4:
        // burning -> 2, interested -> 3, none -> 0 (don't do it).
        private static readonly HashSet<string> PassionOrDisableWork = new HashSet<string>
        {
            "doctor", "surgeon",
        };

        // Applies our default priorities to one pawn's work settings. Returns how many were set.
        public static int Apply(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike)
            {
                return 0;
            }

            Pawn_WorkSettings ws = pawn.workSettings;
            if (ws == null || !ws.EverWork)
            {
                return 0;
            }

            int changed = 0;
            foreach (WorkTypeDef work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                string key = (work.labelShort ?? work.defName)?.Trim().ToLowerInvariant();
                if (key == null)
                {
                    continue;
                }

                int priority;
                if (TargetPriorities.TryGetValue(key, out int flat))
                {
                    priority = flat;
                }
                else if (PassionOrDisableWork.Contains(key))
                {
                    priority = PriorityForPassionOrDisable(pawn.skills?.MaxPassionOfRelevantSkillsFor(work) ?? Passion.None);
                }
                else if (PassionScaledWork.Contains(key))
                {
                    priority = PriorityForPassion(pawn.skills?.MaxPassionOfRelevantSkillsFor(work) ?? Passion.None);
                }
                else
                {
                    continue;
                }

                if (pawn.WorkTypeIsDisabled(work))
                {
                    continue; // can't assign; already 0 for this pawn
                }

                ws.SetPriority(work, priority);
                changed++;
            }

            return changed;
        }

        private static int PriorityForPassion(Passion passion)
        {
            switch (passion)
            {
                case Passion.Major:
                    return 2; // burning
                case Passion.Minor:
                    return 3; // interested
                default:
                    return 4; // none
            }
        }

        private static int PriorityForPassionOrDisable(Passion passion)
        {
            switch (passion)
            {
                case Passion.Major:
                    return 2; // burning
                case Passion.Minor:
                    return 3; // interested
                default:
                    return 0; // no passion -> leave the work off
            }
        }
    }

    // Apply the defaults when a pawn's work settings are first initialized (covers pawns that join later:
    // recruits, births, etc.). Runs after other mods' postfixes so our values are the final word here.
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.EnableAndInitialize))]
    public static class Patch_Pawn_WorkSettings_EnableAndInitialize
    {
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(Pawn_WorkSettings), "pawn");

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn_WorkSettings __instance)
        {
            if (PawnField?.GetValue(__instance) is Pawn pawn)
            {
                WorkPriorityDefaults.Apply(pawn);
            }
        }
    }

    // Re-apply to the starting colonists once the new game is fully set up. EnableAndInitialize runs during
    // pawn/scenario setup (e.g. Prepare Carefully), but something in the start sequence rewrites those
    // priorities to 3 afterward. StartedNewGame fires at the very end of new-game creation, after that, so
    // re-applying here wins. GameComponents are auto-instantiated by RimWorld for every game.
    public class ImprovedDefaultsGameComponent : GameComponent
    {
        public ImprovedDefaultsGameComponent(Game game)
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                WorkPriorityDefaults.Apply(pawn);
            }
        }
    }
}
