// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextUtilities = UnityEngine.UIElements.TextUtilities;

namespace UnityEditor.UIElements
{
    internal static partial class EditorDelegateRegistration
    {
        // Every slot installed here is cleared on code reload, so this registration has to run on each
        // load. A static constructor would only run once per domain, leaving runtime UI Toolkit without
        // its editor-side implementations (play-mode queries, text settings, panel debug, ICU data)
        // after the first reload.
        [OnCodeLoaded]
        static void Initialize()
        {
            DefaultEventSystem.IsEditorRemoteConnected = () => EditorApplication.isRemoteConnected;

            TextUtilities.getEditorTextSettings = () => EditorTextSettings.defaultTextSettings;

            UIDocument.IsEditorPlaying = () => EditorApplication.isPlaying;
            UIDocument.IsEditorPlayingOrWillChangePlaymode = () => EditorApplication.isPlayingOrWillChangePlaymode;

            PanelSettings.CreateRuntimePanelDebug = UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug;
            PanelSettings.GetOrCreateDefaultTheme = PanelSettingsCreator.GetFirstThemeOrCreateDefaultTheme;
#pragma warning disable UAL0018 // the property setter forwards to GameViewRenderInfoQuery.getImplementation, which is [AutoStaticsCleanupOnCodeReload] and reinstalled here on every load; the analyzer cannot see the cleaned field through the property indirection
            PanelSettings.GetGameViewRenderInfo = PlayModeView.GetLastInteractedGameView;
#pragma warning restore UAL0018
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
            PanelSettings.RequestEditorPlayerLoopUpdate = EditorApplication.QueuePlayerLoopUpdate;
            PanelSettings.s_AssignICUData += SetICUDataAsset;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            UIDocument.IsEditingPrefab = () => PrefabStageUtility.GetCurrentPrefabStage() != null;
            UIDocument.IsGameObjectInOpenPrefabStage = go => PrefabStageUtility.GetPrefabStage(go) != null;

            L10nUtility.SetTranslateFunc(text => L10n.Tr(text, null));
        }

        [OnCodeUnloading]
        static void Uninitialize()
        {
            PanelSettings.s_AssignICUData -= SetICUDataAsset;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
                UIElementsRuntimeUtility.OnEnteredPlayMode();
            if (stateChange == PlayModeStateChange.ExitingPlayMode)
                UIElementsRuntimeUtility.OnExitingPlayMode();
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
