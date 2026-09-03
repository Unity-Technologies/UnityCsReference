// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
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
            PanelSettings.GetGameViewRenderInfo = PlayModeView.GetLastInteractedGameView;
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
            PanelSettings.RequestEditorPlayerLoopUpdate = EditorApplication.QueuePlayerLoopUpdate;
            PanelSettings.s_AssignICUData += SetICUDataAsset;

            EditorApplication.playModeStateChanged += stateChange =>
            {
                if (stateChange == PlayModeStateChange.EnteredPlayMode)
                    UIElementsRuntimeUtility.OnEnteredPlayMode();
                if (stateChange == PlayModeStateChange.ExitingPlayMode)
                    UIElementsRuntimeUtility.OnExitingPlayMode();
            };

            UIDocument.IsEditingPrefab = () => PrefabStageUtility.GetCurrentPrefabStage() != null;

            L10nUtility.SetTranslateFunc(text => L10n.Tr(text, null));
        }

        private static void SetICUDataAsset(PanelSettings target)
        {
            Debug.Assert(target != null, "target PanelSetting is null");
            var asset = TextLib.GetICUAssetEditorDelegate?.Invoke();
            Debug.Assert(asset != null, "ICU data in the default resources is not found");

            if (asset != null && target.m_ICUDataAsset != asset)
            {
                target.m_ICUDataAsset = asset;
                target.MarkDirty();
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
