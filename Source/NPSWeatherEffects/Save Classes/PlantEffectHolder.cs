using System.Text;
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

    public string DescriptionHoverText {
        get
        {
            if (field == null) {

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("NPS_effectsActive".Translate());
                ;
                if (weatherExtension.floweringGraphic != null) {
                    stringBuilder.AppendLine("NPS_floweringGraphic".Translate());
                }

                if (weatherExtension.droughtGraphic != null) {
                    stringBuilder.AppendLine("NPS_droughtGraphic".Translate());
                }

                if (weatherExtension.frostGraphic != null) {
                    stringBuilder.AppendLine("NPS_frostGraphic".Translate());
                }

                if (weatherExtension.frostLeaflessGraphic != null) {
                    stringBuilder.AppendLine("NPS_frostLeaflessGraphic".Translate());
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine(" - " + (plant?.modContentPack?.ModMetaData?.Name ?? "NPS_unknownMod".Translate()));
                field = stringBuilder.ToString();
            }

            return field;
        }
    }

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