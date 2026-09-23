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
using UnityEditorInternal;
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
    public sealed partial class HierarchyWindow : EditorWindow, IHasCustomMenu, ISerializationCallbackReceiver, IFramableContainer, ISearchableContainer, IHierarchyWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            HierarchyPreferences.HierarchyV2WindowType = typeof(HierarchyWindow);
        }

        internal partial class ScopedLazyClass
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
        static readonly string s_CreateButtonTooltip = L10n.Tr("Create new GameObject", null);
        static readonly string s_HierarchyToolbarButton = "hierarchy-toolbar-button";
        static readonly string s_HierarchyToolbarCreateButtonUssClassName = ToolbarButton.ussClassName + "-add";
        static readonly string s_HierarchyToolbarGoToSearchButtonName = "HierarchyGotoSearchButton";
        static readonly string s_JumpButton = "SearchJump Icon";
        static readonly string s_JumpButtonTooltip = L10n.Tr("Open query in Search Window", null);
        [AutoStaticsCleanupOnCodeReload]
        static List<HierarchyWindow> s_HierarchyWindows = [];
        [AutoStaticsCleanupOnCodeReload]
        static HierarchyWindow s_LastInteractedHierarchy;

        const string k_HierarchyStatusBarStyleName = "hierarchy__status-bar";
        internal static readonly string s_StatusSingleNode = L10n.Tr("Path: {0}", null);
        internal static readonly string s_StatusMultiNode = L10n.Tr("{0} items selected", null);
        static readonly GUIContent s_RenamingEnabledContent = L10n.TextContent("Rename New Objects", null, null, null);
        static readonly GUIContent s_SyncSearchWithSceneViewContent = L10n.TextContent("Synchronize search in scene view", null, null, null);
        static readonly GUIContent s_NameColumnStretchableContent = L10n.TextContent("Auto stretch Name Column", null, null, null);

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
        [NonSerialized] bool m_SavedStateForDomainReload = false; // Used to prevent non-deterministic view-state persistence on OnDisable during a domain reload.
        bool m_HasSceneHandler;
        HierarchyViewState m_StateBeforeSharedHierarchyChanged;
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

            public static int GetHierarchyUndoId(HierarchyWindow hierarchyWindow)
            {
                return HierarchyStageStack.CurrentUndoId;
            }

            public static void TriggerPlayModeStateChanged(HierarchyWindow hierarchyWindow, PlayModeStateChange mode) =>
                hierarchyWindow.OnPlayModeStateChanged(mode);

            public static void SetCachedStageViewState(HierarchyWindow hierarchyWindow, Stage stage, HierarchyViewState viewState)
            {
                var key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.m_WindowGUID, stage);
                s_StateCache.SetState(key, viewState);
            }

            public static void ClearCachedStageViewState(HierarchyWindow hierarchyWindow, Stage stage)
            {
                var key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.m_WindowGUID, stage);
                s_StateCache.RemoveState(key);
            }

            public static void OnEnable(HierarchyWindow hierarchyWindow) => hierarchyWindow.OnEnable();
            public static void OnDisable(HierarchyWindow hierarchyWindow) => hierarchyWindow.OnDisable();
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
        /// Gets the <see cref="HierarchyWindow"/> the user interacted with most recently, or <see langword="null"/>
        /// when none is open.
        /// </summary>
        internal static HierarchyWindow LastInteractedWindow
        {
            [VisibleToOtherModules]
            get => s_LastInteractedHierarchy;
        }

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

            // Back-fill the handler onto the hierarchies already shared by the open windows.
            HierarchyStageStack.InstantiateNodeTypeHandlers();
        }

        /// <summary>
        /// Unregisters a hierarchy node type handler for the Hierarchy window.
        /// </summary>
        /// <remarks>
        /// The handler's existing nodes are not removed, because they can parent nodes owned by other handlers.
        /// Call <see cref="HierarchyStageStack.Reload"/> to drop them.
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
            if (clearSearchText)
                UpdateSearchStatus();

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
        /// <example>
        /// The following example draws visual connector lines in the Hierarchy window to show the parent and child relationships between GameObjects. It uses `BindView` to register a handler that adds connector lines with hover highlighting and click-to-collapse functionality. 
        ///
        /// The example requires three USS files: `Connectors.uss` for the base styles, `Connectors_dark.uss` for the Dark theme, and `Connectors_light.uss` for the Light theme.
        ///
        /// To use this example, save the script and USS files in a folder called `Assets/Editor/Connectors`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_dark.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_dark.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_light.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_light.uss"/>
        /// </example>
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
        /// <example>
        /// The following example draws visual connector lines in the Hierarchy window to show the parent and child relationships between GameObjects. It uses `UnbindView` to clean up the per-view handler registered by `BindView`. 
        ///
        /// The example requires three USS files: `Connectors.uss` for the base styles, `Connectors_dark.uss` for the Dark theme, and `Connectors_light.uss` for the Light theme.
        ///
        /// To use this example, save the script and USS files in a folder called `Assets/Editor/Connectors`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_dark.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_dark.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_light.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_light.uss"/>
        /// </example>
        [AutoStaticsCleanupOnCodeReload]
        public static event UnbindViewEventHandler UnbindView;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is bound to a <see cref="HierarchyView"/>. Use this event to customize the view item.
        /// </summary>
        /// <example>
        /// The following example changes the icon a GameObject uses in the Hierarchy window if it has a specified tag. It uses `BindViewItem` to register a handler that customizes each item as the window binds it, and applies a custom USS icon class to GameObjects with the `Favorite` tag. 
        ///
        /// The example requires a USS file called `ChangeNodeIcon.uss` and a tag called `Favorite`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/ChangeNodeIcon`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Copy the styles from the USS example on this page. Save them in a USS file called `ChangeNodeIcon.uss` in the same `Assets/Editor/ChangeNodeIcon` folder. 
        ///3. Create a tag called `Favorite`: select a GameObject, open the **Tag** dropdown in the **Inspector** window, and select **Add Tag**.
        ///4. Assign the `Favorite` tag to a GameObject to change the icon it displays.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeNodeIcon/ChangeNodeIcon.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `ChangeNodeIcon.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeNodeIcon/ChangeNodeIcon.uss"/>
        /// </example>
        [AutoStaticsCleanupOnCodeReload]
        public static event BindViewItemEventHandler BindViewItem;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is unbound from a <see cref="HierarchyView"/>. Use this event to clean up the view item.
        /// Note that hierarchy view items are recycled by their handler, so unbinding doesn't mean destruction. For performance reasons, the recommended best practice is
        /// to not undo styles or modifications done during binding in this unbind event.
        /// </summary>
        /// <example>
        /// The following example adds a button next to a GameObject in the Hierarchy window if that GameObject is a prefab instance. You can select the button to locate and highlight the prefab asset in the **Project** window. It uses `UnbindViewItem` to remove custom buttons added to `HierarchyViewItem.RightCustomContainer` during binding, because view items are pooled and reused. 
        ///
        /// The example requires a USS file called `PrefabActionButtons.uss`.
        /// To use this example, save the script and USS file in a folder called `Assets/Editor/PrefabActionButtons`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/PrefabActionButtons/PrefabActionButtons.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `PrefabActionButtons.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/PrefabActionButtons/PrefabActionButtons.uss"/>
        /// </example>
        [AutoStaticsCleanupOnCodeReload]
        public static event UnbindViewItemEventHandler UnbindViewItem;

        /// <summary>
        /// Raised when a right click is handled on a node or on the background of the <see cref="HierarchyView"/>.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to create the context menu for and the <see cref="DropdownMenu"/> to populate.
        /// If the user right-clicks in empty space, the callback receives null for the view item.
        /// </remarks>
        /// <example>
        /// The following example creates a context menu item that collapses all nodes in the Hierarchy window except the paths to the selected items. The action appears in the **Hierarchy Samples** submenu of the context menu. It uses `PopulateContextMenu` to add the context menu action.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CollapseOthers/CollapseOthers.cs"/>
        /// </example>
        [AutoStaticsCleanupOnCodeReload]
        public static event PopulateContextMenuEventHandler PopulateContextMenu;

        /// <summary>
        /// Customize the tooltip that displays when the mouse hovers the node name label.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to get the tooltip for, the
        /// StringBuilder to build the tooltip, and whether the <see cref="HierarchyView"/> is being filtered.
        /// </remarks>
        /// <example>
        /// The following example adds a custom tooltip that displays in the Hierarchy window when you hover over any GameObject that has a custom component named `Notes` attached to it. It uses `GetTooltip` to display the text of the `Notes` component as the tooltip. The example requires a USS file called `CustomTooltip.uss` and a custom MonoBehaviour script called `Notes.cs`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/CustomTooltip`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Copy the styles from the USS example on this page. Save them in a USS file called `CustomTooltip.uss` in the same `Assets/Editor/CustomTooltip` folder. 
        ///3. Save the `Notes.cs` script outside of the `Editor` folder, because MonoBehaviour scripts in an `Editor` folder can't be attached to GameObjects.
        ///4. Add the `Notes` component to a GameObject.
        ///5. In the **Inspector** window, enter text in the **Note** field of the `Notes` component.
        ///6. In the Hierarchy window, hover over the name of the GameObject to display the text as a tooltip. A small overlay indicator also displays in the corner of the icon of any GameObject that has a note.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CustomTooltip/CustomTooltip.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `CustomTooltip.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CustomTooltip/CustomTooltip.uss"/>
        /// </example>
        /// <example>
        /// The following example shows the `Notes` component that the CustomTooltip example uses.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Runtime/Notes.cs"/>
        /// </example>

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

            // Use the hierarchy shared by every window for the current stage. It outlives this window.
            m_Hierarchy = HierarchyStageStack.Current;

            m_HierarchyView = new HierarchyView();
            m_HierarchyView.Bind += OnBindView;
            m_HierarchyView.FlagsChanged += OnHierarchyViewFlagsChanged;
            m_HierarchyView.SourceHierarchyChanged += OnSourceHierarchyChanged;
            m_HierarchyView.BindViewItem += OnBindViewItem;
            m_HierarchyView.UnbindViewItem += OnUnbindViewItem;
            m_HierarchyView.PopulateContextMenu += OnPopulateContextMenu;
            m_HierarchyView.ContextMenuRequested += OnContextMenuRequested;
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
            // GUIUtility.pixelsPerPoint is only reliable while a view is being painted, so when
            // OnEnable runs it may not reflect the actual screen scale yet and IconContent would
            // resolve the low-resolution variant. Force the @2x variant so the loaded texture does
            // not depend on load timing; it is also pixel-exact at the 22x16pt display size (UUM-147596).
            var addNewBlockContent = (Texture2D)EditorGUIUtility.IconContent("search_menu@2x").image;
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

            StageNavigationManager.instance.stageChanging += OnStageChanging;
            PrefabStage.prefabStageReloading += OnPrefabStageReloading;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            // HierarchyStageStack owns the stage transitions now, and raises these once the shared data for the
            // new stage is ready, which is also after the previous stages are closed. Restoring view state any
            // earlier would recover entityIds from GlobalObjectIds against a stage that is going away.
            HierarchyStageStack.Changing += OnSharedHierarchyChanging;
            HierarchyStageStack.Changed += OnSharedHierarchyChanged;

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<KeyUpEvent>(OnKeyUp);
            rootVisualElement.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            EditorApplication.frameAndRenameNewGameObject += OnRequestFrameAndRenameNewGameObjectOrEntity;
            ClipboardUtility.cuttingGameObjects += OnCutGameObjects;
            ClipboardUtility.copyingGameObjects += OnClearCutStyle;
            ClipboardUtility.pastedGameObjects += OnClearCutStyle;
            ClipboardUtility.duplicatingGameObjects += OnClearCutStyle;
            CutBoard.cleared += OnCutboardCleared;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.enterPlayModePreStart += OnEnterPlayModePreStart;

            HierarchyPreferences.UseQueryBuilder.valueChanged += OnToggleQueryBuilder;
            HierarchyPreferences.AlternatingRowBackground.valueChanged += OnToggleBackgroundStyleChange;
            EditorSettings.useLegacyHierarchyChanged += OnUseLegacyHierarchyChanged;
            HierarchyPreferences.GameObjectIconMode.valueChanged += OnGameObjectIconModeChanged;

            // Now that the UI is initialized, set the hierarchy source. The flattened is shared too, so this
            // window only packs its own view model.
            m_HierarchyView.SetSourceHierarchyFlattened(m_Hierarchy, HierarchyStageStack.CurrentFlattened);
            m_HierarchyView.ViewModel.QueryParser = new HierarchyEditorSearchQueryParser();

            RefreshDescriptors();

            RestoreCutFlagsFromCutBoard();

            HierarchyAnalytics.AddWindow(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            // Be certain to register the event only once.
            // In case the window is not visible when starting the editor, opening it would trigger
            // both OnEnable and OnAttachedToPanel, registering the event twice if not unregistered first.
            HierarchyPreferences.GameObjectIconMode.valueChanged -= OnGameObjectIconModeChanged;
            HierarchyPreferences.GameObjectIconMode.valueChanged += OnGameObjectIconModeChanged;
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            HierarchyPreferences.GameObjectIconMode.valueChanged -= OnGameObjectIconModeChanged;
        }


        void OnEnterPlayModePreStart()
        {
            SetViewState(m_ViewState);
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
            EditorApplication.enterPlayModePreStart -= OnEnterPlayModePreStart;

            s_HierarchyWindows.Remove(this);

            // Set another existing hierarchy as last interacted if available
            if (s_LastInteractedHierarchy == this)
            {
                s_LastInteractedHierarchy = null;
                if (s_HierarchyWindows.Count > 0)
                    s_LastInteractedHierarchy = s_HierarchyWindows[0];
            }

            HierarchyStageStack.Changing -= OnSharedHierarchyChanging;
            HierarchyStageStack.Changed -= OnSharedHierarchyChanged;

            PrefabStage.prefabStageReloading -= OnPrefabStageReloading;
            StageNavigationManager.instance.stageChanging -= OnStageChanging;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            if (m_CommandSubscriberHelper != null)
            {
                m_CommandSubscriberHelper.ValidateCommand -= OnValidateCommand;
                m_CommandSubscriberHelper.ExecuteCommand -= OnExecuteCommand;
                m_CommandSubscriberHelper.Dispose();
                m_CommandSubscriberHelper = null;
            }

            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel, TrickleDown.TrickleDown);
            rootVisualElement.UnregisterCallback<KeyUpEvent>(OnKeyUp);

            // Save in memory ViewState in case of domain reload — unless OnBeforeAssemblyReload already did
            // it. Managed OnDisable order during a domain unload is non-deterministic, so we need to save the state
            // before the current stage's OnDisable is called, otherwise the stage might have already removed their nodes.
            if (!m_SavedStateForDomainReload)
                SaveViewState(HierarchyViewState.Content.DomainReload);

            // Persist Column setup on disk
            SaveWindowStateSettings();
            m_SelectionHandler.Dispose();

            ClipboardUtility.cuttingGameObjects -= OnCutGameObjects;
            ClipboardUtility.copyingGameObjects -= OnClearCutStyle;
            ClipboardUtility.pastedGameObjects -= OnClearCutStyle;
            ClipboardUtility.duplicatingGameObjects -= OnClearCutStyle;
            CutBoard.cleared -= OnCutboardCleared;

            HierarchyPreferences.UseQueryBuilder.valueChanged -= OnToggleQueryBuilder;
            HierarchyPreferences.AlternatingRowBackground.valueChanged -= OnToggleBackgroundStyleChange;
            EditorSettings.useLegacyHierarchyChanged -= OnUseLegacyHierarchyChanged;
            HierarchyPreferences.GameObjectIconMode.valueChanged -= OnGameObjectIconModeChanged;

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
                m_HierarchyView.ContextMenuRequested -= OnContextMenuRequested;
                m_HierarchyView.GetTooltip -= OnGetTooltip;
                m_HierarchyView.Dispose();
                m_HierarchyView = null;
            }

            // The hierarchy and its flattened belong to HierarchyStageStack, so only drop our reference.
            m_Hierarchy = null;

            HierarchyAnalytics.RemoveWindow(this);
        }

        void OnBeforeAssemblyReload()
        {
            // A domain reload is about to begin. Persist the view state now before any of the managed objects are
            // unloaded.
            SaveViewState(HierarchyViewState.Content.DomainReload);
            m_SavedStateForDomainReload = true;
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
        }

        void OnContextMenuRequested(HierarchyViewItem item)
        {
            // Synchronize global selection from view model on right-click (context menu request).
            // This makes sure the element currently selected by the right click in the view
            // is set to the global selection. This is important since some context-menu operations
            // don't take parameters and instead are reading directly from Selection.activeObject or Selection.entityIds.
            m_SelectionHandler.SyncGlobalSelectionFromViewModel();
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

        void OnBindView(HierarchyView view) => BindView?.Invoke(this, view);

        void OnBindViewItem(HierarchyView view, HierarchyViewItem item) => BindViewItem?.Invoke(this, view, item);

        void OnUnbindViewItem(HierarchyView view, HierarchyViewItem item) => UnbindViewItem?.Invoke(this, view, item);

        void OnPopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu) => PopulateContextMenu?.Invoke(this, view, item, menu);

        void OnGetTooltip(HierarchyView view, HierarchyViewItem item, StringBuilder tooltip, bool filtering) => GetTooltip?.Invoke(this, view, item, tooltip, filtering);

        void OnUseLegacyHierarchyChanged() => HierarchyPreferences.EnsureCorrectHierarchyIsInUse(this);

        void OnGameObjectIconModeChanged()
        {
            m_HierarchyView.ListView.RefreshItems();
            Repaint();
        }

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
                    m_HierarchyView.ListView.animation?.SkipAnimation();
                    SaveViewState(HierarchyViewState.Content.ExitPlayMode);
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    // A reload on play mode enter invalidates the CutBoard but not the Cut node flag,
                    // so reset both to keep them in sync when a reload will happen.
                    if (WillDomainReloadOnEnterPlayMode || WillSceneReloadOnEnterPlayMode)
                    {
                        ClipboardUtility.ResetCutboardAndRepaintHierarchyWindows();
                        m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Cut);
                    }
                    m_HierarchyView.ListView.animation?.SkipAnimation();
                    SaveViewState(HierarchyViewState.Content.EnterPlayMode);
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    // State restoration happens in OnEnterPlayModePreStart to ensure it happens as early as possible.
                    break;
            }
        }

        static bool WillDomainReloadOnEnterPlayMode
            => !EditorSettings.enterPlayModeOptionsEnabled
                || !EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);

        static bool WillSceneReloadOnEnterPlayMode
            => !EditorSettings.enterPlayModeOptionsEnabled
                || !EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableSceneReload);

        void IFramableContainer.FrameObject(EntityId entityId, bool ping)
        {
            if (m_LockTracker.isLocked)
                return;

            m_HierarchyView.Update();

            // Convert EntityId to HierarchyNode. If the entity is a component, fall back to its
            // owning GameObject — matching the behaviour of the legacy SceneHierarchy.
            var node = m_Hierarchy.GetNodeFromEntityId(entityId);
            if (node == HierarchyNode.Null)
                node = m_Hierarchy.GetNodeFromEntityId(InternalEditorUtility.GetGameObjectEntityIdFromComponent(entityId));
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

        void UpdateSearchStatus()
        {
            UpdateSearchResultsCount();
            UpdateStatusBar();
            m_Progress.style.display = DisplayStyle.None;
        }

        void UpdateSearchResultsCount()
        {
            m_SearchField.ResultsCount = m_HierarchyView.Filtering ? m_HierarchyView.ViewModel.SearchMatchCount : null;
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

        void OnSharedHierarchyChanging()
        {
            // The shared data is about to be disposed with no replacement bound yet, so snapshot what this window
            // is showing and stop referencing it before it goes away.
            if (m_HierarchyView?.ViewModel != null && m_ViewStateInit)
                m_StateBeforeSharedHierarchyChanged = m_HierarchyView.GetState(HierarchyViewState.Content.Stage);

            m_HierarchyView?.SetSourceHierarchyFlattened(null, null);
            m_Hierarchy = null;
        }

        void OnSharedHierarchyChanged(HierarchyStageChange change)
        {
            if (m_HierarchyView == null)
                return;

            m_Hierarchy = HierarchyStageStack.Current;
            m_HierarchyView.SetSourceHierarchyFlattened(m_Hierarchy, HierarchyStageStack.CurrentFlattened);
            m_HierarchyView.ViewModel.QueryParser = new HierarchyEditorSearchQueryParser();

            if (change != HierarchyStageChange.DataRebuilt)
            {
                m_StateBeforeSharedHierarchyChanged = null;

                var currentStage = StageUtility.GetCurrentStage();

                // The search text follows the stage navigation like a stack: drilling into a newly opened stage
                // starts a new empty search, while returning to a stage already in the stage history (e.g. going
                // back up the breadcrumbs) restores the search it had when it was left (UUM-142149). A reload
                // keeps the user in the same stage, so its search is always restored.
                var restoreSearchText = change == HierarchyStageChange.StageReloaded ||
                    currentStage.setSelectionAndScrollWhenBecomingCurrentStage;

                LoadStageViewState(currentStage, restoreSearchText);
                return;
            }

            // Same stage, rebuilt data: restore what this window was showing rather than the persisted per-stage
            // state, which belongs to a stage transition.
            if (m_StateBeforeSharedHierarchyChanged != null)
            {
                SetViewState(m_StateBeforeSharedHierarchyChanged);
                m_StateBeforeSharedHierarchyChanged = null;
            }
        }

        void OnPrefabStageReloading(PrefabStage stage)
        {
            SaveStageViewState(stage);
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
                    SetCutFlagRecursive(viewModel, go.GetEntityId());
            }
        }

        void RestoreCutFlagsFromCutBoard()
        {
            var cutTransformsSpan = CutBoard.cutTransformsSpan;
            if (cutTransformsSpan.IsEmpty)
                return;

            m_HierarchyView.Update();

            var viewModel = m_HierarchyView.ViewModel;
            using (var _ = new HierarchyViewModelFlagsChangeScope(viewModel))
            {
                viewModel.ClearFlags(HierarchyNodeFlags.Cut);
                foreach (var transform in cutTransformsSpan)
                {
                    if (transform != null)
                        SetCutFlagRecursive(viewModel, transform.gameObject.GetEntityId());
                }
            }
        }

        void SetCutFlagRecursive(HierarchyViewModel viewModel, EntityId entityId)
        {
            var node = m_Hierarchy.GetNodeFromEntityId(entityId);
            if (node == HierarchyNode.Null)
                return;
            viewModel.SetFlagsRecursive(in node, HierarchyNodeFlags.Cut, HierarchyTraversalDirection.Children);
        }

        void OnClearCutStyle(GameObject[] _) => OnCutboardCleared();

        void OnCutboardCleared()
        {
            m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Cut);
        }

        // The user commands this window responds to. The UndoRedoPerformed broadcast is handled in
        // its OnExecuteCommand case; unknown/custom commands must not cancel an ongoing rename.
        static bool AcceptsCommand(string commandName)
        {
            switch (commandName)
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
                    return true;

                default:
                    return false;
            }
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            // A command being executed (e.g. Duplicate via Cmd/Ctrl+D) is an implicit request to
            // stop editing the current name, even though the rename TextField still has focus.
            if (AcceptsCommand(evt.commandName))
                m_HierarchyView.CancelRename();

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
                    // The undo/redo may restructure the hierarchy under the active rename; discard the draft.
                    m_HierarchyView.CancelRename();
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
            if (AcceptsCommand(evt.commandName))
                evt.StopPropagation();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (EditorGUIUtility.HandleDefaultRenameEvent(evt.imguiEvent, this))
            {
                evt.StopPropagation();
            }
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (!CutBoard.hasCutboardData)
                return;
            CutBoard.Reset();
        }

        void OnKeyUp(KeyUpEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
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
                UpdateSearchStatus();
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

            // Restore view model state synchronously, to ensure all nodes are properly updated before restoring the rest of the view state.
            var hasViewModelState = viewState.ValidContent.HasFlag(HierarchyViewState.Content.ViewModelState) &&
                viewState.ViewModelState != null &&
                viewState.ViewModelState.Length > 0;
            if (hasViewModelState)
            {
                // Update first to ensure all nodes exist before restoring view model state
                m_HierarchyView.UpdateData();

                // Set the view model state
                m_HierarchyView.ViewModel.SetState(viewState.ViewModelState);
            }

            // Restore search text state synchronously, to ensure the search filter is properly applied before restoring the rest of the view state.
            if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.SearchText))
                SetSearchText(viewState.SearchText);

            // Queue the rest to happen on next frame (all except the view model state)
            m_HierarchyView.SetState(new HierarchyViewState
            {
                ValidContent = viewState.ValidContent & ~HierarchyViewState.Content.ViewModelState,
                SearchText = viewState.SearchText,
                ScrollPositionX = viewState.ScrollPositionX,
                ScrollPositionY = viewState.ScrollPositionY,
                Columns = viewState.Columns
            });

            if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.ViewModelState))
            {
                m_HierarchyView.EnqueuePostUpdateAction(() =>
                {
                    // Always force expand the Preview Scene root "Prefab Mode In Context" node when restoring
                    // a view model state. This node cannot be serialized since it is recreated every time, and should always be expanded.
                    // This is in line with the legacy hierarchy behavior, which always expands this node when entering a prefab stage in context.
                    // When there is no view model state, all nodes will be expanded by default when entering a prefab stage.
                    // For prefabs in isolation, all nodes represent game objects that are persisted and serializable, therefore their view model
                    // state can be restored correctly.
                    if (hasViewModelState && StageUtility.GetCurrentStage() is PrefabStage { mode: PrefabStage.Mode.InContext } ps)
                    {
                        // The dummy "Prefab Mode In Context" node only appears if the "openedFromInstance" game object is under
                        // a valid transform. Therefore, we have to validate that it does in fact exist to expand it.
                        var contentRootParent = ps.prefabContentsRoot.transform.parent;
                        var contentRootParentGo = contentRootParent?.gameObject;
                        if (contentRootParentGo != null && contentRootParentGo.name == PrefabUtility.kDummyPrefabStageRootObjectName)
                        {
                            // Handler and node should exist at this point since this is a post update action.
                            var gameObjectHandler = m_Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
                            var node = gameObjectHandler.GetOrCreateNode(contentRootParentGo);
                            m_HierarchyView.ViewModel.SetFlags(in node, HierarchyNodeFlags.Expanded);
                        }
                    }
                    if (m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected) || !GlobalSelectionIsOnlyAssets())
                        m_SelectionHandler.SyncGlobalSelectionFromViewModel();
                });
            }
        }

        // True when the global selection is non-empty and every selected object is a persistent asset -
        // something the hierarchy can't represent (e.g. a Project Browser selection). Used so an empty
        // hierarchy restore doesn't clear such a selection (UUM-146689), while still clearing stale
        // scene selections.
        static bool GlobalSelectionIsOnlyAssets()
        {
            var objects = Selection.objects;
            if (objects.Length == 0)
                return false;
            foreach (var o in objects)
            {
                if (!EditorUtility.IsPersistent(o))
                    return false;
            }
            return true;
        }

        internal void SaveStageViewState(Stage stage)
        {
            if (stage == null)
                return;
            var key = StageUtility.CreateWindowAndStageIdentifier(m_WindowGUID, stage);
            // The search text is saved along with the stage content so it can be restored when
            // returning to this stage through the stage history; see LoadStageViewState.
            var state = m_HierarchyView.GetState(HierarchyViewState.Content.Stage | HierarchyViewState.Content.SearchText);
            s_StateCache.SetState(key, state);
        }

        internal HierarchyViewState GetStageViewState(Stage stage)
        {
            var key = StageUtility.CreateWindowAndStageIdentifier(m_WindowGUID, stage);
            return s_StateCache.GetState(key);
        }

        internal void LoadStageViewState(Stage stage, bool restoreSearchText)
        {
            if (stage == null)
                return;

            // A newly opened stage always starts with a new, empty search; only returning to a stage
            // restores the search it had when it was left (see OnSharedHierarchyChanged).
            if (!restoreSearchText)
                SetSearchText(string.Empty);

            var state = GetStageViewState(stage);
            if (state == null)
                return;

            if (!restoreSearchText)
            {
                // Apply a copy without the search text; the cached state keeps its search text so
                // that later returns to this stage can still restore it.
                state = new HierarchyViewState
                {
                    ValidContent = state.ValidContent & ~HierarchyViewState.Content.SearchText,
                    ViewModelState = state.ViewModelState,
                    Columns = state.Columns,
                    ScrollPositionX = state.ScrollPositionX,
                    ScrollPositionY = state.ScrollPositionY
                };
            }

            SetViewState(state);
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
                var searching = !string.IsNullOrEmpty(query);
                if (!UnityEditor.SearchService.SceneSearch.HasEngineOverride())
                {
                    var queryDesc = m_HierarchyView.ViewModel.QueryParser.ParseQuery(query);
                    query = SimplifyQuery(queryDesc);
                }
                foreach (var sw in windows)
                {
                    if (sw.m_HierarchyType != HierarchyType.Assets)
                    {
                        if (sw is SceneView sceneView)
                            sceneView.SetSceneViewFilteringForSearch(searching);
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

            var node = m_Hierarchy.GetNodeFromEntityId(entityId);
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
            var content = L10n.IconContent(s_JumpButton, null, null);
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
    }
}
