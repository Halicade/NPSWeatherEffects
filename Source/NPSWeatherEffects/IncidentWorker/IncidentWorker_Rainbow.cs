using RimWorld;
using Verse;

namespace NPSWeather;

public class IncidentWorker_Rainbow : IncidentWorker_MakeGameCondition
{
    private const int EnsureMinDurationTicks = 5000;

    protected override bool CanFireNowSub(IncidentParms parms) {
        Log.Message("IncidentWorker_Rainbow CanFireNowSub");
        if (!base.CanFireNowSub(parms)) {
            Log.Message("Failed at the beginning");
            return false;
        }

        foreach (Map map in Find.Maps) {
            if (!map.IsPlayerHome || map.GameConditionManager.IsAlwaysDarkOutside || RainWillNotEndSoon(map)) {
                return false;
            }
        }

        return false;
    }

    private bool RainWillNotEndSoon(Map map) {
        if (map.weatherManager.RainRate > 0.3) {
            Log.Message("rain rate more than 0.3");
            return false;
        }

        if (map.weatherManager.lastWeather.rainRate == 0) {
            Log.Message("Last rain rate was 0");
            return false;
        }


        if (GenCelestial.CurCelestialSunGlow(map) > 0.5f) {
            Log.Message("sub glow more than 0.5");
            return true;
        }

        if (GenCelestial.CelestialSunGlow(map, Find.TickManager.TicksAbs + EnsureMinDurationTicks) > 0.3f) {
            Log.Message("Sun glow in the future is more than 0.3");
            return true;
        }

        Log.Message("It was just false");
        return false;
    }
}