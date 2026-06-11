// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for windows to edit graphs.
    /// </summary>
    [UnityRestricted]
    internal abstract partial class GraphViewEditorWindow : EditorWindow, ISupportsOverlays, ISerializationCallbackReceiver
    {
        const string k_SaveChangesMessage = @"Changes to an external graph currently opened in the breadcrumbs have not been saved:
{0}
Would you like to save these changes?


";

        public delegate EditorWindow CreateWindowMethod(Type[] types);
        static readonly MethodInfo k_CreateWindowMethod;
        static int s_EnabledWindowCount;

        static List<GraphViewEditorWindow> s_OpenedWindows = new();

        List<RootView> m_Views;
        bool m_AttachedOnce;
        bool m_HasMultipleWindowsForSameGraph;
        bool m_UnsavedChangesWindowIsEnabled;
        string m_GraphViewName;

        // Used only between OnDisable and OnDestroy
        List<GraphObject> m_AssetsToDestroy = new ();

        Dictionary<Type, GTKOverlayWrapper> m_OverlayWrappers = new();

        readonly List<Overlay> m_OverlaysHiddenByBlankPage = new();

        /// <summary>
        /// The path to the custom style sheet for the dark mode of this graph window.
        /// </summary>
        protected virtual string CustomDarkModeStyleSheetPath => "";

        /// <summary>
        /// The path to the custom style sheet for the light mode of this graph window.
        /// </summary>
        protected virtual string CustomLightModeStyleSheetPath => "";

        /// <summary>
        /// Finds an empty window of type <typeparamref name="TWindow"/>. If none is found, creates a new one.
        /// </summary>
        /// <typeparam name="TWindow">The type of the window to find or create.</typeparam>
        /// <returns>The window.</returns>
        public static TWindow FindOrCreateGraphWindow<TWindow>() where TWindow : GraphViewEditorWindow
        {
            return ShowGraphInExistingOrNewWindow<TWindow>(null);
        }

        /// <summary>
        /// Finds a graph object's opened window of type <typeparamref name="TWindow"/>. If no window is found, create a new one.
        /// The window is then opened and focused.
        /// </summary>
        /// <param name="graphObject">The graph object to display in the window. Pass null if you are looking for an empty window of type <typeparamref name="TWindow"/>.</param>
        /// <param name="loadGraphCommand">The command used to load the graph. If null, will use <see cref="LoadGraphCommand"/>.</param>
        /// <typeparam name="TWindow">The window type, which should derive from <see cref="GraphViewEditorWindow"/>.</typeparam>
        /// <returns>A window.</returns>
        public static TWindow ShowGraphInExistingOrNewWindow<TWindow>(GraphObject graphObject, ICommand loadGraphCommand = null) where TWindow : GraphViewEditorWindow
        {
            return (TWindow)ShowGraphInExistingOrNewWindow(graphObject, typeof(TWindow), loadGraphCommand);
        }

        /// <summary>
        /// Finds a graph object's opened window of type <paramref name="windowType"/>. If no window is found, create a new one.
        /// The window is then opened and focused.
        /// </summary>
        /// <param name="graphObject">The graph object to display in the window. Pass null if you are looking for an empty window of type <paramref name="windowType"/>.</param>
        /// <param name="windowType">The window type, which should derive from <see cref="GraphViewEditorWindow"/>.</param>
        /// <param name="loadGraphCommand">The command used to load the graph. If null, will use <see cref="LoadGraphCommand"/>.</param>
        /// <returns>A window.</returns>
        /// <remarks>The type of <see cref="GraphViewEditorWindow"/> will be determined using <see cref="GraphEditorWindowDefinitionAttribute"/> if null is passed.</remarks>
        public static GraphViewEditorWindow ShowGraphInExistingOrNewWindow(GraphObject graphObject, Type windowType = null, ICommand loadGraphCommand = null)
        {
            if (windowType == null && graphObject != null)
            {
                windowType = GraphObjectFactory.GetWindowTypeForGraphObject(graphObject.GetType());
            }

            GraphViewEditorWindow window = null;

            var windowList = Resources.FindObjectsOfTypeAll(windowType);
            if (graphObject != null)
            {
                foreach (var otherWindow in windowList)
                {
                    var otherGraphEditorWindow = otherWindow as GraphViewEditorWindow;
                    if (otherGraphEditorWindow == null)
                        continue;

                    // If we support only 1 window and there is already an existing window, always use that window.
                    if (!otherGraphEditorWindow.GraphTool.SupportsMultipleWindows)
                    {
                        window = otherGraphEditorWindow;
                        break;
                    }

                    // Reuse the window in which the graph object is already opened in.
                    if (otherGraphEditorWindow.GraphTool.ToolState.GraphModel?.GraphObject == graphObject)
                    {
                        window = otherGraphEditorWindow;
                        break;
                    }

                    // Reuse the subgraph tab in which the graph object is already opened in.
                    if (otherGraphEditorWindow.GraphTool.ToolState.SubgraphStack.Count > 0 && otherGraphEditorWindow.GraphTool.ToolState.GetSubGraphObject(0) == graphObject)
                    {
                        window = otherGraphEditorWindow;
                        break;
                    }
                }
            }

            if (window == null)
            {
                foreach (var otherWindow in windowList)
                {
                    var otherGraphEditorWindow = otherWindow as GraphViewEditorWindow;
                    if (otherGraphEditorWindow == null)
                        continue;
                    if (otherGraphEditorWindow.GraphTool.ToolState.GraphModel == null)
                    {
                        window = otherGraphEditorWindow;
                        break;
                    }
                }
            }

            bool isAssetOfSameTypeOpened = windowList.Length > 0;

            if (window == null)
            {
                var createWindow = k_CreateWindowMethod.MakeGenericMethod(windowType);
                window = (GraphViewEditorWindow)createWindow.Invoke(null, new object[]{new []{isAssetOfSameTypeOpened ? windowType : typeof(SceneView)}});
            }

            window.Show();

            if (graphObject != null)
            {
                window.GraphTool.Dispatch(loadGraphCommand ?? new LoadGraphCommand(graphObject.GraphModel));
            }

            window.Focus();

            return window;
        }

        public static readonly string graphProcessingPendingUssClassName = "graph-processing-pending";

        /// <summary>
        /// Creates and shows a new GraphViewEditorWindow.
        /// </summary>
        /// <param name="desiredDockNextTo">An array of EditorWindow types that the window will attempt to dock onto</param>
        /// <returns>The new <see cref="GraphViewEditorWindow"/> instance.</returns>
        public GraphViewEditorWindow CreateWindow(params Type[] desiredDockNextTo)
        {
            var windowType = GetType();

            var creationMethod = k_CreateWindowMethod.MakeGenericMethod(windowType);
            return (GraphViewEditorWindow) creationMethod?.Invoke(this, new object[] { desiredDockNextTo });
        }

        /// <summary>
        /// If this window supports multiple windows, creates a new window with the onboarding screen. If one already exists and is in onboarding screen mode, shows that.
        /// If multiple windows are not supported, displays the onboarding screen in the same window.
        /// </summary>
        public void ShowOnboardingWindow()
        {
            if (GraphTool.SupportsMultipleWindows)
            {
                var windowType = GetType();
                if (Resources.FindObjectsOfTypeAll(windowType) is not GraphViewEditorWindow[] windowsOfSameType)
                    return;

                // If there is an existing onboarding window of same type, show it.
                GraphViewEditorWindow window = null;
                foreach (var w in windowsOfSameType)
                {
                    if (w.GraphTool.ToolState.GraphModel == null)
                    {
                        window = w;
                        break;
                    }
                }

                if (!window)
                {
                    // If there is none, create a new one.
                    window = CreateWindow(windowsOfSameType.Length > 0 ? windowType : typeof(SceneView));
                }

                window.Show();
                window.Focus();
            }
            else
            {
                GraphTool?.Dispatch(new UnloadGraphCommand());
            }
        }

        static EntityId s_LastFocusedEditor = EntityId.None;

        static int s_GraphViewNameCounter;

        bool m_Focused;

        string m_InitialAssetNameCache;
        string m_CurrentAssetNameCache;
        bool m_InitialAssetDirtyCache;
        bool m_CurrentAssetDirtyCache;

        [NonSerialized]
        Hash128 m_RootGraphModelGuid;
        [NonSerialized]
        Hash128 m_CurrentVisualizationContextId;
        [NonSerialized]
        bool m_IsVisualizationContextAttached;

        protected GraphView m_GraphView;
        protected BlackboardView m_BlackboardView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected Label m_GraphProcessingPendingLabel;

        GraphProcessingStatusObserver m_GraphProcessingStatusObserver;

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_WindowID;
#pragma warning restore CS0618

#pragma warning disable CS0169 // The field 'GraphModel.m_ItemLibraryHelper' is never used
        // Important that domain reload restore this.
        UndoStateRecorder m_UndoStateRecorder;
#pragma warning restore CS0169

        [SerializeField]
        Hash128 m_WindowHash;

        //TODO : GTF-2489 - Remove GraphViewEditorWindow.s_FrameElementDelayMs and queue Framing after Graph Load
        static long s_FrameElementDelayMs
        {
            get
            {
                return 10;
            }
        }

        public virtual IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        /// <summary>
        /// The ID of this window.
        /// </summary>
        public Hash128 WindowID => m_WindowHash;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public GraphTool GraphTool { get; private set; }

        /// <summary>
        /// The main <see cref="GraphView"/> in this <see cref="GraphViewEditorWindow"/>.
        /// </summary>
        public GraphView GraphView => m_GraphView;

        /// <summary>
        /// The main <see cref="BlackboardView"/> in this <see cref="GraphViewEditorWindow"/>.
        /// </summary>
        public BlackboardView BlackboardView
        {
            get => m_BlackboardView;
            set => m_BlackboardView = value;
        }

        /// <summary>
        /// The RootView that handles commands when there is no focused view.
        /// </summary>
        /// <seealso cref="RootView.HandleGlobalValidateCommand"/>
        /// <seealso cref="RootView.HandleGlobalExecuteCommand"/>
        public virtual RootView DefaultCommandView => GraphView;

        // internal for testing
        internal ShortcutBlocker ShortcutBlocker { get; private set; }

        static internal IReadOnlyList<GraphViewEditorWindow> OpenedWindows => s_OpenedWindows;

        static GraphViewEditorWindow()
        {
            static MethodInfo GetMethodInfo(Expression<CreateWindowMethod> expression)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return ((MethodCallExpression)expression.Body).Method;
#pragma warning restore UA2001
            }

            k_CreateWindowMethod = GetMethodInfo(types => EditorWindow.CreateWindow<EditorWindow>(types)).GetGenericMethodDefinition();
            s_GraphViewNameCounter = 0;
            SetupLogStickyCallback();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewEditorWindow"/> class.
        /// </summary>
        protected GraphViewEditorWindow()
        {
            s_LastFocusedEditor = GetEntityId();
            m_WindowHash = Hash128Helpers.GenerateUnique();
        }

        /// <summary>
        /// Creates a new graph tool.
        /// </summary>
        /// <returns>A new graph tool.</returns>
        protected virtual GraphTool CreateGraphTool()
        {
            return GraphTool.Create<GraphTool>(WindowID);
        }

        /// <summary>
        /// Creates a new blank page.
        /// </summary>
        /// <returns>A new blank page.</returns>
        protected virtual BlankPage CreateBlankPage()
        {
            if( GraphTool is SimpleGraphTool simpleGraphTool )
            {
                return new BlankPage(GraphTool, new[]{new SimpleOnboardingProvider(simpleGraphTool)});
            }
            return new BlankPage(GraphTool, Array.Empty<OnboardingProvider>());
        }

        /// <summary>
        /// Gets a unique name for a <see cref="GraphView"/>.
        /// </summary>
        protected virtual string GraphViewName
        {
            get
            {
                if (string.IsNullOrEmpty(m_GraphViewName))
                {
                    m_GraphViewName = GraphTool.Name;
                    if (string.IsNullOrEmpty(m_GraphViewName))
                    {
                        Debug.LogWarning("To avoid mixing GraphView data between tools, each GraphTool should have a unique name.");
                        m_GraphViewName = "GraphView_" + ++s_GraphViewNameCounter;
                    }
                }
                return m_GraphViewName;
            }
        }

        /// <summary>
        /// Creates a new <see cref="GraphRootViewModel"/>.
        /// </summary>
        /// <param name="graphViewName">The name of the <see cref="GraphView"/></param>
        /// <returns>A new <see cref="GraphRootViewModel"/>.</returns>
        protected virtual GraphRootViewModel CreateGraphRootViewModel(string graphViewName)
        {
            if (GraphTool == null)
                return null;

            //This GUID should be unique per window and per graphview in the window.
            var graphGUID = WindowID;
            graphGUID.Append(graphViewName);

            var graphModel = GraphTool.ToolState.GraphModel;
            return new GraphRootViewModel(graphViewName, graphModel, GraphTool, graphGUID);
        }

        /// <summary>
        /// Creates a new <see cref="GraphViewSelection"/>.
        /// </summary>
        /// <param name="rootViewModel">The associated <see cref="GraphRootViewModel"/>.</param>
        /// <returns>A new <see cref="GraphViewSelection"/>.</returns>
        protected virtual GraphViewSelection CreateGraphViewSelection(GraphRootViewModel rootViewModel)
        {
            if (rootViewModel == null)
                return null;

            if (GraphTool == null)
                return null;

            return new GraphViewSelection(rootViewModel.SelectionState, rootViewModel.GraphModelState, GraphTool.ClipboardProvider);
        }

        /// <summary>
        /// Creates a new <see cref="GraphView"/>.
        /// </summary>
        /// <returns>A new &lt;see cref="GraphView"/&gt;.</returns>
        protected virtual GraphView CreateGraphView(GraphRootViewModel viewModel, ViewSelection viewSelection)
        {
            return new GraphView(this, GraphTool, GraphViewName, viewModel, viewSelection);
        }

        /// <summary>
        /// Creates a new <see cref="BlackboardContentModel"/>.
        /// </summary>
        /// <returns>A new <see cref="BlackboardContentModel"/>.</returns>
        protected virtual BlackboardContentModel CreateBlackboardContentModel()
        {
            return new BlackboardContentModel(GraphTool);
        }

        /// <summary>
        /// Creates a new <see cref="BlackboardRootViewModel"/>.
        /// </summary>
        /// <returns>A new <see cref="BlackboardRootViewModel"/>.</returns>
        protected virtual BlackboardRootViewModel CreateBlackboardRootViewModel()
        {
            // This gets called early by the overlay system. Return null if the GraphTool is not yet created.
            if (GraphTool == null)
                return null;

            var blackboardContentModel = CreateBlackboardContentModel();

            return new BlackboardRootViewModel(GraphTool.HighlighterState, GraphView?.GraphViewModel?.GraphModelState,
                blackboardContentModel, GraphView?.GraphViewModel?.Guid ?? default);
        }

        /// <summary>
        /// Creates a new <see cref="BlackboardViewSelection"/>.
        /// </summary>
        /// <param name="rootViewModel">The associated <see cref="BlackboardRootViewModel"/>.</param>
        /// <returns>A new <see cref="BlackboardViewSelection"/>.</returns>
        protected virtual BlackboardViewSelection CreateBlackboardViewSelection(BlackboardRootViewModel rootViewModel)
        {
            if (rootViewModel == null)
                return null;

            // This gets called early by the overlay system. Return null if the GraphTool is not yet created.
            if (GraphTool == null)
                return null;

            if (GraphView?.GraphViewModel == null)
                return null;

            return new BlackboardViewSelection(rootViewModel.SelectionState, rootViewModel.BlackboardContentState, rootViewModel.ViewState, GraphTool.ClipboardProvider);
        }

        /// <summary>
        /// Creates a BlackboardView.
        /// </summary>
        /// <returns>A new BlackboardView.</returns>
        public virtual BlackboardView CreateBlackboardView()
        {
            // This gets called early by the overlay system. Return null if the GraphTool is not yet created.
            if (GraphTool == null)
                return null;

            if (GraphView == null)
                return null;

            var viewModel = CreateBlackboardRootViewModel();
            var viewSelection = CreateBlackboardViewSelection(viewModel);

            m_BlackboardView = new BlackboardView(this, GraphTool, GraphView.TypeHandleInfos, viewModel, viewSelection);
            return m_BlackboardView;
        }

        void OnShortcutUpdated(ShortcutBindingChangedEventArgs shortcutBindingChangedEventArgs)
        {
            if (shortcutBindingChangedEventArgs.shortcutId.StartsWith(GraphTool.Name))
                UpdateTooltips();
        }

        public virtual void UpdateTooltips()
        {
            rootVisualElement.panel?.visualTree.Q<BlackboardPanelToggle>()?.UpdateInspectorTooltip();
            rootVisualElement.panel?.visualTree.Q<InspectorPanelToggle>()?.UpdateInspectorTooltip();
            rootVisualElement.panel?.visualTree.Q<MiniMapPanelToggle>()?.UpdateInspectorTooltip();
        }

        /// <summary>
        /// Creates a new <see cref="MiniMapViewModel"/>.
        /// </summary>
        /// <returns>A new <see cref="MiniMapViewModel"/>.</returns>
        protected virtual MiniMapViewModel CreateMiniMapViewModel()
        {
            return new MiniMapViewModel(GraphView);
        }

        /// <summary>
        /// Creates a MiniMapView.
        /// </summary>
        /// <returns>A new MiniMapView.</returns>
        public virtual MiniMapView CreateMiniMapView()
        {
            var viewModel = CreateMiniMapViewModel();
            return new MiniMapView(this, GraphTool, viewModel);
        }

        /// <summary>
        /// Creates a new <see cref="ModelInspectorViewModel"/>.
        /// </summary>
        /// <returns>A new <see cref="ModelInspectorViewModel"/>.</returns>
        public virtual ModelInspectorViewModel CreateModelInspectorViewModel()
        {
            if (GraphView == null)
                return null;

            var graphModel = GraphTool.ToolState.GraphModel;
            var selectedModels = new List<GraphElementModel>();

            if (graphModel != null)
            {
                var selection = GraphView.GraphViewModel.SelectionState.GetSelection(graphModel);
                for (var i = 0; i < selection.Count; i++)
                {
                    var s = selection[i];
                    if (s is AbstractNodeModel or VariableDeclarationModelBase)
                        selectedModels.Add(s);
                }
            }

            return new ModelInspectorViewModel(GraphView.GraphViewModel.GraphModelState, selectedModels, m_GraphView.GraphViewModel.Guid);
        }

        /// <summary>
        /// Creates a new <see cref="ModelInspectorView"/>.
        /// </summary>
        /// <returns>A new <see cref="ModelInspectorView"/>.</returns>
        public virtual ModelInspectorView CreateModelInspectorView(ModelInspectorViewModel viewModel)
        {
            return new ModelInspectorView(this, GraphTool, viewModel, GraphView?.TypeHandleInfos);
        }

        /// <summary>
        /// Method to update relevant UI when the model's name changes
        /// </summary>
        public virtual void OnUpdateModelName(string newName)
        {
            // https://jira.unity3d.com/browse/GTF-2163
            // This update and the update for breadcrumbs below (UpdateBreadcrumbs) should probably use CSO instead
            // of querying and updating visual elements directly. We should be dispatching a "GraphRenamedCommand" (C) or similar
            // that updates a "GraphModelNameStateComponent" (S) that is being observed by the various elements (breadcrumbs,
            // blackboard, graph inspector, etc.) (Os) that need to update their UI.
            // Doing it that way would mean we don't update editor windows that are hidden, we don't need to search for which editor
            // windows are showing the updated graph, effects are generally much more isolated and efficient and queries don't need to traverse larger UI trees.

            // Update the Blackboard view if it's open
            if (TryGetOverlay(BlackboardOverlay.idValue, out var blackboardOverlay))
            {
                var blackboardTitlePartTitleLabel = blackboardOverlay.rootVisualElement?.Q<Label>(className:BlackboardTitlePart.titleUssClassName);
                if (blackboardTitlePartTitleLabel != null)
                    blackboardTitlePartTitleLabel.text = newName;
            }

            // Update the Graph Inspector view if it's open
            if (TryGetOverlay(ModelInspectorOverlay.idValue, out var graphInspectorOverlay))
                graphInspectorOverlay.rootVisualElement?.Q<ModelInspectorView>()?.UpdateTitle();
        }

        /// <summary>
        /// Creates a new <see cref="ItemLibraryHelper"/>.
        /// </summary>
        /// <param name="graphModel">The graph model associated with the <see cref="GraphViewEditorWindow"/>.</param>
        /// <returns>The new <see cref="ItemLibraryHelper"/>.</returns>
        /// <remarks>
        /// 'CreateItemLibraryHelper' initializes a new instance of ItemLibraryHelper, which manages
        /// the item library for the associated <see cref="GraphViewEditorWindow"/>. The helper is responsible
        /// for retrieving the correct item library adapter, database provider, and filter provider, which ensures
        /// that the item library is properly configured. You can override this method to provide a custom
        /// <see cref="ItemLibraryHelper"/>, allowing for specialized item filtering, categorization, or UI
        /// behavior tailored to specific graph implementations.
        /// </remarks>
        public virtual ItemLibraryHelper CreateItemLibraryHelper(GraphModel graphModel)
        {
            return graphModel == null ? null : new ItemLibraryHelper(graphModel);
        }

        /// <inheritdoc />
        public override void SaveChanges()
        {
            // For now, it is not possible to individually close a specific breadcrumb. Thus, we have to save changes in the current opened graph AND in the other opened graphs as well.
            SaveAll();
            base.SaveChanges();
        }

        /// <summary>
        /// Saves all opened graphs in the line of breadcrumbs.
        /// </summary>
        public virtual void SaveAll()
        {
            if (GraphTool?.ToolState?.CurrentGraph != null)
            {
                var openedGraphs = new List<GraphReference> { GraphTool.ToolState.CurrentGraph };
                if (GraphTool.ToolState.SubgraphStack != null)
                {
                    openedGraphs.Capacity += GraphTool.ToolState.SubgraphStack.Count;
                    foreach (var breadcrumbModel in GraphTool.ToolState.SubgraphStack)
                    {
                        openedGraphs.Add(breadcrumbModel);
                    }
                }

                // nothing to save
                if (openedGraphs.Count == 0)
                    return;

                foreach (var openedGraph in openedGraphs)
                {
                    var graphObject = GraphTool.ResolveGraphModelFromReference(openedGraph)?.GraphObject;
                    if (graphObject != null)
                    {
                        graphObject.Save();
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Saves the current graph opened in the line of breadcrumbs.
        /// </summary>
        public virtual void Save()
        {
            if (GraphTool?.ToolState != null)
            {
                var graphObject = GraphTool.ToolState.GraphModel?.GraphObject;
                if (graphObject != null)
                {
                    graphObject.Save();
                    return;
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Saves the current graph opened in the line of breadcrumbs as a new file. Opens a prompt box to create the new file.
        /// </summary>
        public virtual void SaveAs()
        {
            var graphObject = GraphTool?.ToolState?.GraphModel?.GraphObject;
            if (graphObject != null)
            {
                var newGraphAsset = GraphObjectCreationHelpers.PromptToCreateGraphObject(graphObject.GetType(),
                    graphObject.GraphModel.GetType(),
                    "Save As...", graphObject.name,
                    Path.GetExtension(graphObject.FilePath).TrimStart('.'), "Save As");

                if (newGraphAsset != null)
                {
                    newGraphAsset.GraphModel.CloneGraph(graphObject.GraphModel);
                    newGraphAsset.Save();
                }
            }
        }

        /// <inheritdoc />
        public override void DiscardChanges()
        {
            var toolState = GraphTool?.ToolState;
            if (toolState == null)
            {
                base.DiscardChanges();
                return;
            }

            if (toolState.GraphObject != null)
            {
                toolState.GraphObject.UnloadObject();
            }

            // For now, it is not possible to individually close a specific breadcrumb. Thus, we have to save changes in the current opened graph AND in the other opened graphs as well.
            for(int i = 0 ; i < toolState.SubgraphStack.Count ; ++i)
            {
                toolState.GetSubGraphObject(i)?.UnloadObject();
            }

            base.DiscardChanges();
        }

        protected virtual void Reset()
        {
            if (GraphTool?.ToolState == null)
                return;

            using var toolStateUpdater = GraphTool.ToolState.UpdateScope;
            toolStateUpdater.ClearHistory();
            toolStateUpdater.LoadGraph(null, null);
            m_WindowHash = Hash128Helpers.GenerateUnique();
        }

        void LoadLastOpenedGraph()
        {
            try
            {
                var command = GraphTool?.ToolState.GetLoadLastOpenedGraphCommand();
                if (command != null)
                {
                    GraphTool?.Dispatch(command);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        VisualElement GetBaseRootVisualElement()
        {
            // mimics EditorWindow.baseRootVisualElement

            // We are looking for the element that is a children of the EditorPanelRootElement.
            var root = rootVisualElement;
            while (root.parent is { parent: not null })
            {
                root = root.parent;
            }

            return root;
        }

        protected virtual void OnEnable()
        {
            m_AssetsToDestroy.Clear();
            s_EnabledWindowCount++;
            m_Views = new List<RootView>();
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnRootDetach);
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnRootAttach);

            if (rootVisualElement.panel != null) //OnEnable is called after the rootElement is attached to panel when loading the editor with a graphview already open.
            {
                AttachOnce();
            }
            else
            {
                m_AttachedOnce = false;
            }

            GraphTool = CreateGraphTool();

            if (m_GraphContainer != null)
            {
                m_GraphContainer.RemoveFromHierarchy();
                m_GraphContainer = null;
            }

            if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
            {
                rootVisualElement.Remove(m_GraphProcessingPendingLabel);
                m_GraphProcessingPendingLabel = null;
            }

            rootVisualElement.pickingMode = PickingMode.Ignore;

            m_GraphContainer = new VisualElement { name = "graphContainer" };

            CreateAndSetupGraphView();
            m_BlankPage = CreateBlankPage();
            m_BlankPage?.CreateUI();

            rootVisualElement.Add(m_GraphContainer);

            m_GraphContainer.Add(m_GraphView);
            RegisterView(m_GraphView);

            rootVisualElement.AddPackageStylesheet("GraphViewWindow.uss");
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.AddToClassList("gtf-root");

            EditorApplication.update += EditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ShortcutManager.instance.shortcutBindingChanged += OnShortcutUpdated;
            EditorApplication.delayCall += UpdateTooltips;
            GraphVisualization.Registry.contextRegistered += ResolveAndAttachVisualizationContext;
            GraphVisualization.Registry.contextWillUnregister += OnVisualizationContextWillUnregister;
            ResolveAndAttachVisualizationContext();

            m_GraphProcessingPendingLabel = new Label("Graph Processing Pending") { name = "graph-processing-pending-label" };

            UpdateWindowTitle();

            if (GraphView?.DisplayMode == GraphViewDisplayMode.Interactive)
            {
                rootVisualElement.RegisterCallback<MouseMoveEvent>(GraphView.ProcessOnIdleAgent.OnMouseMove);
            }

            m_GraphProcessingStatusObserver = new GraphProcessingStatusObserver(m_GraphProcessingPendingLabel, GraphView?.GraphViewModel?.GraphProcessingState);

            GraphTool?.ObserverManager.RegisterObserver(m_GraphProcessingStatusObserver);

            m_UnsavedChangesWindowIsEnabled = IsUnsavedChangesWindowEnabled();

            ShortcutBlocker = new ShortcutBlocker();
            ShortcutBlocker.Enable(this.GetBaseRootVisualElement());

            // registering ValidateCommandEvent and ExecuteCommandEvent on the rootVisualElement should be sufficient but because of https://jira.unity3d.com/browse/UUM-79252, we need to use the panel.visualTree instead.
            void OnAttach(AttachToPanelEvent _)
            {
                rootVisualElement.panel.visualTree.RegisterCallback<ValidateCommandEvent>(OnRootValidateCommand);
                rootVisualElement.panel.visualTree.RegisterCallback<ExecuteCommandEvent>(OnRootExecuteCommand);
                rootVisualElement.panel.visualTree.RegisterCallback<ShortcutToggleBlackboardEvent>(OnShortcutToggleBlackboardEvent);
                rootVisualElement.panel.visualTree.RegisterCallback<ShortcutToggleInspectorEvent>(OnShortcutToggleInspectorEvent);
                rootVisualElement.panel.visualTree.RegisterCallback<ShortcutToggleMinimapEvent>(OnShortcutToggleMinimapEvent);

                rootVisualElement.panel.visualTree.RegisterCallback<ShortcutFileSaveEvent>(OnShortcutFileSaveEvent);
                rootVisualElement.panel.visualTree.RegisterCallback<ShortcutSaveAsEvent>(OnShortcutSaveAsEvent);
            }
            void OnDetach(DetachFromPanelEvent _)
            {
                rootVisualElement.panel.visualTree.UnregisterCallback<ValidateCommandEvent>(OnRootValidateCommand);
                rootVisualElement.panel.visualTree.UnregisterCallback<ExecuteCommandEvent>(OnRootExecuteCommand);
                rootVisualElement.panel.visualTree.UnregisterCallback<ShortcutToggleBlackboardEvent>(OnShortcutToggleBlackboardEvent);
                rootVisualElement.panel.visualTree.UnregisterCallback<ShortcutToggleInspectorEvent>(OnShortcutToggleInspectorEvent);
                rootVisualElement.panel.visualTree.UnregisterCallback<ShortcutToggleMinimapEvent>(OnShortcutToggleMinimapEvent);

                rootVisualElement.panel.visualTree.UnregisterCallback<ShortcutFileSaveEvent>(OnShortcutFileSaveEvent);
                rootVisualElement.panel.visualTree.UnregisterCallback<ShortcutSaveAsEvent>(OnShortcutSaveAsEvent);
            }

            // on a domain reload with the window opened, the panel is already attached to the rootVisualElement
            if( rootVisualElement.panel != null )
            {
                OnAttach(null);
            }
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttach);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            // because of overlays the ootVisualElement is not the real root and overlays are not a descendant of it. Search for the real root to add global uss files. Note that it is not the same as the panel.visualTree used above.
            var realRootElement = GetBaseRootVisualElement();

            // Add stylesheets
            realRootElement.AddStylesheetWithSkinVariants("View.uss");
            realRootElement.AddPackageStylesheet("TypeIcons.uss");
            if (EditorGUIUtility.isProSkin && CustomDarkModeStylesheetPaths != null)
            {
                foreach (var stylesheetPath in CustomDarkModeStylesheetPaths)
                    realRootElement.AddStyleSheetPath(stylesheetPath);
            }
            if (!EditorGUIUtility.isProSkin && CustomLightModeStylesheetPaths != null)
            {
                foreach (var stylesheetPath in CustomLightModeStylesheetPaths)
                    realRootElement.AddStyleSheetPath(stylesheetPath);
            }

            CreateOverlayContents();
            s_OpenedWindows.Add(this);
        }

        void CreateAndSetupGraphView()
        {
            var viewModel = CreateGraphRootViewModel(GraphViewName);
            var viewSelection = CreateGraphViewSelection(viewModel);

            m_GraphView = CreateGraphView(viewModel, viewSelection);
            m_GraphView?.Initialize();
        }

        /// <summary>
        /// Custom dark mode stylesheets of a graph tool.
        /// </summary>
        protected virtual List<string> CustomDarkModeStylesheetPaths => null;

        /// <summary>
        /// Custom light mode stylesheets of a graph tool.
        /// </summary>
        protected virtual List<string> CustomLightModeStylesheetPaths => null;

        void OnRootExecuteCommand(ExecuteCommandEvent evt)
        {
            if ((evt.target as VisualElement)?.GetFirstOfType<RootView>() == null)
            {
                DefaultCommandView?.HandleGlobalExecuteCommand(evt);
            }
        }

        void OnRootValidateCommand(ValidateCommandEvent evt)
        {
            if ((evt.target as VisualElement)?.GetFirstOfType<RootView>() == null)
            {
                DefaultCommandView?.HandleGlobalValidateCommand(evt);
            }
        }

        /// <summary>
        /// Callback for the <see cref="ShortcutToggleBlackboardEvent"/>.
        /// </summary>
        protected void OnShortcutToggleBlackboardEvent(ShortcutToggleBlackboardEvent e)
        {
            ToggleBlackboard();
            e.StopPropagation();
        }

        void ToggleBlackboard()
        {
            // Use the blackboard panel toggle
            if (TryGetOverlay(BlackboardOverlay.idValue, out var blackboardOverlay))
                blackboardOverlay.displayed = !blackboardOverlay.displayed;
        }

        /// <summary>
        /// Callback for the <see cref="ShortcutToggleInspectorEvent"/>.
        /// </summary>
        protected void OnShortcutToggleInspectorEvent(ShortcutToggleInspectorEvent e)
        {
            ToggleInspector();
            e.StopPropagation();
        }

        void ToggleInspector()
        {
            // Use the inspector panel toggle
            if (TryGetOverlay(ModelInspectorOverlay.idValue, out var inspectorOverlay))
                inspectorOverlay.displayed = !inspectorOverlay.displayed;
        }

        /// <summary>
        /// Callback for the <see cref="ShortcutToggleMinimapEvent"/>.
        /// </summary>
        protected void OnShortcutToggleMinimapEvent(ShortcutToggleMinimapEvent e)
        {
            ToggleMinimap();
            e.StopPropagation();
        }

        void ToggleMinimap()
        {
            // Use the minimap panel toggle
            if (TryGetOverlay(MiniMapOverlay.idValue, out var minimapOverlay))
                minimapOverlay.displayed = !minimapOverlay.displayed;
        }

        protected void OnShortcutFileSaveEvent(ShortcutFileSaveEvent e)
        {
            if (!m_Focused)
                return;

            Save();
            e.StopPropagation();
        }

        protected void OnShortcutSaveAsEvent(ShortcutSaveAsEvent evt)
        {
            if (!m_Focused)
                return;

            SaveAs();
            evt.StopPropagation();
        }

        void OnPlayModeStateChanged(PlayModeStateChange value)
        {
            if (value == PlayModeStateChange.ExitingEditMode)
            {
                if (!EditorBridge.CanClose(this))
                {
                    EditorApplication.ExitPlaymode();
                }
            }
        }

        protected virtual void OnDisable()
        {
            RestoreHiddenOverlays();
            m_AssetsToDestroy.Clear();

            UpdateWindowsWithSameCurrentGraph(true);

            GraphView.ProcessOnIdleAgent?.StopTimer();

            if (GraphTool != null)
            {
                GraphTool.ObserverManager.UnregisterObserver(m_GraphProcessingStatusObserver);
                GraphTool.Dispose();
                GraphTool = null;
            }

            rootVisualElement.Remove(m_GraphContainer);
            m_GraphView.Dispose();

            m_GraphContainer = null;
            m_GraphView = null;
            m_BlankPage = null;

            ShortcutBlocker.Disable();
            ShortcutBlocker = null;

            EditorApplication.update -= EditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ShortcutManager.instance.shortcutBindingChanged -= OnShortcutUpdated;
            GraphVisualization.Registry.contextRegistered -= ResolveAndAttachVisualizationContext;
            GraphVisualization.Registry.contextWillUnregister -= OnVisualizationContextWillUnregister;

            ConsoleWindowHelper.RemoveLogEntries(WindowID.ToString());

            if (--s_EnabledWindowCount == 0)
            {
                //It seems that the static StateCache Dispose() or destructor might not be called on domain reload.
                //Maybe it will be restored later. Currently, this code will make sure the StateCache is written to disk when a domain reload occurs.
                PersistedState.Flush();
            }
            s_OpenedWindows.Remove(this);
        }

        protected virtual void OnDestroy()
        {
            foreach (var view in m_Views)
            {
                view.OnDestroy();
            }

            foreach (var graph in m_AssetsToDestroy)
            {
                graph?.DestroyObjects();
            }
        }

        protected virtual void OnFocus()
        {
            s_LastFocusedEditor = GetEntityId();

            if (m_Focused)
                return;

            if (rootVisualElement == null)
                return;

            m_Focused = true;

            UpdateWindowsWithSameCurrentGraph(false);
        }

        /// <summary>
        /// Updates the focused windows and disables windows that have the same current graph as the focused window's.
        /// </summary>
        internal void UpdateWindowsWithSameCurrentGraph(bool currentWindowIsClosing)
        {
            var currentGraph = GraphTool?.ToolState?.CurrentGraph;
            if (currentGraph == null)
                return;

            if (GraphView != null && !GraphView.enabledSelf)
                GraphView.SetEnabled(true);

            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(GetType());
            var shouldUpdateFocusedWindow = false;

            foreach (var window in windows)
            {
                if (window.GetEntityId() == s_LastFocusedEditor)
                    continue;

                var otherGraph = window.GraphTool?.ToolState?.CurrentGraph;
                if (otherGraph != null && currentGraph.Equals(otherGraph))
                {
                    // Unfocused windows with the same graph are disabled
                    window.GraphView?.SetEnabled(false);

                    if (currentWindowIsClosing)
                    {
                        // If the current window is closing with changes, the changes need to be updated in other
                        // windows with the same graph to not lose the changes.
                        UpdateGraphModelState(window.GraphTool?.State.AllStateComponents);
                    }
                    shouldUpdateFocusedWindow = !currentWindowIsClosing;
                }
            }

            if (shouldUpdateFocusedWindow)
            {
                UpdateGraphModelState(GraphTool.State.AllStateComponents);
            }

            static void UpdateGraphModelState(IReadOnlyList<IStateComponent> stateComponents)
            {
                if (stateComponents == null)
                    return;

                GraphModelStateComponent graphModelStateComponent = null;
                foreach (var component in stateComponents)
                {
                    if (component is GraphModelStateComponent stateComponent)
                    {
                        graphModelStateComponent = stateComponent;
                        break;
                    }
                }

                if (graphModelStateComponent == null)
                    return;

                // Update the focused window
                using var updater = graphModelStateComponent.UpdateScope;
                updater.ForceCompleteUpdate();
            }
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        bool IsUnsavedChangesWindowEnabled()
        {
            var unsavedChangesIsEnabled = GraphTool?.EnableUnsavedChangesDialogWindow;
            if (unsavedChangesIsEnabled != null && m_UnsavedChangesWindowIsEnabled != unsavedChangesIsEnabled)
                return unsavedChangesIsEnabled.Value;
            return true;
        }

        protected virtual void OnInspectorUpdate()
        {
            GraphView?.ProcessOnIdleAgent?.Execute();
        }

        protected virtual void Update()
        {
            Profiler.BeginSample("GraphViewEditorWindow.Update");
            var sw = new Stopwatch();
            sw.Start();

            // PF FIXME To StateObserver, eventually
            UpdateGraphContainer();
            UpdateOverlays();

            sw.Stop();

            if (GraphTool.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({GraphTool?.LastDispatchedCommandName ?? "Unknown command"}) took {sw.ElapsedMilliseconds} ms");
            }

            if (!m_IsVisualizationContextAttached)
                ResolveAndAttachVisualizationContext();

            Profiler.EndSample();
        }

        /// <summary>
        /// Method called each frame as long as it exists, even when the window is hidden and detached from the panel.
        /// Used to update the models.
        /// </summary>
        protected virtual void EditorUpdate()
        {
            UpdateWindowTitle();

            m_UnsavedChangesWindowIsEnabled = IsUnsavedChangesWindowEnabled();
            UpdateHasUnsavedChanges();

            GraphTool?.Update();
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y);
        }

        protected void UpdateGraphContainer()
        {
            var graphModel = GraphTool?.ToolState.GraphModel;

            if (m_GraphContainer != null)
            {
                if (graphModel != null)
                {
                    if (m_GraphContainer.Contains(m_BlankPage))
                    {
                        m_GraphContainer.Remove(m_BlankPage);
                        RestoreHiddenOverlays();
                    }

                    if (!m_GraphContainer.Contains(m_GraphView))
                    {
                        m_GraphContainer.Insert(0, m_GraphView);
                    }

                    if (!rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                    {
                        rootVisualElement.Add(m_GraphProcessingPendingLabel);
                    }
                }
                else
                {
                    if (m_GraphContainer.Contains(m_GraphView))
                    {
                        m_OverlaysHiddenByBlankPage.Clear();

                        foreach (var overlay in overlayCanvas.overlays)
                        {
                            if (overlay.displayed)
                                m_OverlaysHiddenByBlankPage.Add(overlay);

                            overlay.displayed = false;
                        }

                        m_GraphContainer.Remove(m_GraphView);
                    }

                    if (!m_GraphContainer.Contains(m_BlankPage))
                    {
                        m_GraphContainer.Insert(0, m_BlankPage);
                    }

                    if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                    {
                        rootVisualElement.Remove(m_GraphProcessingPendingLabel);
                    }
                }
            }
        }

        void RestoreHiddenOverlays()
        {
            foreach (var overlay in m_OverlaysHiddenByBlankPage)
            {
                overlay.displayed = true;
            }

            m_OverlaysHiddenByBlankPage.Clear();
        }

        protected virtual void UpdateOverlays()
        {
        }

        static void SetupLogStickyCallback()
        {
            EditorBridge.SetEntryDoubleClickedDelegate((file, _) =>
            {
                var info = file.Split('@');
                if( info.Length < 4 )
                    return;
                var filePath = info[0];
                var elementGuid = Hash128.Parse(info[1]);
                var assetGuid = new GUID(info[2]);
                var graphGuid = Hash128.Parse(info[3]);
                var sourceGraphFilePath = info[4];

                GraphViewEditorWindow window = null;
                foreach (var graphViewEditorWindow in (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow)))
                {
                    if (graphViewEditorWindow.GraphTool.ToolState.CurrentGraph.FilePath == filePath ||
                        (graphViewEditorWindow.GraphTool.ToolState.SubgraphStack is { Count: > 0 } &&
                            graphViewEditorWindow.GraphTool.ToolState.SubgraphStack[0].FilePath == filePath))
                    {
                        window = graphViewEditorWindow;
                        break;
                    }
                }

                if (window is null)
                    return;

                var currentGraph = window.GraphTool.ToolState.CurrentGraph;
                GraphModel graphModelToLoad = null;
                if (currentGraph.GraphModelGuid != graphGuid)
                {
                    // The element is not present on the current graph. Find the graph to load.
                    var assets = AssetDatabase.LoadAllAssetsAtPath(sourceGraphFilePath);
                    foreach (var asset in assets)
                    {
                        if (asset is GraphObject graphObject)
                        {
                            graphModelToLoad = graphObject.GetGraphModelByGuid(graphGuid);
                            if (graphModelToLoad != null)
                                break;
                        }
                    }
                }

                FrameGraphElement(elementGuid, window, graphModelToLoad);
            });
        }

        internal static void FrameGraphElement(Hash128 elementGuid, GraphViewEditorWindow window, GraphModel graphModelToLoad = null)
        {
            FrameGraphElement(elementGuid, window.GraphView, graphModelToLoad);
        }

        internal static void FrameGraphElement(Hash128 elementGuid, GraphView graphView,
            GraphModel graphModelToLoad = null)
        {
            if (graphModelToLoad is not null)
            {
                // We have to load the graph first.
                graphView.Dispatch(new LoadGraphCommand(graphModelToLoad, LoadGraphCommand.LoadStrategies.PushOnStack));
                if (graphModelToLoad.TryGetModelFromGuid(elementGuid, out var elementModel))
                {
                    // Wait some time for the graph to be loaded before framing the element.
                    graphView.schedule.Execute(() => { FrameElement(elementModel); }).ExecuteLater(s_FrameElementDelayMs);
                }
            }
            else
            {
                if (graphView.GraphModel.TryGetModelFromGuid(elementGuid, out var elementModel))
                    FrameElement(elementModel);
            }

            return;

            void FrameElement(Model elementModel)
            {
                var graphElement = elementModel.GetView<GraphElement>(graphView);
                if (graphElement != null)
                    graphView.DispatchFrameAndSelectElementsCommand(true, graphElement);
            }
        }

        // Internal for tests
        internal void UpdateWindowTitle(bool forceUpdate = false)
        {
            GraphObject initialObject = null;
            if (GraphTool?.ToolState?.SubgraphStack.Count > 0)
            {
                initialObject = GraphTool.ToolState.GetSubGraphObject(0);
            }
            var currentGraphModel = GraphTool?.ToolState?.GraphModel;

            var initialAssetName = !initialObject ? "" : initialObject.name;
            var currentAssetName = currentGraphModel == null ? "" : currentGraphModel.Name;
            var initialAssetDirty = !initialObject || initialObject.Dirty;
            var currentAssetDirty = currentGraphModel == null || currentGraphModel.GraphObject.Dirty;

            var shouldUpdateIcon = GraphTool?.Icon != null && titleContent?.image != GraphTool?.Icon;
            var shouldUpdateTitle = initialAssetName != m_InitialAssetNameCache || currentAssetName != m_CurrentAssetNameCache || initialAssetDirty != m_InitialAssetDirtyCache || currentAssetDirty != m_CurrentAssetDirtyCache;

            if (forceUpdate || shouldUpdateTitle || shouldUpdateIcon)
            {
                var tooltip = titleContent.tooltip;
                var titleContentText = forceUpdate || shouldUpdateTitle ? FormatWindowTitle(initialAssetName, currentAssetName, initialAssetDirty, currentAssetDirty, out tooltip) : titleContent.text;
                var titleContentIcon = shouldUpdateIcon ? GraphTool?.Icon : titleContent?.image;

                titleContent = new GUIContent(titleContentText, titleContentIcon, tooltip);

                m_InitialAssetNameCache = initialAssetName;
                m_CurrentAssetNameCache = currentAssetName;
                m_InitialAssetDirtyCache = initialAssetDirty;
                m_CurrentAssetDirtyCache = currentAssetDirty;
            }
        }

        /// <summary>
        /// Formats the graph window title. Take into consideration the current graph and the initial graph when multiple graphs are opened in breadcrumbs.
        /// </summary>
        /// <param name="initialAssetName">The initial graph name.</param>
        /// <param name="currentAssetName">The current graph name.</param>
        /// <param name="initialAssetIsDirty">Whether the initial graph has unsaved changes.</param>
        /// <param name="currentAssetIsDirty">Whether the current graph has unsaved changes.</param>
        /// <param name="tooltip">The tooltip when hovering of the graph window.</param>
        /// <returns>The formatted title for the graph window.</returns>
        protected virtual string FormatWindowTitle(string initialAssetName, string currentAssetName, bool initialAssetIsDirty, bool currentAssetIsDirty, out string tooltip)
        {
            return FormatWindowTitle(true, initialAssetName, currentAssetName, initialAssetIsDirty, currentAssetIsDirty, out tooltip);
        }

        /// <summary>
        /// Formats the graph window title. Take into consideration the current graph and the initial graph when multiple graphs are opened in breadcrumbs.
        /// </summary>
        /// <param name="hasDirtyStr">Whether the title should add additional (*)'s to the title to represent the dirty states of the initial and current graph.
        /// Else, only the default (*) from <see cref="EditorWindow"/> will be added to the title.</param>
        /// <param name="initialAssetName">The initial graph name.</param>
        /// <param name="currentAssetName">The current graph name.</param>
        /// <param name="initialAssetIsDirty">Whether the initial graph has unsaved changes.</param>
        /// <param name="currentAssetIsDirty">Whether the current graph has unsaved changes.</param>
        /// <param name="tooltip">The tooltip when hovering of the graph window.</param>
        /// <returns>The formatted title for the graph window.</returns>
        protected string FormatWindowTitle(bool hasDirtyStr, string initialAssetName, string currentAssetName, bool initialAssetIsDirty, bool currentAssetIsDirty, out string tooltip)
        {
            if (string.IsNullOrEmpty(initialAssetName) && string.IsNullOrEmpty(currentAssetName))
            {
                tooltip = GraphTool?.Name ?? "";
                return tooltip;
            }

            const int maxLength = 20; // Maximum limit of characters in a window primary tab
            const string ellipsis = "...";

            if (string.IsNullOrEmpty(initialAssetName))
            {
                var dirtyStrLength = hasDirtyStr && currentAssetIsDirty ? 1 : 0;
                var expectedLength = maxLength - ellipsis.Length - dirtyStrLength; // The max length for the window title without the ellipsis and the dirty flag
                currentAssetName = currentAssetName.Length > maxLength ? currentAssetName.Substring(0, expectedLength) + ellipsis : currentAssetName;
                tooltip = currentAssetName + (hasDirtyStr && currentAssetIsDirty && m_HasMultipleWindowsForSameGraph ? "*" : "");
                return tooltip;
            }

            var initialAssetDirtyStr = hasDirtyStr && initialAssetIsDirty ? "*" : "";
            var currentAssetDirtyStr =  hasDirtyStr && currentAssetIsDirty ? "*" : "";

            // In the case of multiple windows with the same graph, we manually add the (*) since EditorWindow.hasUnsavedChanges is always false for that case and will not add the (*).
            var multipleWindowsForSameGraphDirtyStr = hasDirtyStr && !hasUnsavedChanges && (initialAssetIsDirty || currentAssetIsDirty && m_UnsavedChangesWindowIsEnabled && m_HasMultipleWindowsForSameGraph) ? " *" : "";

            // When EditorWindow.hasUnsavedChanges is true, it will add (*) at the end of the window title. We add a space before the (*) to avoid confusion with prior dirty flags. Eg: (InitialAssetName...*) CurrentAssetName...* *
            const string space = " ";
            // In the case the current graph is a subgraph, the window primary tab's naming should follow this format: (InitialAssetName...*) CurrentAssetName...*
            tooltip = $"({initialAssetName}{initialAssetDirtyStr}) {currentAssetName}{currentAssetDirtyStr}{multipleWindowsForSameGraphDirtyStr}{space}";
            if (tooltip.Length <= maxLength)
                return tooltip;

            var dirtyCount = hasDirtyStr && currentAssetIsDirty ? 1 : 0;
            if (hasDirtyStr && initialAssetIsDirty)
                dirtyCount++;

            var otherCharactersLength = 9 + dirtyCount; // Other characters that are not letters in the naming format: parenthesis, dirty flag, ellipsis in (InitialAssetName...*) CurrentAssetName...*
            var actualLength = (initialAssetName + currentAssetName).Length + otherCharactersLength;

            const int minCurrentAssetNameLength = 5;
            var excessLength = actualLength - maxLength;
            if (currentAssetName.Length - excessLength >= minCurrentAssetNameLength)
            {
                currentAssetName = currentAssetName.Substring(0, currentAssetName.Length - excessLength) + ellipsis;
            }
            else
            {
                var availableLength = maxLength - otherCharactersLength;
                var expectedInitialAssetNameLength = availableLength - currentAssetName.Length;
                if (currentAssetName.Length > minCurrentAssetNameLength)
                {
                    currentAssetName = currentAssetName.Substring(0, minCurrentAssetNameLength);
                    expectedInitialAssetNameLength = availableLength - currentAssetName.Length;
                    currentAssetName += ellipsis;
                }

                initialAssetName = initialAssetName.Length > expectedInitialAssetNameLength ? initialAssetName.Substring(0, expectedInitialAssetNameLength) + ellipsis : initialAssetName;
            }

            return $"({initialAssetName}{initialAssetDirtyStr}) {currentAssetName}{currentAssetDirtyStr}{multipleWindowsForSameGraphDirtyStr}{space}";
        }

        // internal for tests
        internal void UpdateHasUnsavedChanges()
        {
            if (!m_UnsavedChangesWindowIsEnabled)
                return;

            var subgraphStack = GraphTool?.ToolState?.SubgraphStack;
            var initialAsset = subgraphStack is { Count: > 0 } ? GraphTool?.ResolveGraphModelFromReference(subgraphStack[0])?.GraphObject : null;
            var currentAsset = GraphTool?.ToolState?.GraphModel?.GraphObject;

            if (currentAsset == null)
            {
                hasUnsavedChanges = false;
                return;
            }

            // If the current asset is dirty, its parent should be dirty as well
            if (currentAsset.Dirty && initialAsset != null && !initialAsset.Dirty)
            {
                initialAsset.Dirty = true;
            }

            var initialAssetDirty = initialAsset != null && initialAsset.Dirty;

            var oldHasMultipleWindowsForSameGraph = m_HasMultipleWindowsForSameGraph;
            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(GetType());

            m_HasMultipleWindowsForSameGraph = false;

            foreach (var window in windows)
            {
                if (window.GetEntityId() != s_LastFocusedEditor)
                {
                    var otherGraph = window.GraphTool?.ToolState?.GraphModel;
                    if (otherGraph != null && currentAsset.GraphModel == otherGraph)
                    {
                        m_HasMultipleWindowsForSameGraph = true;
                        break;
                    }
                }
            }

            hasUnsavedChanges = !m_HasMultipleWindowsForSameGraph && currentAsset.Dirty || initialAssetDirty;

            // The title update is triggered when there is a change in the title or the graph dirty state. When there are more than one window with the same graph, we need to force the title update:
            // The first window detects the dirty state change. However, the graph dirty state stays the same for the subsequent windows and the title update is not triggered.
            if (oldHasMultipleWindowsForSameGraph != m_HasMultipleWindowsForSameGraph)
                UpdateWindowTitle(true);

            GetSaveChangesMessage();
        }

        void GetSaveChangesMessage()
        {
            var pathsStr = "";

            if (GraphTool?.ToolState is { SubgraphStack: not null } && GraphTool.ToolState.CurrentGraph != default)
            {
                var currentGraph = GraphTool.ToolState.CurrentGraph;
                AppendGraphPath(currentGraph, ref pathsStr);

                var subgraphStack = GraphTool.ToolState.SubgraphStack;
                if (subgraphStack != null)
                {
                    foreach (var openedGraph in subgraphStack)
                    {
                        AppendGraphPath(openedGraph, ref pathsStr);
                    }
                }
            }

            saveChangesMessage = string.Format(k_SaveChangesMessage, string.IsNullOrEmpty(pathsStr) ? "Path not found." : pathsStr);
            return;

            void AppendGraphPath(GraphReference graphReference, ref string s)
            {
                if (GraphTool.ResolveGraphModelFromReference(graphReference)?.GraphObject?.Dirty ?? false)
                {
                    var graphAssetPath = graphReference.FilePath;
                    if (graphAssetPath == null || s.Contains(graphAssetPath))
                        return;
                    s += graphAssetPath + "\n";
                }
            }
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
#pragma warning disable CS0612
            m_WindowID = m_WindowHash;
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
#pragma warning disable CS0612
            m_WindowHash = m_WindowID;
#pragma warning restore CS0612
        }

        /// <summary>
        /// Adds a view to the list of views currently present in this window.
        /// </summary>
        /// <param name="view">The view.</param>
        public void RegisterView(RootView view)
        {
            m_Views.Add(view);

            if (m_AttachedOnce)
            {
                view.OnCreate();
            }
        }

        /// <summary>
        /// Removes a view from the list of view currently present in this window.
        /// </summary>
        /// <param name="view">The view.</param>
        public void UnregisterView(RootView view)
        {
            view.OnDestroy();
            m_Views.Remove(view);
        }

        public void UnregisterBlackboardView(BlackboardView view)
        {
            if (BlackboardView == view)
            {
                UnregisterView(view);
                BlackboardView = null;
            }
        }

        void OnRootDetach(DetachFromPanelEvent e)
        {
            foreach (var view in m_Views)
                view.TryPauseViewObservers();
        }

        void OnRootAttach(AttachToPanelEvent e)
        {
            if (m_AttachedOnce)
            {
                foreach (var view in m_Views)
                    view.TryResumeViewObservers();
            }
            else
            {
                AttachOnce();
            }
        }

        void AttachOnce()
        {
            m_AttachedOnce = true;
            foreach (var view in m_Views)
                view.OnCreate();
        }

        void UpdateGraphViewFocus()
        {
            if (GraphView is null)
                return;

            var focusedElement = GraphView.panel?.focusController?.focusedElement as VisualElement;

            // If the graph window has the focus but the focused element is not part of a root view, give the focus back to the graph view.
            while (focusedElement is not null && focusedElement is not ModelView && focusedElement is not RootView)
                focusedElement = focusedElement.parent;

            if (focusedElement is null)
                GraphView.schedule.Execute(() => { GraphView.Focus(); }).ExecuteLater(0);
        }

        void OnBecameVisible()
        {
            // When opening a graph by clicking on a window tab, the graph view focus is lost to the IMGUIContainer of the window.
            // We try to get it back.
            UpdateGraphViewFocus();
        }

        Hash128 GetRootGraphModelGuid()
        {
            if (GraphTool?.ToolState?.SubgraphStack is { Count: > 0 })
            {
                var subGraphModel = GraphTool.ToolState.GetSubGraphModel(0);
                if (subGraphModel != null)
                    return subGraphModel.Guid;
            }

            return GraphTool?.ToolState?.GraphModel?.Guid ?? default;
        }

        void ResolveAndAttachVisualizationContext(Hash128 visualizationContextId = default)
        {
            var currentRootGraphGuid = GetRootGraphModelGuid();
            if (!currentRootGraphGuid.isValid)
                return;

            // If the graph model has changed, reset the visualization context binding
            if (currentRootGraphGuid != m_RootGraphModelGuid)
            {
                m_CurrentVisualizationContextId = default;
                m_IsVisualizationContextAttached = false;
            }

            m_RootGraphModelGuid = currentRootGraphGuid;

            // Return early if the graph view is not initialized yet, or if we are already attached to a visualization context.
            if (GraphView == null || m_CurrentVisualizationContextId.isValid || m_IsVisualizationContextAttached)
                return;

            GraphVisualization.Session session;

            // Prefer the explicit context id if it matches this window's root graph.
            if (visualizationContextId.isValid && GraphVisualization.Registry.TryGetVisualizationSession(visualizationContextId, out var explicitSession) && explicitSession?.GraphID == m_RootGraphModelGuid)
            {
                session = explicitSession;
            }
            else
            {
                // Fallback: find any existing session for this root graph.
                GraphVisualization.Registry.TryGetVisualizationSessionForGraph(m_RootGraphModelGuid, out session);
            }

            if (session == null)
                return;

            m_CurrentVisualizationContextId = session.VisualizationContextID;
            m_IsVisualizationContextAttached = true;
            session.SessionIsAttached = true;
        }

        void OnVisualizationContextWillUnregister(Hash128 visualizationContextId)
        {
            // Check if the unregistered visualization context is the one we are attached to. If not, do nothing.
            if (m_CurrentVisualizationContextId != visualizationContextId)
                return;

            m_CurrentVisualizationContextId = default;
            m_IsVisualizationContextAttached = false;

            if (GraphVisualization.Registry.TryGetVisualizationSession(visualizationContextId, out var session))
                session.SessionIsAttached = false;
        }

        internal class TestAccess
        {
            readonly GraphViewEditorWindow m_Window;
            public TestAccess (GraphViewEditorWindow window)
            {
                m_Window = window;
            }

            public void Update() => m_Window.Update();
        }
    }
}
