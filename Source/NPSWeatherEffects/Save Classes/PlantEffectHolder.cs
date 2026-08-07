using UnityEngine;
using Verse;

namespace NPSWeather;

public class PlantEffectHolder : IExposable
{
    public string defName;
    public ThingDef plant;
    public bool active;
    public ThingWeatherReaction weatherExtension;

    public bool PlantValid {
        get
        {
            if (!field) {
                plant = DefDatabase<ThingDef>.GetNamed(defName, false);
                weatherExtension = plant.GetModExtension<ThingWeatherReaction>();
                field = true;
            }

            return weatherExtension != null;
        }
    }

    public string ModName => plant?.modContentPack?.ModMetaData?.Name ?? defName;

    public string PlantName => plant.LabelCap;

    public Texture2D PlantIcon => plant.uiIcon;

    public PlantEffectHolder() { }

    public PlantEffectHolder(ThingDef plant) {
        this.plant = plant;
        defName = plant.defName;
        active = true;
    }

    public void ExposeData() {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref active, "active");
    }
}