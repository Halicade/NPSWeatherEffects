# General

This mod makes heavy use of `TerrainGrid.SetTerrain` and `GenTemperature.TryGetTemperatureForCell`. There are warnings that display the name of mods that are patching these specific methods. It is reccomended to turn off relevant settings if there is too much lag. Or remove the mentioned mods.

For `TerrainGrid.SetTerrain`, settings making use of this include `Ground/Water freezes` and `Show rain effects`

For `GenTemperature.TryGetTemperatureForCell`, there is a setting `Use map-wide temperature` toggle this on and most cell calculations will make use of the maps temperature instead. This includes indoors. Pawns will continue to query their current cell for temperature related effects.

Terraforming mods should be compatible. However, if a wet terrain block is terraformed, it will not be affected until save and reload. A warning will appear indicating it has been changed but should otherwise not be an issue.

### Mods that have compatibility in some way with this mod.

| Mod                                                                                                   | PackageId                     | Notes                                                                                                           |
|-------------------------------------------------------------------------------------------------------|-------------------------------|-----------------------------------------------------------------------------------------------------------------|
| [Combat Extended](https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044)                  | ceteam.combatextended         | Ammo can appear dropped off from ocean tide                                                                     
| [Desire Paths (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3555461401)         | mlie.desirepaths              | NPS' path making will not run                                                                                   |
| [Rimbrellas](https://steamcommunity.com/sharedfiles/filedetails/?id=2079784964)                       | battlemage64.Rimbrellas       | Pawns won't get wet if holding umbrella in rain                                                                 |
| [Vanilla Landmarks Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3656316229)       | vanillaexpanded.vexploratione | Added checks for Rising waters tile mutator. If this is active on the map, this mods beach tide will not occur. |
| [Water Freezes (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3542918378)        | mlie.waterfreezes             | NPS' water freezing will not run                                                                                |
| [Winter Taiga Biome 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3666855302)             | reel.wintertaigabiome2        | Lowered chance of allergies.                                                                                    |
| [Yaoma Storytellers - Jianghu Jin](https://steamcommunity.com/sharedfiles/filedetails/?id=3403972335) | zal.jianghujin                | Rebuilds cell map when effect triggers                                                                          |

### Other mods patching this one:

- [Ice Is Slippery](https://steamcommunity.com/sharedfiles/filedetails/?id=3317403790) Ice is patched to allow pawns to fall on ice.