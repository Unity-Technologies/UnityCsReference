// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
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

internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;
    const string k_StyleSheetDark = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowLight.uss";
    const string k_EmptyLabelSeparatorUssClassName = "unity-style-sheets-window__empty-label--separator";
    static readonly string k_EditableEmptyLabel = L10n.Tr("Click the + icon to create a new StyleSheet.", null);
    static readonly string k_ReadOnlyEmptyLabel = L10n.Tr("The selected UI Document has no style sheets.", null);

    Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetEditingNodeTypeHandler m_Handler;
    Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();
    Dictionary<(VisualTreeAsset owningDocument, StyleSheet styleSheet), HierarchyNode> m_ParentStyleSheetNodes = new();
    HierarchyNode m_GroupNode = HierarchyNode.Null;
    ILiveReloadSystem m_LiveReloadSystem;
    VisualTreeAsset m_TrackedVTA;
    StyleSheetAssetTracker m_StyleSheetTracker;
    VisualTreeAssetTracker m_VisualTreeAssetTracker;
    HierarchyGlobalSelectionHandler m_GlobalSelectionHandler;
    Label m_EmptyLabelElement;
    VisualElement m_NoResultsLabelElement;
    VisualElement m_StagingModeContainerElement;
    VisualElement m_ToolbarElement;
    Button m_OpenSettingsButton;
    VisualElement m_ContainerElement;

    [NonSerialized]
    StyleSheetsContext m_Context = StyleSheetsContext.None;

    [NonSerialized]
    VisualElementEditingStage m_EditingStage;

    [NonSerialized]
    VisualTreeAssetEditingContext m_LastSource;

    // Persisted so the last selection-driven document survives a domain reload, keeping the "last selected
    // context" sticky behaviour used outside the UI Stage.
    [SerializeField]
    VisualTreeAsset m_LastSelectedDocument;

    // The panel component driving the current selection-driven display. Tracked (not serialized) so the window
    // reacts to it being deleted or re-targeted. Null when the display comes from a UI Stage or was restored
    // from the serialized sticky document after a domain reload.
    [NonSerialized]
    IPanelComponent m_SelectedPanelComponent;

    // Whether the queries only an authoring window can answer are currently routed here (see
    // UpdateEditingCommandHandlers).
    [NonSerialized]
    bool m_EditingCommandHandlersRegistered;

    [SerializeReference]
    List<StyleSheet> m_LastStyleSheets = new();

    // Used in tests
    internal Hierarchy.Hierarchy Hierarchy => m_Hierarchy;
    internal HierarchyView HierarchyView => m_HierarchyView;
    internal StyleSheetNodeTypeHandler Handler => m_Handler;
    internal HierarchyGlobalSelectionHandler GlobalSelectionHandler => m_GlobalSelectionHandler;
    internal int ParentStyleSheetCount => m_ParentStyleSheetNodes.Count;
    internal VisualTreeAsset EditedAsset => m_Context.EditedAsset;

    /// <summary>The document whose stylesheets are currently displayed (editable or read-only).</summary>
    internal VisualTreeAsset DisplayedDocument => m_Context.Document;

    /// <summary>True when the displayed document cannot be edited from this window.</summary>
    internal bool IsReadOnly => m_Context.IsReadOnly;

    [SerializeField]
    StyleSheet m_ActiveStyleSheet;

    [SerializeField]
    readonly Dictionary<VisualTreeAsset, StyleSheet> m_ActiveStyleSheetInVisualTreeAsset = new();

    public StyleSheet ActiveStyleSheet
    {
        get => m_ActiveStyleSheet;
        private set
        {
            if (m_ActiveStyleSheet == value)
                return;
            m_ActiveStyleSheet = value;
            ActiveStyleSheetChangedMessage.Execute(CommandSources.StyleSheets, m_ActiveStyleSheet);
        }
    }

    [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 4)]
    static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";
        titleContent.image = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;

        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += OnEnableInSceneAuthoringChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged += OnMainStageAuthoringChanged;
        UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
        Selection.selectionChanged += RefreshSelectionContext;

        m_VisualTreeAssetTracker = new VisualTreeAssetTracker(OnVisualTreeAssetChanged);
        m_StyleSheetTracker = new StyleSheetAssetTracker(OnStyleSheetChanged);

        rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnDisable()
    {
        rootVisualElement?.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        rootVisualElement?.UnregisterCallback<KeyUpEvent>(OnKeyUp);
        m_HierarchyView?.ListView.UnregisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);
        m_HierarchyView?.ListView.UnregisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed,
            TrickleDown.TrickleDown);
        rootVisualElement?.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement?.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        RemoveAssetTrackers();
        DisposeHierarchyResources();
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= OnEnableInSceneAuthoringChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged -= OnMainStageAuthoringChanged;
        Selection.selectionChanged -= RefreshSelectionContext;

        var registry = UIAssetRegistry.LiveInstance;
        if (registry != null)
            registry.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        // A disabled window has to stop answering the authoring queries. ApplyContext is what normally keeps
        // that in step with the displayed document, and it does not run on the way out. The context itself is
        // deliberately left alone.
        SetEditingCommandHandlers(false);

        m_LiveReloadSystem = null;
        m_ActiveStyleSheet = null;
        m_ActiveStyleSheetInVisualTreeAsset.Clear();
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
        var document = m_Context.Document;
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
        foreach (var styleSheet in unsaved)
            registry?.SaveAsset(styleSheet, CommandSources.StyleSheets);

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

    void DisposeHierarchyResources()
    {
        m_HierarchyView?.Dispose();
        m_Hierarchy?.Dispose();
        m_GlobalSelectionHandler?.Dispose();
        m_Hierarchy = null;
        m_HierarchyView = null;
        m_Handler = null;
        m_GlobalSelectionHandler = null;
        m_StyleSheetNodes.Clear();
        m_ParentStyleSheetNodes.Clear();
        m_GroupNode = HierarchyNode.Null;
    }

    void CreateGUI()
    {
        PrepareUI();
        m_LiveReloadSystem ??= ((Panel)rootVisualElement.panel).liveReloadSystem;

        // This redundant call is required since on Editor load, the visual tree is not available - therefore the
        // keyboard callback fails to register.
        var visualTree = rootVisualElement.panel?.visualTree;
        if (visualTree != null)
        {
            visualTree.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            visualTree.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            visualTree.RegisterCallback<KeyDownEvent>(OnDelete);
        }

        OnStageChanged(StageUtility.GetCurrentStage());
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        var panelLiveReloadSystem = ((Panel)rootVisualElement.panel).liveReloadSystem;
        // If we are docking or undocking, the panel will be different.
        if (m_LiveReloadSystem != null && m_LiveReloadSystem != panelLiveReloadSystem)
            m_LiveReloadSystem = panelLiveReloadSystem;

        evt.destinationPanel.visualTree.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        evt.destinationPanel.visualTree.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        evt.destinationPanel.visualTree.RegisterCallback<KeyDownEvent>(OnDelete);

        UpdateAssetTrackers();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        RemoveAssetTrackers();

        evt.originPanel.visualTree.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
        evt.originPanel.visualTree.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        evt.originPanel.visualTree.UnregisterCallback<KeyDownEvent>(OnDelete);
    }

    void OnStageChanged(Stage newStage)
    {
        if (m_ContainerElement == null || m_EmptyLabelElement == null)
            return;

        if (newStage is VisualElementEditingStage editingStage)
        {
            EnterStageContext(editingStage);
            return;
        }

        // Outside the UI Stage the window is driven by the current selection. If it maps to a panel
        // component we display that document; otherwise we keep the last one (sticky) or, failing that,
        // show the contextual message.
        m_EditingStage = null;
        m_LastSource = default;

        if (!TryApplySelectionContext() && !TryApplyStickyContext())
            ClearContext();

        // Which stage is displayed decides whether this window answers for the sheets at all, so it is
        // re-evaluated even when the display itself did not have to be rebuilt.
        RefreshUnsavedChangesState();
    }

    void EnterStageContext(VisualElementEditingStage editingStage)
    {
        // Track when we first enter staging mode from a non-staging stage so all sheets are expanded.
        if (m_EditingStage == null && m_Handler != null)
            m_Handler.SetEnteringStagingMode();

        m_EditingStage = editingStage;
        m_LastSource = editingStage.Context;

        var context = StyleSheetsContextFactory.FromStage(editingStage);
        if (!m_Context.DisplaysSameContentAs(context))
            ApplyContext(context);
    }

    /// <summary>
    /// Re-evaluates the current global selection while outside the UI Stage. When the selection resolves to a
    /// <see cref="PanelRenderer"/> or <see cref="UIDocument"/> with a document, that document is displayed;
    /// any other selection is ignored, so the displayed content stays sticky.
    /// </summary>
    internal void RefreshSelectionContext()
    {
        // Inside the UI Stage the displayed data comes from the stage, not the selection.
        if (m_EditingStage != null || m_ContainerElement == null)
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

    /// <summary>
    /// Displays a document resolved from the current selection, rebuilding the content only when what it shows
    /// actually changes — the document itself, or its editability. Re-evaluating the same selection therefore
    /// leaves the hierarchy (and the expansion and selection state it carries) alone.
    /// </summary>
    void ApplySelectionContext(VisualTreeAsset document)
    {
        var context = StyleSheetsContextFactory.FromSelectedDocument(document);
        if (m_Context.DisplaysSameContentAs(context))
            return;

        ApplyContext(context);
    }

    void ClearContext() => ApplyContext(StyleSheetsContext.None);

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

    /// <summary>
    /// The single entry point that sets the window's data. Every source (the UI Stage or the current
    /// selection) funnels through here. Existing nodes are torn down so their read-only state always
    /// matches the incoming context.
    /// </summary>
    void ApplyContext(StyleSheetsContext context)
    {
        TearDownAllNodes();

        m_Context = context;
        m_Handler?.SetReadOnly(context.IsReadOnly);
        UpdateEditingCommandHandlers();

        var hasContent = context.Document != null;

        m_StagingModeContainerElement.style.display = hasContent ? DisplayStyle.None : DisplayStyle.Flex;
        m_ContainerElement.style.display = hasContent ? DisplayStyle.Flex : DisplayStyle.None;
        if (!hasContent)
        {
            m_EmptyLabelElement.style.display = DisplayStyle.None;
            m_NoResultsLabelElement.style.display = DisplayStyle.None;
        }

        UpdateToolbarState();
        UpdateOpenSettingsButton();

        // A different document (or a different editability) means a different set of sheets to answer for.
        RefreshUnsavedChangesState();

        if (!hasContent)
            return;

        // UpdateAssetTrackers refreshes the stylesheet list once the trackers are (re)registered.
        UpdateAssetTrackers();
    }

    void TearDownAllNodes()
    {
        RemoveAssetTrackers();

        if (m_Handler != null)
        {
            foreach (var node in m_StyleSheetNodes.Values)
                m_Handler.RemoveStyleSheet(node);

            foreach (var node in m_ParentStyleSheetNodes.Values)
                m_Handler.RemoveStyleSheet(node);

            if (m_GroupNode != HierarchyNode.Null)
                m_Handler.RemoveStyleSheetGroup(m_GroupNode);
        }

        m_StyleSheetNodes.Clear();
        m_ParentStyleSheetNodes.Clear();
        m_GroupNode = HierarchyNode.Null;
        ActiveStyleSheet = null;
    }

    void UpdateToolbarState()
    {
        // Authoring (add/create USS, add selectors) is only available on an editable document.
        if (m_ToolbarElement != null)
            m_ToolbarElement.style.display = m_Context.IsReadOnly ? DisplayStyle.None : DisplayStyle.Flex;

        m_EmptyLabelElement.text = m_Context.IsReadOnly ? k_ReadOnlyEmptyLabel : k_EditableEmptyLabel;
    }

    /// <summary>
    /// Registers the window as the answer to "which style sheet is being authored?" and "select this rule"
    /// exactly while it has a document it can author — a UI Stage, or a scene document in the Main Stage.
    /// </summary>
    /// <remarks>
    /// A read-only display must leave those queries unanswered: the tools asking (the inspector extracting
    /// inline styles into a class, a style property being written to a rule) would otherwise write into a
    /// document that is not theirs to change.
    /// </remarks>
    void UpdateEditingCommandHandlers() => SetEditingCommandHandlers(m_Context.EditedAsset != null);

    void SetEditingCommandHandlers(bool register)
    {
        if (register == m_EditingCommandHandlersRegistered)
            return;

        m_EditingCommandHandlersRegistered = register;
        if (register)
        {
            UICommandQueue.RegisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
            UICommandQueue.RegisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);
        }
        else
        {
            UICommandQueue.UnregisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
            UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);
        }
    }

    void OnEnableInSceneAuthoringChanged(bool enabled)
    {
        UpdateOpenSettingsButton();
        RefreshEditability();
    }

    void OnMainStageAuthoringChanged(bool enabled)
    {
        RefreshEditability();
    }

    // A style sheet this window is responsible for was dirtied or settled — by an edit made here, or by any
    // other tool sharing it.
    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        if (asset is StyleSheet)
            RefreshUnsavedChangesState();
    }

    /// <summary>
    /// Re-applies the selection-driven context so the displayed document follows the authoring settings: its
    /// stylesheets (and any selected rule) become editable once the scene documents themselves are, and
    /// read-only again as soon as they are not.
    /// </summary>
    void RefreshEditability()
    {
        // A UI Stage is editable no matter how the Main Stage is configured.
        if (m_EditingStage != null || m_ContainerElement == null || m_Context.Document == null)
            return;

        ApplySelectionContext(m_Context.Document);

        // Whether the window answers for these sheets at all follows the settings, so it is re-evaluated even
        // when the display itself did not have to be rebuilt.
        RefreshUnsavedChangesState();
    }

    void UpdateOpenSettingsButton()
    {
        if (m_OpenSettingsButton == null)
            return;

        m_OpenSettingsButton.style.display = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    void OnSelectionRequested(in CommandContext context)
    {
        // Make sure we are up to date.
        OnStyleSheetChanged();
        var toSelect = EntityId.None;
        switch (context.Command)
        {
            case RequestSelectionQuery<StyleSheet> styleSheetRequest:
                if (m_Handler?.Mappings?.TryGetValue(styleSheetRequest.ToSelect, out var styleSheetNode) ?? false)
                    m_Handler.Mappings.TryGetSelectionHandle(styleSheetNode, out toSelect);
                break;
            case RequestSelectionQuery<StyleRule> styleRuleRequest:
                if (m_Handler?.Mappings?.TryGetValue(styleRuleRequest.ToSelect, out var ruleNode) ?? false)
                    m_Handler.Mappings.TryGetSelectionHandle(ruleNode, out toSelect);
                break;
        }

        if(toSelect != EntityId.None)
            Selection.activeEntityId = toSelect;
    }

    void GetActiveStyleSheetRequest(in CommandContext context)
    {
        var styleSheet = ActiveStyleSheet;
        // It is possible that the window has not yet processed a change, so in that case, we directly check on the
        // tracked VTA to see it it contains any style sheet.
        if (styleSheet == null)
        {
            using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
            GetDocumentStyleSheets(styleSheets);
            if (styleSheets.Count > 0)
                styleSheet = styleSheets[0];
        }

        GetActiveStyleSheetQuery.QueryPayload.Execute(CommandSources.StyleSheets, styleSheet);
    }

    void Update()
    {
        PumpHierarchyView();

        // Keep the selection-driven display in sync with its source panel component: a PanelRenderer/UIDocument
        // can be deleted or re-targeted to a different document without the global selection changing. This
        // poll only runs from the editor tick, never re-entrantly from the internal PumpHierarchyView() calls
        // made while rebuilding the list.
        SyncSelectionContextWithSource();
    }

    void PumpHierarchyView()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
        {
            m_HierarchyView.Update();
            UpdateSearchResultsDisplay();
        }
    }

    /// <summary>
    /// While displaying a selection-driven document, tracks the source panel component so the window reacts to
    /// it being deleted or having its <c>visualTreeAsset</c> reassigned.
    /// </summary>
    internal void SyncSelectionContextWithSource()
    {
        if (m_EditingStage != null || m_ContainerElement == null)
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
        if (currentDocument == m_Context.Document)
            return;

        m_LastSelectedDocument = currentDocument;
        if (currentDocument == null)
            ClearContext();
        else
            ApplySelectionContext(currentDocument);
    }

    void UpdateSearchResultsDisplay()
    {
        if (m_HierarchyView == null || m_NoResultsLabelElement == null)
            return;

        if (m_HierarchyView.UpdateNeeded)
        {
            rootVisualElement.schedule.Execute(UpdateSearchResultsDisplay);
            return;
        }

        var noEditableStyleSheets = m_StyleSheetNodes.Count == 0;
        var hasInheritedStyleSheets = m_ParentStyleSheetNodes.Count > 0;
        var noStyleSheets = noEditableStyleSheets && !hasInheritedStyleSheets;
        var noNodesDisplayed = m_HierarchyView.ViewModel.Count == 0;

        if (m_HierarchyView.Filtering)
        {
            m_NoResultsLabelElement.style.display= noNodesDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
            m_HierarchyView.style.display = noNodesDisplayed ? DisplayStyle.None : DisplayStyle.Flex;
        }
        else
        {
            m_NoResultsLabelElement.style.display = DisplayStyle.None;
            m_HierarchyView.style.display = noStyleSheets ? DisplayStyle.None : DisplayStyle.Flex;
        }

        ShowEmptyStyleSheetLabel(noEditableStyleSheets && !m_HierarchyView.Filtering, hasInheritedStyleSheets);
    }

    void ShowEmptyStyleSheetLabel(bool show, bool hasInheritedStyleSheets)
    {
        m_EmptyLabelElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        m_EmptyLabelElement.EnableInClassList(k_EmptyLabelSeparatorUssClassName, show && hasInheritedStyleSheets);
    }

    void OnEmptyLabelDragUpdated(DragUpdatedEvent evt)
    {
        if (!HasDroppableStyleSheet())
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        evt.StopPropagation();
    }

    void OnEmptyLabelDragPerform(DragPerformEvent evt)
    {
        if (m_Context.IsReadOnly || m_TrackedVTA == null)
            return;

        var addedAny = false;
        foreach (var path in DragAndDrop.paths)
        {
            if (!IsAddableStyleSheetPath(path))
                continue;

            AddStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, path);
            addedAny = true;
        }

        if (!addedAny)
            return;

        DragAndDrop.AcceptDrag();
        RefreshStyleSheetList();
        evt.StopPropagation();
    }

    bool HasDroppableStyleSheet()
    {
        if (m_Context.IsReadOnly || m_TrackedVTA == null)
            return false;

        foreach (var path in DragAndDrop.paths)
        {
            if (IsAddableStyleSheetPath(path))
                return true;
        }

        return false;
    }

    bool IsAddableStyleSheetPath(string path)
    {
        if (!StyleSheetAssetUtilities.TryLoadValidStyleSheet(path, out var styleSheet))
            return false;

        using var _ = ListPool<StyleSheet>.Get(out var editable);
        GetDocumentStyleSheets(editable);
        return !editable.Contains(styleSheet);
    }

    void PrepareUI()
    {
        var visualTree = EditorGUIUtility.Load("UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml") as VisualTreeAsset;
        visualTree.CloneTree(rootVisualElement);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        rootVisualElement.styleSheets.Add(styleSheet);

        m_StagingModeContainerElement = rootVisualElement.Q<VisualElement>("unity-style-sheets-window-staging-mode-container");
        m_OpenSettingsButton = rootVisualElement.Q<Button>("unity-style-sheets-window-open-settings-button");
        m_OpenSettingsButton.clicked += UIToolkitAuthoringSettingsProvider.OpenSettings;
        m_EmptyLabelElement = rootVisualElement.Q<Label>("unity-style-sheets-window-empty-label");
        m_EmptyLabelElement.RegisterCallback<DragUpdatedEvent>(OnEmptyLabelDragUpdated);
        m_EmptyLabelElement.RegisterCallback<DragPerformEvent>(OnEmptyLabelDragPerform);
        m_NoResultsLabelElement = rootVisualElement.Q<Label>("unity-style-sheets-window-no-results-label");
        m_ContainerElement = rootVisualElement.Q<VisualElement>("unity-style-sheets-window-container");
        m_ToolbarElement = rootVisualElement.Q<VisualElement>("stylesheet-toolbar");

        var toolbarMenu = rootVisualElement.Q<ToolbarMenu>("add-uss-menu");
        toolbarMenu.menu.AppendAction("Create New USS", _ => CreateStyleSheet());
        toolbarMenu.menu.AppendAction("Add Existing USS", _ => AddStyleSheet());

        var searchField = rootVisualElement.Q<ToolbarSearchField>("stylesheet-search-field");
        searchField.RegisterValueChangedCallback(evt =>
        {
            if (m_HierarchyView == null)
                return;

            m_HierarchyView.Filter = evt.newValue ?? string.Empty;
            UpdateSearchResultsDisplay();
        });

        var newStyleRuleField = rootVisualElement.Q<NewSelectorField>("new-selector-field");
        newStyleRuleField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewStyleRule);

        m_Hierarchy = new Hierarchy.Hierarchy();
        m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<StyleSheetEditingNodeTypeHandler>();
        m_Handler.Window = this;

        m_HierarchyView = new HierarchyView();
        m_HierarchyView.ListView.fixedItemHeight = 20;
        m_HierarchyView.AddToClassList(StyleSheetNodeTypeHandler.StyleSheetsWindowHierarchyViewUssClassName);
        m_HierarchyView.NameColumn.title = "Rules";
        m_HierarchyView.NameColumn.stretchable = true;

        var specificityColumn = new Column
        {
            title = "Specificity",
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var node = m_HierarchyView.ViewModel[index];

                if (m_Handler.Mappings.TryGetValue(node, out var styleNode) && styleNode.Rule?.complexSelectors?.Length > 0)
                {
                    label.text = $"({styleNode.Rule.complexSelectors[0].specificity})";
                }
                else
                {
                    label.text = "";
                }
            },
            visible = false,
            width = 80,
        };

        // Add the column after setting source
        var columns = new List<Column>
        {
            m_HierarchyView.NameColumn,
            specificityColumn
        };

        m_HierarchyView.SetColumns(columns);
        m_HierarchyView.SetSourceHierarchy(m_Hierarchy);

        m_GlobalSelectionHandler =
            new HierarchyGlobalSelectionHandler(m_HierarchyView, new EditorGUIUtility.EditorLockTracker());
        rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
        rootVisualElement.RegisterCallback<KeyUpEvent>(OnKeyUp);

        m_HierarchyView.ListView.RegisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed,
            TrickleDown.TrickleDown);
        m_HierarchyView?.ListView.RegisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);

        m_ContainerElement.Insert(m_ContainerElement.IndexOf(m_EmptyLabelElement), m_HierarchyView);
        m_HierarchyView.style.flexBasis = 0;
        m_EmptyLabelElement.style.flexBasis = 0;
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    void OnKeyUp(KeyUpEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.Escape:
                m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
                break;
        }
    }

    void OnHierarchyWindowDragPerformed(DragPerformEvent evt)
    {
        if (!m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected))
            return;
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    void RemoveAssetTrackers()
    {
        if (m_LiveReloadSystem == null)
            return;

        if (!m_TrackedVTA)
            return;

        m_LiveReloadSystem.UnregisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);
        m_TrackedVTA = null;

        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }

        foreach (var key in m_ParentStyleSheetNodes.Keys)
        {
            m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, key.styleSheet);
        }
    }

    void UpdateAssetTrackers()
    {
        if (m_LiveReloadSystem == null)
            return;

        var document = m_Context.Document;
        if (document == null)
            return;

        if (m_TrackedVTA)
            m_LiveReloadSystem.UnregisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        m_TrackedVTA = document;
        m_LiveReloadSystem.RegisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        // Re-register all existing stylesheets to the live reload system
        // This is necessary when the panel changes (e.g., docking/undocking)
        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            m_LiveReloadSystem.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }

        foreach (var key in m_ParentStyleSheetNodes.Keys)
        {
            m_LiveReloadSystem.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, key.styleSheet);
        }

        RefreshStyleSheetList();
    }

    void OnCreateNewStyleRule(NewSelectorSubmitEvent evt)
    {
        if (m_Context.IsReadOnly)
            return;

        if (!StyleSheetExtensions.ValidateStyleRule(evt.selectorStr, out var error))
        {
            Debug.LogError($"Invalid selector string '{evt.selectorStr}': {error}.");
            return;
        }

        if (!TryEnsureActiveStyleSheetBeforeNewRule())
            return;

        AddStyleRuleCommand.Execute(CommandSources.StyleSheets, ActiveStyleSheet, evt.selectorStr);
        SelectAndScrollTo([m_ActiveStyleSheet.rules[^1]]);
    }

    /// <summary>
    /// When adding a rule but the document has no USS yet, prompts for a save path and creates one
    /// (same idea as the Builder Style Sheets UX for new files).
    /// </summary>
    bool TryEnsureActiveStyleSheetBeforeNewRule()
    {
        if (m_TrackedVTA == null)
            return false;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        GetDocumentStyleSheets(sheets);

        if (ActiveStyleSheet != null && sheets.Contains(ActiveStyleSheet))
            return true;

        if (sheets.Count > 0)
        {
            ActiveStyleSheet = sheets[0];
            m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
            return true;
        }

        return TryCreateStyleSheet() && ActiveStyleSheet != null;
    }

    void GetDocumentStyleSheets(List<StyleSheet> output)
    {
        output.Clear();
        if (m_TrackedVTA != null)
            m_TrackedVTA.GetAllReferencedStyleSheets(output);
    }

    void OnVisualTreeAssetChanged()
    {
        // VTA changed - check if the stylesheet list actually changed
        var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
        if (stylesheets == null)
            stylesheets = new List<StyleSheet>();

        // Early out if the stylesheet list hasn't changed
        if (StyleSheetListsEqual(stylesheets, m_LastStyleSheets))
            return;

        m_LastStyleSheets = new List<StyleSheet>(stylesheets);
        RefreshStyleSheetList();
    }

    bool StyleSheetListsEqual(List<StyleSheet> a, List<StyleSheet> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    void OnStyleSheetChanged()
    {
        RefreshStyleSheetList();
        RefreshStyleSheetRules();
    }

    void OnProjectChange()
    {
        if (m_GroupNode == HierarchyNode.Null)
            return;

        if (HasMissingParent())
            RefreshStyleSheetList();
        else
            RefreshParentDocumentNames();
    }

    void RefreshStyleSheetList()
    {
        // Update the hierarchy nodes in case there any pending changes.
        PumpHierarchyView();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        RefreshContext();

        // Clean up stale nodes that no longer exist in hierarchy (e.g., after domain reload)
        var nodesToRemove = new List<StyleSheet>();
        foreach (var (stylesheet, node) in m_StyleSheetNodes)
        {
            if (!m_Hierarchy.Exists(node))
            {
                nodesToRemove.Add(stylesheet);
            }
        }
        foreach (var stylesheet in nodesToRemove)
        {
            m_StyleSheetNodes.Remove(stylesheet);
        }

        using var staleParentsHandle = ListPool<(VisualTreeAsset owningDocument, StyleSheet styleSheet)>.Get(out var staleParents);
        foreach (var (key, node) in m_ParentStyleSheetNodes)
        {
            if (!m_Hierarchy.Exists(node))
            {
                staleParents.Add(key);
            }
        }
        foreach (var key in staleParents)
        {
            m_ParentStyleSheetNodes.Remove(key);
        }

        using var stylesheetsHandle = ListPool<StyleSheet>.Get(out var stylesheets);
        GetDocumentStyleSheets(stylesheets);

        m_LastStyleSheets.Clear();
        m_LastStyleSheets.AddRange(stylesheets);

        var parents = m_Context.ParentStyleSheets;
        var noEditableStyleSheets = stylesheets.Count == 0;
        var hasInheritedStyleSheets = parents.Count > 0;
        // Only a document with neither its own nor inherited stylesheets is truly empty; an in-context
        // document with inherited-only stylesheets still shows the inherited group plus the prompt.
        var emptyStyleSheetWindow = noEditableStyleSheets && !hasInheritedStyleSheets;

        ShowEmptyStyleSheetLabel(noEditableStyleSheets && !m_HierarchyView.Filtering, hasInheritedStyleSheets);
        m_NoResultsLabelElement.style.display = DisplayStyle.None;
        m_HierarchyView.style.display = emptyStyleSheetWindow ? DisplayStyle.None : DisplayStyle.Flex;

        if (emptyStyleSheetWindow)
        {
            // Clear all nodes if no stylesheets remain
            foreach (var (stylesheet, node) in m_StyleSheetNodes)
            {
                m_Handler.RemoveStyleSheet(node);
                m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
            }
            m_StyleSheetNodes.Clear();
            RemoveOldParentStyleSheets(parents);

            ActiveStyleSheet = null;
            if (m_TrackedVTA)
                m_ActiveStyleSheetInVisualTreeAsset.Remove(m_TrackedVTA);

            return;
        }

        using var remappingsHandle = ListPool<StyleSheetRemap>.Get(out var remappings);
        using var addedHandle = HashSetPool<StyleSheet>.Get(out var added);
        using var removedHandle = HashSetPool<StyleSheet>.Get(out var removed);

        // Separate stylesheets into added and removed sets
        var newStylesheets = new HashSet<StyleSheet>(stylesheets);
        foreach (var stylesheet in newStylesheets)
        {
            if (!m_StyleSheetNodes.ContainsKey(stylesheet))
                added.Add(stylesheet);
        }

        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            if (!newStylesheets.Contains(stylesheet))
                removed.Add(stylesheet);
        }

        // Check for remappings (same asset GUID, different instance due to reload)
        if (added.Count > 0 && removed.Count > 0)
        {
            StyleSheetRemapper.Remap(added, removed, remappings);
        }

        // Handle remapped stylesheets
        if (remappings.Count > 0)
        {
            foreach (var remap in remappings)
            {
                if (m_StyleSheetNodes.TryGetValue(remap.Previous, out var node))
                {
                    // Update tracking dictionary
                    m_StyleSheetNodes.Remove(remap.Previous);
                    m_StyleSheetNodes[remap.Remapped] = node;

                    // Update asset trackers
                    m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, remap.Previous);
                    m_LiveReloadSystem?.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, remap.Remapped);

                    // Remove from added/removed since they're remapped
                    added.Remove(remap.Remapped);
                    removed.Remove(remap.Previous);
                }
            }

            // Update handler mappings
            m_Handler.Mappings.Remap(remappings);
            m_Handler.Remap(remappings);
        }

        RemoveOldParentStyleSheets(parents);

        // Remove truly removed stylesheets
        foreach (var stylesheet in removed)
        {
            if (m_StyleSheetNodes.TryGetValue(stylesheet, out var node))
            {
                m_Handler.RemoveStyleSheet(node);
                m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
                m_StyleSheetNodes.Remove(stylesheet);
            }
        }

        foreach (var stylesheet in added)
        {
            m_LiveReloadSystem?.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
            var rootNode = m_Handler.AddStyleSheet(stylesheet, isReadOnly: m_Context.IsReadOnly);
            m_StyleSheetNodes[stylesheet] = rootNode;
        }

        AddNewParentStyleSheets(parents);

        // The active stylesheet is an editing concept; a read-only display has none.
        if (m_Context.IsReadOnly)
        {
            ActiveStyleSheet = null;
        }
        // Set and store the active stylesheet for the current visual tree asset
        else if (m_TrackedVTA)
        {
            if (m_ActiveStyleSheetInVisualTreeAsset.TryGetValue(m_TrackedVTA, out var savedActive) && stylesheets.Contains(savedActive))
            {
                ActiveStyleSheet = savedActive;
            }
            else if ((ActiveStyleSheet == null || !stylesheets.Contains(ActiveStyleSheet)) && stylesheets.Count > 0)
            {
                ActiveStyleSheet = stylesheets[0];
                m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
            }
        }

        using var _editableHandle = ListPool<HierarchyNode>.Get(out var editableNodes);
        foreach (var styleSheet in stylesheets)
        {
            if (m_StyleSheetNodes.TryGetValue(styleSheet, out var node))
                editableNodes.Add(node);
        }
        m_Handler.SortTopLevel(m_GroupNode, editableNodes);

        using var _orderHandle = ListPool<HierarchyNode>.Get(out var parentNodes);
        foreach (var entry in parents)
        {
            if (entry.StyleSheet != null && m_ParentStyleSheetNodes.TryGetValue((entry.OwningDocument, entry.StyleSheet), out var node))
                parentNodes.Add(node);
        }
        m_Handler.SortGroupChildren(m_GroupNode, parentNodes);
    }

    void RefreshContext()
    {
        // Outside the UI Stage the context is set by the selection-driven path (ApplyContext); leave it alone.
        if (m_EditingStage == null)
            return;

        var source = m_EditingStage.Context;
        if (source == m_LastSource && !HasMissingParent())
            return;

        m_LastSource = source;
        m_Context = StyleSheetsContextFactory.FromStage(m_EditingStage);
    }

    bool HasMissingParent()
    {
        foreach (var parent in m_Context.ParentStyleSheets)
        {
            if (parent.StyleSheet == null)
                return true;
        }

        return false;
    }

    void RefreshParentDocumentNames()
    {
        foreach (var node in m_ParentStyleSheetNodes.Values)
            m_Handler.RefreshStyleSheetNodeName(node);
    }

    /// <summary>
    /// Settles the unsaved changes that are about to leave the document along with <paramref name="styleSheet"/>,
    /// and reports whether the removal may go ahead. Nothing else would ask about them: once the document stops
    /// referencing it, the stylesheet is no longer part of what the UI Stage or the Main Stage saves.
    /// </summary>
    bool TrySettleStyleSheetChanges(StyleSheet styleSheet)
    {
        if (m_EditingStage != null)
            return m_EditingStage.AskUserToSaveModifiedStage();

        var registry = UIAssetRegistry.LiveInstance;
        if (registry == null || !registry.IsDirty(styleSheet))
            return true;

        return UIAssetSavePrompt.AskAndResolve(new UnityEngine.Object[] { styleSheet },
            L10n.Tr("The stylesheet is about to be removed from the document. Your changes will be lost if you don't save them.", null),
            allowCancel: true);
    }

    void RemoveOldParentStyleSheets(IReadOnlyList<ParentStyleSheet> parents)
    {
        if (m_GroupNode != HierarchyNode.Null && !m_Hierarchy.Exists(m_GroupNode))
            m_GroupNode = HierarchyNode.Null;

        using var desiredHandle = HashSetPool<(VisualTreeAsset owningDocument, StyleSheet styleSheet)>.Get(out var desired);
        foreach (var parent in parents)
            desired.Add((parent.OwningDocument, parent.StyleSheet));

        using var removeHandle = ListPool<(VisualTreeAsset owningDocument, StyleSheet styleSheet)>.Get(out var toRemove);
        foreach (var key in m_ParentStyleSheetNodes.Keys)
        {
            if (!desired.Contains(key))
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
        {
            if (m_ParentStyleSheetNodes.TryGetValue(key, out var node))
            {
                m_Handler.RemoveStyleSheet(node);
                m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, key.styleSheet);
                m_ParentStyleSheetNodes.Remove(key);
            }
        }

        if (parents.Count == 0 && m_GroupNode != HierarchyNode.Null)
        {
            m_Handler.RemoveStyleSheetGroup(m_GroupNode);
            m_GroupNode = HierarchyNode.Null;
        }
    }

    void AddNewParentStyleSheets(IReadOnlyList<ParentStyleSheet> parents)
    {
        if (parents.Count == 0)
            return;

        if (m_GroupNode == HierarchyNode.Null)
            m_GroupNode = m_Handler.AddStyleSheetGroup();

        foreach (var parent in parents)
        {
            var key = (parent.OwningDocument, parent.StyleSheet);
            if (parent.StyleSheet == null || m_ParentStyleSheetNodes.ContainsKey(key))
                continue;

            m_LiveReloadSystem?.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, parent.StyleSheet);
            m_ParentStyleSheetNodes[key] =
                m_Handler.AddStyleSheet(parent.StyleSheet, isReadOnly: true, owningDocument: parent.OwningDocument, parent: m_GroupNode);
        }
    }

    void RefreshStyleSheetRules()
    {
        // Update the hierarchy nodes in case there any pending changes.
        PumpHierarchyView();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        // Refresh rules for all existing stylesheets
        foreach (var (styleSheet, ruleNode) in m_StyleSheetNodes)
            m_Handler.RefreshStyleSheetRules(ruleNode, styleSheet, isReadOnly: m_Context.IsReadOnly);

        foreach (var (key, ruleNode) in m_ParentStyleSheetNodes)
            m_Handler.RefreshStyleSheetRules(ruleNode, key.styleSheet, isReadOnly: true);
    }

    public void SetActiveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return;

        if (node.Rule != null)
            return;

        if (node.IsReadOnly)
            return;

        ActiveStyleSheet = node.StyleSheet;
        m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
    }

    public void CreateStyleSheet()
    {
        TryCreateStyleSheet();
    }

    public void AddStyleSheet()
    {
        if (m_Context.IsReadOnly)
            return;

        var ussPath = StyleSheetAssetUtilities.DisplayOpenFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return;

        AddStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
    }

    public void RemoveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return;

        if (node.IsReadOnly)
            return;

        var styleSheet = node.StyleSheet;

        // Make sure pending edits are settled *before* we remove the stylesheet, otherwise edits to that
        // stylesheet would be lost.
        if (!TrySettleStyleSheetChanges(styleSheet))
            return;

        RemoveStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, styleSheet);
        RefreshStyleSheetList();
    }

    public void SelectAndScrollTo(IReadOnlyList<StyleRule> targetRules)
    {
        if (targetRules == null || targetRules.Count == 0)
            return;

        RefreshStyleSheetRules();
        PumpHierarchyView();

        using var _ = ListPool<HierarchyNode>.Get(out var nodesToSelect);

        foreach (var targetRule in targetRules)
        {
            var styleSheet = targetRule.styleSheet;
            if (styleSheet == null)
                continue;

            if (!m_StyleSheetNodes.TryGetValue(styleSheet, out var styleSheetNode))
                continue;

            m_HierarchyView.ViewModel.SetFlags(styleSheetNode, HierarchyNodeFlags.Expanded);

            var children = m_Hierarchy.GetChildren(styleSheetNode);
            foreach (var child in children)
            {
                if (!m_Handler.Mappings.TryGetValue(child, out var styleNode) || styleNode.Rule != targetRule)
                    continue;

                nodesToSelect.Add(child);
                break;
            }
        }

        if (nodesToSelect.Count <= 0)
            return;

        m_HierarchyView.SetSelection(nodesToSelect.ToArray());
        m_HierarchyView.Frame(nodesToSelect[0]);
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    bool TryCreateStyleSheet()
    {
        if (m_Context.IsReadOnly)
            return false;

        var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return false;

        CreateStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
        return true;
    }

    void OnHierarchyWindowMouseDown(PointerDownEvent evt)
    {
        // Synchronize global selection from view model on right-click.
        // This makes sure the element currently selected by the right click in the view
        // is set to the global selection. This is important since some context-menu operations
        // don't take parameters and instead are reading directly from Selection.activeObject or Selection.entityIds.
        if (IsRightClick((MouseButton)evt.button, evt.modifiers))
        {
            // Place the focus on the window, otherwise interactions like the text field will not work
            Focus();
            m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
        }

        static bool IsRightClick(MouseButton btn, EventModifiers modifiers)
        {
            // on OSX a right click can be either right click or ctrl+left click
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                return btn == MouseButton.RightMouse
                       || (btn == MouseButton.LeftMouse
                           && modifiers == EventModifiers.Control);
            }

            return btn == MouseButton.RightMouse;
        }
    }

    void OnValidateCommand(ValidateCommandEvent evt)
    {
        switch (evt.commandName)
        {
            case EventCommandNames.Cut:
            case EventCommandNames.Copy:
            case EventCommandNames.Paste:
            case EventCommandNames.Rename:
            case EventCommandNames.Duplicate:
            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
            case EventCommandNames.SelectAll:
                evt.StopPropagation();
                break;
        }
    }

    void OnExecuteCommand(ExecuteCommandEvent evt)
    {
        switch (evt.commandName)
        {
            case EventCommandNames.Copy:
                m_HierarchyView.OnCopy();
                evt.StopPropagation();
                break;

            case EventCommandNames.Paste:
                m_HierarchyView.OnPaste();
                evt.StopPropagation();
                break;

            case EventCommandNames.Rename:
                var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
                if (count == 1)
                {
                    Span<HierarchyNode> nodes = stackalloc HierarchyNode[1];
                    m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
                    m_HierarchyView.OnSetName(nodes[0]);
                }
                evt.StopPropagation();
                break;

            case EventCommandNames.Duplicate:
                m_HierarchyView.OnDuplicate();
                evt.StopPropagation();
                break;

            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
                m_HierarchyView.OnDelete();
                evt.StopPropagation();
                break;

            case EventCommandNames.SelectAll:
                m_HierarchyView.SelectAll(exposedOnly: true);
                m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
                evt.StopPropagation();
                break;

            case EventCommandNames.UndoRedoPerformed:
                m_Hierarchy.SetDirty();
                evt.StopPropagation();
                break;
        }
    }

    void OnDelete(KeyDownEvent evt)
    {
        // HACK: This must be a bug. TextField leaks its key events to everyone!
        if (evt.target is TextElement)
            return;

        var keyCode = evt.keyCode;
        if (keyCode != KeyCode.Delete && keyCode != KeyCode.Backspace)
            return;

        var nodes = m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected);
        // Will delete all the selected StyleSheets.
        foreach (var node in nodes)
        {
            if (Handler.IsStyleSheet(node))
                RemoveStyleSheet(node);
        }

        // The StyleSheets window's hierarchy view OnDelete workflow only handle StyleRules and will early out on
        // StyleSheets. This handles the case where we multi-select stylesheets with style rules.
        m_HierarchyView.OnDelete();
        evt.StopPropagation();
    }

    class VisualTreeAssetTracker(Action onVisualTreeAssetChanged) : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
    {
        internal override void OnVisualTreeAssetChanged()
        {
            onVisualTreeAssetChanged?.Invoke();
        }
    }

    class StyleSheetAssetTracker(Action onStyleSheetChanged) : LiveReloadStyleSheetAssetTracker, IAuthoringLiveReloadAssetTracker<StyleSheet>
    {
        public override void OnTrackedAssetChanged()
        {
            onStyleSheetChanged?.Invoke();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
