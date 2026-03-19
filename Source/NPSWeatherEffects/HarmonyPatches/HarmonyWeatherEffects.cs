using System;
using System.Reflection;
using HarmonyLib;
using NPSWeatherEffects.HarmonyPatches;
using RimWorld;
using TKKN_NPS;
using Verse;
using Verse.AI;

namespace NPSWeather;

[StaticConstructorOnStartup]
public class HarmonyWeatherEffects
{
    
    // https://steamcommunity.com/sharedfiles/filedetails/?id=2079784964
    public static readonly bool RimBrellasActive;
    
    // https://steamcommunity.com/sharedfiles/filedetails/?id=3555461401
    public static readonly bool DesirePathsActive;
    
    // https://steamcommunity.com/sharedfiles/filedetails/?id=3403972335
    public static readonly bool StorytellerJianghuActive;
    
    public static readonly bool NPSBiomesActive;

    public delegate bool HasUmbrellaDelegate(Pawn pawn);

    public static HasUmbrellaDelegate HasUmbrella;
    
    

    static HarmonyWeatherEffects() {
        TerrainTagUtil.IntializeTerrainTags();
        ThingUtil.InitializeThingUtil();
        PlantReactionUtil.InitializePlantGraphics();
        BiomeUtil.InitializeDefaults();

        DesirePathsActive = ModLister.GetActiveModWithIdentifier("mlie.desirepaths") != null;
        RimBrellasActive = ModLister.GetActiveModWithIdentifier("battlemage64.Rimbrellas", true) != null;
        NPSBiomesActive=ModLister.GetActiveModWithIdentifier("Hali.NPSBiomes", true) != null;
        StorytellerJianghuActive = ModLister.GetActiveModWithIdentifier("zal.jianghujin", true) != null;

        if (ModsConfig.OdysseyActive) {
            EffectSettings.doIce = false;
            EffectSettings.doFloods = false;
            EffectSettings.allowPawnsSwim = false;
        }

        if (DesirePathsActive) {
            EffectSettings.doDirtPath = false;
        }


        var harmony = new Harmony("Hali.NPS_WeatherEffects");
        
        harmony.Patch(AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
            prefix: new HarmonyMethod(typeof(UIRootEntry_Init), nameof(UIRootEntry_Init.Prefix)));

        harmony.Patch(AccessTools.Method(typeof(BiomeDef), nameof(BiomeDef.CommonalityOfDisease)),
            prefix: new HarmonyMethod(typeof(BiomeDef_CommonalityOfDisease),
                nameof(BiomeDef_CommonalityOfDisease.Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn),
            [
                typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool)
            ]),
            postfix: new HarmonyMethod(typeof(GenSpawn_Spawn), nameof(GenSpawn_Spawn.Postfix)));

        harmony.Patch(AccessTools.Method(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI)),
            postfix: new HarmonyMethod(typeof(MouseoverReadout_MouseoverReadoutOnGUI),
                nameof(MouseoverReadout_MouseoverReadoutOnGUI.Postfix)));

        /*
        //Removed these patches cause this mod shouldn't be the one to do it
        harmony.Patch(AccessTools.Method(typeof(CellFinder), nameof(CellFinder.TryRandomClosewalkCellNear)),
            prefix: new HarmonyMethod(typeof(CellFinder_TryRandomClosewalkCellNear),
                nameof(CellFinder_TryRandomClosewalkCellNear.Prefix)));
        harmony.Patch(AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
            prefix: new HarmonyMethod(typeof(Pawn_PathFollower_StartPath),
                nameof(Pawn_PathFollower_StartPath.Prefix)));

        harmony.Patch(AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach), [
                typeof(IntVec3), typeof(LocalTargetInfo),
                typeof(PathEndMode),
                typeof(TraverseParms)
            ]),
            postfix: new HarmonyMethod(typeof(Reachability_CanReach),
                nameof(Reachability_CanReach.Postfix)));
        */

        harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
            postfix: new HarmonyMethod(typeof(Pawn_SpawnSetup),
                nameof(Pawn_SpawnSetup.Postfix)));
/*
        harmony.Patch(AccessTools.Method(typeof(WeatherDecider), "CurrentWeatherCommonality"),
            prefix: new HarmonyMethod(typeof(WeatherDecider_CurrentWeatherCommonality),
                nameof(WeatherDecider_CurrentWeatherCommonality.Prefix)));
  */      
        harmony.Patch(
            AccessTools.Method(typeof(JobGiver_SeekSafeTemperature), "TryGiveJob"),
            postfix: new HarmonyMethod(typeof(JobGiver_SeekSafeTemperature_TryGiveJob),
                nameof(JobGiver_SeekSafeTemperature_TryGiveJob.Postfix)));

        if (EffectSettings.allowPawnsSwim && !ModsConfig.OdysseyActive) {
            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderNodeWorker_Body), nameof(PawnRenderNodeWorker_Body.CanDrawNow)),
                postfix: new HarmonyMethod(typeof(PawnRenderNodeWorker_Body_CanDrawNow),
                    nameof(PawnRenderNodeWorker_Body_CanDrawNow.Postfix)));
            
            harmony.Patch(
                AccessTools.Method(typeof(Graphic_Shadow), nameof(Graphic_Shadow.DrawWorker)),
                prefix: new HarmonyMethod(typeof(Graphic_Shadow_DrawWorker),
                    nameof(Graphic_Shadow_DrawWorker.Prefix)));
        }

        if (EffectSettings.allowPlantEffects) {
            harmony.Patch(AccessTools.PropertyGetter(typeof(Plant), nameof(Plant.Graphic)),
                postfix: new HarmonyMethod(typeof(Plant_Graphic),
                    nameof(Plant_Graphic.Postfix)));
        }

        if (EffectSettings.terrainAffectTemperature) {
            harmony.Patch(AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.AmbientTemperature)),
                postfix: new HarmonyMethod(typeof(Thing_AmbientTemperature),
                    nameof(Thing_AmbientTemperature.Postfix)));
        }

        if (StorytellerJianghuActive) {
            MethodInfo jianghuJinMethodInfo =
                AccessTools.Method(AccessTools.TypeByName("YaomaJin.IncidentWorker_Terraform_Jin"), "TryExecuteWorker");
            if (jianghuJinMethodInfo != null) {
                harmony.Patch(jianghuJinMethodInfo,
                    postfix: new HarmonyMethod(typeof(IncidentWorker_Terraform_Jin),
                        nameof(IncidentWorker_Terraform_Jin.Postfix)));
            }
        }

        if (RimBrellasActive) {
            HasUmbrella = AccessTools.MethodDelegate<HasUmbrellaDelegate>(
                AccessTools.Method("Umbrellas.UmbrellaDefMethods:HasUmbrella"));

            if (HasUmbrella == null) {
                Log.Warning(
                    "[Natures Pretty Sweet]: Rimbrella loaded but could not find the correct method to check for umbrellas");
                RimBrellasActive = false;
            }
        }
    }
}