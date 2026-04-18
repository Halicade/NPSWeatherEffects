using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather.Rain;

public class WetnessGrid : MapComponent
{
    private const float MaxDepth = 1f;


    public WetnessGrid(Map map) : base(map) {
        DepthGridDirect_Unsafe = new float[map.cellIndices.NumGridCells];
    }

    internal float[] DepthGridDirect_Unsafe { get; }

    private bool canHaveRain(int ind) {
        //Add check for if water?
        return map.edificeGrid[ind] == null && !map.fogGrid.IsFogged(ind);
    }

    public bool addDepth(cellData cell, float depthToAdd) {
        float oldDepth = DepthGridDirect_Unsafe[cell.locationIndex];
        if ((oldDepth <= 0f && depthToAdd <= 0f) ||
            (oldDepth >= 0.999f && depthToAdd > MaxDepth)) {
            return false;
        }

        float newDepth = oldDepth + depthToAdd;
        if (newDepth < 0f) {
            newDepth = 0f;
        }
        else if (newDepth > cell.rainNoise) {
            newDepth = cell.rainNoise;
        }

        float depthChange = newDepth - oldDepth;
        if (depthChange is > -0.0001f and < 0.0001f) {
            return false;
        }

        DepthGridDirect_Unsafe[cell.locationIndex] = newDepth;
        
        
        cell.rainLevel = newDepth;
        
        if (newDepth == 0f || depthChange > 0.1f || Rand.Value < 0.0025f) {
            map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Rain, true, false);
            return true;
        }

        return false;
        
        
    }

    public bool subDepth(cellData cell, float depthToAdd) {
        float oldDepth = DepthGridDirect_Unsafe[cell.locationIndex];
        if ((oldDepth <= 0f && depthToAdd <= 0f) ||
            (oldDepth >= 0.999f && depthToAdd > MaxDepth)) {
            return false;
        }

        float newDepth = oldDepth + depthToAdd;
        if (newDepth < 0f) {
            newDepth = 0f;
        }
        else if (newDepth > cell.rainNoise) {
            newDepth = cell.rainNoise;
        }

        float depthChange = Math.Abs( oldDepth-newDepth);
        if (depthChange < 0.0001f) {
            return false;
        }

        DepthGridDirect_Unsafe[cell.locationIndex] = newDepth;
        // checkVisualOrPathCostChange
        cell.rainLevel = newDepth;
        
        if (newDepth == 0f || depthChange > 0.1f || Rand.Value < 0.0085f) {
            map.mapDrawer.MapMeshDirty(cell.location, MapMeshDefOf.NPS_Rain, true, false);
            return true;
        }

        return false;
    }

    public void removeDepth(cellData cell) {
        if (cell.rainLevel == 0) {
            return;
        }
        
        DepthGridDirect_Unsafe[cell.locationIndex] = 0f;
        if (cell.rainLevel < 0.12) {
            cell.rainLevel = 0;
            return;
        }
        cell.rainLevel = 0;
        return;
    }

    public void setDepth(int locationIndex, float newDepth) {
        if (!canHaveRain(locationIndex)) {
            DepthGridDirect_Unsafe[locationIndex] = 0f;
            return;
        }

        newDepth = Mathf.Clamp01(newDepth);
        //var num2 = DepthGridDirect_Unsafe[locationIndex];
        DepthGridDirect_Unsafe[locationIndex] = newDepth;
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

    public float GetDepth(IntVec3 c) => c.InBounds(map) ? DepthGridDirect_Unsafe[map.cellIndices.CellToIndex(c)] : 0f;

    public float GetDepth(cellData c) => c.location.InBounds(map) ? DepthGridDirect_Unsafe[c.locationIndex] : 0f;
}