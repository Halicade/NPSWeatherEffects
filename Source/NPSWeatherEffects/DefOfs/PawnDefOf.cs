using RimWorld;
using Verse;

namespace NPSWeather;


[DefOf]
public class PawnDefOf
{
    [MayRequire("Hali.NPSBiomes")]
    public static PawnKindDef TKKN_crab;
    
    static PawnDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PawnDefOf));
    }
}