using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NPSWeather;

public static class PlantReactionUtil
{
    public static FrozenDictionary<ThingDef, PlantEffectHolder> ApplyGraphicFrozen = [];

    public static void InitializePlantGraphics() {
        List<ThingDef> allPlants = DefDatabase<ThingDef>.AllDefsListForReading;
        EffectSettings.plantsEffects ??= [];

        foreach (var plant in allPlants) {
            if (plant.plant == null) {
                continue;
            }

            ThingWeatherReaction modExtension = plant.GetModExtension<ThingWeatherReaction>();
            if (modExtension == null) {
                continue;
            }

            if (!modExtension.initializeGraphics(plant)) {
                continue;
            }

            if (!EffectSettings.plantsEffects.Any(pfh => pfh.defName == plant.defName)) {
                EffectSettings.plantsEffects.Add(new PlantEffectHolder(plant));
            }
        }

        updateDictionary();
    }

    public static void updateDictionary() {
        EffectSettings.ActivePlants = 0;
        EffectSettings.plantsEffects = EffectSettings.plantsEffects.OrderBy(x => x?.plant?.modContentPack?.Name)
            .ThenBy(x => x?.plant?.label).ToList();

        var applyGraphicFor = new Dictionary<ThingDef, PlantEffectHolder>();

        foreach (PlantEffectHolder pfh in EffectSettings.plantsEffects) {
            if (pfh.PlantValid) {
                applyGraphicFor.Add(pfh.plant, pfh);
                EffectSettings.ActivePlants++;
            }
        }

        ApplyGraphicFrozen = applyGraphicFor.ToFrozenDictionary();
    }
}