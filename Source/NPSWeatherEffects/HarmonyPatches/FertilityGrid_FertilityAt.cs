using Verse;

namespace NPSWeather;

/// <summary>
/// This method feels too expensive to use
/// </summary>
public class FertilityGrid_FertilityAt
{
    private static Map _cachedMap;
    private static Watcher _cachedWatcher;

    public static void Postfix(Map ___map, IntVec3 loc, ref float __result) {
        if (___map != _cachedMap) {
            if (EffectSettings.onlyPlayerHome && 
                ___map?.IsPlayerHome != true) {
                return;
            }

            _cachedMap = ___map;
            _cachedWatcher = _cachedMap.GetComponent<Watcher>();
        }

        if (_cachedWatcher.isRaining) {
            if (_cachedWatcher.cellWeatherAffects.TryGetValue(loc, out var rainIncrease)) {
                __result *= 1 + rainIncrease.rainLevel;
            }
        }
    }
}