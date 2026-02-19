using RimWorld;

namespace NPSWeather;

[DefOf]
public class BiomeDefOf
{
    
    [MayRequire("Hali.NPSBiomes")]
    public static BiomeDef TKKN_Oasis;
    
    [MayRequire("Hali.NPSBiomes")]
    public static BiomeDef TKKN_VolcanicFlow;
    
    static BiomeDefOf() {
        DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
    }
}