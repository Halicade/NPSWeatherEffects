using RimWorld;
using Verse;

namespace NPSWeather;

public class IncidentWorker_Bloom : IncidentWorker_MakeGameCondition
{
    private bool relevantSetting = EffectSettings.allowPlantEffects;

    protected override bool CanFireNowSub(IncidentParms parms) {
        if (!base.CanFireNowSub(parms)) {
            return false;
        }

        foreach (var map in Find.Maps) {
            if (!map.IsPlayerHome) {
                continue;
            }

            //can the biome support it?
            var biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
            if (biomeSettings == null) {
                continue;
            }

            if (biomeSettings.bloomPlants.NullOrEmpty()) {
                continue;
            }

            if (map.mapTemperature.OutdoorTemp < 10f) {
                continue;
            }
            
            if (map.fireWatcher.LargeFireDangerPresent) {
                continue;
            }

            if (GenDate.Season(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile)) == Season.Spring)
                return true;
        }


        return false;
    }
}