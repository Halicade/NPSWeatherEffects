using HarmonyLib;
using Verse;

namespace NPSWeather;

//[HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
internal class Thing_AmbientTemperature
{

    public static void Postfix(Thing __instance, ref float __result) {
        if (__instance.Spawned) {
            var map = __instance.Map;
            IntVec3 c = __instance.Position;
            if (!c.InBounds(map)) {
                return;
            }
            //check if we should have temperature affected by contact with terrain
            TerrainTagUtil.AmbientTempReaction.TryGetValue(c.GetTerrain(map), out var reaction);
            __result += reaction;
        }
        
    }
} 