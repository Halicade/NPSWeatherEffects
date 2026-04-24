using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class cellData : IExposable
{
    private const int PackAt = 75;
    private const int PackAtSmooth = PackAt * 10;

    private const int UnpackAt = PackAt / 2;

    //public TerrainDef baseTerrain;
    public int riverLevel = 999;
    public IntVec3 riverFocus = IntVec3.Invalid;
    public float frostLevel;
    public float frostNoise;

    public float rainLevel;
    public float rainNoise;

    public int howPacked;
    private bool packed = false;
    private int lastPackedCheck;
    public int howWet;
    public float howWetPlants = 60;
    public bool isFrozen;
    public bool isWet;
    public IntVec3 location = IntVec3.Invalid;
    public int locationIndex;
    public Map map;
    private TerrainDef driedTerrain;

    public float temperature = -9999;

    public int tideLevel = 999;
    public IntVec3 tideFocus = IntVec3.Invalid;

    public TerrainDef currentTerrain;

    public TerrainWeatherReactions weatherExtension;

    public void ExposeData() {
        Scribe_Values.Look(ref tideLevel, "tideLevel", 999);
        Scribe_Values.Look(ref tideFocus, "tideFocus", IntVec3.Invalid);
        Scribe_Values.Look(ref riverLevel, "riverLevel", 999);
        Scribe_Values.Look(ref riverFocus, "riverFocus", IntVec3.Invalid);
        Scribe_Values.Look(ref howPacked, "howPacked");
        Scribe_Values.Look(ref packed, "packed");
        Scribe_Values.Look(ref lastPackedCheck, "lastPackedCheck");
        Scribe_Values.Look(ref howWet, "howWet", -1);
        Scribe_Values.Look(ref howWetPlants, "howWetPlants", 60);
        Scribe_Values.Look(ref frostLevel, "frostLevel");
        Scribe_Values.Look(ref rainLevel, "rainLevel");
        Scribe_Values.Look(ref isWet, "isWet");
        Scribe_Values.Look(ref isFrozen, "isFrozen");
        Scribe_Values.Look(ref location, "location", forceSave: true);
        Scribe_Defs.Look(ref driedTerrain, "driedTerrain");
    }

    public void setCurrentExtension() {
        weatherExtension = currentTerrain.GetModExtension<TerrainWeatherReactions>();
    }

    public bool wetCheck(bool gettingWet) {
        if (howWet < 3 && gettingWet) {
            howWet += 2;
            return true;
        }

        if (howWet > -1 && !gettingWet) {
            howWet--;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Use this to determine how we wet terrain
    /// </summary>
    public bool setTerrainWater() {
        if (EffectSettings.showWetTerrain) {
            return setTerrainWet();
        }

        if (EffectSettings.showFloodTerrain) {
            return setTerrainFlood();
        }

        return false;
    }

    private bool setTerrainFlood() {
        //if terrain is temporary we don't want to affect it
        if (currentTerrain.temporary) {
            return false;
        }

        if (isWet) {
            return false;
        }

        if (weatherExtension?.floodTerrain == null) {
            return false;
        }

        if (howWet > weatherExtension.wetAt) {
            driedTerrain = currentTerrain;
            map.terrainGrid.SetTerrain(location, weatherExtension.floodTerrain);
            currentTerrain = weatherExtension.floodTerrain;
            setCurrentExtension();
            isWet = true;
            rainSpawns();
            return true;
        }

        return false;
    }

    private bool setTerrainWet() {
        //if terrain is temporary we don't want to affect it
        if (currentTerrain.temporary) {
            return false;
        }

        if (isWet) {
            return false;
        }

        if (weatherExtension?.wetTerrain == null) {
            return false;
        }

        if (howWet > weatherExtension.wetAt) {
            driedTerrain = currentTerrain;
            map.terrainGrid.SetTerrain(location, weatherExtension.wetTerrain);
            currentTerrain = weatherExtension.wetTerrain;
            setCurrentExtension();
            isWet = true;
            rainSpawns();
            return true;
        }

        return false;
    }

    public bool trySetTerrainDry() {
        if (!isWet) {
            return false;
        }
        /*
        I don't know why I commented this out, but it hasn't seemed to break anything yet
        //if terrain is temporary we don't want to affect it
        if (thisTerrain.temporary) {
            return;
        }*/

        if (howWet < weatherExtension.wetAt) {
            map.terrainGrid.SetTerrain(location, driedTerrain);
            isWet = false;
            howWet = -1;
            driedTerrain = null;
            setCurrentExtension();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Yes this feels hideous, yes I hate it.
    /// Needs to reduce the amount of pieces that turn to ice per tick so now it has a 5% chance to happen
    /// If anyone has a better suggestion I'm all ears.
    /// </summary>
    public bool SetTerrainFrozen() {
        if (isFrozen) {
            return false;
        }

        if (weatherExtension?.freezeTerrain == null)
            return false;

        if (temperature > weatherExtension.freezeTerrain.freezeAt)
            return false;
        // Still want the system to count this as a changed tile
        if (!Rand.Chance(0.05f))
            return true;

        map.terrainGrid.SetTempTerrain(location, weatherExtension.freezeTerrain.terrain);
        setCurrentExtension();
        isFrozen = true;
        return true;
    }

    public bool TrySetTerrainThawed() {
        if (!isFrozen) {
            return false;
        }

        if (!currentTerrain.temporary) {
            return false;
        }

        map.terrainGrid.RemoveTempTerrain(location, doLeavings: false, preventDestroyEffects: true);
        howWet = 4;
        isFrozen = false;
        currentTerrain = map.terrainGrid.TerrainAt(location);
        setTerrainWet();
        return true;
    }

    public bool increaseTide(TerrainDef tidalTerrain) {
        if (isFrozen) {
            return false;
        }

        // Verify the current tile does not have anything on it.
        // Then verify
        if (currentTerrain.isFoundation)
            return false;
        if (location.GetEdifice(map) != null)
            return false;
        if (tideFocus.GetEdifice(map) != null)
            return false;
        if (!tideFocus.GetTerrain(map).IsWater)
            return false;
        map.terrainGrid.SetTempTerrain(location, tidalTerrain);
        setCurrentExtension();
        clearLoot();
        return true;
    }

    public void decreaseTide(TerrainDef tidalTerrain) {
        currentTerrain = map.terrainGrid.TerrainAt(locationIndex);
        if (currentTerrain != tidalTerrain) {
            return;
        }

        map.terrainGrid.RemoveTempTerrain(location);
        leaveLoot();
        if (EffectSettings.showRainEffects) {
            currentTerrain = location.GetTerrain(map);
            setCurrentExtension();
            setTerrainWater();
        }
    }

    /// <summary>
    /// Verifies the current tile does not have anything on it.
    /// Then verifies that the tile it's focusing on doesn't have anything on it and is a water tile.
    /// If true set terrain to riverTerrain
    /// </summary>
    /// <param name="riverTerrain"></param>
    public bool increaseRiver(TerrainDef riverTerrain) {
        if (isFrozen) {
            return false;
        }

        // Verify the current tile does not have anything on it.
        // Then verify
        if (currentTerrain.isFoundation)
            return false;
        if (location.GetEdifice(map) != null)
            return false;
        if (riverFocus.GetEdifice(map) != null)
            return false;
        if (!riverFocus.GetTerrain(map).IsWater)
            return false;
        map.terrainGrid.SetTempTerrain(location, riverTerrain);
        return true;
    }

    public void decreaseRiver(TerrainDef riverTerrain) {
        currentTerrain = map.terrainGrid.TerrainAt(locationIndex);
        if (currentTerrain != riverTerrain) {
            return;
        }

        map.terrainGrid.RemoveTempTerrain(location);
        leaveLoot();
        if (EffectSettings.showRainEffects) {
            howWet = 4;
            currentTerrain = location.GetTerrain(map);
            setCurrentExtension();
            setTerrainWet();
        }
    }

    public void Unpack() {
        if (howPacked > 10000) {
            howPacked = 10000;
            return;
        }

        if (howPacked <= 0) {
            return;
        }

        howPacked--;
        if (!packed) {
            return;
        }

        if (howPacked <= UnpackAt) {
            if (currentTerrain == TerrainDefOf.TKKN_DirtPath) {
                map.terrainGrid.SetTerrain(location, RimWorld.TerrainDefOf.Soil);
                packed = false;
            }
            else if (currentTerrain == TerrainDefOf.TKKN_SandPath) {
                map.terrainGrid.SetTerrain(location, RimWorld.TerrainDefOf.Sand);
                packed = false;
            }
        }
    }

    /// <summary>
    /// This method currently has a weird thing where if you remove a packed path
    /// (You can, and we want the user to do the above)
    /// It will change back to a packed path after being stepped on
    /// Unsure how to address this part.
    /// </summary>
    public void DoPack() {
        var terrain = currentTerrain;
        if (!TerrainTagUtil.CanBePacked.Contains(terrain)) {
            return;
        }

        //don't pack if there's a growing zone.
        if (map.zoneManager.ZoneAt(location) is Zone_Growing) {
            return;
        }

        howPacked++;
        if (packed) {
            return;
        }

        if (howPacked <= PackAt) {
            return;
        }

        if (terrain == RimWorld.TerrainDefOf.Soil) {
            map.terrainGrid.SetTerrain(location, TerrainDefOf.TKKN_DirtPath);
            weatherExtension = null;
            packed = true;
        }
        else if (terrain == RimWorld.TerrainDefOf.Sand) {
            map.terrainGrid.SetTerrain(location, TerrainDefOf.TKKN_SandPath);
            weatherExtension = null;
            packed = true;
        }
        else if (terrain.smoothedTerrain != null && howPacked > PackAtSmooth) {
            map.terrainGrid.SetTerrain(location, terrain.smoothedTerrain);
            weatherExtension = null;
            packed = true;
            //Don't care about the packed level of smooth terrain
            howPacked = 0;
        }
    }

    private void rainSpawns() {
        //spawn special things when it rains.
        if (Rand.Value < .009) {
            if (currentTerrain == TerrainDefOf.TKKN_Lava) {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock), location, map);
            }
            else if (currentTerrain == TerrainDefOf.TKKN_SandBeachWetSalt
                     && PawnDefOf.TKKN_crab != null) {
                var crab = PawnGenerator.GeneratePawn(PawnDefOf.TKKN_crab);
                GenSpawn.Spawn(crab, location, map);
            }
            else {
                if (TerrainTagUtil.TKKN_Wet.Contains(currentTerrain)) {
                    FleckMaker.WaterSplash(location.ToVector3(), map, 1, 1);
                }
            }
        }
        else if (Rand.Value < .04 && TerrainTagUtil.Lava.Contains(currentTerrain)) {
            FleckMaker.ThrowSmoke(location.ToVector3(), map, 5);
        }
    }

    private readonly List<ThingDef> possibleFilth = [
        ThingDefOf.TKKN_FilthPuddle,
        ThingDefOf.Filth_Slime,
        ThingDefOf.TKKN_FilthShells,
        ThingDefOf.TKKN_FilthPuddle,
        ThingDefOf.TKKN_FilthSeaweed,
        ThingDefOf.TKKN_FilthDriftwood
    ];

    private void leaveLoot() {
        if (!EffectSettings.leaveLoot) {
            return;
        }

        if (location.GetEdifice(map) != null) {
            //Prevent items from spawning if a building was placed there
            return;
        }

        var leaveSomething = Rand.Value;
        if (leaveSomething < 0.002f) {
            if (Rand.Bool) {
                FilthMaker.TryMakeFilth(location, map, possibleFilth.RandomElement());
            }
            else {
                List<Thing> allowed = ThingSetMakerDefOf.TKKN_TidalLoot.root.Generate();

                if (allowed == null) {
                    return;
                }

                for (int i = 0; i < allowed.Count; i++) {
                    allowed[i].HitPoints = (int)(allowed[i].HitPoints * Rand.Range(0.1f, 1f));
                    var spawnedThing = GenSpawn.Spawn(allowed[i], location, map);
                    if (EffectSettings.forbidLoot) {
                        spawnedThing.SetForbidden(true);
                    }
                }
            }
        }
        else if (leaveSomething < 0.003f && (location.GetPlant(map) == null && location.GetCover(map) == null)) {
            //grow water and shore plants:
            List<ThingDef> plants = map.Biome.AllWildPlants.ToList();
            plants.Shuffle();
            for (var i = plants.Count - 1; i >= 0; i--) {
                //spawn some water plants:
                var plantDef = plants[i];
                if (!plantDef.CanEverPlantAt(location, map, checkMapTemperature: false))
                    continue;
                var plant = (Plant)ThingMaker.MakeThing(plantDef);
                plant.Growth = Rand.Range(0.7f, 1f);
                if (plant.def.plant.LimitedLifespan) {
                    plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                }

                GenSpawn.Spawn(plant, location, map);
                break;
            }
        }
    }

    private void clearLoot() {
        if (!EffectSettings.leaveLoot) {
            return;
        }

        List<Thing> things = location.GetThingList(map);
        DamageInfo asdf = new DamageInfo(DamageDefOf.Deterioration, 9000f);

        for (var i = things.Count - 1; i >= 0; i--) {
            if (things[i].def.category == ThingCategory.Item || things[i].def.category == ThingCategory.Plant) {
                if (things[i].def.useHitPoints) {
                    things[i].TakeDamage(asdf);
                }
                else {
                    things[i].Destroy(DestroyMode.KillFinalize);
                }
            }
        }
    }

    /// <summary>
    /// Check terrain on load and to if it was previously a wet terrain that is no longer that terrain. 
    /// </summary>
    public void checkTerrainDry() {
        if (driedTerrain == null) {
            return;
        }

        TerrainWeatherReactions driedModExtension = driedTerrain.GetModExtension<TerrainWeatherReactions>();
        if (driedModExtension?.wetTerrain == null) {
            driedTerrain = null;
            isWet = false;
            Log.Error(
                "NPSWeatherEffects: Tile: " + location +
                " Biome: " + map.Biome +
                " Former terrain: " + driedTerrain +
                " Current Terrain " + currentTerrain +
                "NPSWeatherEffects: A terrain that previously had a mod extension for wet terrain no longer has it." +
                " You should report this error. ");
            return;
        }

        TerrainDef baseTerrain = map.terrainGrid.BaseTerrainAt(location);

        if (baseTerrain != driedModExtension.wetTerrain) {
            driedTerrain = null;
            isWet = false;
            Log.Warning(
                "NPSWeatherEffects: Tile: " + location +
                " Biome: " + map.Biome +
                " Former terrain: " + driedTerrain +
                " Current Base Terrain " + baseTerrain +
                "A terrain that was previously wet terrain no longer has that terrain as a base terrain." +
                " Normally this occurs due to terraforming a wet terrain tile. ");
        }
    }

    /// <summary>
    /// Use this to determine how we wet terrain
    /// </summary>
    public void forceTerrainWater() {
        if (EffectSettings.showWetTerrain) {
            forceTerrainWet();
        }
        else {
            forceTerrainFlood();
        }
    }

    public void forceTerrainWet() {
        //if terrain is temporary we don't want to affect it
        if (currentTerrain.temporary) {
            return;
        }

        if (isWet) {
            return;
        }

        if (weatherExtension?.wetTerrain == null) {
            return;
        }

        howWet = 4;

        if (howWet > weatherExtension.wetAt) {
            driedTerrain = currentTerrain;
            map.terrainGrid.SetTerrain(location, weatherExtension.wetTerrain);
            currentTerrain = weatherExtension.wetTerrain;
            setCurrentExtension();
            isWet = true;
            rainSpawns();
        }
    }

    private void forceTerrainFlood() {
        //if terrain is temporary we don't want to affect it
        if (currentTerrain.temporary) {
            return;
        }

        if (isWet) {
            return;
        }

        if (weatherExtension?.floodTerrain == null) {
            return;
        }

        howWet = 4;

        if (howWet > weatherExtension.wetAt) {
            driedTerrain = currentTerrain;
            map.terrainGrid.SetTerrain(location, weatherExtension.floodTerrain);
            currentTerrain = weatherExtension.floodTerrain;
            setCurrentExtension();
            isWet = true;
            rainSpawns();
        }
    }

    public void forceTerrainFrozen() {
        if (isFrozen) {
            return;
        }

        if (weatherExtension?.freezeTerrain == null)
            return;

        if (temperature > weatherExtension.freezeTerrain.freezeAt)
            return;

        map.terrainGrid.SetTempTerrain(location, weatherExtension.freezeTerrain.terrain);
        currentTerrain = weatherExtension.freezeTerrain.terrain;
        setCurrentExtension();
        isFrozen = true;
    }

    public void resetCellData() {
        forceRemoveTempTerrain();
        forceUnpack();
        forceTerrainDry();
        removeWetSand();
        forceRemoveSectionLayers();
    }

    public void forceRemoveTempTerrain() {
        currentTerrain = location.GetTerrain(map);
        isFrozen = false;
        riverFocus = IntVec3.Invalid;
        riverLevel = 999;
        tideLevel = -1;

        if (!currentTerrain.temporary) {
            return;
        }

        if (currentTerrain?.modContentPack == EffectSettings.modContent) {
            map.terrainGrid.RemoveTempTerrain(location, doLeavings: false, preventDestroyEffects: true);
        }
    }

    public void forceUnpack() {
        currentTerrain = map.terrainGrid.TerrainAt(locationIndex);
        howPacked = 0;
        packed = false;

        if (currentTerrain == TerrainDefOf.TKKN_DirtPath ||
            currentTerrain == TerrainDefOf.TKKN_SandPath) {
            map.terrainGrid.RemoveTopLayer(location);
        }
    }


    public void forceTerrainDry() {
        if (driedTerrain == null) {
            return;
        }

        if (map.terrainGrid.UnderTerrainAt(location) != null) {
            map.terrainGrid.SetUnderTerrain(location, driedTerrain);
        }
        else {
            map.terrainGrid.SetTerrain(location, driedTerrain);
        }

        currentTerrain = map.terrainGrid.TerrainAt(locationIndex);

        //map.terrainGrid.SetTerrain(location, driedTerrain);
        isWet = false;
        howWet = 0;
        driedTerrain = null;
        setCurrentExtension();
    }

    public void removeWetSand() {
        if (map.terrainGrid.BaseTerrainAt(location) == TerrainDefOf.TKKN_SandBeachWetSalt) {
            if (map.terrainGrid.UnderTerrainAt(location) != null) {
                map.terrainGrid.SetUnderTerrain(location, RimWorld.TerrainDefOf.Sand);
            }
            else {
                map.terrainGrid.SetTerrain(location, RimWorld.TerrainDefOf.Sand);
            }
        }
    }

    public void forceRemoveSectionLayers() {
        frostLevel = 0;
        rainLevel = 0;
    }
}