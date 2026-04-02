// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextUtilities = UnityEngine.UIElements.TextUtilities;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal static class EditorDelegateRegistration
    {
        static EditorDelegateRegistration()
        {
            DefaultEventSystem.IsEditorRemoteConnected = () => EditorApplication.isRemoteConnected;

            TextUtilities.getEditorTextSettings = () => EditorTextSettings.defaultTextSettings;

            UIDocument.IsEditorPlaying = () => EditorApplication.isPlaying;
            UIDocument.IsEditorPlayingOrWillChangePlaymode = () => EditorApplication.isPlayingOrWillChangePlaymode;

            PanelSettings.CreateRuntimePanelDebug = UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug;
            PanelSettings.GetOrCreateDefaultTheme = PanelSettingsCreator.GetFirstThemeOrCreateDefaultTheme;
            PanelSettings.GetGameViewResolution = (int display) =>
                {
                    // For events to work properly with multiple GameView on the same display, we need to prioritize the focused window
                    var mainPlayModeView = PlayModeView.GetMainPlayModeView();
                    if (mainPlayModeView?.targetDisplay == display)
                        return mainPlayModeView.targetSize;

                    foreach (var playModeView in PlayModeView.GetAllPlayModeViewWindows())
                    {
                        if (playModeView.targetDisplay == display)
                            return playModeView.targetSize;
                    }
                    return null;
                };
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
            PanelSettings.s_AssignICUData += SetICUDataAsset;

            EditorApplication.playModeStateChanged += stateChange =>
            {
                if (stateChange == PlayModeStateChange.EnteredPlayMode)
                    UIElementsRuntimeUtility.OnEnteredPlayMode();
                if (stateChange == PlayModeStateChange.ExitingPlayMode)
                    UIElementsRuntimeUtility.OnExitingPlayMode();
            };

            UIDocument.IsEditingPrefab = () => PrefabStageUtility.GetCurrentPrefabStage() != null;

            L10nUtility.SetTranslateFunc(L10n.Tr);
        }

        private static void SetICUDataAsset(PanelSettings target)
        {
            Debug.Assert(target != null, "target PanelSetting is null");
            var asset = ICUDataAssetUtilities.GetEditorICUAsset();
            Debug.Assert(asset != null, "ICU data in the default resources is not found");

            if (asset != null && target.m_ICUDataAsset != asset)
            {
                target.m_ICUDataAsset = asset;
                target.MarkDirty();
            }
        }
    }
}
