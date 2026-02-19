using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public class CompProperties_DeadlySalt : CompProperties
{
    public CompProperties_DeadlySalt()
    {
        compClass = typeof(CompDeadlySalt);
    }
}