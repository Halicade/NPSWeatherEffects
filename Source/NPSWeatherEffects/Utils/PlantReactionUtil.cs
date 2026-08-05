using System.Collections.Frozen;
using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public static class PlantReactionUtil
{
    public static FrozenDictionary<ThingDef, bool> ApplyGraphicFrozen = [];

    public static void InitializePlantGraphics() {
        List<ThingDef> allPlants = DefDatabase<ThingDef>.AllDefsListForReading;
        EffectSettings.plantEffectsFor ??= new Dictionary<string, bool>();
        var applyGraphicFor = new Dictionary<ThingDef, bool>();
        foreach (var plant in allPlants) {
            if (plant.plant == null)
                continue;

            ThingWeatherReaction modExtension = plant.GetModExtension<ThingWeatherReaction>();
            if (modExtension == null) continue;

            if (modExtension.initializeGraphics(plant)) {
                if (EffectSettings.plantEffectsFor.TryGetValue(plant.defName, out bool result)) {
                    applyGraphicFor.Add(plant, result);
                }
                else {
                    EffectSettings.plantEffectsFor.Add(plant.defName, true);
                    applyGraphicFor.Add(plant, true);
                }
            }
        }

        ApplyGraphicFrozen = applyGraphicFor.ToFrozenDictionary();
    }

    public static void updateDictionary() {
        var applyGraphicFor = new Dictionary<ThingDef, bool>();
        applyGraphicFor.Clear();
        List<ThingDef> allPlants = DefDatabase<ThingDef>.AllDefsListForReading;

        foreach (var plant in allPlants) {
            if (plant.plant == null)
                continue;

            ThingWeatherReaction modExtension = plant.GetModExtension<ThingWeatherReaction>();
            if (modExtension == null) continue;


            if (modExtension.hasGraphic) {
                if (EffectSettings.plantEffectsFor.TryGetValue(plant.defName, out bool result)) {
                    applyGraphicFor.Add(plant, result);
                }
            }
        }

        ApplyGraphicFrozen = applyGraphicFor.ToFrozenDictionary();
    }
}