using System.Linq;
using System.Text;
using NPSWeather;
using RimWorld;
using Verse;

namespace NPSWeatherEffects;

public class Plant_GrowthRateCalcDesc
{
    public static Map cachedMap;
    public static Watcher cachedWatcher;


    public static void Postfix(Plant __instance, ref string __result) {
        if (__instance.Map != cachedMap) {
            cachedMap = __instance.Map;
            cachedWatcher = cachedMap.GetComponent<Watcher>();
        }

        if (cachedWatcher.isRaining) {
            if (cachedWatcher.cellWeatherAffects.TryGetValue(__instance.Position, out var rainIncrease)) {
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