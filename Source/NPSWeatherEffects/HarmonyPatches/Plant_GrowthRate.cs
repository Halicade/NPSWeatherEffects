using HarmonyLib;
using RimWorld;
using Verse;

namespace NPSWeather;

[HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRate), MethodType.Getter)]
public class Plant_GrowthRate
{
    static bool Prepare() => EffectSettings.showRainEffects && EffectSettings.rainIncreaseFertility;

    private static Map cachedMap;
    private static Watcher cachedWatcher;

    public static void Postfix(Plant __instance, ref float __result) {
        if (__result == 0) {
            return;
        }

        if (__instance.Map != cachedMap) {
            cachedMap = __instance.Map;
            cachedWatcher = cachedMap.GetComponent<Watcher>();
        }

        if (cachedWatcher.dontRunAnything) {
            return;
        }

        if (cachedWatcher.isRaining) {
            if (cachedWatcher.cellWeatherAffects.TryGetValue(__instance.Position, out var rainIncrease)) {
                __result *= 1 + rainIncrease.rainLevel / 2;
            }
        }
    }
}