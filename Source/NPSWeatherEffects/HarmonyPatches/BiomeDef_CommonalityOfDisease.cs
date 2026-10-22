using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NPSWeather;

[HarmonyPatch(typeof(BiomeDef), nameof(BiomeDef.CommonalityOfDisease))]
public static class BiomeDef_CommonalityOfDisease
{
    public static void Prefix(BiomeDef __instance, ref Dictionary<IncidentDef, float> ___cachedDiseaseCommonalities) {
        var biomeSettings = __instance.GetModExtension<BiomeSeasonalSettings>();
        /*
        // Saving this so I don't have a 3rd repeat of this happening
        Log.Warning("Looking at disease commonalities for " + __instance + " currently null? " + (___cachedDiseaseCommonalities == null));
        if (___cachedDiseaseCommonalities == null) {
            return;
        }

        foreach (KeyValuePair<IncidentDef, float> diseaseDict in ___cachedDiseaseCommonalities) {
            Log.Message(diseaseDict.Key + ": " + diseaseDict.Value);
        }
        */
        if (biomeSettings == null || biomeSettings.diseaseCacheUpdated) {
            return;
        }

        ___cachedDiseaseCommonalities = null;
        biomeSettings.diseaseCacheUpdated = true;
    }
}