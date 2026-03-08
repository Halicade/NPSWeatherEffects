using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public static class TerrainTagUtil
{
    public static readonly HashSet<TerrainDef> TKKN_Wet = [];
    public static readonly HashSet<TerrainDef> TKKN_Swim = [];
    public static readonly HashSet<TerrainDef> Lava = [];
    public static readonly HashSet<TerrainDef> CanBePacked = [];
    public static readonly HashSet<TerrainDef> SaltTerrains = [];
    public static readonly Dictionary<TerrainDef, float> AmbientTempReaction = [];

    public static void IntializeTerrainTags() {
        List<TerrainDef> allTerrains = DefDatabase<TerrainDef>.AllDefsListForReading;
        CanBePacked.Add(RimWorld.TerrainDefOf.Soil);
        CanBePacked.Add(RimWorld.TerrainDefOf.Sand);
        CanBePacked.Add(TerrainDefOf.TKKN_DirtPath);
        CanBePacked.Add(TerrainDefOf.TKKN_SandPath);

        foreach (var terrain in allTerrains) {
            if (terrain.HasTag("TKKN_Wet")) {
                TKKN_Wet.Add(terrain);
            }

            if (terrain.HasTag("TKKN_Swim")) {
                TKKN_Swim.Add(terrain);
            }

            if (terrain.HasTag("Lava") || terrain.HasTag("TKKN_Lava")) {
                Lava.Add(terrain);
            }

            if (terrain.smoothedTerrain != null) {
                CanBePacked.Add(terrain);
            }
            

            var weatherExtension = terrain.GetModExtension<TerrainWeatherReactions>();
            if (weatherExtension != null) {

                if (weatherExtension.temperatureAdjust != 0) {
                    AmbientTempReaction.Add(terrain, weatherExtension.temperatureAdjust);
                }

                if (weatherExtension.freezeTerrain?.terrain != null) {
                    if (!weatherExtension.freezeTerrain.terrain.temporary) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.freezeTerrain} is a freeze terrain. But it is not temporary. ");
                    }
                }

                if (weatherExtension.isSalty) {
                    SaltTerrains.Add(terrain);
                }
            }
        }
    }
}