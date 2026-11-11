using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace NPSWeather;

[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
public static class UIRootEntry_Init
{
    private static bool _didWarningCheck = false;

    public static void Prefix() {
        if (_didWarningCheck) {
            return;
        }

        MethodInfo methodSearch = typeof(TerrainGrid).GetMethod(nameof(TerrainGrid.SetTerrain));
        Patches patchSearch = Harmony.GetPatchInfo(methodSearch);

        if (patchSearch != null) {
            EffectSettings.modsPatchingTerrain = patchSearch.Owners.Count;
            string terrainPatchingMods = getPatchNames(patchSearch.Owners.ToList());
            
            Log.Warning($"NPSWeatherEffects: Detected {patchSearch.Owners.Count} mod(s) patching 'TerrainGrid.SetTerrain' " +
                        $"It is advised to turn off 'terrain effects' if this leads to considerable performance impact. " +
                        $"These come from the following harmony IDs: {terrainPatchingMods}");
        }

        MethodInfo genTempMethod = typeof(GenTemperature).GetMethod(nameof(GenTemperature.GetTemperatureForCell));
        Patches genTempPatches = Harmony.GetPatchInfo(genTempMethod);
        MethodInfo tryGetTemperatureMethod =
            typeof(GenTemperature).GetMethod(nameof(GenTemperature.TryGetTemperatureForCell));
        Patches tryGetTemperaturePatches = Harmony.GetPatchInfo(tryGetTemperatureMethod);

        if (EffectSettings.terrainAffectTemperature || EffectSettings.useMapTemperature) {
            List<string> temperaturePatchingMods;
            if (genTempPatches != null && tryGetTemperaturePatches != null) {
                temperaturePatchingMods =
                [
                    .. genTempPatches.Owners.Union(tryGetTemperaturePatches.Owners)
                ];
                EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;
                Log.Warning($"NPSWeatherEffects: Detected {genTempPatches.Owners.Count} mod(s) patching 'GenTemperature.GetTemperatureForCell' " +
                            $"and {tryGetTemperaturePatches.Owners.Count} mod(s) patching 'GenTemperature.GetTemperatureForCell'. " +
                            $"It is advised to turn on 'use map-wide temperature' if this leads to considerable performance impact. " +
                            $"These come from the following harmony IDs: {getPatchNames(temperaturePatchingMods)}");
            }
            else {
                if (genTempPatches != null) {
                    temperaturePatchingMods =
                    [
                        .. genTempPatches.Owners
                    ];
                    EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;
                    Log.Warning($"NPSWeatherEffects: Detected {genTempPatches.Owners.Count} mod(s) patching 'GenTemperature.GetTemperatureForCell' " +
                                $"It is advised to turn on 'use map-wide temperature' if this leads to considerable performance impact. " +
                                $"These come from the following harmony IDs: {getPatchNames(temperaturePatchingMods)}");
                }
                else if (tryGetTemperaturePatches != null) {
                    temperaturePatchingMods =
                    [
                        .. tryGetTemperaturePatches.Owners
                    ];
                    EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;
                    Log.Warning($"NPSWeatherEffects: Detected {tryGetTemperaturePatches.Owners.Count} mod(s) patching 'GenTemperature.TryGetTemperatureForCell' " +
                                $"It is advised to turn on 'use map-wide temperature' if this leads to considerable performance impact. " +
                                $"These come from the following harmony IDs: {getPatchNames(temperaturePatchingMods)}");
                }
            }
        }

        if (HarmonyWeatherEffects.OceanModifyingModActive && EffectSettings.doTides ||
            HarmonyWeatherEffects.RiverModifyingModActive && EffectSettings.doFloods) {
            Log.Warning(
                "NPSWeatherEffects: You currently have a mod active that visually changes water terrain. It is advised to turn off the affected setting to prevent graphical issues.");
        }

        _didWarningCheck = true;
    }

    private static string getPatchNames(List<string> patches) {
        StringBuilder patchNames = new StringBuilder();
        foreach (string owner in patches) {
            patchNames.Append(owner).Append(",\t");
        }

        return patchNames.ToString();
    }
}