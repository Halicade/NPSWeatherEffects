using RimWorld;

namespace NPSWeather;

[DefOf]
public class ThingSetMakerDefOf
{
    public static ThingSetMakerDef TKKN_TidalLoot;


    static ThingSetMakerDefOf() {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingSetMakerDefOf));
    }
}