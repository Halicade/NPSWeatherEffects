using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NPSWeather;

//pawns will go sit in cold springs to cool off if there is no better option and there is one nearby
[HarmonyPatch(typeof(JobGiver_SeekSafeTemperature), "TryGiveJob")]
internal class JobGiver_SeekSafeTemperature_TryGiveJob
{
    public static void Postfix(ref Job __result, Pawn pawn) {
        if (__result != null || pawn?.RaceProps?.CanPassFences != true) {
            return;
        }

        Map map = pawn.Map;

        if (map.Biome == BiomeDefOf.TKKN_Oasis ||
            map.TileInfo.mutatorsNullable?.Contains(TileMutatorDefOf.NPS_ColdSpringMutator) == true) {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Heatstroke)?.CurStageIndex !=
                (int)TemperatureInjuryStage.Serious) {
                return;
            }

            if (pawn.Position.GetTerrain(Find.CurrentMap) == TerrainDefOf.TKKN_ColdSpringsWater) {
                __result = new Job(JobDefOf.Wait_SafeTemperature, 500, true);
                return;
            }

            if (CellFinder.TryRandomClosewalkCellNear(
                    root: pawn.Position,
                    map: map,
                    radius: 80,
                    result: out var result,
                    extraValidator: vec3 => vec3.GetTerrain(map) == TerrainDefOf.TKKN_ColdSpringsWater)) {
                __result = new Job(JobDefOf.GotoSafeTemperature, result);
            }
        }
    }
}