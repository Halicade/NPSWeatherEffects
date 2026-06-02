using RimWorld;
using Verse;

namespace NPSWeather;

[DefOf]
public static class TerrainDefOf
{
    [MayRequire("hali.NPSBiomes")]
    public static TerrainDef TKKN_SaltField;
    
    
    public static TerrainDef TKKN_HotSpringsWater;
    public static TerrainDef TKKN_ColdSpringsWater;
    [MayRequire("hali.NPSBiomes")]
    public static TerrainDef TKKN_Lava;
    [MayRequire("hali.NPSBiomes")]
    public static TerrainDef TKKN_LavaDeep;
    [MayRequire("hali.NPSBiomes")]
    public static TerrainDef TKKN_LavaRock_RoughHewn;
    
    public static TerrainDef TKKN_DirtPath;
    public static TerrainDef TKKN_SandPath;

    //tides
    public static TerrainDef TKKN_SandBeachWetSalt;


    static TerrainDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(TerrainDefOf));
    }
}