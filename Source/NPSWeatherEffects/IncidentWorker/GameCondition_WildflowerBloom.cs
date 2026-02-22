using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NPSWeather;

public class GameCondition_WildflowerBloom : GameCondition
{
    private List<ThingDef> floweringPlants = [];

    public override void Init() {
        base.Init();

        floweringPlants = DefDatabase<ThingDef>.AllDefs.Where(PlantConditions)
            .ToList();
        floweringPlants.Add(ThingDefOf.Plant_Tinctoria);
        if (ThingDefOf.TKKN_PlantWildflowers != null) {
            floweringPlants.Add(ThingDefOf.TKKN_PlantWildflowers);
        }
    }

    private bool PlantConditions(ThingDef plant) {
        if (plant?.plant == null) {
            return false;
        }

        if (plant.plant.IsTree ||
            plant.plant.diesToLight ||
            plant.ingestible?.foodType == FoodTypeFlags.Tree ||
            plant.plant.LifespanDays >= 30) {
            return false;
        }

        if (plant.GetStatValueAbstract(StatDefOf.BeautyOutdoors) >= 4) {
            Log.Message("Adding plant because of beauty" + plant);
            return true;
        }


        return false;
    }

    public override void GameConditionTick() {
        base.GameConditionTick();
        if (TicksPassed % 100 == 0) {
            foreach (var map in AffectedMaps) {
                MaybeGetAPlant(map);
            }
        }
    }

    private void MaybeGetAPlant(Map map) {
        if (!Rand.Chance(0.2f)) {
            return;
        }

        var randomCell = CellFinder.RandomCell(map);

        var cellRadius = Rand.Range(3, 7);
        List<IntVec3> radiusCell = GenRadial.RadialCellsAround(randomCell, cellRadius, true).ToList();
        foreach (var radialCell in radiusCell) {
            if (!Rand.Chance(0.65f)) {
                continue;
            }

            // Basically taking the logic done in PlantUtility.CanNowPlantAt and checking that once instead of once for each plant.
            if (!radialCell.IsValid || !radialCell.InBounds(map)) {
                continue;
            }

            if (radialCell.GetTerrain(map).fertility == 0) {
                continue;
            }

            bool continueOriginal = true;

            foreach (Thing thing in radialCell.GetThingList(map)) {
                if (map.designationManager.DesignationOn(thing, DesignationDefOf.Uninstall) != null ||
                    map.designationManager.DesignationOn(thing, DesignationDefOf.Deconstruct) != null ||
                    thing is Building building && map.listerBuildings.TryGetReinstallBlueprint(building, out var _)) {
                    continueOriginal = false;
                    break;
                }
            }

            if (!continueOriginal) {
                continue;
            }

            foreach (var randomPlant in floweringPlants.InRandomOrder()) {
                if (randomPlant.CanEverPlantAt(radialCell, map)) {
                    var makePlant = (Plant)ThingMaker.MakeThing(randomPlant);
                    makePlant.Growth = Rand.Range(0.2f, 0.9f);
                    GenSpawn.Spawn(makePlant, radialCell, SingleMap);
                    break;
                }
            }
        }
    }
}