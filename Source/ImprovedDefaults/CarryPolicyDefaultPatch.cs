using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Default the colonist "Carry" (inventory stock) medicine setting to Healroot x3 instead of Medicine x0.
    //
    // Pawn_InventoryStockTracker.CreateDefaultEntryFor builds a group's default entry as
    // { count = group.min, thingDef = group.DefaultThingDef }. For the vanilla "Medicine" group that's
    // 0 of MedicineIndustrial. We postfix it to hand back MedicineHerbal (shown as "Healroot" with the
    // medieval medicine mod) at a count of 3 (the group's max), leaving all other groups untouched.
    //
    // This is the genuine default factory, so it also applies to any pawn that has never opened its carry
    // setting - including existing colonists on load. That matches "make Healroot 3 the default"; anyone
    // who explicitly set a carry value keeps it (their saved entry is restored, bypassing this method).
    [HarmonyPatch(typeof(Pawn_InventoryStockTracker), "CreateDefaultEntryFor")]
    public static class Patch_Pawn_InventoryStockTracker_CreateDefaultEntryFor
    {
        private const string MedicineGroupDefName = "Medicine";
        private const string HealrootThingDefName = "MedicineHerbal"; // "Healroot" in-game
        private const int DesiredCount = 3;

        public static void Postfix(InventoryStockGroupDef group, ref InventoryStockEntry __result)
        {
            if (__result == null || group == null || group.defName != MedicineGroupDefName)
            {
                return;
            }

            ThingDef healroot = DefDatabase<ThingDef>.GetNamedSilentFail(HealrootThingDefName);
            if (healroot != null && group.thingDefs != null && group.thingDefs.Contains(healroot))
            {
                __result.thingDef = healroot;
            }

            int count = DesiredCount;
            if (count < group.min) count = group.min;
            if (count > group.max) count = group.max;
            __result.count = count;
        }
    }
}
