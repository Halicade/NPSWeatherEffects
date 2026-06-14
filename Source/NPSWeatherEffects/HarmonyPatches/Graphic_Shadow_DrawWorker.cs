using HarmonyLib;
using Verse;

namespace NPSWeather;

//[HarmonyPatch(typeof(Graphic_Shadow), nameof(Graphic_Shadow.DrawWorker))]
public static class Graphic_Shadow_DrawWorker
{
    public static bool Prefix(Thing thing)
    {
        if (thing is not Pawn pawn)
        {
            return true;
        }

        if (!pawn.RaceProps.Humanlike || !pawn.Position.IsValid)
        {
            return true;
        }

        var terrain = pawn.Position.GetTerrain(pawn.MapHeld);
        return !TerrainTagUtil.NPS_DeepWater.Contains(terrain);
    }
}