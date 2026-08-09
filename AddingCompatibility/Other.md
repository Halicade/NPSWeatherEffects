### WashUpOnBeach

WashUpOnBeach is a small extension that can be applied on `PawnKindDef`s.
It allows animals to spawn on the beach when the tide changes.

    <li Class="NPSWeather.WashUpOnBeach">
        <isCrabCritter>true</true>
    </li>

It has an optional field `isCrabCritter` that can be used to indicate small animals like crabs.
These can appear on the beach when it rains.

### DontDestroyTide

Assigning DontDestroyTide on a ThingDef will prevent it from being destroyed by the tide. 

### MutatorSettings

MutatorSettings is an extension that can be applied to `TileMutatorDef`s to allow modifications to ocean tide.

    <li Class="NPSWeather.MutatorSettings">
        <tideFactor>0.75</tideFactor>
        <tideVariant>Diurnal</tideVariant>
    </li>

`tideVariant` can be SemiDiurnal, Diurnal, MixedSemiDiurnal, or left blank for the default SemiDiurnal.
`tideFactor` defaults to 1. Values around 0 - 1.3 should work but verify first.
If set to 0, there will be no tide when this mutator is active.


### PlanetLayerValid

By default, other `PlanetLayerDef` will not be valid unless given this mod extension. The extension itself has no data inside. 

    <li Class="NPSWeather.PlanetLayerValid" />