using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using NPSWeather;
using Verse;

namespace TKKN_NPS;

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
            Log.Warning("NPS_DetectedTerrainGridPatches".Translate(
                patchSearch.Owners.Count,
                getPatchNames(patchSearch.Owners.ToList())));
        }

        MethodInfo genTempMethod = typeof(GenTemperature).GetMethod(nameof(GenTemperature.GetTemperatureForCell));
        Patches genTempPatches = Harmony.GetPatchInfo(genTempMethod);
        MethodInfo tryGetTemperatureMethod =
            typeof(GenTemperature).GetMethod(nameof(GenTemperature.TryGetTemperatureForCell));
        Patches tryGetTemperaturePatches = Harmony.GetPatchInfo(tryGetTemperatureMethod);
        List<string> temperaturePatchingMods;


        if (genTempPatches != null && tryGetTemperaturePatches != null) {
            temperaturePatchingMods = genTempPatches.Owners.Union(tryGetTemperaturePatches.Owners).ToList();
            EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;

            Log.Warning("NPS_BothTemperaturePatches".Translate(
                genTempPatches.Owners.Count,
                tryGetTemperaturePatches.Owners.Count,
                getPatchNames(temperaturePatchingMods)));
        }
        else {
            if (genTempPatches != null) {
                temperaturePatchingMods = genTempPatches.Owners.ToList();
                EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;

                Log.Warning("NPS_BothTemperaturePatches".Translate(
                    genTempPatches.Owners.Count,
                    "0",
                    getPatchNames(temperaturePatchingMods)));
            }
            else if (tryGetTemperaturePatches != null) {
                temperaturePatchingMods = tryGetTemperaturePatches.Owners.ToList();
                EffectSettings.modsPatchingTemperature = temperaturePatchingMods.Count;

                Log.Warning("NPS_BothTemperaturePatches".Translate(
                    "0",
                    tryGetTemperaturePatches.Owners.Count,
                    getPatchNames(temperaturePatchingMods)));
            }
        }

        _didWarningCheck = true;
    }

    private static string getPatchNames(List<string> patches) {
        StringBuilder patchNames = new StringBuilder();
        foreach (var owner in patches) {
            patchNames.Append(owner).Append(",\t");
        }

        return patchNames.ToString();
    }
}