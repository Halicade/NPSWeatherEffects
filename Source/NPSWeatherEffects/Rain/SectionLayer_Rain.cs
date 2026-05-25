using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace NPSWeather;

public class SectionLayer_Rain : SectionLayer
{

    private readonly float[] adjValuesTmp = new float[9];

    private static readonly List<float> opacityListTmp = [];

    private static readonly List<List<int>> vertexWeights = [
        [0, 1, 2, 8],
        [2, 8],
        [2, 3, 4, 8],
        [4, 8],
        [4, 5, 6, 8],
        [6, 8],
        [6, 7, 0, 8],
        [0, 8],
        [8]
    ];

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

        NativeArray<float> depthGrid_Unsafe = WetnessGrid.DepthGridDirect_Unsafe;
        CellRect cellRect = section.CellRect;
        bool bigChange = false;

        CellIndices cellIndices = Map.cellIndices;
        for (int i = cellRect.minX; i <= cellRect.maxX; i++) {
            for (int j = cellRect.minZ; j <= cellRect.maxZ; j++) {
                opacityListTmp.Clear();
                float num = depthGrid_Unsafe[cellIndices.CellToIndex(i, j)];
                for (int k = 0; k < 9; k++) {
                    IntVec3 c = new IntVec3(i, 0, j) + GenAdj.AdjacentCellsAndInsideForUV[k];
                    adjValuesTmp[k] = (c.InBounds(Map) ? depthGrid_Unsafe[cellIndices.CellToIndex(c)] : num);
                }

                for (int l = 0; l < 9; l++) {
                    float num2 = 0f;
                    for (int m = 0; m < vertexWeights[l].Count; m++) {
                        num2 += adjValuesTmp[vertexWeights[l][m]];
                    }

                    num2 /= vertexWeights[l].Count;
                    if (num2 > 0.01f) {
                        bigChange = true;
                    }

                    opacityListTmp.Add(num2);
                }

                for (int num3 = 0; num3 < 9; num3++) {
                    float num6 = opacityListTmp[num3];
                    subMesh.colors.Add(new Color32(243, 140, 247,
                        Convert.ToByte(num6 * 180f)));
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
}