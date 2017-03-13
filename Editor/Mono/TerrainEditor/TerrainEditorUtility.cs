// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class TerrainEditorUtility
    {
        internal static void RemoveSplatTexture(TerrainData terrainData, int index)
        {
            Undo.RegisterCompleteObjectUndo(terrainData, "Remove texture");

            int width = terrainData.alphamapWidth;
            int height = terrainData.alphamapHeight;
            float[,,] alphamap = terrainData.GetAlphamaps(0, 0, width, height);
            int alphaCount = alphamap.GetLength(2);

            int newAlphaCount = alphaCount - 1;
            float[,,] newalphamap = new float[height, width, newAlphaCount];

            // move further alphamaps one index below
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    for (int a = 0; a < index; ++a)
                        newalphamap[y, x, a] = alphamap[y, x, a];
                    for (int a = index + 1; a < alphaCount; ++a)
                        newalphamap[y, x, a - 1] = alphamap[y, x, a];
                }
            }

            // normalize weights in new alpha map
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float sum = 0.0F;
                    for (int a = 0; a < newAlphaCount; ++a)
                        sum += newalphamap[y, x, a];
                    if (sum >= 0.01)
                    {
                        float multiplier = 1.0F / sum;
                        for (int a = 0; a < newAlphaCount; ++a)
                            newalphamap[y, x, a] *= multiplier;
                    }
                    else
                    {
                        // in case all weights sum to pretty much zero (e.g.
                        // removing splat that had 100% weight), assign
                        // everything to 1st splat texture (just like
                        // initial terrain).
                        for (int a = 0; a < newAlphaCount; ++a)
                            newalphamap[y, x, a] = (a == 0) ? 1.0f : 0.0f;
                    }
                }
            }

            // remove splat from terrain prototypes
            SplatPrototype[] splats = terrainData.splatPrototypes;
            SplatPrototype[] newSplats = new SplatPrototype[splats.Length - 1];
            for (int a = 0; a < index; ++a)
                newSplats[a] = splats[a];
            for (int a = index + 1; a < alphaCount; ++a)
                newSplats[a - 1] = splats[a];
            terrainData.splatPrototypes = newSplats;

            // set new alphamaps
            terrainData.SetAlphamaps(0, 0, newalphamap);
        }

        internal static void RemoveTree(Terrain terrain, int index)
        {
            TerrainData terrainData = terrain.terrainData;
            if (terrainData == null)
                return;
            Undo.RegisterCompleteObjectUndo(terrainData, "Remove tree");
            terrainData.RemoveTreePrototype(index);
        }

        internal static void RemoveDetail(Terrain terrain, int index)
        {
            TerrainData terrainData = terrain.terrainData;
            if (terrainData == null)
                return;
            Undo.RegisterCompleteObjectUndo(terrainData, "Remove detail object");
            terrainData.RemoveDetailPrototype(index);
        }

        internal static bool IsLODTreePrototype(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<LODGroup>() != null;
        }
    }
} //namespace
