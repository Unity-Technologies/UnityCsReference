using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class RuntimeEditorDelegateRegistration
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UIDocument.IsEditorPlaying = IsEditorPlaying;
            UIDocument.IsEditorPlayingOrWillChangePlaymode = IsEditorPlayingOrWillChangePlaymode;
            AssetOperationsAccess.LoadStyleSheetAtPath = LoadAssetAtPath<StyleSheet>;
            AssetOperationsAccess.LoadThemeAtPath = LoadAssetAtPath<ThemeStyleSheet>;
            PanelSettings.CreateRuntimePanelDebug = UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug;
            PanelSettings.GetOrCreateDefaultTheme = PanelSettingsCreator.GetOrCreateDefaultTheme;
            // This is a copy of an assignment in the Editor module used only for 2020.1 compatibility and should be
            // removed when support for 2020.1 is dropped.
            VisualTreeAssetChangeTrackerUpdater.IsEditorPlaying = IsEditorPlaying;
            AssetOperationsAccess.GetAssetPath = GetAssetPath;
            AssetOperationsAccess.GetAssetDirtyCount = GetAssetDirtyCount;
        }

        internal static string GetAssetPath(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        internal static T LoadAssetAtPath<T>(string asset) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(asset);
        }

        internal static int GetAssetDirtyCount(Object asset)
        {
            return EditorUtility.GetDirtyCount(asset);
        }

        internal static bool IsEditorPlaying()
        {
            return EditorApplication.isPlaying;
        }

        internal static bool IsEditorPlayingOrWillChangePlaymode()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
