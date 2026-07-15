using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Tweak the default "Anything" apparel policy: disallow tainted apparel and King's crown, and set the
    // allowed hit-points range to 51%-100%.
    //
    // OutfitDatabase.GenerateStartingOutfits() builds the starting apparel policies; policy[0] is
    // "Anything", created allow-all-apparel. We postfix it to adjust that default only.
    //
    // Runs from the database constructor at new-game time; on load ExposeData replaces the policy list
    // with the saved one, so existing saves keep their apparel policies. King's crown (VFE Medieval 2)
    // is looked up silent-fail, so it no-ops if that mod isn't loaded.
    [HarmonyPatch(typeof(OutfitDatabase), "GenerateStartingOutfits")]
    public static class Patch_OutfitDatabase_GenerateStartingOutfits
    {
        private const string KingsCrownDefName = "VFEM2_Apparel_KingsCrown";

        public static void Postfix(OutfitDatabase __instance)
        {
            ThingFilter filter = __instance.DefaultOutfit()?.filter;
            if (filter == null)
            {
                return;
            }

            // Disallow tainted (dead man's) apparel.
            SpecialThingFilterDef tainted = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowDeadmansApparel");
            if (tainted != null)
            {
                filter.SetAllow(tainted, allow: false);
            }

            // Disallow King's crown.
            ThingDef kingsCrown = DefDatabase<ThingDef>.GetNamedSilentFail(KingsCrownDefName);
            if (kingsCrown != null)
            {
                filter.SetAllow(kingsCrown, allow: false);
            }

            // Hit-points range 51%-100%.
            filter.AllowedHitPointsPercents = new FloatRange(0.51f, 1f);
        }
    }
}
