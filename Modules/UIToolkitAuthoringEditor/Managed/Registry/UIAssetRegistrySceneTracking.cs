// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Bridges the scene panels tracked by <see cref="VisualElementSelectionRegistry"/> into
/// <see cref="UIAssetRegistry"/> as open assets, so the registry knows about every
/// <see cref="VisualTreeAsset"/> / <see cref="StyleSheet"/> present in the loaded scenes (MainStage), not
/// just the top-level one. Assets are opened read-write (so Main Stage edits are write-tracked, prompt to
/// save, and survive play-mode entry), and a project/scene save while in the Main Stage writes the dirty ones.
/// </summary>
/// <remarks>
/// <see cref="VisualElementSelectionRegistry"/> raises <c>PanelTracked</c>/<c>PanelUntracked</c> only for
/// scene panels; stage panels manage their own registry tracking. The subscription is deferred until that
/// registry has bootstrapped.
/// </remarks>
static partial class UIAssetRegistrySceneTracking
{
    [OnCodeLoaded]
    static void StartInit()
    {
        EditorApplication.fileMenuSaved += OnFileMenuSaved;
        SubscribeToClosePrompts();
        SubscribeToLiveReload();

        EditorApplication.delayCall += Init;
    }

    static void Init()
    {
        // Ensure the registry is alive so it reconciles external changes even before any tool opens an asset.
        _ = UIAssetRegistry.instance;
        SubscribeToAssetReloads();

        var selection = VisualElementSelectionRegistry.Instance;
        if (selection == null)
        {
            // The selection registry has not bootstrapped yet; try again next tick.
            EditorApplication.delayCall += Init;
            return;
        }

        selection.PanelTracked += OnPanelTracked;
        selection.PanelUntracked += OnPanelUntracked;

        foreach (var panel in selection.TrackedScenePanels)
            OnPanelTracked(panel);
    }

    [OnCodeUnloading]
    static void Cleanup()
    {
        EditorApplication.fileMenuSaved -= OnFileMenuSaved;
        UnsubscribeFromClosePrompts();
        UnsubscribeFromLiveReload();

        var selection = VisualElementSelectionRegistry.Instance;
        if (selection == null)
            return;
        selection.PanelTracked -= OnPanelTracked;
        selection.PanelUntracked -= OnPanelUntracked;
    }

    static void OnPanelTracked(Panel panel)
    {
        if (panel == null)
            return;

        UIAssetRegistry.instance.AttachPanel(
            panel,
            panel,
            roots => PanelDependencyTracker.CollectPanelComponentRoots(panel, roots),
            ResolveSceneAssetAccess);

        ApplyLiveReloadPolicy(panel);
    }

    static void OnPanelUntracked(Panel panel)
    {
        if (panel == null)
            return;

        RestoreLiveReload(panel);
        UIAssetRegistry.instance.DetachPanel(panel);
    }

    // Any element with a VisualElementAsset is editable in the Main Stage (including inside nested templates),
    // so every tracked document and stylesheet can be dirtied.
    static UIAssetAccess ResolveSceneAssetAccess(UnityEngine.Object asset)
        => asset is not ThemeStyleSheet ? UIAssetAccess.ReadWrite : UIAssetAccess.ReadOnly;

    // Saving the scene/project (Ctrl+S or File > Save) while in the Main Stage also writes the dirty UI
    // assets. The UI Builder saves its own documents through its own fileMenuSaved hook; an overlapping save
    // converges to clean and is a harmless no-op.
    static void OnFileMenuSaved()
    {
        if (UIToolkitStageUtility.IsAuthoringActiveInMainStage)
            UIAssetRegistry.instance.SaveAll();
    }
}
