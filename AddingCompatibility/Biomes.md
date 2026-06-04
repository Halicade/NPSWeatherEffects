### Biomes

Biomes can have the mod extension `NPSWeather.BiomeSeasonalSettings`

    //incident settings
    List<ThingDef> bloomPlants;
    List<PawnKindDef> specialHerds;

    //spring settings
    int maxSprings = 1;
    float springSpawnChance = 0;

    float tideFactor = 1;

    //disease settings
    List<BiomeDiseaseRecord> springDiseases;
    List<BiomeDiseaseRecord> summerDiseases;
    List<BiomeDiseaseRecord> fallDiseases;
    List<BiomeDiseaseRecord> winterDiseases;

    //incident settings
    List<TKKN_IncidentCommonalityRecord> springEvents;
    List<TKKN_IncidentCommonalityRecord> summerEvents;
    List<TKKN_IncidentCommonalityRecord> fallEvents;
    List<TKKN_IncidentCommonalityRecord> winterEvents;

    //weather settings
    List<WeatherCommonalityRecord> springWeathers;
    List<WeatherCommonalityRecord> summerWeathers;
    List<WeatherCommonalityRecord> fallWeathers;
    List<WeatherCommonalityRecord> winterWeathers;


`bloomPlants` These are plants that can spawn during the superbloom game condition. 
These can spawn regardless of a biomes allowed plants.

`specialHerds` animals that can spawn from the `Herd Migration` IncidentDef. 
This incident will spawn 50-70 animals that will travel across the map. 
These animals need to be capable of `CanDoHerdMigration` in order to take effect

`maxSprings` determines the maximum amount of water springs that can spawn on a biomes map. Default is 1
`springSpawnChance` if maxSprings is larger than 1, it is used to determine the chance additional springs can appear.
The first spring will always be generated.
If you want to exclude a biome from creating springs entirely, it is better to
patch [TileMutators.xml](../1.6/Defs/MapGen/TileMutators.xml) directly to prevent them from spawning at all

`tideFactor` if on a coast tile this determines the impact of the tide. 
This is better to keep smaller as otherwise the tide won't have enough time to complete a cycle.
Values around 0 - 1.3 should generally be fine but should be tested.
If set to 0, that means no tide will occur on this biome.

### Seasonal settings

Seasonal settings will lead to a change in commonality for the event that's listed. 
All four seasons should be filled out and each should have the same defs defined for consistency.
In order for these changes to work, the biomeDef needs to originally have the disease or weather listed.
For maps that are a permanent summer/winter,
aprimay is treated as spring, Jugust is summer, Septober is fall, and Decembary is winter

`TKKN_IncidentCommonalityRecord` is written similar to the base games `WeatherCommonalityRecord`

Full examples are provided in [BaseGameBiomePatches.xml](../1.6/Patches/BaseGameBiomePatches.xml). 
But for a quick example this was taken from the modExtension for boreal forest

    <li Class="NPSWeather.BiomeSeasonalSettings">
        
        <maxSprings>3</maxSprings>
        <springSpawnChance>0.8</springSpawnChance>
        <bloomPlants>
            <li>Plant_Moss</li>
            <li>Plant_Berry</li>
            <li>Plant_Healroot</li>
            <li MayRequire="hali.NPSBiomes">TKKN_PlantWildflowers</li>
        </bloomPlants>

        <specialHerds>
            <li>Caribou</li>
        </specialHerds>

        <springEvents>
            <Aurora>1</Aurora>
        </springEvents>
        <summerEvents>
            <Aurora>.3</Aurora>
        </summerEvents>
        <fallEvents>
            <Aurora>1</Aurora>
        </fallEvents>
        <winterEvents>
            <Aurora>1.6</Aurora>
        </winterEvents>

        <springWeathers>
            <Fog>2</Fog>
        </springWeathers>
        <summerWeathers>
            <Fog>3</Fog>
        </summerWeathers>
        <fallWeathers>
            <Fog>2</Fog>
        </fallWeathers>
        <winterWeathers>
            <Fog>1</Fog>
        </winterWeathers>

        <springDiseases>
            <li>
                <diseaseInc>TKKN_Disease_Common_Cold</diseaseInc>
                <commonality>180</commonality>
            </li>
        </springDiseases>
        <summerDiseases>
            <li>
                <diseaseInc>TKKN_Disease_Common_Cold</diseaseInc>
                <commonality>80</commonality>
            </li>
        </summerDiseases>
        <fallDiseases>
            <li>
                <diseaseInc>TKKN_Disease_Common_Cold</diseaseInc>
                <commonality>180</commonality>
            </li>
        </fallDiseases>
        <winterDiseases>
            <li>
                <diseaseInc>TKKN_Disease_Common_Cold</diseaseInc>
                <commonality>100</commonality>
            </li>
        </winterDiseases>
    </li>