using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    [StaticConstructorOnStartup]
    public static class ImprovedDefaultsMod
    {
        static ImprovedDefaultsMod()
        {
            new Harmony("wishRobber.improveddefaults").PatchAll();
        }
    }

    // Put Medieval Overhaul's "Battle Brothers" scenario first among the def-based scenarios.
    //
    // The Choose Scenario screen derives BOTH the on-screen order and the default selection from the
    // order of ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef):
    //   - Page_SelectScenario draws the def scenarios in that enumerable's order, and
    //   - EnsureValidSelection() picks .FirstOrDefault() from that same enumerable as curScen.
    // Def order is load order, so Core's scenarios (Crashlanded first) always precede a mod's. By
    // pulling Battle Brothers to the front of this one enumerable, it becomes both the top entry and the
    // pre-selected scenario in a single patch.
    //
    // ScenariosInCategory is a yield-iterator, so this Postfix receives the built enumerator as
    // __result; we materialise it, move Battle Brothers to index 0, and hand back the reordered list.
    // If Medieval Overhaul isn't loaded the def is absent (GetNamedSilentFail == null) and we no-op.
    [HarmonyPatch(typeof(ScenarioLister), nameof(ScenarioLister.ScenariosInCategory))]
    public static class Patch_ScenarioLister_ScenariosInCategory
    {
        private const string TargetDefName = "DankPyon_MercenaryStart"; // "Battle Brothers"

        public static void Postfix(ScenarioCategory cat, ref IEnumerable<Scenario> __result)
        {
            if (cat != ScenarioCategory.FromDef)
            {
                return;
            }

            ScenarioDef def = DefDatabase<ScenarioDef>.GetNamedSilentFail(TargetDefName);
            if (def?.scenario == null)
            {
                return; // Medieval Overhaul not active
            }

            List<Scenario> list = __result.ToList();
            int idx = list.IndexOf(def.scenario);
            if (idx <= 0)
            {
                return; // not present, or already first
            }

            list.RemoveAt(idx);
            list.Insert(0, def.scenario);
            __result = list;
        }
    }
}
