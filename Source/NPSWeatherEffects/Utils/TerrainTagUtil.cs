using System.Collections.Frozen;
using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public static class TerrainTagUtil
{
    private static readonly HashSet<TerrainDef> HashNPS_Water = [];
    private static readonly HashSet<TerrainDef> HashNPS_DeepWater = [];
    private static readonly HashSet<TerrainDef> HashLava = [];
    private static readonly HashSet<TerrainDef> HashCanBePacked = [];
    private static readonly HashSet<TerrainDef> HashTide = [];
    private static HashSet<TerrainDef> HashTideHasTempTide = [];
    private static readonly HashSet<TerrainDef> HashRiver = [];
    private static HashSet<TerrainDef> HashRiverToTempRiver = [];

    private static readonly Dictionary<TerrainDef, float> HashAmbientTempReaction = [];

    public static FrozenSet<TerrainDef> NPS_Water = [];
    public static FrozenSet<TerrainDef> NPS_DeepWater = [];
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
            if (terrain.HasTag("NPS_Water") || terrain.IsWater) {
                HashNPS_Water.Add(terrain);
            }

            if (terrain.HasTag("NPS_DeepWater")) {
                HashNPS_DeepWater.Add(terrain);
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

                if (weatherExtension.freezeTerrain?.temporary == false) {
                    Log.Error(
                        $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.freezeTerrain} is a freeze terrain. But it is not temporary.");
                }

                if (weatherExtension.tideTerrain is { } tideTerrain) {
                    if (!tideTerrain.HasTag("NPS_Tide")) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.tideTerrain} is a tide terrain. But it does not have the \"NPS_Tide\" tag.");
                    }

                    if (!tideTerrain.temporary) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.tideTerrain} is a tide terrain. But it is not temporary.");
                    }

                    HashTideHasTempTide.Add(terrain);
                }

                if (weatherExtension.riverTerrain is { } riverTerrain) {
                    if (!riverTerrain.HasTag("NPS_River")) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.riverTerrain} is a river terrain. But it does not have the \"NPS_River\" tag.");
                    }

                    if (!riverTerrain.temporary) {
                        Log.Error(
                            $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.riverTerrain} is a river terrain. But it is not temporary.");
                    }

                    HashRiverToTempRiver.Add(terrain);
                }
            }
        }

        NPS_Water = HashNPS_Water.ToFrozenSet();
        NPS_DeepWater = HashNPS_DeepWater.ToFrozenSet();
        Lava = HashLava.ToFrozenSet();
        CanBePacked = HashCanBePacked.ToFrozenSet();
        TideHasTempTide = HashTideHasTempTide.ToFrozenSet();
        RiverHasTempRiver = HashRiverToTempRiver.ToFrozenSet();
        AmbientTempReaction = HashAmbientTempReaction.ToFrozenDictionary();
        // Don't need the original dicts
        HashNPS_Water.Clear();
        HashNPS_DeepWater.Clear();
        HashLava.Clear();
        HashCanBePacked.Clear();
        HashAmbientTempReaction.Clear();
        HashTideHasTempTide.Clear();
        HashRiverToTempRiver.Clear();
    }
}