// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
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

            if (stateChange == PlayModeStateChange.EnteredPlayMode)
                DetachPrefabStagePanelComponents();
            else if (stateChange == PlayModeStateChange.EnteredEditMode)
                ReattachPrefabStagePanelComponents();
        }

        static void DetachPrefabStagePanelComponents()
        {
            foreach (var component in GetTopLevelPrefabStagePanelComponents())
            {
                var settings = component.panelSettings;
                if (settings == null || settings.renderMode == PanelRenderMode.WorldSpace || component.GetRootVisualElement()?.panel == null)
                    continue;

                component.DetachRootVisualElement();
            }
        }

        static void ReattachPrefabStagePanelComponents()
        {
            foreach (var component in GetTopLevelPrefabStagePanelComponents())
            {
                var root = component.GetRootVisualElement();
                if (root == null || root.panel != null || !component.GetComponentEnabled())
                    continue;

                component.ReattachRootVisualElement();
            }
        }

        // Prefab stages, including parents of a nested one, outlive play mode transitions without re-enabling their contents
        static IEnumerable<IPanelComponent> GetTopLevelPrefabStagePanelComponents()
        {
            foreach (var prefabStage in PrefabStage.allPrefabStages)
            {
                if (!prefabStage.isValid)
                    continue;

                foreach (var component in prefabStage.prefabContentsRoot.GetComponentsInChildren<IPanelComponent>())
                {
                    if (component.parentUI == null)
                        yield return component;
                }
            }
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
