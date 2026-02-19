using System.Reflection;
using System.Text;
using HarmonyLib;
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
        StringBuilder warningPatches = new StringBuilder();

        if (patchSearch != null) {
            warningPatches.Append("NPS_DetectedTerrainGridPatches".Translate(patchSearch.Owners.Count));
            getPatchNames(warningPatches, patchSearch);

            Log.Warning(warningPatches.ToString());
            warningPatches.Clear();
        }

        MethodInfo genTempMethod = typeof(GenTemperature).GetMethod(nameof(GenTemperature.GetTemperatureForCell));
        Patches genTempPatches = Harmony.GetPatchInfo(genTempMethod);
        MethodInfo tryGetTemperatureMethod =
            typeof(GenTemperature).GetMethod(nameof(GenTemperature.TryGetTemperatureForCell));
        Patches tryGetTemperaturePatches = Harmony.GetPatchInfo(tryGetTemperatureMethod);

        //Yes this is hideous, no I don't care
        if (genTempPatches != null && tryGetTemperaturePatches != null) {
            warningPatches.Append("NPS_DetectedStart".Translate())
                .Append("NPS_GetTemperaturePatches".Translate(genTempPatches.Owners.Count))
                .Append("NPS_And".Translate())
                .Append("NPS_TryGetTemperaturePatches".Translate(tryGetTemperaturePatches.Owners.Count))
                .Append("NPS_AdviseTurnOn".Translate());
            getPatchNames(warningPatches, genTempPatches);
            getPatchNames(warningPatches, tryGetTemperaturePatches);
            Log.Warning(warningPatches.ToString());
        }
        else {
            if (genTempPatches != null) {
                warningPatches.Append("NPS_DetectedStart".Translate())
                    .Append("NPS_GetTemperaturePatches".Translate(genTempPatches.Owners.Count))
                    .Append("NPS_AdviseTurnOn".Translate());
                getPatchNames(warningPatches, genTempPatches);
                Log.Warning(warningPatches.ToString());
            }
            else if (tryGetTemperaturePatches != null) {
                warningPatches.Append("NPS_DetectedStart".Translate())
                    .Append("NPS_TryGetTemperaturePatches".Translate(tryGetTemperaturePatches.Owners.Count))
                    .Append("NPS_AdviseTurnOn".Translate());
                getPatchNames(warningPatches, tryGetTemperaturePatches);
                Log.Warning(warningPatches.ToString());
            }
        }

        _didWarningCheck = true;
    }

    private static void getPatchNames(StringBuilder builderText, Patches patches) {
        foreach (var owner in patches.Owners) {
            builderText.Append(owner).Append(",\t");
        }
    }
}