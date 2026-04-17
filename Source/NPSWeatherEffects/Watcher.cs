using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using NPSWeather.Rain;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NPSWeather;

public class Watcher(Map map) : MapComponent(map)
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

    private const int MaxPuddles = 1000;


    private BiomeSeasonalSettings biomeSettings;
    private Dictionary<IntVec3, cellData> cellWeatherAffectsDict = new();
    public FrozenDictionary<IntVec3, cellData> cellWeatherAffects;
    private List<cellData> cellWeatherList = [];

    private int cycleIndex;

    private List<List<cellData>> riverCellsList = [];
    public int floodLevel;
    private int floodThreat;
    private int floodThreatIncrease;

    public FrostGrid frostGridComponent;
    public WetnessGrid wetnessGridComponent;
    private Vector2 location;

    private ModuleBase frostNoise;
    private ModuleBase wetnessNoise;


    public float outdoorTemp;

    //used by weather
    public bool regenCellLists = true;

    private int ticks;

    //rebuild every save to keep file size down
    public bool doCoast = true; //false if no coast
    private readonly List<List<cellData>> tideCellsList = [];
    public int tideLevel; // 0 - 13
    public float tideFactor = 1;
    private Rot4 coastRotation;

    private float longCurrentTile;
    private long ticksOffsetFromLongitude;
    private int checkUpToCell;
    private List<cellData> failedTidalTiles = [];
    private bool finishedTideMovement = true;
    private bool tideIncreasing;
    private TideVariant savedTidalVariant;
    private TideVariant tidalVariant;

    private int totalPuddles;

    //Values not in use
    //private float humidity;
    //private float wetPlantsValue;

    public bool dontRunAnything;
    private bool anyLavaTerrain;

    private bool doUnpacking;

    //Not using this value
    //Value not in use
    //private bool noHurtPlants;
    public readonly Dictionary<Pawn, bool> validPawns = [];
    public float currentRainRate;
    private float currentSnowRate;
    private int mapArea; //Default area 62500
    public bool isRaining;
    private TerrainDef oceanTerrain;
    private TerrainDef deepOceanTerrain;
    private TerrainDef beachTerrain;
    private TerrainDef shallowRiverTerrain;
    public bool doRiverFlooding;
    private bool iceOrFrostGrid;
    private bool doRoofChecks;
    private IReadOnlyList<Pawn> allPawnsSpawned;
    public Season season;
    private Quadrum quadrum;
    private Quadrum previousQuadrum = Quadrum.Undefined;
    public bool droughtActive;

    private const int MapCheckInterval = 625;
    private const int MinimumCellsPerTick = 5;
    private int cellActionsPerformed;
    private int cellActionsPerTick = 5;

    /* STANDARD STUFF */

    public override void FinalizeInit() {
        base.FinalizeInit();
        RebuildCellLists();
        mapChecks();
        allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
    }

    public override void MapComponentTick() {
        if (dontRunAnything) {
            return;
        }

        ticks = Find.TickManager.TicksGame;

        isRaining = currentRainRate > 0;
        //environmental changes
        if (EffectSettings.doWeather) {
            if (ticks % MapCheckInterval == 0) {
                mapChecks();

                if (cellActionsPerformed > EffectSettings.maxCellsPerTick / 3) {
                    cellActionsPerTick = Math.Min(EffectSettings.maxCellsPerTick, cellActionsPerTick + 10);
                    //Log.Warning("New cell per tick value " + cellActionsPerTick);
                }
                else if (cellActionsPerformed < EffectSettings.maxCellsPerTick / 4) {
                    cellActionsPerTick = MinimumCellsPerTick;
                    //Log.Message("New cell per tick value " + cellActionsPerTick);
                }

                cellActionsPerformed = 0;
            }

            DoTides();
            DoRiverModify();

            for (var i = 0; i < cellActionsPerTick; i++) {
                if (cycleIndex >= mapArea) {
                    cycleIndex = 0;
                }

                DoCellEnvironment(cellWeatherList[cycleIndex]);
                cycleIndex++;
            }
        }

        if (EffectSettings.allowPawnEffects) {
            // rebuild the dictionary of valid pawns every quadrum
            if (ticks % 900000 == 0) {
                validPawns.Clear();
            }

            for (int i = 0; i < allPawnsSpawned.Count; i++) {
                if (checkPawnHuman(allPawnsSpawned[i]))
                    PawnChecks.checks(allPawnsSpawned[i], map, this, isRaining, ticks);
            }
        }

        UpdateBiomeSettings();
    }

    private bool checkPawnHuman(Pawn pawn) {
        if ((pawn.HashOffsetTicks() + ticks) % 10 != 0) {
            return false;
        }

        if (EffectSettings.pawnEffectsOnlyColonists && !pawn.IsColonist) {
            return false;
        }

        if (validPawns.TryGetValue(pawn, out var result)) {
            return result;
        }

        if (!pawn.RaceProps.Humanlike ||
            pawn.IsShambler ||
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
        /*
        var baseHumidity = (map.TileInfo.rainfall + 1) * (map.TileInfo.temperature + 1) *
                           (map.TileInfo.swampiness + 1);
        var currentHumidity =
            (1 + currentRainRate) * (1 + outdoorTemp);
        humidity = ((baseHumidity + currentHumidity) / 1000) + 18;
        wetPlantsValue = -1 * (outdoorTemp / humidity / 10);
        //noHurtPlants = !EffectSettings.allowPlantEffects || ticks % 150 != 0;
        */
        floodThreatIncrease = 1 + 2 * (int)Math.Round(currentRainRate);

        doUnpacking = EffectSettings.doDirtPath && !doUnpacking;
        iceOrFrostGrid = EffectSettings.doIce || EffectSettings.showFrostGrid;
        doRoofChecks = EffectSettings.showWetTerrain || EffectSettings.showFrostGrid;
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref regenCellLists, "regenCellLists", true);
        Scribe_Collections.Look(ref cellWeatherAffectsDict, "cellWeatherAffects", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref floodThreat, "floodThreat");
        Scribe_Values.Look(ref tideLevel, "tideLevel");
        Scribe_Values.Look(ref floodLevel, "floodLevel");
        Scribe_Values.Look(ref totalPuddles, "totalPuddles", totalPuddles);
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
            if (ticks % BiomeUpdateCheck != 0) {
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


        if (biomeSettings == null)
            return;
        if (quadrum == previousQuadrum) {
            return;
        }

        previousQuadrum = quadrum;

        biomeSettings.setWeatherBySeason(map, season, quadrum);
        biomeSettings.setDiseaseBySeason(map, season, quadrum);
        biomeSettings.setIncidentsBySeason(map, season, quadrum);
    }

    private bool gettingWet;
    private bool roofed;
    private TerrainDef currentTerrain;


    private void DoCellEnvironment(cellData cell) {
        var c = cell.location;

        if (map.edificeGrid[cell.locationIndex] != null) {
            return;
        }

        if (doUnpacking) {
            cell.Unpack();
        }

        // Can the soil be null? I'm not really sure but Imma check it just in case
        currentTerrain = map.terrainGrid.TerrainAt(cell.locationIndex) ?? RimWorld.TerrainDefOf.Soil;
        if (cell.currentTerrain != currentTerrain) {
            cell.currentTerrain = currentTerrain;
            cell.setCurrentExtension();
        }

        roofed = doRoofChecks && map.roofGrid.Roofed(cell.locationIndex);
        gettingWet = false;

        /*
        //check if the terrain has been floored
        // Should there be a blanket check on if a terrain is a floor? Thought for future me

        if (currentTerrain.designationCategory == DesignationCategoryDefOf.Floors) {
            //cell.baseTerrain = currentTerrain;
        }*/

        //spawn special things
        if (anyLavaTerrain) {
            LavaRockSpecials(c);
        }

        if (EffectSettings.showRainEffects) {
            if (!roofed) {
                //if it's raining in this cell:
                if (isRaining) {
                    if (floodThreat < 1090000) {
                        floodThreat += floodThreatIncrease;
                    }

                    gettingWet = true;
                    if (cell.setTerrainWater()) {
                        cellActionsPerformed++;
                    }

                    if (EffectSettings.showRainGrid) {
                        if (wetnessGridComponent.addDepth(cell, currentRainRate * 0.178f)) {
                            cellActionsPerformed++;
                        }
                    }
                }
                else {
                    if (currentRainRate == 0) {
                        floodThreat--;
                    }

                    //DRY GROUND
                    if (cell.trySetTerrainDry()) {
                        cellActionsPerformed++;
                    }

                    if (EffectSettings.showRainGrid) {
                        if (wetnessGridComponent.subDepth(cell, -0.05f * cell.rainNoise)) {
                            cellActionsPerformed++;
                        }
                    }
                }
            }
            else {
                if (EffectSettings.showRainGrid) {
                    if (wetnessGridComponent.subDepth(cell, -0.05f * cell.rainNoise)) {
                        cellActionsPerformed++;
                    }
                }
            }
        }
        else {
            cell.trySetTerrainDry();
        }

        if (iceOrFrostGrid) {
            cell.temperature = EffectSettings.useMapTemperature ? outdoorTemp : cell.location.GetTemperature(map);

            if (cell.temperature <= 1) {
                if (EffectSettings.doIce) {
                    if (cell.SetTerrainFrozen()) {
                        cellActionsPerformed++;
                    }
                }

                if (EffectSettings.showFrostGrid) {
                    if (cell.weatherExtension?.holdFrost == true) {
                        //handle frost based on snowing
                        if (!roofed && currentSnowRate > 0.001f) {
                            //if it's snowing remove frost slowly
                            if (frostGridComponent.addDepth(cell, currentSnowRate * -.01f)) {
                                cellActionsPerformed++;
                            }
                        }
                        else {
                            if (frostGridComponent.addDepth(cell, 0.138f * cell.frostNoise)) {
                                cellActionsPerformed++;
                            }
                        }
                    }
                    else {
                        frostGridComponent.setDepth(cell.locationIndex, 0);
                    }
                }
            }
            else if (cell.temperature >= 2) {
                if (cell.TrySetTerrainThawed()) {
                    cellActionsPerformed++;
                }

                if (EffectSettings.showFrostGrid) {
                    if (frostGridComponent.removeDepth(cell)) {
                        cellActionsPerformed++;
                    }
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

        if (EffectSettings.showRainEffects) {
            if (cell.wetCheck(gettingWet)) {
                cellActionsPerformed++;
            }
        }

        if (EffectSettings.makePuddles) {
            if (cell.howWet == 3 && (outdoorTemp > 2 && MaxPuddles > totalPuddles &&
                                     currentTerrain != TerrainDefOf.TKKN_SandBeachWetSalt)) {
                FilthMaker.TryMakeFilth(c, map, ThingDefOf.TKKN_FilthPuddle);
                totalPuddles++;
            }
        }
    }

    private void LavaRockSpecials(IntVec3 c) {
        if (Rand.Value < .0001f) {
            if (c.InBounds(map)) {
                if (currentTerrain == TerrainDefOf.TKKN_Lava) {
                    var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock);
                    GenSpawn.Spawn(thing, c, map);
                }
                else if (currentTerrain == TerrainDefOf.TKKN_LavaRock_RoughHewn &&
                         map.Biome == BiomeDefOf.TKKN_VolcanicFlow &&
                         map.listerThings.ThingsOfDef(ThingDefOf.TKKN_SteamVent).Count < 10) {
                    var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_SteamVent);
                    GenSpawn.Spawn(thing, c, map);
                }
            }
        }
    }

    public int GetRiverLevel() {
        if (floodThreat > 1000000 || season == Season.Spring) {
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
            if (!EffectSettings.doFloods || !doRiverFlooding || ticks % RiverIntervalCheck != 0) {
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
                if (!cell.increaseRiver(shallowRiverTerrain)) {
                    failedFloodingTiles.Add(cell);
                }
            }
            else {
                cell.decreaseRiver(shallowRiverTerrain);
            }
        }

        //Try again with any cells that may have failed because they were reliant on neighboring cells
        if (increaseFlood) {
            foreach (var failedCell in failedFloodingTiles.InRandomOrder()) {
                failedCell.increaseRiver(shallowRiverTerrain);
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
            case TideVariant.Weak:
            case TideVariant.Strong:
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
            if (!doCoast || !EffectSettings.doTides || ticks % TideIntervalCheck != 0) {
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
                if (!cell.increaseTide(oceanTerrain)) {
                    failedTidalTiles.Add(cell);
                }
            }
            else {
                cell.decreaseTide(oceanTerrain);
            }
        }

        if (checkUpToCell != cellsToChange.Count) {
            return;
        }

        checkUpToCell = 0;
        finishedTideMovement = true;

        foreach (var cell in failedTidalTiles.InRandomOrder()) {
            if (tideIncreasing) {
                cell.increaseTide(oceanTerrain);
            }
            else {
                cell.decreaseTide(oceanTerrain);
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
                    " Biome source: " + map.Biome.modContentPack.Name +
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

        if (beachTerrain == RimWorld.TerrainDefOf.Sand) {
            beachTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
        }
        //rebuild lookup lists.

        shallowRiverTerrain = TerrainDefOf.NPS_WaterRiverFlood;
        biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
        frostGridComponent = map.GetComponent<FrostGrid>();
        wetnessGridComponent = map.GetComponent<WetnessGrid>();
        location = Find.WorldGrid.LongLatOf(map.Tile);
        allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
        UpdateBiomeSettings(true);
        frostNoise = new Perlin(0.039999999105930328, 2.0, 0.5, 5,
            map.Tile.tileId, QualityMode.Medium);
        wetnessNoise = new Perlin(0.0321182236075401, 2.0, 0.401477873325348, 5,
            map.Tile.tileId + 1, QualityMode.Medium);

        tideCellsList.Clear();
        riverCellsList.Clear();
        Rand.PushState(map.Tile.tileId);

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

        if (map.Biome.inVacuum) {
            //Log.Message("In the vacuum of space. Not running");
            dontRunAnything = true;
            return;
        }

        ticks = Find.TickManager.TicksGame;
        mapArea = map.Area;
        doCoast = map.TileInfo.IsCoastal;

        if (MapGenUtility.ShallowOceanWaterTerrainAt(IntVec3.NorthEast, map) !=
            RimWorld.TerrainDefOf.WaterOceanShallow) {
            doCoast = false;
        }

        //oceanTerrain = MapGenUtility.ShallowOceanWaterTerrainAt(new IntVec3(1, 0, 1), map);
        deepOceanTerrain = MapGenUtility.DeepOceanWaterTerrainAt(IntVec3.NorthEast, map);
        oceanTerrain = TerrainDefOf.NPS_WaterOceanTide;
        beachTerrain = MapGenUtility.BeachTerrainAt(IntVec3.NorthEast, map);

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
                    map.Biome + " From " + map.Biome.modContentPack?.Name);
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
            foreach (var focusCell in map.AllCells.InRandomOrder()) {
                var terrain = focusCell.GetTerrain(map);
                var bottomTerrain = map.terrainGrid.BaseTerrainAt(focusCell);

                if (!focusCell.InBounds(map)) {
                    continue;
                }

                if (bottomTerrain == TerrainDefOf.TKKN_Lava || bottomTerrain == TerrainDefOf.TKKN_LavaRock_RoughHewn) {
                    anyLavaTerrain = true;
                }

                if (isRiverTerrain(bottomTerrain)) {
                    doRiverFlooding = true;
                }

                cellData cell = new cellData {
                    location = focusCell, currentTerrain = terrain,
                    locationIndex = map.cellIndices.CellToIndex(focusCell)
                };
                cellWeatherAffectsDict[focusCell] = cell;
                if (doCoast && isOceanicTerrain(bottomTerrain)) {
                    cell.tideLevel = 0;
                }
                else if (isRiverTerrain(bottomTerrain)) {
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
                    if (isOceanicTerrain(bottomTerrain)) {
                        int tideVariance = howManyTideSteps - Rand.Range(0, Mathf.Min(howManyTideSteps, 5));
                        for (var j = 1; j < tideVariance; j++) {
                            var num = GenRadial.NumCellsInRadius(j);
                            for (var i = 0; i <= num; i++) {
                                IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                                if (!bankCheck.InBounds(map)) {
                                    continue;
                                }

                                TerrainDef bankCheckTerrain = map.terrainGrid.BaseTerrainAt(bankCheck);
                                //Don't want to tide over the river. Or over the ocean...
                                if (isOceanicTerrain(bankCheckTerrain) || isRiverTerrain(bankCheckTerrain)) {
                                    continue;
                                }

                                if (!cellWeatherAffectsDict.TryGetValue(bankCheck, out var affect)) {
                                    affect = new cellData { location = bankCheck, currentTerrain = bankCheckTerrain };
                                }

                                if (bankCheckTerrain == RimWorld.TerrainDefOf.Sand) {
                                    if (map.terrainGrid.UnderTerrainAt(bankCheck) != null) {
                                        map.terrainGrid.SetUnderTerrain(bankCheck, TerrainDefOf.TKKN_SandBeachWetSalt);
                                    }
                                    else {
                                        map.terrainGrid.SetTerrain(bankCheck, TerrainDefOf.TKKN_SandBeachWetSalt);
                                    }
                                }

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

                if (isRiverTerrain(bottomTerrain)) {
                    for (var j = 1; j < HowManyRiverSteps; j++) {
                        int riverVariance = GenRadial.NumCellsInRadius(j) - Rand.Range(0, 2);
                        for (var i = 0; i <= riverVariance; i++) {
                            IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                            if (!bankCheck.InBounds(map)) {
                                continue;
                            }

                            TerrainDef bankCheckTerrain = map.terrainGrid.BaseTerrainAt(bankCheck);
                            if (bottomTerrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                                TerrainTagUtil.TKKN_Wet.Contains(bankCheckTerrain)) {
                                continue;
                            }

                            if (!cellWeatherAffectsDict.TryGetValue(bankCheck, out var affect)) {
                                affect = new cellData { location = bankCheck, currentTerrain = bankCheckTerrain };
                            }

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


        cellWeatherList = cellWeatherAffectsDict.Values.ToList();
        cellWeatherAffects = cellWeatherAffectsDict.ToFrozenDictionary();
        cellWeatherList.Shuffle();

        for (var k = 0; k <= howManyTideSteps; k++) {
            tideCellsList.Add([]);
        }

        for (var k = 0; k < HowManyRiverSteps; k++) {
            riverCellsList.Add([]);
        }

        foreach ((IntVec3 _, cellData cellDataValue) in cellWeatherAffects) {
            cellDataValue.locationIndex = map.cellIndices.CellToIndex(cellDataValue.location);
            cellDataValue.map = map;
            cellDataValue.frostNoise = Mathf.Lerp(0.25f, 0.1f, frostNoise.GetValue(cellDataValue.location));
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
                    else if (isOceanicTerrain(map.terrainGrid.BaseTerrainAt(cellAround))) {
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
                    else if (isRiverTerrain(map.terrainGrid.BaseTerrainAt(cellAround))) {
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

        isRaining = map.weatherManager.RainRate > 0;

        bool isSnowing = map.weatherManager.SnowRate > 0;

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

                    //TODO separate settings for wetness grid, flooding, and wet
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

    private bool isOceanicTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterOceanShallow ||
               terrain == TerrainDefOf.NPS_WaterOceanTide ||
               terrain == oceanTerrain ||
               terrain == RimWorld.TerrainDefOf.WaterOceanDeep ||
               terrain == deepOceanTerrain;
    }

    private bool isRiverTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterMovingShallow ||
               terrain == TerrainDefOf.NPS_WaterRiverFlood ||
               terrain == RimWorld.TerrainDefOf.WaterMovingChestDeep;
    }
}