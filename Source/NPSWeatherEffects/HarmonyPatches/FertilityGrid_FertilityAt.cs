using NPSWeather;
using Verse;

namespace NPSWeatherEffects;


/// <summary>
/// This method feels too expensive to use
/// </summary>
public class FertilityGrid_FertilityAt
{
    public static Map cachedMap;
    public static Watcher cachedWatcher;
    
    public static void Postfix(Map ___map, IntVec3 loc, ref float __result) {
        if (___map != cachedMap) {
            cachedMap = ___map;
            cachedWatcher = cachedMap.GetComponent<Watcher>();
        }

        if (cachedWatcher.isRaining) {
            if (cachedWatcher.cellWeatherAffects.TryGetValue(loc, out var rainIncrease)) {
                __result *= 1 + rainIncrease.rainLevel;
            }
            
        }
    }
}