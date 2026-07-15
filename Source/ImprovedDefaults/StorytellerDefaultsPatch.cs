using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedDefaults
{
    // Preset the Choose AI Storyteller screen: The Antagonist / Blood and dust / Reload anytime / Anomaly disabled.
    //
    // Page_SelectStoryteller.PreOpen only defaults the storyteller (lowest listOrder = Cassandra) and
    // leaves difficulty + permadeath unset, forcing the player to click them every new game. The screen
    // reads its selection straight from the page's private fields (storyteller, difficulty,
    // difficultyValues) and from GameInitData's permadeath flags, so setting those in a PreOpen Postfix
    // makes each option appear pre-selected while staying fully changeable.
    //
    // Difficulty "blood and dust" = DifficultyDef "Hard"; the anomaly playstyle lives on the Difficulty
    // object (difficultyValues.AnomalyPlaystyleDef) and is only meaningful with the Anomaly DLC.
    [HarmonyPatch(typeof(Page_SelectStoryteller), nameof(Page_SelectStoryteller.PreOpen))]
    public static class Patch_Page_SelectStoryteller_PreOpen
    {
        private const string StorytellerDefName = "WR_TheAntagonist"; // The Antagonist (our custom storyteller); falls back to Cassandra if that mod isn't loaded
        private const string DifficultyDefName = "Hard"; // "blood and dust"
        private const string AnomalyPlaystyleDefName = "Disabled"; // "anomaly incidents disabled"

        private static readonly FieldInfo StorytellerField =
            AccessTools.Field(typeof(Page_SelectStoryteller), "storyteller");
        private static readonly FieldInfo DifficultyField =
            AccessTools.Field(typeof(Page_SelectStoryteller), "difficulty");
        private static readonly FieldInfo DifficultyValuesField =
            AccessTools.Field(typeof(Page_SelectStoryteller), "difficultyValues");

        public static void Postfix(Page_SelectStoryteller __instance)
        {
            StorytellerDef storyteller = DefDatabase<StorytellerDef>.GetNamedSilentFail(StorytellerDefName);
            if (storyteller != null)
            {
                StorytellerField?.SetValue(__instance, storyteller);
            }

            DifficultyDef difficulty = DefDatabase<DifficultyDef>.GetNamedSilentFail(DifficultyDefName);
            if (difficulty != null)
            {
                var difficultyValues = new Difficulty(difficulty);

                if (ModsConfig.AnomalyActive)
                {
                    AnomalyPlaystyleDef playstyle =
                        DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail(AnomalyPlaystyleDefName);
                    if (playstyle != null)
                    {
                        difficultyValues.AnomalyPlaystyleDef = playstyle;
                    }
                }

                DifficultyField?.SetValue(__instance, difficulty);
                DifficultyValuesField?.SetValue(__instance, difficultyValues);
            }

            // Reload anytime mode (permadeath off). Mark it chosen so the radio shows selected and the
            // page's "must choose a mode" gate is satisfied.
            if (Find.GameInitData != null)
            {
                Find.GameInitData.permadeathChosen = true;
                Find.GameInitData.permadeath = false;
            }
        }
    }
}
