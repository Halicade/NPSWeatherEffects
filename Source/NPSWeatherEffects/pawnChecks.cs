using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

public static class PawnChecks
{
    public static void checks(Pawn pawn, Map map, Watcher watcher, bool isRaining, int ticks) {
        if (!pawn.Spawned || pawn.Dead) {
            return;
        }


        TerrainDef terrain = pawn.Position.GetTerrain(pawn.MapHeld);

        makePaths(pawn, watcher, map);
        makeWet(pawn, terrain, isRaining, map);
        if ((pawn.HashOffset() + ticks) % 300 == 0) {
            makeBreath(pawn, map);
            if (!drowningCheck(pawn, terrain)) {
                springCheck(pawn, terrain);
            }
        }
    }

    private static void springCheck(Pawn pawn, TerrainDef terrain) {
        if (pawn.needs == null) {
            return;
        }

        if (terrain == TerrainDefOf.TKKN_HotSpringsWater) {
            if (pawn.needs.comfort != null) {
                pawn.needs.comfort.lastComfortUseTick--;
            }

            HealthUtility.AdjustSeverity(pawn, HediffDefOf.TKKN_hotspring_chill_out, 0.5f);
        }
        else if (terrain == TerrainDefOf.TKKN_ColdSpringsWater) {
            pawn.needs.rest?.TickResting(.05f);


            //Remove heatstroke if pawn is in cold spring
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Heatstroke);
            if (heatstroke != null) {
                pawn.health.RemoveHediff(heatstroke);
            }

            HealthUtility.AdjustSeverity(pawn, HediffDefOf.TKKN_coldspring_chill_out, 0.5f);
        }
    }

    private static bool drowningCheck(Pawn pawn, TerrainDef terrain) {
        //drowning == immobile and in water
        if (!EffectSettings.allowPawnsDrowning) return false;

        if (!TerrainTagUtil.NPS_Water.Contains(terrain) || !pawn.health.Downed) {
            return false;
        }

        //ignore if they're mostly vacuum resistant
        if (ModsConfig.OdysseyActive &&
            pawn.GetStatValue(StatDefOf.VacuumResistance) > 0.95)
            return false;

        if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.TKKN_Drowning) != null)
            return false;

        var hediff = HediffMaker.MakeHediff(HediffDefOf.TKKN_Drowning, pawn);
        hediff.Severity = 0.001f;
        pawn.health.AddHediff(hediff);
        if (pawn.IsColonist) {
            Messages.Message("TKKN_NPS_DrowningText".Translate(pawn.NameShortColored),
                new LookTargets(pawn),
                MessageTypeDefOf.NegativeHealthEvent,
                false);
        }

        return true;
    }

    private static void makeWet(Pawn pawn, TerrainDef currentTerrain, bool isRaining, Map map) {
        if (!EffectSettings.allowPawnsToGetWet) {
            return;
        }

        var c = pawn.Position;
        if (!c.IsValid) {
            return;
        }

        var isWet = false;
        if (isRaining) {
            if (!map.roofGrid.Roofed(c)) {
                //I hate writing it like this, but it's a lot more readable than the inverse
                if (HarmonyWeatherEffects.RimBrellasActive && HarmonyWeatherEffects.HasUmbrella(pawn)) { }
                else {
                    isWet = true;
                }
            }
        }

        if (TerrainTagUtil.NPS_Water.Contains(currentTerrain)) {
            isWet = true;
        }


        if (!isWet) {
            return;
        }


        if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.TKKN_Wetness) != null) {
            return;
        }

        var hediff = HediffMaker.MakeHediff(HediffDefOf.TKKN_Wetness, pawn);
        hediff.Severity = 0.01f;
        pawn.health.AddHediff(hediff);
    }


    private static void makePaths(Pawn pawn, Watcher watcher, Map map) {
        if (!EffectSettings.doDirtPath) {
            return;
        }

        if (!pawn.Position.InBounds(map) || !pawn.pather.MovingNow) {
            return;
        }

        if (!watcher.cellWeatherAffects.TryGetValue(pawn.Position, out var cell))
            return;

        if (EffectSettings.doWalkThroughSnow && watcher.outdoorTemp < 3) {
            watcher.frostGridComponent.addDepth(cell, -.005f);
            map.snowGrid.AddDepth(pawn.Position, -.005f);
        }

        //pack down the soil only if the pawn is moving AND is in our colony
        if (pawn.IsColonist) {
            cell.DoPack();
        }
    }

    private static readonly Vector3 BreathOffset = new(0f, 0f, -0.04f);

    private static void makeBreath(Pawn pawn, Map map) {
        if (!EffectSettings.doColdBreath)
            return;
        if (pawn.Position.GetTemperature(map) >= 3f ||
            (ModsConfig.OdysseyActive &&
             pawn.GetStatValue(StatDefOf.VacuumResistance) > 0.95)) {
            return;
        }

        var head = pawn.Drawer.DrawPos + pawn.Drawer.renderer.BaseHeadOffsetAt(pawn.Rotation) +
                   pawn.Rotation.FacingCell.ToVector3() * 0.21f + BreathOffset;
        MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.TKKN_Mote_ColdBreath);
        moteThrown.Scale = Rand.Range(.5f, 1.5f);
        moteThrown.rotationRate = Rand.Range(-30f, 30f);
        moteThrown.exactPosition = head;

        moteThrown.SetVelocity(Rand.Range(-20, 30), Rand.Range(0.5f, 0.7f));
        GenSpawn.Spawn(moteThrown, head.ToIntVec3(), map);
    }
}