using System.Collections.Frozen;
using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public class ThingUtil
{
    private static readonly Dictionary<ThingDef, float> DictHeatThings = [];
    private static readonly HashSet<ThingDef> hashDontDestroyThings = [];

    public static FrozenDictionary<ThingDef, float> heatThings;
    public static FrozenSet<ThingDef> dontDestroyThings;


    public static void InitializeThingUtil() {
        List<ThingDef> thingList = DefDatabase<ThingDef>.AllDefsListForReading;

        foreach (ThingDef thingDef in thingList) {
            var heater = thingDef.GetCompProperties<CompProperties_HeatPusher>();

            if (heater == null) {
                continue;
            }

            if (heater.heatPerSecond != 0) {
                DictHeatThings.Add(thingDef, heater.heatPerSecond / 400);
            }

            if (thingDef.HasModExtension<DontDestroyTide>()) {
                hashDontDestroyThings.Add(thingDef);
            }
        }

        heatThings = DictHeatThings.ToFrozenDictionary();
        dontDestroyThings = hashDontDestroyThings.ToFrozenSet();
    }
}