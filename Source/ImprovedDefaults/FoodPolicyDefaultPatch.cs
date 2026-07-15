using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Trim the default "Lavish" food policy: disallow rotten/human-meat/insect-meat foods and a set of
    // categories, keeping targeted exceptions (Beer under Drugs; vegetarian & animal-product raw food).
    //
    // FoodRestrictionDatabase.GenerateStartingFoodRestrictions() builds the starting policies; policy[0]
    // is "Lavish", created allow-all. We postfix it to switch off the requested special filters and
    // categories on that default only. Disallowing a category cascades to its descendants, so we disallow
    // the parent first, then re-allow the exception children.
    //
    // Runs from the database constructor at new-game time; on load ExposeData replaces the list with the
    // saved policies, so existing saves keep their food policies unchanged. Each lookup is silent-fail, so
    // missing defs (e.g. Medieval Overhaul's Condiments/Grains categories not loaded) simply no-op.
    [HarmonyPatch(typeof(FoodRestrictionDatabase), "GenerateStartingFoodRestrictions")]
    public static class Patch_FoodRestrictionDatabase_GenerateStartingFoodRestrictions
    {
        public static void Postfix(FoodRestrictionDatabase __instance)
        {
            ThingFilter filter = __instance.DefaultFoodRestriction()?.filter;
            if (filter == null)
            {
                return;
            }

            // Special filters off.
            DisallowSpecial(filter, "AllowRotten");
            DisallowSpecial(filter, "AllowCannibal");    // "food with human meat"
            DisallowSpecial(filter, "AllowInsectMeat");

            // Whole categories off.
            DisallowCategory(filter, "Corpses");
            DisallowCategory(filter, "Plants");
            DisallowCategory(filter, "Items");
            DisallowCategory(filter, "ResourcesRaw");    // "raw resources"
            DisallowCategory(filter, "DankPyon_Condiments"); // Medieval Overhaul "condiments"
            DisallowCategory(filter, "DankPyon_Cereal");     // Medieval Overhaul "grains"

            // Manufactured off, but keep Beer (a drug) allowed.
            DisallowCategory(filter, "Manufactured");
            AllowThing(filter, "Beer");

            // Raw food off, but keep vegetarian and animal-product raw food allowed.
            DisallowCategory(filter, "FoodRaw");
            AllowCategory(filter, "PlantFoodRaw");        // "vegetarian"
            AllowCategory(filter, "AnimalProductRaw");    // "animal products"
        }

        private static void DisallowSpecial(ThingFilter filter, string defName)
        {
            SpecialThingFilterDef def = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                filter.SetAllow(def, allow: false);
            }
        }

        private static void DisallowCategory(ThingFilter filter, string defName)
        {
            ThingCategoryDef def = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                filter.SetAllow(def, allow: false);
            }
        }

        private static void AllowCategory(ThingFilter filter, string defName)
        {
            ThingCategoryDef def = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                filter.SetAllow(def, allow: true);
            }
        }

        private static void AllowThing(ThingFilter filter, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                filter.SetAllow(def, allow: true);
            }
        }
    }
}
