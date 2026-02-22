using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NPSWeather;

public class GameCondition_SuperBloom : GameCondition
{

    private Dictionary<Map, List<ThingDef>> biomeSeasonalPlants = [];

    public override void Init() {
        base.Init();
        foreach (var map in AffectedMaps) {
            biomeSeasonalPlants[map] = SingleMap.Biome.GetModExtension<BiomeSeasonalSettings>()?.bloomPlants;
        }
    }

    public override void GameConditionTick() {
        base.GameConditionTick();
        if (TicksPassed % 100 == 0) {
            foreach (var map in AffectedMaps) {
                if (biomeSeasonalPlants.TryGetValue(map, out var biomePlants)) {
                    if (biomePlants.NullOrEmpty()) {
                        return;
                    }

                    MaybeGetAPlant(map, biomePlants);
                }
                else {
                    biomeSeasonalPlants[map] = SingleMap.Biome.GetModExtension<BiomeSeasonalSettings>()?.bloomPlants;
                }
            }
        }
    }

    private void MaybeGetAPlant(Map map, List<ThingDef> biomePlants) {
        if (!Rand.Chance(0.2f)) {
            return;
        }

        var randomCell = CellFinder.RandomCell(map);

        var cellRadius = Rand.Range(3, 7);
        List<IntVec3> radiusCell = GenRadial.RadialCellsAround(randomCell, cellRadius, true).ToList();
        foreach (var radialCell in radiusCell) {
            if (!Rand.Chance(0.7f)) {
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
                    thing is Building building && map.listerBuildings.TryGetReinstallBlueprint(building, out _)) {
                    
                    continueOriginal = false;
                    break;
                }
            }

            if (!continueOriginal) {
                continue;
            }

            foreach (var randomPlant in biomePlants.InRandomOrder()) {
                if(randomPlant.CanEverPlantAt(radialCell,map)) {
                    var makePlant = (Plant)ThingMaker.MakeThing(randomPlant);
                    makePlant.Growth = Rand.Range(0.5f, 1f);
                    GenSpawn.Spawn(makePlant, radialCell, SingleMap);
                    break;
                }
            }
        }
    }

}