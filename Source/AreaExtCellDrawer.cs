using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Verse;

namespace AreaInclusionExclusion
{
    public class AreaExtCellDrawer
    {
        public readonly AreaExt parentAreaExt;
        public bool dirty = true;
        private List<Mesh> meshes = new List<Mesh>();
        private Material material = null;

        private bool shouldDraw = false;

        private const float opacity = 0.33f;

        public AreaExtCellDrawer(AreaExt areaExt)
        {
            this.parentAreaExt = areaExt;
        }

        public void RegenerateMesh()
        {
            foreach (var m in meshes)
            {
                m.Clear();
            }
            meshes.Clear();

            float y = AltitudeLayer.MapDataOverlay.AltitudeFor();

            int cells = 0;
            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();
            Mesh mesh = new Mesh();

            var mapSize = parentAreaExt.Map.Size;

            BitArray exclusionBA = null;
            bool needToCalcExclusion = true;

            var innerAreas = parentAreaExt.InnerAreas;
            for (int i = 0; i < innerAreas.Count; ++i)
            {
                Area area = innerAreas[i].Key;
                AreaExtOperator op = innerAreas[i].Value;

                if (op == AreaExtOperator.Inclusion)
                {
                    if (needToCalcExclusion)
                    {
                        exclusionBA = new BitArray(mapSize.x * mapSize.z);
                        exclusionBA.SetAll(false);

                        for (int j = i + 1; j < innerAreas.Count; ++j)
                        {
                            if (innerAreas[j].Value != AreaExtOperator.Exclusion)
                            {
                                continue;
                            }

                            exclusionBA = exclusionBA.Or(AreaExt.GetAreaBitArray(innerAreas[j].Key));
                        }

                        exclusionBA = exclusionBA.Not();
                    }

                    CellRect cellRect = new CellRect(0, 0, mapSize.x, mapSize.z);
                    for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                    {
                        for (int k = cellRect.minZ; k <= cellRect.maxZ; k++)
                        {
                            int index = CellIndicesUtility.CellToIndex(j, k, mapSize.x);
                            if (area[index] && exclusionBA[index])
                            {
                                verts.Add(new Vector3((float)j, y, (float)k));
                                verts.Add(new Vector3((float)j, y, (float)(k + 1)));
                                verts.Add(new Vector3((float)(j + 1), y, (float)(k + 1)));
                                verts.Add(new Vector3((float)(j + 1), y, (float)k));
                                colors.Add(area.Color);
                                colors.Add(area.Color);
                                colors.Add(area.Color);
                                colors.Add(area.Color);

                                int count = verts.Count;
                                tris.Add(count - 4);
                                tris.Add(count - 3);
                                tris.Add(count - 2);
                                tris.Add(count - 4);
                                tris.Add(count - 2);
                                tris.Add(count - 1);

                                cells++;
                                if (cells >= 16383)
                                {
                                    mesh.SetVertices(verts);
                                    mesh.SetColors(colors);
                                    mesh.SetTriangles(tris, 0);

                                    verts.Clear();
                                    colors.Clear();
                                    tris.Clear();

                                    meshes.Add(mesh);
                                    mesh = new Mesh();

                                    cells = 0;
                                }
                            }
                        }
                    }

                }
                else
                {
                    needToCalcExclusion = true;
                }
            }

            if (verts.Count > 0)
            {
#if DEBUG
                Log.Message(string.Format("Vertices: {0}", verts.Count));
#endif
                mesh.SetVertices(verts);
                mesh.SetColors(colors);
                mesh.SetTriangles(tris, 0);
                meshes.Add(mesh);
            }

            if (material == null)
            {
                material = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 1f, 1f, opacity), true);
                material.renderQueue = 3600;
            }

            dirty = false;
        }

        public void MarkForDraw()
        {
            shouldDraw = true;
        }

        public void Update()
        {
            if (shouldDraw)
            {
                if (dirty)
                {
                    RegenerateMesh();
                }

                foreach (Mesh mesh in meshes)
                {
                    Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
                }
            }

            shouldDraw = false;
        }
    }
}
