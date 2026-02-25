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
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_PlantBarnacles;
    [MayRequire("Hali.NPSBiomes")] 
    public static ThingDef TKKN_PlantWildflowers;
    [MayRequire("Hali.NPSBiomes")]
    public static ThingDef TKKN_SaltCrystal;

    public static ThingDef TKKN_DustDevil;
    public static ThingDef Plant_Tinctoria;
    public static ThingDef TKKN_Mote_ColdBreath;
    public static ThingDef TKKN_FilthPuddle;
    public static ThingDef Filth_Slime;
    public static ThingDef TKKN_FilthShells;
    public static ThingDef TKKN_FilthSeaweed;
    public static ThingDef TKKN_FilthDriftwood;
    
    
    


    static ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}