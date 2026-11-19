// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The Style Sheets window of the in-scene workflow: a <see cref="StyleSheetsView"/> on the global context, showing
/// the UI Stage's document, or outside it the document of the selected panel component.
/// </summary>
internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;

    StyleSheetsView m_View;

    [NonSerialized]
    VisualElementEditingStage m_EditingStage;

    // Persisted so the last selection-driven document survives a domain reload, keeping the "last selected
    // context" sticky behaviour used outside the UI Stage.
    [SerializeField]
    VisualTreeAsset m_LastSelectedDocument;

    // The panel component driving the current selection-driven display. Tracked (not serialized) so the window
    // reacts to it being deleted or re-targeted. Null when the display comes from a UI Stage or was restored
    // from the serialized sticky document after a domain reload.
    [NonSerialized]
    IPanelComponent m_SelectedPanelComponent;

    // The window is a view of the in-scene workflow, which runs on the global context.
    readonly AuthoringContext m_AuthoringContext = AuthoringContext.Global;

    // Used in tests
    internal StyleSheetsView View => m_View;
    internal Unity.Hierarchy.Hierarchy Hierarchy => m_View?.SourceHierarchy;
    internal HierarchyView HierarchyView => m_View?.HierarchyView;
    internal StyleSheetNodeTypeHandler Handler => m_View?.Handler;
    internal HierarchyGlobalSelectionHandler GlobalSelectionHandler => m_View?.GlobalSelectionHandler;
    internal int ParentStyleSheetCount => m_View?.ParentStyleSheetCount ?? 0;
    internal VisualTreeAsset EditedAsset => m_View?.EditedAsset;

    /// <summary>The document whose stylesheets are currently displayed (editable or read-only).</summary>
    internal VisualTreeAsset DisplayedDocument => m_View?.DisplayedDocument;

    /// <summary>True when the displayed document cannot be edited from this window.</summary>
    internal bool IsReadOnly => m_View?.IsReadOnly ?? false;

    public StyleSheet ActiveStyleSheet => m_View?.ActiveStyleSheet;

    [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 4)]
    static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";
        titleContent.image = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;

        UIStageNavigation.StageSettled += OnStageChanged;
        UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
        m_AuthoringContext.Selection.Changed += RefreshSelectionContext;
    }

    void OnDisable()
    {
        UIStageNavigation.StageSettled -= OnStageChanged;
        m_AuthoringContext.Selection.Changed -= RefreshSelectionContext;

        var registry = UIAssetRegistry.LiveInstance;
        if (registry != null)
            registry.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        // A disabled window has to stop answering the authoring queries; the view does that as it goes.
        if (m_View != null)
        {
            m_View.DisplayChanged -= RefreshUnsavedChangesState;
            m_View.StyleSheetSettled -= RefreshUnsavedChangesState;
            m_View.Dispose();
            m_View = null;
        }
    }

    void CreateGUI()
    {
        m_View = new StyleSheetsView(m_AuthoringContext, handlesPanelCommands: true)
        {
            SettleStyleSheetChangesBeforeRemoval = SettleStyleSheetChangesBeforeRemoval,
        };
        m_View.DisplayChanged += RefreshUnsavedChangesState;
        m_View.StyleSheetSettled += RefreshUnsavedChangesState;
        m_View.RegisterCallback<PointerDownEvent>(OnViewPointerDown, TrickleDown.TrickleDown);
        rootVisualElement.Add(m_View);

        OnStageChanged(StageUtility.GetCurrentStage());
    }

    /// <summary>
    /// The style sheets on display whose unsaved changes this window answers for. Empty outside the Main Stage:
    /// a UI Stage prompts for its own document when it is left, and with authoring off the rows are read-only,
    /// so whatever is unsaved in them belongs to the tool that changed it.
    /// </summary>
    /// <remarks>
    /// Rebuilt from the document rather than read off the rows — it is the same set they are built from, and it
    /// still answers once the hierarchy is torn down.
    /// </remarks>
    void CollectUnsavedStyleSheets(List<StyleSheet> results)
    {
        if (!UIToolkitStageUtility.IsAuthoringActiveInMainStage)
            return;

        var registry = UIAssetRegistry.LiveInstance;
        var document = DisplayedDocument;
        if (registry == null || document == null)
            return;

        using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
        document.GetAllReferencedStyleSheets(styleSheets);
        foreach (var styleSheet in styleSheets)
        {
            if (styleSheet != null && registry.IsDirty(styleSheet))
                results.Add(styleSheet);
        }
    }

    /// <summary>
    /// Keeps the editor's own unsaved-changes state in step with the registry, so closing the window (or the
    /// tab, or the container) raises the standard Save/Discard/Cancel prompt over
    /// <see cref="SaveChanges"/>/<see cref="DiscardChanges"/>, and the tab shows the same "*" its rows do.
    /// </summary>
    void RefreshUnsavedChangesState()
    {
        using var _ = ListPool<StyleSheet>.Get(out var unsaved);
        CollectUnsavedStyleSheets(unsaved);

        // Only rebuilt while there is something to report: the message is read when the prompt opens, which is
        // always while hasUnsavedChanges holds.
        if (unsaved.Count > 0)
            saveChangesMessage = UIAssetSavePrompt.BuildMessage(unsaved,
                L10n.Tr("Your changes will be lost if you don't save them.", null));

        hasUnsavedChanges = unsaved.Count > 0;
    }

    public override void SaveChanges()
    {
        using var _ = ListPool<StyleSheet>.Get(out var unsaved);
        CollectUnsavedStyleSheets(unsaved);

        var registry = UIAssetRegistry.LiveInstance;

        // Batch the imports so saving an imported sheet doesn't reimport its dirty importers first.
        registry?.SaveAssets(unsaved, CommandSources.StyleSheets);

        // Deliberately re-derived instead of base.SaveChanges(): a write that did not land — a read-only or
        // unchecked-out file — leaves the sheet dirty, and the flag staying set is what aborts the close.
        RefreshUnsavedChangesState();
    }

    public override void DiscardChanges()
    {
        using var _ = ListPool<StyleSheet>.Get(out var unsaved);
        CollectUnsavedStyleSheets(unsaved);

        var registry = UIAssetRegistry.LiveInstance;
        foreach (var styleSheet in unsaved)
            registry?.DiscardAsset(styleSheet, CommandSources.StyleSheets);

        RefreshUnsavedChangesState();
    }

    // A UI Stage settles its whole document; outside it, the style sheet alone.
    bool SettleStyleSheetChangesBeforeRemoval(StyleSheet styleSheet)
        => m_EditingStage != null ? m_EditingStage.AskUserToSaveModifiedStage() : StyleSheetsView.SettleChangesOf(styleSheet);

    // Place the focus on the window before the context menu opens, otherwise interactions like the text field will
    // not work.
    void OnViewPointerDown(PointerDownEvent evt)
    {
        if (HierarchyViewUtility.IsContextClick((MouseButton)evt.button, evt.modifiers))
            Focus();
    }

    void OnStageChanged(Stage newStage)
    {
        if (m_View == null)
            return;

        if (newStage is VisualElementEditingStage editingStage)
        {
            m_EditingStage = editingStage;
            m_View.ShowSession(editingStage.GetOrCreateSession());
            return;
        }

        // Outside the UI Stage the window is driven by the current selection. If it maps to a panel
        // component we display that document; otherwise we keep the last one (sticky) or, failing that,
        // show the contextual message.
        m_EditingStage = null;

        if (!TryApplySelectionContext() && !TryApplyStickyContext())
            ClearContext();

        // Which stage is displayed decides whether this window answers for the sheets at all, so it is
        // re-evaluated even when the display itself did not have to be rebuilt.
        RefreshUnsavedChangesState();
    }

    /// <summary>
    /// Re-evaluates the current global selection while outside the UI Stage. When the selection resolves to a
    /// <see cref="PanelRenderer"/> or <see cref="UIDocument"/> with a document, that document is displayed;
    /// any other selection is ignored, so the displayed content stays sticky.
    /// </summary>
    internal void RefreshSelectionContext()
    {
        // Inside the UI Stage the displayed data comes from the stage, not the selection.
        if (m_EditingStage != null || m_View == null)
            return;

        TryApplySelectionContext();
    }

    bool TryApplySelectionContext()
    {
        var component = ResolvePanelComponentFromSelection();
        var document = component?.visualTreeAsset;
        if (document == null)
            return false;

        m_SelectedPanelComponent = component;
        m_LastSelectedDocument = document;
        ApplySelectionContext(document);
        return true;
    }

    bool TryApplyStickyContext()
    {
        if (m_LastSelectedDocument == null)
            return false;

        ApplySelectionContext(m_LastSelectedDocument);
        return true;
    }

    // Displays a document resolved from the current selection. The view only rebuilds when what it shows
    // actually changes, so re-evaluating the same selection leaves the hierarchy state alone.
    void ApplySelectionContext(VisualTreeAsset document)
        => m_View.ShowDocument(StyleSheetsContextFactory.FromSelectedDocument(document));

    void ClearContext() => m_View.ClearDisplay();

    static IPanelComponent ResolvePanelComponentFromSelection()
    {
        var gameObject = Selection.activeGameObject;
        if (gameObject != null)
        {
            // Use TryGetComponent: casting a missing component (a Unity "fake null") to IPanelComponent would
            // produce a non-null managed reference and throw a MissingComponentException on first access.
            if (gameObject.TryGetComponent<PanelRenderer>(out var panelRenderer))
                return panelRenderer;
            if (gameObject.TryGetComponent<UIDocument>(out var uiDocument))
                return uiDocument;
        }

        // Selecting a VisualElement from a panel component's in-scene hierarchy behaves like selecting the
        // component itself: walk up to the owning PanelRenderer/UIDocument.
        if (Selection.activeObject is VisualElementSelection { Element: not null } elementSelection)
        {
            var panelComponent = elementSelection.Element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;
            if (panelComponent as UnityEngine.Object != null)
                return panelComponent;
        }

        // Selecting a VisualTreeAsset from a panel component's in-scene hierarchy behaves like selecting the
        // component itself: walk up to the owning PanelRenderer/UIDocument.
        if (Selection.activeObject is VisualTreeAssetSelection { PanelComponent: not null } visualTreeAssetSelection)
        {
            var panelComponent = visualTreeAssetSelection.PanelComponent;
            if (panelComponent as UnityEngine.Object != null)
                return panelComponent;
        }

        return null;
    }

    // A style sheet this window is responsible for was dirtied or settled — by an edit made here, or by any
    // other tool sharing it.
    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        if (asset is StyleSheet)
            RefreshUnsavedChangesState();
    }

    internal void Update()
    {
        m_View?.Update();

        // Keep the selection-driven display in sync with its source panel component: a PanelRenderer/UIDocument
        // can be deleted or re-targeted to a different document without the global selection changing. This
        // poll only runs from the editor tick, never re-entrantly from the internal pumps made while rebuilding
        // the list.
        SyncSelectionContextWithSource();
    }

    /// <summary>
    /// While displaying a selection-driven document, tracks the source panel component so the window reacts to
    /// it being deleted or having its <c>visualTreeAsset</c> reassigned.
    /// </summary>
    internal void SyncSelectionContextWithSource()
    {
        if (m_EditingStage != null || m_View == null)
            return;

        // A C# null means we have no live reference to track (e.g. restored from the serialized sticky
        // document after a domain reload). Leave the display alone in that case.
        if (m_SelectedPanelComponent == null)
            return;

        // The interface reference can still wrap a destroyed Unity object; a Unity null-check detects that
        // the component (or its GameObject) was deleted. Stop tracking and re-evaluate the selection.
        if (m_SelectedPanelComponent as UnityEngine.Object == null)
        {
            m_SelectedPanelComponent = null;
            m_LastSelectedDocument = null;
            if (!TryApplySelectionContext())
                ClearContext();
            return;
        }

        // Keep the display in sync with the tracked component's current document. The component stays tracked
        // even when its document is cleared, so restoring it (e.g. via undo) brings the display back.
        var currentDocument = m_SelectedPanelComponent.visualTreeAsset;
        if (currentDocument == DisplayedDocument)
            return;

        m_LastSelectedDocument = currentDocument;
        if (currentDocument == null)
            ClearContext();
        else
            ApplySelectionContext(currentDocument);
    }

    void OnProjectChange() => m_View?.RefreshAfterProjectChange();

    internal void FocusNewSelectorField(string prefillText) => m_View?.FocusNewSelectorField(prefillText);

    public void SetActiveStyleSheet(HierarchyNode hierarchyNode) => m_View?.SetActiveStyleSheet(hierarchyNode);

    public void CreateStyleSheet() => m_View?.CreateStyleSheet();

    public void AddStyleSheet() => m_View?.AddStyleSheet();

    public bool CanSettleStyleSheet(HierarchyNode hierarchyNode) => m_View?.CanSettleStyleSheet(hierarchyNode) ?? false;

    public void SaveStyleSheet(HierarchyNode hierarchyNode) => m_View?.SaveStyleSheet(hierarchyNode);

    public void DiscardStyleSheet(HierarchyNode hierarchyNode) => m_View?.DiscardStyleSheet(hierarchyNode);

    public void RemoveStyleSheet(HierarchyNode hierarchyNode) => m_View?.RemoveStyleSheet(hierarchyNode);

    public void SelectAndScrollTo(IReadOnlyList<StyleRule> targetRules) => m_View?.SelectAndScrollTo(targetRules);
}
