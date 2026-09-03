// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

static class IntegratedAuthoringWorkflow
{
    [InitializeOnLoadMethod, UsedImplicitly]
    private static void RegisterStageHandlers()
    {
        StageNavigationManager.instance.stageChanging += OnStageWillChange;
    }

    [/*BeforeManagedObjectsDisabled,*/ UsedImplicitly]
    private static void UnregisterStageHandlers()
    {
        StageNavigationManager.instance.stageChanging -= OnStageWillChange;
    }

    private static void OnStageWillChange(Stage previousStage, Stage nextStage)
    {
        switch (nextStage)
        {
            case VisualElementEditingStage:
                var fromMainStage = previousStage is MainStage;
                MaybeOpenWindow<StyleSheetsWindow>(UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow, fromMainStage);
                MaybeOpenWindow<UIViewportWindow>(UIToolkitAuthoringSettings.AutoOpenUIViewportWindow, fromMainStage, typeof(SceneView));
                HierarchyWindow.RegisterNodeTypeHandler<VisualElementNodeHandler>();
                break;
            case MainStage:
                FocusWindow<SceneView>();
                HierarchyWindow.RegisterNodeTypeHandler<VisualElementNodeHandler>();
                break;
            default:
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

    static void FocusWindow<TWindow>()
        where TWindow : EditorWindow
    {
        if (!EditorWindow.HasOpenInstances<TWindow>())
            return;
        var window = EditorWindow.GetWindow<TWindow>();
        window.Focus();
    }
}
