using System;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class WetnessGrid(Map map) : MapComponent(map), IDisposable
{

    private NativeArray<float> depthGrid = new(map.cellIndices.NumGridCells, Allocator.Persistent);

    internal NativeArray<float> DepthGridDirect_Unsafe => depthGrid;

    private bool canHaveRain(int ind) {
        //Add check for if water?
        return map.edificeGrid[ind] == null && !map.fogGrid.IsFogged(ind);
    }

    public bool addDepth(cellData cell, float depthToAdd) {
        float oldDepth = depthGrid[cell.locationIndex];
        if ((oldDepth <= 0f && depthToAdd < 0f) || (oldDepth >= 0.999f && depthToAdd > 1f)) {
            return false;
        }

        float newDepth = oldDepth + depthToAdd;
        newDepth = Mathf.Clamp(newDepth, 0, cell.rainNoise);
        float depthChange = newDepth - oldDepth;
        if (!(Mathf.Abs(depthChange) > 0.0001f)) {
            return false;
        }

        depthGrid[cell.locationIndex] = newDepth;
        cell.rainLevel = newDepth;
        if (!Mathf.Approximately(oldDepth, newDepth)) {
            if (newDepth == 0f || Mathf.Abs(oldDepth - newDepth) > 0.25f || Rand.Value < 0.0125f) {
                //map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Rain, regenAdjacentCells: true, regenAdjacentSections: false);
                return true;
            }
        }


        return false;
    }

    public void refreshAt(IntVec3 cellToRefresh) {
        map.mapDrawer.MapMeshDirty(cellToRefresh, MapMeshDefOf.NPS_Rain, true, false);
    }

    public void removeDepth(cellData cell) {
        if (cell.rainLevel == 0) {
            return;
        }

        depthGrid[cell.locationIndex] = 0f;

        checkVisualOrPathCostChange(cell, cell.rainLevel, 0);
        return;
    }

    public void setDepth(int locationIndex, float newDepth) {
        if (!canHaveRain(locationIndex)) {
            depthGrid[locationIndex] = 0f;
            return;
        }

        newDepth = Mathf.Clamp01(newDepth);
        //var num2 = depthGrid[locationIndex];
        depthGrid[locationIndex] = newDepth;
        //checkVisualOrPathCostChange(c, num2, newDepth);
    }

    private bool checkVisualOrPathCostChange(cellData cell, float oldDepth, float newDepth) {
        cell.rainLevel = newDepth;
        if (Math.Abs(oldDepth - newDepth) < 1e-6f) {
            return false;
        }

        if (newDepth == 0f || Math.Abs(oldDepth - newDepth) > 0.1f || Rand.Value < 0.0025f) {
            map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Rain, true, false);
            return true;
        }

        return false;
    }

    public float GetDepth(IntVec3 c) => c.InBounds(map) ? depthGrid[map.cellIndices.CellToIndex(c)] : 0f;

    public float GetDepth(cellData c) => c.location.InBounds(map) ? depthGrid[c.locationIndex] : 0f;

    public void Dispose() {
        depthGrid.Dispose();
    }
}