using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NPSWeather;

public class BiomeSeasonalSettings : DefModExtension
{
    //incident settings
    public List<ThingDef> bloomPlants;
    public List<PawnKindDef> specialHerds;

    //spring settings
    public int maxSprings = 1;
    public float springSpawnChance = 0;

    //tide setting
    public float tideFactor = 1;

    public bool activeForBiome = true;

    //disease settings
    public List<BiomeDiseaseRecord> springDiseases;
    public List<BiomeDiseaseRecord> summerDiseases;
    public List<BiomeDiseaseRecord> fallDiseases;
    public List<BiomeDiseaseRecord> winterDiseases;

    public List<TKKN_IncidentCommonalityRecord> springEvents;
    public List<TKKN_IncidentCommonalityRecord> summerEvents;
    public List<TKKN_IncidentCommonalityRecord> fallEvents;
    public List<TKKN_IncidentCommonalityRecord> winterEvents;


    //weather settings
    public List<WeatherCommonalityRecord> springWeathers;
    public List<WeatherCommonalityRecord> summerWeathers;
    public List<WeatherCommonalityRecord> fallWeathers;
    public List<WeatherCommonalityRecord> winterWeathers;

    //internal use
    public bool diseaseCacheUpdated;

    public void setWeatherBySeason(Map map, Season season, Quadrum quadrum, bool northernLatitude) {
        if (!EffectSettings.seasonalWeather) {
            return;
        }

        if (springWeathers.NullOrEmpty() ||
            summerWeathers.NullOrEmpty() ||
            fallWeathers.NullOrEmpty() ||
            winterWeathers.NullOrEmpty()) {
            return;
        }
        // Quadrum is used if we're on a permanent summer/winter map.
        // This helps give the illusion of changing seasons

        switch (season) {
            case Season.Spring:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, springWeathers);
                break;
            case Season.Summer:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, summerWeathers);
                break;
            case Season.Fall:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, fallWeathers);
                break;
            case Season.Winter:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, winterWeathers);
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default:
            {

                switch (quadrum) {
                    case Quadrum.Aprimay:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, northernLatitude ? springWeathers : fallWeathers);
                        break;
                    case Quadrum.Jugust:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, northernLatitude ? summerWeathers : winterWeathers);
                        break;
                    case Quadrum.Septober:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, northernLatitude ? fallWeathers : springWeathers);
                        break;
                    case Quadrum.Decembary:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, northernLatitude ? winterWeathers : summerWeathers);
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPSWeatherEffects: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            1586 ^ map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        if (map.Biome.baseWeatherCommonalities?.Any() == false) {
            Log.Error(
                $"NPSWeatherEffects: Something broke horribly and despite the there are no weather settings for the current map. Please report this. biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}");
            map.Biome.baseWeatherCommonalities =
            [
                new WeatherCommonalityRecord { commonality = 1f, weather = WeatherDefOf.Clear }
            ];
        }
    }

    /// <summary>
    /// Set weather commonality without overriding any weathers that are not listed
    /// </summary>
    private static void setWeatherCommonalities(List<WeatherCommonalityRecord> baseWeatherCommonalities, BiomeDef biome,
        List<WeatherCommonalityRecord> newCommonalities) {
        if (!BiomeUtil.OriginalBiomeWeather.TryGetValue(biome, out List<WeatherCommonalityRecord> originalRecords))
            return;
        originalRecords.CopyToList(baseWeatherCommonalities);
        foreach (WeatherCommonalityRecord record in baseWeatherCommonalities) {
            foreach (WeatherCommonalityRecord newWeathers in newCommonalities) {
                if (record.weather == newWeathers.weather) {
                    record.commonality = newWeathers.commonality;
                    break;
                }
            }
        }
    }

    public void setDiseaseBySeason(Map map, Season season, Quadrum quadrum, bool northernLatitude) {
        if (!EffectSettings.seasonalDiseases) {
            return;
        }

        if (springDiseases.NullOrEmpty() ||
            summerDiseases.NullOrEmpty() ||
            fallDiseases.NullOrEmpty() ||
            winterDiseases.NullOrEmpty()) {
            return;
        }

        List<BiomeDiseaseRecord> seasonalDiseases;
        switch (season) {
            case Season.Spring:
                seasonalDiseases = springDiseases;
                break;
            case Season.Summer:
                seasonalDiseases = summerDiseases;
                break;
            case Season.Fall:
                seasonalDiseases = fallDiseases;
                break;
            case Season.Winter:
                seasonalDiseases = winterDiseases;
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default:
            {
                switch (quadrum) {
                    case Quadrum.Aprimay:

                        seasonalDiseases = northernLatitude ? springDiseases : fallDiseases;
                        break;
                    case Quadrum.Jugust:
                        seasonalDiseases = northernLatitude ? summerDiseases : winterDiseases;
                        break;
                    case Quadrum.Septober:
                        seasonalDiseases = northernLatitude ? fallDiseases : springDiseases;
                        break;
                    case Quadrum.Decembary:
                        seasonalDiseases = northernLatitude ? winterDiseases : summerDiseases;
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPSWeatherEffects: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            456 ^ map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        foreach (var diseaseRec in seasonalDiseases) {
            var disease = diseaseRec.diseaseInc;
            disease.baseChance = diseaseRec.commonality;
        }

        diseaseCacheUpdated = false;
    }

    public void setIncidentsBySeason(Map map, Season season, Quadrum quadrum, bool northernLatitude) {
        if (!EffectSettings.seasonalIncidents) {
            return;
        }

        if (springEvents.NullOrEmpty() ||
            summerEvents.NullOrEmpty() ||
            fallEvents.NullOrEmpty() ||
            winterEvents.NullOrEmpty()) {
            return;
        }

        List<TKKN_IncidentCommonalityRecord> seasonalIncidents;
        switch (season) {
            case Season.Spring:
                seasonalIncidents = springEvents;
                break;
            case Season.Summer:
                seasonalIncidents = summerEvents;
                break;
            case Season.Fall:
                seasonalIncidents = fallEvents;
                break;
            case Season.Winter:
                seasonalIncidents = winterEvents;
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default:
            {
                switch (quadrum) {
                    case Quadrum.Aprimay:
                        seasonalIncidents = northernLatitude ? springEvents : fallEvents;
                        break;
                    case Quadrum.Jugust:
                        seasonalIncidents = northernLatitude ? summerEvents : winterEvents;
                        break;
                    case Quadrum.Septober:
                        seasonalIncidents = northernLatitude ? fallEvents : springEvents;
                        break;
                    case Quadrum.Decembary:
                        seasonalIncidents = northernLatitude ? winterEvents : summerEvents;
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPSWeatherEffects: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            4367 ^ map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        foreach (var incidentRate in seasonalIncidents) {
            var incident = incidentRate.incident;
            incident.baseChance = incidentRate.commonality;
        }
    }
}