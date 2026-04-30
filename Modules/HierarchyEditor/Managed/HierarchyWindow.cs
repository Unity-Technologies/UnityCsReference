// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using static UnityEditor.SearchableEditorWindow;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Represents the Hierarchy Window in the Unity Editor. Use this class to customize the Hierarchy window, register node type handlers, and handle view events.
    /// </summary>
    [EditorWindowTitle(title = "Hierarchy")]
    public sealed class HierarchyWindow : EditorWindow, IHasCustomMenu, ISerializationCallbackReceiver, IFramableContainer, ISearchableContainer, IHierarchyWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            HierarchyPreferences.HierarchyV2WindowType = typeof(HierarchyWindow);
        }

        internal sealed class ScopedLazyClass
        {
            StateCache<HierarchyViewState> m_StateCache;
            public StateCache<HierarchyViewState> StateCache { get => m_StateCache; set => m_StateCache = value; }
            public ScopedLazyClass()
            {
                m_StateCache = new StateCache<HierarchyViewState>("Library/StateCache/HierarchyWindowStageViewState/",
            HierarchyViewState.BinarySerialization, HierarchyViewState.BinaryDeserialization);
            }
        }

        static readonly ScopedLazy<ScopedLazyClass, CodeLoadedScope> s_ScopedLazy =
            new ScopedLazy<ScopedLazyClass, CodeLoadedScope>(() => new ScopedLazyClass());

        static StateCache<HierarchyViewState> s_StateCache { get => s_ScopedLazy.Value.StateCache; set => s_ScopedLazy.Value.StateCache = value; }

        const double k_UpdateTimeout = 1000.0 / 60.0;
        const float k_BatchTime = 50f;
        const string k_HierarchyProgress = "hierarchy-progress";

        internal static readonly string s_ProjectLocalSettingsFolder = Utils.CleanPath(new DirectoryInfo("UserSettings").FullName);
        [NoAutoStaticsCleanup] // referenced in tests
        internal static string s_ProjectLocalSettingsPath = $"{s_ProjectLocalSettingsFolder}/HierarchyWindow.settings";

        static readonly string s_HierarchyToolbarUssClassName = "hierarchy-toolbar";
        static readonly string s_CreateButtonTooltip = L10n.Tr("Create new GameObject");
        static readonly string s_HierarchyToolbarButton = "hierarchy-toolbar-button";
        static readonly string s_HierarchyToolbarCreateButtonUssClassName = ToolbarButton.ussClassName + "-add";
        static readonly string s_HierarchyToolbarGoToSearchButtonName = "HierarchyGotoSearchButton";
        static readonly string s_JumpButton = "SearchJump Icon";
        static readonly string s_JumpButtonTooltip = L10n.Tr("Open query in Search Window");
        static readonly List<HierarchyWindow> s_HierarchyWindows = [];
        static HierarchyWindow s_LastInteractedHierarchy;

        const string k_HierarchyStatusBarStyleName = "hierarchy__status-bar";
        internal static readonly string s_StatusSingleNode = L10n.Tr("Path: {0}");
        internal static readonly string s_StatusMultiNode = L10n.Tr("{0} items selected");
        static readonly GUIContent s_RenamingEnabledContent = EditorGUIUtility.TrTextContent("Rename New Objects");
        static readonly GUIContent s_SyncSearchWithSceneViewContent = EditorGUIUtility.TrTextContent("Synchronize search in scene view");
        static readonly GUIContent s_NameColumnStretchableContent = EditorGUIUtility.TrTextContent("Auto stretch Name Column");

        static readonly string s_UssBasePath = "StyleSheets/HierarchyWindow";
        static readonly string s_EditorStyleSheet = $"{s_UssBasePath}/HierarchyWindow.uss";
        static readonly string s_EditorStyleSheetDark = $"{s_UssBasePath}/HierarchyWindow_dark.uss";
        static readonly string s_EditorStyleSheetLight = $"{s_UssBasePath}/HierarchyWindow_light.uss";

        Hierarchy m_Hierarchy;
        SearchFieldElement m_SearchField;
        HierarchyView m_HierarchyView;
        ProgressBar m_Progress;
        System.Diagnostics.Stopwatch m_FilterTimer;
        HierarchySearchView m_SearchView;
        VisualElement m_CreateMenuButton;
        StageNavigationView m_StageNavigationView;
        HierarchyGlobalSelectionHandler m_SelectionHandler;
        Label m_StatusBar;
        bool m_ViewStateInit;
        bool m_HasSceneHandler;
        CommandSubscriberHelper m_CommandSubscriberHelper;

        readonly List<HierarchyViewCellDescriptor> m_CellDescriptors = new();
        internal List<HierarchyViewCellDescriptor> CellDescriptors => m_CellDescriptors;

        readonly List<HierarchyViewColumnDescriptor> m_ColumnDescriptors = new();
        internal List<HierarchyViewColumnDescriptor> ColumnDescriptors => m_ColumnDescriptors;
        [SerializeField] HierarchyViewState m_ViewState;

        [SerializeField]
        string m_WindowGUID;
        [SerializeField]
        readonly EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        // Note: These internal members are used in testing.
        internal Hierarchy Hierarchy
        {
            [VisibleToOtherModules]
            get => m_Hierarchy;
        }

        internal bool m_IsUpdating;
        internal bool UpdateNeeded => m_HierarchyView.UpdateNeeded || m_IsUpdating;
        internal bool IsLocked
        {
            get => m_LockTracker.isLocked;
            set => m_LockTracker.isLocked = value;
        }
        internal SearchFieldElement SearchField => m_SearchField;

        internal static class TestHelper
        {
            public static HierarchyGlobalSelectionHandler GetSelectionHandler(HierarchyWindow hierarchyWindow)
            {
                return hierarchyWindow.m_SelectionHandler;
            }
        }

        string ISearchableContainer.SearchText
        {
            get
            {
                return m_SearchField.searchTextInput.value;
            }
            set
            {
                SetSearchText(value);
            }
        }

        HierarchyType ISearchableContainer.HierarchyType => HierarchyType.GameObjects;

        /// <summary>
        /// Gets the <see cref="HierarchyView"/> currently being displayed in this <see cref="HierarchyWindow"/>.
        /// </summary>
        public HierarchyView View => m_HierarchyView;

        /// <summary>
        /// Registers a <see cref="HierarchyNodeTypeHandler"/> for the <see cref="HierarchyWindow"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="HierarchyNodeTypeHandler"/> type to register.</typeparam>
        [VisibleToOtherModules]
        internal static void RegisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandler
        {
            // If the node type handler is already registered, we don't need to do anything.
            if (!HierarchyWindowManager.RegisterNodeTypeHandler<T>())
                return;

            // Instantiate the node type handlers for all hierarchy windows.
            var windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            foreach (var window in windows)
            {
                // This static method can be called before the hierarchy window is enabled, ensure the hierarchy is valid.
                var hierarchy = window.m_Hierarchy;
                if (hierarchy != null && hierarchy.IsCreated)
                    HierarchyWindowManager.InstantiateNodeTypeHandlers(hierarchy);
            }
        }

        /// <summary>
        /// Unregisters a hierarchy node type handler for the Hierarchy window.
        /// </summary>
        /// <remarks>
        /// The change will take effect the next time the hierarchy window is instantiated.
        /// </remarks>
        /// <typeparam name="T">The type of the hierarchy node type handler.</typeparam>
        [VisibleToOtherModules]
        internal static void UnregisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandler =>
            HierarchyWindowManager.UnregisterNodeTypeHandler<T>();

        /// <summary>
        /// Creates a new <see cref="HierarchyWindow"/>.
        /// </summary>
        public HierarchyWindow()
        {
            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).New()");
            titleContent = new GUIContent("Hierarchy");
        }

        /// <summary>
        /// Sets the search filter text in the <see cref="HierarchyWindow"/>.
        /// </summary>
        /// <param name="query">The filter query text.</param>
        public void SetSearchText(string query)
        {
            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).SetSearchText(query=\"{query}\")");
            query = query ?? string.Empty;
            var clearSearchText = !string.IsNullOrEmpty(m_HierarchyView.Filter) && string.IsNullOrEmpty(query);
            ((ISearchView)m_SearchView).SetSearchText(query, TextCursorPlacement.Default);
            m_SearchField.SetValueWithoutNotify(query);
            m_HierarchyView.Filter = query;

            SynchronizeSearchWithSearchableWindows(query);
            if (clearSearchText && !m_LockTracker.isLocked)
            {
                var selectedNodes = m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected);
                if (selectedNodes.Length > 0)
                {
                    m_HierarchyView.Frame(selectedNodes);
                }
            }
        }

        /// <summary>
        /// Delegate type for the <see cref="BindView"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> being bound.</param>
        public delegate void BindViewEventHandler(HierarchyWindow window, HierarchyView view);

        /// <summary>
        /// Delegate type for the <see cref="UnbindView"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> being unbound.</param>
        public delegate void UnbindViewEventHandler(HierarchyWindow window, HierarchyView view);

        /// <summary>
        /// Delegate type for the <see cref="BindViewItem"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> that owns the item.</param>
        /// <param name="item">The <see cref="HierarchyViewItem"/> being bound.</param>
        public delegate void BindViewItemEventHandler(HierarchyWindow window, HierarchyView view, HierarchyViewItem item);

        /// <summary>
        /// Delegate type for the <see cref="UnbindViewItem"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> that owns the item.</param>
        /// <param name="item">The <see cref="HierarchyViewItem"/> being unbound.</param>
        public delegate void UnbindViewItemEventHandler(HierarchyWindow window, HierarchyView view, HierarchyViewItem item);

        /// <summary>
        /// Delegate type for the <see cref="PopulateContextMenu"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> handling the context menu request.</param>
        /// <param name="item">The <see cref="HierarchyViewItem"/> the context menu is being created for, or <see langword="null"/> when invoked from the background.</param>
        /// <param name="menu">The <see cref="DropdownMenu"/> being populated.</param>
        public delegate void PopulateContextMenuEventHandler(HierarchyWindow window, HierarchyView view, HierarchyViewItem item, DropdownMenu menu);

        /// <summary>
        /// Delegate type for the <see cref="GetTooltip"/> event.
        /// </summary>
        /// <param name="window">The <see cref="HierarchyWindow"/> that fired the event.</param>
        /// <param name="view">The <see cref="HierarchyView"/> requesting the tooltip.</param>
        /// <param name="item">The <see cref="HierarchyViewItem"/> the tooltip is being requested for.</param>
        /// <param name="tooltip">A <see cref="StringBuilder"/> to append tooltip text to.</param>
        /// <param name="filtering">Whether the <see cref="HierarchyView"/> is currently filtering nodes.</param>
        public delegate void GetTooltipEventHandler(HierarchyWindow window, HierarchyView view, HierarchyViewItem item, StringBuilder tooltip, bool filtering);

        /// <summary>
        /// Raised when a <see cref="HierarchyView"/> is bound to the Hierarchy window.
        /// Typically used to load additional stylesheets and add styles to <see cref="HierarchyView.StyleContainer"/>.
        /// </summary>
        /// <remarks>
        /// This event provides the same functionality as <see cref="HierarchyNodeTypeHandler.OnBindView(HierarchyView)"/>
        /// but at the window level for global customization. Use <see cref="UnbindView"/> for symmetric cleanup.
        /// </remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event BindViewEventHandler BindView;

        /// <summary>
        /// Raised when a <see cref="HierarchyView"/> is about to be unbound from the <see cref="HierarchyWindow"/>.
        /// Typically used to cleanup resources associated with the view.
        /// </summary>
        /// <remarks>
        /// This event provides the same functionality as <see cref="HierarchyNodeTypeHandler.OnUnbindView(HierarchyView)"/>
        /// but at the window level for global cleanup.
        /// </remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event UnbindViewEventHandler UnbindView;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is bound to a <see cref="HierarchyView"/>. Use this event to customize the view item.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event BindViewItemEventHandler BindViewItem;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is unbound from a <see cref="HierarchyView"/>. Use this event to cleanup the view item.
        /// Note that hierarchy view item are recycled by handler, so unbinding doesn't mean destruction. For performance reasons, the recommended best practice is
        /// to not undo styles or modifications done during binding in this unbind event.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event UnbindViewItemEventHandler UnbindViewItem;

        /// <summary>
        /// Raised when a right click is handled on a node or on the background of the <see cref="HierarchyView"/>.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to create the context menu for and the <see cref="DropdownMenu"/> to populate.
        /// If the user right-clicks in empty space, the callback receives null for the view item.
        /// </remarks>
        public static event PopulateContextMenuEventHandler PopulateContextMenu;

        /// <summary>
        /// Customize the tooltip that displays when the mouse hovers the node name label.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to get the tooltip for, the
        /// StringBuilder to build the tooltip, and whether the <see cref="HierarchyView"/> is being filtered.
        /// </remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event GetTooltipEventHandler GetTooltip;

        /// <summary>
        /// Updates the Editor <see cref="Selection"/> to match the <see cref="EntityId"/>
        /// of the nodes that are flagged as selected in the Hierarchy.
        /// </summary>
        /// <remarks>
        /// Use this method to update the Editor <see cref="Selection"/> after making a change
        /// to the <see cref="View"/> selection.
        /// </remarks>
        public void UpdateEditorSelection()
            => m_SelectionHandler.SyncGlobalSelectionFromViewModel();

        void OnEnable()
        {
            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).OnEnable()");

            titleContent.image = EditorGUIUtility.LoadIconRequired(typeof(HierarchyWindow).ToString());

            s_LastInteractedHierarchy = this;
            s_HierarchyWindows.Add(this);

            m_CommandSubscriberHelper = new CommandSubscriberHelper(rootVisualElement);
            m_CommandSubscriberHelper.ValidateCommand += OnValidateCommand;
            m_CommandSubscriberHelper.ExecuteCommand += OnExecuteCommand;

            if (string.IsNullOrEmpty(m_WindowGUID))
                m_WindowGUID = GUID.Generate().ToString();

            // Load styling for the SearchField + Query Builder.
            SearchElement.AppendStyleSheets(rootVisualElement);

            // Load Hierarchy editor specific styling.
            LoadStyleSheet(rootVisualElement, EditorGUIUtility.isProSkin ? s_EditorStyleSheetDark : s_EditorStyleSheetLight);
            LoadStyleSheet(rootVisualElement, s_EditorStyleSheet);

            // Create a new hierarchy with registered node type handlers.
            m_Hierarchy = new Hierarchy();
            HierarchyWindowManager.InstantiateNodeTypeHandlers(m_Hierarchy);

            m_HierarchyView = new HierarchyView();
            m_HierarchyView.Bind += OnBindView;
            m_HierarchyView.FlagsChanged += OnHierarchyViewFlagsChanged;
            m_HierarchyView.SourceHierarchyChanged += OnSourceHierarchyChanged;
            m_HierarchyView.BindViewItem += OnBindViewItem;
            m_HierarchyView.UnbindViewItem += OnUnbindViewItem;
            m_HierarchyView.PopulateContextMenu += OnPopulateContextMenu;
            m_HierarchyView.GetTooltip += OnGetTooltip;

            m_HierarchyView.ListView.showAlternatingRowBackgrounds = HierarchyPreferences.AlternatingRowBackground
                ? AlternatingRowBackground.All : AlternatingRowBackground.None;
            m_HierarchyView.ListViewLayoutConfiguration.headerContextMenuPopulateEvent += OnHeaderContextMenu;
            m_HierarchyView.ListView.RegisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);
            m_HierarchyView.ListView.RegisterCallback<DragExitedEvent>(OnHierarchyWindowDragExited, TrickleDown.TrickleDown); // called when ESC, mouse leave window, or drag successfully finished
            m_HierarchyView.ListView.RegisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed, TrickleDown.TrickleDown);

            m_HasSceneHandler =
                m_Hierarchy.GetNodeTypeHandlerBase<HierarchyGameObjectHandler>() != null ||
                m_Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>() != null;

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.AddToClassList(s_HierarchyToolbarUssClassName);
            toolbar.Add(CreateAddToHierarchyButton());

            m_SearchView = new HierarchySearchView(this);
            m_SearchView.state.queryBuilderEnabled = HierarchyPreferences.UseQueryBuilder;
            m_SearchField = new SearchFieldElement(nameof(SearchFieldElement), m_SearchView, SearchQueryBuilderViewFlags.None);
            var addNewBlockContent = (Texture2D)EditorGUIUtility.IconContent("search_menu").image;
            m_SearchField.addNewBlockIcon = addNewBlockContent;
            toolbar.Add(m_SearchField);

            toolbar.Add(CreateGotoSearchButton());

            m_StageNavigationView = new();

            m_Progress = new();
            m_Progress.AddToClassList(k_HierarchyProgress);
            m_Progress.style.display = DisplayStyle.None;

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(m_StageNavigationView);
            rootVisualElement.Add(m_Progress);
            rootVisualElement.Add(m_HierarchyView);

            m_StatusBar = new Label();
            m_StatusBar.AddToClassList(k_HierarchyStatusBarStyleName);
            m_StatusBar.style.display = DisplayStyle.None;
            rootVisualElement.Add(m_StatusBar);

            m_FilterTimer = new();

            m_SelectionHandler = new HierarchyGlobalSelectionHandler(m_HierarchyView, m_LockTracker);

            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            StageNavigationManager.instance.stageChanging += OnStageChanging;
            StageNavigationManager.instance.stageChanged += OnStageChanged;
            PrefabStage.prefabStageReloading += OnPrefabStageReloading;
            PrefabStage.prefabStageReloaded += OnPrefabStageReloaded;

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<KeyUpEvent>(OnKeyUp);
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);

            EditorApplication.frameAndRenameNewGameObject += OnRequestFrameAndRenameNewGameObjectOrEntity;
            ClipboardUtility.cuttingGameObjects += OnCutGameObjects;
            ClipboardUtility.copyingGameObjects += OnClearCutStyle;
            ClipboardUtility.pastedGameObjects += OnClearCutStyle;
            ClipboardUtility.duplicatingGameObjects += OnClearCutStyle;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            HierarchyPreferences.UseQueryBuilder.valueChanged += OnToggleQueryBuilder;
            HierarchyPreferences.AlternatingRowBackground.valueChanged += OnToggleBackgroundStyleChange;
            HierarchyPreferences.UseNewHierarchy.valueChanged += OnUseNewHierarchyChanged;

            // Now that the UI is initialized, set the hierarchy source.
            m_HierarchyView.SetSourceHierarchy(m_Hierarchy);
            m_HierarchyView.ViewModel.QueryParser = new HierarchyEditorSearchQueryParser();

            RefreshDescriptors();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            m_SelectionHandler.SyncGlobalSelectionFromViewModel();
        }

        void OnDisable()
        {
            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).OnDisable()");
            EditorApplication.frameAndRenameNewGameObject -= OnRequestFrameAndRenameNewGameObjectOrEntity;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            s_HierarchyWindows.Remove(this);

            // Set another existing hierarchy as last interacted if available
            if (s_LastInteractedHierarchy == this)
            {
                s_LastInteractedHierarchy = null;
                if (s_HierarchyWindows.Count > 0)
                    s_LastInteractedHierarchy = s_HierarchyWindows[0];
            }

            PrefabStage.prefabStageReloading -= OnPrefabStageReloading;
            PrefabStage.prefabStageReloaded -= OnPrefabStageReloaded;
            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            StageNavigationManager.instance.stageChanging -= OnStageChanging;
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            if (m_CommandSubscriberHelper != null)
            {
                m_CommandSubscriberHelper.ValidateCommand -= OnValidateCommand;
                m_CommandSubscriberHelper.ExecuteCommand -= OnExecuteCommand;
                m_CommandSubscriberHelper.Dispose();
                m_CommandSubscriberHelper = null;
            }

            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.UnregisterCallback<KeyUpEvent>(OnKeyUp);

            // Save in memory ViewState in case of domain reload
            SaveViewState(HierarchyViewState.Content.DomainReload);

            // Persist Column setup on disk
            SaveWindowStateSettings();
            m_SelectionHandler.Dispose();

            ClipboardUtility.cuttingGameObjects -= OnCutGameObjects;
            ClipboardUtility.copyingGameObjects -= OnClearCutStyle;
            ClipboardUtility.pastedGameObjects -= OnClearCutStyle;
            ClipboardUtility.duplicatingGameObjects -= OnClearCutStyle;

            HierarchyPreferences.UseQueryBuilder.valueChanged -= OnToggleQueryBuilder;
            HierarchyPreferences.AlternatingRowBackground.valueChanged -= OnToggleBackgroundStyleChange;
            HierarchyPreferences.UseNewHierarchy.valueChanged -= OnUseNewHierarchyChanged;

            m_StageNavigationView?.Dispose();

            if (m_HierarchyView != null)
            {
                UnbindView?.Invoke(this, m_HierarchyView);

                m_HierarchyView.ListView?.UnregisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);
                m_HierarchyView.ListView?.UnregisterCallback<DragExitedEvent>(OnHierarchyWindowDragExited, TrickleDown.TrickleDown);
                m_HierarchyView.ListView?.UnregisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed, TrickleDown.TrickleDown);
                if (m_HierarchyView.ListViewLayoutConfiguration != null)
                    m_HierarchyView.ListViewLayoutConfiguration.headerContextMenuPopulateEvent -= OnHeaderContextMenu;
                m_HierarchyView.SourceHierarchyChanged -= OnSourceHierarchyChanged;
                m_HierarchyView.Bind -= OnBindView;
                m_HierarchyView.BindViewItem -= OnBindViewItem;
                m_HierarchyView.UnbindViewItem -= OnUnbindViewItem;
                m_HierarchyView.PopulateContextMenu -= OnPopulateContextMenu;
                m_HierarchyView.GetTooltip -= OnGetTooltip;
                m_HierarchyView.Dispose();
                m_HierarchyView = null;
            }

            if (m_Hierarchy != null)
            {
                if (m_Hierarchy.IsCreated)
                    m_Hierarchy.Dispose();
                m_Hierarchy = null;
            }
        }

        void OnFocus() => s_LastInteractedHierarchy = this;

        void InitViewState()
        {
            if (m_ViewState == null)
            {
                var settingsState = LoadProjectWindowState();
                // Note: since we only restore columns, we can do a synchronous SetViewState.
                ResetColumns(settingsState);
            }
            else
            {
                ResetColumns();
                SetViewState(m_ViewState);
            }

            m_HierarchyView.EnqueuePostUpdateAction(() =>
            {
                m_SelectionHandler.SyncViewModelFromGlobalSelection(frameSelection: false);
            });
            m_ViewStateInit = true;
        }

        void CreateGUI()
        {
            if (HierarchyPreferences.UseQueryBuilder && m_SearchField?.queryBuilder != null)
                m_SearchField.queryBuilder.blocksSupportExclude = false;
            InitViewState();
        }

        void OnHierarchyWindowMouseDown(PointerDownEvent evt)
        {
            // Update last interacted hierarchy when user presses mouse button (including right-click).
            s_LastInteractedHierarchy = this;

            // Synchronize global selection from view model on right-click.
            // This makes sure the element currently selected by the right click in the view
            // is set to the global selection. This is important since some context-menu operations
            // don't take parameters and instead are reading directly from Selection.activeObject or Selection.entityIds.
            if (IsRightClick((MouseButton)evt.button, evt.modifiers))
                m_SelectionHandler.SyncGlobalSelectionFromViewModel();

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

        void OnHierarchyWindowDragExited(DragExitedEvent evt)
        {
            PointerCaptureHelper.ReleaseEditorMouseCapture();
            if (Selection.entityIds.Length == 0)
                return;
            m_SelectionHandler.SyncViewModelFromGlobalSelection(frameSelection: false);
        }

        void OnHierarchyWindowDragPerformed(DragPerformEvent evt)
        {
            if (!m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected))
                return;
            m_SelectionHandler.SyncGlobalSelectionFromViewModel();
        }

        void OnSourceHierarchyChanged(HierarchyView view, Hierarchy hierarchy, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            if (m_Hierarchy == null || !m_Hierarchy.IsCreated)
                return;

            // If we are on prefab stage, set its scene as the root
            var currentStage = StageUtility.GetCurrentStage();
            if (currentStage is PrefabStage prefabStage)
            {
                var sceneHandler = m_Hierarchy.GetNodeTypeHandler<HierarchySceneHandler>();
                var sceneNode = sceneHandler.GetOrCreateNode(prefabStage.scene);
                if (sceneNode != HierarchyNode.Null)
                {
                    m_HierarchyView.ViewModel.SetRoot(in sceneNode);
                    m_HierarchyView.ViewModel.Update();
                    m_HierarchyView.UpdateListView();
                }
            }
        }

        void OnBindView(HierarchyView view)
        {
            BindView?.Invoke(this, view);
#pragma warning disable CS0618
            InitializingView?.Invoke(view);
#pragma warning restore CS0618
        }

        void OnBindViewItem(HierarchyView view, HierarchyViewItem item) => BindViewItem?.Invoke(this, view, item);

        void OnUnbindViewItem(HierarchyView view, HierarchyViewItem item) => UnbindViewItem?.Invoke(this, view, item);

        void OnPopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu) => PopulateContextMenu?.Invoke(this, view, item, menu);

        void OnGetTooltip(HierarchyView view, HierarchyViewItem item, StringBuilder tooltip, bool filtering) => GetTooltip?.Invoke(this, view, item, tooltip, filtering);

        void OnUseNewHierarchyChanged() => HierarchyPreferences.EnsureCorrectHierarchyIsInUse(this);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Note: OnBeforeSerialize is necessary for proper layout saves since OnDisable is not called.
            SaveViewState(HierarchyViewState.Content.Layout);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Nothing to do: OnEnable will properly reload State.
        }

        // Called from DockArea
        void ShowButton(Rect rect) => m_LockTracker.ShowButton(rect, "IN LockButton");

        void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            switch (mode)
            {
                case PlayModeStateChange.EnteredEditMode:
                    SetViewState(m_ViewState);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    SaveViewState(HierarchyViewState.Content.ExitPlayMode);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    SaveViewState(HierarchyViewState.Content.EnterPlayMode);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    SetViewState(m_ViewState);
                    break;
            }
        }

        void IFramableContainer.FrameObject(EntityId entityId, bool ping)
        {
            if (m_LockTracker.isLocked)
                return;

            m_HierarchyView.Update();

            // Convert EntityId to HierarchyNode
            var node = m_Hierarchy.GetNode(entityId);
            if (node == HierarchyNode.Null)
                return;

            if (ping)
                m_HierarchyView.Ping(node);
            else
                m_HierarchyView.Frame(node);
        }

        void OnRequestFrameAndRenameNewGameObjectOrEntity()
        {
            if (!HierarchyPreferences.RenameNewObjects || Selection.activeEntityId == EntityId.None)
                return;

            // All hierarchy windows need to update and frame the new GameObject.
            FrameAndRenameNewGameObjectOrEntity(Selection.activeEntityId);

            // Only the last interacted hierarchy window should get focus and handle renaming.
            if (this == s_LastInteractedHierarchy)
            {
                // Ensure this hierarchy window is focused when handling new GameObject renaming.
                // This is necessary because GOCreationCommands.Place() focuses the old HierarchyWindow.
                Focus();
            }
        }

        void UpdateStatusBar()
        {
            // Hide StatusBar when not in search mode
            // In SearchMode:
            // Display the path of a single selected items (even if not filtered).
            // In case of multi selection display the number of selected items (even if not filtered).

            var isInFilterMode = m_HierarchyView.Filtering;
            var selectedItemCount = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            m_StatusBar.style.display = isInFilterMode && selectedItemCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (isInFilterMode)
            {
                var statusMsg = "";
                if (selectedItemCount == 1)
                {
                    foreach (ref readonly var node in m_HierarchyView.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                    {
                        statusMsg = string.Format(s_StatusSingleNode, m_Hierarchy.GetPath(node));
                        break;
                    }
                }
                else if (selectedItemCount > 1)
                {
                    statusMsg = string.Format(s_StatusMultiNode, selectedItemCount);
                }
                m_StatusBar.text = statusMsg;
            }
        }

        void OnHierarchyViewFlagsChanged(HierarchyView view, HierarchyNodeFlags flags)
        {
            if (!flags.HasFlag(HierarchyNodeFlags.Selected))
                return;

            UpdateStatusBar();
        }

        void FrameAndRenameNewGameObjectOrEntity(EntityId entityId)
        {
            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).FrameAndRenameNewGameObjectOrEntity(entity={entityId})");

            if (entityId == EntityId.None || m_LockTracker.isLocked)
                return;

            // HACK: Because the frame and rename is called too early, there is no commands in the
            // hierarchy command list at this point. So we do this hack to ensure the node exists, but that
            // is bad because we only force the creation of the node without doing its normal setup process.
            var gameObjectHandler = m_Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
            if (gameObjectHandler == null)
                return; // Silent return, but this is an error, should never happen

            var node = gameObjectHandler.GetOrCreateNode(entityId);
            if (node == HierarchyNode.Null)
                return; // Silent return, but this is an error, should never happen

            // Update hierarchy to ensure the new entity node exists
            m_HierarchyView.Update();

            // Frame node and begin rename
            m_HierarchyView.Frame(in node);
            if (this == s_LastInteractedHierarchy)
                m_HierarchyView.BeginRename(in node);
        }

        void OnStageChanging(Stage previousStage, Stage currentStage)
        {
            SaveStageViewState(previousStage);
        }

        void OnStageChanged(Stage previousStage, Stage currentStage)
        {
            // Keep a reference to the current hierarchy to dispose it later
            var oldHierarchy = m_Hierarchy;

            // Create and set the new hierarchy with registered node type handlers
            m_Hierarchy = new Hierarchy();
            HierarchyWindowManager.InstantiateNodeTypeHandlers(m_Hierarchy);

            // Set the new hierarchy for the hierarchy view
            m_HierarchyView.SetSourceHierarchy(m_Hierarchy);
            m_HierarchyView.ViewModel.QueryParser = new HierarchyEditorSearchQueryParser();

            // Dispose the old hierarchy
            if (oldHierarchy != null)
            {
                if (oldHierarchy.IsCreated)
                    oldHierarchy.Dispose();
            }

            // Set the stage view state
            LoadStageViewState(currentStage);
        }

        void OnPrefabStageReloading(PrefabStage stage)
        {
            SaveStageViewState(stage);
        }

        void OnPrefabStageReloaded(PrefabStage stage)
        {
            OnStageChanged(null, stage);
        }

        void OnCutGameObjects(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0)
                return;

            // Ensure all game object nodes exists
            m_HierarchyView.Update();

            // Synchronize the cut flag with game objects received in parameter
            var viewModel = m_HierarchyView.ViewModel;
            using (var _ = new HierarchyViewModelFlagsChangeScope(viewModel))
            {
                viewModel.ClearFlags(HierarchyNodeFlags.Cut);
                foreach (var go in gameObjects)
                {
                    var node = m_Hierarchy.GetNode(go.GetEntityId());
                    viewModel.SetFlagsRecursive(in node, HierarchyNodeFlags.Cut, HierarchyTraversalDirection.Children);
                }
            }
        }

        void OnClearCutStyle(GameObject[] _)
        {
            m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Cut);
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Find:
                    m_SearchField.textField.Focus();
                    evt.StopPropagation();
                    break;

                case EventCommandNames.SelectPrefabRoot:
                {
                    Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>()?.SelectPrefabRoot(m_HierarchyView);
                    break;
                }
                case EventCommandNames.FrameSelected:
                case EventCommandNames.FrameSelectedWithLock:
                {
                    HandleFrameSelectedNodesCommand();
                    break;
                }
                case EventCommandNames.Cut:
                    m_HierarchyView.OnCut();
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
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                    evt.StopPropagation();
                    break;

                case EventCommandNames.DeselectAll:
                    m_HierarchyView.DeselectAll();
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                    evt.StopPropagation();
                    break;

                case EventCommandNames.InvertSelection:
                    m_HierarchyView.ToggleSelection();
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                    evt.StopPropagation();
                    break;

                case EventCommandNames.SelectChildren:
                    m_HierarchyView.SelectChildrenAndExpandRecursive();
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                    evt.StopPropagation();
                    break;

                case EventCommandNames.UndoRedoPerformed:
                    m_Hierarchy.SetDirty();
                    evt.StopPropagation();
                    break;
            }
        }

        void HandleFrameSelectedNodesCommand()
        {
            var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
                return;

            if (count == 1)
            {
                Span<HierarchyNode> nodes = stackalloc HierarchyNode[1];
                m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
                m_HierarchyView.Frame(in nodes[0]);
            }
            else
            {
                using var rentedNodes = new RentSpanUnmanaged<HierarchyNode>(count);
                m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, rentedNodes.Span);
                foreach (ref readonly var node in rentedNodes.Span[..^1])
                {
                    m_HierarchyView.ExpandParents(in node);
                }
                m_HierarchyView.Frame(in rentedNodes.Span[^1]);
            }
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Find:
                case EventCommandNames.SelectPrefabRoot:
                case EventCommandNames.FrameSelected:
                case EventCommandNames.FrameSelectedWithLock:
                case EventCommandNames.Cut:
                case EventCommandNames.Copy:
                case EventCommandNames.Paste:
                case EventCommandNames.Rename:
                case EventCommandNames.Duplicate:
                case EventCommandNames.Delete:
                case EventCommandNames.SoftDelete:
                case EventCommandNames.SelectAll:
                case EventCommandNames.DeselectAll:
                case EventCommandNames.InvertSelection:
                case EventCommandNames.SelectChildren:
                    evt.StopPropagation();
                    break;
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (EditorGUIUtility.HandleDefaultRenameEvent(evt.imguiEvent, this))
            {
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Escape && CutBoard.hasCutboardData)
            {
                CutBoard.Reset();
            }
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
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                    break;
            }
        }

        void OnHeaderContextMenu(ContextualMenuPopulateEvent evt, Column column)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Reset Columns", a => ResetColumns());
        }

        internal void ResetColumns(HierarchyViewState state = null)
        {
            m_HierarchyView.SetColumnDescriptors(ColumnDescriptors, CellDescriptors, state);
        }

        void OnPrefabInstanceUpdated(GameObject go)
        {
            if (go == null || !go)
                return;

            m_Hierarchy.SetDirty();
        }

        void OnUndoRedoPerformed() => m_HierarchyView.Source.SetDirty();

        internal void Update()
        {
            if (!m_HierarchyView.UpdateNeeded)
                return;

            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).Update()");

            if (m_HierarchyView.UpdateIncrementalTimed(k_UpdateTimeout))
            {
                if (m_HierarchyView.Filtering)
                {
                    m_Progress.style.display = DisplayStyle.Flex;
                    m_Progress.value = m_HierarchyView.UpdateProgress;
                }
                else
                {
                    m_Progress.style.display = DisplayStyle.None;
                }
            }
            else // We are done updating
            {
                UpdateStatusBar();
                m_Progress.style.display = DisplayStyle.None;
            }

            HierarchyLogging.Flush();
        }

        #region ViewStateManagement
        void SaveWindowStateSettings()
        {
            var viewState = m_HierarchyView.GetState(HierarchyViewState.Content.Settings);
            WriteWindowStateSettings(viewState);
        }

        internal static void WriteWindowStateSettings(HierarchyViewState state)
        {
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(s_ProjectLocalSettingsPath, json);
        }

        void SaveViewState(HierarchyViewState.Content content)
        {
            if (m_HierarchyView == null || m_HierarchyView.ViewModel == null || !m_ViewStateInit)
                return;
            m_ViewState = m_HierarchyView.GetState(content);
        }

        static HierarchyViewState LoadProjectWindowState()
        {
            HierarchyViewState viewState = null;
            if (File.Exists(s_ProjectLocalSettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(s_ProjectLocalSettingsPath);
                    viewState = JsonUtility.FromJson<HierarchyViewState>(json);
                    return viewState;
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        internal void SetViewState(HierarchyViewState viewState)
        {
            if (viewState == null)
                return;

            HierarchyLogging.Log($"HierarchyWindow({GetHashCode():X}).SetViewState(state=...)");
            if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.SearchText))
            {
                SetSearchText(viewState.SearchText);
            }

            m_HierarchyView.SetState(viewState);

            if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.ViewModelState))
            {
                m_HierarchyView.EnqueuePostUpdateAction(() =>
                {
                    if (StageUtility.GetCurrentStage() is PrefabStage)
                    {
                        // The top level object of a prefab is *most of the time* an object without a proper FID/PID. Always expand it:
                        using var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyView.ViewModel);

                        var rootChildrenCount = m_Hierarchy.GetChildrenCount(in Hierarchy.Root);
                        using var rootChildren = new RentSpanUnmanaged<HierarchyNode>(rootChildrenCount);
                        m_Hierarchy.GetChildren(Hierarchy.Root, rootChildren);
                        m_HierarchyView.ViewModel.SetFlags(rootChildren, HierarchyNodeFlags.Expanded);

                        foreach (ref readonly var node in rootChildren)
                        {
                            var childrenCount = m_Hierarchy.GetChildrenCount(in node);
                            using var children = new RentSpanUnmanaged<HierarchyNode>(childrenCount);
                            m_Hierarchy.GetChildren(in node, children);
                            m_HierarchyView.ViewModel.SetFlags(children, HierarchyNodeFlags.Expanded);
                        }
                    }
                    m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                });
            }
        }

        internal void SaveStageViewState(Stage stage)
        {
            if (stage == null)
                return;
            var key = StageUtility.CreateWindowAndStageIdentifier(m_WindowGUID, stage);
            var state = m_HierarchyView.GetState(HierarchyViewState.Content.Stage);
            s_StateCache.SetState(key, state);
        }

        internal HierarchyViewState GetStageViewState(Stage stage)
        {
            var key = StageUtility.CreateWindowAndStageIdentifier(m_WindowGUID, stage);
            return s_StateCache.GetState(key);
        }

        internal void LoadStageViewState(Stage stage)
        {
            if (stage == null)
                return;
            var state = GetStageViewState(stage);
            if (state != null)
            {
                SetViewState(state);
            }
        }
        #endregion

        void RefreshDescriptors()
        {
            m_ColumnDescriptors.Clear();
            RefreshColumnDescriptors(m_ColumnDescriptors);
            m_CellDescriptors.Clear();
            RefreshCellDescriptors(m_ColumnDescriptors, m_CellDescriptors);
        }

        void SynchronizeSearchWithSearchableWindows(string query)
        {
            SearchableEditorWindow[] windows;
            if ((windows = Resources.FindObjectsOfTypeAll<SearchableEditorWindow>()) != null && windows.Length > 0)
            {
                if (!UnityEditor.SearchService.SceneSearch.HasEngineOverride())
                {
                    var queryDesc = m_HierarchyView.ViewModel.QueryParser.ParseQuery(query);
                    query = SimplifyQuery(queryDesc);
                }
                foreach (var sw in windows)
                {
                    if (sw.m_HierarchyType != HierarchyType.Assets)
                    {
                        sw.SetSearchFilter(query, SearchMode.All, false, true);
                    }
                }
            }
        }

        internal static string SimplifyQuery(HierarchySearchQueryDescriptor queryDesc)
        {
            // Only keep the type filter and words

            var query = "";
            foreach (var filter in queryDesc.Filters)
            {
                if (filter.Name == "t")
                {
                    if (query.Length > 0)
                        query += " ";
                    query += filter.ToString();
                }
            }
            foreach (var word in queryDesc.TextValues)
            {
                if (query.Length > 0)
                    query += " ";
                query += word;
            }
            return query;
        }

        IHierarchyWindow IHierarchyWindow.LastInteractedHierarchyWindow => s_LastInteractedHierarchy;

        void IHierarchyWindow.SetExpanded(EntityId entityId, bool expanded)
        {
            if (IsLocked)
                return;

            m_HierarchyView.Update();

            var node = m_Hierarchy.GetNode(entityId);
            if (node == HierarchyNode.Null)
                return;

            if (expanded)
                m_HierarchyView.Expand(in node);
            else
                m_HierarchyView.Collapse(in node);
        }

        void LoadStyleSheet(VisualElement element, string path)
        {
            var editorSheet = EditorGUIUtility.Load(path) as StyleSheet;
            if (editorSheet == null)
            {
                Debug.LogWarning($"Cannot load uss stylesheet: {path}");
            }
            element.styleSheets.Add(editorSheet);
        }

        void RefreshColumnDescriptors(List<HierarchyViewColumnDescriptor> descs)
        {
            var param = new object[1];
            foreach (var mi in TypeCache.GetMethodsWithAttribute<HierarchyViewColumnDescriptorAttribute>())
            {
                var attr = mi.GetAttribute<HierarchyViewColumnDescriptorAttribute>();
                if (string.IsNullOrEmpty(attr.ColumnId))
                {
                    Debug.LogWarning($"Not a proper columnId for : {mi.Name}");
                    continue;
                }

                var desc = new HierarchyViewColumnDescriptor(attr.ColumnId);
                try
                {
                    param[0] = desc;
                    mi.Invoke(null, param);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error while creating Column Descriptor: {mi.Name} {e}");
                }

                if (desc != null)
                {
                    descs.Add(desc);
                }
            }
        }

        void RefreshCellDescriptors(List<HierarchyViewColumnDescriptor> columnDescs, List<HierarchyViewCellDescriptor> descs)
        {
            var param = new object[1];
            foreach (var mi in TypeCache.GetMethodsWithAttribute<HierarchyViewCellDescriptorAttribute>())
            {
                var attr = mi.GetAttribute<HierarchyViewCellDescriptorAttribute>();
                if (string.IsNullOrEmpty(attr.ColumnHint))
                {
                    Debug.LogWarning($"Empty column hint for : {mi.Name}");
                    continue;
                }

                if (attr.Handler != null && !typeof(HierarchyNodeTypeHandlerBase).IsAssignableFrom(attr.Handler))
                {
                    Debug.LogWarning($"Type provided is : {attr.Handler} is not a HierarchyNodeTypeHandler");
                    continue;
                }

                string columnId = null;
                // Look for registered columnId
                foreach (var col in columnDescs)
                {
                    if (col.Id == attr.ColumnHint)
                    {
                        columnId = col.Id;
                        break;
                    }
                }

                // Look for column Name
                if (columnId == null)
                {
                    foreach (var col in columnDescs)
                    {
                        if (col.Title == attr.ColumnHint)
                        {
                            columnId = col.Id;
                            break;
                        }
                    }
                }
                if (columnId == null)
                {
                    Debug.LogWarning($"Column Hint: {attr.ColumnHint} doesn't correspond to any columns.");
                    continue;
                }

                var desc = new HierarchyViewCellDescriptor(columnId, attr.Handler);
                try
                {
                    param[0] = desc;
                    mi.Invoke(null, param);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error while customizing Cell Descriptor: {mi.Name} {e}");
                }

                if (desc != null)
                {
                    descs.Add(desc);
                }
            }
        }

        VisualElement CreateAddToHierarchyButton()
        {
            m_CreateMenuButton = CreateButton(ShowCreateMenu, s_CreateButtonTooltip, null);
            m_CreateMenuButton.AddToClassList(s_HierarchyToolbarCreateButtonUssClassName);
            return m_CreateMenuButton;
        }

        VisualElement CreateGotoSearchButton()
        {
            var content = EditorGUIUtility.TrIconContent(s_JumpButton);
            var button = CreateButton(OpenSearchWindow, s_JumpButtonTooltip, (Texture2D)content.image);
            button.name = s_HierarchyToolbarGoToSearchButtonName;
            return button;
        }

        VisualElement CreateButton(Action action, string tooltip, Texture2D image)
        {
            var button = new ToolbarButton();
            button.RegisterCallback<ClickEvent>(evt => action());
            if (image)
            {
                button.iconImage = image;
            }
            button.tooltip = tooltip;
            button.AddToClassList(s_HierarchyToolbarButton);
            return button;
        }

        void OpenSearchWindow()
        {
            var query = m_SearchField.queryString;
            if (query.Length > 0)
            {
                var queryDescriptor = m_HierarchyView.ViewModel.QueryParser.ParseQuery(query);
                query = "";
                if (queryDescriptor.Filters.Length > 0)
                {
                    query += queryDescriptor.BuildFilterQuery();
                }
                if (queryDescriptor.TextValues.Length > 0)
                {
                    if (query.Length > 0)
                    {
                        query += $" {queryDescriptor.BuildTextQuery()}";
                    }
                    else
                    {
                        query = queryDescriptor.BuildTextQuery();
                    }
                }
            }
            UnityEditor.SearchService.OpenSearchHelper.OpenSearchInContext(this, query, "jumpButton");
        }

        // internal for testing
        internal void PopulateCreateMenu(DropdownMenu menu)
        {
            foreach (var handler in m_Hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyExtendCreateMenu extendCreateMenu)
                    extendCreateMenu.PopulateCreateMenu(menu);
            }
        }

        void ShowCreateMenu()
        {
            DropdownMenu menu = new DropdownMenu();
            PopulateCreateMenu(menu);
            EditorMenuExtensions.DoDisplayEditorMenu(menu, m_CreateMenuButton.worldBound, this.rootVisualElement);
        }

        void OnLostFocus()
        {
            m_HierarchyView?.OnLostFocus();
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Query Builder"), HierarchyPreferences.UseQueryBuilder, () => HierarchyPreferences.UseQueryBuilder.value = !HierarchyPreferences.UseQueryBuilder);
            menu.AddItem(new GUIContent("Alternating Row Background"), HierarchyPreferences.AlternatingRowBackground, () => HierarchyPreferences.AlternatingRowBackground.value = !HierarchyPreferences.AlternatingRowBackground);
            menu.AddItem(s_RenamingEnabledContent, HierarchyPreferences.RenameNewObjects, () => HierarchyPreferences.RenameNewObjects.value = !HierarchyPreferences.RenameNewObjects);

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Reset Columns"), false, () => ResetColumns());
            menu.AddItem(new GUIContent("Copy Search Text"), false, () => CopyQueryToClipboard());

        }

        internal void OnToggleQueryBuilder()
        {
            m_SearchView.state.queryBuilderEnabled = HierarchyPreferences.UseQueryBuilder;
            m_SearchField.ToggleQueryBuilder();
        }

        void OnToggleBackgroundStyleChange() => m_HierarchyView.ListView.showAlternatingRowBackgrounds = HierarchyPreferences.AlternatingRowBackground ? AlternatingRowBackground.All : AlternatingRowBackground.None;

        void CopyQueryToClipboard()
        {
            var trimmedQuery = Utils.TrimText(m_HierarchyView.Filter);
            EditorGUIUtility.systemCopyBuffer = Utils.TrimText(trimmedQuery);
        }

        internal sealed class CommandSubscriberHelper : IDisposable
        {
            readonly VisualElement m_RootVisualElement;

            public CommandSubscriberHelper(VisualElement rootVisualElement)
            {
                m_RootVisualElement = rootVisualElement;

                var visualTree = m_RootVisualElement.panel?.visualTree;
                if (visualTree != null)
                    RegisterToCommandEvents(visualTree);

                m_RootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                m_RootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            }

            void RegisterToCommandEvents(VisualElement visualTree)
            {
                visualTree.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
                visualTree.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            }

            void UnregisterFromCommandEvents(VisualElement visualTree)
            {
                visualTree.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
                visualTree.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            }

            void OnAttachedToPanel(AttachToPanelEvent evt) => RegisterToCommandEvents(evt.destinationPanel.visualTree);

            void OnDetachedFromPanel(DetachFromPanelEvent evt) => UnregisterFromCommandEvents(evt.originPanel.visualTree);

            void OnValidateCommand(ValidateCommandEvent evt) => ValidateCommand?.Invoke(evt);

            void OnExecuteCommand(ExecuteCommandEvent evt) => ExecuteCommand?.Invoke(evt);

            public void Dispose()
            {
                m_RootVisualElement.UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                m_RootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            }

            public event Action<ValidateCommandEvent> ValidateCommand;
            public event Action<ExecuteCommandEvent> ExecuteCommand;
        }

        #region Marked as obsolete warning in 6.5
        /// <summary>
        /// Raised when the <see cref="HierarchyView"/> is initializing, typically
        /// allowing to load additional stylesheets and add styles to <see cref="HierarchyView.StyleContainer"/>.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("InitializingView is deprecated. Use BindView instead, which provides direct access to the HierarchyView and has a symmetric UnbindView event.", false)]
        public static event Action<VisualElement> InitializingView;
        #endregion
    }
}
