using System.Collections.Frozen;
using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public static class TerrainTagUtil
{
    private static readonly HashSet<TerrainDef> HashTKKN_Wet = [];
    private static readonly HashSet<TerrainDef> HashTKKN_Swim = [];
    private static readonly HashSet<TerrainDef> HashLava = [];
    private static readonly HashSet<TerrainDef> HashCanBePacked = [];
    private static readonly HashSet<TerrainDef> HashTide = [];
    private static HashSet<TerrainDef> HashTideHasTempTide = [];
    private static readonly HashSet<TerrainDef> HashRiver = [];
    private static HashSet<TerrainDef> HashRiverToTempRiver = [];

    private static readonly Dictionary<TerrainDef, float> HashAmbientTempReaction = [];

    public static FrozenSet<TerrainDef> TKKN_Wet = [];
    public static FrozenSet<TerrainDef> TKKN_Swim = [];
    public static FrozenSet<TerrainDef> Lava = [];
    public static FrozenSet<TerrainDef> CanBePacked = [];
    public static FrozenSet<TerrainDef> NPS_Tide = [];
    public static FrozenSet<TerrainDef> TideHasTempTide = [];
    public static FrozenSet<TerrainDef> NPS_River = [];
    public static FrozenSet<TerrainDef> RiverHasTempRiver = [];
    public static FrozenDictionary<TerrainDef, float> AmbientTempReaction = [];

    public static void InitializeTerrainTags() {
        List<TerrainDef> allTerrains = DefDatabase<TerrainDef>.AllDefsListForReading;
        HashCanBePacked.Add(RimWorld.TerrainDefOf.Soil);
        HashCanBePacked.Add(RimWorld.TerrainDefOf.Sand);
        HashCanBePacked.Add(TerrainDefOf.TKKN_DirtPath);
        HashCanBePacked.Add(TerrainDefOf.TKKN_SandPath);

        foreach (var terrain in allTerrains) {
            if (terrain.HasTag("TKKN_Wet") &&
                terrain.waterBodyType is WaterBodyType.Freshwater or WaterBodyType.Saltwater) {
                HashTKKN_Wet.Add(terrain);
            }

            if (terrain.HasTag("TKKN_Swim")) {
                HashTKKN_Swim.Add(terrain);
            }

            if (terrain.HasTag("Lava") || terrain.HasTag("TKKN_Lava")) {
                HashLava.Add(terrain);
            }

            if (terrain.smoothedTerrain != null) {
                HashCanBePacked.Add(terrain);
            }

            if (terrain.temporary) {
                if (terrain.HasTag("NPS_Tide")) {
                    HashTide.Add(terrain);
                }

                if (terrain.HasTag("NPS_River")) {
                    HashRiver.Add(terrain);
                }
            }


            var weatherExtension = terrain.GetModExtension<TerrainWeatherReactions>();
            if (weatherExtension != null) {
                if (weatherExtension.temperatureAdjust != 0) {
                    HashAmbientTempReaction.Add(terrain, weatherExtension.temperatureAdjust);
                }

                if (weatherExtension.freezeTerrain?.terrain is { temporary: false }) {
                    Log.Error(
                        $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.freezeTerrain} is a freeze terrain. But it is not temporary.");
                }

                if (weatherExtension.tideTerrain is { } tideTerrain ) {
                    if (!tideTerrain.HasTag("NPS_Tide")) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.tideTerrain} is a tide terrain. But it does not have the \"NPS_Tide\" tag.");
                        continue;
                    }

                    if (!tideTerrain.temporary) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.tideTerrain} is a tide terrain. But it is not temporary.");
                        continue;
                    }
                    
                    
                    HashTideHasTempTide.Add(terrain);
                    
                }

                if (weatherExtension.riverTerrain is { } riverTerrain ) {
                    if (!riverTerrain.HasTag("NPS_River")) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.riverTerrain} is a river terrain. But it does not have the \"NPS_River\" tag.");
                        continue;
                    }

                    if (!riverTerrain.temporary) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.riverTerrain} is a river terrain. But it is not temporary.");
                        continue;
                    }
                    
                    HashRiverToTempRiver.Add(terrain);
                }

            }
        }

        TKKN_Wet = HashTKKN_Wet.ToFrozenSet();
        TKKN_Swim = HashTKKN_Swim.ToFrozenSet();
        Lava = HashLava.ToFrozenSet();
        CanBePacked = HashCanBePacked.ToFrozenSet();
        NPS_Tide = NPS_Tide.ToFrozenSet();
        NPS_River = NPS_River.ToFrozenSet();
        TideHasTempTide = HashTideHasTempTide.ToFrozenSet();
        RiverHasTempRiver = HashRiverToTempRiver.ToFrozenSet();
        AmbientTempReaction = HashAmbientTempReaction.ToFrozenDictionary();
        // Don't need the original dicts
        HashTKKN_Wet.Clear();
        HashTKKN_Swim.Clear();
        HashLava.Clear();
        HashCanBePacked.Clear();
        HashAmbientTempReaction.Clear();
    }
}