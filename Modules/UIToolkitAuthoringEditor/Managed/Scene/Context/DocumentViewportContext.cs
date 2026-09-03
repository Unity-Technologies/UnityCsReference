// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A document shown outside the UI Stage — picked from the Main Stage Hierarchy, or handed to the window by
/// code. There is no stage to borrow a <see cref="PanelElement"/> from here, so this context creates one.
/// </summary>
/// <remarks>
/// The panel is an independent clone of the document, not the live instances a scene panel component renders.
/// Nothing selects, inspects or holds on to its elements, which is what makes rebuilding it wholesale cheap —
/// and why every edit reaches it as a re-clone rather than as a write onto its elements.
/// </remarks>
sealed class DocumentViewportContext : IUIViewportContext
{
    // Matches the size the UI Stage gives its own panel before the canvas sizes it from its settings.
    static readonly Vector2Int k_InitialPanelSize = new(480, 640);

    // Nothing writes to this clone, so every category that changes how a document looks has to be rebuilt from
    // the assets — Styling included, unlike for a scene panel. Save is in the set because writing a document
    // reimports it, which rebuilds the VisualElementAssets the current clone points at.
    const CommandCategory k_NeedsReclone =
        CommandCategory.Styling | CommandCategory.StylingContext | CommandCategory.Attributes |
        CommandCategory.Hierarchy | CommandCategory.Variables | CommandCategory.Save;

    // Both faces of the same object: an interface reference never turns into Unity's null, so aliveness has to
    // be asked of the Object one.
    readonly IPanelComponent m_PanelComponent;
    readonly Object m_PanelComponentObject;

    readonly VisualTreeAsset m_Document;
    readonly PanelSettings m_PanelSettings;

    PanelElement m_PanelElement;
    bool m_Acquired;
    bool m_ReclonePending;

    public DocumentViewportContext(IPanelComponent panelComponent)
    {
        m_PanelComponent = panelComponent;
        m_PanelComponentObject = panelComponent as Object;
        m_Document = panelComponent?.visualTreeAsset;
        m_PanelSettings = panelComponent?.panelSettings;
    }

    public DocumentViewportContext(VisualTreeAsset document, PanelSettings panelSettings)
    {
        m_Document = document;
        m_PanelSettings = panelSettings;
    }

    public object Source => (object)m_PanelComponentObject ?? m_Document;

    public PanelElement PanelElement => m_PanelElement;

    public VisualTreeAsset RootVisualTreeAsset => m_Document;

    public VisualTreeAsset EditedVisualTreeAsset => m_Document;

    public PanelSettings PanelSettings => m_PanelSettings;

    public string HeaderTitle => m_Document ? m_Document.name + ".uxml" : string.Empty;

    // Keyed exactly like Stage.GetHashForStateStorage keys the same document, so the framing carries over
    // between previewing it here and editing it in the stage.
    public string CanvasStorageKey
    {
        get
        {
            if (!m_Document)
                return null;

            var path = AssetDatabase.GetAssetPath(m_Document);
            var hash = string.IsNullOrEmpty(path) ? new Hash128() : Hash128.Compute(AssetDatabase.AssetPathToGUID(path));
            return $"CanvasSettings-{hash}";
        }
    }

    // A context built from a panel component dies with it; one built from a document only needs the document.
    public bool IsValid => m_Document != null && (ReferenceEquals(m_PanelComponent, null) || m_PanelComponentObject != null);

    public bool AllowsAuthoring => UIToolkitStageUtility.IsAuthoringEnabledInMainStage;

    public void Acquire()
    {
        if (m_Acquired)
            return;
        m_Acquired = true;

        m_PanelElement = new PanelElement();
        m_PanelElement.CreateSubPanel();
        Binding.SetPanelLogLevel(m_PanelElement.SubPanel, BindingLogLevel.None);
        m_PanelElement.SetPanelSize(k_InitialPanelSize);
        m_PanelElement.PanelSettings = m_PanelSettings;

        // Tracked read-only so the document is watched even when no scene panel renders it, which is what makes
        // the reimport and discard notifications below reach this preview.
        UIAssetRegistry.instance.AttachPanel(m_PanelElement.SubPanel, this, CollectRoots);

        UICommandQueue.RegisterHandlerForCategory(k_NeedsReclone, OnAuthoringCommandExecuted);
        UIAssetRegistry.instance.AssetReloaded += OnAssetReloaded;
        Undo.undoRedoPerformed += ScheduleReclone;

        CloneTree();
    }

    public void Release()
    {
        if (!m_Acquired)
            return;
        m_Acquired = false;
        m_ReclonePending = false;

        EditorApplication.delayCall -= FlushReclone;
        UICommandQueue.UnregisterHandlerForCategory(k_NeedsReclone, OnAuthoringCommandExecuted);
        Undo.undoRedoPerformed -= ScheduleReclone;

        var registry = UIAssetRegistry.LiveInstance;
        if (registry != null)
            registry.AssetReloaded -= OnAssetReloaded;

        if (m_PanelElement == null)
            return;

        if (registry != null)
            registry.DetachPanel(m_PanelElement.SubPanel);

        m_PanelElement.subRootVisualElement?.Clear();
        m_PanelElement.DestroyPanelPermanently();
        m_PanelElement = null;
    }

    public void RequestRefresh()
    {
        if (m_PanelElement == null)
            return;

        // Process whatever is already pending before cloning, so the new tree is built from an up-to-date panel.
        m_PanelElement.FrameUpdate();
        CloneTree();
    }

    public bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset)
    {
        // Nothing resolved means we cannot prove the drop is safe; refuse it.
        if (!m_Document || !visualTreeAsset)
            return true;

        using var _ = HashSetPool<string>.Get(out var visitedPaths);
        visitedPaths.Add(AssetDatabase.GetAssetPath(m_Document));
        return VisualElementEditingUtility.WillCauseCircularDependency(visualTreeAsset, visitedPaths);
    }

    // Neither crumb navigates: unlike the stage history, this is a location rather than a path walked into.
    public void PopulateBreadcrumbs(UIViewport viewport)
    {
        viewport.ClearBreadcrumbs();

        var gameObject = m_PanelComponentObject ? m_PanelComponent.gameObject : null;
        if (gameObject)
            viewport.PushBreadcrumb(gameObject.name, EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image as Texture2D);

        if (m_Document)
            viewport.PushBreadcrumb(m_Document.name, EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D);
    }

    void CollectRoots(List<VisualTreeAsset> roots)
    {
        if (m_Document)
            roots.Add(m_Document);
    }

    void CloneTree()
    {
        var root = m_PanelElement?.subRootVisualElement;
        if (root == null)
            return;

        root.Clear();
        if (m_Document)
            m_Document.CloneTree(root);

        // The clone decides which templates and style sheets the panel depends on.
        UIAssetRegistry.instance.RefreshPanel(m_PanelElement.SubPanel);
        m_PanelElement.FrameUpdate();
    }

    void OnAuthoringCommandExecuted(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        // Deliberately not filtered by the asset the command touched: a command carries no reliable one (the UI
        // Builder records none), and a pointless rebuild costs less than a missed change.
        ScheduleReclone();
    }

    void OnAssetReloaded(Object asset) => ScheduleReclone();

    void ScheduleReclone()
    {
        if (!m_Acquired)
            return;

        m_ReclonePending = true;

        // Coalesced through the pending flag rather than an "already scheduled" latch: a delayCall can be
        // dropped without ever running, and a latch would then swallow every later request.
        EditorApplication.delayCall += FlushReclone;
    }

    // Never run from the change that caused it: a re-clone releases every element of the preview while the
    // code that made the change is still running.
    void FlushReclone()
    {
        if (!m_ReclonePending)
            return;
        m_ReclonePending = false;

        if (!m_Acquired)
            return;

        CloneTree();
    }
}
