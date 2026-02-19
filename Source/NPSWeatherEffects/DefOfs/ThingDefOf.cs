using RimWorld;
using Verse;

namespace NPSWeather;

[DefOf]
public static class ThingDefOf
{
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_SteamVent;
    
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_LavaRock;

    
    public static ThingDef TKKN_DustDevil;
    
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_PlantBarnacles;
    public static ThingDef TKKN_Mote_ColdBreath;
    public static ThingDef TKKN_FilthPuddle;
    
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_SaltCrystal;

    static ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}