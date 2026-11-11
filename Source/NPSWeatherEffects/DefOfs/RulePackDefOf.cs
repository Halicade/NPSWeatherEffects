using RimWorld;
using Verse;

namespace NPSWeather;

[DefOf]
public class RulePackDefOf
{
    public static RulePackDef NPS_MessageInABottle;
    
    static RulePackDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(RulePackDefOf));
    }
}