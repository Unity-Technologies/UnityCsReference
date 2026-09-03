// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Prompts the user to save or discard Main Stage UI edits before the editor quits or a scene closes.
//
// Scene UI assets are only writable while Main Stage authoring is on, and nothing else asks about them: the
// UI Builder and the UI Stage only prompt for the documents they hold themselves, and the editor's own
// "Scene(s) Have Been Modified" dialog only knows about scene dirtiness — a .uxml edited through the Main
// Stage leaves the scene file itself untouched.
static partial class UIAssetRegistrySceneTracking
{
    static void SubscribeToClosePrompts()
    {
        EditorApplication.wantsToQuit += OnWantsToQuit;
        EditorSceneManager.sceneClosing += OnSceneClosing;
    }

    static void UnsubscribeFromClosePrompts()
    {
        EditorApplication.wantsToQuit -= OnWantsToQuit;
        EditorSceneManager.sceneClosing -= OnSceneClosing;
    }

    static bool OnWantsToQuit()
    {
        using var _ = ListPool<UnityEngine.Object>.Get(out var dirty);
        CollectDirtyScenePanelAssets(dirty);

        return UIAssetSavePrompt.AskAndResolve(dirty,
            L10n.Tr("Your changes will be lost if you don't save them.", null), allowCancel: true);
    }

    static void OnSceneClosing(Scene scene, bool removingScene)
    {
        // Entering and leaving play mode tears scenes down and rebuilds them; those edits are not being
        // abandoned by the user and the registry re-baselines them itself on the way back to edit mode.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        using var _ = ListPool<UnityEngine.Object>.Get(out var dirty);
        CollectDirtyAssetsLeavingWithScene(scene, dirty);

        UIAssetSavePrompt.AskAndResolve(dirty,
            L10n.Tr("The scene using them is closing. Your changes will be lost if you don't save them.", null),
            allowCancel: false);
    }

    // The dirty Main Stage assets this prompt is responsible for: the ones no tool other than the scene panels
    // holds for writing. Internal so a test can assert on the prompt's input without driving a modal.
    internal static void CollectDirtyScenePanelAssets(List<UnityEngine.Object> results)
    {
        var registry = UIAssetRegistry.LiveInstance;
        if (registry == null || !UIToolkitStageUtility.IsAuthoringEnabledInMainStage)
            return;

        registry.CollectDirtyAssetsHeldOnlyBy(IsTrackedScenePanel, results);
    }

    // Narrows the Main Stage's dirty assets to the ones the closing scene is the last user of. A panel is
    // shared by every component using the same PanelSettings, so it can straddle scenes: an asset still
    // reachable from a component in a scene that stays open is not going anywhere and must not be prompted
    // for. Runs from sceneClosing, while the closing scene's components are still alive to be walked.
    static void CollectDirtyAssetsLeavingWithScene(Scene scene, List<UnityEngine.Object> results)
    {
        var selection = VisualElementSelectionRegistry.Instance;
        if (selection == null)
            return;

        using var _ = ListPool<UnityEngine.Object>.Get(out var candidates);
        CollectDirtyScenePanelAssets(candidates);
        if (candidates.Count == 0)
            return;

        using var __ = HashSetPool<UnityEngine.Object>.Get(out var leaving);
        using var ___ = HashSetPool<UnityEngine.Object>.Get(out var staying);
        var panels = selection.TrackedScenePanels;
        for (var i = 0; i < panels.Count; i++)
        {
            PanelDependencyTracker.CollectPanelComponentDependencies(panels[i],
                component => TryGetComponentScene(component, out var owning) && owning == scene, leaving);
            PanelDependencyTracker.CollectPanelComponentDependencies(panels[i],
                component => TryGetComponentScene(component, out var owning) && owning != scene, staying);
        }

        // A component we could not place counts as neither, so its assets stay out of the prompt: silently
        // keeping unsaved changes is recoverable, wrongly discarding them is not.
        foreach (var asset in candidates)
            if (leaving.Contains(asset) && !staying.Contains(asset))
                results.Add(asset);
    }

    static bool TryGetComponentScene(IPanelComponent component, out Scene scene)
    {
        // Deliberately null-checked through UnityEngine.Object: a destroyed component still leaves a live
        // interface reference behind (fake-null does not survive the cast to IPanelComponent), and reading its
        // gameObject would throw.
        if (component is UnityEngine.Object obj && obj != null)
        {
            scene = component.gameObject.scene;
            return true;
        }

        scene = default;
        return false;
    }

    static bool IsTrackedScenePanel(object owner)
    {
        if (owner is not Panel panel)
            return false;

        var selection = VisualElementSelectionRegistry.Instance;
        if (selection == null)
            return false;

        var tracked = selection.TrackedScenePanels;
        for (var i = 0; i < tracked.Count; i++)
            if (ReferenceEquals(tracked[i], panel))
                return true;

        return false;
    }
}
