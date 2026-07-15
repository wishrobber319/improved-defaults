using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Disable Tomes in the default "Anything" reading policy.
    //
    // ReadingPolicyDatabase.GenerateStartingPolicies() builds the starting policy set; policy[0] is the
    // "Anything" default, which disallows all then allows every Book-subclass def (Tome included). We
    // postfix it to un-allow the Tome book def on that default policy only - the other starting policies
    // (Textbook, Schematic, ...) already whitelist specific books, and user-created policies are untouched.
    //
    // GenerateStartingPolicies runs from the database constructor at new-game time; on load, ExposeData
    // replaces the policy list with the saved one, so existing saves keep their reading policies as-is.
    [HarmonyPatch(typeof(ReadingPolicyDatabase), "GenerateStartingPolicies")]
    public static class Patch_ReadingPolicyDatabase_GenerateStartingPolicies
    {
        private const string TomeDefName = "Tome";

        public static void Postfix(ReadingPolicyDatabase __instance)
        {
            ThingDef tome = DefDatabase<ThingDef>.GetNamedSilentFail(TomeDefName);
            if (tome == null)
            {
                return; // Anomaly not active
            }

            ReadingPolicy defaultPolicy = __instance.DefaultReadingPolicy();
            defaultPolicy?.defFilter?.SetAllow(tome, allow: false);
        }
    }
}
