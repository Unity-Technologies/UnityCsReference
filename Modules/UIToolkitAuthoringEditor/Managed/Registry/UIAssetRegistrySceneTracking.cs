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
/// just the top-level one. Assets are opened read-only by default; when
/// <see cref="UIToolkitAuthoringSettings.EnableMainStageAuthoring"/> is on they are opened read-write (so
/// Main Stage edits are write-tracked, prompt to save, and survive play-mode entry), and a project/scene
/// save while in the Main Stage writes the dirty ones.
/// </summary>
/// <remarks>
/// <see cref="VisualElementSelectionRegistry"/> raises <c>PanelTracked</c>/<c>PanelUntracked</c> only for
/// scene panels (gated by <see cref="UIToolkitAuthoringSettings.EnableInSceneUIAuthoring"/>); stage panels
/// manage their own registry tracking. The subscription is deferred until that registry has bootstrapped.
/// </remarks>
static partial class UIAssetRegistrySceneTracking
{
    [OnCodeLoaded]
    static void StartInit()
    {
        UIToolkitAuthoringSettings.MainStageAuthoringChanged += OnMainStageAuthoringChanged;
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
        UIToolkitAuthoringSettings.MainStageAuthoringChanged -= OnMainStageAuthoringChanged;
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

    // Scene assets become writable only while Main Stage authoring is enabled. Any element with a
    // VisualElementAsset is editable in the Main Stage (including inside nested templates), so every tracked
    // document and stylesheet can be dirtied.
    static UIAssetAccess ResolveSceneAssetAccess(UnityEngine.Object asset)
    {
        var writable = UIToolkitStageUtility.IsAuthoringEnabledInMainStage && asset is not ThemeStyleSheet;
        return writable ? UIAssetAccess.ReadWrite : UIAssetAccess.ReadOnly;
    }

    static void OnMainStageAuthoringChanged(bool enabled)
    {
        var selection = VisualElementSelectionRegistry.Instance;
        if (selection == null)
            return;

        var registry = UIAssetRegistry.LiveInstance;
        foreach (var panel in selection.TrackedScenePanels)
        {
            // The access of the panel's assets follows the setting, so re-resolve it before suspending or
            // restoring live reload, which is what the newly (non-)authorable panel needs next.
            registry?.RefreshPanel(panel);
            ApplyLiveReloadPolicy(panel);
        }
    }

    // Saving the scene/project (Ctrl+S or File > Save) while in the Main Stage also writes the dirty UI
    // assets. The UI Builder saves its own documents through its own fileMenuSaved hook; an overlapping save
    // converges to clean and is a harmless no-op.
    static void OnFileMenuSaved()
    {
        if (UIToolkitStageUtility.IsAuthoringActiveInMainStage)
            UIAssetRegistry.instance.SaveAll();
    }
}
