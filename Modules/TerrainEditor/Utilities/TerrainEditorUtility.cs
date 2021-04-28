// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TerrainTools;

namespace UnityEditor
{
    internal class TerrainModificationProcessor : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            PaintContext.ApplyDelayedActions();
            return paths;
        }
    }

    internal class TerrainEditorUtility
    {
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
