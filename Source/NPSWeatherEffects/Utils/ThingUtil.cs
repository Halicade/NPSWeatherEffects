using System.Collections.Frozen;
using System.Collections.Generic;
using RimWorld;
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
            if (thingDef.HasModExtension<DontDestroyTide>()) {
                hashDontDestroyThings.Add(thingDef);
            }
            
            var heater = thingDef.GetCompProperties<CompProperties_HeatPusher>();

            if (heater is { heatPerSecond: > 0 }) {
                DictHeatThings.Add(thingDef, heater.heatPerSecond / 6);
            }

            var tempControl = thingDef.GetCompProperties<CompProperties_TempControl>();
            if (tempControl is { energyPerSecond: > 0 }) {
                DictHeatThings.Add(thingDef, tempControl.energyPerSecond / 6);
            }
        }

        heatThings = DictHeatThings.ToFrozenDictionary();
        dontDestroyThings = hashDontDestroyThings.ToFrozenSet();
    }
}