using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ImprovedDefaults
{
    // Default Planning Extended (Scherub.PlanningExtended) plans to HIDDEN on every load.
    //
    // That mod's "Startup plan visibility" option defaults to Visible, and it's a machine-local mod
    // setting - editing the config only fixes it on one PC, it doesn't travel with the modlist. So we
    // set the default from here, so everyone using this collection gets it. Reflection-only and gated on
    // the type existing, so Improved Defaults still loads fine without Planning Extended (soft dep).
    //
    // Stays editable: if the player has explicitly chosen the option, their settings file already
    // contains the <startupPlanVisibility> node, and we leave their choice untouched. Only when there's
    // no explicit choice do we set the session default to Invisible. Runs at startup, before any game
    // loads and applies plan visibility.
    [StaticConstructorOnStartup]
    public static class PlanVisibilityDefaultPatch
    {
        static PlanVisibilityDefaultPatch()
        {
            try
            {
                Apply();
            }
            catch (Exception ex)
            {
                Log.Warning("[Improved Defaults] Could not set Planning Extended startup visibility default: " + ex.Message);
            }
        }

        private static void Apply()
        {
            Type modType = AccessTools.TypeByName("PlanningExtended.PlanningMod");
            Type settingsType = AccessTools.TypeByName("PlanningExtended.Settings.PlanningSettings");
            Type enumType = AccessTools.TypeByName("PlanningExtended.StartupPlanVisibility");
            if (modType == null || settingsType == null || enumType == null)
            {
                return; // Planning Extended not installed
            }

            // Respect an explicit player choice: if the settings file already has the node, do nothing.
            ModContentPack pack = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(m => string.Equals(m.PackageId, "scherub.planningextended", StringComparison.OrdinalIgnoreCase));
            if (pack != null)
            {
                string path = Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_" + pack.FolderName + "_PlanningMod.xml");
                if (File.Exists(path) && File.ReadAllText(path).Contains("<startupPlanVisibility>"))
                {
                    return;
                }
            }

            object settings = AccessTools.PropertyGetter(modType, "Settings")?.Invoke(null, null);
            FieldInfo field = AccessTools.Field(settingsType, "startupPlanVisibility");
            if (settings == null || field == null)
            {
                return;
            }
            field.SetValue(settings, Enum.Parse(enumType, "Invisible"));
        }
    }
}
