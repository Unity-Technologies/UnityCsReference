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
