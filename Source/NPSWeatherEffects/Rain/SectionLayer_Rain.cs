using RimWorld;
using UnityEngine;
using Verse;

namespace NPSWeather.Rain;

public class SectionLayer_Rain : SectionLayer
{
    private static readonly Color32 ColorClear = new(243, 140, 247, 0); // 194, 219, 249

    private static readonly Color32 ColorWhite = new(243, 140, 247, 180);
    private readonly float[] vertDepth = new float[9];

    public SectionLayer_Rain(Section section) : base(section) {
        relevantChangeTypes = MapMeshDefOf.NPS_Rain;
    }

    public override bool Visible => EffectSettings.showRainGrid;

    private WetnessGrid WetnessGrid => Map.GetComponent<WetnessGrid>();

    public override void Regenerate() {
        LayerSubMesh subMesh = GetSubMesh(MatBases.Wetness);

        if (subMesh.mesh.vertexCount == 0) {
            SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh,
                AltitudeLayer.LayingPawn);
        }

        subMesh.Clear(MeshParts.Colors);

        float[] depthGridDirect_Unsafe = WetnessGrid.DepthGridDirect_Unsafe;
        CellRect cellRect = section.CellRect;
        int sizeZ = Map.Size.z - 1;
        int sizeX = Map.Size.x - 1;
        bool bigChange = false;

        CellIndices cellIndices = Map.cellIndices;
        for (int i = cellRect.minX; i <= cellRect.maxX; i++)
        {
            for (int j = cellRect.minZ; j <= cellRect.maxZ; j++) {
                float origin = depthGridDirect_Unsafe[cellIndices.CellToIndex(i, j)];
                /*
                 Faster method but does not look as smooth
                var color = rainDepthColor(num3);
                subMesh.colors.AddRange([color, color, color, color, color, color, color, color, color]);
                for (int k = 0; k < 9; k++) {

                    subMesh.colors.Add(color);
                }
                continue;
                */

                int num4 = cellIndices.CellToIndex(i, j - 1);
                float south = j <= 0 ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j - 1);
                float southWest = j <= 0 || i <= 0 ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j);
                float west = i <= 0 ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j + 1);
                float northWest = j >= sizeZ || i <= 0 ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i, j + 1);
                float north = j >= sizeZ ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j + 1);
                float northEast = j >= sizeZ || i >= sizeX ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j);
                float east = i >= sizeX ? origin : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j - 1);
                float southEast = j <= 0 || i >= sizeX ? origin : depthGridDirect_Unsafe[num4];
                vertDepth[0] = (south + southWest + west + origin) / 4f;
                vertDepth[1] = (west + origin) / 2f;
                vertDepth[2] = (west + northWest + north + origin) / 4f;
                vertDepth[3] = (north + origin) / 2f;
                vertDepth[4] = (north + northEast + east + origin) / 4f;
                vertDepth[5] = (east + origin) / 2f;
                vertDepth[6] = (east + southEast + south + origin) / 4f;
                vertDepth[7] = (south + origin) / 2f;
                vertDepth[8] = origin;
                for (int k = 0; k < 9; k++) {
                    if (vertDepth[k] > 0.01f) {
                        bigChange = true;
                    }

                    subMesh.colors.Add(rainDepthColor(vertDepth[k]));
                }
            }
        }

        if (bigChange) {
            subMesh.disabled = false;
            subMesh.FinalizeMesh(MeshParts.Colors);
        }
        else {
            subMesh.disabled = true;
        }
    }


    private static Color32 rainDepthColor(float rainDepth) {
        return ColorWhite.MutateAlpha((byte)(rainDepth * 180));
    }
}