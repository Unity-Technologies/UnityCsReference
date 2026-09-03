// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
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

    internal partial class TerrainEditorUtility
    {
        [AutoStaticsCleanupOnCodeReload]
        internal static bool? IsEditableOverride { get; set; }
        [AutoStaticsCleanupOnCodeReload]
        static int s_CachedModeIndex;
        [AutoStaticsCleanupOnCodeReload]
        static bool? s_CachedIsEditable;

        internal static bool IsEditable()
        {
            if (IsEditableOverride.HasValue)
                return IsEditableOverride.Value;

            var modeIndex = ModeService.currentIndex;
            if (!s_CachedIsEditable.HasValue || modeIndex != s_CachedModeIndex)
            {
                s_CachedIsEditable = ModeService.HasCapability(modeIndex, ModeCapability.AssetAuthoring, true);
                s_CachedModeIndex = modeIndex;
            }

            return s_CachedIsEditable.Value;
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
