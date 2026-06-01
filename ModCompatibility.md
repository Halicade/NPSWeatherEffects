# General

This mod makes heavy use of `TerrainGrid.SetTerrain` and `GenTemperature.TryGetTemperatureForCell`. There are warnings that display the name of mods that are patching these specific methods. It is reccomended to turn off relevant settings if there is too much lag. Or remove the mentioned mods.

For `TerrainGrid.SetTerrain`, settings making use of this include `Ground/Water freezes` and `Show rain effects`

For `GenTemperature.TryGetTemperatureForCell`, there is a setting `Use map-wide temperature` toggle this on and most cell calculations will make use of the maps temperature instead. This includes indoors. Pawns will continue to query their current cell for temperature related effects.

Terraforming mods should be compatible. However, if a wet terrain block is terraformed, it will not be affected until save and reload. A warning will appear indicating it has been changed but should otherwise not be an issue.

### Mods that have compatibility in some way with this mod.

| Mod                                                                                                   | PackageId                     | Notes                                                                                                           |
|-------------------------------------------------------------------------------------------------------|-------------------------------|-----------------------------------------------------------------------------------------------------------------|
| [Biomes! Caverns](https://steamcommunity.com/sharedfiles/filedetails/?id=2969748433)                  | biomesteam.biomescaverns      | Excludes Earthen depths from having an oasis                                                                    |
| [Biomes! Oasis](https://steamcommunity.com/sharedfiles/filedetails/?id=2538518381)                    | biomesteam.oasis              | Excludes chromatic oasis from having springs                                                                    |
| [Biomes! Prehistoric](https://steamcommunity.com/sharedfiles/filedetails/?id=2860715703)                                                                               | biomesteam.biomesprehistoric                              | Trilobite can appear from the beach                                                                             |
| [Combat Extended](https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044)                  | ceteam.combatextended         | Ammo can appear dropped off from ocean tide                                                                     
| [Desire Paths (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3555461401)         | mlie.desirepaths              | NPS' path making will not run                                                                                   |
| [GRiNDTerra Biomes](https://steamcommunity.com/sharedfiles/filedetails/?id=3537211820)                | grimterra.biomesmod           | Stone crab can appear from the beach                                                                            |
| [Mashed's Ashlands](https://steamcommunity.com/sharedfiles/filedetails/?id=3109835541)                | sirmashedpotato.ashlands                              | Excludes some biomes from having springs. Most maps will not have ocean tide.                                   |
| [More Vanilla Biomes](https://steamcommunity.com/sharedfiles/filedetails/?id=1931453053)              | zylle.morevanillabiomes       | Excludes some biomes from having springs                                                                        |
| [[TW1.6]幻彩林地 Rainbow forest](https://steamcommunity.com/sharedfiles/filedetails/?id=3532501549)       | tw.tangsbiome.rainbowforest   | Crystal crab can appear from the beach                                                                          |
| [Rimbrellas](https://steamcommunity.com/sharedfiles/filedetails/?id=2079784964)                       | battlemage64.Rimbrellas       | Pawns won't get wet if holding umbrella in rain                                                                 |
| [Seasonal Weather (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2045114229)     | mlie.seasonalweather          | NPS' seasonal weather will not be active.                                                                       |
| [Vanilla Landmarks Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3656316229)       | vanillaexpanded.vexploratione | Added checks for Rising waters tile mutator. If this is active on the map, this mods beach tide will not occur. |
| [VGP Garden Tools](https://steamcommunity.com/sharedfiles/filedetails/?id=2007063961)                 | dismarzero.vgp.vgpgardentools | Plowed soil and Tilled farm soil have wet variants                                                              |
| [Water Freezes (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3542918378)        | mlie.waterfreezes             | NPS' water freezing will not run                                                                                |
| [Wayward Biomes: Tidewrack Reef](https://steamcommunity.com/sharedfiles/filedetails/?id=3698012987)   | zylle.waywardbiomes.landreef  | Smaller animals can appear from beach                                                                           |
| [WeatherControl](https://steamcommunity.com/sharedfiles/filedetails/?id=3047040031)                   | nightmare.weathercontrol      | NPS' seasonal weather will not be active.                                                                       |
| [Winter Taiga Biome 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3666855302)             | reel.wintertaigabiome2        | Lowered chance of allergies.                                                                                    |
| [Yaoma Storytellers - Jianghu Jin](https://steamcommunity.com/sharedfiles/filedetails/?id=3403972335) | zal.jianghujin                | Rebuilds cell map when effect triggers                                                                          |

### Other mods patching this one:

- [Ice Is Slippery](https://steamcommunity.com/sharedfiles/filedetails/?id=3317403790) Ice is patched to allow pawns to fall on ice.