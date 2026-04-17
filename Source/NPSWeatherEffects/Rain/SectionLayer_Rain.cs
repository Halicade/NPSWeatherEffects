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
        // for some reason the custom one was causing a huge memory issue and rendering in giant squares :(

        if (subMesh.mesh.vertexCount == 0) {
            SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh,
                AltitudeLayer.LayingPawn); //so rain forms over items/plants
        }

        subMesh.Clear(MeshParts.Colors);

        var depthGridDirect_Unsafe = WetnessGrid.DepthGridDirect_Unsafe;
        var cellRect = section.CellRect;
        var num = Map.Size.z - 1;
        var num2 = Map.Size.x - 1;
        var b = false;

        var cellIndices = Map.cellIndices;
        for (var i = cellRect.minX; i <= cellRect.maxX; i++) // this is what renders it all blobby, I think
        {
            for (var j = cellRect.minZ; j <= cellRect.maxZ; j++) {
                var num3 = depthGridDirect_Unsafe[cellIndices.CellToIndex(i, j)];
                var color = rainDepthColor(num3);
                subMesh.colors.AddRange([color, color, color, color, color, color, color, color, color]);
                // I really have no idea what I'm doing here but this works
                // and is faster than having to calculate the numbers like below
                /*for (int k = 0; k < 9; k++) {

                    subMesh.colors.Add(color);
                }*/


                continue;

                var num4 = cellIndices.CellToIndex(i, j - 1);
                var num5 = j <= 0 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j - 1);
                var num6 = j <= 0 || i <= 0 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j);
                var num7 = i <= 0 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i - 1, j + 1);
                var num8 = j >= num || i <= 0 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i, j + 1);
                var num9 = j >= num ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j + 1);
                var num10 = j >= num || i >= num2 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j);
                var num11 = i >= num2 ? num3 : depthGridDirect_Unsafe[num4];
                num4 = cellIndices.CellToIndex(i + 1, j - 1);
                var num12 = j <= 0 || i >= num2 ? num3 : depthGridDirect_Unsafe[num4];
                vertDepth[0] = (num5 + num6 + num7 + num3) / 4f;
                vertDepth[1] = (num7 + num3) / 2f;
                vertDepth[2] = (num7 + num8 + num9 + num3) / 4f;
                vertDepth[3] = (num9 + num3) / 2f;
                vertDepth[4] = (num9 + num10 + num11 + num3) / 4f;
                vertDepth[5] = (num11 + num3) / 2f;
                vertDepth[6] = (num11 + num12 + num5 + num3) / 4f;
                vertDepth[7] = (num5 + num3) / 2f;
                vertDepth[8] = num3;
                for (var k = 0; k < 9; k++) {
                    if (vertDepth[k] > 0.01f) {
                        b = true;
                    }

                    subMesh.colors.Add(rainDepthColor(vertDepth[k]));
                }
            }
        }

        if (true) {
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