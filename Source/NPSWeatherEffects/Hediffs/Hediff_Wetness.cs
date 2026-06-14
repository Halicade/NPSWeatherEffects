using RimWorld;
using Verse;

namespace NPSWeather;

public class Hediff_Wetness : HediffWithComps
{
    private Map map;

    private IntVec3 position;
    private int timeDrying;
    private float storedWetness;

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref timeDrying, "timeDrying");
        Scribe_Values.Look(ref storedWetness, "storedWetness");
    }

    public override void PreRemoved() {
        base.PreRemoved();
        pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(ThoughtDefOf.SoakingWet);
    }

    public override void PostAdd(DamageInfo? dinfo) {
        position = pawn.Position;
        if (!position.IsValid) {
            Severity = 0;
            return;
        }

        map = pawn.MapHeld;
        if (map == null || !position.InBounds(map)) {
            Severity = 0;
            return;
        }

        storedWetness = wetnessRate();
    }

    public override void TickInterval(int delta) {
        position = pawn.Position;
        if (!position.IsValid) {
            Severity = 0;
            return;
        }

        map = pawn.MapHeld;
        if (map == null || !position.InBounds(map)) {
            Severity = 0;
            return;
        }

        if (pawn.IsHashIntervalTick(30, delta)) {
            storedWetness = wetnessRate();
        }

        if (storedWetness < 0 && Severity > 0.4 && pawn.IsHashIntervalTick(1000, delta)) {
            if (FilthMaker.TryMakeFilth(position, map, ThingDefOf.TKKN_FilthPuddle))
                Severity -= 0.1f;
        }

        Severity += storedWetness * delta;
    }

    private float wetnessRate() {
        //check if the pawn is in water
        var terrain = position.GetTerrain(map);
        if (terrain != null && TerrainTagUtil.NPS_Water.Contains(terrain)) {
            timeDrying = 0;
            //deep water gets them soaked.
            if (TerrainTagUtil.NPS_DeepWater.Contains(terrain)) {
                if (Severity < 0.75f) {
                    Severity = 0.75f;
                }

                return 0.06f;
            }

            return 0.035f;
        }

        //check if the pawn is wet from the weather
        var weatherRate = map.weatherManager.SnowRate;
        if (weatherRate > .001f) {
            if (!map.roofGrid.Roofed(position)) {
                timeDrying = 0;
                return weatherRate * 0.00001f;
            }
        }
        else {
            weatherRate = map.weatherManager.RainRate;
            if (weatherRate > .001f) {
                if (!map.roofGrid.Roofed(position)) {
                    timeDrying = 0;
                    return weatherRate * 0.000005f;
                }
            }
        }

        //dry the pawn.
        var rate = pawn.AmbientTemperature;
        if (rate <= 0) {
            return 0;
        }

        rate = -rate / 250f;

        // This is such a niche case, and pawns dry relatively quick.
        // If it's a performance issue I can remove it but it doesn't seem like it should be
        foreach (var c in GenAdj.CellsAdjacent8Way(pawn)) {
            if (!c.InBounds(map) || !c.IsValid) {
                continue;
            }

            foreach (var thing in c.GetThingList(map)) {
                if (ThingUtil.heatThings.TryGetValue(thing.def, out float heat)) {
                    rate -= heat;

                    /*
                     * Could use this method, but it feels like too much work.
                     * So for now it doesn't matter if they are powered or not.
                     * if (thing.TryGetComp<CompHeatPusher>().ShouldPushHeatNow)
                     * if(thing.TryGetComp<CompTempControl>().operatingAtHighPower)
                     */
                }
            }
        }
        /*
         * If temperature is 70f ~= 21c
         * temperature rate is -0.084
         * -0.084 / 2000 = 0.000042 per tick
         * 1 / 0.000042 = 23,809
         * ~24000 ticks to remove about 4 hours
         */

        return rate / 2000f;
    }
}