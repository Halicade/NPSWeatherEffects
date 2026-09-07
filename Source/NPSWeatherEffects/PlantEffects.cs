using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

[StaticConstructorOnStartup]
public class PlantEffects : Plant
{
    private static readonly Graphic GraphicSowing = GraphicDatabase.Get<Graphic_Single>("Things/Plant/Plant_Sowing",
        ShaderDatabase.Cutout, Vector2.one, Color.white);

    private bool hasAnyGraphic;

    private PlantEffectHolder effectsHolder;

    public override void SpawnSetup(Map map, bool respawningAfterLoad) {
        base.SpawnSetup(map, respawningAfterLoad);
        if (Destroyed) {
            return;
        }

        cachedMap = map;
        watcherComponent = cachedMap.GetComponent<Watcher>();
        frostComponent = cachedMap.GetComponent<FrostGrid>();
        PlantReactionUtil.ApplyGraphicFrozen.TryGetValue(def, out effectsHolder);
        hasAnyGraphic = effectsHolder?.weatherExtension != null;
        if (respawningAfterLoad) {
            verifyGraphics();
        }

        calculateGraphics();
    }

    public override void PostMapInit() {
        base.PostMapInit();
        if (Destroyed) {
            return;
        }

        cachedMap = MapHeld;
        watcherComponent = cachedMap.GetComponent<Watcher>();
        frostComponent = cachedMap.GetComponent<FrostGrid>();
        PlantReactionUtil.ApplyGraphicFrozen.TryGetValue(def, out effectsHolder);
        hasAnyGraphic = effectsHolder != null;
        calculateGraphics();
    }

    private Map cachedMap;
    private Watcher watcherComponent;
    private FrostGrid frostComponent;
    private bool displayingFloweringGraphic;
    private bool displayingDroughtGraphic;
    private bool displayingFrostGraphic;
    private bool displayingFrostLeaflessGraphic;

    public override void TickLong() {
        base.TickLong();
        calculateGraphics();
    }

    private void calculateGraphics() {
        if (Destroyed) {
            return;
        }

        if (!hasAnyGraphic) {
            return;
        }

        if (!EffectSettings.allowPlantEffects && !effectsHolder.active) {
            return;
        }

        if (MapHeld != cachedMap) {
            cachedMap = MapHeld;
            watcherComponent = MapHeld.GetComponent<Watcher>();
            frostComponent = MapHeld.GetComponent<FrostGrid>();
        }

        if (watcherComponent?.dontRunAnything != false) {
            return;
        }

        float temperature = watcherComponent.outdoorTemp;
        if (temperature > 21 &&
            (watcherComponent.season == Season.Spring ||
             (watcherComponent.season == Season.PermanentSummer &&
              watcherComponent.quadrum == Quadrum.Aprimay)) &&
            effectsHolder.weatherExtension.floweringGraphic != null) {
            if (!displayingFloweringGraphic) {
                displayingFloweringGraphic = true;
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            }
        }
        else {
            displayingFloweringGraphic = false;
        }


        if (watcherComponent.droughtActive && effectsHolder.weatherExtension.droughtGraphic != null) {
            if (!displayingDroughtGraphic) {
                displayingDroughtGraphic = true;
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            }
        }
        else {
            displayingDroughtGraphic = false;
        }

        if (EffectSettings.showFrostGrid
            && temperature < 0 &&
            frostComponent?.GetDepth(PositionHeld) >= 0.3f) {
            
            if (effectsHolder.weatherExtension.frostLeaflessGraphic != null && LeaflessNow) {
                if (!displayingFrostLeaflessGraphic) {
                    displayingFrostLeaflessGraphic = true;
                    Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
                }
            }
            else {
                displayingFrostLeaflessGraphic = false;
            }

            if (effectsHolder.weatherExtension.frostGraphic != null) {
                if (!displayingFrostGraphic) {
                    displayingFrostGraphic = true;
                    Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
                }
            }
            else {
                displayingFrostGraphic = false;
            }
        }
        else {
            displayingFrostLeaflessGraphic = false;
            displayingFrostGraphic = false;
        }
    }

    public override Graphic Graphic {
        get
        {
            if (!hasAnyGraphic || !effectsHolder.active) {
                return base.Graphic;
            }

            if (LifeStage == PlantLifeStage.Sowing) {
                return GraphicSowing;
            }

            if (def.plant.pollutedGraphic != null && PositionHeld.IsPolluted(MapHeld)) {
                return def.plant.pollutedGraphic;
            }

            if (def.plant.leaflessImmatureGraphic != null && LeaflessNow && !HarvestableNow) {
                return def.plant.leaflessImmatureGraphic;
            }

            if (displayingFrostLeaflessGraphic && LeaflessNow && (!sown || !HarvestableNow)) {
                // Have this check before the other frost and leafless check
                return effectsHolder.weatherExtension.frostLeaflessGraphic;
            }

            if (def.plant.leaflessGraphic != null && LeaflessNow && (!sown || !HarvestableNow)) {
                return def.plant.leaflessGraphic;
            }

            if (displayingFrostGraphic) {
                return effectsHolder.weatherExtension.frostGraphic;
            }

            if (def.plant.immatureGraphic != null && !HarvestableNow) {
                return def.plant.immatureGraphic;
            }

            if (displayingFloweringGraphic) {
                return effectsHolder.weatherExtension.floweringGraphic;
            }

            if (displayingDroughtGraphic) {
                return effectsHolder.weatherExtension.droughtGraphic;
            }

            return base.Graphic;
        }
    }

    private void verifyGraphics() {
        if (effectsHolder?.weatherExtension?.floweringGraphic == null) {
            displayingFloweringGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.droughtGraphic == null) {
            displayingDroughtGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.frostGraphic == null) {
            displayingFrostGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.frostLeaflessGraphic == null) {
            displayingFrostLeaflessGraphic = false;
        }
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref displayingFloweringGraphic, "displayFloweringGraphic");
        Scribe_Values.Look(ref displayingDroughtGraphic, "displayDroughtGraphic");
        Scribe_Values.Look(ref displayingFrostGraphic, "displayFrostGraphic");
        Scribe_Values.Look(ref displayingFrostLeaflessGraphic, "displayFrostLeaflessGraphic");
    }
}