using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class FrostGrid : MapComponent
{
    private const float MaxDepth = 1f;


    public FrostGrid(Map map) : base(map) {
        DepthGridDirect_Unsafe = new float[map.cellIndices.NumGridCells];
    }

    internal float[] DepthGridDirect_Unsafe { get; }

    private bool canHaveFrost(int ind) {
        var building = map.edificeGrid[ind];


        return building == null || building.def.category == ThingCategory.Building;
    }
    
    private bool canHaveFrost(cellData cell) {
        return !TerrainTagUtil.TKKN_Wet.Contains(cell.currentTerrain);
    }

    public void addDepth(cellData cell, float depthToAdd) {
        var num = cell.locationIndex;
        var oldDepth = DepthGridDirect_Unsafe[num];
        if ((oldDepth <= 0f && depthToAdd <= 0f) ||
            (oldDepth >= 0.999f && depthToAdd > MaxDepth)) {
            return;
        }

        if (!canHaveFrost(cell)) {
            DepthGridDirect_Unsafe[num] = 0f;
            return;
        }

        var newDepth = oldDepth + depthToAdd;
        newDepth = Mathf.Clamp(newDepth, 0f, cell.frostNoise);
        var num4 = newDepth - oldDepth;
        if (!(Mathf.Abs(num4) > 0.0001f)) {
            return;
        }

        DepthGridDirect_Unsafe[num] = newDepth;
        cell.frostLevel = newDepth;
        checkVisualOrPathCostChange(cell, oldDepth, newDepth);
    }

    public void removeDepth(cellData cell) {
        if (cell.frostLevel == 0) {
            return;
        }
        

        if (cell.frostLevel < 0.12) {
            DepthGridDirect_Unsafe[cell.locationIndex] = 0f;
            cell.frostLevel = 0;
            return;
        }

        var oldDepth = DepthGridDirect_Unsafe[cell.locationIndex];
        DepthGridDirect_Unsafe[cell.locationIndex] = 0;
        cell.frostLevel = 0;
        checkVisualOrPathCostChange(cell, oldDepth, 0);
    }

    public void setDepth(int locationIndex, float newDepth) {
        if (!canHaveFrost(locationIndex)) {
            DepthGridDirect_Unsafe[locationIndex] = 0f;
            return;
        }

        newDepth = Mathf.Clamp01(newDepth);
        DepthGridDirect_Unsafe[locationIndex] = newDepth;
    }

    private void checkVisualOrPathCostChange(cellData cell, float oldDepth, float newDepth) {
        if (Math.Abs(oldDepth - newDepth) < 1e-6f) {
            return;
        }

        if (newDepth == 0f || Mathf.Abs(oldDepth - newDepth) > 0.12f || Rand.Value < 0.0025f) {
            map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Frost, true, false);
        }
    }

    public float GetDepth(IntVec3 c) => c.InBounds(map) ? DepthGridDirect_Unsafe[map.cellIndices.CellToIndex(c)] : 0f;

    public float GetDepth(cellData c) => c.location.InBounds(map) ? DepthGridDirect_Unsafe[c.locationIndex] : 0f;
}