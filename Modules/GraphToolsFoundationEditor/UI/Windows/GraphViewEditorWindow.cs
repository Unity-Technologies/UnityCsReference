// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for windows to edit graphs.
    /// </summary>
    abstract class GraphViewEditorWindow : EditorWindow, ISupportsOverlays, ISerializationCallbackReceiver
    {
        const string k_SaveChangesMessage = "Changes to an external graph asset in the breadcrumb have not been saved:\n\n{0}\nWould you like to save the graph asset?\n\n";
        bool m_HasMultipleWindowsForSameGraph;
        bool m_UnsavedChangesWindowIsEnabled;

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
        /// Finds a graph asset's opened window of type <typeparamref name="TWindow"/>. If no window is found, create a new one.
        /// The window is then opened and focused.
        /// </summary>
        /// <param name="graphAsset">The graph asset to display in the window. Pass null if you are looking for an empty window of type <typeparamref name="TWindow"/>.</param>
        /// <typeparam name="TWindow">The window type, which should derive from <see cref="GraphViewEditorWindow"/>.</typeparam>
        /// <returns>A window.</returns>
        public static TWindow ShowGraphInExistingOrNewWindow<TWindow>(GraphAsset graphAsset) where TWindow : GraphViewEditorWindow
        {
            TWindow window = null;

            var windowList = Resources.FindObjectsOfTypeAll<TWindow>();
            if (graphAsset != null)
            {
                window = windowList.FirstOrDefault(w =>
                        w.GraphTool.ToolState.CurrentGraph.GetGraphAsset() == graphAsset ||
                        w.GraphTool.ToolState.SubGraphStack.FirstOrDefault().GetGraphAsset() == graphAsset);
            }

            if (window == null)
            {
                window = windowList.FirstOrDefault(w => w.GraphTool.ToolState.CurrentGraph.GetGraphModel() == null);
            }

            bool isAssetOfSameTypeOpened = windowList.FirstOrDefault(w => w is TWindow) != null;

            if (window == null)
            {
                window = CreateWindow<TWindow>(isAssetOfSameTypeOpened ? typeof(TWindow) : typeof(SceneView));
            }

            window.Show();

            if (graphAsset != null)
            {
                window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
            }

            window.Focus();

            return window;
        }

        public static readonly string graphProcessingPendingUssClassName = "graph-processing-pending";

        static int s_LastFocusedEditor = -1;

        bool m_Focused;

        string m_InitialAssetNameCache;
        string m_CurrentAssetNameCache;
        bool m_InitialAssetDirtyCache;
        bool m_CurrentAssetDirtyCache;

        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected Label m_GraphProcessingPendingLabel;
        List<string> m_DisplayedOverlays;
        protected ShortcutBlocker_Internal m_ShortcutBlocker;

        GraphProcessingStatusObserver_Internal m_GraphProcessingStatusObserver;

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_WindowID;
#pragma warning restore CS0618

        [SerializeField]
        Hash128 m_WindowHash;

        public virtual IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        protected Hash128 WindowID => m_WindowHash;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool { get; private set; }

        public GraphView GraphView => m_GraphView;

        static GraphViewEditorWindow()
        {
            SetupLogStickyCallback();
        }

        protected GraphViewEditorWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            m_WindowHash = Hash128Extensions.Generate();
        }

        protected virtual BaseGraphTool CreateGraphTool()
        {
            return BaseGraphTool.Create<BaseGraphTool>(WindowID);
        }

        protected virtual BlankPage CreateBlankPage()
        {
            return new BlankPage(GraphTool, Enumerable.Empty<OnboardingProvider>());
        }

        protected virtual GraphView CreateGraphView()
        {
            return GraphView.Create(this, GraphTool, GraphTool.Name);
        }

        /// <summary>
        /// Creates a BlackboardView.
        /// </summary>
        /// <returns>A new BlackboardView.</returns>
        public virtual BlackboardView CreateBlackboardView()
        {
            return GraphView != null ? BlackboardView.Create(this, GraphView) : null;
        }

        /// <summary>
        /// Creates a MiniMapView.
        /// </summary>
        /// <returns>A new MiniMapView.</returns>
        public virtual MiniMapView CreateMiniMapView()
        {
            return MiniMapView.Create(this, GraphView);
        }

        /// <summary>
        /// Creates a ModelInspectorView.
        /// </summary>
        /// <returns>A new ModelInspectorView.</returns>
        public virtual ModelInspectorView CreateModelInspectorView()
        {
            return GraphView != null ? ModelInspectorView.Create(this, GraphView) : null;
        }

        /// <inheritdoc />
        public override void SaveChanges()
        {
            if (GraphTool?.ToolState != null)
            {
                // For now, it is not possible to individually close a specific breadcrumb. Thus, we have to save changes in the current opened graph AND in the other opened graphs as well.
                foreach (var openedGraph in GraphTool.ToolState.SubGraphStack.Append(GraphTool.ToolState.CurrentGraph))
                {
                    var graphAsset = openedGraph.GetGraphAsset();
                    if (graphAsset != null)
                        graphAsset.Save();
                }
            }

            base.SaveChanges();
        }

        /// <inheritdoc />
        public override void DiscardChanges()
        {
            if (GraphTool?.ToolState != null)
            {
                // For now, it is not possible to individually close a specific breadcrumb. Thus, we have to save changes in the current opened graph AND in the other opened graphs as well.
                foreach (var openedGraph in GraphTool.ToolState.SubGraphStack.Append(GraphTool.ToolState.CurrentGraph))
                {
                    var graphAsset = openedGraph.GetGraphAsset();
                    if (graphAsset != null)
                    {
                        // Unload the asset from memory. Any subsequent load of the asset will cause a new instance to be loaded from disk.
                        Resources.UnloadAsset(graphAsset);
                    }
                }
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
            m_WindowHash = Hash128Extensions.Generate();
        }

        void LoadLastOpenedGraph()
        {
            try
            {
                var graphModel = GraphTool?.ToolState.LastOpenedGraph.GetGraphModel();
                if (graphModel != null)
                {
                    GraphTool?.Dispatch(new LoadGraphCommand(graphModel,
                        GraphTool.ToolState.LastOpenedGraph.BoundObject,
                        LoadGraphCommand.LoadStrategies.KeepHistory));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual void OnEnable()
        {
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
            m_GraphView = CreateGraphView();
            m_BlankPage = CreateBlankPage();
            m_BlankPage?.CreateUI();


            rootVisualElement.Add(m_GraphContainer);

            m_GraphContainer.Add(m_GraphView);

            rootVisualElement.AddStylesheet_Internal("GraphViewWindow.uss");
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.AddToClassList("gtf-root");

            // After a domain reload, all loaded objects will get reloaded and their OnEnable() called again
            // It looks like all loaded objects are put in a deserialization/OnEnable() queue
            // the previous graph's nodes/wires/... might be queued AFTER this window's OnEnable
            // so relying on objects to be loaded/initialized is not safe
            // hence, we need to defer the loading command. Since we can be here from multiple scenarios,
            // i.e. EnterPlayMode, ProjectLoad, LayoutChanged, etc. or even just opening a new window
            // and each can have their own order of execution wrt OnEnable() and AfterAssemblyReload,
            // we therefore rely on EditorApplication.CallDelayed instead which will execute one tick later. We could load the
            // last opened graph in an EditorApplication.update event, but those event are handled by the test runner and it messes
            // with some tests.
            EditorApplication.CallDelayed(LoadLastOpenedGraph);
            EditorApplication.update += EditorUpdate;

            m_GraphProcessingPendingLabel = new Label("Graph Processing Pending") { name = "graph-processing-pending-label" };

            UpdateWindowTitle_Internal();

            if (GraphView?.DisplayMode == GraphViewDisplayMode.Interactive)
            {
                rootVisualElement.RegisterCallback<MouseMoveEvent>(GraphView.ProcessOnIdleAgent.OnMouseMove);
            }

            m_GraphProcessingStatusObserver = new GraphProcessingStatusObserver_Internal(m_GraphProcessingPendingLabel, GraphView?.GraphViewModel?.GraphProcessingState);

            GraphTool?.ObserverManager.RegisterObserver(m_GraphProcessingStatusObserver);

            foreach (var overlay in overlayCanvas.overlays)
            {
                overlay.RebuildContent();
            }

            m_UnsavedChangesWindowIsEnabled = IsUnsavedChangesWindowEnabled();

            m_ShortcutBlocker = new ShortcutBlocker_Internal();
            m_ShortcutBlocker.Enable(baseRootVisualElement);
        }

        protected virtual void OnDisable()
        {
            UpdateWindowsWithSameCurrentGraph_Internal(true);

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

            m_ShortcutBlocker.Disable();
            m_ShortcutBlocker = null;

            EditorApplication.update -= EditorUpdate;
        }

        protected virtual void OnFocus()
        {
            s_LastFocusedEditor = GetInstanceID();

            if (m_Focused)
                return;

            if (rootVisualElement == null)
                return;

            m_Focused = true;

            UpdateWindowsWithSameCurrentGraph_Internal(false);
        }

        /// <summary>
        /// Updates the focused windows and disables windows that have the same current graph as the focused window's.
        /// </summary>
        internal void UpdateWindowsWithSameCurrentGraph_Internal(bool currentWindowIsClosing)
        {
            var currentGraph = GraphTool?.ToolState?.CurrentGraph;
            if (currentGraph == null)
                return;

            if (GraphView != null && !GraphView.enabledSelf)
                GraphView.SetEnabled(true);

            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(GetType());
            var shouldUpdateFocusedWindow = false;

            foreach (var window in windows.Where(w => w.GetInstanceID() != s_LastFocusedEditor))
            {
                var otherGraph = window.GraphTool?.ToolState?.CurrentGraph;
                if (otherGraph != null && currentGraph.Equals(otherGraph))
                {
                    // Unfocused windows with the same graph are disabled
                    window.GraphView?.SetEnabled(false);

                    if (currentWindowIsClosing)
                    {
                        // If the current window is closing with changes, the changes need to be updated in other windows with the same graph to not lose the changes.
                        UpdateGraphModelState(window.GraphTool?.State.AllStateComponents.OfType<GraphModelStateComponent>().FirstOrDefault());
                    }
                    shouldUpdateFocusedWindow = !currentWindowIsClosing;
                }
            }

            if (shouldUpdateFocusedWindow)
            {
                UpdateGraphModelState(GraphTool.State.AllStateComponents.OfType<GraphModelStateComponent>().FirstOrDefault());
            }

            static void UpdateGraphModelState(GraphModelStateComponent graphModelState)
            {
                if (graphModelState == null)
                    return;

                // Update the focused window
                using var updater = graphModelState.UpdateScope;
                updater.ForceCompleteUpdate();
            }
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        bool IsUnsavedChangesWindowEnabled()
        {
            var unsavedChangesIsEnabled = GraphTool?.EnableUnsavedChangesDialogueWindow;
            if (unsavedChangesIsEnabled != null && m_UnsavedChangesWindowIsEnabled != unsavedChangesIsEnabled)
                return unsavedChangesIsEnabled.Value;
            return true;
        }

        protected void OnInspectorUpdate()
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
                Debug.Log($"UI Update ({GraphTool?.LastDispatchedCommandName_Internal ?? "Unknown command"}) took {sw.ElapsedMilliseconds} ms");
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Method called each frame as long as it exists, even when the window is hidden and detached from the panel.
        /// Used to update the models.
        /// </summary>
        protected virtual void EditorUpdate()
        {
            UpdateWindowTitle_Internal();

            m_UnsavedChangesWindowIsEnabled = IsUnsavedChangesWindowEnabled();
            UpdateHasUnsavedChanges_Internal();

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
                        m_GraphContainer.Remove(m_BlankPage);
                    if (!m_GraphContainer.Contains(m_GraphView))
                        m_GraphContainer.Insert(0, m_GraphView);

                    if (!rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                        rootVisualElement.Add(m_GraphProcessingPendingLabel);
                }
                else
                {
                    if (m_GraphContainer.Contains(m_GraphView))
                        m_GraphContainer.Remove(m_GraphView);
                    if (!m_GraphContainer.Contains(m_BlankPage))
                        m_GraphContainer.Insert(0, m_BlankPage);
                    if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                        rootVisualElement.Remove(m_GraphProcessingPendingLabel);
                }
            }
        }

        protected virtual void UpdateOverlays()
        {
            var graphModel = GraphTool?.ToolState.GraphModel;
            if (graphModel != null)
            {
                if (m_DisplayedOverlays != null)
                {
                    foreach (var overlayId in m_DisplayedOverlays)
                    {
                        if (TryGetOverlay(overlayId, out var overlay))
                        {
                            overlay.displayed = true;
                        }
                    }

                    m_DisplayedOverlays = null;
                }
            }
            else
            {
                if (m_DisplayedOverlays == null)
                {
                    m_DisplayedOverlays = new List<string>();
                    foreach (var overlay in overlayCanvas.overlays)
                    {
                        if (overlay.displayed)
                        {
                            m_DisplayedOverlays.Add(overlay.id);
                            overlay.displayed = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicates if the graphview window can handle the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">The asset we want to know if hte window handles</param>
        /// <returns>True if the window can handle the givne <paramref name="asset"/>. False otherwise.</returns>
        protected abstract bool CanHandleAssetType(GraphAsset asset);

        static void SetupLogStickyCallback()
        {
            ConsoleWindowHelper_Internal.SetEntryDoubleClickedDelegate((file, _) =>
            {
                var pathAndGuid = file.Split('@');

                var window = Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow)).OfType<GraphViewEditorWindow>().FirstOrDefault(w =>
                    w.GraphTool.ToolState.CurrentGraph.GetGraphAssetPath() == pathAndGuid[0] ||
                    w.GraphTool.ToolState.SubGraphStack.FirstOrDefault().GetGraphAssetPath() == pathAndGuid[0]);

                if (window != null)
                {
                    window.Focus();
                    var guid = Hash128.Compute(pathAndGuid[1]);
                    if (window.GraphView.GraphModel.TryGetModelFromGuid(guid, out var nodeModel))
                    {
                        var graphElement = nodeModel.GetView<GraphElement>(window.GraphView);
                        if (graphElement != null)
                        {
                            window.GraphView.DispatchFrameAndSelectElementsCommand(true, graphElement);
                        }
                    }
                }
            });
        }

        // Internal for tests
        internal void UpdateWindowTitle_Internal(bool forceUpdate = false)
        {
            var initialAsset = GraphTool?.ToolState?.SubGraphStack?.FirstOrDefault().GetGraphAsset();
            var currentAsset = GraphTool?.ToolState?.CurrentGraph.GetGraphAsset();

            var initialAssetName = (initialAsset == null) ? "" : initialAsset.Name;
            var currentAssetName = (currentAsset == null) ? "" : currentAsset.Name;
            var initialAssetDirty = (initialAsset == null) ? false : initialAsset.Dirty;
            var currentAssetDirty = (currentAsset == null) ? false : currentAsset.Dirty;

            if (!forceUpdate && initialAssetName == m_InitialAssetNameCache && currentAssetName == m_CurrentAssetNameCache && initialAssetDirty == m_InitialAssetDirtyCache && currentAssetDirty == m_CurrentAssetDirtyCache)
                return;

            var formattedTitle = FormatWindowTitle(initialAssetName, currentAssetName, initialAssetDirty, currentAssetDirty, out var toolTip);
            titleContent = new GUIContent(formattedTitle, GraphTool?.Icon, toolTip);

            m_InitialAssetNameCache = initialAssetName;
            m_CurrentAssetNameCache = currentAssetName;
            m_InitialAssetDirtyCache = initialAssetDirty;
            m_CurrentAssetDirtyCache = currentAssetDirty;
        }

        string FormatWindowTitle(string initialAssetName, string currentAssetName, bool initialAssetIsDirty, bool currentAssetIsDirty, out string completeTitle)
        {
            if (string.IsNullOrEmpty(initialAssetName) && string.IsNullOrEmpty(currentAssetName))
            {
                completeTitle = GraphTool?.Name ?? "";
                return completeTitle;
            }

            const int maxLength = 20; // Maximum limit of characters in a window primary tab
            const string ellipsis = "...";

            if (string.IsNullOrEmpty(initialAssetName))
            {
                var expectedLength = maxLength - ellipsis.Length - (currentAssetIsDirty ? 1 : 0); // The max length for the window title without the ellipsis and the dirty flag
                currentAssetName = currentAssetName.Length > maxLength ? currentAssetName.Substring(0, expectedLength) + ellipsis : currentAssetName;
                completeTitle = currentAssetName + (currentAssetIsDirty && m_HasMultipleWindowsForSameGraph ? "*" : "");
                return completeTitle;
            }

            var initialAssetDirtyStr = initialAssetIsDirty ? "*" : "";
            var currentAssetDirtyStr =  currentAssetIsDirty ? "*" : "";

            // In the case of multiple windows with the same graph, we manually add the (*) since EditorWindow.hasUnsavedChanges is always false for that case and will not add the (*).
            var multipleWindowsForSameGraphDirtyStr = !hasUnsavedChanges && (initialAssetIsDirty || currentAssetIsDirty && m_UnsavedChangesWindowIsEnabled && m_HasMultipleWindowsForSameGraph) ? " *" : "";

            // When EditorWindow.hasUnsavedChanges is true, it will add (*) at the end of the window title. We add a space before the (*) to avoid confusion with prior dirty flags. Eg: (InitialAssetName...*) CurrentAssetName...* *
            const string space = " ";
            // In the case the current graph is a subgraph, the window primary tab's naming should follow this format: (InitialAssetName...*) CurrentAssetName...*
            completeTitle = $"({initialAssetName}{initialAssetDirtyStr}) {currentAssetName}{currentAssetDirtyStr}{multipleWindowsForSameGraphDirtyStr}{space}";
            if (completeTitle.Length <= maxLength)
                return completeTitle;

            var dirtyCount = currentAssetIsDirty ? 1 : 0;
            if (initialAssetIsDirty)
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
        internal void UpdateHasUnsavedChanges_Internal()
        {
            if (!m_UnsavedChangesWindowIsEnabled)
                return;

            var initialAsset = GraphTool?.ToolState?.SubGraphStack?.FirstOrDefault().GetGraphAsset();
            var currentAsset = GraphTool?.ToolState?.CurrentGraph.GetGraphAsset();

            if (currentAsset == null)
            {
                hasUnsavedChanges = false;
                return;
            }

            var initialAssetDirty = initialAsset != null && initialAsset.Dirty;

            var oldHasMultipleWindowsForSameGraph = m_HasMultipleWindowsForSameGraph;
            m_HasMultipleWindowsForSameGraph = ((GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(GetType())).Where(w => w.GetInstanceID() != s_LastFocusedEditor).Select(window => window.GraphTool?.ToolState?.GraphModel).Any(otherGraph => otherGraph != null && currentAsset.GraphModel == otherGraph);

            hasUnsavedChanges = !m_HasMultipleWindowsForSameGraph && currentAsset.Dirty || initialAssetDirty;

            // The title update is triggered when there is a change in the title or the graph dirty state. When there are more than one window with the same graph, we need to force the title update:
            // The first window detects the dirty state change. However, the graph dirty state stays the same for the subsequent windows and the title update is not triggered.
            if (oldHasMultipleWindowsForSameGraph != m_HasMultipleWindowsForSameGraph)
                UpdateWindowTitle_Internal(true);

            GetSaveChangesMessage();
        }

        void GetSaveChangesMessage()
        {
            var pathsStr = "";

            if (GraphTool?.ToolState != null)
            {
                foreach (var openedGraph in GraphTool.ToolState.SubGraphStack.Append(GraphTool.ToolState.CurrentGraph))
                {
                    if (openedGraph.IsValid() && openedGraph.GetGraphAsset().Dirty)
                        pathsStr += openedGraph.GetGraphAssetPath() + "\n";
                }
            }
            saveChangesMessage = string.Format(k_SaveChangesMessage, string.IsNullOrEmpty(pathsStr) ? "Path not found." : pathsStr);
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
    }
}
