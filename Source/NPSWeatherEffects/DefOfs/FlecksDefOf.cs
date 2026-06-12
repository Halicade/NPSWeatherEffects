using RimWorld;
using Verse;

namespace NPSWeather;


[DefOf]
public class FlecksDefOf
{
    public static FleckDef NPS_PuddleFleck;
    
    static FlecksDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(FlecksDefOf));
    }
}