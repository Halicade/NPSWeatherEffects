using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class WeatherEffectsController : Mod
{
    public WeatherEffectsController(ModContentPack content)
        : base(content) {
        GetSettings<EffectSettings>();
        EffectSettings.modContent = content;
    }


    public override void DoSettingsWindowContents(Rect inRect) {
        DoWindowContents(inRect);
    }

    public override string SettingsCategory() {
        return "NPS_WeatherEffects".Translate();
    }

    public override void WriteSettings() {
        if (!EffectSettings.allowPawnEffects) {
            EffectSettings.pawnEffectsOnlyColonists = false;
            EffectSettings.allowPawnsToGetWet = false;
            EffectSettings.allowPawnsDrowning = false;
            EffectSettings.allowPawnsSwim = false;
            EffectSettings.doDirtPath = false;
            EffectSettings.doWalkThroughSnow = false;
        }

        base.WriteSettings();

        if (Current.ProgramState == ProgramState.Playing) {
            foreach (var map in Find.Maps) {
                var watcher = map.GetComponent<Watcher>();
                watcher.validPawns.Clear();
            }
        }
    }


    private Vector2 scrollPosition = Vector2.zero;

    private void DoWindowContents(Rect inRect) {
        Listing_Standard list = new Listing_Standard();
        Rect outRect = new(inRect.x, inRect.y, inRect.width, inRect.height - 20);
        Rect viewRect = new(0f, 0f,
            width: inRect.width - 30f,
            height: 800
        );
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        list.Begin(viewRect.LeftPart(0.49f));

        Text.Font = GameFont.Medium;
        list.Label("NPS_weatherEffectsHeader".Translate());
        Text.Font = GameFont.Small;
        list.CheckboxLabeled(
            "NPS_doWeather_title".Translate(),
            ref EffectSettings.doWeather,
            "NPS_doWeather_text".Translate());
        if (EffectSettings.doWeather) {
            EffectSettings.maxCellsPerTick = (int)list.SliderLabeled(
                "NPS_weatherCellUpdateSpeed_title".Translate(EffectSettings.maxCellsPerTick),
                EffectSettings.maxCellsPerTick, 5, 100, 
                0.5f, 
                "NPS_weatherCellUpdateSpeed_text".Translate());

            list.CheckboxLabeled(
                "NPS_OnlyTargetPlayerHome_title".Translate(),
                ref EffectSettings.onlyPlayerHome,
                "NPS_OnlyTargetPlayerHome_text".Translate()
            );

            list.GapLine();

            Text.Font = GameFont.Medium;
            list.Label("NPS_coldEffects".Translate());
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(
                "NPS_useMapTemperature_title".Translate(),
                ref EffectSettings.useMapTemperature,
                "NPS_useMapTemperature_text".Translate());
            if (!EffectSettings.useMapTemperature && EffectSettings.modsPatchingTemperature > 0) {
                Text.Font = GameFont.Tiny;
                list.Label(
                    "NPS_numModsPatching".Translate(EffectSettings.modsPatchingTemperature).Colorize(Color.yellow));
                Text.Font = GameFont.Small;
            }


            if (!ModsConfig.OdysseyActive) {
                list.CheckboxLabeled(
                    "NPS_doIce_title".Translate(),
                    ref EffectSettings.doIce,
                    "NPS_doIce_text".Translate());

                if (EffectSettings.doIce) {
                    Text.Font = GameFont.Tiny;
                    list.Label("NPS_numModsPatching".Translate(EffectSettings.modsPatchingTerrain)
                        .Colorize(Color.yellow));
                    Text.Font = GameFont.Small;
                }
            }

            list.CheckboxLabeled(
                "NPS_ShowFrostGrid_title".Translate(),
                ref EffectSettings.showFrostGrid,
                "NPS_FrostGrid_text".Translate());

            list.GapLine();
            Text.Font = GameFont.Medium;
            list.Label("NPS_rainEffects".Translate());
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(
                "NPS_showRainEffects_title".Translate(),
                ref EffectSettings.showRainEffects,
                "NPS_showRainEffects_text".Translate());
            
            if (EffectSettings.showRainEffects) {
                if (list.RadioButton("NPS_noTerrainEffects_title".Translate(),
                        EffectSettings.rainOptionSelector == 0,
                        tooltip: "NPS_noTerrainEffects_text".Translate())) {
                    EffectSettings.rainOptionSelector = 0;
                    EffectSettings.showWetTerrain = false;
                    EffectSettings.showFloodTerrain = false;
                }

                if (list.RadioButton("NPS_showWetTerrain_title".Translate(),
                        EffectSettings.rainOptionSelector == 1,
                        tooltip: "NPS_showWetTerrain_text".Translate())) {
                    EffectSettings.rainOptionSelector = 1;
                    EffectSettings.showWetTerrain = true;
                    EffectSettings.showFloodTerrain = false;
                }

                if (list.RadioButton("NPS_showFloodTerrain_title".Translate(),
                        EffectSettings.rainOptionSelector == 2,
                        tooltip: "NPS_showFloodTerrain_text".Translate())) {
                    EffectSettings.rainOptionSelector = 2;
                    EffectSettings.showWetTerrain = false;
                    EffectSettings.showFloodTerrain = true;
                }

                if (list.RadioButton("NPS_showRainAndFloodTerrain_title".Translate(),
                        EffectSettings.rainOptionSelector == 3,
                        tooltip: "NPS_showRainAndFloodTerrain_text".Translate())) {
                    EffectSettings.rainOptionSelector = 3;
                    EffectSettings.showWetTerrain = true;
                    EffectSettings.showFloodTerrain = true;
                }

                if (EffectSettings.rainOptionSelector > 0 && EffectSettings.modsPatchingTerrain > 0) {
                    Text.Font = GameFont.Tiny;
                    list.Label("NPS_numModsPatching".Translate(EffectSettings.modsPatchingTerrain)
                        .Colorize(Color.yellow));
                    Text.Font = GameFont.Small;
                }

                list.CheckboxLabeled("NPS_showRainGrid_title".Translate(),
                    ref EffectSettings.showRainGrid,
                    "NPS_showrainGrid_text".Translate());

                list.CheckboxLabeled("NPS_rainIncreaseFertility_title".Translate(),
                    ref EffectSettings.rainIncreaseFertility,
                    "NPS_rainIncreaseFertility_text".Translate());
                
                if (EffectSettings.showWetTerrain && EffectSettings.rainIncreaseFertility) {
                    Text.Font = GameFont.Tiny;
                    list.Label("NPS_notRecommendedWetFertility".Translate().Colorize(Color.yellow));
                    Text.Font = GameFont.Small;
                }

                list.CheckboxLabeled(
                    "NPS_makePuddles_title".Translate(),
                    ref EffectSettings.makePuddles,
                    "NPS_makePuddles_text".Translate());

                list.GapLine();
            }

            Text.Font = GameFont.Medium;
            list.Label("NPS_waterEffects".Translate());
            Text.Font = GameFont.Small;

            if (!ModsConfig.OdysseyActive) {
                list.CheckboxLabeled(
                    "NPS_doFloods_title".Translate(),
                    ref EffectSettings.doFloods,
                    "NPS_doFloods_text".Translate());
            }

            list.CheckboxLabeled(
                "NPS_doTides_title".Translate(),
                ref EffectSettings.doTides,
                "NPS_doTides_text".Translate());

            list.CheckboxLabeled(
                "NPS_doWetSand_title".Translate(),
                ref EffectSettings.doWetSand,
                "NPS_doWetSand_text".Translate());
            
            list.CheckboxLabeled(
                "NPS_leaveLoot_title".Translate(),
                ref EffectSettings.leaveLoot,
                "NPS_leaveLoot_text".Translate());

            if (EffectSettings.leaveLoot) {
                list.CheckboxLabeled(
                    "NPS_forbidLoot_title".Translate(),
                    ref EffectSettings.forbidLoot,
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
            ref EffectSettings.terrainAffectTemperature,
            "NPS_doAmbientTemperature_text".Translate());

        list.CheckboxLabeled(
            "NPS_allowPlantEffects_title".Translate(),
            ref EffectSettings.allowPlantEffects,
            "NPS_allowPlantEffects_text".Translate());

        if (EffectSettings.allowPawnEffects && !ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "NPS_allowPawnsSwim_title".Translate(),
                ref EffectSettings.allowPawnsSwim,
                "NPS_allowPawnsToSwim_text".Translate());
        }

        list.End();

        list.Begin(viewRect.RightPart(0.49f));

        Text.Font = GameFont.Medium;
        list.Label("NPS_seasonalEffects".Translate());
        Text.Font = GameFont.Small;
        list.CheckboxLabeled(
            "NPS_seasonalDiseases_title".Translate(),
            ref EffectSettings.seasonalDiseases,
            "NPS_seasonalDiseases_text".Translate());
        
        list.CheckboxLabeled(
            "NPS_seasonalIncidents_title".Translate(),
            ref EffectSettings.seasonalIncidents,
            "NPS_seasonalIncidents_text".Translate());
        
        if (!HarmonyWeatherEffects.SeasonalWeatherModActive) {
            list.CheckboxLabeled(
                "NPS_seasonalWeather_title".Translate(),
                ref EffectSettings.seasonalWeather,
                "NPS_seasonalWeather_text".Translate());
        }

        list.GapLine();
        Text.Font = GameFont.Medium;
        list.Label("NPS_pawnEffects".Translate());
        Text.Font = GameFont.Small;
        
        list.CheckboxLabeled(
            "NPS_allowPawnEffects_title".Translate(),
            ref EffectSettings.allowPawnEffects,
            "NPS_allowPawnEffects_text".Translate());

        if (EffectSettings.allowPawnEffects) {
            list.CheckboxLabeled(
                "NPS_pawnEffectsOnlyColonists_title".Translate(),
                ref EffectSettings.pawnEffectsOnlyColonists,
                "NPS_pawnEffectsOnlyColonists_text".Translate());
            
            list.Label("NPS_springEffects_title".Translate(),
                tooltip: "NPS_springEffects_text".Translate());
            
            if (!HarmonyWeatherEffects.DesirePathsActive) {
                list.CheckboxLabeled(
                    "NPS_doDirtPath_title".Translate(),
                    ref EffectSettings.doDirtPath,
                    "NPS_doDirtPath_text".Translate());

                if (EffectSettings.doDirtPath) {
                    list.CheckboxLabeled(
                        "NPS_doWalkThroughSnow_title".Translate(),
                        ref EffectSettings.doWalkThroughSnow,
                        "NPS_doWalkThroughSnow_text".Translate());
                }
            }

            list.CheckboxLabeled(
                "NPS_DoColdBreath_title".Translate(),
                ref EffectSettings.doColdBreath,
                "NPS_DoColdBreath_text".Translate());

            list.CheckboxLabeled(
                "NPS_allowPawnsToGetWet_title".Translate(),
                ref EffectSettings.allowPawnsToGetWet,
                "NPS_allowPawnsToGetWet_text".Translate());

            list.CheckboxLabeled(
                "NPS_allowPawnsToDrown_title".Translate(),
                ref EffectSettings.allowPawnsDrowning,
                "NPS_allowPawnsToDrown_text".Translate());
        }

        list.GapLine(30f);

        Text.Font = GameFont.Medium;
        list.Label("NPS_devTools".Translate());
        Text.Font = GameFont.Small;

        list.CheckboxLabeled(
            "NPS_showDevReadout_title".Translate(),
            ref EffectSettings.showDevReadout,
            "NPS_showDevReadout_text".Translate());

        list.Gap(30f);
        if (Current.Game?.CurrentMap != null) {
            list.Label("NPS_reapplyMap_text".Translate());
            if (list.ButtonText("NPS_reapplyMap".Translate())) {
                Map currentMap = Current.Game.CurrentMap;
                var watcherComponent = currentMap.GetComponent<Watcher>();
                EffectSettings.regenCells = true;
                watcherComponent.RemoveEffects();
                watcherComponent.RebuildCellLists();
                Messages.Message("NPS_RebuildingFinished".Translate(), MessageTypeDefOf.NeutralEvent, false);
                EffectSettings.regenCells = false;
            }

            list.Gap(30f);

            list.Label("NPS_removeEffects_text".Translate());
            if (list.ButtonText(label: "NPS_removeEffects".Translate())) {
                Map currentMap = Current.Game.CurrentMap;
                var watcherComponent = currentMap.GetComponent<Watcher>();
                EffectSettings.regenCells = true;
                watcherComponent.RemoveEffects();
                Messages.Message("NPS_RemovalFinished".Translate(), MessageTypeDefOf.NeutralEvent, false);
                EffectSettings.regenCells = false;
            }
        }

        list.End();
        Widgets.EndScrollView();
    }
}