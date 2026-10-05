// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

static class IntegratedAuthoringWorkflow
{
    [InitializeOnLoadMethod, UsedImplicitly]
    private static void RegisterStageHandlers()
    {
        UIStageNavigation.StageChanging += OnStageWillChange;
    }

    [/*BeforeManagedObjectsDisabled,*/ UsedImplicitly]
    private static void UnregisterStageHandlers()
    {
        UIStageNavigation.StageChanging -= OnStageWillChange;
    }

    private static void OnStageWillChange(Stage previousStage, Stage nextStage)
    {
        switch (nextStage)
        {
            case VisualElementEditingStage:
                var fromMainStage = previousStage is MainStage;
                if (fromMainStage)
                    SaveFrontTabInSceneViewDock();
                MaybeOpenWindow<StyleSheetsWindow>(UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow, fromMainStage);
                MaybeOpenWindow<UIViewportWindow>(UIToolkitAuthoringSettings.AutoOpenUIViewportWindow, fromMainStage, typeof(SceneView));
                HierarchyWindow.RegisterNodeTypeHandler<VisualElementNodeHandler>();
                break;
            case MainStage:
                RestorePreviousFrontTab();
                HierarchyWindow.RegisterNodeTypeHandler<VisualElementNodeHandler>();
                break;
            default:
                UIStageFocusState.instance.Clear();
                HierarchyWindow.UnregisterNodeTypeHandler<VisualElementNodeHandler>();
                break;
        }
    }

    static void MaybeOpenWindow<TWindow>(AutoOpenMode mode, bool fromMainStage, params Type[] desiredDockNextTo)
        where TWindow : EditorWindow
    {
        switch (mode)
        {
            case AutoOpenMode.Never:
            case AutoOpenMode.FromMainStage when !fromMainStage:
                return;
        }

        EditorWindow.GetWindow<TWindow>(null, true, desiredDockNextTo);
    }

    static void SaveFrontTabInSceneViewDock()
    {
        var state = UIStageFocusState.instance;
        state.Clear();
        var sceneView = GetActiveSceneView();
        if (sceneView != null)
            state.Set(sceneView.m_Parent.actualView);
    }

    static void RestorePreviousFrontTab()
    {
        var state = UIStageFocusState.instance;
        var previousFrontTab = state.PreviousFrontTab;
        state.Clear();

        if (UIToolkitAuthoringSettings.AutoOpenUIViewportWindow == AutoOpenMode.Never)
            return;

        if (previousFrontTab != null && previousFrontTab.m_Parent != null)
        {
            if (previousFrontTab.m_Parent.actualView is UIViewportWindow)
                previousFrontTab.ShowTab();
            return;
        }

        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            if (sceneView != null && sceneView.m_Parent != null && sceneView.m_Parent.actualView is UIViewportWindow)
            {
                sceneView.Focus();
                return;
            }
        }
    }

    static SceneView GetActiveSceneView()
    {
        var lastActive = SceneView.lastActiveSceneView;
        if (lastActive != null && lastActive.m_Parent != null)
            return lastActive;
        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            if (sceneView != null && sceneView.m_Parent != null)
                return sceneView;
        }
        return null;
    }
}

// HideAndDontSave so the recorded windows survive the domain reloads a stage session can span.
sealed partial class UIStageFocusState : ScriptableObject
{
    // Cleared on reload; OnEnable and the getter re-bind to the surviving instance.
    [AutoStaticsCleanupOnCodeReload]
    static UIStageFocusState s_Instance;

    [SerializeField] EditorWindow m_PreviousFrontTab;

    public static UIStageFocusState instance
    {
        get
        {
            if (s_Instance == null)
            {
                var found = Resources.FindObjectsOfTypeAll<UIStageFocusState>();
                s_Instance = found.Length > 0 ? found[0] : CreateInstance<UIStageFocusState>();
            }
            return s_Instance;
        }
    }

    public EditorWindow PreviousFrontTab => m_PreviousFrontTab;

    public void Set(EditorWindow previousFrontTab)
    {
        m_PreviousFrontTab = previousFrontTab;
    }

    public void Clear()
    {
        Set(null);
    }

    void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
        s_Instance = this;
    }
}
