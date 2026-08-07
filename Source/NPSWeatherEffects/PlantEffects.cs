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
        PlantReactionUtil.ApplyGraphicFrozen.TryGetValue(def, out effectsHolder);
        hasAnyGraphic = effectsHolder?.weatherExtension != null;
        if (respawningAfterLoad) {
            verifyGraphics();
        }
        else {
            calculateGraphics();
        }
    }

    public override void PostMapInit() {
        base.PostMapInit();
        if (Destroyed) {
            return;
        }

        cachedMap = MapHeld;
        watcherComponent = cachedMap.GetComponent<Watcher>();
        PlantReactionUtil.ApplyGraphicFrozen.TryGetValue(def, out effectsHolder);
        hasAnyGraphic = effectsHolder != null;
    }

    private Map cachedMap;
    private Watcher watcherComponent;
    private bool displayFloweringGraphic;
    private bool displayDroughtGraphic;
    private bool displayFrostGraphic;
    private bool displayFrostLeaflessGraphic;

    public override void TickLong() {
        base.TickLong();
        calculateGraphics();
    }

    private void calculateGraphics() {
        displayFloweringGraphic = false;
        displayDroughtGraphic = false;
        displayFrostGraphic = false;
        displayFrostLeaflessGraphic = false;
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
        }

        if (watcherComponent?.dontRunAnything != false) {
            return;
        }

        var temperature = watcherComponent.outdoorTemp;
        if (temperature > 21) {
            if (watcherComponent.season == Season.Spring ||
                (watcherComponent.season == Season.PermanentSummer &&
                 watcherComponent.quadrum == Quadrum.Aprimay)) {
                if (!watcherComponent.isRaining
                    && effectsHolder.weatherExtension.floweringGraphic != null) {
                    displayFloweringGraphic = true;
                }
            }
        }


        if (watcherComponent.droughtActive && effectsHolder.weatherExtension.droughtGraphic != null) {
            displayDroughtGraphic = true;
        }


        if (EffectSettings.showFrostGrid
            && temperature < 0) {
            if (watcherComponent?.frostGridComponent.GetDepth(PositionHeld) >= 0.3f) {
                if (effectsHolder.weatherExtension.frostLeaflessGraphic != null && LeaflessNow) {
                    displayFrostLeaflessGraphic = true;
                }

                if (effectsHolder.weatherExtension.frostGraphic != null) {
                    displayFrostGraphic = true;
                }
            }
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

            if (displayFrostLeaflessGraphic && LeaflessNow && (!sown || !HarvestableNow)) {
                // Have this check before the other frost and leafless check
                return effectsHolder.weatherExtension.frostLeaflessGraphic;
            }

            if (def.plant.leaflessGraphic != null && LeaflessNow && (!sown || !HarvestableNow)) {
                return def.plant.leaflessGraphic;
            }

            if (displayFrostGraphic) {
                return effectsHolder.weatherExtension.frostGraphic;
            }

            if (def.plant.immatureGraphic != null && !HarvestableNow) {
                return def.plant.immatureGraphic;
            }

            if (displayFloweringGraphic) {
                return effectsHolder.weatherExtension.floweringGraphic;
            }

            if (displayDroughtGraphic) {
                return effectsHolder.weatherExtension.droughtGraphic;
            }

            return base.Graphic;
        }
    }

    private void verifyGraphics() {
        if (effectsHolder?.weatherExtension?.floweringGraphic == null) {
            displayFloweringGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.droughtGraphic == null) {
            displayDroughtGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.frostGraphic == null) {
            displayFrostGraphic = false;
        }

        if (effectsHolder?.weatherExtension?.frostLeaflessGraphic == null) {
            displayFrostLeaflessGraphic = false;
        }
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref displayFloweringGraphic, "displayFloweringGraphic");
        Scribe_Values.Look(ref displayDroughtGraphic, "displayDroughtGraphic");
        Scribe_Values.Look(ref displayFrostGraphic, "displayFrostGraphic");
        Scribe_Values.Look(ref displayFrostLeaflessGraphic, "displayFrostLeaflessGraphic");
    }
}