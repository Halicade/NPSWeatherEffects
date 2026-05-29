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
    private static readonly Dictionary<TerrainDef, float> HashAmbientTempReaction = [];

    public static FrozenSet<TerrainDef> TKKN_Wet = [];
    public static FrozenSet<TerrainDef> TKKN_Swim = [];
    public static FrozenSet<TerrainDef> Lava = [];
    public static FrozenSet<TerrainDef> CanBePacked = [];
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


            var weatherExtension = terrain.GetModExtension<TerrainWeatherReactions>();
            if (weatherExtension != null) {
                if (weatherExtension.temperatureAdjust != 0) {
                    HashAmbientTempReaction.Add(terrain, weatherExtension.temperatureAdjust);
                }

                if (weatherExtension.freezeTerrain?.terrain is { temporary: false }) {
                    Log.Error(
                        $"NPSWeatherEffects: Terrain {terrain} has an extension indicating {weatherExtension.freezeTerrain} is a freeze terrain. But it is not temporary. ");
                }
            }
        }

        TKKN_Wet = HashTKKN_Wet.ToFrozenSet();
        TKKN_Swim = HashTKKN_Swim.ToFrozenSet();
        Lava = HashLava.ToFrozenSet();
        CanBePacked = HashCanBePacked.ToFrozenSet();
        AmbientTempReaction = HashAmbientTempReaction.ToFrozenDictionary();
        // Don't need the original dicts
        HashTKKN_Wet.Clear();
        HashTKKN_Swim.Clear();
        HashLava.Clear();
        HashCanBePacked.Clear();
        HashAmbientTempReaction.Clear();
    }
}