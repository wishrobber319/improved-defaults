using System;
using HarmonyLib;
using Verse;

namespace ImprovedDefaults
{
    // Set Isekai RPG Leveling (JellyCreative.IsekaiLeveling) to this collection's progression pace:
    // XP gain 5x (mod default 3x, the slider's maximum) and 3 skill points per level (mod default 1).
    //
    // These are machine-local mod settings, so editing the config only changes one PC and does not travel
    // with the modlist. Applying them here bakes the pace into the collection so everyone gets the same one.
    // Reflection-only and gated on the type existing, so Improved Defaults still loads without Isekai (soft dep).
    //
    // Applied on every startup, before any game loads. Isekai reads these live at runtime, so the effect is
    // immediate and it always reflects the collection's intended pace. To change the pace, edit the constants
    // here rather than the in-game slider (this re-applies them on the next launch).
    [StaticConstructorOnStartup]
    public static class IsekaiDefaultsPatch
    {
        private const float XpMultiplier = 5f;      // Isekai default 3, slider range 0.1 - 5
        private const int SkillPointsPerLevel = 3;  // Isekai default 1, slider range 1 - 5

        static IsekaiDefaultsPatch()
        {
            try
            {
                Apply();
            }
            catch (Exception ex)
            {
                Log.Warning("[Improved Defaults] Could not set Isekai leveling defaults: " + ex.Message);
            }
        }

        private static void Apply()
        {
            Type modType = AccessTools.TypeByName("IsekaiLeveling.IsekaiMod");
            if (modType == null)
            {
                return; // Isekai RPG Leveling not installed
            }

            // IsekaiMod.Settings is a static property returning the IsekaiSettings instance.
            object settings = AccessTools.PropertyGetter(modType, "Settings")?.Invoke(null, null);
            if (settings == null)
            {
                return;
            }

            Type settingsType = settings.GetType();
            AccessTools.Field(settingsType, "XPMultiplier")?.SetValue(settings, XpMultiplier);
            AccessTools.Field(settingsType, "SkillPointsPerLevel")?.SetValue(settings, SkillPointsPerLevel);
        }
    }
}
