# Adding compatibility

## TerrainWeatherReactions



Terrain can have the mod extension `NPSWeather.TerrainWeatherReactions`

    TerrainDef floodTerrain;
    TerrainDef freezeTerrain;
    int freezeAt;
    bool holdFrost;
    float temperatureAdjust;
    int wetAt;
    TerrainDef wetTerrain;

    TerrainDef tideTerrain;
    TerrainDef riverTerrain;

- `wetTerrain` Terrain this cell will turn into when it rains. This only occurs if the setting "Show wet terrain" is
  active.
- `floodTerrain` Terrain this cell will turn into when it rains. This only occurs if the setting "Show flood terrain" is
  active.
- `wetAt` The level of wetness that needs to be reached before a terrain can become wet or flooded. This value defaults
  to 0.
- `holdFrost` This terrain can receive frost from the setting "Show frost". This is purely visual.
- `temperatureAdjust` Pawns will receive an offset of this value if they are on this terrain. This is only active if
  the "Apply ambient temperature patch" is active.
- `tideTerrain` The terrain that will be used when a biome has a custom `oceanShallowTerrain`
- `riverTerrain` The terrain that will be used when a biome has a custom `waterMovingShallowTerrain`. This is only used
  if Odyssey is not active.
- `freezeTerrain` Terrain this cell will turn into when the temperature

Most of these patches were extracted directly from [terrain-water-patches.xml](./../1.6/Patches/terrain-water-patches.xml)
with Defs coming from [TerrainDefs](./../1.6/Defs/TerrainDefs).

### Wet terrain

Terrain can change when it is raining by adding a mod extension like below

    <Operation Class="PatchOperationAddModExtension">
      <xpath>Defs/TerrainDef[defName = "Soil"]</xpath>
      <value>
        <li Class="NPSWeather.TerrainWeatherReactions">
          <wetTerrain>TKKN_SoilWet</wetTerrain>
          <wetAt>1</wetAt>
          <holdFrost>True</holdFrost>
        </li>
      </value>
    </Operation>

Where `TKKN_SoilWet` is the wet terrain texture. There are a couple wet terrain variants that can be found
in [WetTerrain.xml](./../1.6/Defs/TerrainDefs/WetTerrain.xml) and others can be made using similar logic.
The `wetAt` field can be used to determine how much rain should be required. Higher numbers increase the time it takes
to get wet.

If you want to create a custom wet terrain, you want to either omit make sure `<driesTo />` is set to not receive
anything.
Additionally, a blank modExtension is necessary.

The field `<holdFrost>True</holdFrost>` is to allow the frostGrid to be active on this terrain. Wet terrain and frost
are not exclusive to each other. You can have one active without the other.

### Flood terrain

`floodTerrain` is similar to `wetTerrain` but intended for when normally soil block should become water instead. A
terrain can use both of these options

### Freezing terrain

If Odyssey is not active, terrain can freeze when certain temperatures are met. Setting a `freezeTerrain` will allow
that terrain to freeze when `freezeAt` is reached. If `freezeAt` is not indicated, it defaults to 0 degrees Celsius.

    <Operation Class="PatchOperationAddModExtension">
    <xpath>Defs/TerrainDef[defName = "Mud"]</xpath>
    <value>
      <li Class="NPSWeather.TerrainWeatherReactions">
        <wetTerrain>NPS_WaterMuddy</wetTerrain>
        <floodTerrain>NPS_WaterMuddy</floodTerrain>
        <freezeTerrain>TKKN_MuddyIce</freezeTerrain>
        <wetAt>1</wetAt>
        <holdFrost>True</holdFrost>
      </li>
    </value>
    </Operation>

<br/>

    <Operation Class="PatchOperationAddModExtension">
        <xpath>Defs/TerrainDef[defName = "WaterMovingChestDeep"]</xpath>
        <value>
            <li Class="NPSWeather.TerrainWeatherReactions">
                <freezeTerrain>TKKN_Ice</freezeTerrain>
                <freezeAt>-1</freezeAt>
                <temperatureAdjust>-5</temperatureAdjust>
            </li>
        </value>
    </Operation>

For creating the frozen terrain, it is required to be temporary

### Adding tidal and river water terrain

If a BiomeDef has their own `oceanShallowTerrain` or `waterMovingShallowTerrain` defined, TerrainDefs will need to be
generated in order for that biome to have those effects active.

Easiest way to do this is to copy the terrain needed, prefix the defName with your unique identifier and add/replace the
following in the terrainDef

    <driesTo />
    <canFreeze>false</canFreeze>
    <temporary>true</temporary>

Additionally, you will need to add the below terrain tags for ocean tides

    <li>Flood</li>
    <li>NPS_Tide</li>

And these tags for river flooding

    <li>Flood</li>
    <li>NPS_River</li>

The `Flood` tag allows plants that have `destroyedByFlooding` (usually trees) to not be destroyed.
While not required, it is recommended
`NPS_Tide` and `NPS_River` are used internally to identify the terrain

An example of new defs taken from base game ocean and river terrain

    <TerrainDef ParentName="WaterShallowBase">
        <defName>NPS_WaterOceanShallow</defName>
        <label>shallow ocean water</label>
        <texturePath>Terrain/Surfaces/WaterShallowRamp</texturePath>
        <pollutedTexturePath>Terrain/Surfaces/WaterShallowRampPolluted</pollutedTexturePath>
        <waterDepthShader>Map/WaterDepth</waterDepthShader>
        <renderPrecedence>396</renderPrecedence>
        
        <driesTo />
        <canFreeze>false</canFreeze>
        <temporary>true</temporary>
        
        <waterBodyType>Saltwater</waterBodyType>
        <tags>
            <li>Ocean</li>
            <li>Flood</li>
            <li>NPS_Tide</li>
        </tags>
    </TerrainDef>
    
    <TerrainDef ParentName="WaterShallowBase">
        <defName>NPS_WaterMovingShallow</defName>
        <label>shallow moving water</label>
        <texturePath>Terrain/Surfaces/WaterShallowRamp</texturePath>
        <pollutedTexturePath>Terrain/Surfaces/WaterShallowRampPolluted</pollutedTexturePath>
        <waterDepthShader>Map/WaterDepth</waterDepthShader>
        <renderPrecedence>398</renderPrecedence>
        <affordances>
            <li>MovingFluid</li>
        </affordances>
        <tags>
            <li>River</li>
            <li>WaterFreshShallow</li>
            <li>WaterFreshShallowMoving</li>
        </tags>
        <waterDepthShaderParameters>
            <_UseWaterOffset>1</_UseWaterOffset>
        </waterDepthShaderParameters>
    </TerrainDef>

After this you will need to add an xml patch to the original def

    <Operation Class="PatchOperationAddModExtension">
        <xpath>Defs/TerrainDef[defName="WaterOceanShallow"]</xpath>
        <value>
            <li Class="NPSWeather.TerrainWeatherReactions">
                <!-- The tide terrain we just made-->
                <tideTerrain>NPS_WaterOceanShallow</tideTerrain>
                <!-- 
                If the "Ambient temperature" patch is active, the pawns ambient temperature will be offset by tyhis amount
                --> 
                <temperatureAdjust>-5</temperatureAdjust>
            </li>
        </value>
    </Operation>
    
    <Operation Class="PatchOperationAddModExtension">
        <xpath>Defs/TerrainDef[defName="WaterOceanShallow"]</xpath>
        <value>
            <li Class="NPSWeather.TerrainWeatherReactions">
                <!-- The terrain we just made-->
                <riverTerrain>NPS_WaterOceanShallow</riverTerrain>
                <!-- 
                If the "Ambient temperature" patch is active, the pawns ambient temperature will be offset by tyhis amount
                -->
                <temperatureAdjust>-5</temperatureAdjust>
            </li>
        </value>
    </Operation>