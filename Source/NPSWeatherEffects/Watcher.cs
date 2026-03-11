using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace NPSWeather;

public class Watcher(Map map) : MapComponent(map)
{
    private const int HowManyRiverSteps = 6;
    private readonly int halfRiverSteps = (int)Math.Round((HowManyRiverSteps - 1M) / 2);
    private const int MaxRiverSteps = HowManyRiverSteps - 1;

    private int howManyTideSteps = 13;
    private int halfTideSteps = 6;
    private int maxTideSteps = 12;

    //Every quarter hour
    private const int TideIntervalCheck = 625;

    //Every 6 hours
    private const int RiverIntervalCheck = 15000;

    private const int MaxPuddles = 1000;


    private BiomeSeasonalSettings biomeSettings;
    public Dictionary<IntVec3, cellData> cellWeatherAffects = new();
    private int cycleIndex;
    private bool doCoast = true; //false if no coast
    private List<List<IntVec3>> riverCellsList = [];

    private int floodLevel; // 0 - 3
    private int floodThreat;
    private int floodThreatIncrease;

    public FrostGrid frostGridComponent;
    private Vector2 location;

    private ModuleBase frostNoise;
    private float humidity;

    public float outdoorTemp;

    //used by weather
    private bool regenCellLists = true;

    private int ticks;

    //rebuild every save to keep file size down
    private List<List<IntVec3>> tideCellsList = [];
    private int tideLevel; // 0 - 13
    private int previousTideLevel;
    private int totalPuddles;

    private float wetPlantsValue;

    public bool dontRunAnything;
    private bool anyLavaTerrain;
    private bool doUnpacking;
    private bool noHurtPlants;
    public readonly Dictionary<Pawn, bool> validPawns = [];
    private float currentRainRate;
    private float currentSnowRate;
    private int mapArea; //Default area 62500
    private bool isRaining;
    private Rot4 coastRotation;
    private TerrainDef oceanTerrain;
    private TerrainDef beachTerrain;
    private TerrainDef shallowRiverTerrain;
    private bool doRiverFlooding;
    private bool iceOrFrostGrid;
    private bool doRoofChecks;
    private IReadOnlyList<Pawn> allPawnsSpawned;

    /* STANDARD STUFF */

    public override void FinalizeInit() {
        base.FinalizeInit();
        if (map.IsPocketMap) {
            //Log.Message("This is a pocket map. Nothing is running");
            dontRunAnything = true;
            return;
        }

        if (map.Biome.inVacuum) {
            //Log.Message("In the vacuum of space. Not running");
            dontRunAnything = true;
            return;
        }

        ticks = Find.TickManager.TicksGame;
        mapArea = map.Area;
        doCoast = map.TileInfo.IsCoastal;

        if (MapGenUtility.ShallowOceanWaterTerrainAt(new IntVec3(1, 0, 1), map) !=
            RimWorld.TerrainDefOf.WaterOceanShallow) {
            doCoast = false;
        }

        IList<TileMutatorDef> mutators = map.TileInfo.Mutators;
        if (mutators.Contains(TileMutatorDefOf.VEE_RisingWaters)) {
            doCoast = false;
        }

        //oceanTerrain = MapGenUtility.ShallowOceanWaterTerrainAt(new IntVec3(1, 0, 1), map);
        oceanTerrain = TerrainDefOf.NPS_WaterOceanTide;
        beachTerrain = MapGenUtility.BeachTerrainAt(new IntVec3(1, 0, 1), map);
        if (doCoast) {
            coastRotation = Find.World.CoastDirectionAt(map.Tile);
            if (!coastRotation.IsValid) {
                Log.Error(
                    "NPSWeatherEffects: Tried to generate a coast but could not find coast rotation. This was on the biome " +
                    map.Biome + " From " + map.Biome.modContentPack?.Name);
                doCoast = false;
            }

            if (mutators.Contains(TileMutatorDefOf.NPS_StrongOceanTide)) {
                howManyTideSteps = 19;
                halfTideSteps = 9;
                maxTideSteps = 18;
            }
            else if (mutators.Contains(TileMutatorDefOf.NPS_WeakOceanTide)) {
                howManyTideSteps = 7;
                halfTideSteps = 3;
                maxTideSteps = 6;
            }
        }

        if (beachTerrain == RimWorld.TerrainDefOf.Sand) {
            beachTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
        }

        shallowRiverTerrain = TerrainDefOf.NPS_WaterRiverFlood;

        biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
        frostGridComponent = map.GetComponent<FrostGrid>();
        location = Find.WorldGrid.LongLatOf(map.Tile);
        allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
        UpdateBiomeSettings(true);

        frostNoise = new Perlin(0.039999999105930328, 2.0, 0.5, 5,
            Rand.Range(0, 651431), QualityMode.Medium);

        RebuildCellLists();
    }

    public override void MapComponentTick() {
        if (dontRunAnything) {
            return;
        }

        ticks = Find.TickManager.TicksGame;
        
        
        isRaining = currentRainRate > 0;
        //environmental changes
        if (EffectSettings.doWeather) {
            //set up humidity
            outdoorTemp = map.mapTemperature.OutdoorTemp;
            currentRainRate = map.weatherManager.curWeather.rainRate;
            currentSnowRate = map.weatherManager.curWeather.snowRate;
            var baseHumidity = (map.TileInfo.rainfall + 1) * (map.TileInfo.temperature + 1) *
                               (map.TileInfo.swampiness + 1);
            var currentHumidity =
                (1 + currentRainRate) * (1 + outdoorTemp);
            humidity = ((baseHumidity + currentHumidity) / 1000) + 18;
            wetPlantsValue = -1 * (outdoorTemp / humidity / 10);
            floodThreatIncrease = 1 + 2 * (int)Math.Round(currentRainRate);
            noHurtPlants = !EffectSettings.allowPlantEffects || ticks % 150 != 0;
            doUnpacking = EffectSettings.doDirtPath && !doUnpacking;
            iceOrFrostGrid = EffectSettings.doIce || EffectSettings.showFrostGrid;
            doRoofChecks = EffectSettings.showRain || EffectSettings.showFrostGrid;
            DoTides();
            DoRiverModify();

            for (var i = 0; i < EffectSettings.cellsPerTick; i++) {
                if (cycleIndex >= mapArea) {
                    cycleIndex = 0;
                }

                DoCellEnvironment(map.cellsInRandomOrder.Get(cycleIndex));
                cycleIndex++;
            }
        }

        if (EffectSettings.allowPawnEffects) {
            if (ticks % 250 == 0) {
                allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
            }

            //rebuild the list of valid pawns every quadrum
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

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref regenCellLists, "regenCellLists", true);
        Scribe_Collections.Look(ref cellWeatherAffects, "cellWeatherAffects", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref floodThreat, "floodThreat");
        Scribe_Values.Look(ref tideLevel, "tideLevel");
        Scribe_Values.Look(ref totalPuddles, "totalPuddles", totalPuddles);
        Scribe_Values.Look(ref doRiverFlooding, "doRiverFlooding", doRiverFlooding);
        Scribe_Values.Look(ref anyLavaTerrain, "anyLavaTerrain", anyLavaTerrain);
        Scribe_Values.Look(ref howManyTideSteps, "HowManyTideSteps", 13);
        Scribe_Values.Look(ref halfTideSteps, "halfTideSteps", 6);
        Scribe_Values.Look(ref maxTideSteps, "MaxTideSteps", 12);
    }


    private void RebuildCellLists() {
        if (EffectSettings.regenCells) {
            regenCellLists = true;
        }

        if (regenCellLists) {
            //random so we can spawn plants and stuff in this step.
            IEnumerable<IntVec3> tmpTerrain = map.AllCells.InRandomOrder();
            cellWeatherAffects = new Dictionary<IntVec3, cellData>();
            foreach (var focusCell in tmpTerrain) {
                var terrain = focusCell.GetTerrain(map);

                if (!focusCell.InBounds(map)) {
                    continue;
                }

                if (terrain == TerrainDefOf.TKKN_Lava || terrain == TerrainDefOf.TKKN_LavaRock_RoughHewn) {
                    anyLavaTerrain = true;
                }

                if (isRiverTerrain(terrain)) {
                    doRiverFlooding = true;
                }

                cellData cell = new cellData {
                    location = focusCell, currentTerrain = terrain, howWetPlants = 70,
                    locationIndex = map.cellIndices.CellToIndex(focusCell)
                };

                var frostVal = frostNoise.GetValue(focusCell) + 1;
                frostVal += 1f;
                frostVal *= 0.5f;
                if (frostVal < 0.5f) {
                    frostVal = 0.5f;
                }

                cell.frostNoise = frostVal;

                if (terrain == RimWorld.TerrainDefOf.Sand ||
                    terrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                    terrain == beachTerrain) {
                    //get all the sand pieces that are touching the beach.
                    for (var j = 0; j < howManyTideSteps; j++) {
                        // Checks to see if water is in the direction of the cell
                        // checks every cell up to HowManyTideSteps and will change the cell to TKKN_SandBeachWetSalt if this is true
                        var waterCheck = AdjustForRotation(focusCell, j);
                        if (!waterCheck.InBounds(map) ||
                            !isOceanicTerrain(waterCheck.GetTerrain(map))) {
                            continue;
                        }

                        if (terrain == RimWorld.TerrainDefOf.Sand ||
                            terrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                            map.terrainGrid.SetTerrain(focusCell, TerrainDefOf.TKKN_SandBeachWetSalt);
                        }

                        cell.tideLevel = j;
                        break;
                    }
                }
                else if (isRiverTerrain(terrain)) {
                    cell.riverLevel = 0;
                    for (var j = 0; j < HowManyRiverSteps; j++) {
                        var num = GenRadial.NumCellsInRadius(j);
                        for (var i = 0; i < num; i++) {
                            IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                            if (!bankCheck.InBounds(map)) {
                                continue;
                            }

                            TerrainDef bankCheckTerrain = bankCheck.GetTerrain(map);
                            if (terrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                                TerrainTagUtil.TKKN_Wet.Contains(bankCheckTerrain)) {
                                continue;
                            }

                            cellData bankCell;
                            if (cellWeatherAffects.TryGetValue(bankCheck, out var affect))
                                bankCell = affect;
                            else
                                bankCell = new cellData { location = bankCheck, currentTerrain = bankCheckTerrain };

                            if (j <= bankCell.riverLevel) {
                                bankCell.riverLevel = j;
                                // If bankCell has already had a riverFocus assigned, and the distance is further away, ignore it.
                                // Otherwise, assign it focusCell 
                                if (bankCell.riverFocus != IntVec3.Invalid &&
                                    bankCheck.DistanceToSquared(bankCell.riverFocus) <
                                    bankCheck.DistanceToSquared(focusCell)) {
                                    continue;
                                }

                                bankCell.riverFocus = focusCell;
                            }
                        }
                    }
                }

                //Spawn special elements:
                SpawnSpecialPlants(focusCell);
                cell.setCurrentExtension();

                cellWeatherAffects[focusCell] = cell;
            }
        }


        //rebuild lookup lists.
        tideCellsList = [];
        riverCellsList = [];

        for (var k = 0; k < howManyTideSteps; k++) {
            tideCellsList.Add([]);
        }

        for (var k = 0; k < HowManyRiverSteps; k++) {
            riverCellsList.Add([]);
        }

        foreach ((IntVec3 cellLocation, cellData cellDataValue) in cellWeatherAffects) {
            cellDataValue.locationIndex = map.cellIndices.CellToIndex(cellDataValue.location);
            cellWeatherAffects[cellLocation].map = map;

            cellDataValue.currentTerrain = map.terrainGrid.TerrainAt(cellDataValue.locationIndex);
            cellDataValue.setCurrentExtension();

            if (cellDataValue?.weatherExtension?.holdFrost == true) {
                frostGridComponent.SetDepth(cellDataValue.locationIndex, cellDataValue.frostLevel);
            }

            if (cellDataValue.tideLevel > -1) {
                tideCellsList[cellDataValue.tideLevel].Add(cellLocation);
            }

            if (cellDataValue.riverLevel != 999 &&
                cellDataValue.riverLevel != 0) {
                riverCellsList[cellDataValue.riverLevel].Add(cellLocation);
            }
        }

        // After calculating the initial river levels for all tiles we want to recalculate them.
        // This time, we try to get each cell to point to the lowest nearby level
        //Ignore the first row because that is empty.
        for (int i = 1; i < riverCellsList.Count; i++) {
            foreach (var riverLevel in riverCellsList[i]) {
                if (!cellWeatherAffects.TryGetValue(riverLevel, out var levelCell)) {
                    Log.Error("A cell that should have a value doesn't have a value");
                    continue;
                }

                foreach (var cellAround in GenAdjFast.AdjacentCells8Way(riverLevel).InRandomOrder()) {
                    if (!cellAround.IsValid)
                        continue;
                    if (!cellWeatherAffects.TryGetValue(cellAround, out var possiblePotentialCell)) {
                        continue;
                    }

                    if (levelCell.riverLevel >= possiblePotentialCell.riverLevel) {
                        levelCell.riverFocus = cellAround;
                    }
                    else if (isRiverTerrain(map.terrainGrid.TerrainAt(cellAround))) {
                        levelCell.riverFocus = cellAround;
                        break;
                    }

                    if (levelCell.riverLevel > possiblePotentialCell.riverLevel)
                        break;
                }
            }
        }

        if (!regenCellLists) {
            return;
        }

        SetUpTidesBanks();
        SetUpRiverLevel();
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

    /// <summary>
    /// Takes the current cell and moves moveCount cells into coastRotations direction
    /// </summary>
    /// <param name="cell">Current location</param>
    /// <param name="moveCount">cells to move</param>
    /// <returns>new cell location</returns>
    private IntVec3 AdjustForRotation(IntVec3 cell, int moveCount) {
        var newDirection = new IntVec3(cell.x, cell.y, cell.z);
        if (coastRotation == Rot4.North) {
            newDirection.z += moveCount + 1;
        }
        else if (coastRotation == Rot4.South) {
            newDirection.z -= moveCount + 1;
        }
        else if (coastRotation == Rot4.East) {
            newDirection.x += moveCount + 1;
        }
        else if (coastRotation == Rot4.West) {
            newDirection.x -= moveCount + 1;
        }

        return newDirection;
    }

    private void SetUpTidesBanks() {
        //set up ocean tides for the first time:
        if (doCoast) {
            //set up for low tide
            previousTideLevel = 0;
            tideLevel = 0;

            for (var i = 0; i < howManyTideSteps; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    if (beachTerrain == RimWorld.TerrainDefOf.Sand ||
                        beachTerrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                        map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_SandBeachWetSalt);
                        cell.currentTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
                        cell.setCurrentExtension();
                    }
                }
            }

            //bring to current tide levels
            FloodType level = GetTideLevel();
            int max = level switch {
                FloodType.Normal => halfTideSteps,
                FloodType.High => howManyTideSteps - 1,
                _ => 0
            };

            for (var i = 0; i < max; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    cell.increaseTide(oceanTerrain);
                }
            }

            previousTideLevel = Math.Max(0, max - 1);
            tideLevel = max;
        }
    }

    private void SetUpRiverLevel() {
        if (!EffectSettings.doFloods) return;

        for (int i = 0; i < HowManyRiverSteps; i++)
            DoRiverModify(force: true);
    }


    public Season season;
    private Quadrum quadrum;
    private Quadrum previousQuadrum = Quadrum.Undefined;


    private void UpdateBiomeSettings(bool force = false) {
        if (!force) {
            if (ticks % 30000 != 0) {
                // Check every 12 hours
                return;
            }
        }

        //previousQuadrum = quadrum;
        quadrum = GenDate.Quadrum(ticks, location.x);

        season = GenDate.Season(ticks, location);
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


    private void DoCellEnvironment(IntVec3 c) {
        if (c.GetEdifice(map) != null) {
            return;
        }

        if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
            return;
        }

        if (doUnpacking) {
            cell.Unpack();
        }

        currentTerrain = c.GetTerrain(map);
        if (cell.currentTerrain != currentTerrain) {
            cell.currentTerrain = currentTerrain;
            cell.setCurrentExtension();
        }

        roofed = doRoofChecks && map.roofGrid.Roofed(c);
        gettingWet = false;

        /*
        //check if the terrain has been floored
        // Should there be a blanket check on if a terrain is a floor? Thought for future me

        if (currentTerrain.designationCategory == DesignationCategoryDefOf.Floors) {
            //cell.baseTerrain = currentTerrain;
        }*/

        //spawn special things
        if (anyLavaTerrain) {
            LavaRockSpecials(c, currentTerrain);
        }

        if (EffectSettings.showRain) {
            if (!roofed) {
                //if it's raining in this cell:
                if (isRaining) {
                    if (floodThreat < 1090000) {
                        floodThreat += floodThreatIncrease;
                    }

                    gettingWet = true;
                    cell.setTerrainWet();
                }
                /*
                else if (currentSnowRate > .001f) {
                    gettingWet = true;
                    cell.setTerrainWet();
                }
                */
                else {
                    if (currentRainRate == 0) {
                        floodThreat--;
                    }

                    //DRY GROUND
                    cell.trySetTerrainDry();
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
                    cell.SetTerrainFrozen();
                }

                if (EffectSettings.showFrostGrid) {
                    if (cell.weatherExtension?.holdFrost == true) {
                        //handle frost based on snowing
                        if (!roofed && currentSnowRate > 0.001f) {
                            frostGridComponent.AddDepth(cell, currentSnowRate * -.01f);
                        }
                        else {
                            CreepFrostAt(cell, 0.138f);
                        }
                    }
                    else {
                        frostGridComponent.SetDepth(cell.locationIndex, 0);
                    }
                }
            }
            else if (cell.temperature >= 2) {
                cell.TrySetTerrainThawed();
                if (EffectSettings.showFrostGrid) {
                    if (cell.weatherExtension?.holdFrost == true) {
                        //TODO see if this works more performant
                        frostGridComponent.removeDepth(cell.locationIndex);
                        //frostGridComponent.AddDepth(cell, cell.temperature * -.025f);
                    }
                    else {
                        frostGridComponent.removeDepth(cell.locationIndex);
                    }
                }
            }
        }


        //HANDLE PLANT DAMAGES:

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

        if (EffectSettings.showRain) {
            cell.wetCheck(gettingWet);
        }

        if (EffectSettings.makePuddles) {
            if (cell.howWet == 3 && (outdoorTemp > 2 && MaxPuddles > totalPuddles &&
                                     currentTerrain != TerrainDefOf.TKKN_SandBeachWetSalt)) {
                FilthMaker.TryMakeFilth(c, map, ThingDefOf.TKKN_FilthPuddle);
                totalPuddles++;
            }
        }

        //cellWeatherAffects[c] = cell;
    }

    private void LavaRockSpecials(IntVec3 c, TerrainDef currentTerrain) {
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

    private void CreepFrostAt(cellData c, float baseAmount) {
        var depthToAdd = baseAmount * c.frostNoise;
        frostGridComponent.AddDepth(c, depthToAdd);
    }

    public FloodType GetRiverLevel() {
        var flood = FloodType.Normal;
        if (floodThreat > 1000000 || season == Season.Spring) {
            flood = FloodType.High;
        }
        else if (season == Season.Fall) {
            flood = FloodType.Low;
        }

        if (map.gameConditionManager.GetActiveCondition<GameCondition_Drought>() != null) {
            flood = FloodType.Low;
        }

        return flood;
    }

    private void DoRiverModify(bool force = false) {
        if (!force) {
            if (!EffectSettings.doFloods || !doRiverFlooding || ticks % RiverIntervalCheck != 0) {
                return;
            }
        }

        FloodType riverLevel = GetRiverLevel();

        bool increaseFlood;
        if (riverLevel == FloodType.High && floodLevel < HowManyRiverSteps)
            increaseFlood = true;
        else if (riverLevel == FloodType.Low && floodLevel > 0)
            increaseFlood = false;
        else if (riverLevel == FloodType.Normal && floodLevel < halfRiverSteps)
            increaseFlood = true;
        else if (riverLevel == FloodType.Normal && floodLevel > halfRiverSteps)
            increaseFlood = false;
        else
            return;

        if ((riverLevel == FloodType.High && floodLevel == HowManyRiverSteps) ||
            (riverLevel == FloodType.Low && floodLevel == 0) ||
            (riverLevel == FloodType.Normal && floodLevel == halfRiverSteps)) {
            return;
        }

        List<IntVec3> cellsToChange = riverCellsList[floodLevel];
        foreach (var c in cellsToChange.InRandomOrder()) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            if (increaseFlood)
                cell.increaseRiver(shallowRiverTerrain);
            else {
                cell.decreaseRiver();
            }
        }

        if (riverLevel == FloodType.High && floodLevel < MaxRiverSteps)
            floodLevel++;
        else if (riverLevel == FloodType.Low && floodLevel > 0)
            floodLevel--;
        else if (riverLevel == FloodType.Normal && floodLevel < halfRiverSteps)
            floodLevel++;
        else if (riverLevel == FloodType.Normal && floodLevel > halfRiverSteps)
            floodLevel--;
    }

    private FloodType GetTideLevel() {
        if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse)) {
            return FloodType.High;
        }

        return GenLocalDate.HourOfDay(map) switch {
            > 4 and < 8 => FloodType.Low,
            > 15 and < 20 => FloodType.High,
            _ => FloodType.Normal
        };
    }

    private void DoTides() {
        //notes to future me: use this.howManyTideSteps - 1, so we always have a little bit of wet sand, or else it looks stupid.
        if (!doCoast || !EffectSettings.doTides || ticks % TideIntervalCheck != 0) {
            return;
        }

        var tideType = GetTideLevel();

        if ((tideType == FloodType.Normal && tideLevel == halfTideSteps) ||
            (tideType == FloodType.High && tideLevel == maxTideSteps) ||
            (tideType == FloodType.Low && tideLevel == 0))
            return;

        if (tideType == FloodType.Normal && tideLevel == maxTideSteps) {
            previousTideLevel = tideLevel;
            tideLevel--;
            return;
        }

        List<IntVec3> cellsToChange = tideCellsList[tideLevel];
        foreach (var c in cellsToChange) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            var previousCell = AdjustForRotation(c, 0);
            if (!previousCell.InBounds(map)) {
                continue;
            }

            // Check if the previous tile is an ocean tile
            // If it isn't, check if the current tile is and remove it if so
            cell.currentTerrain = c.GetTerrain(map);

            if (!isOceanicTerrain(previousCell.GetTerrain(map))) {
                if (isOceanicTerrain(cell.currentTerrain)) {
                    cell.decreaseTide();
                }

                continue;
            }

            if (cell.currentTerrain.isFoundation) {
                continue;
            }

            switch (tideType) {
                case FloodType.High:
                    //cell.terrainOverride = TerrainType.Wet;
                    //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                    cell.increaseTide(oceanTerrain);
                    break;
                case FloodType.Low:
                    //cell.terrainOverride = TerrainType.Dry;
                    //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                    cell.decreaseTide();
                    break;
                case FloodType.Normal:
                    if (tideLevel < halfTideSteps) {
                        //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                        cell.increaseTide(oceanTerrain);
                    }
                    else if (tideLevel > halfTideSteps) {
                        //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                        cell.decreaseTide();
                    }
                    else if (previousTideLevel < tideLevel) {
                        //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                        cell.increaseTide(oceanTerrain);
                    }
                    else if (previousTideLevel > tideLevel) {
                        //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                    }
                    else {
                        cell.changeTide(oceanTerrain);
                    }

                    break;
            }
        }

        switch (tideType) {
            case FloodType.High: {
                if (tideLevel < maxTideSteps) {
                    previousTideLevel = tideLevel;
                    tideLevel++;
                }

                break;
            }
            case FloodType.Low: {
                if (tideLevel > 0) {
                    previousTideLevel = tideLevel;
                    tideLevel--;
                }

                break;
            }
            case FloodType.Normal when tideLevel > halfTideSteps:
                previousTideLevel = tideLevel;
                tideLevel--;
                break;
            case FloodType.Normal: {
                if (tideLevel < halfTideSteps) {
                    previousTideLevel = tideLevel;
                    tideLevel++;
                }

                break;
            }
        }
    }

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

    private bool isOceanicTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterOceanShallow ||
               terrain == TerrainDefOf.NPS_WaterOceanTide ||
               terrain == oceanTerrain;
    }

    private bool isRiverTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterMovingShallow ||
               terrain == TerrainDefOf.NPS_WaterRiverFlood ||
               terrain == RimWorld.TerrainDefOf.WaterMovingChestDeep;
    }
}