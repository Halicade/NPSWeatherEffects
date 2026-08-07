using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NPSWeather;

[HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRateCalcDesc), MethodType.Getter)]
public class Plant_GrowthRateCalcDesc
{
    static bool Prepare() => EffectSettings.showRainEffects && EffectSettings.rainIncreaseFertility;

    private static Map _cachedMap;
    private static Watcher _cachedWatcher;


    public static void Postfix(Plant __instance, ref string __result) {
        if (__instance.Map != _cachedMap) {
            _cachedMap = __instance.Map;
            _cachedWatcher = _cachedMap.GetComponent<Watcher>();
        }

        if (_cachedWatcher.isRaining) {
            if (_cachedWatcher.cellWeatherAffects.TryGetValue(__instance.Position, out var rainIncrease)) {
                StringBuilder plantString = new StringBuilder();
                plantString.Append(__result);
                plantString.AppendInNewLine("StatsReport_MultiplierFor".Translate("NPS_RainIncrease".Translate()) +
                                            ": " +
                                            (1 + rainIncrease.rainLevel / 2).ToStringPercent());
                __result = plantString.ToString();
            }
        }
    }
}