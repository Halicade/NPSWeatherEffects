using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public class EffectSettings : ModSettings
{
    public static int modsPatchingTerrain = 0;
    public static int modsPatchingTemperature = 0;
    public static bool leaveLoot = true;
    public static bool forbidLoot = true;

    public static bool spawnLavaOnlyInBiome = true;
    public static bool allowLavaEruption = true;
    public static bool allowPlantEffects = true;
    public static List<PlantEffectHolder> plantsEffects = [];
    // This field isn't saved, I just put it with the plantsEffects cause it matches 
    public static int ActivePlants;

    public static bool doIce = true;
    public static bool doColdBreath = true;
    public static bool showFrostGrid = true;
    public static bool doWalkThroughSnow = true;

    public static bool allowPawnEffects = true;
    public static bool pawnEffectsOnlyColonists = true;
    public static bool allowPawnsToGetWet = true;
    public static bool allowPawnsDrowning = true;
    public static bool allowPawnsSwim = true;

    public static int rainOptionSelector = 1;
    public static bool showRainEffects = true;
    public static bool showWetTerrain = true;
    public static bool showFloodTerrain = false;
    public static bool showRainGrid = false;
    public static bool rainIncreaseFertility = false;
    public static bool makePuddles = true;
    public static bool doWeather = true;
    public static bool onlyPlayerHome = true;
    public static bool doDirtPath = true;
    public static bool regenCells;
    public static bool doTides = true;
    public static bool doWetSand = true;
    public static bool showDevReadout;

    public static bool doFloods = true;
    public static int maxCellsPerTick = 75;
    public static bool useMapTemperature = false;

    public static bool seasonalDiseases = true;
    public static bool seasonalIncidents = true;
    public static bool seasonalWeather = true;

    public static bool changeGrassGraphics = true;

    public static bool terrainAffectTemperature = false;


    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref doWeather, "doWeather", true);
        Scribe_Values.Look(ref maxCellsPerTick, "cellsPerTick", 75);
        Scribe_Values.Look(ref onlyPlayerHome, "onlyPlayerHome", true);
        Scribe_Values.Look(ref doDirtPath, "doDirtPath", true);
        Scribe_Values.Look(ref allowPlantEffects, "allowPlantEffects", false);

        Scribe_Collections.Look(ref plantsEffects, "plantsEffects", LookMode.Deep);

        Scribe_Values.Look(ref rainOptionSelector, "rainOptionSelector", 1);
        Scribe_Values.Look(ref showRainEffects, "showRainEffects", true);
        Scribe_Values.Look(ref showWetTerrain, "showWetTerrain", true);
        Scribe_Values.Look(ref showFloodTerrain, "showFloodTerrain", false);
        Scribe_Values.Look(ref showRainGrid, "showRainGrid", false);
        Scribe_Values.Look(ref rainIncreaseFertility, "rainIncreaseFertility", false);

        Scribe_Values.Look(ref makePuddles, "makePuddles", true);
        Scribe_Values.Look(ref doTides, "doTides", true);
        Scribe_Values.Look(ref doWetSand, "doWetSand", true);
        Scribe_Values.Look(ref doFloods, "doFloods", true);
        Scribe_Values.Look(ref leaveLoot, "leaveStuff", true);
        Scribe_Values.Look(ref forbidLoot, "forbidLoot", true);

        Scribe_Values.Look(ref doIce, "doIce", true);
        Scribe_Values.Look(ref doColdBreath, "doColdBreath", true);
        Scribe_Values.Look(ref showFrostGrid, "showFrostGrid", true);
        Scribe_Values.Look(ref useMapTemperature, "useMapTemperature", false);
        Scribe_Values.Look(ref allowPawnEffects, "allowPawnEffects", true);
        Scribe_Values.Look(ref pawnEffectsOnlyColonists, "pawnEffectsOnlyColonists", true);
        Scribe_Values.Look(ref seasonalDiseases, "seasonalDiseases", true);
        Scribe_Values.Look(ref seasonalIncidents, "seasonalIncidents", true);
        Scribe_Values.Look(ref seasonalWeather, "seasonalWeather", true);

        Scribe_Values.Look(ref allowPawnsToGetWet, "allowPawnsToGetWet", true);
        Scribe_Values.Look(ref allowPawnsDrowning, "allowPawnsDrowning", true);
        Scribe_Values.Look(ref allowPawnsSwim, "allowPawnsSwim", true);
        Scribe_Values.Look(ref showDevReadout, "showDevReadout", false);
        Scribe_Values.Look(ref spawnLavaOnlyInBiome, "spawnLavaOnlyInBiome", false);
        Scribe_Values.Look(ref allowLavaEruption, "allowLavaEruption", true);
        Scribe_Values.Look(ref regenCells, "regenCells", false);
        Scribe_Values.Look(ref terrainAffectTemperature, "terrainAffectTemperature", false);
    }
}