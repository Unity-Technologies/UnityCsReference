// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/TerrainEditor/TerrainInspectorUtil.bindings.h")]
    internal static class TerrainInspectorUtil
    {
        // Calculate the size of the brush with which we are painting trees
        public static extern float GetTreePlacementSize(TerrainData terrainData, int prototypeIndex, float spacing, float treeCount);

        public static extern bool CheckTreeDistance(TerrainData terrainData, Vector3 position, int prototypeIndex, float distanceBias);

        public static extern Vector3 GetPrototypeExtent(TerrainData terrainData, int prototypeIndex);

        public static extern int GetPrototypeCount(TerrainData terrainData);

        public static extern bool PrototypeIsRenderable(TerrainData terrainData, int prototypeIndex);
    }
}
