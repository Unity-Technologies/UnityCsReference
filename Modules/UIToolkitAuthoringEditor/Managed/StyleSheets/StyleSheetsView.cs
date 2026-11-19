// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The style sheets pane: the style sheets of one document and their rules, the field that adds a rule, and the
/// commands that edit them.
/// </summary>
/// <remarks>
/// <para>
/// What the view shows is set by its host: a <see cref="UIDocumentSession"/> (<see cref="ShowSession"/>), whose
/// document it follows, or a fixed <see cref="StyleSheetsContext"/> (<see cref="ShowDocument"/>). The Style
/// Sheets window resolves that from the UI Stage and the selection; a window with a local context shows its own
/// session.
/// </para>
/// <para>
/// The view answers the active style sheet query and the style sheet and rule selection requests of the
/// <see cref="AuthoringContext"/> it was created for, and writes the rows the user selects to that context's
/// selection: through the editor selection for <see cref="AuthoringContext.Global"/>, through a
/// <see cref="HierarchyLocalSelectionHandler"/> otherwise.
/// </para>
/// <para>
/// Its host pumps it (<see cref="Update"/>).
/// </para>
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class StyleSheetsView : VisualElement, IDisposable
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowLight.uss";
    const string k_EmptyLabelSeparatorUssClassName = "unity-style-sheets-window__empty-label--separator";
    static readonly string k_EditableEmptyLabel = L10n.Tr("Click the + icon to create a new StyleSheet.", null);
    static readonly string k_ReadOnlyEmptyLabel = L10n.Tr("The selected UI Document has no style sheets.", null);

    internal const string updateMarkerName = "StyleSheetsView.Update";
    [NoAutoStaticsCleanup] // immutable profiler marker, safe to persist
    static readonly ProfilerMarker k_UpdateMarker = new(updateMarkerName);

    readonly AuthoringContext m_AuthoringContext;
    readonly bool m_HandlesPanelCommands;

    Unity.Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetEditingNodeTypeHandler m_Handler;
    readonly Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();
    readonly Dictionary<(VisualTreeAsset owningDocument, StyleSheet styleSheet), HierarchyNode> m_ParentStyleSheetNodes = new();
    HierarchyNode m_GroupNode = HierarchyNode.Null;
    ILiveReloadSystem m_LiveReloadSystem;
    VisualTreeAsset m_TrackedVTA;
    ToolbarMenu m_AddStyleSheetMenu;
    readonly StyleSheetAssetTracker m_StyleSheetTracker;
    readonly VisualTreeAssetTracker m_VisualTreeAssetTracker;
    HierarchyGlobalSelectionHandler m_GlobalSelectionHandler;
    HierarchyLocalSelectionHandler m_LocalSelectionHandler;
    readonly Label m_EmptyLabelElement;
    readonly VisualElement m_NoResultsLabelElement;
    readonly VisualElement m_StagingModeContainerElement;
    readonly VisualElement m_ToolbarElement;
    readonly ToolbarSearchField m_SearchField;
    readonly VisualElement m_ContainerElement;
    readonly NewSelectorField m_NewSelectorField;

    StyleSheetsContext m_Context = StyleSheetsContext.None;

    // The session whose document is on display, when the host follows one, and the document it was showing the
    // last time the display was derived from it.
    UIDocumentSession m_Session;
    VisualTreeAssetEditingContext m_LastSessionDocument;

    // Whether the queries only an authoring view can answer are currently routed here (see
    // UpdateEditingCommandHandlers).
    bool m_EditingCommandHandlersRegistered;

    readonly List<StyleSheet> m_LastStyleSheets = new();

    StyleSheet m_ActiveStyleSheet;
    readonly Dictionary<VisualTreeAsset, StyleSheet> m_ActiveStyleSheetInVisualTreeAsset = new();

    VisualElement m_CommandTarget;
    bool m_Disposed;

    /// <param name="context">The context the view selects in and answers the style sheet queries of.</param>
    /// <param name="handlesPanelCommands">
    /// Whether the view takes the edit commands of its whole panel, not only those sent while it has focus. A
    /// window the view fills does; a pane next to other panes does not.
    /// </param>
    public StyleSheetsView(AuthoringContext context, bool handlesPanelCommands = false)
    {
        m_AuthoringContext = context ?? throw new ArgumentNullException(nameof(context));
        m_HandlesPanelCommands = handlesPanelCommands;
        style.flexGrow = 1;

        m_VisualTreeAssetTracker = new VisualTreeAssetTracker(OnVisualTreeAssetChanged);
        m_StyleSheetTracker = new StyleSheetAssetTracker(OnStyleSheetChanged);

        var visualTree = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        visualTree.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_StagingModeContainerElement = this.Q<VisualElement>("unity-style-sheets-window-staging-mode-container");
        m_EmptyLabelElement = this.Q<Label>("unity-style-sheets-window-empty-label");
        m_EmptyLabelElement.RegisterCallback<DragUpdatedEvent>(OnEmptyLabelDragUpdated);
        m_EmptyLabelElement.RegisterCallback<DragPerformEvent>(OnEmptyLabelDragPerform);
        m_NoResultsLabelElement = this.Q<Label>("unity-style-sheets-window-no-results-label");
        m_ContainerElement = this.Q<VisualElement>("unity-style-sheets-window-container");
        m_ToolbarElement = this.Q<VisualElement>("stylesheet-toolbar");

        m_AddStyleSheetMenu = this.Q<ToolbarMenu>("add-uss-menu");
        m_AddStyleSheetMenu.menu.AppendAction("Create New USS", _ => CreateStyleSheet());
        m_AddStyleSheetMenu.menu.AppendAction("Add Existing USS", _ => AddStyleSheet());

        m_SearchField = this.Q<ToolbarSearchField>("stylesheet-search-field");
        m_SearchField.RegisterValueChangedCallback(evt =>
        {
            if (m_HierarchyView == null)
                return;

            m_HierarchyView.Filter = evt.newValue ?? string.Empty;
            UpdateSearchResultsDisplay();
        });

        m_NewSelectorField = this.Q<NewSelectorField>("new-selector-field");
        m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewStyleRule);

        CreateHierarchy();

        RegisterCallback<PointerUpEvent>(OnPointerUp);
        RegisterCallback<KeyUpEvent>(OnKeyUp);
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        ApplyContext(StyleSheetsContext.None);
    }

    public AuthoringContext AuthoringContext => m_AuthoringContext;

    internal Unity.Hierarchy.Hierarchy SourceHierarchy => m_Hierarchy;
    internal HierarchyView HierarchyView => m_HierarchyView;
    internal StyleSheetEditingNodeTypeHandler Handler => m_Handler;
    internal HierarchyGlobalSelectionHandler GlobalSelectionHandler => m_GlobalSelectionHandler;
    internal HierarchyLocalSelectionHandler LocalSelectionHandler => m_LocalSelectionHandler;
    internal int ParentStyleSheetCount => m_ParentStyleSheetNodes.Count;
    public NewSelectorField NewSelectorField => m_NewSelectorField;
    public ToolbarSearchField SearchField => m_SearchField;

    /// <summary>What the view displays.</summary>
    public StyleSheetsContext DisplayedContext => m_Context;

    /// <summary>The session whose document the view follows, or null when the host sets the document.</summary>
    public UIDocumentSession Session => m_Session;

    /// <summary>The message shown while the view has no document to show.</summary>
    public string EmptyStateText
    {
        get => m_StagingModeContainerElement.Q<Label>()?.text;
        set
        {
            if (m_StagingModeContainerElement.Q<Label>() is { } label)
                label.text = value;
        }
    }

    /// <summary>The document whose style sheets can be edited, or null when the display is read-only or empty.</summary>
    public VisualTreeAsset EditedAsset => m_Context.EditedAsset;

    /// <summary>Whether style sheets can be added to the displayed document, removed from it, or reordered.</summary>
    public bool CanChangeStyleSheets => m_Context.CanChangeStyleSheets;

    /// <summary>The document whose style sheets are currently displayed (editable or read-only).</summary>
    public VisualTreeAsset DisplayedDocument => m_Context.Document;

    /// <summary>True when the displayed document cannot be edited from this view.</summary>
    public bool IsReadOnly => m_Context.IsReadOnly;

    /// <summary>
    /// Asks whether the unsaved changes of a style sheet about to be removed from the document may be left
    /// behind, settling them if the user wants to. Null asks about that style sheet alone.
    /// </summary>
    public Func<StyleSheet, bool> SettleStyleSheetChangesBeforeRemoval { get; set; }

    /// <summary>Raised after the view displays something else.</summary>
    public event Action DisplayChanged;

    /// <summary>Raised after a style sheet was saved or discarded on its own from the view.</summary>
    public event Action StyleSheetSettled;

    public StyleSheet ActiveStyleSheet
    {
        get => m_ActiveStyleSheet;
        private set
        {
            if (m_ActiveStyleSheet == value)
                return;
            m_ActiveStyleSheet = value;
            ActiveStyleSheetChangedMessage.Execute(m_AuthoringContext, CommandSources.StyleSheets, m_ActiveStyleSheet);
        }
    }

    void CreateHierarchy()
    {
        m_Hierarchy = new Unity.Hierarchy.Hierarchy();
        m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<StyleSheetEditingNodeTypeHandler>();
        m_Handler.View = this;
        m_Handler.Context = m_AuthoringContext;

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

        if (m_AuthoringContext.IsGlobal)
            m_GlobalSelectionHandler = new HierarchyGlobalSelectionHandler(m_HierarchyView, new EditorGUIUtility.EditorLockTracker());
        else
            m_LocalSelectionHandler = new HierarchyLocalSelectionHandler(m_HierarchyView, m_Hierarchy, m_AuthoringContext.Selection);

        m_HierarchyView.ListView.RegisterCallback<DragPerformEvent>(OnHierarchyDragPerformed, TrickleDown.TrickleDown);
        m_HierarchyView.ListView.RegisterCallback<PointerDownEvent>(OnHierarchyPointerDown);

        m_ContainerElement.Insert(m_ContainerElement.IndexOf(m_EmptyLabelElement), m_HierarchyView);
        m_HierarchyView.style.flexBasis = 0;
        m_EmptyLabelElement.style.flexBasis = 0;
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        UnregisterCommandCallbacks();
        RemoveAssetTrackers();
        SetEditingCommandHandlers(false);
        m_AuthoringContext.Messages.UnregisterHandler<RequestNewSelectorCommand>(OnNewSelectorRequested);

        m_HierarchyView?.ListView.UnregisterCallback<PointerDownEvent>(OnHierarchyPointerDown);
        m_HierarchyView?.ListView.UnregisterCallback<DragPerformEvent>(OnHierarchyDragPerformed, TrickleDown.TrickleDown);
        m_HierarchyView?.Dispose();
        m_Hierarchy?.Dispose();
        m_GlobalSelectionHandler?.Dispose();
        m_LocalSelectionHandler?.Dispose();
        m_Hierarchy = null;
        m_HierarchyView = null;
        m_Handler = null;
        m_GlobalSelectionHandler = null;
        m_LocalSelectionHandler = null;
        m_StyleSheetNodes.Clear();
        m_ParentStyleSheetNodes.Clear();
        m_GroupNode = HierarchyNode.Null;
        m_Session = null;

        m_LiveReloadSystem = null;
        m_ActiveStyleSheet = null;
        m_ActiveStyleSheetInVisualTreeAsset.Clear();
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (m_Disposed)
            return;

        // Docking or undocking moves the view to another panel, with its own live reload system.
        m_LiveReloadSystem = ((Panel)evt.destinationPanel).liveReloadSystem;

        RegisterCommandCallbacks(m_HandlesPanelCommands ? evt.destinationPanel.visualTree : this);
        m_AuthoringContext.Messages.RegisterHandler<RequestNewSelectorCommand>(OnNewSelectorRequested);

        UpdateAssetTrackers();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        RemoveAssetTrackers();
        UnregisterCommandCallbacks();
        m_AuthoringContext.Messages.UnregisterHandler<RequestNewSelectorCommand>(OnNewSelectorRequested);
    }

    void RegisterCommandCallbacks(VisualElement target)
    {
        UnregisterCommandCallbacks();

        m_CommandTarget = target;
        m_CommandTarget.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        m_CommandTarget.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        m_CommandTarget.RegisterCallback<KeyDownEvent>(OnDelete);
    }

    void UnregisterCommandCallbacks()
    {
        if (m_CommandTarget == null)
            return;

        m_CommandTarget.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
        m_CommandTarget.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        m_CommandTarget.UnregisterCallback<KeyDownEvent>(OnDelete);
        m_CommandTarget = null;
    }

    /// <summary>
    /// Shows the document of <paramref name="session"/>, editable, with the style sheets it inherits when it is
    /// edited in context, and follows its document from then on.
    /// </summary>
    public void ShowSession(UIDocumentSession session)
    {
        if (session == null)
        {
            ClearDisplay();
            return;
        }

        // Every sheet is expanded the first time a session is shown.
        if (m_Session == null)
            m_Handler?.SetEnteringStagingMode();

        m_Session = session;
        m_LastSessionDocument = session.Document;

        var context = StyleSheetsContextFactory.FromSession(session);
        if (!m_Context.DisplaysSameContentAs(context))
            ApplyContext(context);
    }

    /// <summary>
    /// Shows <paramref name="context"/>, rebuilding the display only when what it shows actually changes — the
    /// document, its editability or its inherited style sheets. Showing the same thing again therefore leaves the
    /// hierarchy (and the expansion and selection state it carries) alone.
    /// </summary>
    public void ShowDocument(StyleSheetsContext context)
    {
        m_Session = null;
        m_LastSessionDocument = default;

        if (m_Context.DisplaysSameContentAs(context))
            return;

        ApplyContext(context);
    }

    /// <summary>Shows nothing.</summary>
    public void ClearDisplay()
    {
        m_Session = null;
        m_LastSessionDocument = default;
        ApplyContext(StyleSheetsContext.None);
    }

    /// <summary>
    /// The single entry point that sets the view's data. Existing nodes are torn down so their read-only state
    /// always matches the incoming context.
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

        if (hasContent)
        {
            // UpdateAssetTrackers refreshes the stylesheet list once the trackers are (re)registered.
            UpdateAssetTrackers();
        }

        DisplayChanged?.Invoke();
    }

    void TearDownAllNodes()
    {
        RemoveAssetTrackers();

        // Nodes added since the last update only exist once it runs; removed before then, they would stay.
        PumpHierarchyView();

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

        // A style sheet edited on its own takes new selectors, but no other style sheet.
        if (m_AddStyleSheetMenu != null)
            m_AddStyleSheetMenu.style.display = m_Context.StandaloneStyleSheet != null ? DisplayStyle.None : DisplayStyle.Flex;

        m_EmptyLabelElement.text = m_Context.IsReadOnly ? k_ReadOnlyEmptyLabel : k_EditableEmptyLabel;
    }

    /// <summary>
    /// Registers the view as the answer to "which style sheet is being authored?" and "select this rule"
    /// exactly while it has a document it can author.
    /// </summary>
    /// <remarks>
    /// A read-only display must leave those queries unanswered: the tools asking (the inspector extracting
    /// inline styles into a class, a style property being written to a rule) would otherwise write into a
    /// document that is not theirs to change.
    /// </remarks>
    void UpdateEditingCommandHandlers() => SetEditingCommandHandlers(!m_Disposed && m_Context.EditedAsset != null);

    void SetEditingCommandHandlers(bool register)
    {
        if (register == m_EditingCommandHandlersRegistered)
            return;

        m_EditingCommandHandlersRegistered = register;
        if (register)
        {
            m_AuthoringContext.Messages.RegisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
            m_AuthoringContext.Messages.RegisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);
        }
        else
        {
            m_AuthoringContext.Messages.UnregisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
            m_AuthoringContext.Messages.UnregisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);
        }
    }

    /// <summary>
    /// Stops answering the authoring queries until the view displays something again. A host that is disabled
    /// but keeps the view calls this, since nothing else would run on the way out.
    /// </summary>
    public void StopAnsweringQueries() => SetEditingCommandHandlers(false);

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

        if (toSelect != EntityId.None)
            m_AuthoringContext.Selection.ActiveEntityId = toSelect;
    }

    void GetActiveStyleSheetRequest(in CommandContext context)
    {
        var styleSheet = ActiveStyleSheet;
        // It is possible that the view has not yet processed a change, so in that case, we directly check on the
        // tracked VTA to see it it contains any style sheet.
        if (styleSheet == null)
        {
            using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
            GetDocumentStyleSheets(styleSheets);
            if (styleSheets.Count > 0)
                styleSheet = styleSheets[0];
        }

        GetActiveStyleSheetQuery.QueryPayload.Execute(m_AuthoringContext, CommandSources.StyleSheets, styleSheet);
    }

    void OnNewSelectorRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        FocusNewSelectorField(((RequestNewSelectorCommand)context.Command).SelectorText);
    }

    /// <summary>Brings the rows up to date with the hierarchy.</summary>
    public void Update()
    {
        PumpHierarchyView();
    }

    void PumpHierarchyView()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
        {
            using var _ = k_UpdateMarker.Auto();
            m_HierarchyView.Update();
            UpdateSearchResultsDisplay();
        }
    }

    void UpdateSearchResultsDisplay()
    {
        if (m_HierarchyView == null || m_NoResultsLabelElement == null)
            return;

        if (m_HierarchyView.UpdateNeeded)
        {
            schedule.Execute(UpdateSearchResultsDisplay);
            return;
        }

        var noEditableStyleSheets = m_StyleSheetNodes.Count == 0;
        var hasInheritedStyleSheets = m_ParentStyleSheetNodes.Count > 0;
        var noStyleSheets = noEditableStyleSheets && !hasInheritedStyleSheets;
        var noNodesDisplayed = m_HierarchyView.ViewModel.Count == 0;

        if (m_HierarchyView.Filtering)
        {
            m_NoResultsLabelElement.style.display = noNodesDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
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
        if (!m_Context.CanChangeStyleSheets || m_TrackedVTA == null)
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
        if (!m_Context.CanChangeStyleSheets || m_TrackedVTA == null)
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

    /// <summary>Writes the rows selected in the view to the context's selection.</summary>
    internal void SyncSelectionFromViewModel()
    {
        if (m_GlobalSelectionHandler != null)
            m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
        else
            m_LocalSelectionHandler?.SyncSelectionFromViewModel();
    }

    void OnPointerUp(PointerUpEvent evt) => SyncSelectionFromViewModel();

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
                SyncSelectionFromViewModel();
                break;
        }
    }

    void OnHierarchyDragPerformed(DragPerformEvent evt)
    {
        if (!m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected))
            return;
        SyncSelectionFromViewModel();
    }

    // The row under a right-click becomes the selection before the menu opens: some of its operations read the
    // selection rather than take the row.
    void OnHierarchyPointerDown(PointerDownEvent evt)
    {
        if (HierarchyViewUtility.IsContextClick((MouseButton)evt.button, evt.modifiers))
            SyncSelectionFromViewModel();
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

    internal void FocusNewSelectorField(string prefillText)
    {
        if (m_NewSelectorField == null)
            return;
        m_NewSelectorField.value = prefillText;
        m_NewSelectorField.Focus();
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
        if (m_Context.StandaloneStyleSheet != null)
            output.Add(m_Context.StandaloneStyleSheet);
        else if (m_TrackedVTA != null)
            m_TrackedVTA.GetAllReferencedStyleSheets(output);
    }

    void OnVisualTreeAssetChanged()
    {
        // VTA changed - check if the stylesheet list actually changed
        var stylesheets = new List<StyleSheet>();
        GetDocumentStyleSheets(stylesheets);

        // Early out if the stylesheet list hasn't changed
        if (StyleSheetListsEqual(stylesheets, m_LastStyleSheets))
            return;

        m_LastStyleSheets.Clear();
        m_LastStyleSheets.AddRange(stylesheets);
        RefreshStyleSheetList();
    }

    static bool StyleSheetListsEqual(List<StyleSheet> a, List<StyleSheet> b)
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

    void RefreshStyleSheetList()
    {
        // Update the hierarchy nodes in case there any pending changes.
        PumpHierarchyView();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        RefreshSessionContext();

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
        var emptyStyleSheetView = noEditableStyleSheets && !hasInheritedStyleSheets;

        ShowEmptyStyleSheetLabel(noEditableStyleSheets && !m_HierarchyView.Filtering, hasInheritedStyleSheets);
        m_NoResultsLabelElement.style.display = DisplayStyle.None;
        m_HierarchyView.style.display = emptyStyleSheetView ? DisplayStyle.None : DisplayStyle.Flex;

        if (emptyStyleSheetView)
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

    // A session can move to another document, or a sub-document's parent lose a sheet, while the same session
    // stays on display.
    void RefreshSessionContext()
    {
        // When the host sets the document, the context is the host's to change.
        if (m_Session == null)
            return;

        var source = m_Session.Document;
        if (source == m_LastSessionDocument && !HasMissingParent())
            return;

        m_LastSessionDocument = source;
        m_Context = StyleSheetsContextFactory.FromSession(m_Session);
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

    /// <summary>
    /// Re-reads what a project change can affect: a deleted parent style sheet, or a renamed parent document.
    /// </summary>
    public void RefreshAfterProjectChange()
    {
        if (m_GroupNode == HierarchyNode.Null)
            return;

        if (HasMissingParent())
            RefreshStyleSheetList();
        else
            RefreshParentDocumentNames();
    }

    void RefreshParentDocumentNames()
    {
        foreach (var node in m_ParentStyleSheetNodes.Values)
            m_Handler.RefreshStyleSheetNodeName(node);
    }

    // Settles the unsaved changes that are about to leave the document along with the style sheet, and reports
    // whether the removal may go ahead. Nothing else would ask about them: once the document stops referencing
    // it, the style sheet is no longer part of what the document saves.
    bool TrySettleStyleSheetChanges(StyleSheet styleSheet)
        => SettleStyleSheetChangesBeforeRemoval?.Invoke(styleSheet) ?? SettleChangesOf(styleSheet);

    /// <summary>
    /// Asks whether to save the unsaved changes of <paramref name="styleSheet"/> alone before it leaves its
    /// document, and settles them. False when the user cancels.
    /// </summary>
    public static bool SettleChangesOf(StyleSheet styleSheet)
    {
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
        if (!m_Context.CanChangeStyleSheets)
            return;

        var ussPath = StyleSheetAssetUtilities.DisplayOpenFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return;

        AddStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
    }

    /// <summary>
    /// Whether the style sheet on <paramref name="hierarchyNode"/> can be saved or discarded on its own from
    /// here — an editable row of a document shown outside a session, in the Main Stage, with unsaved changes. A
    /// session settles its whole document instead.
    /// </summary>
    public bool CanSettleStyleSheet(HierarchyNode hierarchyNode)
    {
        if (m_Session != null || !UIToolkitStageUtility.IsAuthoringActiveInMainStage)
            return false;

        if (!TryGetEditableStyleSheet(hierarchyNode, out var styleSheet))
            return false;

        var registry = UIAssetRegistry.LiveInstance;
        return registry != null && registry.CanSettleSingleAsset(styleSheet);
    }

    /// <summary>
    /// Writes the style sheet on <paramref name="hierarchyNode"/> and nothing else: the documents that
    /// reference it keep whatever they have unsaved.
    /// </summary>
    public void SaveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!CanSettleStyleSheet(hierarchyNode) || !TryGetEditableStyleSheet(hierarchyNode, out var styleSheet))
            return;

        UIAssetRegistry.instance.SaveSingleAsset(styleSheet, CommandSources.StyleSheets);
        StyleSheetSettled?.Invoke();
    }

    /// <summary>
    /// Reverts the style sheet on <paramref name="hierarchyNode"/> to disk, after asking, and nothing else:
    /// the documents that reference it keep whatever they have unsaved.
    /// </summary>
    public void DiscardStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!CanSettleStyleSheet(hierarchyNode) || !TryGetEditableStyleSheet(hierarchyNode, out var styleSheet))
            return;

        UIAssetSavePrompt.ConfirmAndDiscardSingleAsset(styleSheet, CommandSources.StyleSheets);

        // Re-derived by the host rather than assumed: the user may have backed out of the confirmation, in which
        // case the sheet is still unsaved.
        StyleSheetSettled?.Invoke();
    }

    bool TryGetEditableStyleSheet(HierarchyNode hierarchyNode, out StyleSheet styleSheet)
    {
        styleSheet = null;
        if (m_Handler?.Mappings == null || !m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return false;

        if (node.Rule != null || node.IsReadOnly || node.StyleSheet == null)
            return false;

        styleSheet = node.StyleSheet;
        return true;
    }

    public void RemoveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return;

        if (node.IsReadOnly || !m_Context.CanChangeStyleSheets)
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
        SyncSelectionFromViewModel();
    }

    bool TryCreateStyleSheet()
    {
        if (!m_Context.CanChangeStyleSheets)
            return false;

        var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return false;

        CreateStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
        return true;
    }

    void OnValidateCommand(ValidateCommandEvent evt)
    {
        switch (evt.commandName)
        {
            case EventCommandNames.Find:
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
            case EventCommandNames.Find:
                m_SearchField.Focus();
                evt.StopPropagation();
                break;

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
                SyncSelectionFromViewModel();
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

        // The hierarchy view's OnDelete workflow only handles StyleRules and will early out on StyleSheets. This
        // handles the case where we multi-select stylesheets with style rules.
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
