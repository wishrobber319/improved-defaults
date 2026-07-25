using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // On the "Choose your ideoligion" screen (New Colony), pre-select the first bundled "Modded"
    // ideoligion card when one exists, so it is ready to confirm with Next without a click. The cards
    // come from Ideoligion Framework (CustomPresetBuilder), which files them under the runtime category
    // defName "IdeoligionFramework_Custom". Purely a default: the player can still pick anything else.
    // No-op when the framework is absent or ships no bundled ideoligions.
    [HarmonyPatch(typeof(Page_ChooseIdeoPreset), nameof(Page_ChooseIdeoPreset.PostOpen))]
    public static class ModdedIdeoAutoSelectPatch
    {
        private const string ModdedCategoryDefName = "IdeoligionFramework_Custom";

        // selectedIdeo and presetSelection are private on the page; presetSelection is a private nested enum.
        private static readonly FieldInfo SelectedIdeoField = AccessTools.Field(typeof(Page_ChooseIdeoPreset), "selectedIdeo");
        private static readonly FieldInfo PresetSelectionField = AccessTools.Field(typeof(Page_ChooseIdeoPreset), "presetSelection");
        private static readonly object PresetSelectionPreset = ResolvePresetEnumValue();

        public static void Postfix(Page_ChooseIdeoPreset __instance)
        {
            if (SelectedIdeoField == null || PresetSelectionField == null || PresetSelectionPreset == null)
            {
                return;
            }

            // First card in the Modded group (DefDatabase order == the framework's file order).
            IdeoPresetDef first = DefDatabase<IdeoPresetDef>.AllDefs
                .FirstOrDefault(p => p.categoryDef != null && p.categoryDef.defName == ModdedCategoryDefName);
            if (first == null)
            {
                return;
            }

            SelectedIdeoField.SetValue(__instance, first);
            PresetSelectionField.SetValue(__instance, PresetSelectionPreset);
        }

        private static object ResolvePresetEnumValue()
        {
            Type enumType = typeof(Page_ChooseIdeoPreset).GetNestedType("PresetSelection", BindingFlags.NonPublic);
            if (enumType == null || !enumType.IsEnum)
            {
                return null;
            }
            try
            {
                return Enum.Parse(enumType, "Preset");
            }
            catch
            {
                return null;
            }
        }
    }
}
