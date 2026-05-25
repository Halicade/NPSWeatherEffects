# [Nature's Pretty Sweet: Weather Effects](https://steamcommunity.com/sharedfiles/filedetails/?id=3542949511)

TODO update link

This is an update and rework of the mod Natures Pretty Sweet
Originally created by tkkntkkns
https://steamcommunity.com/sharedfiles/filedetails/?id=1211694919
Then continued by Mlie
https://steamcommunity.com/sharedfiles/filedetails/?id=3542949511

When I first found this mod back in November I fell in love with it. Downloaded it, opened up a quick test, and watched the terrain slowly transform from the usual dryness to a density that made the dense forest feel like a forest when it rained.

The overall performance was pretty bad though. So I embarked on a multi-month journey to rewrite and optimize the heck out of this mod. Many months later, I think it's ready for release.
This mod has been split into three parts, Weather Effects, Biomes, and Snow textures.

### Map features

These can all be turned off in settings.

- Rain effects - Soil and sand will darken when wet. This results in an increase in soil fertility. Other terrain, like mud or river banks, can completely change to shallow water.
- Ocean tides - These tides follow a semidiurnal cycle. You can also find tiles that contain diurnal and mixed semidiurnal cycles. - Inspired by a combination of beaches in Florida in the pacific northwest.
- Rare items - Ocean tides can reveal items. This ranges from seaweed to rare treasure. They will wash away during the next tide.
- Frost grid - When it gets cold, frost will appear around the map. This is a very subtle effect that is purely cosmetic.
- Seasonal weather - Most biomes have weather tuned to the current season/month. Expect more rain in the spring than in the summer for example.
- Seasonal incidents - Heat waves can be more common in the summer and less common in the winter
- Seasonal diseases - You are less likely to get a cold in the summer and more likely in the fall and winter.
- Springs - Hot springs appear in colder areas while cold springs appear in warmer. Pawns resting in these springs will gain a mood boost. They can enter springs naturally to relax, or even remove heatstroke in cold springs. Springs inspired by Arkansas Hot Springs, Crystal Springs and Blue Springs, Florida.
- New weather events - Wind storms, overcast, thick fog, dust storms
- New incidents - herd migrations, droughts, rainbows, wildflower blooms

### Pawn Features

These can all be turned off in settings. There is also an option to affect all pawns, or only colonists.

- Water impact - Pawns will be impacted more by water. Rain or entering water will raise a pawns comfortable temperature range.
- Drowning - If downed, pawns can drown in water.
- Paths - In high traffic areas, pawns can generate dirt paths from regular paths. Pawns will also pack down snow naturally while moving.
- More noticeable cold breath. Pawns emit a much more noticeable breath of air when it's cold.

### Additional features

This mod was originally made many years before Odyssey and has some similar features that Odyssey has. As such, they will not be active if Odyssey is active. They do not make use of any Odyssey code or concepts.

- River levels - Rivers will rise and fall during the year. This can depend on rainfall, season, and different events.
- Water freezes - Water, riverbanks, and some other terrain can freeze when it gets colder.
- "swimming" - Pawms can "swim" while in water. This is purely visual and does not offer any benefit.

### Performance

My main motivation for making this was improving performance. As such, I did all I could to make this much more performant than previous versions. While some settings can cause lag (changing the terrain on 70% of the map can cause a little slowdown, who knew?) terrain changes should be rather fast. Regardless, measure the performance in Dubs Analyzer and let me know of any excessive slowdowns you observe. Additionally, warnings will pop up if you have any mods patching methods this mod makes heavy use of. More info in "Compatibility".

### Compatibility

For specific compatibility please see https://github.com/Halicade/NPSWeatherEffects/blob/Main/ModCompatibility.md
There are some warnings that pop up in the logs at startup if other mods patch certain methods are detected.
Terraforming mods should be compatible. However, if a wet terrain block is terraformed, it may not be affected until save and reload.

This should be save compatible. As with any mod that adds items/terrain hash collisions are possible. While I don't recommend removal, there is an option to disable all weather effects and will remove all effects from the current map.

### Credits

tkkntkkn - Original creator of this mod
mlie - Continued this mod so I was able to find it
Paradox - Helped with bugs, testing, and general balance
Rogue The Rogue - Helped with bugs, testing, and suggested new features
Rimworld discord - Helped with general balance and comforted me during my descent into madness

### Bug reports

Please supply a log using the [Log Publisher](https://steamcommunity.com/sharedfiles/filedetails/?id=2873415404) for any bugs that may occur.
