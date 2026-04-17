using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class EffectSettings : ModSettings
{
    public static bool leaveLoot = true;
    public static bool forbidLoot = true;

    public static bool spawnLavaOnlyInBiome = true;
    public static bool allowLavaEruption = true;
    public static bool allowPlantEffects = false;

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
    public static bool showDevReadout;

    public static bool doFloods = true;
    public static int maxCellsPerTick = 50;
    public static bool useMapTemperature = false;

    public static bool seasonalDiseases = true;
    public static bool seasonalIncidents = true;
    public static bool seasonalWeather = true;

    public static bool changeGrassGraphics = true;

    public static bool terrainAffectTemperature = false;
    public static ModContentPack modContent;


    public static void DoWindowContents(Rect inRect) {
        Listing_Standard list = new Listing_Standard(GameFont.Small);

        list.Begin(inRect.LeftPart(0.49f));

        //Performance Settings
        list.CheckboxLabeled(
            "NPS_doWeather_title".Translate(),
            ref doWeather,
            "NPS_doWeather_text".Translate());
        if (doWeather) {
            maxCellsPerTick = (int)list.SliderLabeled(
                "NPS_weatherCellUpdateSpeed_title".Translate(maxCellsPerTick),
                maxCellsPerTick, 10, 100, 0.5f, "NPS_weatherCellUpdateSpeed_text".Translate());

            list.CheckboxLabeled(
                "NPS_OnlyTargetPlayerHome_title".Translate(),
                ref onlyPlayerHome,
                "NPS_OnlyTargetPlayerHome_text".Translate()
            );


            list.CheckboxLabeled(
                "NPS_useMapTemperature_title".Translate(),
                ref useMapTemperature,
                "NPS_useMapTemperature_text".Translate());

            if (!ModsConfig.OdysseyActive) {
                list.CheckboxLabeled(
                    "NPS_doIce_title".Translate(),
                    ref doIce,
                    "NPS_doIce_text".Translate());
            }

            list.CheckboxLabeled(
                "NPS_ShowFrostGrid_title".Translate(),
                ref showFrostGrid,
                "NPS_FrostGrid_text".Translate());

            list.CheckboxLabeled("NPS_showRainEffects_title".Translate(),
                ref showRainEffects,
                "NPS_showRainEffects_text");
            if (showRainEffects) {
                if (list.RadioButton("NPS_showWetTerrain_title".Translate(), rainOptionSelector == 1,
                        tooltip: "NPS_showWetTerrain_text".Translate())) {
                    rainOptionSelector = 1;
                    showWetTerrain = true;
                    showFloodTerrain = false;
                    showRainGrid = false;
                }

                if (list.RadioButton("NPS_showFloodTerrain_title".Translate(), rainOptionSelector == 2,
                        tooltip: "NPS_showFloodTerrain_text".Translate())) {
                    rainOptionSelector = 2;
                    showWetTerrain = false;
                    showFloodTerrain = true;
                    showRainGrid = false;
                }

                if (list.RadioButton("NPS_showRainGrid_title".Translate(), rainOptionSelector == 3,
                        tooltip: "NPS_showrainGrid_text".Translate())) {
                    rainOptionSelector = 3;
                    showWetTerrain = false;
                    showFloodTerrain = false;
                    showRainGrid = true;
                }

                if (list.RadioButton("NPS_showFloodAndRainGrid_title".Translate(), rainOptionSelector == 4,
                        tooltip: "NPS_showFloodAndRainGrid_text".Translate())) {
                    rainOptionSelector = 4;
                    showWetTerrain = false;
                    showFloodTerrain = true;
                    showRainGrid = true;
                }

                if (rainOptionSelector >= 2) {
                    list.CheckboxLabeled("NPS_rainIncreaseFertility_title".Translate(),
                        ref rainIncreaseFertility,
                        "NPS_rainIncreaseFertility_text");
                }
            }

            list.CheckboxLabeled(
                "NPS_makePuddles_title".Translate(),
                ref makePuddles,
                "NPS_makePuddles_text".Translate());


            if (!ModsConfig.OdysseyActive) {
                list.CheckboxLabeled(
                    "NPS_doFloods_title".Translate(),
                    ref doFloods,
                    "NPS_doFloods_text".Translate());
            }

            list.CheckboxLabeled(
                "NPS_doTides_title".Translate(),
                ref doTides,
                "NPS_doTides_text".Translate());

            list.CheckboxLabeled(
                "NPS_leaveLoot_title".Translate(),
                ref leaveLoot,
                "NPS_leaveLoot_text".Translate());

            if (leaveLoot) {
                list.CheckboxLabeled(
                    "NPS_forbidLoot_title".Translate(),
                    ref forbidLoot,
                    "NPS_forbidLoot_text".Translate());
            }
        }

/*
        list.Gap();

        list.CheckboxLabeled(
            "NPS_allowLavaEruption_title".Translate(),
            ref allowLavaEruption,
            "NPS_allowLavaEruption_text".Translate());
        list.CheckboxLabeled(
            "NPS_spawnLavaOnlyInBiome_title".Translate(),
            ref spawnLavaOnlyInBiome,
            "NPS_spawnLavaOnlyInBiome_text".Translate());
*/
        list.Gap();

        Text.Font = GameFont.Medium;
        list.Label("NPS_RequiresRestart".Translate());
        Text.Font = GameFont.Small;

        list.CheckboxLabeled(
            "NPS_doAmbientTemperature_title".Translate(),
            ref terrainAffectTemperature,
            "NPS_doAmbientTemperature_text".Translate());

        list.CheckboxLabeled(
            "NPS_allowPlantEffects_title".Translate(),
            ref allowPlantEffects,
            "NPS_allowPlantEffects_text".Translate());

        if (allowPawnEffects && !ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "NPS_allowPawnsSwim_title".Translate(),
                ref allowPawnsSwim,
                "NPS_allowPawnsToSwim_text".Translate());
        }

        list.End();

        list.Begin(inRect.RightPart(0.49f));


        list.CheckboxLabeled(
            "NPS_seasonalDiseases_title".Translate(),
            ref seasonalDiseases,
            "NPS_seasonalDiseases_text".Translate());
        list.CheckboxLabeled(
            "NPS_seasonalIncidents_title".Translate(),
            ref seasonalIncidents,
            "NPS_seasonalIncidents_text".Translate());
        if (!HarmonyWeatherEffects.SeasonalWeatherModActive) {
            list.CheckboxLabeled(
                "NPS_seasonalWeather_title".Translate(),
                ref seasonalWeather,
                "NPS_seasonalWeather_text".Translate());
        }

        list.Gap();

        list.CheckboxLabeled(
            "NPS_allowPawnEffects_title".Translate(),
            ref allowPawnEffects,
            "NPS_allowPawnEffects_text".Translate());

        if (allowPawnEffects) {
            list.CheckboxLabeled(
                "NPS_pawnEffectsOnlyColonists_title".Translate(),
                ref pawnEffectsOnlyColonists,
                "NPS_pawnEffectsOnlyColonists_text".Translate());
            list.Label("NPS_springEffects_title".Translate(), tooltip: "NPS_springEffects_text".Translate());
            if (!HarmonyWeatherEffects.DesirePathsActive) {
                list.CheckboxLabeled(
                    "NPS_doDirtPath_title".Translate(),
                    ref doDirtPath,
                    "NPS_doDirtPath_text".Translate());

                if (doDirtPath) {
                    list.CheckboxLabeled(
                        "NPS_doWalkThroughSnow_title".Translate(),
                        ref doWalkThroughSnow,
                        "NPS_doWalkThroughSnow_text".Translate());
                }
            }

            list.CheckboxLabeled(
                "NPS_DoColdBreath_title".Translate(),
                ref doColdBreath,
                "NPS_DoColdBreath_text".Translate());

            list.CheckboxLabeled(
                "NPS_allowPawnsToGetWet_title".Translate(),
                ref allowPawnsToGetWet,
                "NPS_allowPawnsToGetWet_text".Translate());

            list.CheckboxLabeled(
                "NPS_allowPawnsToDrown_title".Translate(),
                ref allowPawnsDrowning,
                "NPS_allowPawnsToDrown_text".Translate());
        }

        list.Gap(30f);

        list.CheckboxLabeled(
            "NPS_showDevReadout_title".Translate(),
            ref showDevReadout,
            "NPS_showDevReadout_text".Translate());

        list.Gap(30f);
        if (Current.Game?.CurrentMap != null) {
            list.Label("NPS_reapplyMap_text".Translate());
            if (list.ButtonText("NPS_reapplyMap".Translate())) {
                Map currentMap = Current.Game.CurrentMap;
                var watcherComponent = currentMap.GetComponent<Watcher>();
                regenCells = true;
                watcherComponent.RemoveEffects();
                watcherComponent.RebuildCellLists();
                Messages.Message("NPS_RebuildingFinished".Translate(), MessageTypeDefOf.NeutralEvent, false);
                regenCells = false;
            }

            list.Gap(30f);

            list.Label("NPS_removeEffects_text".Translate());
            if (list.ButtonText(label: "NPS_removeEffects".Translate())) {
                Map currentMap = Current.Game.CurrentMap;
                var watcherComponent = currentMap.GetComponent<Watcher>();
                regenCells = true;
                watcherComponent.RemoveEffects();
                Messages.Message("NPS_RemovalFinished".Translate(), MessageTypeDefOf.NeutralEvent, false);
                regenCells = false;
            }
        }

        list.End();
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref doWeather, "doWeather", true);
        Scribe_Values.Look(ref maxCellsPerTick, "cellsPerTick", 50);
        Scribe_Values.Look(ref onlyPlayerHome, "onlyPlayerHome", true);
        Scribe_Values.Look(ref doDirtPath, "doDirtPath", true);
        Scribe_Values.Look(ref allowPlantEffects, "allowPlantEffects", false);
        Scribe_Values.Look(ref rainOptionSelector, "rainOptionSelector", 1);
        Scribe_Values.Look(ref showRainEffects, "showRainEffects", true);
        Scribe_Values.Look(ref showWetTerrain, "showWetTerrain", true);
        Scribe_Values.Look(ref showFloodTerrain, "showFloodTerrain", false);
        Scribe_Values.Look(ref showRainGrid, "showRainGrid", false);
        Scribe_Values.Look(ref rainIncreaseFertility, "rainIncreaseFertility", false);

        Scribe_Values.Look(ref makePuddles, "makePuddles", true);
        Scribe_Values.Look(ref doTides, "doTides", true);
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