using RimWorld;

namespace NPSWeather;

[DefOf]
public class ThingSetMakerDefOf
{
    public static ThingSetMakerDef TKKN_TidalLoot;

    public static ThingSetMakerDef TKKN_RiverLoot;

    static ThingSetMakerDefOf() {
        DefOfHelper.EnsureInitializedInCtor(typeof(ThingSetMakerDefOf));
    }
}