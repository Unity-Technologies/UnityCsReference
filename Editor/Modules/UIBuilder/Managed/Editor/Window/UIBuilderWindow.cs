// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.Hierarchy;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder;

/// <summary>
/// The UI Builder: edits one UI document in a window of its own, with the style sheets, hierarchy, library,
/// viewport and inspector panes working together on a selection that stays in the window.
/// </summary>
/// <remarks>
/// <para>
/// The window owns a local <see cref="AuthoringContext"/> and, while a document is open, the
/// <see cref="UIDocumentSession"/> that edits it. Nothing it selects reaches the editor selection, so the
/// Inspector and Hierarchy windows keep showing the scene. Undo and redo still bring the window's selection back,
/// through an object the window keeps for it.
/// </para>
/// <para>
/// Changes to the document go through <see cref="UICommandQueue"/> like any other edit, so a UI Stage or another
/// window showing the same document follows them.
/// </para>
/// </remarks>
sealed partial class UIBuilderWindow : EditorWindow, IHasCustomMenu
{
    const string k_MenuPath = "Window/UI Toolkit/UI Builder";

    // Opening a document at this line opens it in the IDE instead.
    const int k_OpenInIDELineNumber = 1;
    internal const string LastStateConfigKey = "UIToolkitAuthoring.UIBuilderWindow.LastState";
    const string k_ThemeStateKeyPrefix = "UIToolkitAuthoring.UIBuilderWindow.PreviewTheme.";
    static readonly Vector2 k_InitialSize = new(1200, 800);

    // Previews a style sheet edited on its own when the project settings name no document.
    const string k_BuiltInStyleSheetPreviewPath = "UIBuilderPackageResources/UI/PreviewTemplates/StyleSheetEditingPreview.uxml";
    const string k_StyleSheetPath = "UIBuilderPackageResources/UI/UIBuilderWindow.uss";
    const string k_NewSelectorHelpTipsStyleSheetPath = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspector.uss";
    const string k_NewSelectorHelpTipsStyleSheetDarkPath = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorDark.uss";
    const string k_NewSelectorHelpTipsStyleSheetLightPath = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorLight.uss";
    const float k_NewSelectorHelpTipsOffset = 10;

    const HierarchyViewState.Content k_HierarchyStateContent = HierarchyViewState.Content.ViewModelState |
        HierarchyViewState.Content.SearchText | HierarchyViewState.Content.ScrollPosition;

    // The style sheets, hierarchy and library panes are stacked in the first column; the viewport and the inspector
    // span all the rows.
    const string k_PaneColumns = "280px 1fr 320px";
    const string k_PaneRows = "200px 1fr 260px";
    const float k_PaneMinWidth = 100;
    const float k_PaneMinHeight = 50;

    public const string UssClassName = "unity-ui-builder-window";
    public const string PanesName = UssClassName + "__panes";
    public const string FileMenuUssClassName = UssClassName + "__file-menu";
    public const string HierarchyPaneUssClassName = UssClassName + "__hierarchy-pane";
    public const string EmptyHierarchyLabelUssClassName = UssClassName + "__empty-hierarchy-label";
    public const string StyleSheetModeLabelUssClassName = UssClassName + "__style-sheet-mode-label";
    public const string PaneUssClassName = UssClassName + "__pane";
    public const string FocusedPaneUssClassName = PaneUssClassName + "--focused";
    public const string PaneHeaderUssClassName = UssClassName + "__pane-header";
    public const string PaneTitleUssClassName = UssClassName + "__pane-title";
    public const string ViewportToolbarsName = UssClassName + "__viewport-toolbars";
    public const string PanelSettingsFieldUssClassName = UssClassName + "__panel-settings-field";
    public const string NewSelectorHelpTipsUssClassName = UssClassName + "__new-selector-help-tips";

    internal static readonly string WindowTitle = L10n.Tr("UI Builder", null);
    internal static readonly string EmptyStateText = L10n.Tr("Open a UI Document (UXML) to edit it: drop one here, or use File > Open.", null);
    internal static readonly string StyleSheetsPaneTitle = L10n.Tr("Style Sheets", null);
    internal static readonly string HierarchyPaneTitle = L10n.Tr("Hierarchy", null);
    internal static readonly string LibraryPaneTitle = L10n.Tr("Library", null);
    internal static readonly string ViewportPaneTitle = L10n.Tr("Viewport", null);
    internal static readonly string InspectorPaneTitle = L10n.Tr("Inspector", null);
    static readonly string k_FileMenuText = L10n.Tr("File", null);
    static readonly string k_NewMenuText = L10n.Tr("New...", null);
    static readonly string k_OpenMenuText = L10n.Tr("Open...", null);
    static readonly string k_SaveMenuText = L10n.Tr("Save", null);
    static readonly string k_SaveAsMenuText = L10n.Tr("Save As...", null);
    static readonly string k_SaveAsDialogTitle = L10n.Tr("Save UI Document As", null);
    static readonly string k_SaveAsDialogMessage = L10n.Tr("Choose where to save a copy of the UI Document. The window then edits the copy.", null);
    static readonly string k_SaveAsInvalidPathTitle = L10n.Tr("Invalid Path", null);
    static readonly string k_SaveAsOpenDocumentMessage = L10n.Tr("Cannot save over a UI Document that is open in this window.", null);
    static readonly string k_SaveDialogTitle = L10n.Tr("Save UI Document", null);
    static readonly string k_SaveDialogMessage = L10n.Tr("Choose where to save the UI Document. The window then edits the saved file.", null);
    internal static readonly string UnsavedDocumentName = L10n.Tr("Untitled", null);
    static readonly string k_ResetLayoutMenuText = L10n.Tr("Reset Layout", null);
    static readonly string k_ShowInProjectMenuText = L10n.Tr("Show Document in Project", null);
    static readonly string k_OpenDialogTitle = L10n.Tr("Open UI Document", null);
    static readonly string k_NotInProjectMessage = L10n.Tr("Only UI Documents (UXML) inside the project can be opened.", null);
    static readonly string k_UnsavedChangesConsequence = L10n.Tr("Your changes will be lost if you don't save them.", null);
    static readonly string k_NoDocumentText = L10n.Tr("No UI Document open.", null);
    static readonly string k_StyleSheetNotInProjectMessage = L10n.Tr("Only style sheets (USS, TSS) inside the project can be opened.", null);
    static readonly string k_NoStyleSheetPreviewMessage = L10n.Tr("No UI Document to preview the style sheet with: pick one in Project Settings > UI Toolkit.", null);
    static readonly string k_StyleSheetEditingText = L10n.Tr("Editing a style sheet on its own. The UI Document only previews it.", null);
    static readonly string k_PanelSettingsTooltip = L10n.Tr("The Panel Settings that scale the preview. Only runtime UI uses them.", null);
    static readonly string k_InvalidDocumentTitle = L10n.Tr("Unable to Open the UI Document", null);
    static readonly string k_InvalidDocumentMessage = L10n.Tr("The UI Builder cannot open {0}: it has import errors. Check the Console for details.", null);
    static readonly string k_MissingSubDocumentMessage = L10n.Tr("The UI Builder cannot open the UI Document: a template it uses is missing.", null);
    static readonly string k_OkButtonText = L10n.Tr("OK", null);

    // What is opened from outside the windows goes to the one that had the focus last.
    [AutoStaticsCleanupOnCodeReload]
    static UIBuilderWindow s_LastFocusedWindow;

    [SerializeField]
    UIBuilderWindowState m_State = new();

    // Outlives the context, which is built again after every domain reload, so the undo history stays valid.
    [SerializeField]
    LocalSelectionUndoState m_SelectionUndoState;

    // The document the window created and has not saved yet. It lives as long as the window edits it.
    [SerializeField]
    VisualTreeAsset m_UnsavedDocument;

    LocalAuthoringSelection m_Selection;
    AuthoringContext m_Context;
    UIDocumentSession m_Session;

    ToolbarMenu m_FileMenu;
    Toolbar m_ViewportToolbar;
    Toolbar m_BreadcrumbsToolbar;
    ObjectField m_PanelSettingsField;
    Label m_StyleSheetModeLabel;
    StyleSheetsView m_StyleSheetsView;
    VisualElement m_HierarchyPane;
    Label m_EmptyHierarchyLabel;
    AuthoringHierarchyView m_HierarchyView;
    LibraryView m_LibraryView;
    GridViewControl m_Panes;
    VisualElement m_LibraryPane;
    UIViewportView m_ViewportView;
    AuthoringInspectorView m_InspectorView;
    VisualElement m_NewSelectorHelpTips;
    LibraryItemPreview m_LibraryItemPreview;
    Label m_NewSelectorHelpLink;
    VisualElement m_FocusedPane;

    // Taken when the document panel is replaced, to bring the rows and the selection back on the new one.
    HierarchyViewState m_StateBeforePanelSwitch;
    bool m_KeepSelectionAcrossPanelSwitch;

    (int selectedId, List<int> instanceIds, VisualTreeAsset sourceDocument)? m_PendingSelection;

    // Set on a window opened next to others: it starts on a new document, not on what the last closed one showed.
    bool m_StartOnNewDocument;

    internal AuthoringContext AuthoringContext => m_Context;
    internal UIDocumentSession Session => m_Session;
    internal UIBuilderWindowState State => m_State;
    internal StyleSheetsView StyleSheetsView => m_StyleSheetsView;
    internal AuthoringHierarchyView HierarchyView => m_HierarchyView;
    internal LibraryView LibraryView => m_LibraryView;
    internal UIViewportView ViewportView => m_ViewportView;
    internal AuthoringInspectorView InspectorView => m_InspectorView;
    internal VisualElement NewSelectorHelpTips => m_NewSelectorHelpTips;
    internal LibraryItemPreview LibraryItemPreview => m_LibraryItemPreview;
    internal Label StyleSheetModeLabel => m_StyleSheetModeLabel;
    internal ObjectField PanelSettingsField => m_PanelSettingsField;
    internal VisualTreeAsset UnsavedDocument => m_UnsavedDocument;

    /// <summary>
    /// Picks where to save the unsaved document, from its name. Tests replace the file panel with it.
    /// </summary>
    internal Func<string, string> SaveDocumentPathProvider { get; set; }

    [MenuItem(k_MenuPath)]
    static void ShowWindow() => GetOrCreateWindow();

    [InitializeOnLoadMethod]
    static void RegisterNewWindowMenuCommand()
        => UIToolkitProjectSettings.openNewBuilderWindowMenuCommand = () => ShowNewWindow();

    static UIBuilderWindow GetOrCreateWindow()
    {
        if (s_LastFocusedWindow != null)
        {
            s_LastFocusedWindow.Show();
            s_LastFocusedWindow.Focus();
            return s_LastFocusedWindow;
        }

        if (HasOpenInstances<UIBuilderWindow>())
            return GetWindow<UIBuilderWindow>();

        return CreateAndShowWindow(startOnNewDocument: false);
    }

    /// <summary>
    /// Opens another UI Builder window, on a new document of its own when other windows are open.
    /// </summary>
    internal static UIBuilderWindow ShowNewWindow() => CreateAndShowWindow(HasOpenInstances<UIBuilderWindow>());

    static UIBuilderWindow CreateAndShowWindow(bool startOnNewDocument)
    {
        var window = CreateInstance<UIBuilderWindow>();
        window.m_StartOnNewDocument = startOnNewDocument;
        // Unity only falls back to this size when it has no saved geometry for the window.
        window.position = new Rect(window.position.position, k_InitialSize);
        window.Show();
        window.Focus();
        return window;
    }

    /// <summary>
    /// Opens <paramref name="document"/> in the UI Builder window that had the focus last, creating a window if there
    /// is none.
    /// </summary>
    internal static UIBuilderWindow Open(VisualTreeAssetEditingContext document)
    {
        var window = GetOrCreateWindow();
        window.OpenDocument(document);
        return window;
    }

    /// <summary>
    /// Opens <paramref name="styleSheet"/> on its own in the UI Builder window that had the focus last, creating a
    /// window if there is none.
    /// </summary>
    internal static UIBuilderWindow Open(StyleSheet styleSheet)
    {
        var window = GetOrCreateWindow();
        window.OpenStyleSheet(styleSheet);
        return window;
    }

    [OnOpenAsset(0)]
    static bool OnOpenAsset(EntityId entityId, int line)
    {
        if (line == k_OpenInIDELineNumber)
            return false;

        var asset = EditorUtility.EntityIdToObject(entityId);
        if (asset is StyleSheet styleSheet)
        {
            if (!UIToolkitProjectSettings.enableStyleSheetEditingMode)
                return false;

            Open(styleSheet);
            return true;
        }

        if (asset is not VisualTreeAsset document)
            return false;

        if (ValidateDocument(document))
            Open(new VisualTreeAssetEditingContext(document));
        return true;
    }

    [InitializeOnLoadMethod]
    static void RegisterLoadDocumentCommandHandler()
        => UICommandQueue.RegisterHandler<LoadUIDocumentCommand>(OnLoadUIDocumentCommand);

    static void OnLoadUIDocumentCommand(in CommandContext context)
    {
        var command = (LoadUIDocumentCommand)context.Command;
        if (context.Status == CommandExecutionStatus.ValidationFailed)
        {
            if (command.Document != null && command.Document.importedWithErrors)
            {
                ValidateDocument(command.Document);
            }
            else if (command.SubDocuments is { } subDocuments
                     && subDocuments.FindIndex(d => d == null || d.importedWithErrors) is var index and >= 0)
            {
                var subDocument = subDocuments[index];
                if (subDocument == null)
                    EditorUtility.DisplayDialog(k_InvalidDocumentTitle, k_MissingSubDocumentMessage, k_OkButtonText);
                else
                    ValidateDocument(subDocument);
            }
            return;
        }

        if (context.Status != CommandExecutionStatus.Success)
            return;

        var document = command.Options != SubDocumentOptions.None && command.ContextInstances is { Count: > 0 } instances
            ? new VisualTreeAssetEditingContext(command.Document, instances.ToArray(), command.Options)
            : new VisualTreeAssetEditingContext(command.Document);

        var window = GetOrCreateWindow();
        if (window.OpenDocument(document) && command.SelectedId != -1)
            window.RequestSelection(command.SelectedId, command.SelectedInstanceIds, command.SelectedSourceDocument);
    }

    // Says why a document that did not import cannot be opened.
    static bool ValidateDocument(VisualTreeAsset document)
    {
        if (!document.importedWithErrors)
            return true;

        EditorUtility.DisplayDialog(k_InvalidDocumentTitle, string.Format(k_InvalidDocumentMessage, AssetDatabase.GetAssetPath(document)), k_OkButtonText);
        return false;
    }

    /// <summary>
    /// Selects the element of the open document that <paramref name="selectedId"/> and the ids of its enclosing
    /// instances name, once the hierarchy has its row.
    /// </summary>
    internal void RequestSelection(int selectedId, List<int> instanceIds, VisualTreeAsset sourceDocument)
    {
        // The GUI is not there yet: the document opens with it.
        if (m_Context == null)
        {
            m_PendingSelection = (selectedId, instanceIds, sourceDocument);
            return;
        }

        if (m_Session is not { IsDisposed: false } session)
            return;

        session.EnsureDocumentShown();
        var element = LoadUIDocumentCommand.FindSelectedElement(session.PanelElement.subRootVisualElement, selectedId, instanceIds, sourceDocument);
        if (element?.visualElementAsset == null)
            return;

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(m_Context, new[] { element.visualElementAsset });
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(m_Context, element.hierarchy.parent);
    }

    void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle, EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D);
        UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
        EditorApplication.fileMenuSaved += OnFileMenuSaved;
        EditorApplication.projectChanged += OnProjectChanged;
    }

    void OnDisable()
    {
        EditorApplication.fileMenuSaved -= OnFileMenuSaved;
        EditorApplication.projectChanged -= OnProjectChanged;
        UIAssetRegistry.LiveInstance?.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        // Also what a domain reload goes through: everything the window shows is rebuilt from the state.
        CaptureState();
        TearDownGUI(destroyCanvasSettings: false);
    }

    void OnFocus() => s_LastFocusedWindow = this;

    void OnDestroy()
    {
        if (s_LastFocusedWindow == this)
            s_LastFocusedWindow = null;

        m_ViewportView?.ReleaseContext(destroyCanvasSettings: true);

        if (m_SelectionUndoState != null)
        {
            Undo.ClearUndo(m_SelectionUndoState);
            DestroyImmediate(m_SelectionUndoState);
            m_SelectionUndoState = null;
        }

        // A window opened later picks up where this one stopped, unless that was a document that goes with it.
        var holdsUnsavedDocument = m_State.UnsavedRootDocument != null;
        ReleaseUnsavedDocument();
        EditorUserSettings.SetConfigValue(LastStateConfigKey, holdsUnsavedDocument ? null : JsonUtility.ToJson(m_State));
    }

    [UsedImplicitly]
    void CreateGUI()
    {
        var root = rootVisualElement;
        root.AddToClassList(UssClassName);
        if (EditorGUIUtility.Load(k_StyleSheetPath) is StyleSheet styleSheet)
            root.styleSheets.Add(styleSheet);

        // Claimed before the views are added: the canvas and the code previews resolve their context on attach.
        if (m_SelectionUndoState == null)
            m_SelectionUndoState = LocalSelectionUndoState.Create();
        m_Selection = new LocalAuthoringSelection(m_SelectionUndoState);
        m_Context = new AuthoringContext(m_Selection);
        m_Context.Claim(root);
        m_Context.Messages.RegisterHandler<RequestOpenDocumentCommand>(OnOpenDocumentRequested);

        m_StyleSheetsView = new StyleSheetsView(m_Context) { EmptyStateText = k_NoDocumentText };

        m_HierarchyPane = new VisualElement { style = { flexGrow = 1 } };
        m_HierarchyPane.AddToClassList(HierarchyPaneUssClassName);
        m_HierarchyPane.RegisterCallback<ExecuteCommandEvent>(OnHierarchyPaneExecuteCommand, TrickleDown.TrickleDown);
        m_EmptyHierarchyLabel = new Label(k_NoDocumentText)
        {
            style =
            {
                unityTextAlign = TextAnchor.MiddleCenter,
                whiteSpace = WhiteSpace.Normal,
                flexGrow = 1,
                paddingLeft = 4,
                paddingRight = 4,
            },
        };
        m_EmptyHierarchyLabel.AddToClassList(EmptyHierarchyLabelUssClassName);
        m_HierarchyPane.Add(m_EmptyHierarchyLabel);

        m_LibraryView = new LibraryView(m_Context);

        m_ViewportView = new UIViewportView(m_Context)
        {
            EmptyStateText = EmptyStateText,
            ThemeStateProvider = GetThemeState,
            DefaultThemeProvider = GetDefaultTheme,
            ShowContextTypeMenu = true,
        };

        m_InspectorView = new AuthoringInspectorView(m_Context);

        // The view data key is what keeps the track sizes across reloads.
        m_Panes = new GridViewControl(k_PaneColumns, k_PaneRows) { name = PanesName, viewDataKey = PanesName };
        m_Panes.style.flexGrow = 1;
        m_Panes.style.columnGap = GridViewComponent.DefaultLineWidth;
        m_Panes.style.rowGap = GridViewComponent.DefaultLineWidth;
        m_LibraryPane = CreatePane("library", LibraryPaneTitle, m_LibraryView);
        AddPane(CreatePane("style-sheets", StyleSheetsPaneTitle, m_StyleSheetsView), 1, 1);
        AddPane(CreatePane("hierarchy", HierarchyPaneTitle, m_HierarchyPane), 1, 2);
        AddPane(m_LibraryPane, 1, 3);
        AddPane(CreateViewportPane(), 2, 1, rowSpan: 3);
        AddPane(CreatePane("inspector", InspectorPaneTitle, m_InspectorView), 3, 1, rowSpan: 3);
        root.Add(m_Panes);

        m_NewSelectorHelpTips = CreateNewSelectorHelpTips();
        root.Add(m_NewSelectorHelpTips);

        // Over everything else: it follows the pointer over the library, and library drags over every pane.
        m_LibraryItemPreview = new LibraryItemPreview(m_LibraryView) { Canvas = m_ViewportView.Canvas };
        root.Add(m_LibraryItemPreview);

        // The code previews are panes of their own inside the viewport pane, with a header of their own.
        m_ViewportView.UxmlPreview.AddToClassList(PaneUssClassName);
        m_ViewportView.UssPreview.AddToClassList(PaneUssClassName);
        TrackPaneFocus(m_ViewportView.UxmlPreview);
        TrackPaneFocus(m_ViewportView.UssPreview);

        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        root.RegisterCallback<DragPerformEvent>(OnDragPerform);
        root.RegisterCallback<CanvasManipulatorMessageEvent>(OnCanvasManipulatorMessage);

        UICommandQueue.RegisterHandler<SetEditorExtensionModeCommand>(OnEditorExtensionModeSet);
        Undo.undoRedoPerformed += RefreshEditorExtensionState;
        UIToolkitProjectSettings.onEditorExtensionModeByDefaultChanged += RefreshEditorExtensionState;

        RestoreFromState();

        if (m_PendingSelection is { } pending)
        {
            m_PendingSelection = null;
            RequestSelection(pending.selectedId, pending.instanceIds, pending.sourceDocument);
        }
    }

    void TearDownGUI(bool destroyCanvasSettings)
    {
        if (m_Context == null)
            return;

        UICommandQueue.UnregisterHandler<SetEditorExtensionModeCommand>(OnEditorExtensionModeSet);
        Undo.undoRedoPerformed -= RefreshEditorExtensionState;
        UIToolkitProjectSettings.onEditorExtensionModeByDefaultChanged -= RefreshEditorExtensionState;

        CloseSession(destroyCanvasSettings);

        m_StyleSheetsView?.Dispose();
        m_LibraryView?.Dispose();
        m_InspectorView?.Dispose();
        m_Context.Messages.UnregisterHandler<RequestOpenDocumentCommand>(OnOpenDocumentRequested);
        m_Context.Dispose();
        m_Context = null;
        m_Selection = null;

        // The viewport stays referenced until the window is destroyed, which is when its canvas settings go.
        rootVisualElement.Clear();
        m_StyleSheetsView = null;
        m_LibraryView = null;
        m_InspectorView = null;
        m_HierarchyPane = null;
        m_EmptyHierarchyLabel = null;
        m_FileMenu = null;
        m_ViewportToolbar = null;
        m_BreadcrumbsToolbar = null;
        m_PanelSettingsField = null;
        m_StyleSheetModeLabel = null;
        m_Panes = null;
        m_LibraryPane = null;
        m_NewSelectorHelpTips = null;
        m_LibraryItemPreview = null;
        m_NewSelectorHelpLink = null;
        m_FocusedPane = null;
    }

    /// <summary>
    /// The selector cheat sheet, shown over the top left corner of the viewport while the new selector field of the
    /// style sheets pane has the focus.
    /// </summary>
    VisualElement CreateNewSelectorHelpTips()
    {
        var helpTips = new VisualElement { style = { display = DisplayStyle.None } };
        helpTips.AddToClassList(NewSelectorHelpTipsUssClassName);
        var themedStyleSheetPath = EditorGUIUtility.isProSkin ? k_NewSelectorHelpTipsStyleSheetDarkPath : k_NewSelectorHelpTipsStyleSheetLightPath;
        foreach (var path in new[] { k_NewSelectorHelpTipsStyleSheetPath, themedStyleSheetPath })
        {
            if (EditorGUIUtility.Load(path) is StyleSheet styleSheet)
                helpTips.styleSheets.Add(styleSheet);
        }
        helpTips.Add(new StyleSheetsNewSelectorHelpTips());

        // The link takes the focus on a click, so the field losing it does not hide the link before the click opens it.
        m_NewSelectorHelpLink = helpTips.Q<Label>(StyleSheetsNewSelectorHelpTips.k_HelpTooltipLabel);
        m_NewSelectorHelpLink.focusable = true;
        m_NewSelectorHelpLink.RegisterCallback<BlurEvent>(_ => HideNewSelectorHelpTips());
        m_NewSelectorHelpLink.RegisterCallback<ClickEvent>(_ => HideNewSelectorHelpTips());

        var newSelectorField = m_StyleSheetsView.NewSelectorField;
        newSelectorField.RegisterCallback<FocusEvent>(_ => ShowNewSelectorHelpTips(), TrickleDown.TrickleDown);
        newSelectorField.RegisterCallback<BlurEvent>(OnNewSelectorFieldBlur, TrickleDown.TrickleDown);
        m_ViewportView.Viewport.RegisterCallback<GeometryChangedEvent>(_ => PlaceNewSelectorHelpTips());
        return helpTips;
    }

    void OnNewSelectorFieldBlur(BlurEvent evt)
    {
        if (evt.relatedTarget != m_NewSelectorHelpLink)
            HideNewSelectorHelpTips();
    }

    void ShowNewSelectorHelpTips()
    {
        if (m_NewSelectorHelpTips == null)
            return;

        m_NewSelectorHelpTips.style.display = DisplayStyle.Flex;
        PlaceNewSelectorHelpTips();
    }

    void HideNewSelectorHelpTips()
    {
        if (m_NewSelectorHelpTips != null)
            m_NewSelectorHelpTips.style.display = DisplayStyle.None;
    }

    void PlaceNewSelectorHelpTips()
    {
        if (m_NewSelectorHelpTips == null || m_NewSelectorHelpTips.style.display == DisplayStyle.None)
            return;

        var corner = rootVisualElement.WorldToLocal(m_ViewportView.Viewport.worldBound.position);
        m_NewSelectorHelpTips.style.left = corner.x + k_NewSelectorHelpTipsOffset;
        m_NewSelectorHelpTips.style.top = corner.y + k_NewSelectorHelpTipsOffset;
    }

    ToolbarMenu CreateFileMenu()
    {
        var fileMenu = new ToolbarMenu { text = k_FileMenuText };
        fileMenu.AddToClassList(FileMenuUssClassName);
        fileMenu.menu.AppendAction(k_NewMenuText, _ => NewDocument());
        fileMenu.menu.AppendAction(k_OpenMenuText, _ => OpenWithFilePanel());
        fileMenu.menu.AppendAction(k_SaveMenuText, _ => Save(),
            _ => m_Session is { HasUnsavedChanges: true } || IsEditingUnsavedDocument ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        fileMenu.menu.AppendAction(k_SaveAsMenuText, _ => SaveAsWithFilePanel(),
            _ => CanSaveAs() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        fileMenu.menu.AppendSeparator();
        fileMenu.menu.AppendAction(k_ShowInProjectMenuText, _ => ShowDocumentInProject(), _ => ShowInProjectStatus());
        return fileMenu;
    }

    /// <summary>
    /// The viewport pane, with the File menu in place of the Save button and the breadcrumbs under the toolbar. The
    /// header says when a style sheet is edited on its own.
    /// </summary>
    /// <remarks>
    /// The toolbars are taken out of the view: it hides what it holds while no document is open, and the File menu
    /// has to stay available then.
    /// </remarks>
    VisualElement CreateViewportPane()
    {
        var viewport = m_ViewportView.Viewport;
        m_ViewportToolbar = viewport.Toolbar;
        m_BreadcrumbsToolbar = viewport.BreadcrumbsToolbar;

        m_FileMenu = CreateFileMenu();
        m_ViewportToolbar.Insert(0, m_FileMenu);

        // File > Save and Ctrl/Cmd+S save the document.
        m_ViewportToolbar.Q(className: UIViewport.ToolbarSaveButtonUssClass)?.RemoveFromHierarchy();

        // The panel settings only apply to runtime UI: the field goes between the context type and the theme.
        m_PanelSettingsField = CreatePanelSettingsField();
        var themeMenu = viewport.ThemeMenu;
        themeMenu.Insert(themeMenu.IndexOf(themeMenu.ContextTypeMenu) + 1, m_PanelSettingsField);

        var toolbars = new VisualElement { name = ViewportToolbarsName };
        toolbars.style.flexShrink = 0;
        CopyStyleSheets(m_ViewportView, toolbars);
        CopyStyleSheets(viewport, toolbars);
        toolbars.Add(m_ViewportToolbar);
        toolbars.Add(m_BreadcrumbsToolbar);

        var pane = CreatePane("viewport", ViewportPaneTitle, m_ViewportView);
        pane.Insert(1, toolbars);

        m_StyleSheetModeLabel = new Label { text = k_StyleSheetEditingText, style = { display = DisplayStyle.None } };
        m_StyleSheetModeLabel.AddToClassList(StyleSheetModeLabelUssClassName);
        m_StyleSheetModeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        m_StyleSheetModeLabel.style.flexShrink = 1;
        m_StyleSheetModeLabel.style.textOverflow = TextOverflow.Ellipsis;
        m_StyleSheetModeLabel.style.overflow = Overflow.Hidden;
        m_StyleSheetModeLabel.tooltip = k_StyleSheetEditingText;
        pane.Q(className: PaneHeaderUssClassName).Add(m_StyleSheetModeLabel);

        RefreshViewportToolbars();
        return pane;
    }

    static void CopyStyleSheets(VisualElement source, VisualElement destination)
    {
        for (var i = 0; i < source.styleSheets.count; ++i)
            destination.styleSheets.Add(source.styleSheets[i]);
    }

    /// <summary>
    /// Without a document, the File menu is all the toolbar shows. The breadcrumbs only show inside a sub-document.
    /// </summary>
    void RefreshViewportToolbars()
    {
        if (m_ViewportToolbar == null)
            return;

        var hasDocument = m_Session != null;
        foreach (var item in m_ViewportToolbar.Children())
        {
            if (item == m_FileMenu)
                continue;

            item.style.display = hasDocument ? DisplayStyle.Flex : DisplayStyle.None;
        }

        m_BreadcrumbsToolbar.style.display = IsInSubDocument() ? DisplayStyle.Flex : DisplayStyle.None;
        RefreshPanelSettingsState();
    }

    ObjectField CreatePanelSettingsField()
    {
        var field = new ObjectField { objectType = typeof(PanelSettings), allowSceneObjects = false, tooltip = k_PanelSettingsTooltip };
        field.AddToClassList(PanelSettingsFieldUssClassName);
        field.style.minWidth = 120;
        field.style.width = 180;
        field.style.flexShrink = 1;
        field.RegisterValueChangedCallback(evt => SetPanelSettings(evt.newValue as PanelSettings));
        return field;
    }

    /// <summary>
    /// Previews the document with <paramref name="panelSettings"/>, for its scaling, while it is runtime UI. The
    /// choice goes with the document: the sub-documents opened from it, Save As and a reload keep it.
    /// </summary>
    internal void SetPanelSettings(PanelSettings panelSettings)
    {
        if (m_Session is not { IsDisposed: false } || m_Session.Document.PanelSettings == panelSettings)
            return;

        m_Session.Document = WithPanelSettings(m_Session.Document, panelSettings);
        RefreshPanelSettingsState();
        m_ViewportView.RefreshPanelSettings();
    }

    PanelSettings CurrentPanelSettings => m_Session is { IsDisposed: false } ? m_Session.Document.PanelSettings : null;

    static VisualTreeAssetEditingContext WithPanelSettings(VisualTreeAssetEditingContext document, PanelSettings panelSettings)
        => document.SubDocumentOptions == SubDocumentOptions.None
            ? new VisualTreeAssetEditingContext(document.RootVisualTreeAsset, panelSettings)
            : new VisualTreeAssetEditingContext(document.RootVisualTreeAsset, document.SubDocumentPath, document.SubDocumentOptions, panelSettings);

    /// <summary>
    /// Runtime UI previews with the chosen panel settings, editor UI with none: editor UI does not scale with
    /// panel settings. The field only shows for runtime UI.
    /// </summary>
    void RefreshPanelSettingsState()
    {
        if (m_PanelSettingsField == null)
            return;

        var panelElement = m_Session is { IsDisposed: false } ? m_Session.PanelElement : null;
        var isRuntimePanel = panelElement != null && panelElement.ContextType == ContextType.Player;
        var panelSettings = CurrentPanelSettings;

        if (panelElement != null)
            panelElement.PanelSettings = isRuntimePanel ? panelSettings : null;

        m_PanelSettingsField.SetValueWithoutNotify(panelSettings);
        m_PanelSettingsField.style.display = isRuntimePanel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    bool IsInSubDocument()
        => m_Session is { IsDisposed: false, StandaloneStyleSheet: null } session &&
            session.Document.SubDocumentOptions != SubDocumentOptions.None &&
            session.Document.SubDocumentPath is { Length: > 0 };

    static VisualElement CreatePane(string name, string title, VisualElement content)
    {
        var pane = new VisualElement { name = $"{UssClassName}__{name}-pane" };
        pane.AddToClassList(PaneUssClassName);
        pane.style.flexGrow = 1;
        pane.style.overflow = Overflow.Hidden;

        var header = new Toolbar();
        header.AddToClassList(PaneHeaderUssClassName);
        header.style.flexShrink = 0;
        pane.Add(header);

        var titleLabel = new Label(title);
        titleLabel.AddToClassList(PaneTitleUssClassName);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        titleLabel.style.paddingLeft = 4;
        header.Add(titleLabel);

        content.style.flexGrow = 1;
        content.style.flexShrink = 1;
        content.style.flexBasis = 0;
        content.style.overflow = Overflow.Hidden;
        pane.Add(content);
        return pane;
    }

    // A click focuses the nearest element that can take the focus, so a click on a part of the pane that cannot take
    // it focuses the pane. Tabbing skips the pane.
    void TrackPaneFocus(VisualElement pane)
    {
        pane.focusable = true;
        pane.tabIndex = -1;
        pane.RegisterCallback<FocusInEvent>(OnPaneFocusIn, TrickleDown.TrickleDown);
        pane.RegisterCallback<FocusOutEvent>(OnPaneFocusOut, TrickleDown.TrickleDown);
    }

    // The focus trickles down from the outer panes, so a code preview takes the indicator from the viewport pane.
    void OnPaneFocusIn(FocusInEvent evt) => SetFocusedPane((VisualElement)evt.currentTarget);

    void OnPaneFocusOut(FocusOutEvent evt)
    {
        if (evt.currentTarget == m_FocusedPane)
            SetFocusedPane(null);
    }

    void SetFocusedPane(VisualElement pane)
    {
        if (m_FocusedPane == pane)
            return;

        m_FocusedPane?.RemoveFromClassList(FocusedPaneUssClassName);
        m_FocusedPane = pane;
        m_FocusedPane?.AddToClassList(FocusedPaneUssClassName);
    }

    // The minimum sizes keep a pane from being dragged out of sight: the grid stops its lines there.
    void AddPane(VisualElement pane, int column, int row, int rowSpan = 1)
    {
        pane.style.minWidth = k_PaneMinWidth;
        pane.style.minHeight = k_PaneMinHeight;
        pane.style.gridColumnStart = GridLine.AtLine(column);
        pane.style.gridRowStart = GridLine.AtLine(row);
        pane.style.gridRowEnd = GridLine.Span(rowSpan);
        TrackPaneFocus(pane);
        m_Panes.Add(pane);
    }

    DropdownMenuAction.Status ShowInProjectStatus()
        => GetDocumentProjectAsset() != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

    /// <summary>
    /// Whether the window edits the document it created, which is not saved anywhere yet.
    /// </summary>
    internal bool IsEditingUnsavedDocument
        => m_UnsavedDocument != null && m_Session is { IsDisposed: false } && m_Session.EditedVisualTreeAsset == m_UnsavedDocument;

    /// <summary>
    /// Opens <paramref name="document"/> in place of the document the window edits, after settling the unsaved
    /// changes of that one.
    /// </summary>
    /// <returns>False when the user kept the current document.</returns>
    internal bool OpenDocument(VisualTreeAssetEditingContext document)
    {
        if (document.RootVisualTreeAsset == null)
            return false;

        // The GUI is not there yet: it opens what the state says once it is.
        if (m_Context == null)
        {
            m_State.Clear();
            m_State.SetDocument(document);
            return true;
        }

        // A document that names no panel settings keeps the ones the window previews with.
        var currentPanelSettings = CurrentPanelSettings;
        if (document.PanelSettings == null && currentPanelSettings != null)
            document = WithPanelSettings(document, currentPanelSettings);

        if (m_Session != null && m_Session.Document == document)
            return true;

        if (!SettleUnsavedChanges(document.RootVisualTreeAsset))
            return false;

        CloseSession(destroyCanvasSettings: true);
        ReleaseUnsavedDocument(keep: document.RootVisualTreeAsset);
        m_State.Clear();
        ShowSession(document);
        return true;
    }

    /// <summary>
    /// Opens <paramref name="styleSheet"/> to be edited on its own, previewed on the document the project settings
    /// name (else a built-in one), after settling the unsaved changes of what the window edits.
    /// </summary>
    /// <returns>False when the user kept the current document, or nothing can preview the style sheet.</returns>
    internal bool OpenStyleSheet(StyleSheet styleSheet)
    {
        if (styleSheet == null)
            return false;

        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(styleSheet)))
        {
            ShowNotification(new GUIContent(k_StyleSheetNotInProjectMessage));
            return false;
        }

        var previewDocument = ResolveStyleSheetPreviewDocument();
        if (previewDocument == null)
        {
            ShowNotification(new GUIContent(k_NoStyleSheetPreviewMessage));
            return false;
        }

        if (m_Context == null)
        {
            m_State.Clear();
            m_State.SetStyleSheet(styleSheet);
            return true;
        }

        if (m_Session != null && m_Session.StandaloneStyleSheet == styleSheet)
            return true;

        if (!SettleUnsavedChanges())
            return false;

        var panelSettings = CurrentPanelSettings;
        CloseSession(destroyCanvasSettings: true);
        ReleaseUnsavedDocument();
        m_State.Clear();
        ShowSession(new VisualTreeAssetEditingContext(previewDocument, panelSettings), styleSheet);
        return true;
    }

    /// <summary>
    /// The document a style sheet edited on its own is previewed on: the one the project settings name, else the
    /// built-in one.
    /// </summary>
    internal static VisualTreeAsset ResolveStyleSheetPreviewDocument()
    {
        var previewDocument = UIToolkitProjectSettings.styleSheetEditingPreviewDocument;
        if (previewDocument != null && previewDocument.visualTree != null)
            return previewDocument;

        return EditorGUIUtility.Load(k_BuiltInStyleSheetPreviewPath) as VisualTreeAsset;
    }

    /// <summary>
    /// Edits a new, empty document that only lives in the window until it is saved.
    /// </summary>
    void ShowUnsavedDocument(PanelSettings panelSettings)
    {
        m_UnsavedDocument = MenuUtility.CreateUnsavedVisualTreeAsset(UnsavedDocumentName);
        ShowSession(new VisualTreeAssetEditingContext(m_UnsavedDocument, panelSettings));
    }

    /// <summary>
    /// Destroys the unsaved document, unless it is <paramref name="keep"/>. Only call this once no session edits it:
    /// nothing else can reach it, and whatever changes it has are lost.
    /// </summary>
    void ReleaseUnsavedDocument(VisualTreeAsset keep = null)
    {
        var document = m_UnsavedDocument;
        if (document == null || document == keep)
            return;

        m_UnsavedDocument = null;
        MenuUtility.DestroyUnsavedVisualTreeAsset(document);
    }

    void ShowSession(VisualTreeAssetEditingContext document, StyleSheet standaloneStyleSheet = null)
    {
        if (standaloneStyleSheet != null)
            m_State.SetStyleSheet(standaloneStyleSheet, document.PanelSettings);
        else
            m_State.SetDocument(document);

        m_Session = standaloneStyleSheet != null
            ? new UIDocumentSession(document, standaloneStyleSheet, m_Context)
            : new UIDocumentSession(document, m_Context);
        m_Session.Open();
        m_Session.DocumentCloned += OnDocumentCloned;
        m_Session.PanelElement.SubPanelReleasing += OnSubPanelReleasing;
        m_Session.PanelElement.SubPanelRecreated += OnSubPanelRecreated;

        m_EmptyHierarchyLabel.style.display = DisplayStyle.None;

        // Like the canvas header, the document row selects the canvas options.
        m_HierarchyView = new AuthoringHierarchyView(m_Session, showDocumentNode: true)
        {
            DocumentSelectionObject = () => m_ViewportView?.Canvas?.Settings,
        };
        m_HierarchyPane.Add(m_HierarchyView);

        m_ViewportView.SetContext(new SessionViewportContext(m_Session, this, PopulateBreadcrumbs));
        m_StyleSheetsView.ShowSession(m_Session);
        m_LibraryView.Refresh();

        RefreshStyleSheetModeLayout();
        RefreshEditorExtensionState();
        RefreshUnsavedChangesState();
        RefreshViewportToolbars();
    }

    // A style sheet edited on its own goes with a preview the library cannot add to.
    void RefreshStyleSheetModeLayout()
    {
        if (m_LibraryPane == null)
            return;

        var styleSheetMode = m_Session is { IsDocumentReadOnly: true };
        m_StyleSheetModeLabel.style.display = styleSheetMode ? DisplayStyle.Flex : DisplayStyle.None;

        // The grid collapses the row of a hidden pane.
        m_LibraryPane.style.display = styleSheetMode ? DisplayStyle.None : StyleKeyword.Null;
    }

    void CloseSession(bool destroyCanvasSettings)
    {
        if (m_Session == null)
            return;

        if (destroyCanvasSettings)
            m_ViewportView.SetContext(null);
        else
            m_ViewportView.ReleaseContext(destroyCanvasSettings: false);

        m_StyleSheetsView.ClearDisplay();

        m_HierarchyView.Dispose();
        m_HierarchyView.RemoveFromHierarchy();
        m_HierarchyView = null;
        m_EmptyHierarchyLabel.style.display = DisplayStyle.Flex;

        m_Session.DocumentCloned -= OnDocumentCloned;
        if (m_Session.PanelElement != null)
        {
            m_Session.PanelElement.SubPanelReleasing -= OnSubPanelReleasing;
            m_Session.PanelElement.SubPanelRecreated -= OnSubPanelRecreated;
        }

        m_Session.Dispose();
        m_Session = null;
        using (m_Selection.SuspendUndo())
            m_Context.Selection.Clear();
        m_LibraryView.Refresh();

        RefreshStyleSheetModeLayout();
        RefreshEditorExtensionState();
        RefreshUnsavedChangesState();
        RefreshViewportToolbars();
        RefreshOtherWindowsUnsavedChangesState();
    }

    // Changes another window also holds are its to prompt for once this window lets go of them.
    void RefreshOtherWindowsUnsavedChangesState()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<UIBuilderWindow>())
        {
            if (window != this && window.m_Context != null)
                window.RefreshUnsavedChangesState();
        }
    }

    void RestoreFromState()
    {
        var startOnNewDocument = m_StartOnNewDocument;
        m_StartOnNewDocument = false;
        if (!m_State.HasDocument && !startOnNewDocument)
        {
            var lastState = EditorUserSettings.GetConfigValue(LastStateConfigKey);
            if (!string.IsNullOrEmpty(lastState))
                JsonUtility.FromJsonOverwrite(lastState, m_State);
        }

        // The preview is resolved again: the project settings may name another one by now.
        StyleSheet standaloneStyleSheet = null;
        VisualTreeAssetEditingContext document;
        if (m_State.TryGetStyleSheet(out var styleSheet, out var styleSheetPanelSettings) &&
            ResolveStyleSheetPreviewDocument() is { } previewDocument)
        {
            standaloneStyleSheet = styleSheet;
            document = new VisualTreeAssetEditingContext(previewDocument, styleSheetPanelSettings);
        }
        else if (!m_State.TryGetDocument(out document))
        {
            // Nothing to come back to: the window starts on a document of its own, like a new one.
            m_State.Clear();
            ReleaseUnsavedDocument();
            ShowUnsavedDocument(panelSettings: null);
            return;
        }

        // An unsaved document the state no longer names was replaced before the GUI was there.
        ReleaseUnsavedDocument(keep: document.RootVisualTreeAsset);

        // The theme state outlives a domain reload, but not the editor.
        if (m_State.ThemeKey != null)
            SessionState.SetString(GetThemeStateKey(standaloneStyleSheet != null ? standaloneStyleSheet : document.RootVisualTreeAsset), m_State.ThemeKey);

        var hierarchyState = m_State.HierarchyViewState;
        var rules = m_State.ResolveSelectedRules();
        ShowSession(document, standaloneStyleSheet);
        RestoreSelection(hierarchyState, rules);
    }

    /// <summary>
    /// Brings back the rows and the selection: the style rules when the state names some, else the element
    /// rows the hierarchy state has selected.
    /// </summary>
    void RestoreSelection(HierarchyViewState hierarchyState, List<StyleRule> rules)
    {
        if (m_HierarchyView == null)
            return;

        var view = m_HierarchyView.HierarchyView;
        if (hierarchyState != null)
        {
            view.SetState(hierarchyState);
            if (hierarchyState.ValidContent.HasFlag(HierarchyViewState.Content.SearchText))
                m_HierarchyView.SearchField.SetValueWithoutNotify(hierarchyState.SearchText ?? string.Empty);
        }

        // The style sheets pane builds its rows on the spot. Selecting there makes the element hierarchy queue a
        // refresh, which it refuses while it runs its queued actions, so this is not one of them.
        if (rules.Count > 0)
        {
            using (m_Selection.SuspendUndo())
                m_StyleSheetsView.SelectAndScrollTo(rules);
            return;
        }

        if (hierarchyState == null)
            return;

        // After the rows the state names exist, and the state has flagged them.
        var hierarchyView = m_HierarchyView;
        view.EnqueuePostUpdateAction(() =>
        {
            if (hierarchyView != m_HierarchyView)
                return;

            using (m_Selection.SuspendUndo())
                hierarchyView.SelectionHandler.SyncSelectionFromViewModel();
        });
    }

    void CaptureState()
    {
        if (m_Session == null || m_Session.IsDisposed)
            return;

        if (m_Session.StandaloneStyleSheet != null)
            m_State.SetStyleSheet(m_Session.StandaloneStyleSheet, m_Session.Document.PanelSettings);
        else
            m_State.SetDocument(m_Session.Document);
        m_State.ThemeKey = PreviewThemeState.ToKey(m_ViewportView.SelectedTheme);
        m_State.HierarchyViewState = m_HierarchyView?.HierarchyView.GetState(k_HierarchyStateContent);
        m_State.SetSelectedRules(m_Context.Selection.Objects);
    }

    /// <summary>
    /// Simulates what a domain reload does to the window: everything is torn down and built again from the
    /// serialized state alone.
    /// </summary>
    internal void SimulateReload()
    {
        OnDisable();
        m_State = JsonUtility.FromJson<UIBuilderWindowState>(JsonUtility.ToJson(m_State));
        OnEnable();
        CreateGUI();
    }

    /// <summary>
    /// Simulates closing the window, without the prompt for unsaved changes: what a window opened later gets
    /// back is recorded, and the rest is let go. <see cref="SimulateOpen"/> then stands for that later window.
    /// </summary>
    internal void SimulateClose()
    {
        OnDisable();
        OnDestroy();
        m_State = new UIBuilderWindowState();
    }

    internal void SimulateOpen()
    {
        OnEnable();
        CreateGUI();
    }

    // A style sheet edited on its own keeps its theme apart from the document that previews it.
    PreviewThemeState GetThemeState(VisualTreeAsset document)
        => new(m_Session is { StandaloneStyleSheet: { } styleSheet } ? GetThemeStateKey(styleSheet) : GetThemeStateKey(document));

    // A document without panel settings previews like the project's runtime UI would look by default.
    internal static StyleSheet GetDefaultTheme()
    {
        var projectTheme = UIToolkitProjectSettings.defaultRuntimeTheme;
        return projectTheme != null ? projectTheme : ThemeUtility.FindProjectDefaultRuntimeThemeAssetOrDefault();
    }

    // The window keys its own preview theme: it offers editor themes, which the UI Viewport window cannot show.
    internal static string GetThemeStateKey(UnityEngine.Object asset)
        => k_ThemeStateKeyPrefix + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

    /// <summary>
    /// The path from the root document to the edited one. Every crumb but the last opens its document.
    /// </summary>
    void PopulateBreadcrumbs(UIViewport viewport)
    {
        viewport.ClearBreadcrumbs();
        if (m_Session == null || m_Session.IsDisposed)
            return;

        if (m_Session.StandaloneStyleSheet is { } styleSheet)
        {
            var name = Path.GetFileName(AssetDatabase.GetAssetPath(styleSheet));
            viewport.PushBreadcrumb(m_Session.HasUnsavedChanges ? name + "*" : name, EditorGUIUtility.Load("StyleSheet Icon") as Texture2D);
            return;
        }

        var document = m_Session.Document;
        var icon = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
        var path = document.SubDocumentPath;
        var depth = document.SubDocumentOptions == SubDocumentOptions.None || path == null ? 0 : path.Length;

        for (var i = 0; i <= depth; ++i)
        {
            var asset = i == 0 ? document.RootVisualTreeAsset : path[i - 1].ResolveTemplate();
            if (asset == null)
                continue;

            if (i == depth)
            {
                viewport.PushBreadcrumb(m_Session.HasUnsavedChanges ? asset.name + "*" : asset.name, icon);
                break;
            }

            var target = i == 0
                ? new VisualTreeAssetEditingContext(document.RootVisualTreeAsset, document.PanelSettings)
                : new VisualTreeAssetEditingContext(document.RootVisualTreeAsset, path[..i], document.SubDocumentOptions, document.PanelSettings);
            var label = UIAssetRegistry.instance.IsDirty(asset) ? asset.name + "*" : asset.name;
            viewport.PushBreadcrumb(label, icon, () => OpenDocument(target));
        }
    }

    void OnOpenDocumentRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        // The request comes from a menu of the hierarchy or the canvas, which opening the document rebuilds.
        var document = ((RequestOpenDocumentCommand)context.Command).Document;
        rootVisualElement.schedule.Execute(() => OpenDocument(document));
    }

    void OnSubPanelReleasing(PanelElement panelElement)
    {
        // The elements are cloned again in the new panel: only their rows can tell which ones were selected.
        m_StateBeforePanelSwitch = m_HierarchyView?.HierarchyView.GetState(k_HierarchyStateContent);
        m_KeepSelectionAcrossPanelSwitch = HoldsNonElementSelection();
    }

    void OnSubPanelRecreated(PanelElement panelElement)
    {
        // A theme switch between runtime and editor themes is what replaces the panel.
        RefreshPanelSettingsState();

        var state = m_StateBeforePanelSwitch;
        m_StateBeforePanelSwitch = null;
        if (state == null || m_HierarchyView == null)
            return;

        if (m_KeepSelectionAcrossPanelSwitch)
            m_HierarchyView.HierarchyView.SetState(state);
        else
            RestoreSelection(state, new List<StyleRule>());
    }

    // Style rules and sheets are assets: a new panel does not change them, nor their selection.
    bool HoldsNonElementSelection()
    {
        foreach (var selected in m_Context.Selection.Objects)
        {
            if (selected is StyleRuleSelection or StyleSheetSelection)
                return true;
        }

        return false;
    }

    void OnEditorExtensionModeSet(in CommandContext context)
    {
        if (context.Status == CommandExecutionStatus.Success)
            RefreshEditorExtensionState();
    }

    // A reload from disk or a discard can bring back another editor extension setting.
    void OnDocumentCloned(UIDocumentSession session) => RefreshEditorExtensionState();

    /// <summary>
    /// Follows the document's editor extension setting, which the context type menu, undo, a reload of the
    /// document or the project's default can change: the preview switches between runtime and editor UI.
    /// </summary>
    void RefreshEditorExtensionState()
    {
        if (m_Context == null)
            return;

        m_ViewportView.RefreshContextType();
        RefreshPanelSettingsState();
    }

    bool TryOpenProjectDocument(VisualTreeAsset document)
    {
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(document)))
        {
            ShowNotification(new GUIContent(k_NotInProjectMessage));
            return false;
        }

        return OpenDocument(new VisualTreeAssetEditingContext(document));
    }

    void OpenWithFilePanel()
    {
        var path = UIToolkitProjectSettings.enableStyleSheetEditingMode
            ? EditorUtility.OpenFilePanelWithFilters(k_OpenDialogTitle, Application.dataPath, new[] { L10n.Tr("UI Documents and Style Sheets", null), "uxml,uss,tss" })
            : EditorUtility.OpenFilePanel(k_OpenDialogTitle, Application.dataPath, "uxml");
        if (string.IsNullOrEmpty(path))
            return;

        var projectPath = FileUtil.GetProjectRelativePath(path.Replace('\\', '/'));
        if (!string.IsNullOrEmpty(projectPath) && AssetDatabase.LoadMainAssetAtPath(projectPath) is StyleSheet styleSheet)
        {
            OpenStyleSheet(styleSheet);
            return;
        }

        var document = string.IsNullOrEmpty(projectPath) ? null : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(projectPath);
        if (document == null)
        {
            ShowNotification(new GUIContent(k_NotInProjectMessage));
            return;
        }

        TryOpenProjectDocument(document);
    }

    // An explicit action, the only one that reaches the editor selection.
    void ShowDocumentInProject()
    {
        var asset = GetDocumentProjectAsset();
        if (asset == null)
            return;

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // The unsaved document is in no project folder.
    UnityEngine.Object GetDocumentProjectAsset()
    {
        if (m_Session is not { IsDisposed: false })
            return null;

        UnityEngine.Object asset = m_Session.StandaloneStyleSheet != null ? m_Session.StandaloneStyleSheet : m_Session.EditedVisualTreeAsset;
        return asset != null && EditorUtility.IsPersistent(asset) ? asset : null;
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        if (GetDocumentProjectAsset() != null)
            menu.AddItem(new GUIContent(k_ShowInProjectMenuText), false, ShowDocumentInProject);
        else
            menu.AddDisabledItem(new GUIContent(k_ShowInProjectMenuText));

        menu.AddItem(new GUIContent(k_ResetLayoutMenuText), false, ResetLayout);
    }

    /// <summary>
    /// Gives the panes their default sizes back. The document, the selection and the canvas framing are kept.
    /// </summary>
    internal void ResetLayout()
    {
        // The pane sizes live in the view data, which the panes only read when they are created.
        CaptureState();
        TearDownGUI(destroyCanvasSettings: false);
        ClearPersistentViewData();
        CreateGUI();
    }

    /// <summary>
    /// Edits a new, empty document, after settling the unsaved changes of the current one. The new document only
    /// lives in the window: saving it is what creates its file.
    /// </summary>
    internal bool NewDocument()
    {
        if (m_Context == null || !SettleUnsavedChanges())
            return false;

        var panelSettings = CurrentPanelSettings;
        CloseSession(destroyCanvasSettings: true);
        ReleaseUnsavedDocument();
        m_State.Clear();
        ShowUnsavedDocument(panelSettings);
        return true;
    }

    void SaveAsWithFilePanel()
    {
        var document = m_Session?.EditedVisualTreeAsset;
        if (document == null)
            return;

        var currentPath = AssetDatabase.GetAssetPath(document);
        var folder = string.IsNullOrEmpty(currentPath) ? "Assets" : Path.GetDirectoryName(currentPath);
        var path = EditorUtility.SaveFilePanelInProject(k_SaveAsDialogTitle, document.name, "uxml", k_SaveAsDialogMessage, folder);
        if (string.IsNullOrEmpty(path))
            return;

        if (!CanSaveAs(path))
        {
            EditorUtility.DisplayDialog(k_SaveAsInvalidPathTitle, k_SaveAsOpenDocumentMessage, L10n.Tr("OK", null));
            return;
        }

        SaveAs(path);
    }

    /// <summary>
    /// Whether the edited document can be saved elsewhere. Not a sub-document of the unsaved document: its copy
    /// would replace the unsaved document, which has to be saved first.
    /// </summary>
    bool CanSaveAs()
        => m_Session is { IsDisposed: false, IsDocumentReadOnly: false } session &&
            (m_UnsavedDocument == null || session.EditedVisualTreeAsset == m_UnsavedDocument);

    /// <summary>
    /// Whether the edited document may be saved to <paramref name="assetPath"/>: not over a document the window
    /// has open, which includes the documents above the edited one.
    /// </summary>
    internal bool CanSaveAs(string assetPath)
    {
        if (!CanSaveAs() || string.IsNullOrEmpty(assetPath))
            return false;

        var document = m_Session.Document;
        if (AssetDatabase.GetAssetPath(document.RootVisualTreeAsset) == assetPath)
            return false;

        if (document.SubDocumentPath != null)
        {
            foreach (var template in document.SubDocumentPath)
            {
                var templateDocument = template?.ResolveTemplate();
                if (templateDocument != null && AssetDatabase.GetAssetPath(templateDocument) == assetPath)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes the edited document, unsaved changes included, to <paramref name="assetPath"/>, and edits that copy
    /// from then on. The original goes back to what is on disk, unless another view still holds its changes. The
    /// unsaved document is gone once its copy is written.
    /// </summary>
    internal bool SaveAs(string assetPath)
    {
        if (!CanSaveAs(assetPath))
            return false;

        var startTime = DateTime.UtcNow;
        var original = m_Session.EditedVisualTreeAsset;
        var content = UIAssetDirtyTracker.ComputeContentText(original);
        if (content == null)
            return false;

        var folder = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllText(assetPath, content);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        var copy = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        if (copy == null)
            return false;

        using (ListPool<UnityEngine.Object>.Get(out var unsaved))
        {
            CollectUnsavedAssets(unsaved);
            if (original != m_UnsavedDocument && unsaved.Contains(original))
                UIAssetRegistry.instance.DiscardSingleAsset(original, CommandSources.Menus);
        }

        // The style sheets keep their unsaved changes: the copy uses the same ones.
        var panelSettings = m_Session.Document.PanelSettings;
        CloseSession(destroyCanvasSettings: true);
        ReleaseUnsavedDocument();
        m_State.Clear();
        var context = new VisualTreeAssetEditingContext(copy, panelSettings);
        ShowSession(context);
        UIBuilderAnalytics.SendSaveEvent(startTime, context);
        return true;
    }

    /// <summary>
    /// Saves the edited document. The unsaved document asks where first, and is edited from its file afterwards.
    /// </summary>
    internal bool Save()
    {
        if (IsEditingUnsavedDocument)
            return SaveUnsavedDocumentWithFilePanel();

        if (m_Session is not { IsDisposed: false, HasUnsavedChanges: true })
            return false;

        var saved = SaveSession();
        RefreshUnsavedChangesState();
        m_ViewportView.RefreshMetadata();
        return saved;
    }

    bool SaveUnsavedDocumentWithFilePanel()
    {
        var path = SaveDocumentPathProvider != null
            ? SaveDocumentPathProvider(m_UnsavedDocument.name)
            : EditorUtility.SaveFilePanelInProject(k_SaveDialogTitle, m_UnsavedDocument.name, "uxml", k_SaveDialogMessage, "Assets");
        return !string.IsNullOrEmpty(path) && SaveUnsavedDocument(path);
    }

    /// <summary>
    /// Writes the unsaved document to <paramref name="assetPath"/> and edits that file from then on, with the
    /// style sheets it uses saved too, as they are with any document.
    /// </summary>
    internal bool SaveUnsavedDocument(string assetPath)
    {
        if (!IsEditingUnsavedDocument || !SaveAs(assetPath))
            return false;

        var saved = !m_Session.HasUnsavedChanges || m_Session.Save();
        RefreshUnsavedChangesState();
        m_ViewportView.RefreshMetadata();
        return saved;
    }

    /// <summary>
    /// Saves every unsaved change the window holds. The unsaved document asks where, and is first brought back
    /// from the sub-document the window may edit in it.
    /// </summary>
    /// <returns>False when a save failed, or the user did not say where to save the unsaved document.</returns>
    bool SaveAll()
    {
        if (m_Session is not { IsDisposed: false })
            return true;

        var registry = UIAssetRegistry.instance;
        if (m_UnsavedDocument != null && registry.IsDirty(m_UnsavedDocument))
        {
            var saved = true;
            if (!IsEditingUnsavedDocument)
            {
                saved = SaveSession();
                var panelSettings = CurrentPanelSettings;
                CloseSession(destroyCanvasSettings: true);
                m_State.Clear();
                ShowSession(new VisualTreeAssetEditingContext(m_UnsavedDocument, panelSettings));
            }

            return SaveUnsavedDocumentWithFilePanel() && saved;
        }

        // An unchanged unsaved document is not worth a file: only what it uses is saved.
        if (IsEditingUnsavedDocument)
        {
            using var _ = ListPool<UnityEngine.Object>.Get(out var unsaved);
            CollectUnsavedAssets(unsaved);

            var saved = true;
            foreach (var asset in unsaved)
            {
                if (asset != m_UnsavedDocument && registry.IsDirty(asset))
                    saved &= registry.SaveAsset(asset);
            }

            return saved;
        }

        return SaveSession();
    }

    // Saves what the session edits, and reports the saved document to the analytics.
    bool SaveSession()
    {
        var startTime = DateTime.UtcNow;
        var saved = m_Session.Save();
        if (saved)
            UIBuilderAnalytics.SendSaveEvent(startTime, m_Session.Document);
        return saved;
    }

    // The unsaved document is left alone: saving it means asking where.
    void OnFileMenuSaved()
    {
        if (!IsEditingUnsavedDocument && m_Session is { IsDisposed: false, HasUnsavedChanges: true })
            Save();
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (!evt.actionKey || evt.keyCode != KeyCode.S || evt.shiftKey || evt.altKey)
            return;

        evt.StopPropagation();

        // A field being edited commits its value when it loses focus, which has to happen before the save.
        rootVisualElement.focusController?.focusedElement?.Blur();
        rootVisualElement.schedule.Execute(() => Save());
    }

    // The panes take the commands meant for them while they have focus. What reaches the window goes to the
    // element hierarchy, which acts on the same selection, but for Find, which goes to the search of the focused pane.
    void OnValidateCommand(ValidateCommandEvent evt)
    {
        if (evt.commandName == EventCommandNames.Find ||
            (m_HierarchyView != null && AuthoringHierarchyView.AcceptsCommand(evt.commandName)))
            evt.StopPropagation();
    }

    void OnExecuteCommand(ExecuteCommandEvent evt)
    {
        if (evt.commandName == EventCommandNames.Find)
        {
            FocusSearchField();
            evt.StopPropagation();
            return;
        }

        if (m_HierarchyView == null)
            return;

        if (evt.commandName == EventCommandNames.FrameSelected)
        {
            RequestFramingCommand.Execute(m_Context, CommandSources.Viewport, element: null, orientToFace: false);
            evt.StopPropagation();
            return;
        }

        if (m_HierarchyView.ExecuteCommand(evt.commandName))
            evt.StopPropagation();
    }

    // The hierarchy takes the frame command to bring its selected rows into view, and the canvas frames the same
    // selection.
    void OnHierarchyPaneExecuteCommand(ExecuteCommandEvent evt)
    {
        if (m_HierarchyView != null && evt.commandName == EventCommandNames.FrameSelected)
            RequestFramingCommand.Execute(m_Context, CommandSources.Hierarchy, element: null, orientToFace: false);
    }

    // The code previews search with the pane they show the code of: the element hierarchy for the UXML, the style
    // sheets for the USS. The panes without a search, and the inspector with nothing to inspect, search the hierarchy.
    void FocusSearchField()
    {
        var pane = m_FocusedPane;
        if (pane == m_ViewportView.UssPreview || (pane != null && pane.Contains(m_StyleSheetsView)))
        {
            m_StyleSheetsView.SearchField.Focus();
            return;
        }

        if (pane == m_LibraryPane)
        {
            m_LibraryView.FocusSearchField();
            return;
        }

        if (pane != null && pane.Contains(m_InspectorView) && m_InspectorView.FocusSearchField())
            return;

        m_HierarchyView?.SearchField.Focus();
    }

    // With a document open, a UXML dropped on the canvas or the hierarchy is added to it as a template instance.
    void OnDragUpdated(DragUpdatedEvent evt)
    {
        if (m_Session == null && TryGetDraggedDocument() != null)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            evt.StopPropagation();
        }
    }

    void OnDragPerform(DragPerformEvent evt)
    {
        if (m_Session != null || TryGetDraggedDocument() is not { } document)
            return;

        DragAndDrop.AcceptDrag();
        TryOpenProjectDocument(document);
        evt.StopPropagation();
    }

    static VisualTreeAsset TryGetDraggedDocument()
    {
        foreach (var reference in DragAndDrop.objectReferences)
        {
            if (reference is VisualTreeAsset document)
                return document;
        }

        return null;
    }

    void OnCanvasManipulatorMessage(CanvasManipulatorMessageEvent evt)
        => ShowNotification(new GUIContent(evt.Message, EditorGUIUtility.FindTexture("console.warnicon")), 4);

    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        RefreshUnsavedChangesState();
        if (m_Context != null)
            m_ViewportView.RefreshMetadata();
    }

    // A rename or move of the document or style sheet on screen changes the canvas header and the breadcrumbs.
    void OnProjectChanged()
    {
        if (m_Context != null)
            m_ViewportView.RefreshMetadata();
    }

    /// <summary>
    /// The unsaved changes only this window holds. A document a UI Stage also edits keeps its changes there when
    /// the window lets go of it. The unsaved document is the window's alone, even while it is only shown around a
    /// sub-document.
    /// </summary>
    void CollectUnsavedAssets(List<UnityEngine.Object> results)
    {
        var session = m_Session;
        var registry = UIAssetRegistry.LiveInstance;
        if (session == null || session.IsDisposed || registry == null)
            return;

        registry.CollectDirtyAssetsHeldOnlyBy(owner => ReferenceEquals(owner, session), results);

        if (m_UnsavedDocument != null && !results.Contains(m_UnsavedDocument) && registry.IsDirty(m_UnsavedDocument))
            results.Insert(0, m_UnsavedDocument);
    }

    /// <summary>
    /// Asks what to do with the unsaved changes the window holds, before it edits a document of
    /// <paramref name="nextRoot"/>, and carries the answer out.
    /// </summary>
    /// <returns>False when the user kept the current document, or a save failed.</returns>
    bool SettleUnsavedChanges(VisualTreeAsset nextRoot = null)
    {
        using var _ = ListPool<UnityEngine.Object>.Get(out var unsaved);
        CollectUnsavedAssets(unsaved);

        // Another part of the unsaved document keeps it open, changes included.
        if (m_UnsavedDocument != null && nextRoot == m_UnsavedDocument)
            unsaved.Remove(m_UnsavedDocument);

        var settled = m_UnsavedDocument != null && unsaved.Contains(m_UnsavedDocument)
            ? SettleWithUnsavedDocument(unsaved)
            : UIAssetSavePrompt.AskAndResolve(unsaved, k_UnsavedChangesConsequence, allowCancel: true);
        RefreshUnsavedChangesState();
        return settled;
    }

    // The registry cannot save a document that has no file, so the window carries the answer out itself.
    bool SettleWithUnsavedDocument(List<UnityEngine.Object> unsaved)
    {
        switch (UIAssetSavePrompt.AskWithoutResolving(unsaved, k_UnsavedChangesConsequence, allowCancel: true))
        {
            case UIAssetSaveChoice.Cancel:
                return false;

            case UIAssetSaveChoice.Save:
                return SaveAll();

            case UIAssetSaveChoice.Discard:
            {
                // The unsaved document has no file to go back to: it is dropped with the session.
                var registry = UIAssetRegistry.instance;
                foreach (var asset in unsaved)
                {
                    if (asset != null && asset != m_UnsavedDocument && registry.IsDirty(asset))
                        registry.DiscardAsset(asset);
                }

                return true;
            }

            default:
                return true;
        }
    }

    /// <summary>
    /// Keeps the editor's unsaved-changes state of the window in step with the registry, so closing the window
    /// raises the standard prompt over <see cref="SaveChanges"/> and <see cref="DiscardChanges"/>.
    /// </summary>
    void RefreshUnsavedChangesState()
    {
        using var _ = ListPool<UnityEngine.Object>.Get(out var unsaved);
        CollectUnsavedAssets(unsaved);

        if (unsaved.Count > 0)
            saveChangesMessage = UIAssetSavePrompt.BuildMessage(unsaved, k_UnsavedChangesConsequence);

        hasUnsavedChanges = unsaved.Count > 0;
    }

    public override void SaveChanges()
    {
        SaveAll();

        // Re-derived rather than base.SaveChanges(): a save that did not land keeps the window open.
        RefreshUnsavedChangesState();
    }

    public override void DiscardChanges()
    {
        m_Session?.DiscardChanges();
        RefreshUnsavedChangesState();
    }

    void Update() => UpdateViews();

    /// <summary>
    /// Brings the hierarchy-based panes up to date, which the window does on every editor update.
    /// </summary>
    internal void UpdateViews()
    {
        m_HierarchyView?.Update();
        m_StyleSheetsView?.Update();
    }
}
