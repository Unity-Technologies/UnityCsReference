// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

// Alt+F frames the current UI selection and rotates the SceneView to face the panel. Registered as a
// tool context so the shortcut only fires from UI-aware views with a UI selection — the plain F
// binding stays reserved for stock Frame Selected.
static partial class FrameAndFaceShortcut
{
    [AutoStaticsCleanupOnCodeReload]
    static ShortcutContext s_Context;

    [OnCodeLoaded, UsedImplicitly]
    static void Register()
    {
        s_Context = new ShortcutContext();
        EditorApplication.delayCall += () =>
            ShortcutIntegration.instance.contextManager.RegisterToolContext(s_Context);
    }

    class ShortcutContext : IShortcutContext
    {
        public bool active =>
            EditorWindow.focusedWindow is SceneView or HierarchyWindow or UIViewportWindow &&
            Selection.activeObject is UISelectionObject &&
            StageUtility.GetCurrentStage() is MainStage or VisualElementEditingStage;
    }

    [Shortcut("UI Toolkit Authoring/Frame and Align Element to View", typeof(ShortcutContext), KeyCode.F, ShortcutModifiers.Alt)]
    static void OnShortcut(ShortcutArguments args)
        => RequestFramingCommand.Execute(CommandSources.Hierarchy, element: null, orientToFace: true);
}
