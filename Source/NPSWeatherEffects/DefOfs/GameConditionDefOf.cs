using RimWorld;
using Verse;

namespace NPSWeather;

[DefOf]
public class GameConditionDefOf
{
    public static GameConditionDef TKKN_Drought;
    
    static GameConditionDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(GameConditionDefOf));
    }
}