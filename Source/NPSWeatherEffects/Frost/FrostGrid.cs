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

    public void AddDepth(cellData cell, float depthToAdd) {
        var num = cell.locationIndex;
        var num2 = DepthGridDirect_Unsafe[num];
        if (num2 <= 0f && depthToAdd <= 0f) {
            return;
        }

        if (num2 >= 0.999f && depthToAdd > MaxDepth) {
            return;
        }
/*
        if (!canHaveFrost(num)) {
            DepthGridDirect_Unsafe[num] = 0f;
            return;
        }
*/
        var num3 = num2 + depthToAdd;
        num3 = Mathf.Clamp(num3, 0f, cell.frostNoise);
        var num4 = num3 - num2;
        if (!(Mathf.Abs(num4) > 0.0001f)) {
            return;
        }

        DepthGridDirect_Unsafe[num] = num3;
        checkVisualOrPathCostChange(cell, num2, num3);
    }

    public void removeDepth(cellData cell) {
        if (cell.frostLevel == 0) {
            return;
        }
        DepthGridDirect_Unsafe[cell.locationIndex] = 0f;
        checkVisualOrPathCostChange(cell, cell.frostLevel, 0f);
    }

    public void SetDepth(int locationIndex, float newDepth) {

        
        if (!canHaveFrost(locationIndex)) {
            DepthGridDirect_Unsafe[locationIndex] = 0f;
            return;
        }

        newDepth = Mathf.Clamp(newDepth, 0f, 1f);
        var num2 = DepthGridDirect_Unsafe[locationIndex];
        DepthGridDirect_Unsafe[locationIndex] = newDepth;
        //checkVisualOrPathCostChange(c, num2, newDepth);
    }

    private void checkVisualOrPathCostChange(cellData cell, float oldDepth, float newDepth) {
        cell.frostLevel = newDepth;
        if (Mathf.Approximately(oldDepth, newDepth)) {
            //Checked in case values didn't change/were 0
            return;
        }

        if (newDepth == 0f || Mathf.Abs(oldDepth - newDepth) > 0.12f || Rand.Value < 0.0025f) {
            map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Frost, true, false);
        }
    }
    
    public float GetDepth(IntVec3 c) => c.InBounds(map) ? DepthGridDirect_Unsafe[map.cellIndices.CellToIndex(c)] : 0f;

    public float GetDepth(cellData c) => c.location.InBounds(map) ? DepthGridDirect_Unsafe[c.locationIndex] : 0f;
}