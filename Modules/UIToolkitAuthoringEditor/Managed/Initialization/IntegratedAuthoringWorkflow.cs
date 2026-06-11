// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

static class IntegratedAuthoringWorkflow
{
    internal static void OnStageChanged(Stage previous, Stage current)
    {
        switch (current)
        {
            case VisualElementEditingStage:
                var fromMainStage = previous is MainStage;
                MaybeOpenWindow<StyleSheetsWindow>(UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow, fromMainStage);
                MaybeOpenWindow<UIViewportWindow>(UIToolkitAuthoringSettings.AutoOpenUIViewportWindow, fromMainStage, typeof(SceneView));
                break;
            case MainStage:
                FocusWindow<SceneView>();
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
