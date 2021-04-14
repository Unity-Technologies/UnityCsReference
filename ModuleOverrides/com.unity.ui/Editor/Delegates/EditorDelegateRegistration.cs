// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class EditorDelegateRegistration
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            VisualTreeAssetChangeTrackerUpdater.IsEditorPlaying = IsEditorPlaying;
            DefaultEventSystem.IsEditorRemoteConnected = IsEditorRemoteConnected;
            VisualTreeAssetChangeTrackerUpdater.UpdateGameView = EditorApplication.QueuePlayerLoopUpdate;
            AssetOperationsAccess.GetAssetPath = GetAssetPath;
            AssetOperationsAccess.GetAssetDirtyCount = GetAssetDirtyCount;
        }

        internal static bool IsEditorPlaying()
        {
            return EditorApplication.isPlaying;
        }

        internal static bool IsEditorRemoteConnected()
        {
            return EditorApplication.isRemoteConnected;
        }

        internal static string GetAssetPath(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        internal static int GetAssetDirtyCount(Object asset)
        {
            return EditorUtility.GetDirtyCount(asset);
        }
    }
}
