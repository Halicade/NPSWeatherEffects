using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NPSWeather;

public class Watcher(Map map) : MapComponent(map), IDisposable
{
    private const int HowManyRiverSteps = 6;
    private const int HalfRiverSteps = HowManyRiverSteps / 2;

    private const int BaseTideValue = 3;
    private int howManyTideSteps;
    private int savedHowManyTideSteps = -1;

    //Increment in the case of large scale changes that would affect currently generated maps
    private const int CurrentRevision = 3;
    private int savedRevision;

    //Every quarter hour
    private const int TideIntervalCheck = 625;

    //Every 6 hours
    private const int RiverIntervalCheck = 15000;

    //Every 12 hours
    private const int BiomeUpdateCheck = 30000;


    private BiomeSeasonalSettings biomeSettings;
    private Dictionary<IntVec3, cellData> cellWeatherAffectsDict = new();
    public FrozenDictionary<IntVec3, cellData> cellWeatherAffects;
    private List<cellData> cellWeatherList = [];
    private cellData activeCellData;

    private int cycleIndex;

    private readonly List<List<cellData>> riverCellsList = [];
    public int floodLevel;
    public bool rainingPreviousFloodCheck;

    public FrostGrid frostGridComponent;
    public WetnessGrid wetnessGridComponent;
    private Vector2 location;

    private ModuleBase frostNoise;
    private ModuleBase wetnessNoise;

    public float outdoorTemp;

    public bool regenCellLists = true;

    private int ticks;
    private int tickOffset;

    /// <summary>
    /// Doing this so that all checks aren't at the same time as other people probably have.<br/>
    /// Original ticks is still required for time specific things like tide level
    /// </summary>
    private int ticksWithModifier;

    public bool doCoast = true; //false if no coast
    private readonly List<List<cellData>> tideCellsList = [];
    public int tideLevel; // 0 - 13
    public float tideFactor = 1;
    private Rot4 coastRotation;

    private float longCurrentTile;
    private long ticksOffsetFromLongitude;
    private int checkUpToCell;
    private readonly List<cellData> failedTidalTiles = [];
    private bool finishedTideMovement = true;
    private bool tideIncreasing;
    private TideVariant savedTidalVariant;
    private TideVariant tidalVariant;

    //Values not in use
    //private float humidity;
    //private float wetPlantsValue;

    public bool dontRunAnything;
    private bool anyLavaTerrain;

    private bool doUnpacking;

    //Value not in use
    //private bool noHurtPlants;
    public readonly Dictionary<Pawn, bool> validPawns = [];
    private float currentRainRate;
    private float currentSnowRate;
    private int mapArea; //Default area 62500

    /// <summary>
    /// It is not raining if it is snowing
    /// </summary>
    public bool isRaining;

    private bool isSnowing;

    /// <summary>
    /// Accounts for either rain or snow. Don't care about sand
    /// </summary>
    private bool isPrecipitation;

    private readonly List<IntVec3> cellsToRainGrid = [];
    private TerrainDef beachTerrain;
    public bool doRiverFlooding;
    private bool iceOrFrostGrid;
    private bool doRoofChecks;
    private int currentSpeed;
    private float puddleSpawnChance;
    private List<Pawn> allPawnsSpawned;
    public Season season;
    public Quadrum quadrum;
    public Quadrum previousQuadrum = Quadrum.Undefined;
    public bool droughtActive;

    private const int MapCheckInterval = 625;

    private const int MinimumCellsPerTick = 5;

    //Wanted to make this occur at a different time from other intervals
    private const int RainGridCheckInterval = 450;
    private int cellActionsPerformed;
    private int cellActionsPerTick = 5;
    private bool acceleratedChecks;

    public override void FinalizeInit() {
        base.FinalizeInit();
        ticks = Find.TickManager.TicksAbs;
        tickOffset = Rand.RangeInclusiveSeeded(1000, 30000, map.Tile.tileId);
        ticksWithModifier = ticks + tickOffset;
        RebuildCellLists();
        mapChecks();
        allPawnsSpawned = map.mapPawns.AllHumanlikeSpawned;
    }

    public override void MapComponentTick() {
        if (dontRunAnything) {
            return;
        }

        ticks = Find.TickManager.TicksAbs;
        ticksWithModifier = ticks + tickOffset;
        
        //environmental changes
        if (ticksWithModifier % MapCheckInterval == 0) {
            mapChecks();

            if (cellActionsPerformed > EffectSettings.maxCellsPerTick / 3) {
                cellActionsPerTick = Math.Min(EffectSettings.maxCellsPerTick, cellActionsPerTick + 10);
                acceleratedChecks = true;
                //Log.Warning("New cell per tick value " + cellActionsPerTick);
            }
            else if (cellActionsPerformed < EffectSettings.maxCellsPerTick / 4) {
                cellActionsPerTick = MinimumCellsPerTick;
                acceleratedChecks = false;
                //Log.Message("New cell per tick value " + cellActionsPerTick);
            }

            cellActionsPerformed = 0;
        }

        if (EffectSettings.doWeather) {
            DoTides();
            DoRiverModify();

            if (ticksWithModifier % RainGridCheckInterval == 0) {
                foreach (var cellBeingRefreshed in cellsToRainGrid) {
                    wetnessGridComponent.refreshAt(cellBeingRefreshed);
                }

                cellsToRainGrid.Clear();
            }

            for (var i = 0; i < cellActionsPerTick; i++) {
                if (cycleIndex >= mapArea) {
                    cycleIndex = 0;
                }

                activeCellData = cellWeatherList[cycleIndex++];

                if (!basicCellChecks()) {
                    continue;
                }

                lavaCellChecks();
                rainCellChecks();
                if (!acceleratedChecks) {
                    iceCellChecks();
                    extraRainChecks();
                }
            }
        }

        if (EffectSettings.allowPawnEffects) {
            // rebuild the dictionary of valid pawns every quadrum
            if (ticksWithModifier % 900000 == 0) {
                validPawns.Clear();
            }

            if (ticksWithModifier % MapCheckInterval == 0) {
                allPawnsSpawned = map.mapPawns.AllHumanlikeSpawned;
            }

            for (int i = 0; i < allPawnsSpawned.Count; i++) {
                if (checkPawnHuman(allPawnsSpawned[i]))
                    PawnChecks.checks(allPawnsSpawned[i], map, this, isPrecipitation, ticksWithModifier);
            }
        }

        UpdateBiomeSettings();
    }

    private bool checkPawnHuman(Pawn pawn) {
        if (pawn.HashOffsetTicks() % 10 != 0) {
            return false;
        }

        if (EffectSettings.pawnEffectsOnlyColonists && !pawn.IsColonist) {
            return false;
        }

        if (validPawns.TryGetValue(pawn, out var result)) {
            return result;
        }

        if (pawn.IsShambler ||
            pawn.IsMutant ||
            (EffectSettings.pawnEffectsOnlyColonists && !pawn.IsColonist)) {
            validPawns.Add(pawn, false);
            return false;
        }

        validPawns.Add(pawn, true);
        return true;
    }

    private void mapChecks() {
        //set up humidity
        outdoorTemp = map.mapTemperature.OutdoorTemp;
        currentRainRate = map.weatherManager.curWeather.rainRate;
        currentSnowRate = map.weatherManager.curWeather.snowRate;
        isSnowing = currentSnowRate > 0;
        isRaining = !isSnowing && currentRainRate > 0;
        isPrecipitation = isSnowing || isRaining;
        /*
        var baseHumidity = (map.TileInfo.rainfall + 1) * (map.TileInfo.temperature + 1) *
                           (map.TileInfo.swampiness + 1);
        var currentHumidity =
            (1 + currentRainRate) * (1 + outdoorTemp);
        humidity = ((baseHumidity + currentHumidity) / 1000) + 18;
        wetPlantsValue = -1 * (outdoorTemp / humidity / 10);
        //noHurtPlants = !EffectSettings.allowPlantEffects || ticks % 150 != 0;
        floodThreatIncrease = 1 + 2 * (int)Math.Round(currentRainRate);
        */

        doUnpacking = EffectSettings.doDirtPath && !doUnpacking;
        iceOrFrostGrid = EffectSettings.doIce || EffectSettings.showFrostGrid;
        doRoofChecks = EffectSettings.showWetTerrain || EffectSettings.showFrostGrid || EffectSettings.makePuddles;
        currentSpeed = (int)(Find.TickManager.CurTimeSpeed + 3) * 6;
        puddleSpawnChance = 1f / currentSpeed;
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref regenCellLists, "regenCellLists", true);
        Scribe_Collections.Look(ref cellWeatherAffectsDict, "cellWeatherAffects", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref rainingPreviousFloodCheck, "rainingPreviousFloodCheck");
        Scribe_Values.Look(ref tideLevel, "tideLevel");
        Scribe_Values.Look(ref floodLevel, "floodLevel");
        Scribe_Values.Look(ref doRiverFlooding, "doRiverFlooding", doRiverFlooding);
        Scribe_Values.Look(ref anyLavaTerrain, "anyLavaTerrain", anyLavaTerrain);
        Scribe_Values.Look(ref checkUpToCell, "checkUpToCell");
        Scribe_Values.Look(ref finishedTideMovement, "finishedTideMovement", true);
        Scribe_Values.Look(ref savedTidalVariant, "savedTidalVariant");
        Scribe_Values.Look(ref savedHowManyTideSteps, "savedHowManyTideSteps", -1);
        Scribe_Values.Look(ref savedRevision, "savedRevision");
    }


    private void UpdateBiomeSettings(bool force = false) {
        if (!force) {
            if (ticksWithModifier % BiomeUpdateCheck != 0) {
                // Check every 12 hours
                return;
            }
        }

        //previousQuadrum = quadrum;
        quadrum = GenDate.Quadrum(ticks, location.x);

        season = GenDate.Season(ticks, location);
        droughtActive = false;
        foreach (var gameCondition in map.gameConditionManager.ActiveConditions) {
            if (gameCondition.def == GameConditionDefOf.TKKN_Drought ||
                gameCondition.def == RimWorld.GameConditionDefOf.Drought) {
                droughtActive = true;
            }
        }

        if (quadrum == previousQuadrum) {
            return;
        }

        previousQuadrum = quadrum;

        if (biomeSettings == null)
            return;


        biomeSettings.setWeatherBySeason(map, season, quadrum);
        biomeSettings.setDiseaseBySeason(map, season, quadrum);
        biomeSettings.setIncidentsBySeason(map, season, quadrum);
    }

    private bool waterChanged;
    private bool roofed;
    private TerrainDef currentTerrain;


    private bool basicCellChecks() {
        Building building = map.edificeGrid[activeCellData.locationIndex];
        if (building?.def?.Fillage == FillCategory.Full ||
            map.fogGrid.IsFogged(activeCellData.locationIndex)) {
            frostGridComponent.removeDepth(activeCellData);
            wetnessGridComponent.removeDepth(activeCellData);
            return false;
        }

        if (doUnpacking) {
            activeCellData.Unpack();
        }

        currentTerrain = map.terrainGrid.TerrainAt(activeCellData.locationIndex);
        if (activeCellData.currentTerrain != currentTerrain) {
            activeCellData.currentTerrain = currentTerrain;
            activeCellData.setCurrentExtension();
        }

        roofed = doRoofChecks && map.roofGrid.Roofed(activeCellData.locationIndex);
        waterChanged = false;

        /*
        //check if the terrain has been floored
        // Should there be a blanket check on if a terrain is a floor? Thought for future me

        if (currentTerrain.designationCategory == DesignationCategoryDefOf.Floors) {
            //cell.baseTerrain = currentTerrain;
        }*/
        return true;
    }

    private void lavaCellChecks() {
        //spawn special things
        if (!anyLavaTerrain) {
            return;
        }

        if (Rand.Value < 0.0001f) {
            if (currentTerrain == TerrainDefOf.TKKN_Lava) {
                var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock);
                GenSpawn.Spawn(thing, activeCellData.location, map);
            }
            else if (currentTerrain == TerrainDefOf.TKKN_LavaRock_RoughHewn &&
                     map.Biome == BiomeDefOf.TKKN_VolcanicFlow &&
                     map.listerThings.ThingsOfDef(ThingDefOf.TKKN_SteamVent).Count < 10) {
                var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_SteamVent);
                GenSpawn.Spawn(thing, activeCellData.location, map);
            }
        }
    }


    private void rainCellChecks() {
        if (!EffectSettings.showRainEffects) {
            return;
        }

        if (roofed) {
            if (EffectSettings.showRainGrid) {
                if (wetnessGridComponent.addDepth(activeCellData, -0.05f * activeCellData.rainNoise)) {
                    cellActionsPerformed++;
                }
            }

            if (activeCellData.trySetTerrainDry()) {
                cellActionsPerformed++;
            }

            return;
        }

        //if it's raining in this cell:
        if (isRaining) {
            if (activeCellData.setTerrainWater()) {
                waterChanged = true;
            }

            //currentRainRate * 0.0106f
            if (EffectSettings.showRainGrid) {
                if (wetnessGridComponent.addDepth(activeCellData, currentRainRate * 0.106f)) {
                    cellsToRainGrid.Add(activeCellData.location);
                    waterChanged = true;
                }
            }

            activeCellData.wetCheck();
        }
        else {
            //DRY GROUND
            if (activeCellData.trySetTerrainDry()) {
                waterChanged = true;
            }

            if (EffectSettings.showRainGrid) {
                if (wetnessGridComponent.addDepth(activeCellData, -0.07f * activeCellData.rainNoise)) {
                    cellsToRainGrid.Add(activeCellData.location);
                    waterChanged = true;
                }
            }
        }

        if (waterChanged) {
            cellActionsPerformed++;
        }
    }

    private void iceCellChecks() {
        if (!iceOrFrostGrid) {
            return;
        }

        activeCellData.temperature = EffectSettings.useMapTemperature
            ? outdoorTemp
            : activeCellData.location.GetTemperature(map);

        switch (activeCellData.temperature) {
            case <= 1: {
                if (EffectSettings.doIce) {
                    if (activeCellData.SetTerrainFrozen()) {
                        //Keeping this here in order to give terrain setting a break
                        cellActionsPerformed++;
                    }
                }

                if (EffectSettings.showFrostGrid) {
                    if (activeCellData.weatherExtension?.holdFrost == true) {
                        //handle frost based on snowing
                        if (!roofed && currentSnowRate > 0.001f) {
                            //if it's snowing remove frost slowly
                            frostGridComponent.addDepth(activeCellData, currentSnowRate * -.01f);
                        }
                        else {
                            frostGridComponent.addDepth(activeCellData, 0.138f * activeCellData.frostNoise);
                        }
                    }
                    else {
                        frostGridComponent.addDepth(activeCellData, 0.138f * activeCellData.frostNoise);
                    }
                }

                break;
            }
            case <= 3: {
                if (activeCellData.TrySetTerrainThawed()) {
                    cellActionsPerformed++;
                }

                if (EffectSettings.showFrostGrid) {
                    frostGridComponent.addDepth(activeCellData, -0.138f * activeCellData.frostNoise);
                }

                break;
            }
            default: {
                if (activeCellData.TrySetTerrainThawed()) {
                    cellActionsPerformed++;
                }

                if (EffectSettings.showFrostGrid) {
                    frostGridComponent.addDepth(activeCellData, -0.276f * activeCellData.frostNoise);
                }

                break;
            }
        }
    }

    //HANDLE PLANT DAMAGES:
    /*
     The effect this has on the map is minimal unless on a desert tile or something.
     Then it just becomes actively harmful
    if (gettingWet) {
        //note - removed ismelt because the dirt shouldn't dry out in winter, and snow wets the ground then.
        if (cell.howWetPlants < 100) {
            if (currentRainRate > 0) {
                cell.howWetPlants += currentRainRate * 2;
            }
            else if (currentSnowRate > 0) {
                cell.howWetPlants += currentSnowRate * 2;
            }
        }
    }
    else {
        if (outdoorTemp > 20) {
            cell.howWetPlants += wetPlantsValue;
            if (cell.howWetPlants <= 0) {
                HurtPlants(c, false, true);
            }
        }
    }
    */
    private void extraRainChecks() {
        if (EffectSettings.showRainEffects && (!isRaining || roofed)) {
            activeCellData.dryCheck();
        }

        if (EffectSettings.makePuddles) {
            if (isRaining &&
                outdoorTemp > 2 &&
                !roofed &&
                activeCellData.currentTerrain.holdSnowOrSand &&
                Rand.Chance(puddleSpawnChance)
               ) {
                FleckCreationData newPuddle = FleckMaker.GetDataStatic(
                    activeCellData.location.ToVector3() + Rand.UnitVector3,
                    map,
                    FlecksDefOf.NPS_PuddleFleck);
                map.flecks.CreateFleck(newPuddle);
            }
        }
    }


    public int GetRiverLevel() {
        if (isRaining) {
            if (rainingPreviousFloodCheck) {
                return HowManyRiverSteps;
            }

            rainingPreviousFloodCheck = true;
        }
        else {
            rainingPreviousFloodCheck = false;
        }

        if (season == Season.Spring ||
            (ModsConfig.OdysseyActive &&
             (map.listerThings.AnyThingWithDef(RimWorld.ThingDefOf.SeasonalFlood) ||
              map.listerThings.AnyThingWithDef(RimWorld.ThingDefOf.TorrentialRainFlood)))) {
            return HowManyRiverSteps;
        }

        if (season == Season.Fall ||
            map.gameConditionManager.ConditionIsActive(GameConditionDefOf.TKKN_Drought) ||
            map.GameConditionManager.GetActiveCondition<GameCondition_Drought>() != null) {
            return 0;
        }

        return HalfRiverSteps;
    }

    private void DoRiverModify(bool force = false) {
        if (!force) {
            if (!EffectSettings.doFloods || !doRiverFlooding || ticksWithModifier % RiverIntervalCheck != 0) {
                return;
            }
        }

        int calculatedRiver = GetRiverLevel();

        if (floodLevel == calculatedRiver) {
            return;
        }

        bool increaseFlood = floodLevel < calculatedRiver;
        if (floodLevel == HowManyRiverSteps) {
            floodLevel--;
        }

        List<cellData> cellsToChange = riverCellsList[floodLevel];
        List<cellData> failedFloodingTiles = [];
        foreach (var cell in cellsToChange.InRandomOrder()) {
            if (increaseFlood) {
                if (!cell.increaseRiver()) {
                    failedFloodingTiles.Add(cell);
                }
            }
            else {
                cell.decreaseRiver();
            }
        }

        //Try again with any cells that may have failed because they were reliant on neighboring cells
        if (increaseFlood) {
            foreach (var failedCell in failedFloodingTiles.InRandomOrder()) {
                failedCell.increaseRiver();
            }

            floodLevel++;
        }
        else {
            floodLevel--;
        }
    }


    public int GetTideLevel() {
        var hoursPassed = GenMath.PositiveModRemap(Find.TickManager.TicksAbs + ticksOffsetFromLongitude, 2500, 24);


        hoursPassed = GenDate.DaysPassed * 24 + hoursPassed;
        float calculatedTide;

        switch (tidalVariant) {
            case TideVariant.SemiDiurnal:
                // For Desmos
                // \operatorname{round}\left(1.5\cdot(\sin((2*\pi)/((25/24)*12)*x))+1.5\right)
                calculatedTide = 1.5f * Mathf.Sin(0.5235988f * hoursPassed) + 1.5f;
                break;
            case TideVariant.Diurnal:
                // For Desmos
                // \operatorname{round}\left(1.5\cdot(\sin((2*\pi)/((25))*x))+1.5\right)
                calculatedTide = 1.5f * Mathf.Sin(0.25132743f * hoursPassed) + 1.5f;
                break;
            case TideVariant.MixedSemiDiurnal:
                /*
                 For Desmos
                \operatorname{round}\left(-\left(1\sin\left(\frac{\pi x}{6.21}\right)+0.6\ \sin\left(\frac{\pi x}{12.42}\right)\right)+1.5\right)
                int mixedSemiDiurnalValue =
                    (int)(-(1 * Mathf.Sin(Mathf.PI * hoursPassed / 6.21f) +
                            0.6 * Mathf.Sin(Mathf.PI * hoursPassed / 12.42f)) + 1.5);
                */
                calculatedTide = -(1 * Mathf.Sin(Mathf.PI * hoursPassed / 6.21f) +
                                   0.6f * Mathf.Sin(Mathf.PI * hoursPassed / 12.42f)) + 1.5f;
                break;
            case TideVariant.None:
            default:
                Log.Error(
                    "NPSWeatherEffects: Tried to get a tidal variant but there wasn't one. Please report this with a log" +
                    " Biome: " + map.Biome +
                    " Coords: " + map.TileInfo.Layer.LongLatOf(map.Tile) +
                    " Seed: " + Find.World.info.seedString);
                throw new ArgumentOutOfRangeException();
        }

        if (map.gameConditionManager.ConditionIsActive(RimWorld.GameConditionDefOf.Eclipse)) {
            calculatedTide++;
            calculatedTide *= 1.25f;
        }

        return Mathf.RoundToInt(Mathf.RoundToInt(calculatedTide) * tideFactor * BaseTideValue);
    }


    private void DoTides(bool force = false) {
        //notes to future me: use this.howManyTideSteps - 1, so we always have a little bit of wet sand, or else it looks stupid.
        if (!force) {
            if (!doCoast || !EffectSettings.doTides || ticksWithModifier % TideIntervalCheck != 0) {
                return;
            }
        }

        if (finishedTideMovement) {
            checkUpToCell = 0;
            int calculatedTide = GetTideLevel();


            if (calculatedTide >= tideCellsList.Count) {
                calculatedTide = tideCellsList.Count - 1;
            }

            if (tideLevel == calculatedTide) {
                return;
            }

            tideIncreasing = tideLevel < calculatedTide;
        }

        List<cellData> cellsToChange = tideCellsList[tideLevel];
        int initialCellCheck = 0;
        if (checkUpToCell == 0) {
            checkUpToCell = cellsToChange.Count / 2;
            finishedTideMovement = false;
        }
        else {
            initialCellCheck = checkUpToCell;
            checkUpToCell = cellsToChange.Count;
        }

        for (int cellCount = initialCellCheck; cellCount < checkUpToCell; cellCount++) {
            cellData cell = cellsToChange[cellCount];
            if (tideIncreasing) {
                if (!cell.increaseTide()) {
                    failedTidalTiles.Add(cell);
                }
            }
            else {
                cell.decreaseTide();
            }
        }

        if (checkUpToCell != cellsToChange.Count) {
            return;
        }

        checkUpToCell = 0;
        finishedTideMovement = true;

        foreach (var cell in failedTidalTiles.InRandomOrder()) {
            if (tideIncreasing) {
                cell.increaseTide();
            }
            else {
                cell.decreaseTide();
            }
        }

        failedTidalTiles.Clear();

        if (tideIncreasing) {
            tideLevel++;
        }
        else {
            tideLevel--;
        }
    }

    /*
     // There's no fair way to implement this. It occurs rarely and the player has no way to react to this happening
    private void HurtPlants(IntVec3 c, bool onlyLow, bool saveHarvest) {
        if (noHurtPlants) {
            return;
        }

        //don't hurt things in growing zone
        if (map.zoneManager.ZoneAt(c) is Zone_Growing) {
            return;
        }

        List<Thing> things = c.GetThingList(map);
        foreach (var thing in things.ToList()) {
            if (thing is not Plant) {
                continue;
            }

            var isLow = true;
            if (onlyLow) {
                isLow = thing.def.altitudeLayer == AltitudeLayer.LowPlant;
            }

            var isHarvestable = true;
            if (saveHarvest) {
                isHarvestable = thing.def.plant.harvestTag != "Standard";
            }

            if (thing.def.category != ThingCategory.Plant || !isLow || !isHarvestable) {
                continue;
            }

            var damage = -.001f;
            damage *= thing.def.plant.fertilityMin;
            thing.TakeDamage(new DamageInfo(DamageDefOf.Rotting, damage, 0, 0));
        }
    }
    */

    public void RemoveEffects() {
        IEnumerable<IntVec3> allCells = map.AllCells;
        //IEnumerable<IntVec3> tmpTerrain = map.AllCells.InRandomOrder();
        Log.Message("NPSWeatherEffects: Resetting cells for map: " + map +
                    " Biome: " + map.Biome +
                    " Biome name: " + map.Biome.label +
                    " Biome source: " + map.Biome?.modContentPack?.Name +
                    " Map size: " + map.Size);
        if (cellWeatherAffectsDict?.Count > 0) {
            foreach (var focusCell in allCells) {
                if (!cellWeatherAffectsDict.TryGetValue(focusCell, out var cellData)) {
                    Log.Error("NPSWeatherEffects: unable to find cellData for " + focusCell);
                    continue;
                }

                // Need to assign the map and locationIndex in case this is initiated on game load.
                // Values are not saved
                cellData.map ??= map;
                if (cellData.locationIndex == 0) {
                    cellData.locationIndex = map.cellIndices.CellToIndex(focusCell);
                }

                cellData.resetCellData();
                frostGridComponent.removeDepth(cellData);
                wetnessGridComponent.removeDepth(cellData);
                map.mapDrawer.MapMeshDirty(focusCell, MapMeshDefOf.NPS_Frost);
                map.mapDrawer.MapMeshDirty(focusCell, MapMeshDefOf.NPS_Rain);
            }

            cellWeatherAffectsDict.Clear();
        }

        cellWeatherAffectsDict = [];
        cellWeatherAffects = [];
        cellWeatherList.Clear();
        riverCellsList.Clear();
        tideCellsList.Clear();
        dontRunAnything = true;
    }

    public void RebuildCellLists() {
        if (EffectSettings.regenCells) {
            regenCellLists = true;
        }

        //rebuild lookup lists.

        biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
        frostGridComponent = map.GetComponent<FrostGrid>();
        wetnessGridComponent = map.GetComponent<WetnessGrid>();
        location = Find.WorldGrid.LongLatOf(map.Tile);
        allPawnsSpawned = map.mapPawns.AllHumanlikeSpawned;
        UpdateBiomeSettings(true);
        frostNoise = new Perlin(0.039999999105930328, 2.0, 0.5, 5,
            map.Tile.tileId, QualityMode.Medium);
        wetnessNoise = new Perlin(0.0321182236075401, 2.0, 0.401477873325348, 5,
            map.Tile.tileId + 1, QualityMode.Medium);

        tideCellsList.Clear();
        riverCellsList.Clear();

        if (!regenCellLists && CurrentRevision != savedRevision) {
            Log.Message("NPSWeatherEffects had to make some large changes. Recreating map effects.");
            RemoveEffects();
            regenCellLists = true;
        }

        dontRunAnything = false;
        if (map.IsPocketMap) {
            //Log.Message("This is a pocket map. Nothing is running");
            dontRunAnything = true;
            return;
        }

        if (!map.IsPlayerHome) {
            if (EffectSettings.onlyPlayerHome) {
                dontRunAnything = true;
                return;
            }
        }

        if (map.Tile.LayerDef?.HasModExtension<PlanetLayerValid>() != true)
        {
            dontRunAnything = true;
            return;
        }

        if (biomeSettings is { activeForBiome: false })
        {
            dontRunAnything = true;
            return;
        }

        if (map.Biome?.inVacuum == true)
        {
            dontRunAnything = true;
            return;
        }

        ticksWithModifier = Find.TickManager.TicksAbs;
        mapArea = map.Area;
        doCoast = map.TileInfo.IsCoastal;
        SurfaceTile surfaceTile = (SurfaceTile)map.TileInfo;
        if (surfaceTile == null) {
            Log.Error("NPSWeatherEffects Looking for a river on a map that isn't a surface tile");
        }

        if (!surfaceTile.Rivers.NullOrEmpty()) {
            doRiverFlooding = true;
        }

        beachTerrain = MapGenUtility.BeachTerrainAt(IntVec3.NorthEast, map);
        if (beachTerrain == RimWorld.TerrainDefOf.Sand) {
            beachTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
        }

        tideFactor = biomeSettings?.tideFactor ?? 1;

        if (tideFactor == 0) {
            doCoast = false;
        }

        if (doCoast) {
            longCurrentTile = Find.WorldGrid.LongLatOf(map.Tile).x;
            ticksOffsetFromLongitude = GenDate.LocalTicksOffsetFromLongitude(longCurrentTile);
            coastRotation = Find.World.CoastDirectionAt(map.Tile);

            if (!coastRotation.IsValid) {
                Log.Error(
                    "NPSWeatherEffects: Tried to generate a coast but could not find coast rotation. This was on the biome " +
                    map.Biome + " From " + map.Biome?.modContentPack?.Name);
                doCoast = false;
            }


            tidalVariant = TideVariant.SemiDiurnal;

            foreach (TileMutatorDef mutator in map.TileInfo.Mutators) {
                MutatorSettings mutatorExtension = mutator.GetModExtension<MutatorSettings>();
                if (mutatorExtension == null) {
                    continue;
                }

                tideFactor *= mutatorExtension.tideFactor;
                if (mutatorExtension.tideVariant != TideVariant.None) {
                    tidalVariant = mutatorExtension.tideVariant;
                }
            }

            howManyTideSteps = Mathf.RoundToInt(BaseTideValue * tideFactor * 3);

            if (howManyTideSteps == 0 || tideFactor == 0) {
                doCoast = false;
            }

            if (!regenCellLists) {
                if (tidalVariant != savedTidalVariant) {
                    Log.Warning("NPSWeatherEffects: Tide variant has changed. Regenerating the map");
                    regenCellLists = true;
                }

                if (howManyTideSteps != savedHowManyTideSteps) {
                    Log.Warning("NPSWeatherEffects: Tide levels have changed. Regenerating the map");
                    regenCellLists = true;
                }
            }
        }

        Rand.PushState(map.Tile.tileId);
        if (regenCellLists) {
            savedRevision = CurrentRevision;
            cellWeatherAffectsDict.Clear();
            cellWeatherAffects = [];
            cellWeatherList.Clear();
            savedTidalVariant = tidalVariant;
            savedHowManyTideSteps = howManyTideSteps;
            finishedTideMovement = true;
            checkUpToCell = 0;
            floodLevel = 0;
            previousQuadrum = Quadrum.Undefined;

            // First loop we get stats and build the initial dictionary
            foreach (var focusCell in map.AllCells) {
                var terrain = focusCell.GetTerrain(map);
                var bottomTerrain = map.terrainGrid.BaseTerrainAt(focusCell);

                if (!focusCell.InBounds(map)) {
                    continue;
                }

                if (bottomTerrain == TerrainDefOf.TKKN_Lava || bottomTerrain == TerrainDefOf.TKKN_LavaRock_RoughHewn) {
                    anyLavaTerrain = true;
                }

                if (!TerrainTagUtil.TideHasTempTide.Contains(
                        MapGenUtility.ShallowOceanWaterTerrainAt(focusCell, map))) {
                    doCoast = false;
                }


                if (isRiverTerrain(focusCell, bottomTerrain)) {
                    if (!MapGenUtility.ShallowMovingWaterTerrainAt(focusCell, map).HasTag("NPS_River")) {
                        Log.Error("Terrain at "+focusCell+MapGenUtility.ShallowMovingWaterTerrainAt(focusCell, map));
                        doRiverFlooding = false;
                    }
                }

                cellData cell = new cellData {
                    location = focusCell, currentTerrain = terrain,
                    locationIndex = map.cellIndices.CellToIndex(focusCell)
                };
                cellWeatherAffectsDict[focusCell] = cell;
                if (doCoast && isOceanicTerrain(focusCell, bottomTerrain)) {
                    cell.tideLevel = 0;
                }
                else if (isRiverTerrain(focusCell, bottomTerrain)) {
                    cell.riverLevel = 0;
                }

                //Spawn special elements:
                SpawnSpecialPlants(focusCell);
                cell.setCurrentExtension();
            }

            //2nd loop we Get the tides
            foreach (var focusCell in map.AllCells.InRandomOrder()) {
                var bottomTerrain = map.terrainGrid.BaseTerrainAt(focusCell);
                if (doCoast) {
                    if (isOceanicTerrain(focusCell, bottomTerrain)) {
                        int tideVariance = howManyTideSteps - Rand.Range(0, Mathf.Min(howManyTideSteps, 5));
                        for (int j = 1; j < tideVariance; j++) {
                            int cellsInRadius = GenRadial.NumCellsInRadius(j);
                            for (int i = 0; i <= cellsInRadius; i++) {
                                IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                                if (!bankCheck.InBounds(map)) {
                                    continue;
                                }

                                TerrainDef bankCheckTerrain = map.terrainGrid.BaseTerrainAt(bankCheck);
                                //Don't want to tide over the river. Or over the ocean...
                                if (isOceanicTerrain(focusCell, bankCheckTerrain) ||
                                    isRiverTerrain(focusCell, bankCheckTerrain)) {
                                    continue;
                                }

                                if (!cellWeatherAffectsDict.TryGetValue(bankCheck, out var affect)) {
                                    List<TileMutatorDef> mutators = map.TileInfo.mutatorsNullable;
                                    string mutatorList = "";
                                    if (mutators?.NullOrEmpty() == true) {
                                        foreach (var mutator in mutatorList) {
                                            mutatorList += mutator + ", ";
                                        }
                                    }

                                    Log.Error("NPSWeatherEffects: Tile: " + bankCheck +
                                              " Biome: " + map.Biome +
                                              (mutatorList.Length > 0 ? " Mutators: " + mutatorList : "") +
                                              "A new cell is being made for a ocean tile when it should already have been created");
                                    affect = new cellData { location = bankCheck, currentTerrain = bankCheckTerrain };
                                }

                                if (bankCheckTerrain == RimWorld.TerrainDefOf.Sand && EffectSettings.doWetSand) {
                                    if (map.terrainGrid.UnderTerrainAt(bankCheck) != null) {
                                        map.terrainGrid.SetUnderTerrain(bankCheck, TerrainDefOf.TKKN_SandBeachWetSalt);
                                    }
                                    else {
                                        map.terrainGrid.SetTerrain(bankCheck, TerrainDefOf.TKKN_SandBeachWetSalt);
                                    }
                                }

                                affect.tideTerrainAt = MapGenUtility.ShallowOceanWaterTerrainAt(bankCheck, map)
                                    .GetModExtension<TerrainWeatherReactions>().tideTerrain;

                                //Prefer lower tide levels first. Then go to cardinal direction 
                                if (j < affect.tideLevel) {
                                    affect.tideLevel = j;
                                    affect.tideFocus = focusCell;
                                    continue;
                                }

                                if (j == affect.tideLevel) {
                                    affect.tideLevel = j;
                                    if (affect.tideFocus != IntVec3.Invalid &&
                                        bankCheck.CardinalTo(affect.tideFocus)) {
                                        continue;
                                    }

                                    affect.tideFocus = focusCell;
                                }
                            }
                        }
                    }
                }

                if (doRiverFlooding) {
                    if (isRiverTerrain(focusCell, bottomTerrain)) {
                        for (int j = 1; j < HowManyRiverSteps; j++) {
                            int riverVariance = GenRadial.NumCellsInRadius(j) - Rand.Range(0, 2);
                            for (int i = 0; i <= riverVariance; i++) {
                                IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                                if (!bankCheck.InBounds(map)) {
                                    continue;
                                }

                                TerrainDef bankCheckTerrain = map.terrainGrid.BaseTerrainAt(bankCheck);
                                if (bottomTerrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                                    TerrainTagUtil.NPS_Water.Contains(bankCheckTerrain)) {
                                    continue;
                                }

                                if (!cellWeatherAffectsDict.TryGetValue(bankCheck, out var affect)) {
                                    List<TileMutatorDef> mutators = map.TileInfo.mutatorsNullable;
                                    string mutatorList = "";
                                    if (mutators?.NullOrEmpty() == true) {
                                        foreach (var mutator in mutatorList) {
                                            mutatorList += mutator + ", ";
                                        }
                                    }

                                    Log.Error("NPSWeatherEffects: Tile: " + bankCheck +
                                              " Biome: " + map.Biome +
                                              (mutatorList.Length > 0 ? " Mutators: " + mutatorList : "") +
                                              "A new cell is being made for a river tile when it should already have been created");
                                    affect = new cellData { location = bankCheck, currentTerrain = bankCheckTerrain };
                                }

                                affect.riverTerrainAt = MapGenUtility.ShallowMovingWaterTerrainAt(bankCheck, map)
                                    .GetModExtension<TerrainWeatherReactions>()?.riverTerrain;

                                //Prefer lower river levels first. Then go to cardinal direction 
                                if (j < affect.riverLevel) {
                                    affect.riverLevel = j;
                                    affect.riverFocus = focusCell;
                                    continue;
                                }

                                if (j == affect.riverLevel) {
                                    affect.riverLevel = j;
                                    if (affect.riverFocus != IntVec3.Invalid &&
                                        bankCheck.CardinalTo(affect.riverFocus)) {
                                        continue;
                                    }

                                    affect.riverFocus = focusCell;
                                }
                            }
                        }
                    }
                }
            }
        }


        cellWeatherList = cellWeatherAffectsDict.Values.ToList();
        cellWeatherAffects = cellWeatherAffectsDict.ToFrozenDictionary();
        cellWeatherList.Shuffle();

        for (var k = 0; k <= howManyTideSteps; k++) {
            tideCellsList.Add([]);
        }

        for (var k = 0; k < HowManyRiverSteps; k++) {
            riverCellsList.Add([]);
        }

        foreach (cellData cellDataValue in cellWeatherList) {
            cellDataValue.locationIndex = map.cellIndices.CellToIndex(cellDataValue.location);
            cellDataValue.map = map;
            cellDataValue.frostNoise = Mathf.Lerp(0.25f, 1f, frostNoise.GetValue(cellDataValue.location));
            cellDataValue.rainNoise = Mathf.Lerp(0.55f, 0.85f, wetnessNoise.GetValue(cellDataValue.location));

            cellDataValue.currentTerrain = map.terrainGrid.TerrainAt(cellDataValue.locationIndex);
            cellDataValue.setCurrentExtension();
            cellDataValue.checkTerrainDry();

            if (cellDataValue.weatherExtension?.holdFrost == true) {
                frostGridComponent.setDepth(cellDataValue.locationIndex, cellDataValue.frostLevel);
            }
            else {
                frostGridComponent.removeDepth(cellDataValue);
            }

            wetnessGridComponent.setDepth(cellDataValue.locationIndex, cellDataValue.rainLevel);


            if (cellDataValue.tideLevel != 999 &&
                cellDataValue.tideLevel != 0) {
                tideCellsList[cellDataValue.tideLevel].Add(cellDataValue);
            }

            if (cellDataValue.riverLevel != 999 &&
                cellDataValue.riverLevel != 0) {
                riverCellsList[cellDataValue.riverLevel].Add(cellDataValue);
            }
        }

        // After calculating the initial tidal levels for all tiles we want to recalculate them.
        // This time, we try to get each cell to point to the lowest nearby level
        //Ignore the first row because that is empty.
        for (int i = 1; i < tideCellsList.Count; i++) {
            foreach (var levelCell in tideCellsList[i]) {
                foreach (var cellAround in GenAdjFast.AdjacentCells8Way(levelCell.location).InRandomOrder()) {
                    if (!cellAround.IsValid)
                        continue;
                    if (!cellWeatherAffects.TryGetValue(cellAround, out var possiblePotentialCell)) {
                        continue;
                    }

                    if (levelCell.tideLevel >= possiblePotentialCell.tideLevel) {
                        levelCell.tideFocus = cellAround;
                    }
                    else if (isOceanicTerrain(cellAround, map.terrainGrid.BaseTerrainAt(cellAround))) {
                        levelCell.tideFocus = cellAround;
                        break;
                    }

                    if (levelCell.tideLevel > possiblePotentialCell.tideLevel)
                        break;
                }
            }
        }

        // After calculating the initial river levels for all tiles we want to recalculate them.
        // This time, we try to get each cell to point to the lowest nearby level
        //Ignore the first row because that is empty.
        for (int i = 1; i < riverCellsList.Count; i++) {
            foreach (var levelCell in riverCellsList[i]) {
                foreach (var cellAround in GenAdjFast.AdjacentCells8Way(levelCell.location).InRandomOrder()) {
                    if (!cellAround.IsValid)
                        continue;
                    if (!cellWeatherAffects.TryGetValue(cellAround, out var possiblePotentialCell)) {
                        continue;
                    }

                    if (levelCell.riverLevel >= possiblePotentialCell.riverLevel) {
                        levelCell.riverFocus = cellAround;
                    }
                    else if (isRiverTerrain(cellAround, map.terrainGrid.BaseTerrainAt(cellAround))) {
                        levelCell.riverFocus = cellAround;
                        break;
                    }

                    if (levelCell.riverLevel > possiblePotentialCell.riverLevel)
                        break;
                }
            }
        }

        Rand.PopState();

        if (!regenCellLists) {
            return;
        }

        SetUpTidesBanks();
        SetUpRiverLevel();
        SetUpWeatherEffects();
        regenCellLists = false;
    }

    private void SpawnSpecialPlants(IntVec3 c) {
        //salt crystals:
        var terrain = c.GetTerrain(map);
        if (ThingDefOf.TKKN_SaltCrystal != null) {
            if (terrain == TerrainDefOf.TKKN_SaltField || terrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                if (c.GetEdifice(map) == null && c.GetCover(map) == null && Rand.Value < .003f) {
                    var plant = (Plant)ThingMaker.MakeThing(ThingDefOf.TKKN_SaltCrystal);
                    plant.Growth = Rand.Range(0.07f, 1f);
                    if (plant.def.plant.LimitedLifespan) {
                        plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                    }

                    GenSpawn.Spawn(plant, c, map);
                }
            }
        }

        if (ThingDefOf.TKKN_PlantBarnacles != null) {
            //barnacles and other ocean stuff
            if (terrain != TerrainDefOf.TKKN_SandBeachWetSalt) {
                return;
            }

            if (c.GetEdifice(map) != null || c.GetCover(map) != null || !(Rand.Value < .003f)) {
                return;
            }

            var barnaclePlant = (Plant)ThingMaker.MakeThing(ThingDefOf.TKKN_PlantBarnacles);
            barnaclePlant.Growth = Rand.Range(0.07f, 1f);
            if (barnaclePlant.def.plant.LimitedLifespan) {
                barnaclePlant.Age = Rand.Range(0, Mathf.Max(barnaclePlant.def.plant.LifespanTicks - 50, 0));
            }

            GenSpawn.Spawn(barnaclePlant, c, map);
        }
    }

    private void SetUpTidesBanks() {
        //set up ocean tides for the first time:
        if (!doCoast) return;
        //set up for low tide
        tideLevel = 0;
        for (int i = 0; i <= howManyTideSteps * 2; i++) {
            DoTides(force: true);
        }
    }

    private void SetUpRiverLevel() {
        if (!EffectSettings.doFloods || !doRiverFlooding) {
            return;
        }

        floodLevel = 0;
        for (int i = 0; i < HowManyRiverSteps; i++) {
            DoRiverModify(force: true);
        }
    }

    private void SetUpWeatherEffects() {
        if (!EffectSettings.showFrostGrid && !EffectSettings.doIce && !EffectSettings.showRainEffects) {
            return;
        }

        isSnowing = map.weatherManager.SnowRate > 0;
        isRaining = !isSnowing && map.weatherManager.RainRate > 0;

        foreach (var focusCell in cellWeatherList) {
            // Can the soil be null? I'm not really sure but Imma check it just in case
            if (map.edificeGrid[focusCell.locationIndex] != null) {
                continue;
            }

            focusCell.currentTerrain = map.terrainGrid.TerrainAt(focusCell.locationIndex) ?? RimWorld.TerrainDefOf.Soil;
            focusCell.setCurrentExtension();
            if (isRaining) {
                if (EffectSettings.showRainEffects) {
                    if (!map.roofGrid.Roofed(focusCell.locationIndex)) {
                        focusCell.forceTerrainWater();
                    }

                    focusCell.rainLevel = focusCell.rainNoise;
                    wetnessGridComponent.setDepth(focusCell.locationIndex, focusCell.rainNoise);
                    map.mapDrawer.MapMeshDirty(focusCell.location, MapMeshDefOf.NPS_Frost);
                    map.mapDrawer.MapMeshDirty(focusCell.location, MapMeshDefOf.NPS_Rain);
                }
            }


            focusCell.temperature = EffectSettings.useMapTemperature
                ? map.mapTemperature.OutdoorTemp
                : focusCell.location.GetTemperature(map);

            if (EffectSettings.doIce) {
                focusCell.forceTerrainFrozen();
            }

            if (EffectSettings.showFrostGrid) {
                if (!isSnowing && focusCell.temperature <= 1) {
                    if (focusCell.weatherExtension?.holdFrost == true) {
                        if (map.snowGrid.GetDepth(focusCell.location) == 0) {
                            focusCell.frostLevel = focusCell.frostNoise;
                            frostGridComponent.setDepth(focusCell.locationIndex, focusCell.frostNoise);
                        }
                    }
                }
            }
        }
    }

    private bool isOceanicTerrain(IntVec3 focusCell, TerrainDef terrain) {
        TerrainDef shallowTerrain = MapGenUtility.ShallowOceanWaterTerrainAt(focusCell, map);
        if (terrain == shallowTerrain) {
            return true;
        }

        // Only check mod extension if the terrain is temporary
        // because the ocean and river terrains should be temporary
        if (terrain.temporary) {
            var shallowWeatherReaction = shallowTerrain.GetModExtension<TerrainWeatherReactions>();
            if (shallowWeatherReaction?.tideTerrain == terrain)
                return true;
        }

        return terrain == MapGenUtility.DeepOceanWaterTerrainAt(focusCell, map);
    }

    private bool isRiverTerrain(IntVec3 focusCell, TerrainDef terrain) {
        TerrainDef riverTerrain = MapGenUtility.ShallowMovingWaterTerrainAt(focusCell, map);
        if (terrain == riverTerrain) {
            return true;
        }

        // Only check mod extension if the terrain is temporary
        // because the ocean and river terrains should be temporary
        if (terrain.temporary) {
            var shallowWeatherReaction = riverTerrain.GetModExtension<TerrainWeatherReactions>();
            if (shallowWeatherReaction?.riverTerrain == terrain)
                return true;
        }

        return terrain == MapGenUtility.DeepMovingWaterTerrainAt(focusCell, map);
    }

    public void Dispose() {
        frostNoise?.Dispose();
        wetnessNoise?.Dispose();
    }
}