using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ImprovedDefaults
{
    // Preset the Create World screen: population 5/7, landmarks 6/7, pollution 0%, map size 325x325,
    // and a customised faction set.
    //
    // Page_CreateWorldParams.Reset() establishes every slider default and calls ResetFactionCounts()
    // to build the faction list; both read straight from private fields that the UI then renders. We
    // postfix each so our values become the starting point while everything stays user-adjustable:
    //   - Reset()             -> population / landmarkDensity / pollution + GameInitData.mapSize
    //   - ResetFactionCounts()-> remove/add specific FactionDefs (also covers the "Reset factions"
    //                            button, which calls ResetFactionCounts directly)
    // Map size lives on GameInitData (default 250); we set it here rather than in a PreOpen hook so a
    // manual change survives navigating away and back, and only "Reset all" restores 325.

    [HarmonyPatch(typeof(Page_CreateWorldParams), "Reset")]
    public static class Patch_Page_CreateWorldParams_Reset
    {
        private const int TargetMapSize = 325;
        private const float TargetPlanetCoverage = 0.3f; // 30% (valid options are 0.3, 0.5, 1.0)

        private static readonly FieldInfo PlanetCoverageField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "planetCoverage");
        private static readonly FieldInfo PopulationField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "population");
        private static readonly FieldInfo LandmarkDensityField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "landmarkDensity");
        private static readonly FieldInfo PollutionField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "pollution");

        public static void Postfix(Page_CreateWorldParams __instance)
        {
            // Globe coverage: default is 0.5 (50%) with Odyssey active; drop to 0.3 (30%).
            PlanetCoverageField?.SetValue(__instance, TargetPlanetCoverage);

            // Sliders are 0..(count-1); "5/7" and "6/7" are the 5th and 6th of 7 notches.
            PopulationField?.SetValue(__instance, OverallPopulation.LittleBitMore);   // 5/7
            LandmarkDensityField?.SetValue(__instance, LandmarkDensity.SlightlyMoreCrowded); // 6/7
            PollutionField?.SetValue(__instance, 0f);

            if (Find.GameInitData != null)
            {
                Find.GameInitData.mapSize = TargetMapSize;
            }
        }
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams), "ResetFactionCounts")]
    public static class Patch_Page_CreateWorldParams_ResetFactionCounts
    {
        private static readonly string[] RemoveDefNames =
        {
            "OutlanderCivil",          // civil outlander union
            "OutlanderRoughPig",       // rough pig union
            "TribeCivil",              // gentle tribe
            "TribeRoughNeanderthal",   // fierce neanderthal tribe
            "TribeSavageImpid",        // savage impid tribe
            "PirateYttakin",           // yttakin pirates
            "PirateWaster",            // waster pirates
            "Mechanoid",               // mechanoid hive
            "Insect",                  // insect geneline
        };

        private static readonly string[] AddDefNames =
        {
            "VFEM2_KingdomRough",      // rough kingdom
            "VFEM2_KingdomSavage",     // savage kingdom
            "VFEM2_ClanSavage",        // savage clan
            "VFEM2_CivilClan",         // civil clan
        };

        private static readonly FieldInfo FactionsField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "factions");
        private static readonly FieldInfo InitialFactionsField =
            AccessTools.Field(typeof(Page_CreateWorldParams), "initialFactions");

        public static void Postfix(Page_CreateWorldParams __instance)
        {
            if (FactionsField?.GetValue(__instance) is not List<FactionDef> factions)
            {
                return;
            }

            foreach (string defName in RemoveDefNames)
            {
                FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
                if (def != null)
                {
                    factions.RemoveAll(f => f == def);
                }
            }

            foreach (string defName in AddDefNames)
            {
                FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
                // Only add if present (VFE Medieval 2 loaded) and not already in the list.
                if (def != null && !factions.Contains(def))
                {
                    factions.Add(def);
                }
            }

            // Re-baseline initialFactions so the screen treats our set as the unmodified default
            // (otherwise it would flag the list as changed and light up "Reset factions").
            if (InitialFactionsField?.GetValue(__instance) is List<FactionDef> initialFactions)
            {
                initialFactions.Clear();
                initialFactions.AddRange(factions);
            }
        }
    }

    // Suppress the yellow "Disabling mechanoid hive / insect geneline..." warnings on the Create World
    // screen. WorldFactionsUIUtility.DoWindowContents prints them whenever those factions aren't in the
    // list, and we deliberately remove them (medieval - no mechanoid raids or insect infestations), so
    // the warnings are just noise. We temporarily re-add Mechanoid + Insect for the duration of the draw
    // so the warning checks (factions.Contains(...)) pass. Both have displayInFactionSelection = false, so
    // they never render in the faction list, and the finalizer removes them again so world generation
    // still excludes them (they stay disabled). The finalizer guarantees cleanup even if the draw throws.
    [HarmonyPatch(typeof(WorldFactionsUIUtility), nameof(WorldFactionsUIUtility.DoWindowContents))]
    public static class Patch_WorldFactionsUIUtility_SuppressDisabledWarnings
    {
        private static readonly List<FactionDef> tempAdded = new List<FactionDef>();

        public static void Prefix(List<FactionDef> factions)
        {
            tempAdded.Clear();
            if (factions == null)
            {
                return;
            }

            TryAddHidden(factions, FactionDefOf.Mechanoid);
            TryAddHidden(factions, FactionDefOf.Insect);
        }

        public static void Finalizer(List<FactionDef> factions)
        {
            if (factions != null)
            {
                for (int i = 0; i < tempAdded.Count; i++)
                {
                    factions.Remove(tempAdded[i]);
                }
            }

            tempAdded.Clear();
        }

        private static void TryAddHidden(List<FactionDef> factions, FactionDef def)
        {
            if (def != null && !factions.Contains(def))
            {
                factions.Add(def);
                tempAdded.Add(def);
            }
        }
    }
}
