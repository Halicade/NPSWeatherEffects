using HarmonyLib;
using UnityEngine;
using Verse;

namespace NPSWeather;

[HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
internal class MouseoverReadout_MouseoverReadoutOnGUI
{
    private static Map cachedMap;
    private static FrostGrid cachedFrostGrid;
    private static Watcher watcher;

    public static void Postfix() {
        if (!EffectSettings.showDevReadout || Event.current.type != EventType.Repaint ||
            Find.MainTabsRoot.OpenTab != null) {
            return;
        }

        var c = UI.MouseCell();
        var map = Find.CurrentMap;
        if (!c.InBounds(map)) {
            return;
        }

        if (cachedMap != map) {
            cachedMap = map;
            cachedFrostGrid = map.GetComponent<FrostGrid>();
            watcher = map.GetComponent<Watcher>();
        }

        if (watcher.dontRunAnything) {
            return;
        }

        Rect rect;
        var botLeft = new Vector2(15f, 65f);
        var num = 38f;
        var zone = c.GetZone(map);
        if (zone != null) {
            num += 19f;
        }

        var depth = map.snowGrid.GetDepth(c);
        if (depth > 0.03f) {
            num += 19f;
        }

        var thingList = c.GetThingList(map);
        foreach (var thing in thingList) {
            if (thing.def.category != ThingCategory.Mote) {
                num += 19f;
            }
        }

        var roof = c.GetRoof(map);
        if (roof != null) {
            num += 19f;
        }


        rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
        var label3 = $"C: x-{c.x} y-{c.y} z-{c.z}";
        Widgets.Label(rect, label3);
        num += 19f;

        if (watcher.cellWeatherAffects.TryGetValue(c, out var cell)) {
            var currentTerrain = cell.currentTerrain;
            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var cellTemp = $"Temperature: {cell.temperature}";
            Widgets.Label(rect, cellTemp);
            num += 19f;
            // GetTideLevel

            if (watcher.doCoast) {
                rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
                var labelTideLevel =
                    $"Current Tide height: {watcher.tideLevel} | Desired Tide height: {watcher.GetTideLevel()}";
                Widgets.Label(rect, labelTideLevel);
                num += 19f;

                rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
                var labelCellTideLevel =
                    $"Tide level: {cell.tideLevel} | Tide focus: {cell.tideFocus}";
                Widgets.Label(rect, labelCellTideLevel);
                num += 19f;
            }

            if (watcher.doRiverFlooding) {
                rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
                var labelFloodLevel =
                    $"Current Flood level: {watcher.floodLevel} | Desired flood level: {watcher.GetRiverLevel()}";
                Widgets.Label(rect, labelFloodLevel);
                num += 19f;

                rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
                var labelCellFloodLevel =
                    $"Flood level: {cell.riverLevel} | River focus: {cell.riverFocus}";
                Widgets.Label(rect, labelCellFloodLevel);
                num += 19f;
            }

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var cellTerrain =
                $"Cell Info: Current Terrain: {c.GetTerrain(map)} | Current Terrain cached: {currentTerrain}";
            Widgets.Label(rect, cellTerrain);
            num += 19f;

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var cellStatus = $"Wet {cell.isWet} | Frozen {cell.isFrozen}";
            Widgets.Label(rect, cellStatus);
            num += 19f;

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var cellWetTags =
                $"NPS_Wet {TerrainTagUtil.NPS_Water.Contains(currentTerrain)}NPS_Deep {TerrainTagUtil.NPS_DeepWater.Contains(currentTerrain)}";
            Widgets.Label(rect, cellWetTags);
            num += 19f;


            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var cellWet = $"Cell Info: howWet {cell.howWet} | How Packed {cell.howPacked}";
            //var cellWet = $"Cell Info: howWet {cell.howWet} | How Wet (Plants) {cell.howWetPlants} | How Packed {cell.howPacked}";
            TerrainWeatherReactions weatherExt = currentTerrain?.GetModExtension<TerrainWeatherReactions>();
            if (weatherExt != null) {
                if (weatherExt.wetTerrain != null) {
                    cellWet += $" | T Wet {weatherExt.wetTerrain}";
                }

                if (weatherExt.freezeTerrain != null) {
                    cellWet += $" | T Freeze {weatherExt.freezeTerrain}";
                }
            }


            Widgets.Label(rect, cellWet);
            num += 19f;

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var frostLabel = $"MaxFrost: {cell.frostNoise} | CurrentFrost: {cell.frostLevel}";
            Widgets.Label(rect, frostLabel);
            num += 19f;

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var rainLabel = $"MaxRain: {cell.rainNoise} | CurrentRain: {cell.rainLevel}";
            Widgets.Label(rect, rainLabel);
            num += 19f;

            rect = new Rect(botLeft.x, UI.screenHeight - botLeft.y - num, 999f, 999f);
            var seasonLabel =
                $"Quadrum: {watcher.quadrum} | Prev quadrum: {watcher.previousQuadrum} | Season: {watcher.season}";
            Widgets.Label(rect, seasonLabel);
        }


        //	Widgets.Label(rect, unused + " " + depth.ToString());
    }
}