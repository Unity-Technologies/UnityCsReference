// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class ContentViewContainer : VisualElement
    {
        public override bool Overlaps(Rect r)
        {
            return true;
        }
    }

    /// <summary>
    /// Display modes for the GraphView.
    /// </summary>
    [UnityRestricted]
    internal enum GraphViewDisplayMode
    {
        /// <summary>
        /// The graph view will handle user interactions through events.
        /// </summary>
        Interactive,

        /// <summary>
        /// The graph view will only display the model.
        /// </summary>
        NonInteractive,
    }

    /// <summary>
    /// The <see cref="RootView"/> in which graphs are drawn.
    /// </summary>
    [UnityRestricted]
    internal class GraphView : RootView, IDragSource, IHasItemLibrary, IHasContextualMenuItems
    {
        public const int frameBorder = 30;

        /// <summary>
        /// The zoom level seen as very small.
        /// </summary>
        public const float VerySmallZoom = 0.11f;

        /// <summary>
        /// The zoom level seen as small.
        /// </summary>
        public const float SmallZoom = 0.20f;

        /// <summary>
        /// The zoom level seen as medium.
        /// </summary>
        public const float MediumZoom = 0.75f;

        GraphViewZoomMode m_ZoomMode;

        /// <summary>
        /// GraphView elements are organized into layers to ensure some type of graph elements
        /// are always drawn on top of others.
        /// </summary>
        [UnityRestricted]
        internal class Layer : VisualElement {}

        static readonly List<ChildView> k_UpdateAllUIs = new();

        /// <summary>
        /// The USS class name added to a <see cref="GraphView"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-graph-view";

        /// <summary>
        /// The USS class name added to a <see cref="GraphView"/> when it is not interactive.
        /// </summary>
        public static readonly string nonInteractiveUssClassName = ussClassName.WithUssModifier("non-interactive");

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        ContextualMenuManipulator m_ContextualMenuManipulator;
        ContentZoomer m_Zoomer;
        bool m_IsWireDragging;

        AutoAlignmentHelper m_AutoAlignmentHelper;
        AutoDistributingHelper m_AutoDistributingHelper;

        protected ItemLibraryHelper m_ItemLibraryHelper;

        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_MaxScaleOnFrame = 1.0f;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        float m_Zoom = 1.0f;
        Vector2 m_Pan;

        readonly VisualElement m_GraphViewContainer;
        readonly VisualElement m_MarkersParent;

        Dictionary<VisualElement, string>[] m_ElementsPerZoom = new Dictionary<VisualElement, string> [(int)GraphViewZoomMode.Unknown];

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>> m_SpacePartitioningByContainer;
        Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>> m_PlacematsUsingBoundingBoxSpacePartitioningByContainer;
        Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>> m_PlacematsUsingLayoutSpacePartitioningByContainer;
        static readonly ProfilerMarker k_SpacePartitioningUpdateMarker = new ProfilerMarker($"{nameof(GraphView)}.{nameof(UpdateSpacePartitioning)}");
        HashSet<GraphElementSpacePartitioningKey> m_GraphElementsInView;

        protected IDragAndDropHandler m_CurrentDragAndDropHandler;
        protected IDragAndDropHandler m_BlackboardDragAndDropHandler;
        protected IDragAndDropHandler m_SubgraphAssetDragAndDropHandler;

        protected bool m_SelectionDraggerWasActive;

        // The action used by the shortcut to delete and reconnect nodes.
        protected DeleteAndReconnectAction m_DeleteAndReconnectAction = new();

        StateObserver m_GraphViewGraphLoadedObserver;
        StateObserver m_GraphModelGraphLoadedAssetObserver;
        StateObserver m_SelectionGraphLoadedObserver;
        StateObserver m_ProcessingGraphLoadedObserver;
        StateObserver m_ProcessingErrorsGraphLoadedObserver;
        ExternalVariablesUpdater m_ExternalVariablesUpdater;
        SubgraphNodeUpdater m_SubgraphNodeUpdater;
        ModelViewUpdater m_UpdateObserver;
        WireOrderObserver m_WireOrderObserver;
        DeclarationHighlighter m_DeclarationHighlighter;
        AutoPlacementObserver m_AutoPlacementObserver;
        AutomaticGraphProcessingObserver m_AutomaticGraphProcessingObserver;
        GraphProcessingErrorObserver m_GraphProcessingErrorObserver;
        SpacePartitioningObserver m_SpacePartitioningObserver;
        GraphViewCullingObserver m_CullingObserver;


        /// <summary>
        /// The display mode.
        /// </summary>
        public GraphViewDisplayMode DisplayMode { get; }

        GraphViewCullingState m_CullingState;

        public virtual bool HasCullingOnZoom => true;

        /// <summary>
        /// The culling state of the <see cref="GraphView"/>. Setting this value will activate or deactivate culling for all <see cref="GraphElement"/>.
        /// </summary>
        public virtual GraphViewCullingState CullingState
        {
            get => m_CullingState;
            set
            {
                if (m_CullingState == value) return;
                m_CullingState = value;
                if (value == GraphViewCullingState.Enabled)
                    EnableCulling();
                else
                    DisableCulling();
            }
        }

        /// <summary>
        /// The current zoom level of the <see cref="GraphView"/>
        /// </summary>
        public float Zoom => m_Zoom;

        /// <summary>
        /// The current translation of the <see cref="GraphView"/>.
        /// </summary>
        public Vector2 Pan => m_Pan;

        /// <summary>
        /// Whether a wire is currently being dragged in the graph.
        /// </summary>
        public bool IsWireDragging
        {
            get => m_IsWireDragging;
            set
            {
                if (m_IsWireDragging != value)
                {
                    m_IsWireDragging = value;
                    // Clear any selection when a wire is being dragged.
                    if (m_IsWireDragging)
                        ClearSelection();
                }
            }
        }

        /// <summary>
        /// The VisualElement that contains all the views.
        /// </summary>
        public VisualElement ContentViewContainer { get; }

        GridBackground GridBackground { get; }

        /// <summary>
        /// The current <see cref="GraphViewZoomMode"/> of the graph view.
        /// </summary>
        public GraphViewZoomMode ZoomMode => m_ZoomMode;

        public SelectionDragger SelectionDragger
        {
            get => m_SelectionDragger;
            protected set => this.ReplaceManipulator(ref m_SelectionDragger, value);
        }

        protected ContentDragger ContentDragger
        {
            get => m_ContentDragger;
            set => this.ReplaceManipulator(ref m_ContentDragger, value);
        }

        protected Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        protected RectangleSelector RectangleSelector
        {
            get => m_RectangleSelector;
            set => this.ReplaceManipulator(ref m_RectangleSelector, value);
        }

        public FreehandSelector FreehandSelector
        {
            get => m_FreehandSelector;
            set => this.ReplaceManipulator(ref m_FreehandSelector, value);
        }

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        protected ContentZoomer ContentZoomer
        {
            get => m_Zoomer;
            set => this.ReplaceManipulator(ref m_Zoomer, value);
        }

        /// <summary>
        /// The agent responsible for triggering graph processing when the mouse is idle.
        /// </summary>
        public ProcessOnIdleAgent ProcessOnIdleAgent { get; }

        /// <summary>
        /// The model backing the graph view.
        /// </summary>
        public GraphRootViewModel GraphViewModel => (GraphRootViewModel)Model;

        /// <summary>
        /// The graph model displayed by the graph view.
        /// </summary>
        public GraphModel GraphModel => GraphViewModel?.GraphModelState?.GraphModel;

        /// <summary>
        /// The <see cref="ViewSelection"/> of the graph view.
        /// </summary>
        public ViewSelection ViewSelection { get; }

        /// <inheritdoc />
        public IReadOnlyList<GraphElementModel> GetSelection()
        {
            return ViewSelection?.GetSelection();
        }

        /// <summary>
        /// Container for the whole content of the graph, potentially partially visible.
        /// </summary>
        public override VisualElement contentContainer => m_GraphViewContainer;

        /// <summary>
        /// The layer in which <see cref="Placemat"/> are placed.
        /// </summary>
        public PlacematContainer PlacematContainer { get; }

        internal PositionDependenciesManager PositionDependenciesManager { get; }

        /// <summary>
        /// Instantiates another <see cref="GraphView"/> meant for display only.
        /// </summary>
        /// <remarks>Override this for the previews in Item Library to use your own class.</remarks>
        /// <returns>A new instance of <see cref="GraphView"/>.</returns>
        public virtual GraphView CreateSimplePreview()
        {
            return new GraphView(null, null, "",  null, null, GraphViewDisplayMode.NonInteractive);
        }

        public void RegisterElementZoomLevelClass(VisualElement element, GraphViewZoomMode zoomLevel, string ussClass)
        {
            m_ElementsPerZoom[(int)zoomLevel].Add(element, ussClass);
            if (m_ZoomMode >= zoomLevel && m_ZoomMode != GraphViewZoomMode.Unknown)
            {
                element.AddToClassList(ussClass);
            }
        }

        public void UnregisterElementZoomLevelClass(VisualElement element, GraphViewZoomMode zoomLevel)
        {
            if (m_ElementsPerZoom[(int)zoomLevel].TryGetValue(element, out var ussClass))
            {
                m_ElementsPerZoom[(int)zoomLevel].Remove(element);
                element.RemoveFromClassList(ussClass);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        /// <param name="window">The window to which the GraphView belongs.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="graphViewName">The name of the GraphView.</param>
        /// <param name="graphViewModel">The model for the view.</param>
        /// <param name="viewSelection">The selection helper.</param>
        /// <param name="displayMode">The display mode for the graph view.</param>
        /// <param name="typeHandleInfos">A <see cref="TypeHandleInfos"/> to use for this view. If null a new one will be created.</param>
        public GraphView(EditorWindow window, GraphTool graphTool, string graphViewName,
                            GraphRootViewModel graphViewModel,
                            ViewSelection viewSelection,
                            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive,
                            TypeHandleInfos typeHandleInfos = null)
            : base(window, graphTool, typeHandleInfos)
        {
            DisplayMode = displayMode;

            Model = graphViewModel;
            if (GraphTool != null)
            {
                if (DisplayMode == GraphViewDisplayMode.Interactive)
                {
                    ProcessOnIdleAgent = new ProcessOnIdleAgent(GraphTool.Preferences);
                    GraphTool.State.AddStateComponent(ProcessOnIdleAgent.StateComponent);
                }
            }

            ViewSelection = viewSelection;
            ViewSelection?.AttachToView(this);

            name = graphViewName ?? "GraphView_" + UnityEngine.Random.Range(0, Int32.MaxValue);

            AddToClassList(ussClassName);
            EnableInClassList(nonInteractiveUssClassName, DisplayMode == GraphViewDisplayMode.NonInteractive);

            m_GraphViewContainer = new VisualElement() { name = "graph-view-container" };
            m_GraphViewContainer.pickingMode = PickingMode.Ignore;
            hierarchy.Add(m_GraphViewContainer);

            ContentViewContainer = new ContentViewContainer
            {
                name = "content-view-container",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.GroupTransform
            };
            ContentViewContainer.style.transformOrigin = new TransformOrigin(0, 0, 0);
            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            m_GraphViewContainer.Add(ContentViewContainer);

            m_MarkersParent = new VisualElement { name = "marker-container" };

            for (int i = 0; i < (int)GraphViewZoomMode.Unknown; ++i)
            {
                m_ElementsPerZoom[i] = new Dictionary<VisualElement, string>();
            }

            this.AddPackageStylesheet("GraphView.uss");

            GridBackground = new GridBackground();
            Insert(0, GridBackground);

            PlacematContainer = new PlacematContainer(this);
            AddLayer(PlacematContainer, PlacematContainer.PlacematsLayer);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            PositionDependenciesManager = new PositionDependenciesManager(this, GraphTool?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper(this);
            m_AutoDistributingHelper = new AutoDistributingHelper(this);

            m_SpacePartitioningByContainer = new Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>>();
            m_PlacematsUsingLayoutSpacePartitioningByContainer = new Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>>();
            m_PlacematsUsingBoundingBoxSpacePartitioningByContainer = new Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>>();
            m_GraphElementsInView = new HashSet<GraphElementSpacePartitioningKey>();
            m_CullingState = GraphTool != null ? GraphViewCullingState.Enabled : GraphViewCullingState.Disabled;

            // The graph elements in view (and viewport culling) need to be updated when the size of the viewport changes.
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (DisplayMode == GraphViewDisplayMode.Interactive)
            {
                // The ContentDragger must be created before the ContextualMenuManipulator, so that it has a chance to stop the propagation if the is a right click drag and prevent the contextual menu to show up.
                ContentDragger = new ContentDragger();
                ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

                Clickable = new Clickable(OnDoubleClick);
                Clickable.activators.Clear();
                Clickable.activators.Add(
                    new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

                SelectionDragger = new SelectionDragger(this);
                RectangleSelector = new RectangleSelector();
                FreehandSelector = new FreehandSelector();

                RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
                RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

                RegisterCallback<MouseOverEvent>(OnMouseOver);
                RegisterCallback<MouseMoveEvent>(OnMouseMove);

                RegisterCallback<DragEnterEvent>(OnDragEnter);
                RegisterCallback<DragLeaveEvent>(OnDragLeave);
                RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                RegisterCallback<DragExitedEvent>(OnDragExited);
                RegisterCallback<DragPerformEvent>(OnDragPerform);

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

                RegisterCallback<ShortcutFrameAllEvent>(OnShortcutFrameAllEvent);
                RegisterCallback<ShortcutFrameOriginEvent>(OnShortcutFrameOriginEvent);
                RegisterCallback<ShortcutShowItemLibraryEvent>(OnShortcutShowItemLibraryEvent);
                RegisterCallback<ShortcutConvertConstantAndVariableEvent>(OnShortcutConvertVariableAndConstantEvent);
                RegisterCallback<ShortcutConvertWireToPortalEvent>(OnShortcutConvertWireToPortalEvent);
                RegisterCallback<ShortcutCreateLocalSubgraphFromSelectionEvent>(OnShortcutCreateLocalSubgraphFromSelectionEvent);

                // TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
                // RegisterCallback<ShortcutAlignNodesEvent>(OnShortcutAlignNodesEvent);
                // RegisterCallback<ShortcutAlignNodeHierarchiesEvent>(OnShortcutAlignNodeHierarchyEvent);
                RegisterCallback<ShortcutCreateStickyNoteEvent>(OnShortcutCreateStickyNoteEvent);
                RegisterCallback<ShortcutCreatePlacematEvent>(OnShortcutCreatePlacematEvent);
                RegisterCallback<ShortcutDisconnectWiresEvent>(OnShortcutDisconnectWiresEvent);
                RegisterCallback<ShortcutToggleNodeCollapseEvent>(OnShortcutToggleNodeCollapseEvent);
                RegisterCallback<ShortcutExtractContentsToPlacematEvent>(OnShortcutExtractContentsToPlacematEvent);
                RegisterCallback<ShortcutDeleteAndReconnectEvent>(OnShortcutDeleteAndReconnect);

                RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            }
            else
            {
                void StopEvent(EventBase e)
                {
                    e.StopImmediatePropagation();
                    focusController.IgnoreEvent(e);
                }

                pickingMode = PickingMode.Ignore;

                RegisterCallback<MouseDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseMoveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<PointerDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerMoveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<MouseEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseLeaveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseOverEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseOutEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<WheelEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<PointerEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerLeaveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerOverEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerOutEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<KeyDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<KeyUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<DragEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragExitedEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragPerformEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragUpdatedEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragLeaveEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<ClickEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<ContextClickEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<ValidateCommandEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<ExecuteCommandEvent>(StopEvent, TrickleDown.TrickleDown);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            ViewSelection?.DetachFromView();
        }

        /// <inheritdoc />
        protected override void RegisterCommandHandlers(CommandHandlerRegistrar registrar)
        {
            if (GraphTool != null && DisplayMode == GraphViewDisplayMode.Interactive)
            {
                GraphViewCommandsRegistrarHelper.RegisterCommands(registrar, this);
            }
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.Dispatch(command, diagnosticsFlags);
            }
        }

        public void ClearGraph()
        {
            // Directly query the UI here - slow, but the usual path of going through GraphModel.GraphElements
            // won't work because it's often not initialized at this point
            var elements = ContentViewContainer.Query<GraphElement>().ToList();

            PositionDependenciesManager.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element);
            }

            ClearSpacePartitioning();

            if (GraphViewModel?.SpacePartitioningState != null)
            {
                //This is needed because, when the current GraphModel is changed to null, the GraphView registers all its elements for culling removal. But space partition is not updated if GraphModel == null.
                //if you then reopen the same graph, the space partition will still have all models as needing removal, therefore nothing will be culled.
                // This is shown when running NodeCullingTests.NodeAreCulledWithWidthBasedOnLongTitleHasBeenSetup(??, ReloadKind.LoadingGraph) that fails without this line because the node is not culled when it should.
                using (var spUpdater = GraphViewModel.SpacePartitioningState.UpdateScope)
                {
                    spUpdater.ForceCompleteUpdate();
                }
            }
        }

        static ProfilerMarker s_UpdateViewTransformMarker = new ProfilerMarker("UpdateAllElementsLevelOfDetail");
        /// <summary>
        /// Updates the graph view pan and zoom.
        /// </summary>
        /// <remarks>This method only updates the view pan and zoom and does not save the
        /// new values in the state. To make the change persistent, dispatch a
        /// <see cref="ReframeGraphViewCommand"/>.</remarks>
        /// <param name="pan">The new coordinate at the top left corner of the graph view.</param>
        /// <param name="zoom">The new zoom factor of the graph view.</param>
        /// <seealso cref="ReframeGraphViewCommand"/>
        public void UpdateViewTransform(Vector3 pan, Vector3 zoom)
        {
            // If pan and zoom are the default values, re-frame to see all elements in the graph
            if (ShouldFrameAllOnFirstLoad && GraphModel != null && pan == GraphViewStateComponent.defaultPosition && zoom == GraphViewStateComponent.defaultScale)
            {
                // Needs to schedule to have the graph elements views
                schedule.Execute(this.DispatchFrameAllCommand).ExecuteLater(0);
                return;
            }

            s_UpdateViewTransformMarker.Begin();

            GridBackground.MarkDirtyRepaint();
            float validateFloat = pan.x + pan.y + pan.z + zoom.x + zoom.y + zoom.z;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
                return;

            pan.x = this.RoundToPanelPixelSize(pan.x);
            pan.y = this.RoundToPanelPixelSize(pan.y);

            m_Zoom = zoom.x;
            m_Pan = pan;

            ContentViewContainer.style.translate = new StyleTranslate(new Translate(pan.x, pan.y));

            Vector3 oldScale = ContentViewContainer.resolvedStyle.scale.value;

            if (oldScale != zoom)
            {
                GraphViewZoomMode oldMode = m_ZoomMode;
                if (zoom.x < VerySmallZoom)
                    m_ZoomMode = GraphViewZoomMode.VerySmall;
                else if (zoom.x < SmallZoom)
                    m_ZoomMode = GraphViewZoomMode.Small;
                else if (zoom.x < MediumZoom)
                    m_ZoomMode = GraphViewZoomMode.Medium;
                else
                    m_ZoomMode = GraphViewZoomMode.Normal;

                for (GraphViewZoomMode mode = GraphViewZoomMode.Medium; mode < GraphViewZoomMode.Unknown; ++mode)
                {
                    if (m_ZoomMode >= mode && oldMode < mode)
                    {
                        foreach (var kv in m_ElementsPerZoom[(int)mode])
                        {
                            kv.Key.AddToClassList(kv.Value);
                        }
                    }
                    else if (m_ZoomMode < mode && oldMode >= mode)
                    {
                        foreach (var kv in m_ElementsPerZoom[(int)mode])
                        {
                            kv.Key.RemoveFromClassList(kv.Value);
                        }
                    }
                }

                ContentViewContainer.style.scale = new StyleScale(new Scale(zoom));

                RectangleSelector?.MarkDirtyRepaint();
                FreehandSelector?.MarkDirtyRepaint();

                UpdateAllElementsLevelOfDetail(zoom.x, m_ZoomMode, oldMode);
            }

            UpdateGraphElementsInView();
            s_UpdateViewTransformMarker.End();
        }

        void UpdateAllElementsLevelOfDetail(float zoomLevel, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            if (GraphModel != null)
            {
                using var cullingUpdater = GraphViewModel?.GraphViewCullingState?.UpdateScope;
                var needZoomCullingUpdate = CullingState == GraphViewCullingState.Enabled && cullingUpdater != null && IsZoomCullingTransition(newZoomMode, oldZoomMode);
                var newCullingState = IsZoomCullingSize(newZoomMode) ? GraphViewCullingState.Enabled : GraphViewCullingState.Disabled;
                GraphModel.GetGraphElementModels().GetAllViewsRecursively(this, _ => true, k_UpdateAllUIs);
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var graphElement in k_UpdateAllUIs.OfType<GraphElement>())
#pragma warning restore RS0030
                {
                    graphElement.SetLevelOfDetail(zoomLevel, newZoomMode, oldZoomMode);
                    if (HasCullingOnZoom && needZoomCullingUpdate)
                        cullingUpdater.MarkGraphElementCullingChanged(graphElement, GraphViewCullingSource.Zoom, newCullingState);
                }

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var marker in m_MarkersParent.Children().OfType<Marker>())
#pragma warning restore RS0030
                {
                    marker.SetLevelOfDetail(zoomLevel, newZoomMode, oldZoomMode);

                    if (HasCullingOnZoom && needZoomCullingUpdate)
                        cullingUpdater.MarkGraphElementCullingChanged(marker, GraphViewCullingSource.Zoom, newCullingState);
                }

                k_UpdateAllUIs.Clear();
            }
        }

        static bool IsZoomCullingSize(GraphViewZoomMode zoomMode)
        {
            return zoomMode is GraphViewZoomMode.VerySmall or GraphViewZoomMode.Small;
        }

        static bool IsZoomCullingTransition(GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            if (IsZoomCullingSize(newZoomMode) && !IsZoomCullingSize(oldZoomMode))
                return true;
            if (!IsZoomCullingSize(newZoomMode) && IsZoomCullingSize(oldZoomMode))
                return true;
            return false;
        }

        /// <summary>
        /// Base speed for panning, made internal to disable panning in tests.
        /// </summary>
        internal static float BasePanSpeed { get; set; } = 0.4f;
        internal const int k_PanIntervalMs = 10; // interval between each pan in milliseconds
        internal static float MinPanSpeed => GraphViewSettings.k_PanMinSpeedFactor * BasePanSpeed;
        internal static float MaxPanSpeed => GraphViewSettings.k_PanMaxSpeedFactor * BasePanSpeed;

        /// <summary>
        /// Whether the graph must frame all graph elements when it is first loaded. Internal to allow to disable in tests.
        /// </summary>
        internal static bool ShouldFrameAllOnFirstLoad = true;

        static float ApplyEasing(float x, GraphViewSettings.EasingFunction function)
        {
            x = Mathf.Clamp(x, 0f, 1f);
            switch (function)
            {
                case GraphViewSettings.EasingFunction.InOutCubic:
                    return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
                case GraphViewSettings.EasingFunction.InOutQuad:
                    return x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2;
            }
            return x;
        }

        /// <summary>
        /// Gets pan speed depending on distance.
        /// </summary>
        /// <param name="distanceRatio">The distance between 0 and 1, 1 being the furthest.</param>
        /// <returns>A speed between user settings min and max.</returns>
        internal static float GetPanSpeed(float distanceRatio)
        {
            distanceRatio = ApplyEasing(distanceRatio, GraphViewSettings.k_PanEasingFunction);
            return distanceRatio * (MaxPanSpeed - MinPanSpeed) + MinPanSpeed;
        }

        internal static float GetPanBorderSize(Vector2 viewSize)
        {
            if (!GraphViewSettings.k_PanUsePercentage)
                return GraphViewSettings.k_PanAreaSize;

            var minLayoutSize = Mathf.Min(viewSize.x, viewSize.y);
            return GraphViewSettings.k_PanAreaSize / 100f * minLayoutSize;
        }

        internal float GetPanBorderSize()
        {
            return GetPanBorderSize(layout.size);
        }

        internal static Vector2 GetPanSpeed(Vector2 mousePos, Vector2 viewSize)
        {
            var effectiveSpeed = Vector2.zero;

            var panAreaSize = GetPanBorderSize(viewSize);
            if (mousePos.x <= panAreaSize)
                effectiveSpeed.x = -GetPanSpeed((panAreaSize - mousePos.x) / panAreaSize);
            else if (mousePos.x >= viewSize.x - panAreaSize)
                effectiveSpeed.x = GetPanSpeed((mousePos.x - (viewSize.x - panAreaSize)) / panAreaSize);

            if (mousePos.y <= panAreaSize)
                effectiveSpeed.y = -GetPanSpeed((panAreaSize - mousePos.y) / panAreaSize);
            else if (mousePos.y >= viewSize.y - panAreaSize)
                effectiveSpeed.y = GetPanSpeed((mousePos.y - (viewSize.y - panAreaSize)) / panAreaSize);

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, MaxPanSpeed);

            return effectiveSpeed;
        }

        internal Vector2 GetEffectivePanSpeed(Vector2 worldMousePos)
        {
            var localMouse = contentContainer.WorldToLocal(worldMousePos);
            return GetPanSpeed(localMouse, contentContainer.layout.size);
        }

        /// <summary>
        /// Gets a <see cref="IDragAndDropHandler"/> that can handle dragged and dropped objects from the blackboard.
        /// </summary>
        protected virtual IDragAndDropHandler BlackboardDragAndDropHandler => m_BlackboardDragAndDropHandler ??= new SelectionDropperDropHandler(this);

        /// <summary>
        /// Gets a <see cref="IDragAndDropHandler"/> that can handle dragged and dropped graph assets into the graph.
        /// </summary>
        protected virtual IDragAndDropHandler GraphAssetDragAndDropHandler => m_SubgraphAssetDragAndDropHandler ??= new SubgraphDragAndDropHandler(this);

        /// <summary>
        /// Find an appropriate drag and drop handler for the current drag and drop operation.
        /// </summary>
        /// <returns>The <see cref="IDragAndDropHandler"/> that can handle the objects being dragged.</returns>
        protected virtual IDragAndDropHandler GetDragAndDropHandler()
        {
            var selectionDropperDropHandler = BlackboardDragAndDropHandler;
            if (selectionDropperDropHandler?.CanHandleDrop() ?? false)
                return selectionDropperDropHandler;

            var graphAssetDragAndDropHandler = GraphAssetDragAndDropHandler;
            if (graphAssetDragAndDropHandler?.CanHandleDrop() ?? false)
                return graphAssetDragAndDropHandler;

            return null;
        }

        void AddLayer(Layer layer, int index)
        {
            m_ContainerLayers.Add(index, layer);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            int indexOfLayer = m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value).ToList().IndexOf(layer);
#pragma warning restore RS0030

            ContentViewContainer.Insert(indexOfLayer, layer);
        }

        void AddLayer(int index)
        {
            Layer layer = new Layer { pickingMode = PickingMode.Ignore };
            AddLayer(layer, index);
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        internal void ChangeLayer(GraphElement element)
        {
            if (!m_ContainerLayers.ContainsKey(element.Layer))
                AddLayer(element.Layer);

            var oldContainer = element.parent;
            GetLayer(element.Layer).Add(element);

            if (element.parent == oldContainer)
                return;

            using (var updater = GraphViewModel.SpacePartitioningState.UpdateScope)
            {
                updater.MarkGraphElementForChangingContainer(element, oldContainer, element.parent);
            }
        }

        /// <summary>
        /// Configures the zoom settings for the <see cref="GraphView"/>.
        /// </summary>
        /// <param name="minScaleSetup">The minimum allowed zoom scale.</param>
        /// <param name="maxScaleSetup">The maximum allowed zoom scale.</param>
        /// <param name="maxScaleOnFrame">The maximum zoom scale when framing content.</param>
        /// <remarks>
        /// 'SetupZoom' configures the zoom settings for the <see cref="GraphView"/> and defines the allowed zoom range and the maximum zoom level when framing content.
        /// This method ensures that the <see cref="GraphView"/> maintains an appropriate level of zoom control, which improves navigation and usability. It also ensures that the
        /// scale values are appropriately set to balance visibility and accessibility. Incorrect values may lead to a poor user experience, such as excessive zooming
        /// that distorts the layout or prevents users from viewing the entire graph effectively.
        /// </remarks>
        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, maxScaleOnFrame, m_ScaleStep, m_ReferenceScale);
        }

        /// <summary>
        /// Configures the zoom settings for the <see cref="GraphView"/>.
        /// </summary>
        /// <param name="minScaleSetup">The minimum allowed zoom scale.</param>
        /// <param name="maxScaleSetup">The maximum allowed zoom scale.</param>
        /// <param name="maxScaleOnFrame">The maximum zoom scale when framing content.</param>
        /// <param name="scaleStepSetup">The <see cref="ContentZoomer.ScaleStep"/>.</param>
        /// <param name="referenceScaleSetup">The <see cref="ContentZoomer.ReferenceScale"/>.</param>
        /// <remarks>
        /// 'SetupZoom' configures the zoom settings for the <see cref="GraphView"/>, defining the allowed zoom range and the maximum zoom level when framing content.
        /// This method ensures that the <see cref="GraphView"/> maintains an appropriate level of zoom control, improving navigation and usability. Ensure that the
        /// scale values are appropriately set to balance visibility and accessibility. Incorrect values may lead to a poor user experience, such as excessive zooming
        /// that distorts the layout or prevents users from viewing the entire graph effectively.
        /// Use this method to set up the <see cref="ContentZoomer.ScaleStep"/> and the <see cref="ContentZoomer.ReferenceScale"/> in addition to the standard zoom constraints.
        /// The <paramref name="scaleStepSetup"/> defines the relative scale change when zooming in or out. The <paramref name="referenceScaleSetup"/>
        /// specifies the scale to apply when the scroll wheel offset is zero.
        /// </remarks>
        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame, float scaleStepSetup, float referenceScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
            m_MaxScaleOnFrame = maxScaleOnFrame;
            m_ScaleStep = scaleStepSetup;
            m_ReferenceScale = referenceScaleSetup;
            UpdateContentZoomer();
        }

        void UpdateContentZoomer()
        {
            if (Math.Abs(m_MinScale - m_MaxScale) > float.Epsilon)
            {
                ContentZoomer = new ContentZoomer
                {
                    MinScale = m_MinScale,
                    MaxScale = m_MaxScale,
                    ScaleStep = m_ScaleStep,
                    ReferenceScale = m_ReferenceScale
                };
            }
            else
            {
                ContentZoomer = null;
            }

            ValidateTransform();
        }

        void ValidateTransform()
        {
            if (ContentViewContainer == null)
                return;
            Vector3 transformScale = resolvedStyle.scale.value;

            transformScale.x = Mathf.Clamp(transformScale.x, m_MinScale, m_MaxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, m_MinScale, m_MaxScale);

            style.scale = new StyleScale(new Scale(transformScale));
        }

        /// <summary>
        /// Populates the contextual menu for the graph view.
        /// </summary>
        /// <param name="evt">The event sent to populate the contextual menu.</param>
        /// <remarks>
        /// 'BuildContextualMenu' populates the contextual menu for the graph view. You can override this method
        /// to customize the menu items, so you can add or modify context-specific options
        /// based on the needs of the graph. When you override this method, you must call the base implementation
        /// to ensure that all necessary functionalities required for the graph to function correctly are included.
        /// Failing to do so may result in missing essential actions, such as node creation, selection operations,
        /// or other core graph interactions.
        /// </remarks>
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // If the menu already has items, append a separator.
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            var selectionSource = GetSelection();
            var selection = new List<GraphElementModel>(selectionSource.Count);
            for (var i = 0; i < selectionSource.Count; i++)
            {
                selection.Add(selectionSource[i]);
            }

            // Get the categorized menu items based on the selection.
            var categorizedMenuItems = GetMenuItemsForSpecialSelectionCases(selection, evt) ?? ContextualMenuHelpers.GetMenuItemsForSelection(selection);

            // If there are no categorized menu items, we can return early.
            if (categorizedMenuItems == null)
                return;

            var menuActionMap = new Dictionary<string, Action>();
            PopulateContextualMenuActionMap(menuActionMap, evt, selection);
            ViewSelection.BuildContextualMenu(categorizedMenuItems, evt, menuActionMap);

            if (Unsupported.IsDeveloperBuild())
                AppendDeveloperBuildMenuActions(evt, selection);
        }

        // CONTEXTUAL MENU METHODS:
        Dictionary<ContextualMenuCategory, List<ContextualMenuItem>> GetMenuItemsForSpecialSelectionCases(List<GraphElementModel> selection, ContextualMenuPopulateEvent evt)
        {
            // This method is used to get the contextual menu items for specific cases:
            // - If the selection contains placeholders.
            // - If the selection contains only wires.
            // - If the user right-clicked on a placemat title or on an empty part of a placemat.
            // - If the user right-clicked on an empty space in the graph view while there are graph elements selected.
            // - If the user right-clicked on an empty space in the graph view while there is nothing selected graph.

            if (selection == null)
                return null;

            var allWires = true;

            for (var i = 0; i < selection.Count; i++)
            {
                if (selection[i] is not WireModel)
                    allWires = false;

                // If there is a placeholder in the selection, we only append the "Delete" menu item.
                if (evt.target is not GraphView && selection[i] is IPlaceholder || selection[i] is IHasDeclarationModel { DeclarationModel: IPlaceholder })
                    return ContextualMenuHelpers.CategorizeMenuItems(new[] { ContextualMenuHelpers.deleteItem });

                // If there is a placemat in the selection, we only append placemat menu items:
                // - If the user clicked on the placemat title
                // - If the user clicked on an empty part of the placemat (not on a contained element)
                if (selection[i] is PlacematModel placematModel)
                {
                    var placemat = placematModel.GetView<Placemat>(this);
                    if (placemat == null)
                        continue;

                    var clickedOnPlacematTitle = placemat.ContainsPoint(placemat.WorldToLocal(evt.mousePosition));
                    if (clickedOnPlacematTitle)
                        return ContextualMenuHelpers.CategorizeMenuItems(placematModel.ContextualMenuItems);

                    var clickedOnEmptyPartOfPlacemat = evt.target is GraphView && placemat.parent.layout.Contains(placemat.WorldToLocal(evt.mousePosition));
                    if (clickedOnEmptyPartOfPlacemat)
                        return ContextualMenuHelpers.CategorizeMenuItems(placematModel.ContextualMenuItems);
                }
            }

            var clickedOnEmptySpace = evt.target is GraphView;
            if (clickedOnEmptySpace && selection.Count > 0)
            {
                // Check if the user clicked on any of the selected elements.
                foreach (var elementModel in selection)
                {
                    var elementView = elementModel.GetView(this);
                    if (elementView == null)
                        continue;

                    if (elementView.ContainsPoint(elementView.WorldToLocal(evt.mousePosition)))
                    {
                        clickedOnEmptySpace = false;
                        break;
                    }
                }
            }

            // If there are no selected elements OR the user right-clicked on an empty space, show the menu for the graph view.
            if (selection.Count == 0 || clickedOnEmptySpace)
            {
                return ContextualMenuHelpers.CategorizeMenuItems(ContextualMenuItems);
            }

            // All selected elements are wires: show wire-specific menu items.
            if (allWires && selection[0] is WireModel wireModel)
                return ContextualMenuHelpers.CategorizeMenuItems(wireModel.ContextualMenuItems);

            return null;
        }

        void AppendDeveloperBuildMenuActions(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Refresh All UI", _ =>
            {
                using (var updater = GraphViewModel.GraphViewState.UpdateScope)
                {
                    updater.ForceCompleteUpdate();
                }
            });

            if (selection.Count > 0)
            {
                evt.menu.AppendAction("Refresh Selected Element(s)",
                    _ =>
                    {
                        using (var graphUpdater = GraphViewModel.GraphModelState.UpdateScope)
                        {
                            graphUpdater.MarkChanged(selection);
                        }
                    });
            }

            evt.menu.AppendAction("Log Graph IDs", _ =>
            {
                Debug.Log($"ToolStateComponent.CurrentGraph: {GraphTool.ToolState.CurrentGraph}");
                var gmsRef = GraphViewModel.GraphModelState.GraphModel.GetGraphReference();
                Debug.Log($"GraphModelStateComponent.GraphModel ref: {gmsRef}");
            });
        }

        /// <summary>
        /// Populates the contextual menu action map with actions for the graph view.
        /// </summary>
        /// <param name="menuActionMap">The contextual menu action map.</param>
        /// <param name="evt">The <see cref="ContextualMenuPopulateEvent"/>.</param>
        /// <param name="selection">The current selection of graph element models in the graph.</param>
        /// <remarks>
        /// This method maps <see cref="ContextualMenuItem"/>s' names to actions that can be executed when the user selects
        /// the corresponding menu item in the contextual menu. It is called by the <see cref="BuildContextualMenu"/> method
        /// to populate the contextual menu with actions that can be performed on the graph elements in the current selection.
        /// You can override this method to add custom actions or modify existing ones.
        /// </remarks>
        protected virtual void PopulateContextualMenuActionMap(Dictionary<string, Action> menuActionMap, ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (menuActionMap == null)
                return;

            // ViewSelection menu items
            ViewSelection.PopulateMenuActionMap(menuActionMap, evt);

            // Common graph element items:
            menuActionMap.Add(ContextualMenuHelpers.createPlacematItem.Name, () => AppendCreatePlacematMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.colorItem.Name, () => AppendColorMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.frameSelectionItem.Name, () => AppendFrameSelectionMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.alignAndDistributeElementsItem.Name, () => AppendAlignAndDistributeElementsMenuItems(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.createLocalSubgraphFromSelectionItem.Name, () => AppendCreateLocalSubgraphFromSelectionMenuItem(evt));

            // GraphView menu items:
            menuActionMap.Add(ContextualMenuHelpers.addNodeItem.Name, () => AppendAddNodeItemMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.createStickyNoteItem.Name, () => AppendCreateStickyNoteMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.createEmptyLocalSubgraphItem.Name, () => AppendCreateEmptyLocalSubgraph(evt));

            // Nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.deleteAndReconnectItem.Name, () => AppendDeleteAndReconnectMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.toggleCollapseItem.Name, () => AppendToggleCollapseMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.disableNodeItem.Name, () => AppendDisableNodeMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.disconnectAllWiresItem.Name, () => AppendDisconnectAllWiresMenuItem(evt, selection));
            // TODO (GTF-2216): Implement the Edit Subtitle functionality.
            // TODO: Implement the Bypass functionality.

            // State nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.createLocalTransitionMenuItem.Name, () => AppendCreateTransitionMenuItem(evt, selection, TransitionSupportKind.Local));
            menuActionMap.Add(ContextualMenuHelpers.createOnEnterTransitionMenuItem.Name, () => AppendCreateTransitionMenuItem(evt, selection, TransitionSupportKind.OnEnter));
            menuActionMap.Add(ContextualMenuHelpers.createSelfTransitionMenuItem.Name, () => AppendCreateTransitionMenuItem(evt, selection, TransitionSupportKind.Self));
            menuActionMap.Add(ContextualMenuHelpers.setAsDefaultStateMenuItem.Name, () => AppendSetAsDefaultStateMenuItem(evt, selection));
            // TODO (GTF-2242): Implement the Create Transition functionality.

            // Subgraph nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.extractContentsToPlacematItem.Name, () => AppendExtractContentsToPlacematMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.openAssetSubgraphItem.Name, () => AppendOpenSubgraphMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.openLocalSubgraphItem.Name, () => AppendOpenSubgraphMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.convertToAssetSubgraphItem.Name, () => AppendConvertToAssetSubgraphMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.unpackToLocalSubgraphItem.Name, () => AppendUnpackToLocalSubgraphMenuItem(evt));
            menuActionMap.Add(ContextualMenuHelpers.findAssetInProjectItem.Name, () => AppendFindAssetInProjectMenuItem(evt, selection));

            // Variable and constant nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.convertToConstantItem.Name, () => AppendConvertToConstantMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.convertToVariableItem.Name, () => AppendConvertToVariableMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.itemizeItem.Name, () => AppendItemizeMenuItem(evt, selection));

            // Wire menu items:
            menuActionMap.Add(ContextualMenuHelpers.insertNodeItem.Name, () => AppendInsertNodeMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.convertToPortalsItem.Name, () => AppendConvertToPortalsMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.reorderWireItem.Name, () => AppendReorderWireMenuItem(evt, selection));
            // TODO: Implement the insert junction feature.

            // Context and block nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.addBlockItem.Name, () => AppendAddNodeItemMenuItem(evt, "Add Block"));
            menuActionMap.Add(ContextualMenuHelpers.insertBlockAboveItem.Name, () => AppendInsertBlockItemMenuItem(evt, selection, true));
            menuActionMap.Add(ContextualMenuHelpers.insertBlockBelowItem.Name, () => AppendInsertBlockItemMenuItem(evt, selection, false));
            // TODO: Implement the "Convert to Block Subgraph" functionality.

            // Sticky notes menu items:
            menuActionMap.Add(ContextualMenuHelpers.fontSizeAndThemeItem.Name, () => AppendFontSizeAndThemeMenuItem(evt, selection));
            // TODO: Implement the "Fit to Text" functionality. See: ContextualMenuHelpers.fitToTextItem

            // Placemat menu items:
            menuActionMap.Add(ContextualMenuHelpers.deleteAndSelectContentsItem.Name, () => AppendDeleteAndSelectContentsMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.smartResizeItem.Name, () => AppendSmartResizeMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.reorderPlacematItem.Name, () => AppendReorderPlacematMenuItems(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.selectAllPlacematContentsItem.Name, () => AppendSelectAllPlacematContentsMenuItem(evt, selection));

            // Portal nodes menu items:
            menuActionMap.Add(ContextualMenuHelpers.createOppositePortalItem.Name, () => AppendCreateOppositePortalMenuItem(evt, selection));
            menuActionMap.Add(ContextualMenuHelpers.revertToWireItem.Name, () => AppendRevertWiresMenuItem(evt, selection, false));
            menuActionMap.Add(ContextualMenuHelpers.revertAllToWiresItem.Name, () => AppendRevertWiresMenuItem(evt, selection, true));
        }

        void AppendCreateOppositePortalMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.AllowPortalCreation)
                return;

            var portals = new List<WirePortalModel>();
            var enable = true;
            foreach (var elementModel in selection)
            {
                // If the element is not a portal, we don't append this menu item.
                if (elementModel is not WirePortalModel portal)
                    return;

                // If one of the selected portals cannot create its opposite portal, we disable the menu item.
                if (!portal.CanCreateOppositePortal())
                    enable = false;

                portals.Add(portal);
            }

            evt.menu.AppendAction(L10n.Tr("Create Opposite Portal"),
                _ =>
                {
                    Dispatch(new CreateOppositePortalCommand(portals));
                }, enable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendRevertWiresMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, bool revertAll)
        {
            if (!GraphModel.AllowPortalCreation)
                return;

            var portals = new List<WirePortalModel>();
            var enable = true;
            foreach (var elementModel in selection)
            {
                // If the element is not a portal, we don't append this menu item.
                if (elementModel is not WirePortalModel portal)
                    return;

                // If one of the selected portals cannot be reverted, we disable the menu item.
                if (!portal.CanRevertToWire())
                {
                    enable = false;
                    break;
                }

                portals.Add(portal);
            }

            if (revertAll)
            {
                evt.menu.AppendAction(L10n.Tr("Revert All to Wires"),
                    _ =>
                    {
                        Dispatch(new RevertAllPortalsToWireCommand(portals));
                    }, enable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            else
            {
                evt.menu.AppendAction(L10n.Tr("Revert to Wire"),
                    _ =>
                    {
                        Dispatch(new RevertPortalsToWireCommand(portals));
                    }, enable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        protected void AppendInsertBlockItemMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, bool insertAbove, string itemName = "")
        {
            if (selection.Count > 1 || selection[0] is not BlockNodeModel blockNodeModel)
            {
                // If there are more than 1 selected element or the selected element is not a BlockNodeModel, don't append this menu item.
                return;
            }

            var contextView = blockNodeModel.ContextNodeModel?.GetView<ContextNodeView>(this);
            if (contextView == null)
                return;

            var menuItemName = string.IsNullOrEmpty(itemName) ? "Insert Block " + (insertAbove ? "Above" : "Below") : itemName;
            var index = insertAbove ? blockNodeModel.GetIndex() : blockNodeModel.GetIndex() + 1;

            evt.menu.AppendAction(L10n.Tr(menuItemName),
                action =>
                {
                    Vector2 mousePosition = action?.eventInfo?.mousePosition ?? evt.mousePosition;
                    contextView.ShowItemLibrary(mousePosition, index);
                });
        }

        void AppendFontSizeAndThemeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var themesAreDifferent = false;
            var fontSizesAreDifferent = false;

            var firstTheme = "";
            var firstFontSize = "";

            var stickyNotes = new List<StickyNoteModel>(selection.Count);

            for (var i = 0; i < selection.Count; i++)
            {
                // If the element is not a sticky note, don't append this menu item.
                if (selection[i] is not StickyNoteModel stickyNoteModel)
                    return;

                stickyNotes.Add(stickyNoteModel);

                // Check if the themes and font sizes are the same for all selected sticky notes.
                if (i == 0)
                {
                    firstTheme = stickyNoteModel.Theme;
                    firstFontSize = stickyNoteModel.TextSize;
                    continue;
                }

                if (!themesAreDifferent && stickyNoteModel.Theme != firstTheme)
                    themesAreDifferent = true;
                if (!fontSizesAreDifferent && stickyNoteModel.TextSize != firstFontSize)
                    fontSizesAreDifferent = true;
            }

            DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
            {
                if (themesAreDifferent)
                {
                    // Values are not all the same.
                    return DropdownMenuAction.Status.Normal;
                }

                return stickyNotes[0].Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            }

            DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
            {
                if (fontSizesAreDifferent)
                {
                    // Values are not all the same.
                    return DropdownMenuAction.Status.Normal;
                }

                return stickyNotes[0].TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            }

            foreach (var value in StickyNote.GetSizes())
            {
                evt.menu.AppendAction(L10n.Tr("Font Size/" + value),
                    menuAction => Dispatch(new UpdateStickyNoteTextSizeCommand(menuAction.userData as string, stickyNotes)),
                    GetSizeStatus, value);
            }

            foreach (var value in StickyNote.GetThemes())
            {
                evt.menu.AppendAction(L10n.Tr("Color/" + value),
                    menuAction => Dispatch(new UpdateStickyNoteThemeCommand(menuAction.userData as string, stickyNotes)),
                    GetThemeStatus, value);
            }
        }

        void AppendCreatePlacematMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var selectedVisibleGraphElements = new List<GraphElement>();
            var allBlocks = true;
            var allPortals = true;
            foreach (var elementModel in selection)
            {
                // If a graph element is not on the graph (eg: block nodes) or the graph element is a placemat, don't append this menu item.
                if (elementModel is PlacematModel)
                    return;

                if (elementModel is WireModel)
                    continue;

                if (elementModel.NeedsContainer() || elementModel is BlockNodeModel)
                    continue;

                if (elementModel is not WirePortalModel)
                    allPortals = false;

                allBlocks = false;

                var view = elementModel.GetView<GraphElement>(this);
                if (view is { visible: true })
                    selectedVisibleGraphElements.Add(view);
            }

            // If selected elements are all block or portal nodes, don't append this menu item.
            if (selection.Count > 0 && (allBlocks || allPortals))
                return;

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutCreatePlacematEvent>(GraphTool, selectedVisibleGraphElements.Count > 0 ? L10n.Tr("Create Placemat from Selection") : ShortcutCreatePlacematEvent.id, menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

                CreatePlacematFromGraphElements(selectedVisibleGraphElements, graphPosition);
            });
        }

        void AppendColorMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var colorables = new List<GraphElementModel>();
            var showAlpha = true;
            foreach (var elementModel in selection)
            {
                if (elementModel is WireModel)
                    continue;

                // If the graph element cannot have its color changed, don't append this menu item.
                if (!elementModel.IsColorable() || elementModel is not IHasElementColor hasElementColor)
                    return;

                if (showAlpha && !hasElementColor.UseColorAlpha)
                    showAlpha = false;

                colorables.Add(elementModel);
            }

            evt.menu.AppendAction(L10n.Tr("Color/Change..."), _ =>
            {
                void ChangeNodesColor(Color pickedColor)
                {
                    Dispatch(new ChangeElementColorCommand(pickedColor, colorables));
                }

                var defaultColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.7843f, 0.7843f, 0.7843f);
                if (selection.Count == 1)
                {
                    var firstColorable = (IHasElementColor)colorables[0];
                    if (firstColorable.ElementColor.HasUserColor)
                    {
                        defaultColor = firstColorable.ElementColor.Color;
                    }
                    else if (!firstColorable.DefaultColor.Equals(default))
                    {
                        defaultColor = firstColorable.DefaultColor;
                    }
                }

                EditorBridge.ShowColorPicker(ChangeNodesColor, defaultColor, showAlpha);
            });

            evt.menu.AppendAction(L10n.Tr("Color/Reset"), _ =>
            {
                Dispatch(new ResetElementColorCommand(colorables));
            });
        }

        void AppendFrameSelectionMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(CommandMenuItemNames.FrameSelected, _ =>
            {
                this.DispatchFrameSelectionCommand();
            });
        }

        void AppendAlignAndDistributeElementsMenuItems(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            // If there are less than 2 elements selected, don't append these menu items.
            if (selection.Count < 2)
                return;

            foreach (var elementModel in selection)
            {
                // Ignore wires, as they are not movable.
                if (elementModel is WireModel)
                    continue;

                // If the graph element is not on the graph (eg: block nodes) or the graph element cannot be moved, don't append these menu items.
                if (elementModel.NeedsContainer() || !elementModel.IsMovable() || elementModel is not IMovable)
                    return;
            }

            evt.menu.AppendAction(L10n.Tr("Align Elements/Top"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Top));
            evt.menu.AppendAction(L10n.Tr("Align Elements/Bottom"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Bottom));
            evt.menu.AppendAction(L10n.Tr("Align Elements/Left"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Left));
            evt.menu.AppendAction(L10n.Tr("Align Elements/Right"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Right));
            evt.menu.AppendAction(L10n.Tr("Align Elements/Horizontal Center"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference
                    .HorizontalCenter));
            evt.menu.AppendAction(L10n.Tr("Align Elements/Vertical Center"),
                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference
                    .VerticalCenter));
            evt.menu.AppendAction(L10n.Tr("Distribute Elements/Horizontal"),
                _ => m_AutoDistributingHelper.SendDistributeCommand(PortOrientation.Horizontal));
            evt.menu.AppendAction(L10n.Tr("Distribute Elements/Vertical"),
                _ => m_AutoDistributingHelper.SendDistributeCommand(PortOrientation.Vertical));
        }

        protected void AppendAddNodeItemMenuItem(ContextualMenuPopulateEvent evt, string itemName = "")
        {
            var menuItemName = string.IsNullOrEmpty(itemName) ? GraphModel.IsStateMachineGraph ? "Create State" : "Add Node" : itemName;

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutShowItemLibraryEvent>(GraphTool,  L10n.Tr(menuItemName), menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                ShowItemLibrary(mousePosition);
            });
        }

        void AppendCreateStickyNoteMenuItem(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendMenuItemFromShortcut<ShortcutCreateStickyNoteEvent>(GraphTool, menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

                Dispatch(new CreateStickyNoteCommand(graphPosition));
            });
        }

        void AppendCreateEmptyLocalSubgraph(ContextualMenuPopulateEvent evt)
        {
            if (!GraphModel.AllowSubgraphCreation)
                return;

            var menuItemName = "Create Empty {0}Local Subgraph";

            if (GraphModel.SubgraphTemplates == null || GraphModel.SubgraphTemplates.Count == 0)
            {
                // If there are no subgraph templates, append a menu item to create a local subgraph.
                evt.menu.AppendAction(L10n.Tr(string.Format(menuItemName, "")),
                    menuAction =>
                    {
                        Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                        Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);
                        Dispatch(new CreateLocalSubgraphFromSelectionCommand(new List<GraphElementModel>(), this, graphPosition));
                    });
            }
            else
            {
                // If there are subgraph templates, append a menu item for each template.
                foreach (var graphTemplate in GraphModel.SubgraphTemplates)
                {
                    // If there is only one template possible, we don't need to display its name.
                    evt.menu.AppendAction(L10n.Tr(string.Format(menuItemName, GraphModel.SubgraphTemplates.Count < 2 ? "" : graphTemplate.GraphTypeName + " ")),
                        menuAction =>
                        {
                            Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                            Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);
                            Dispatch(new CreateLocalSubgraphFromSelectionCommand(new List<GraphElementModel>(), this, graphPosition, template: graphTemplate));
                        });
                }
            }
        }

        void AppendToggleCollapseMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var nodes = new List<AbstractNodeModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not AbstractNodeModel node)
                    continue;

                if (!elementModel.IsCollapsible() || elementModel is not ICollapsible)
                    return;

                nodes.Add(node);
            }

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutToggleNodeCollapseEvent>(GraphTool, L10n.Tr("Toggle Collapse"), _ =>
            {
                var firstValue = ((ICollapsible)nodes[0]).Collapsed;
                Dispatch(new CollapseNodeCommand(!firstValue, nodes));
            });
        }

        void AppendDeleteAndReconnectMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            bool canDeleteAndReconnect = m_DeleteAndReconnectAction.ValidateAction(GraphModel, GetSelection());

            if (m_DeleteAndReconnectAction.HasNodes) //We display the "Delete and reconnect" menu item if there are nodes in the selection. If no node can be reconnected the command will be disabled.
            {
                evt.menu.AppendMenuItemFromShortcut<ShortcutDeleteAndReconnectEvent>( GraphTool, _ =>
                {
                    m_DeleteAndReconnectAction.ExecuteAction(this);
                }, !canDeleteAndReconnect ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }
        }

        void AppendDisableNodeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var nodes = new List<AbstractNodeModel>();
            var willDisable = false;
            var isContext = true;
            var isBlock = true;

            foreach (var elementModel in selection)
            {
                if (elementModel is not AbstractNodeModel node)
                    continue;

                // If the graph element cannot be disabled, don't append this menu item.
                if (!elementModel.IsDisableable())
                    return;

                // If all nodes are disabled, we set the item name to "Enable nodes". If at least 1 is enabled, we set the item name to "Disable nodes".
                if (node.State == ModelState.Enabled)
                    willDisable = true;

                if (node is not ContextNodeModel)
                    isContext = false;

                if (node is not BlockNodeModel)
                    isBlock = false;

                nodes.Add(node);
            }

            var isPlural = nodes.Count > 1;
            var nodeWord = (isContext ? "Context" : isBlock ? "Block" : "Node") + (isPlural ? "s" : "");
            evt.menu.AppendAction(L10n.Tr(willDisable ? "Disable " + nodeWord : "Enable " + nodeWord), _ =>
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                Dispatch(new ChangeNodeStateCommand(willDisable ? ModelState.Disabled : ModelState.Enabled, nodes.Where(t => t.IsDisableable()).ToList()));
#pragma warning restore RS0030
            });
        }

        void AppendDisconnectAllWiresMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var connectedNodes = new List<AbstractNodeModel>();

            foreach (var elementModel in selection)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (elementModel is not AbstractNodeModel node || node.GetConnectedWires().Count() == 0)
#pragma warning restore RS0030
                    continue;

                connectedNodes.Add(node);
            }

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutDisconnectWiresEvent>(GraphTool, L10n.Tr("Disconnect All Wires"), _ =>
            {
                Dispatch(new DisconnectWiresCommand(connectedNodes));
            }, connectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
        }

        void AppendSetAsDefaultStateMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.IsStateMachineGraph || selection.Count > 1 || selection[0] is not StateModel stateModel)
                return;

            evt.menu.AppendAction(L10n.Tr("Set as Default State"), _ =>
            {
                Dispatch(new SetEntryPointCommand(GraphModel, stateModel, !stateModel.IsEntryPoint));
            }, stateModel.IsEntryPoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        void AppendCreateTransitionMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, TransitionSupportKind transitionSupportKind)
        {
            if (!GraphModel.IsStateMachineGraph)
                return;

            var itemName = transitionSupportKind switch
            {
                TransitionSupportKind.OnEnter => "Create OnEnter Transition",
                TransitionSupportKind.Self => "Create Self Transition",
                _ => "Create Local Transition"
            };

            evt.menu.AppendAction(L10n.Tr(itemName), _ =>
            {
                foreach (var elementModel in selection)
                {
                    if (elementModel is not StateModel stateModel)
                        continue;

                    Dispatch(new CreateSingleStateTransitionSupportCommand(GraphModel, stateModel, transitionSupportKind));
                }
            });
        }

        void AppendCreateLocalSubgraphFromSelectionMenuItem(ContextualMenuPopulateEvent evt)
        {
            var data = SubgraphFromSelectionAction.CollectData(this, null, null);

            if (!data.IsValid)
                return;

            var menuItemName = data.shouldConvertToPlacemat ? "Convert to {0}Local Subgraph" : "Create {0}Local Subgraph from Selection";

            if (GraphModel.SubgraphTemplates == null || GraphModel.SubgraphTemplates.Count == 0)
            {
                // If there are no subgraph templates, append a menu item to convert to a local subgraph.
                evt.menu.AppendMenuItemFromShortcutWithName<ShortcutCreateLocalSubgraphFromSelectionEvent>(GraphTool, L10n.Tr(string.Format(menuItemName, "")), menuAction =>
                {
                    Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                    Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);
                    Dispatch(new CreateLocalSubgraphFromSelectionCommand(data.elementsToInclude, this,
                        graphPosition, null, null,
                        data.defaultName,
                        data.elementsToDelete));
                });
            }
            else
            {
                // If there are subgraph templates, append a menu item for each template.
                foreach (var graphTemplate in GraphModel.SubgraphTemplates)
                {
                    evt.menu.AppendMenuItemFromShortcutWithName<ShortcutCreateLocalSubgraphFromSelectionEvent>(GraphTool, L10n.Tr(string.Format(menuItemName, GraphModel.SubgraphTemplates.Count < 2 ? "" : graphTemplate.GraphTypeName + " ")), menuAction =>
                    {
                        Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                        Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);
                        Dispatch(new CreateLocalSubgraphFromSelectionCommand(data.elementsToInclude, this,
                            graphPosition, null, graphTemplate, data.defaultName, data.elementsToDelete));
                    });
                }
            }
        }

        void AppendExtractContentsToPlacematMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.AllowSubgraphCreation)
                return;

            var subgraphNodes = new List<SubgraphNodeModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not SubgraphNodeModel subgraphNodeModel)
                    continue;

                if (!subgraphNodeModel.CanBeExpanded)
                    return;

                subgraphNodes.Add(subgraphNodeModel);
            }

            foreach (var subgraphNodeModel in subgraphNodes)
            {
                evt.menu.AppendMenuItemFromShortcut<ShortcutExtractContentsToPlacematEvent>(GraphTool, menuAction =>
                {
                    Dispatch(new ExpandSubgraphCommand(GraphModel, subgraphNodeModel, ContentViewContainer.WorldToLocal(menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition)));
                }, subgraphNodeModel.GetSubgraphModel() is null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void AppendOpenSubgraphMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.AllowSubgraphCreation || selection.Count != 1 || selection[0] is not SubgraphNodeModel subgraphNodeModel)
                return;

            var subgraphNode = subgraphNodeModel.GetView<SubgraphNodeView>(this);
            if (subgraphNode == null)
                return;

            var menuItemName = "Open " + (subgraphNodeModel.IsReferencingLocalSubgraph ? "Local" : "Asset") + " Subgraph";
            evt.menu.AppendAction(L10n.Tr(menuItemName), _ =>
            {
                subgraphNode.OpenSubgraph();
            });
        }

        void AppendConvertToAssetSubgraphMenuItem(ContextualMenuPopulateEvent evt)
        {
            if (!GraphModel.AllowSubgraphCreation)
                return;
            if (GraphModel.SubgraphTemplates == null || GraphModel.SubgraphTemplates.Count == 0)
            {
                AppendConvertToAssetSubgraphAction(null);
            }
            else
            {
                foreach (var graphTemplate in GraphModel.SubgraphTemplates)
                {
                    AppendConvertToAssetSubgraphAction(graphTemplate);
                }
            }

            return;

            void AppendConvertToAssetSubgraphAction(GraphTemplate template)
            {
                var data = SubgraphFromSelectionAction.CollectData(this, null, template?.GraphModelType ?? GraphModel.GetType());
                if (!data.IsValid)
                    return;

                var menuItemName = "Convert to {0}Asset Subgraph" + (data.localSubgraphNodes.Count > 1 ? "s" : "") + "...";
                evt.menu.AppendAction(L10n.Tr(string.Format(menuItemName, GraphModel.SubgraphTemplates?.Count < 2 || template == null ? "" : template.GraphTypeName + " ")),
                    _ => Dispatch(new ConvertLocalToAssetSubgraphCommand(data.localSubgraphNodes, template)));
            }
        }

        void AppendUnpackToLocalSubgraphMenuItem(ContextualMenuPopulateEvent evt)
        {
            if (!GraphModel.AllowSubgraphCreation)
                return;

            if (GraphModel.SubgraphTemplates == null || GraphModel.SubgraphTemplates.Count == 0)
            {
                AppendUnpackToLocalSubgraphAction(null);
            }
            else
            {
                foreach (var graphTemplate in GraphModel.SubgraphTemplates)
                {
                    AppendUnpackToLocalSubgraphAction(graphTemplate);
                }
            }

            return;

            void AppendUnpackToLocalSubgraphAction(GraphTemplate template)
            {
                var data = SubgraphFromSelectionAction.CollectData(this, null, template?.GraphModelType ?? GraphModel.GetType());
                if (!data.IsValid)
                    return;

                var menuItemName = "Unpack to {0}Local Subgraph" + (data.assetSubgraphNodes.Count > 1 ? "s" : "");
                evt.menu.AppendAction(L10n.Tr(string.Format(menuItemName, GraphModel.SubgraphTemplates?.Count < 2 || template == null ? "" : template.GraphTypeName + " ")),
                    _ => Dispatch(new ConvertAssetToLocalSubgraphCommand(data.assetSubgraphNodes, template)));
            }
        }

        void AppendFindAssetInProjectMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.AllowSubgraphCreation || selection.Count != 1 || selection[0] is not SubgraphNodeModel subgraphNodeModel)
                return;

            var associateFileObject = subgraphNodeModel.GetSubgraphModel()?.GraphObject;
            // Only add the menu item if the asset can be found in the Project Window and is a main asset.
            if (associateFileObject is null ||
                !AssetDatabase.IsMainAsset(associateFileObject) ||
                !AssetDatabaseHelper.TryGetGUIDAndLocalFileIdentifier(associateFileObject, out _, out _))
                return;

            evt.menu.AppendAction(L10n.Tr("Find Asset in Project"), _ =>
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = associateFileObject;
                EditorGUIUtility.PingObject(associateFileObject);
            });
        }

        void AppendConvertToConstantMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var variableNodes = new List<VariableNodeModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not AbstractNodeModel)
                    continue;

                // If a graph element is not a variable node or a selected variable node cannot be converted to a constant, don't append this menu item.
                if (elementModel is not VariableNodeModel variableNode || !variableNode.CanConvertToConstant() || GraphModel.GetConstantType(variableNode.DataType) == null)
                    return;

                variableNodes.Add(variableNode);
            }

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutConvertConstantAndVariableEvent>(GraphTool, L10n.Tr("Convert to Constant"),
                _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(null, variableNodes)));
        }

        void AppendConvertToVariableMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var constantNodes = new List<ConstantNodeModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not AbstractNodeModel)
                    continue;

                // If a graph element is not a constant node, don't append this menu item.
                if (elementModel is not ConstantNodeModel constantNode)
                    return;

                constantNodes.Add(constantNode);
            }

            evt.menu.AppendMenuItemFromShortcutWithName<ShortcutConvertConstantAndVariableEvent>(GraphTool, L10n.Tr("Convert to Variable"),
                _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constantNodes, null)));
        }

        void AppendItemizeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var singleOutputPortNodes = new List<ISingleOutputPortNodeModel>();
            var canBeItemized = true;
            foreach (var elementModel in selection)
            {
                if (elementModel is not NodeModel nodeModel)
                    continue;

                // If a graph element is not a constant node, don't append this menu item.
                if (elementModel is not ISingleOutputPortNodeModel singleOutputPortNode)
                    return;

                if (canBeItemized && !nodeModel.CanBeItemized())
                    canBeItemized = false;

                singleOutputPortNodes.Add(singleOutputPortNode);
            }

            evt.menu.AppendAction(L10n.Tr("Itemize"),
                _ => Dispatch(new ItemizeNodeCommand(singleOutputPortNodes)),
                canBeItemized ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendReorderWireMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (selection.Count != 1 || selection[0] is not WireModel wireModel || selection[0] is TransitionSupportModel || wireModel.FromPort is not { HasReorderableWires: true })
                return;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var siblingWires = wireModel.FromPort.GetConnectedWires().ToList();
#pragma warning restore RS0030
            var siblingWiresCount = siblingWires.Count;

            var index = siblingWires.IndexOf(wireModel);
            evt.menu.AppendAction("Reorder Wire/Move First",
                _ => ReorderWire(ReorderType.MoveFirst),
                siblingWiresCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Wire/Move Up",
                _ => ReorderWire(ReorderType.MoveUp),
                siblingWiresCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Wire/Move Down",
                _ => ReorderWire(ReorderType.MoveDown),
                siblingWiresCount > 1 && index < siblingWiresCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Wire/Move Last",
                _ => ReorderWire(ReorderType.MoveLast),
                siblingWiresCount > 1 && index < siblingWiresCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            void ReorderWire(ReorderType reorderType)
            {
                Dispatch(new ReorderWireCommand(wireModel, reorderType));
            }
        }

        void AppendConvertToPortalsMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (!GraphModel.AllowPortalCreation)
                return;

            var wires = new List<WireModel>();
            var hasNullOrMissingPort = false;
            foreach (var elementModel in selection)
            {
                // If the graph element is not a wire, don't append this menu item.
                if (elementModel is not WireModel wireModel || wireModel is TransitionSupportModel)
                    return;

                // If the wire has a missing port, do not allow creation of portals.
                hasNullOrMissingPort = wireModel.ToPort is null || wireModel.FromPort is null ||
                    wireModel.ToPort.PortType == PortType.MissingPort ||
                    wireModel.FromPort.PortType == PortType.MissingPort;

                wires.Add(wireModel);
            }

            if (wires.Count > 0)
            {
                var wireData = Wire.GetPortalsWireData(wires, this);
                evt.menu.AppendMenuItemFromShortcutWithName<ShortcutConvertWireToPortalEvent>(GraphTool, L10n.Tr("Convert to Portals"), _ =>
                {
                    Dispatch(new ConvertWiresToPortalsCommand(wireData, this));
                }, hasNullOrMissingPort ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }
        }

        void AppendInsertNodeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            if (selection.Count != 1 || selection[0] is not WireModel wireModel || selection[0] is TransitionSupportModel)
                return;

            var hasNullOrMissingPort = wireModel.ToPort is null || wireModel.FromPort is null ||
                wireModel.ToPort.PortType == PortType.MissingPort ||
                wireModel.FromPort.PortType == PortType.MissingPort;

            evt.menu.AppendAction(L10n.Tr("Insert Node"), menuAction =>
            {
                var mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                ShowItemLibrary(mousePosition);
            }, _ => hasNullOrMissingPort ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
        }

        void AppendSelectAllPlacematContentsMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var placematModels = new List<PlacematModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not PlacematModel placematModel)
                    continue;

                placematModels.Add(placematModel);
            }

            // If there are not placemats or more than 1 placemat in the selection, don't append this menu item.
            if (placematModels.Count is 0 or > 1)
                return;

            var placemat = placematModels[0].GetView<Placemat>(this);
            if (placemat == null)
                return;

            evt.menu.AppendAction(L10n.Tr("Select All Placemat Contents"),
                _ =>
                {
                    placemat.SelectAllInside();
                }, placemat.HasElementsOverThisPlacemat() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendReorderPlacematMenuItems(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var placematModels = new List<PlacematModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not PlacematModel placematModel)
                    continue;

                placematModels.Add(placematModel);
            }

            // If there are not placemats in the selection, don't append this menu item.
            if (placematModels.Count == 0)
                return;

            var placematModelsInGraph = GraphModel.PlacematModels;

            // JOCE TODO: Check that *ALL* placemats are at the top or bottom. We should be able to do something otherwise.
            var placematIsTop = placematModelsInGraph[^ 1] == placematModels[0];
            var placematIsBottom = placematModelsInGraph[0] == placematModels[0];
            var canBeReordered = placematModelsInGraph.Count > 1;

            evt.menu.AppendSeparator();
            evt.menu.AppendAction(L10n.Tr("Bring to Front"),
                _ => Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToFront, placematModels)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(L10n.Tr("Bring Forward"),
                _ => Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Forward, placematModels)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(L10n.Tr("Send Backward"),
                _ => Dispatch(new ChangePlacematOrderCommand(ZOrderMove.Backward, placematModels)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(L10n.Tr("Send to Back"),
                _ => Dispatch(new ChangePlacematOrderCommand(ZOrderMove.ToBack, placematModels)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendSmartResizeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var placemats = new List<Placemat>();
            var hasElementsOverPlacemat = false;
            foreach (var elementModel in selection)
            {
                if (elementModel is not PlacematModel placematModel)
                    continue;

                var placemat = placematModel.GetView<Placemat>(this);
                if (placemat == null)
                    return;

                placemats.Add(placemat);
                if (placemat.HasElementsOverThisPlacemat())
                    hasElementsOverPlacemat = true;
            }

            // If there are not placemats or more than 1 placemat in the selection, don't append this menu item.
            if (placemats.Count is 0 or > 1)
                return;

            evt.menu.AppendAction(L10n.Tr("Smart Resize"),
                _ =>
                {
                    foreach (var placemat in placemats)
                    {
                        placemat.SmartResize();
                    }
                },
                // If at least one selected placemat has elements over it, enable the menu item.
                hasElementsOverPlacemat ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void AppendDeleteAndSelectContentsMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection)
        {
            var placemats = new List<PlacematModel>();
            foreach (var elementModel in selection)
            {
                if (elementModel is not PlacematModel placematModel)
                    continue;

                placemats.Add(placematModel);
            }

            evt.menu.AppendAction(L10n.Tr("Delete and Select Contents"), _ =>
            {
                Dispatch(new DeleteAndSelectPlacematContentCommand(placemats, this));
            });
        }

        /// <summary>
        /// Creates a placemat including the given <paramref name="graphElements"/>.
        /// </summary>
        /// <param name="graphElements">The graph elements to be included in the placemat.</param>
        /// <param name="graphPosition">The position of the placemat, if no valid graph elements are provided.</param>
        protected void CreatePlacematFromGraphElements(List<GraphElement> graphElements, Vector2 graphPosition)
        {
            Rect bounds = new Rect();

            using (ListPool<GraphElement>.Get(out List<GraphElement> elementsInPlacemat))
            {
                foreach (var element in graphElements)
                    elementsInPlacemat.Add(element.GraphElementModel.NeedsContainer() ? element.GetFirstAncestorOfType<GraphElement>() : element);

                Dispatch(Placemat.ComputeElementBounds(ContentViewContainer, ref bounds, elementsInPlacemat)
                    ? new CreatePlacematCommand(bounds)
                    : new CreatePlacematCommand(graphPosition));
            }
        }

        public ItemLibraryHelper GetItemLibraryHelper()
        {
            if (m_ItemLibraryHelper == null || m_ItemLibraryHelper.GraphModel != GraphModel)
                m_ItemLibraryHelper = CreateItemLibraryHelper();

            return m_ItemLibraryHelper;
        }

        protected virtual ItemLibraryHelper CreateItemLibraryHelper()
        {
            return (Window as GraphViewEditorWindow)?.CreateItemLibraryHelper(GraphModel);
        }

        /// <summary>
        /// Shows the Item Library to add graph elements at the mouse position.
        /// </summary>
        /// <param name="position">The position where to show the Item Library</param>
        public virtual void ShowItemLibrary(Vector2 position)
        {
            var graphPosition = ContentViewContainer.WorldToLocal(position);
            var element = panel.Pick(position)?.GetFirstOfType<ModelView>();

            VisualElement current = element;
            while (current != null && current != this)
            {
                if (current is IShowItemLibraryUI dssUI)
                    if (dssUI.ShowItemLibrary(position))
                        return;

                current = current.parent;
            }

            ItemLibraryService.ShowGraphNodes(this, position, item =>
            {
                switch (item)
                {
                    case GraphNodeModelLibraryItem nodeItem:
                        Dispatch(CreateNodeCommand.OnGraph(nodeItem, graphPosition));
                        break;
                    case VariableLibraryItem variableItem:
                        var blackboardSection = GraphModel.GetSectionModel(GraphModel.DefaultSectionName);
                        var index = blackboardSection.Items.Count;
                        Dispatch(CreateNodeCommand.OnGraph(variableItem, graphPosition, blackboardSection, index));
                        break;
                }
            });
        }

        /// <inheritdoc />
        protected override void RegisterModelObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            // PF TODO use a single observer on graph loaded to update all states.

            if (m_GraphViewGraphLoadedObserver == null)
            {
                m_GraphViewGraphLoadedObserver = new GraphLoadedObserver<GraphViewStateComponent.StateUpdater>(GraphTool.ToolState, GraphViewModel.GraphViewState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphViewGraphLoadedObserver);
            }

            if (m_GraphModelGraphLoadedAssetObserver == null)
            {
                m_GraphModelGraphLoadedAssetObserver = new GraphLoadedObserver<GraphModelStateComponent.StateUpdater>(GraphTool.ToolState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphModelGraphLoadedAssetObserver);
            }

            if (m_SelectionGraphLoadedObserver == null)
            {
                m_SelectionGraphLoadedObserver = new GraphLoadedObserver<SelectionStateComponent.StateUpdater>(GraphTool.ToolState, GraphViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_SelectionGraphLoadedObserver);
            }

            if (m_ProcessingGraphLoadedObserver == null)
            {
                m_ProcessingGraphLoadedObserver = new GraphLoadedObserver<GraphProcessingStateComponent.StateUpdater>(GraphTool.ToolState, GraphViewModel.GraphProcessingState);
                GraphTool.ObserverManager.RegisterObserver(m_ProcessingGraphLoadedObserver);
            }

            if (m_ProcessingErrorsGraphLoadedObserver == null)
            {
                m_ProcessingErrorsGraphLoadedObserver = new GraphLoadedObserver<GraphProcessingErrorsStateComponent.StateUpdater>(GraphTool.ToolState, GraphViewModel.ProcessingErrorsState);
                GraphTool.ObserverManager.RegisterObserver(m_ProcessingErrorsGraphLoadedObserver);
            }

            if (m_ExternalVariablesUpdater == null)
            {
                m_ExternalVariablesUpdater = new ExternalVariablesUpdater(GraphTool.ExternalAssetsState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_ExternalVariablesUpdater);
            }

            if (m_SubgraphNodeUpdater == null)
            {
                m_SubgraphNodeUpdater = new SubgraphNodeUpdater(GraphTool.ExternalAssetsState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_SubgraphNodeUpdater);
            }
        }

        /// <inheritdoc />
        protected override void RegisterViewObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this,
                    new IStateComponent[]
                    {
                        GraphViewModel.GraphViewState,
                        GraphViewModel.GraphModelState,
                        GraphViewModel.SelectionState,
                        GraphViewModel.AutoPlacementState,
                        GraphViewModel.ProcessingErrorsState,
                        GraphTool.HighlighterState
                    }, new IStateComponent[]
                    {
                        GraphViewModel.SpacePartitioningState,
                        GraphViewModel.GraphViewCullingState,
                        GraphViewModel.GraphModelState,
                        GraphTool.UndoState
                    });
                GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
            }

            if (m_WireOrderObserver == null)
            {
                m_WireOrderObserver = new WireOrderObserver(GraphViewModel.SelectionState, GraphViewModel.GraphModelState);
                GraphTool?.ObserverManager?.RegisterObserver(m_WireOrderObserver);
            }

            if (m_DeclarationHighlighter == null)
            {
                m_DeclarationHighlighter = new DeclarationHighlighter(GraphTool.ToolState, GraphViewModel.SelectionState, GraphTool.HighlighterState,
                    model => model is IHasDeclarationModel hasDeclarationModel ? hasDeclarationModel.DeclarationModel : null);
                GraphTool?.ObserverManager?.RegisterObserver(m_DeclarationHighlighter);
            }

            if (m_AutoPlacementObserver == null)
            {
                m_AutoPlacementObserver = new AutoPlacementObserver(this, GraphViewModel.AutoPlacementState, GraphViewModel.GraphModelState);
                GraphTool?.ObserverManager?.RegisterObserver(m_AutoPlacementObserver);
            }

            if (m_AutomaticGraphProcessingObserver == null && DisplayMode == GraphViewDisplayMode.Interactive)
            {
                m_AutomaticGraphProcessingObserver = new AutomaticGraphProcessingObserver(
                    GraphViewModel.GraphModelState, ProcessOnIdleAgent.StateComponent,
                    GraphViewModel.GraphProcessingState, GraphTool.Preferences);
                GraphTool?.ObserverManager?.RegisterObserver(m_AutomaticGraphProcessingObserver);
            }

            if (m_GraphProcessingErrorObserver == null)
            {
                m_GraphProcessingErrorObserver = new GraphProcessingErrorObserver(GraphViewModel.GraphModelState, GraphViewModel.GraphProcessingState, GraphViewModel.ProcessingErrorsState);
                GraphTool?.ObserverManager?.RegisterObserver(m_GraphProcessingErrorObserver);
            }

            if (m_SpacePartitioningObserver == null)
            {
                m_SpacePartitioningObserver = new SpacePartitioningObserver(this, GraphViewModel.SpacePartitioningState, GraphViewModel.GraphViewCullingState);
                GraphTool?.ObserverManager?.RegisterObserver(m_SpacePartitioningObserver);
            }

            if (m_CullingObserver == null)
            {
                m_CullingObserver = new GraphViewCullingObserver(this, GraphViewModel.GraphViewCullingState);
                GraphTool?.ObserverManager?.RegisterObserver(m_CullingObserver);
            }
        }

        public override bool TryPauseViewObservers()
        {
            if (base.TryPauseViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                if (m_WireOrderObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_WireOrderObserver);
                if (m_DeclarationHighlighter != null) GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                if (m_AutoPlacementObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_AutoPlacementObserver);
                if (m_AutomaticGraphProcessingObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_AutomaticGraphProcessingObserver);
                if (m_GraphProcessingErrorObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_GraphProcessingErrorObserver);
                if (m_SpacePartitioningObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_SpacePartitioningObserver);
                if (m_CullingObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_CullingObserver);

                return true;
            }
            return false;
        }

        public override bool TryResumeViewObservers()
        {
            if (base.TryResumeViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
                if (m_WireOrderObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_WireOrderObserver);
                if (m_DeclarationHighlighter != null) GraphTool?.ObserverManager?.RegisterObserver(m_DeclarationHighlighter);
                if (m_AutoPlacementObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_AutoPlacementObserver);
                if (m_AutomaticGraphProcessingObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_AutomaticGraphProcessingObserver);
                if (m_GraphProcessingErrorObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_GraphProcessingErrorObserver);
                if (m_SpacePartitioningObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_SpacePartitioningObserver);
                if (m_CullingObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_CullingObserver);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        protected override void UnregisterModelObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            if (m_GraphViewGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphViewGraphLoadedObserver);
                m_GraphViewGraphLoadedObserver = null;
            }

            if (m_GraphModelGraphLoadedAssetObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphModelGraphLoadedAssetObserver);
                m_GraphModelGraphLoadedAssetObserver = null;
            }

            if (m_SelectionGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionGraphLoadedObserver);
                m_SelectionGraphLoadedObserver = null;
            }

            if (m_ProcessingGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_ProcessingGraphLoadedObserver);
                m_ProcessingGraphLoadedObserver = null;
            }

            if (m_ProcessingErrorsGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_ProcessingErrorsGraphLoadedObserver);
                m_ProcessingErrorsGraphLoadedObserver = null;
            }

            if (m_ExternalVariablesUpdater != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_ExternalVariablesUpdater);
                m_ExternalVariablesUpdater = null;
            }

            if (m_SubgraphNodeUpdater != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SubgraphNodeUpdater);
                m_SubgraphNodeUpdater = null;
            }
        }

        /// <inheritdoc />
        protected override void UnregisterViewObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            if (m_UpdateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;
            }

            if (m_WireOrderObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_WireOrderObserver);
                m_WireOrderObserver = null;
            }

            if (m_AutomaticGraphProcessingObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_AutomaticGraphProcessingObserver);
                m_AutomaticGraphProcessingObserver = null;
            }

            if (m_GraphProcessingErrorObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphProcessingErrorObserver);
                m_GraphProcessingErrorObserver = null;
            }

            if (m_DeclarationHighlighter != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                m_DeclarationHighlighter = null;
            }

            if (m_AutoPlacementObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_AutoPlacementObserver);
                m_AutoPlacementObserver = null;
            }

            if (m_SpacePartitioningObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SpacePartitioningObserver);
                m_SpacePartitioningObserver = null;
            }

            if (m_CullingObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_CullingObserver);
                m_CullingObserver = null;
            }
        }

        internal void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNamesBridge.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        internal void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNamesBridge.FrameSelected)
            {
                this.DispatchFrameSelectionCommand();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
            {
                evt.imguiEvent?.Use();
            }
        }

        public virtual void AddElement(GraphElement graphElement)
        {
            if (graphElement is Marker)
            {
                m_MarkersParent.Add(graphElement);
            }
            else if (graphElement is Placemat placemat)
            {
                PlacematContainer.Add(placemat);
            }
            else
            {
                int newLayer = graphElement.Layer;
                if (!m_ContainerLayers.ContainsKey(newLayer))
                {
                    AddLayer(newLayer);
                }

                GetLayer(newLayer).Add(graphElement);
            }

            switch (DisplayMode)
            {
                case GraphViewDisplayMode.NonInteractive:
                    IgnorePickingRecursive(graphElement);
                    break;

                case GraphViewDisplayMode.Interactive:
                    if (graphElement is NodeView || graphElement is Wire)
                    {
                        graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);
                    }
                    break;
            }

            if (graphElement.Model is WirePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }

            void IgnorePickingRecursive(VisualElement e)
            {
                e.pickingMode = PickingMode.Ignore;
                if (e is IMGUIContainer imguiContainer)
                {
                    // We disable IMGUIContainer elements because we cannot prevent them from being interactable.
                    imguiContainer.SetEnabled(false);
                }

                foreach (var child in e.hierarchy.Children())
                {
                    IgnorePickingRecursive(child);
                }
            }

            graphElement.SetLevelOfDetail(Zoom, ZoomMode, GraphViewZoomMode.Unknown);

            if (HasCullingOnZoom && CullingState == GraphViewCullingState.Enabled && IsZoomCullingSize(m_ZoomMode))
            {
                using var updater = GraphViewModel.GraphViewCullingState.UpdateScope;
                updater.MarkGraphElementAsCulled(graphElement, GraphViewCullingSource.Zoom);
            }
        }

        public virtual void RemoveElement(GraphElement graphElement)
        {
            if (graphElement == null)
                return;

            var graphElementModel = graphElement.Model;
            switch (graphElementModel)
            {
                case WireModel e:
                    RemovePositionDependency(e);
                    break;
                case WirePortalModel portalModel:
                    RemovePortalDependency(portalModel);
                    break;
            }

            if (graphElement is NodeView || graphElement is Wire)
                graphElement.UnregisterCallback<MouseOverEvent>(OnMouseOver);

            if (GraphViewModel != null)
            {
                // This must be done before removing from the hierarchy so the parent it still valid.
                using var updater = GraphViewModel.SpacePartitioningState.UpdateScope;
                updater.MarkGraphElementForRemoval(graphElement);
            }

            graphElement.RemoveFromHierarchy();
            graphElement.RemoveFromRootView();
        }

        static readonly List<ChildView> k_CalculateRectToFitAllAllUIs = new();

        public Rect CalculateRectToFitAll()
        {
            Rect rectToFit = ContentViewContainer.layout;
            bool reachedFirstChild = false;

            (GraphModel?.GetGraphElementModels()).GetAllViews(this, null, k_CalculateRectToFitAllAllUIs);
            foreach (var ge in k_CalculateRectToFitAllAllUIs)
            {
                if (ge is null || ge.layout == Rect.zero || ge is ModelView { Model: WireModel or PortModel })
                    continue;

                if (!reachedFirstChild)
                {
                    rectToFit = ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout);
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils.Encompass(rectToFit, ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout));
                }
            }

            k_CalculateRectToFitAllAllUIs.Clear();

            return rectToFit;
        }

        public void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling, float maxZoomLevel = -1.0f)
        {
            // bring slightly smaller screen rect into GUI space
            var screenRect = new Rect
            {
                xMin = border,
                xMax = clientRect.width - border,
                yMin = border,
                yMax = clientRect.height - border
            };

            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

            // measure zoom level necessary to fit the canvas rect into the screen rect
            float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

            // clamp
            zoomLevel = Mathf.Clamp(zoomLevel, m_MinScale, Math.Min(m_MaxScale, maxZoomLevel > 0 ? maxZoomLevel : m_MaxScaleOnFrame));

            var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var wire = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = wire
            };

            var parentScale = new Vector3(trs.GetColumn(0).magnitude,
                trs.GetColumn(1).magnitude,
                trs.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }

        protected void AddPositionDependency(WireModel model)
        {
            PositionDependenciesManager.AddPositionDependency(model);
        }

        protected void RemovePositionDependency(WireModel wireModel)
        {
            PositionDependenciesManager.Remove(wireModel.FromNodeGuid, wireModel.ToNodeGuid);
            PositionDependenciesManager.LogDependencies();
        }

        protected void AddPortalDependency(WirePortalModel model)
        {
            PositionDependenciesManager.AddPortalDependency(model);
        }

        protected void RemovePortalDependency(WirePortalModel model)
        {
            PositionDependenciesManager.RemovePortalDependency(model);
            PositionDependenciesManager.LogDependencies();
        }

        /// <summary>
        /// Returns the center point of nodes.
        /// </summary>
        /// <param name="elements">List of nodes.</param>
        /// <returns></returns>
        protected Vector2 GetCenterOfSelectionOfNodes(List<GraphElementModel> elements)
        {
            Rect selectionRect = new Rect();

            elements.ForEach((element) =>
            {
                // Only consider nodes for the spawn position calculation.
                if (element is AbstractNodeModel)
                {
                    var elementView = element.GetView<GraphElement>(this);

                    selectionRect = (selectionRect == default)
                    ? elementView.worldBound
                    : RectUtils.Encompass(selectionRect, elementView.worldBound);
                }
            });

            return selectionRect.center;
        }

        /// <summary>
        /// Converts 2D world coordinates to the graph view content area coordinates.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        protected Vector2 WorldToGraphViewArea(Vector2 worldPosition)
        {
            // Convert the world position to the graph view content area.
            return this.ChangeCoordinatesTo(ContentViewContainer, this.WorldToLocal(worldPosition));
        }

        /// <summary>
        /// Returns the mouse position relative to the graph view content area.
        /// If the mouse is outside of the EditorWindow, the center of the content area is returned.
        /// </summary>
        /// <param name="worldPosition">Position as received in events.</param>
        /// <returns></returns>
        protected Vector2 GetLocalMousePositionOrCenter(Vector2 worldPosition)
        {
            if (EditorWindow.mouseOverWindow != Window)
                return WorldToGraphViewArea(new Vector2(contentRect.width / 2f, contentRect.height / 2f));
            return WorldToGraphViewArea(worldPosition);
        }

        /// <summary>
        /// Returns a Rect positioned at the mouse position relative to the graph view content area.
        /// If the mouse is outside of the EditorWindow, the center of the content area is returned.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="elementSize"></param>
        /// <returns></returns>
        protected Rect GetLocalMousePositionOrCenter(Vector2 worldPosition, Vector2 elementSize)
        {
            return new Rect(GetLocalMousePositionOrCenter(worldPosition), elementSize);
        }

        public virtual void StopSelectionDragger()
        {
            // cancellation is handled in the MoveMove callback
            m_SelectionDraggerWasActive = false;
        }

        /// <summary>
        /// Callback for the ShortcutFrameAllEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameAllEvent(ShortcutFrameAllEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFrameAllCommand();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutFrameOriginEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameOriginEvent(ShortcutFrameOriginEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                Vector3 frameTranslation = Vector3.zero;
                Vector3 frameScaling = Vector3.one;
                Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the <see cref="ShortcutShowItemLibraryEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutShowItemLibraryEvent(ShortcutShowItemLibraryEvent e)
        {
            ShowItemLibrary((EditorWindow.mouseOverWindow != Window)? new Vector2(contentRect.width / 2f, contentRect.height / 2f) : e.MousePosition);
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutConvertConstantToVariableEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutConvertVariableAndConstantEvent(ShortcutConvertConstantAndVariableEvent e)
        {
            using var dispose = ListPool<VariableNodeModel>.Get(out var variableModels);
            using var dispose2 = ListPool<ConstantNodeModel>.Get(out var constantModels);

            foreach (var selected in GetSelection())
            {
                switch(selected)
                {
                    case ConstantNodeModel constantModel:
                        constantModels.Add(constantModel);
                        break;
                    case VariableNodeModel variableModel when variableModel.CanConvertToConstant():
                        variableModels.Add(variableModel);
                        break;
                }
            }

            if (constantModels.Count > 0 || variableModels.Count > 0)
            {
                Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constantModels, variableModels));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Cabllback for the ShortcutConvertWireToPortalEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutConvertWireToPortalEvent(ShortcutConvertWireToPortalEvent e)
        {
            if (!GraphModel.AllowPortalCreation)
                return;

            var selection = GetSelection();
            if (selection.Count == 0)
                return;
            using var dispose = ListPool<WireModel>.Get(out var wires);

            foreach (var selectedItem in selection)
            {
                if (selectedItem is not WireModel wire)
                    continue;
                wires.Add(wire);
            }

            if (wires.Count == 0)
                return;

            var wireData = Wire.GetPortalsWireData(wires, this);
            Dispatch(new ConvertWiresToPortalsCommand(wireData, this));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutCreateLocalSubgraphFromSelectionEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutCreateLocalSubgraphFromSelectionEvent(ShortcutCreateLocalSubgraphFromSelectionEvent e)
        {
            var data = SubgraphFromSelectionAction.CollectData(this, null, null);

            if (!data.IsValid)
                return;

            Dispatch(new CreateLocalSubgraphFromSelectionCommand(
                data.elementsToInclude,
                this,
                ContentViewContainer.WorldToLocal(GetCenterOfSelectionOfNodes(data.elementsToInclude)),
                null,
                null,
                data.defaultName,
                data.elementsToDelete));

            e.StopPropagation();
        }

        /* TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
        /// <summary>
        /// Callback for the ShortcutAlignNodesEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutAlignNodesEvent(ShortcutAlignNodesEvent e)
        {
            Dispatch(new AlignNodesCommand(this, false, GetSelection()));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutAlignNodeHierarchyEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutAlignNodeHierarchyEvent(ShortcutAlignNodeHierarchiesEvent e)
        {
            Dispatch(new AlignNodesCommand(this, true, GetSelection()));
            e.StopPropagation();
        }
        */

        /// <summary>
        /// Callback for the ShortcutCreateStickyNoteEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutCreateStickyNoteEvent(ShortcutCreateStickyNoteEvent e)
        {
            Dispatch(new CreateStickyNoteCommand(GetLocalMousePositionOrCenter(e.MousePosition, StickyNote.defaultSize)));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutCreateStickyNoteEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutCreatePlacematEvent(ShortcutCreatePlacematEvent e)
        {
            using var dispose = ListPool<GraphElement>.Get( out var selectedGraphElements);

            var allBlocks = true;
            var allPortals = true;

            foreach (var selection in GetSelection())
            {
                if (selection is not WireModel)
                {
                    if (selection is not BlockNodeModel)
                        allBlocks = false;

                    if (selection is not WirePortalModel)
                        allPortals = false;

                    var graphElement = selection.GetView<GraphElement>(this);
                    if (graphElement != null && graphElement.visible)
                        selectedGraphElements.Add(graphElement);
                }
            }

            if (selectedGraphElements.Count > 0 && (allBlocks || allPortals))
            {
                // If only blocks or portals are selected, we don't allow the shortcut.
                e.StopPropagation();
                return;
            }

            if (selectedGraphElements.Count != 1 || selectedGraphElements[0].Model is not PlacematModel)
            {
                CreatePlacematFromGraphElements(selectedGraphElements, GetLocalMousePositionOrCenter(e.MousePosition));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutDeleteAndReconnectEvent.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnShortcutDeleteAndReconnect(ShortcutDeleteAndReconnectEvent evt)
        {
            bool canDeleteAndReconnect = m_DeleteAndReconnectAction.ValidateAction(GraphModel, GetSelection()) && m_DeleteAndReconnectAction.HasNodes;

            if (canDeleteAndReconnect) //We display the "Delete and reconnect" menu item if there are nodes in the selection. If no node can be reconnected the command will be disabled.
            {
                m_DeleteAndReconnectAction.ExecuteAction(this);
                evt.StopPropagation();
            }
        }

        protected void OnShortcutExtractContentsToPlacematEvent(ShortcutExtractContentsToPlacematEvent e)
        {
            if (!GraphModel.AllowSubgraphCreation)
                return;

            var selection = GetSelection();
            if (selection.Count == 1 && selection[0] is SubgraphNodeModel { CanBeExpanded: true } subgraphNodeModel)
            {
                Dispatch(new ExpandSubgraphCommand(GraphModel, subgraphNodeModel, subgraphNodeModel.Position));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutDisconnectWiresEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutDisconnectWiresEvent(ShortcutDisconnectWiresEvent e)
        {
            var selection = GetSelection();
            if (selection.Count == 0)
                return;

            using var dispose = ListPool<AbstractNodeModel>.Get(out var nodes);
            foreach (var selectedItem in selection)
            {
                if (selectedItem is not AbstractNodeModel nodeModel)
                    continue;

                if (nodeModel is ContextNodeModel contextNodeModel)
                {
                    for (int i = 0; i < contextNodeModel.BlockCount; ++i)
                    {
                        var block = contextNodeModel.GetBlock(i);
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        if (block.GetConnectedWires().Count() > 0)
#pragma warning restore RS0030
                            nodes.Add(block);
                    }
                }

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (nodeModel.GetConnectedWires().Count() == 0)
#pragma warning restore RS0030
                    continue;

                nodes.Add(nodeModel);
            }

            if (nodes.Count == 0)
                return;

            Dispatch(new DisconnectWiresCommand(nodes));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutToggleNodeCollapseEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutToggleNodeCollapseEvent(ShortcutToggleNodeCollapseEvent e)
        {
            using var dispose = ListPool<AbstractNodeModel>.Get(out var selectedGraphElementsUncollapsed);
            using var dispose2 = ListPool<AbstractNodeModel>.Get(out var selectedGraphElementsCollapsed);

            // Gather all collapsible nodes in the selection and avoid duplicates in collections.
            // User can select blocks and the parent contextNode at the same time.
            void GatherCollapsibleNodes(NodeModel nodeModel)
            {
                if (!nodeModel.IsCollapsible())
                    return;

                if (nodeModel.Collapsed)
                {
                    if (!selectedGraphElementsCollapsed.Contains(nodeModel))
                        selectedGraphElementsCollapsed.Add(nodeModel);
                }
                else
                {
                    if (!selectedGraphElementsUncollapsed.Contains(nodeModel))
                        selectedGraphElementsUncollapsed.Add(nodeModel);
                }
            }

            foreach (var selection in GetSelection())
            {
                switch (selection)
                {
                    case ContextNodeModel contextNode:
                        for (int i = 0; i < contextNode.BlockCount; ++i)
                            GatherCollapsibleNodes(contextNode.GetBlock(i));
                        break;
                    case NodeModel nodeModel:
                        GatherCollapsibleNodes(nodeModel);
                        break;
                }
            }

            if (selectedGraphElementsUncollapsed.Count > 0)
            {
                Dispatch(new CollapseNodeCommand(true, selectedGraphElementsUncollapsed));
                e.StopPropagation();
            }
            else if (selectedGraphElementsCollapsed.Count > 0)
            {
                Dispatch(new CollapseNodeCommand(false, selectedGraphElementsCollapsed));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnRenameKeyDown(KeyDownEvent e)
        {
            if (ModelView.IsRenameKey(e))
            {
                if (e.target == this)
                {
                    // Forward event to the last selected element.
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var renamableSelection = GetSelection().Where(x => x.IsRenamable());
#pragma warning restore RS0030
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var lastSelectedItem = renamableSelection.LastOrDefault();
#pragma warning restore RS0030
                    var lastSelectedItemUI = lastSelectedItem?.GetView<GraphElement>(this);

                    lastSelectedItemUI?.OnRenameKeyDown(e);
                }
            }
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (Window is GraphViewEditorWindow gvWindow)
            {
                // Set the window min size from the graphView
                gvWindow.AdjustWindowMinSize(new Vector2(resolvedStyle.minWidth.value, resolvedStyle.minHeight.value));
            }
        }

        protected void OnMouseOver(MouseOverEvent evt)
        {
            evt.StopPropagation();
        }

        protected void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Capsule.Select() which does not stop propagation
            Selection.activeObject = GraphViewModel.GraphModelState?.GraphModel.GraphObject;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManager.CancelMove();
            }
            else if (!m_SelectionDraggerWasActive && SelectionDragger.IsActive) // started
            {
                m_SelectionDraggerWasActive = true;

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var elemModel = GetSelection().OfType<AbstractNodeModel>().FirstOrDefault();
#pragma warning restore RS0030
                var elem = elemModel?.GetView<GraphElement>(this);
                if (elem == null)
                    return;

                Vector2 elemPos = elemModel.Position;
                Vector2 startPos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                bool requireShiftToMoveDependencies = !(elemModel.GraphModel?.MoveNodeDependenciesByDefault).GetValueOrDefault();
                bool hasShift = evt.modifiers.HasFlag(EventModifiers.Shift);
                bool moveNodeDependencies = requireShiftToMoveDependencies == hasShift;

                if (moveNodeDependencies)
                    PositionDependenciesManager.StartNotifyMove(GetSelection(), startPos);

                // schedule execute because the mouse won't be moving when the graph view is panning
                schedule.Execute(() =>
                {
                    if (SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.layout.position);
                        PositionDependenciesManager.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }
        }

        protected virtual void OnDragEnter(DragEnterEvent evt)
        {
            var dragAndDropHandler = GetDragAndDropHandler();
            if (dragAndDropHandler != null)
            {
                m_CurrentDragAndDropHandler = dragAndDropHandler;
                m_CurrentDragAndDropHandler.OnDragEnter(evt);
            }
        }

        protected virtual void OnDragLeave(DragLeaveEvent evt)
        {
            m_CurrentDragAndDropHandler?.OnDragLeave(evt);
            m_CurrentDragAndDropHandler = null;
        }

        protected virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            if (m_CurrentDragAndDropHandler != null)
            {
                m_CurrentDragAndDropHandler?.OnDragUpdated(e);
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        protected virtual void OnDragPerform(DragPerformEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragPerform(e);
            m_CurrentDragAndDropHandler = null;
        }

        protected virtual void OnDragExited(DragExitedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragExited(e);
            m_CurrentDragAndDropHandler = null;
        }

        /// <summary>
        /// Updates the graph view to reflect the changes in the state components.
        /// </summary>
        public override void Update()
        {
            if (m_UpdateObserver == null || GraphViewModel == null)
                return;

            using (var graphViewObservation = m_UpdateObserver.ObserveState(GraphViewModel.GraphViewState))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    UpdateViewTransform(GraphViewModel.GraphViewState.Position, GraphViewModel.GraphViewState.Scale);
                }
            }

            UpdateType updateType;

            using (var graphModelObservation = m_UpdateObserver.ObserveState(GraphViewModel.GraphModelState))
            using (var selectionObservation = m_UpdateObserver.ObserveState(GraphViewModel.SelectionState))
            using (var highlighterObservation = m_UpdateObserver.ObserveState(GraphTool.HighlighterState))
            {
                updateType = DoUpdate(graphModelObservation, selectionObservation, highlighterObservation);
            }

            DoUpdateProcessingErrorMarkers(updateType);
        }

        /// <summary>
        /// Performs an update based on the provided observations.
        /// </summary>
        /// <param name="graphModelObservation">The <see cref="Observation"/> related to the graph model.</param>
        /// <param name="selectionObservation">The <see cref="Observation"/> related to the current selection.</param>
        /// <param name="highlighterObservation">The <see cref="Observation"/> related to the highlighter state.</param>
        /// <returns>The <see cref="UpdateType"/> performed.</returns>
        /// <remarks>
        /// 'DoUpdate' is called during <see cref="GraphView.Update"/> to process changes in the <see cref="GraphModel"/>, selection, and highlighting,
        /// ensuring that the <see cref="GraphView"/> accurately reflects the most recent updates.
        /// </remarks>
        protected virtual UpdateType DoUpdate(Observation graphModelObservation, Observation selectionObservation, Observation highlighterObservation)
        {
            var rebuildType = graphModelObservation.UpdateType;

            var selectionChangeSet = selectionObservation.UpdateType == UpdateType.Partial ? GraphViewModel.SelectionState.GetAggregatedChangeset(selectionObservation.LastObservedVersion) : null;
            var selectionAlreadyUpdatedModels = selectionChangeSet != null ? new HashSet<Hash128>() : null;

            if (rebuildType == UpdateType.Complete)
            {
                // Sad. We lose the focused element.
                Focus();

                BuildUITree();
            }
            else if (rebuildType == UpdateType.Partial || highlighterObservation.UpdateType != UpdateType.None)
            {
                PartialUpdate(graphModelObservation, highlighterObservation, selectionChangeSet, selectionAlreadyUpdatedModels);
            }

            if (selectionObservation.UpdateType == UpdateType.Complete && rebuildType != UpdateType.Complete)
                // if graphModelObservation.UpdateType == UpdateType.Complete, we don't need to update the selection again : it will be initialized at creation
            {
                if (GraphModel != null)
                {
                    foreach (var model in GraphModel.GetGraphElementModels())
                    {
                        model.AppendAllViews(this, null, k_UpdateAllUIs);
                        k_UpdateAllUIs.AddRange(model.GetModelDependencies());
                    }

                    foreach (var ui in k_UpdateAllUIs)
                    {
                        UpdateSelectionVisitor.Visitor.Update(ui);
                    }
                    k_UpdateAllUIs.Clear();
                }
            }
            else if( selectionObservation.UpdateType == UpdateType.Partial)
            {
                PartialSelectionUpdate(selectionChangeSet, selectionAlreadyUpdatedModels);
            }

            return rebuildType;
        }

        /// <summary>
        /// Perform a partial update of the graph view.
        /// </summary>
        /// <param name="graphModelObservation">The GraphModel observation.</param>
        /// <param name="highlighterObservation">The Highlighter observation.</param>
        /// <param name="selectionChangeset">The changeset for a partial selection update or null.</param>
        /// <param name="selectionAlreadyUpdatedModels">Upon return. The list of model on which updating selection is no longer required. Filled only if selectionChangeset != null</param>
        protected virtual void PartialUpdate(Observation graphModelObservation, Observation highlighterObservation, SimpleChangeset selectionChangeset, HashSet<Hash128> selectionAlreadyUpdatedModels)
        {
            if (GraphModel == null)
                return;

            var focusedElement = panel?.focusController?.focusedElement as VisualElement;
            while (focusedElement != null && !(focusedElement is ModelView))
            {
                focusedElement = focusedElement.parent;
            }

            var focusedModelView = focusedElement as ModelView;

            var modelChangeSet = GraphViewModel.GraphModelState.GetAggregatedChangeset(graphModelObservation.LastObservedVersion);

            if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                Debug.Log($"Partial GraphView Update {modelChangeSet?.NewModels.Count() ?? 0} new {modelChangeSet?.ChangedModels.Count() ?? 0} changed {modelChangeSet?.DeletedModels.Count() ?? 0} deleted");
#pragma warning restore RS0030
            }

            var changedModels = new Dictionary<Hash128, ChangeHintList>();
            var shouldUpdatePlacematContainer = false;
            var newPlacemats = new List<GraphElement>();
            if (modelChangeSet != null)
            {
                DeleteElementsFromChangeSet(modelChangeSet, focusedModelView);
                if( selectionChangeset != null)
                    selectionAlreadyUpdatedModels.UnionWith(modelChangeSet.DeletedModels);

                AddElementsFromChangeSet(modelChangeSet, newPlacemats);
                if( selectionChangeset != null)
                    selectionAlreadyUpdatedModels.UnionWith(modelChangeSet.NewModels);

                shouldUpdatePlacematContainer = newPlacemats.HasAny();

                //Update new and deleted node containers
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var modelGuid in modelChangeSet.NewModels.Concat(modelChangeSet.DeletedModels))
#pragma warning restore RS0030
                {
                    if (GraphModel.TryGetModelFromGuid(modelGuid, out var model) &&
                        model.Container is GraphElementModel container)
                    {
                        // Whatever change hint was there is superseded by Unspecified.
                        changedModels[container.Guid] = ChangeHintList.Unspecified;
                    }
                }
            }

            if (modelChangeSet != null)
            {
                foreach (var changedModelAndHint in modelChangeSet.ChangedModelsAndHints)
                {
                    AddChangedModel(changedModelAndHint.Key, changedModelAndHint.Value);
                }
            }

            if (highlighterObservation.UpdateType == UpdateType.Complete)
            {
                foreach (var nodeModel in GraphModel.NodeModels)
                {
                    if (nodeModel is IHasDeclarationModel)
                    {
                        AddChangedModel(nodeModel.Guid, ChangeHintList.Unspecified);
                    }
                }
            }
            else if (highlighterObservation.UpdateType == UpdateType.Partial)
            {
                var declRefs = new List<AbstractNodeModel>();
                var changeset = GraphTool.HighlighterState.GetAggregatedChangeset(highlighterObservation.LastObservedVersion);
                foreach (var changedModelGuid in changeset.ChangedModels)
                {
                    if (GraphModel.TryGetModelFromGuid(changedModelGuid, out DeclarationModel declarationModel))
                    {
                        declRefs.Clear();
                        GraphModel.FindReferencesInGraph(declarationModel, declRefs);
                        foreach (var model in declRefs)
                        {
                            AddChangedModel(model.Guid, ChangeHintList.Unspecified);
                        }
                    }
                }
            }

            UpdateChangedModels(changedModels, selectionChangeset, selectionAlreadyUpdatedModels, shouldUpdatePlacematContainer, newPlacemats);

            // PF FIXME: node state (enable/disabled, used/unused) should be part of the State.
            if (GraphTool.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                PositionDependenciesManager.UpdateNodeState();

            HideAutoPlacedElements();

            if (modelChangeSet?.RenamedModel != null)
            {
                Window.Focus();
                var modelUis = new List<ChildView>();
                modelChangeSet.RenamedModel.AppendAllViews(this, _ => true, modelUis);
                foreach (var ui in modelUis)
                {
                    if (ui is ModelView modelView)
                        modelView.ActivateRename();
                }
            }

            return;

            void AddChangedModel(Hash128 modelGuid, ChangeHintList changeHints)
            {
                if (!changedModels.TryAdd(modelGuid, changeHints))
                {
                    changedModels[modelGuid] = ChangeHintList.AddRange(changedModels[modelGuid], changeHints);
                }
            }
        }

        /// <summary>
        /// Partial update the childview selection state.
        /// </summary>
        /// <param name="selectionChangeSet">The changeset containing change models.</param>
        /// <param name="selectionAlreadyUpdatedModels">A hashset containing models which selection has already been updated.</param>
        protected virtual void PartialSelectionUpdate(SimpleChangeset selectionChangeSet, HashSet<Hash128> selectionAlreadyUpdatedModels)
        {
            if (GraphModel == null)
                return;

            foreach (var placemat in GraphModel.PlacematModels)
            {
                if (selectionChangeSet.ChangedModels.Contains(placemat.Guid) )
                {
                    if (!GraphViewModel.SelectionState.IsSelected(placemat.Guid))
                    {
                        var placematUI = placemat.GetView<Placemat>(this);

                        if (placematUI != null)
                            placematUI.MoveOnly = false;
                    }
                }
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var changedModelGuid in selectionChangeSet.ChangedModels.Except(selectionAlreadyUpdatedModels))
#pragma warning restore RS0030
            {
                changedModelGuid.AppendAllViews(this, null, k_UpdateAllUIs);
                k_UpdateAllUIs.AddRange(changedModelGuid.GetModelDependencies());
            }

            foreach (var ui in k_UpdateAllUIs)
            {
                UpdateSelectionVisitor.Visitor.Update(ui);
            }

            k_UpdateAllUIs.Clear();

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var lastSelectedNode = GetSelection().OfType<AbstractNodeModel>().LastOrDefault();
#pragma warning restore RS0030
            if (lastSelectedNode != null && lastSelectedNode.IsAscendable())
            {
                var nodeUI = lastSelectedNode.GetView<GraphElement>(this);
                nodeUI?.BringToFront();
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var lastSelectedWire = GetSelection().OfType<WireModel>().LastOrDefault();
#pragma warning restore RS0030
            if (lastSelectedWire != null && lastSelectedWire.IsAscendable())
            {
                var wireUI = lastSelectedWire.GetView<GraphElement>(this);
                wireUI?.BringToFront();
            }
        }

        void HideAutoPlacedElements()
        {
            if (m_UpdateObserver == null || GraphViewModel == null)
                return;

            // Models that need repositioning are hidden until they are moved by the observer next frame.
            using (var autoPlacementObservation = m_UpdateObserver.ObserveState(GraphViewModel.AutoPlacementState))
            {
                var autoPlacementChangeset = GraphViewModel.AutoPlacementState.GetAggregatedChangeset(autoPlacementObservation.LastObservedVersion);
                if (autoPlacementChangeset != null && autoPlacementChangeset.ModelsToHideDuringAutoPlacement.Count != 0)
                {
                    foreach (var elementToHide in autoPlacementChangeset.ModelsToHideDuringAutoPlacement)
                    {
                        var childView = elementToHide.GetView(this);
                        if (childView != null)
                            childView.visible = false;
                    }
                }
            }
        }

        protected virtual void DeleteElementsFromChangeSet(GraphModelStateComponent.Changeset modelChangeSet, ModelView focusedModelView)
        {
            foreach (var guid in modelChangeSet.DeletedModels)
            {
                if (guid == focusedModelView?.Model.Guid)
                {
                    // Focused element will be deleted. Switch the focus to the graph view to avoid dangling focus.
                    Focus();
                }

                guid.AppendAllViews(this, null, k_UpdateAllUIs);
                foreach (var ui in k_UpdateAllUIs)
                {
                    if( ui is GraphElement ge)
                        RemoveElement(ge);
                    else if( ui is ModelView mv) // Port are not graph elements and will be removed by their node PortContainer, however,
                                                 // we need to clear their dependencies as they could be otherwise updated before the node containing
                                                 // (and potentially deleting) them is removed as the order in the changed models is not controlled.
                        mv.ClearDependencies();
                }

                k_UpdateAllUIs.Clear();

                // ToList is needed to bake the dependencies.
                // PF FIXME return this list and process the update along the others.
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var ui in guid.GetModelDependencies().ToList())
#pragma warning restore RS0030
                {
                    ui.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                }
            }
        }

        protected virtual void AddElementsFromChangeSet(GraphModelStateComponent.Changeset modelChangeSet, List<GraphElement> newPlacemats)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var newModels = modelChangeSet.NewModels.Select(GraphModel.GetModel).Where(m => m != null).ToList();
#pragma warning restore RS0030

            foreach (var model in newModels)
            {
                if (model is WireModel or PortModel or PlacematModel or DeclarationModel or NodePreviewModel or GroupModelBase)
                    continue;

                if (model.Container != GraphModel)
                    continue;

                var ui = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (ui != null)
                    AddElement(ui);
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in newModels.OfType<WireModel>())
#pragma warning restore RS0030
            {
                CreateWireUI(model);
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in newModels.OfType<PlacematModel>())
#pragma warning restore RS0030
            {
                var placemat = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (placemat != null)
                {
                    newPlacemats.Add(placemat);
                    AddElement(placemat);

                    if (placemat is Placemat placematUI)
                    {
                        placematUI.MoveOnly = true;
                    }
                }
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in newModels.OfType<NodePreviewModel>())
#pragma warning restore RS0030
            {
                var preview = ModelViewFactory.CreateUI<NodePreview>(this, model);
                if (preview != null)
                {
                    AddElement(preview);
                }
            }
        }

        /// <summary>
        /// Calls UpdateFromModel on views of given models.
        /// </summary>
        /// <param name="changedModels">The models which views will be updated.</param>
        /// <param name="selectionChangeset">The selection changeset. Can be null.</param>
        /// <param name="selectionAlreadyUpdatedModels">On return, filled with models on which UpdateElementSelection has been called.</param>
        /// <param name="shouldUpdatePlacematContainer">If true, placemats will be reorderd.</param>
        /// <param name="placemats">A list of placemats in the <see cref="GraphView"/>.</param>
        protected virtual void UpdateChangedModels(Dictionary<Hash128, ChangeHintList> changedModels, SimpleChangeset selectionChangeset, HashSet<Hash128> selectionAlreadyUpdatedModels, bool shouldUpdatePlacematContainer, List<GraphElement> placemats)
        {
            var sharedVisitor = new UpdateFromModelVisitor(null);
            foreach (var(guid, changeHints) in changedModels)
            {
                guid.AppendAllViews(this, null, k_UpdateAllUIs);
                bool inSelection = selectionChangeset?.ChangedModels.Contains(guid) ?? false;
                if (inSelection)
                {
                    selectionAlreadyUpdatedModels.Add(guid);
                }

                UpdateFromModelVisitor viewUpdater;
                if (ReferenceEquals(changeHints, ChangeHintList.Unspecified))
                    viewUpdater = UpdateFromModelVisitor.genericUpdateFromModelVisitor;
                else
                {
                    viewUpdater = sharedVisitor;
                    sharedVisitor.Reset(changeHints);
                }

                foreach (var ui in k_UpdateAllUIs)
                {
                    ui.UpdateView(viewUpdater);
                    if (inSelection)
                    {
                        UpdateSelectionVisitor.Visitor.Update(ui);
                    }

                    if (ui.parent == PlacematContainer)
                        shouldUpdatePlacematContainer = true;
                }

                k_UpdateAllUIs.Clear();

                // ToList is needed to bake the dependencies.
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var ui in guid.GetModelDependencies().ToList())
#pragma warning restore RS0030
                {
                    if( changedModels.TryGetValue(ui.Model.Guid, out var doneChangeList) && doneChangeList.IsSupersetOf(changeHints))
                        continue;
                    ui.UpdateView(viewUpdater);
                }
            }

            if (shouldUpdatePlacematContainer)
                PlacematContainer?.UpdateElementsOrder();

            foreach (var placemat in placemats)
            {
                placemat.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
            }
        }

        protected virtual void DoUpdateProcessingErrorMarkers(UpdateType rebuildType)
        {
            // Update processing error markers.
            using (var processingStateObservation = m_UpdateObserver.ObserveState(GraphViewModel.ProcessingErrorsState))
            {
                if (processingStateObservation.UpdateType != UpdateType.None || rebuildType == UpdateType.Partial)
                {
                    var windowId = (Window as GraphViewEditorWindow)?.WindowID.ToString();

                    // Make sure console is empty before adding entries.
                    ConsoleWindowHelper.RemoveLogEntries(windowId);

                    if (GraphModel is null)
                        return;

                    // Remove current markers on the graph.
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var markersToRemove = m_MarkersParent.Children().OfType<Marker>().Where(b => b.Model is MultipleGraphProcessingErrorsModel).ToList();
#pragma warning restore RS0030
                    foreach (var marker in markersToRemove)
                    {
                        RemoveElement(marker);

                        // ToList is needed to bake the dependencies.
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        foreach (var ui in marker.GraphElementModel.GetModelDependencies().ToList())
#pragma warning restore RS0030
                        {
                            ui.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
                        }
                    }

                    var currentGraphAsset = GraphModel.GraphObject;
                    var currentGraphAssetPath = currentGraphAsset.FilePath;
                    var subGraphPath = new StringBuilder();

                    for (var i = 0; i < GraphViewModel.ProcessingErrorsState.Errors.Count; i++)
                    {
                        var multipleErrorModel = GraphViewModel.ProcessingErrorsState.Errors[i];

                        for (var j = 0; j < multipleErrorModel.Errors.Count; j++)
                        {
                            var error = multipleErrorModel.Errors[j];
                            string sourceAssetPath;
                            Hash128 graphModelGUID;
                            var assetGuid = default(GUID);
                            subGraphPath.Clear();
                            GraphModel graphModelWithError = null;
                            if (error.SourceGraphReference != default)
                            {
                                assetGuid = error.SourceGraphReference.AssetGuid;
                                graphModelGUID = error.SourceGraphReference.GraphModelGuid;
                                sourceAssetPath = error.SourceGraphReference.FilePath;

                                graphModelWithError = GraphModel.ResolveGraphModelFromReference(error.SourceGraphReference);


                            }
                            else
                            {
                                // If the error has no source graph, use the current graph.
                                currentGraphAssetPath = currentGraphAsset.FilePath;
                                graphModelGUID = GraphModel.Guid;
                                sourceAssetPath = currentGraphAssetPath;
                                graphModelWithError = GraphModel;
                            }

                            if (graphModelWithError is {IsLocalSubgraph : true})
                            {
                                using var dispose = ListPool<string>.Get(out var subGraphPathList);

                                var currentGraphModel = graphModelWithError;
                                while (currentGraphModel.ParentGraph != null)
                                {
                                    subGraphPathList.Add(currentGraphModel.Name);

                                    currentGraphModel = currentGraphModel.ParentGraph;
                                }

                                subGraphPath.Append('(');
                                for (int k = subGraphPathList.Count - 1; k >= 0; k--)
                                {
                                    subGraphPath.Append(subGraphPathList[k]);
                                    if (k != 0)
                                    {
                                        subGraphPath.Append('/');
                                    }
                                }
                                subGraphPath.Append(')');
                            }

                            ConsoleWindowHelper.LogSticky(
                                $"{currentGraphAssetPath}{subGraphPath}: {error.ErrorMessage}",
                                $"{currentGraphAssetPath}@{error.ParentModelGuid}@{assetGuid}@{graphModelGUID}@{sourceAssetPath}",
                                error.ErrorType,
                                LogOption.None,
                                currentGraphAsset.GetEntityId(),
                                windowId);
                        }

                        // Add marker to the graph.
                        if (multipleErrorModel.GetParentModel(GraphModel) == null)
                            continue;

                        var marker = ModelViewFactory.CreateUI<Marker>(this, multipleErrorModel);
                        if (marker != null)
                            AddElement(marker);
                    }
                }
            }
        }

        public override void BuildUITree()
        {
            if (GraphTool?.Preferences != null)
            {
                if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
                {
                    Debug.Log($"Complete GraphView Update");
                }
            }

            ClearGraph();

            var graphModel = GraphViewModel?.GraphModelState.GraphModel;
            if (graphModel == null)
                return;

            foreach (var placeholderModel in graphModel.Placeholders)
            {
                GraphElement placeholder = null;

                switch (placeholderModel)
                {
                    case DeclarationModel:
                        continue;
                    case WirePlaceholder wirePlaceholder:
                        CreateWireUI(wirePlaceholder);
                        continue;
                    case NodePlaceholder nodePlaceholder:
                        placeholder = ModelViewFactory.CreateUI<GraphElement>(this, nodePlaceholder);
                        break;
                    case BlockNodePlaceholder blockNodePlaceholder:
                        placeholder = ModelViewFactory.CreateUI<GraphElement>(this, blockNodePlaceholder);
                        break;
                    case ContextNodePlaceholder contextNodePlaceholder:
                        placeholder = ModelViewFactory.CreateUI<GraphElement>(this, contextNodePlaceholder);
                        break;
                }

                if (placeholder != null)
                    AddElement(placeholder);
            }

            ContentViewContainer.Add(m_MarkersParent);

            m_MarkersParent.Clear();

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = ModelViewFactory.CreateUI<GraphElement>(this, nodeModel);
                if (node != null)
                {
                    AddElement(node);

                    if (nodeModel.NodePreviewModel != null)
                    {
                        var nodePreview = ModelViewFactory.CreateUI<NodePreview>(this, nodeModel.NodePreviewModel);
                        if (nodePreview != null)
                        {
                            AddElement(nodePreview);
                        }
                    }
                }
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var stickyNote = ModelViewFactory.CreateUI<GraphElement>(this, stickyNoteModel);
                if (stickyNote != null)
                    AddElement(stickyNote);
            }

            int index = 0;
            foreach (var wire in graphModel.WireModels)
            {
                if (!CreateWireUI(wire))
                {
                    Debug.LogWarning($"Wire {index} cannot be restored: {wire}");
                }
                index++;
            }

            var placemats = new List<GraphElement>();
            foreach (var placematModel in GraphViewModel.GraphModelState.GraphModel.PlacematModels)
            {
                var placemat = ModelViewFactory.CreateUI<GraphElement>(this, placematModel);
                if (placemat != null)
                {
                    placemats.Add(placemat);
                    AddElement(placemat);
                }
            }

            // We need to do this after all graph elements are created.
            foreach (var placemat in placemats)
            {
                placemat.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
            }

            HideAutoPlacedElements();

            UpdateViewTransform(GraphViewModel.GraphViewState.Position, GraphViewModel.GraphViewState.Scale);
        }

        internal bool CreateWireUI(WireModel wire)
        {
            if (wire == null)
                return false;

            if (wire.ToPort != null && wire.FromPort != null)
            {
                AddWireUI(wire);
                return true;
            }

            var(inputResult, outputResult) = wire.AddMissingPorts(out var inputNode, out var outputNode);

            if (inputResult == PortMigrationResult.MissingPortAdded && inputNode != null)
            {
                var inputNodeUi = inputNode.GetView(this);
                inputNodeUi?.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
            }

            if (outputResult == PortMigrationResult.MissingPortAdded && outputNode != null)
            {
                var outputNodeUi = outputNode.GetView(this);
                outputNodeUi?.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);
            }

            if (inputResult != PortMigrationResult.MissingPortFailure &&
                outputResult != PortMigrationResult.MissingPortFailure)
            {
                AddWireUI(wire);
                return true;
            }

            return false;
        }

        void AddWireUI(WireModel wireModel)
        {
            var wire = ModelViewFactory.CreateUI<GraphElement>(this, wireModel);
            if (wire == null)
                return;

            AddElement(wire);
            AddPositionDependency(wireModel);
        }

        bool CheckIntegrity(out string message)
        {
            message = "";

            if (GraphModel == null)
                return true;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidNodeCount = GraphModel.NodeModels.Count(n => n == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidWireCount = GraphModel.WireModels.Count(n => n == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidStickyCount = GraphModel.StickyNoteModels.Count(n => n == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidVariableCount = GraphModel.VariableDeclarations.Count(v => v == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidPlacematCount = GraphModel.PlacematModels.Count(p => p == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidPortalCount = GraphModel.PortalDeclarations.Count(p => p == null);
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var invalidSectionCount = GraphModel.SectionModels.Count(s => s == null);
#pragma warning restore RS0030

            var countMessage = new StringBuilder();
            countMessage.Append(invalidNodeCount == 0 ? string.Empty : $"{invalidNodeCount} invalid node(s) found.\n");
            countMessage.Append(invalidWireCount == 0 ? string.Empty : $"{invalidWireCount} invalid wire(s) found.\n");
            countMessage.Append(invalidStickyCount == 0 ? string.Empty : $"{invalidStickyCount} invalid sticky note(s) found.\n");
            countMessage.Append(invalidVariableCount == 0 ? string.Empty : $"{invalidVariableCount} invalid variable declaration(s) found.\n");
            countMessage.Append(invalidPlacematCount == 0 ? string.Empty : $"{invalidPlacematCount} invalid placemat(s) found.\n");
            countMessage.Append(invalidPortalCount == 0 ? string.Empty : $"{invalidPortalCount} invalid portal(s) found.\n");
            countMessage.Append(invalidSectionCount == 0 ? string.Empty : $"{invalidSectionCount} invalid section(s) found.\n");

            if (countMessage.ToString() != string.Empty)
                message = countMessage.ToString();
            else
                return false;

            return true;
        }

        /// <summary>
        /// Populates the save menu.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        public virtual void BuildSaveMenu(GenericMenu menu)
        {
            if (Window is not GraphViewEditorWindow graphViewEditorWindow)
            {
                Debug.LogError("Cannot save. Window is not a GraphViewEditorWindow.");
                return;
            }

            var saveAllGUI = new GUIContent("Save All");
            if (GraphTool?.ToolState?.CurrentGraph != null
                && GraphTool.ToolState.SubgraphStack.Count > 0)
            {
                menu.AddItem(saveAllGUI, false, () =>
                    {
                        graphViewEditorWindow.SaveAll();
                    });
            }
            else
                menu.AddDisabledItem(saveAllGUI);

            menu.AddItem(new GUIContent(ShortcutEventBase<ShortcutSaveAsEvent>.GetMenuItemName(GraphTool)), false, () =>
                {
                    graphViewEditorWindow.SaveAs();
                });
        }

        /// <summary>
        /// Action when the save button is Pressed. Default is to call Save() on the window.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown when the <see cref="RootView.Window"/> is not a <see cref="GraphViewEditorWindow"/>.</exception>
        public virtual void ClickSave()
        {
            if (Window is not GraphViewEditorWindow graphViewEditorWindow)
            {
                Debug.LogError("Cannot save. Window is not a GraphViewEditorWindow.");
                return;
            }
            graphViewEditorWindow.Save();
        }

        /// <summary>
        /// Populates the option menu.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        public virtual void BuildOptionMenu(GenericMenu menu)
        {
            var prefs = GraphTool?.Preferences;
            if (Unsupported.IsDeveloperMode())
            {
                if (CheckIntegrity(out var countMessage))
                    menu.AddItem(new GUIContent("Clean Up Graph"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog(
                                "Invalid graph",
                                $"Invalid elements found:\n{countMessage}\n" +
                                $"Click the Clean button to remove all the invalid elements from the graph.",
                                "Clean",
                                "Cancel"))
                        {
                            GraphModel.Repair();
                            using (var updater = GraphViewModel.GraphModelState.UpdateScope)
                            {
                                updater.ForceCompleteUpdate();
                            }
                        }
                    });

                if (prefs != null)
                {
                    menu.AddItem(new GUIContent("Leave Item Library open on focus lost"),
                        prefs.GetBool(BoolPref.ItemLibraryStaysOpenOnBlur),
                        () =>
                        {
                            prefs.ToggleBool(BoolPref.ItemLibraryStaysOpenOnBlur);
                        });
                }

                menu.AddSeparator("");

                bool graphLoaded = GraphTool?.ToolState.GraphModel != null;

                // ReSharper disable once RedundantCast : needed in 2020.3.
                menu.AddItem(new GUIContent("Reload Graph"), false, !graphLoaded
                    ? (GenericMenu.MenuFunction)null
                    : () =>
                    {
                        if (GraphTool?.ToolState.GraphModel != null)
                        {
                            var openedGraph = GraphTool.ToolState.GraphModel;
                            var label = GraphTool.ToolState.CurrentGraphLabel;
                            openedGraph.GraphObject.UnloadObject();
                            GraphTool?.Dispatch(new LoadGraphCommand(openedGraph, title: label));
                        }
                    });

                // ReSharper disable once RedundantCast : needed in 2020.3.
                menu.AddItem(new GUIContent("Rebuild UI"), false, !graphLoaded
                    ? (GenericMenu.MenuFunction)null
                    : () =>
                    {
                        using (var updater = GraphViewModel.GraphModelState.UpdateScope)
                        {
                            updater.ForceCompleteUpdate();
                        }
                    });

                menu.AddItem(new GUIContent("Reset Item Library Window Size"), false, () => GraphTool.Preferences.ResetItemLibrarySizes());

                menu.AddSeparator("");
            }

            if (prefs != null)
            {
                menu.AddItem(new GUIContent("Evaluate Graph Only When Idle"),
                    prefs.GetBool(BoolPref.OnlyProcessWhenIdle), () =>
                    {
                        prefs.ToggleBool(BoolPref.OnlyProcessWhenIdle);
                    });
            }
        }

        static readonly List<ChildView> k_OnFocusGraphElementList = new List<ChildView>();

        /// <inheritdoc />
        protected override void OnFocus(FocusInEvent e)
        {
            base.OnFocus(e);
            RefreshBorders();
        }

        /// <inheritdoc />
        protected override void OnLostFocus(FocusOutEvent e)
        {
            m_OnFocusCalled = false;

            // If the element that gains the focus is part of the graph inspector, we keep the focused uss class in the graph view.
            if (e.relatedTarget is not ModelInspectorView && e.relatedTarget is VisualElement v && v.GetFirstAncestorOfType<ModelInspectorView>() == null)
            {
                schedule.Execute(() =>
                {
                    if (!m_OnFocusCalled)
                    {
                        UpdateBordersOnFocus(false);
                    }
                }).ExecuteLater(0);
            }
        }

        internal void UpdateBordersOnFocus(bool hasFocus)
        {
            EnableInClassList(focusedViewUssClassName, hasFocus);
            RefreshBorders();
        }

        void RefreshBorders()
        {
            if (GraphModel == null)
                return;

            k_OnFocusGraphElementList.Clear();

            GraphModel.GetGraphElementModels()
                .GetAllViews(this, e => e is GraphElement ge && ge.Border != null, k_OnFocusGraphElementList);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var element in k_OnFocusGraphElementList.OfType<GraphElement>())
#pragma warning restore RS0030
            {
                element.RefreshBorder();
            }
        }

        void ClearSelection()
        {
            var selectionHelper = new GlobalSelectionCommandHelper(GraphViewModel.SelectionState);
            using (var selectionUpdaters = selectionHelper.UpdateScopes)
            {
                foreach (var updater in selectionUpdaters)
                    updater.ClearSelection();
            }
        }

        /// <summary>
        /// Updates the GraphView's space partitioning from the changes in the <see cref="SpacePartitioningStateComponent"/>.
        /// </summary>
        public virtual void UpdateSpacePartitioning()
        {
            if (m_SpacePartitioningObserver == null || GraphModel == null)
                return;

            if (GraphModel == null)
                return;

            if( GraphViewModel.GraphViewCullingState == null)
                return;

            using var markerScope = k_SpacePartitioningUpdateMarker.Auto();
            using var observation = m_SpacePartitioningObserver.ObserveState(GraphViewModel.SpacePartitioningState);

            if (observation.UpdateType == UpdateType.None)
                return;

            var changeset = GraphViewModel.SpacePartitioningState.GetAggregatedChangeset(observation.LastObservedVersion);

            if (observation.UpdateType == UpdateType.Complete || changeset == null)
            {
                RebuildSpacePartitioning();
                return;
            }

            if (changeset.ElementsToPartition.Count == 0 && changeset.ElementsToRemoveFromPartitioning.Count == 0 && changeset.ElementsToChangeContainer.Count == 0)
            {
                UpdateGraphElementsInView();
                return;
            }

            ComputeAndUpdateBounds();

            // Elements to repartition
            using var pooledToRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var elementsToRepartitionByContainer);

            using var pooledToPlacematByLayoutRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var placematUsingLayoutByContainer);
            using var pooledToPlacematByBoundingBoxRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var placematUsingBoundingBoxByContainer);

            {
                using var updater = GraphViewModel.GraphViewCullingState.UpdateScope;
                foreach (var graphElementSpacePartitioningKey in changeset.ElementsToPartition)
                {
                    var changedModel = graphElementSpacePartitioningKey.ModelGuid;
                    var context = graphElementSpacePartitioningKey.ViewContext;
                    var view = changedModel.GetView<GraphElement>(this, context);
                    if (view == null)
                        continue;

                    ResetOutOfViewCullingForGraphElement(view, updater);

                    if (view is Placemat)
                    {
                        AddViewToSpacePartitioningByContainer(view, view.parent, placematUsingLayoutByContainer, true);
                        AddViewToSpacePartitioningByContainer(view, view.parent, placematUsingBoundingBoxByContainer, false);
                    }
                    else
                    {
                        AddViewToSpacePartitioningByContainer(view, view.parent, elementsToRepartitionByContainer);
                    }
                }

                // Elements to remove from partitioning
                using var pooledToRemove = DictionaryPool<VisualElement, List<GraphElementSpacePartitioningKey>>.Get(out var elementsToRemoveByContainer);
                List<GraphElementSpacePartitioningKey> elementsToRemoveFromAllContainer = null;
                foreach (var (container, graphElementSpacePartitioningKey) in changeset.ElementsToRemoveFromPartitioning)
                {
                    // Elements that are removed from space partitioning should no longer be culled.
                    if (CullingState == GraphViewCullingState.Enabled)
                    {
                        m_GraphElementsInView.Remove(graphElementSpacePartitioningKey);
                        updater.MarkGraphElementAsRevealed(graphElementSpacePartitioningKey, GetAllCullingSources());
                    }

                    // During an undo/redo, we can't get the old container so remove them from all containers
                    if (container == null)
                    {
                        elementsToRemoveFromAllContainer ??= ListPool<GraphElementSpacePartitioningKey>.Get();
                        elementsToRemoveFromAllContainer.Add(graphElementSpacePartitioningKey);
                        continue;
                    }

                    RemoveElementFromSpacePartitioningByContainer(graphElementSpacePartitioningKey, container, elementsToRemoveByContainer);
                }

                // Elements to move from containers
                foreach (var(oldContainer, newContainer, graphElementSpacePartitioningKey) in changeset.ElementsToChangeContainer)
                {
                    RemoveElementFromSpacePartitioningByContainer(graphElementSpacePartitioningKey, oldContainer, elementsToRemoveByContainer);

                    var changedModel = graphElementSpacePartitioningKey.ModelGuid;
                    var context = graphElementSpacePartitioningKey.ViewContext;
                    var view = changedModel.GetView<GraphElement>(this, context);
                    if (view == null)
                        continue;
                    AddViewToSpacePartitioningByContainer(view, newContainer, elementsToRepartitionByContainer);

                    if (view is Placemat)
                    {
                        AddViewToSpacePartitioningByContainer(view, view.parent, placematUsingLayoutByContainer, true);
                        AddViewToSpacePartitioningByContainer(view, view.parent, placematUsingBoundingBoxByContainer, false);
                    }
                    else
                    {
                        AddViewToSpacePartitioningByContainer(view, view.parent, elementsToRepartitionByContainer);
                    }
                }

                foreach (var kvp in elementsToRepartitionByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.General);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                foreach (var kvp in placematUsingLayoutByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematLayout);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                foreach (var kvp in placematUsingBoundingBoxByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematBoundingBox);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                foreach (var kvp in elementsToRemoveByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRemove = kvp.Value;
                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.General);
                    spacePartitioning.RemoveElements(elementsToRemove);

                    spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematLayout);
                    spacePartitioning.RemoveElements(elementsToRemove);

                    spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematBoundingBox);
                    spacePartitioning.RemoveElements(elementsToRemove);
                }

                // Remove elements flagged to be removed from all containers.
                if (elementsToRemoveFromAllContainer != null)
                {
                    foreach (var kvp in m_SpacePartitioningByContainer)
                        kvp.Value.RemoveElements(elementsToRemoveFromAllContainer);
                    foreach (var kvp in m_PlacematsUsingBoundingBoxSpacePartitioningByContainer)
                        kvp.Value.RemoveElements(elementsToRemoveFromAllContainer);
                    foreach (var kvp in m_PlacematsUsingLayoutSpacePartitioningByContainer)
                        kvp.Value.RemoveElements(elementsToRemoveFromAllContainer);
                }


                // Release inner pooled collections.
                foreach (var elementList in elementsToRepartitionByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }
                foreach (var elementList in placematUsingLayoutByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }
                foreach (var elementList in placematUsingBoundingBoxByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }

                foreach (var keyList in elementsToRemoveByContainer.Values)
                {
                    ListPool<GraphElementSpacePartitioningKey>.Release(keyList);
                }

                if (elementsToRemoveFromAllContainer != null)
                    ListPool<GraphElementSpacePartitioningKey>.Release(elementsToRemoveFromAllContainer);

            }
            // Update GraphElements in view
            UpdateGraphElementsInView();
        }

        void ComputeAndUpdateBounds()
        {
            // Do not make the object dirty just because the bounds changed.
            if(!GraphModel?.GraphObject.Dirty ?? true)
                return;

            k_UpdateAllUIs.Clear();
            foreach (var model in GraphModel.GetGraphElementModels())
            {
                if (model is WireModel || (model is VariableNodeModel variableNode && variableNode.VariableDeclarationModel.IsInputOrOutput))
                    continue;

                // we want the root view only as we assume all model stored in GraphElementContainer are included in their parents.
                model.AppendAllViews(this, null, k_UpdateAllUIs);
            }

            if (k_UpdateAllUIs.Count > 0)
            {
                //Some element bounds have changed, recompute the Graph bounds.
                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;

                foreach (var childView in k_UpdateAllUIs)
                {
                    if (childView.parent.parent != ContentViewContainer)
                        continue;

                    var childBounds = childView.layout;
                    if (float.IsNaN(childBounds.xMin) || float.IsNaN(childBounds.yMin) || float.IsNaN(childBounds.xMax) || float.IsNaN(childBounds.yMax))
                        continue;

                    if (minX > childBounds.xMin)
                        minX = childBounds.xMin;
                    if (minY > childBounds.yMin)
                        minY = childBounds.yMin;
                    if (maxX < childBounds.xMax)
                        maxX = childBounds.xMax;
                    if (maxY < childBounds.yMax)
                        maxY = childBounds.yMax;
                }

                k_UpdateAllUIs.Clear();

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (minX != float.MaxValue && minY != float.MaxValue && maxX != float.MinValue && maxY != float.MinValue)
                // ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    GraphModel.LastKnownBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                }
            }
        }

        /// <summary>
        /// Rebuilds the space partitioning of the entire <see cref="GraphView"/>.
        /// </summary>
        protected internal virtual void RebuildSpacePartitioning() //internal for tests
        {
            ClearSpacePartitioning();

            if (GraphModel?.GetGraphElementModels() == null)
                return;

            using var pooledList = ListPool<ChildView>.Get(out var allViews);
            GraphModel.GetGraphElementModels()
                .GetAllViews(this, e => e is GraphElement, allViews);

            if (allViews.Count == 0)
                return;

            using var pooledToRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var elementsToRepartitionByContainer);
            using var pooledPlacematUsingBoundingBoxToRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var placematsByBoundingBoxToRepartitionByContainer);
            using var pooledPlacematUsingLayoutToRepartition = DictionaryPool<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>>.Get(out var placematsByLayoutToRepartitionByContainer);

            {
                using var updater = GraphViewModel.GraphViewCullingState.UpdateScope;
                foreach (var childView in allViews)
                {
                    if (childView is not GraphElement ge)
                        continue;

                    ResetOutOfViewCullingForGraphElement(ge, updater);

                    if (childView is Placemat)
                    {
                        AddViewToSpacePartitioningByContainer(ge, ge.parent, placematsByLayoutToRepartitionByContainer, true);
                        AddViewToSpacePartitioningByContainer(ge, ge.parent, placematsByBoundingBoxToRepartitionByContainer, false);
                    }
                    else
                    {
                        AddViewToSpacePartitioningByContainer(ge, ge.parent, elementsToRepartitionByContainer);
                    }
                }

                foreach (var kvp in elementsToRepartitionByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.General);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                foreach (var kvp in placematsByLayoutToRepartitionByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematLayout);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                foreach (var kvp in placematsByBoundingBoxToRepartitionByContainer)
                {
                    var container = kvp.Key;
                    var elementsToRepartition = kvp.Value;

                    var spacePartitioning = GetSpacePartitioningByContainer(container, SpacePartitioningType.PlacematBoundingBox);
                    spacePartitioning?.AddOrUpdateElements(elementsToRepartition);
                }

                // Release inner pooled collections.
                foreach (var elementList in elementsToRepartitionByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }

                foreach (var elementList in placematsByLayoutToRepartitionByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }

                foreach (var elementList in placematsByBoundingBoxToRepartitionByContainer.Values)
                {
                    ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Release(elementList);
                }
            }

            // Update GraphElements in view
            UpdateGraphElementsInView();
        }

        void ResetOutOfViewCullingForGraphElement(GraphElement ge, GraphViewCullingStateComponent.StateUpdater updater)
        {
            // When resetting the out of view culling state of an element,
            // it must be added back into the m_GraphElementsInView and marked as revealed
            // so it can be properly processed by UpdateGraphElementsInView.
            if (CullingState == GraphViewCullingState.Enabled)
            {
                var key = new GraphElementSpacePartitioningKey(ge);
                m_GraphElementsInView.Add(key);
                updater.MarkGraphElementAsRevealed(key, GraphViewCullingSource.OutOfView);
            }
        }

        protected void ClearSpacePartitioning()
        {
            // Clear all space partitioning.
            foreach (var kvp in m_SpacePartitioningByContainer)
            {
                kvp.Value.Clear();
            }
            // Clear all space partitioning.
            foreach (var kvp in m_PlacematsUsingLayoutSpacePartitioningByContainer)
            {
                kvp.Value.Clear();
            }
            // Clear all space partitioning.
            foreach (var kvp in m_PlacematsUsingBoundingBoxSpacePartitioningByContainer)
            {
                kvp.Value.Clear();
            }

            // Clear the elements considered in view.
            m_GraphElementsInView.Clear();
        }

        static void AddViewToSpacePartitioningByContainer(GraphElement view, VisualElement container, Dictionary<VisualElement, List<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>> elementsToRepartitionByContainer, bool useLayout = false)
        {
            if (view == null || !view.CanBePartitioned())
                return;

            var graphElementBoundingBox = useLayout ? view.layout : view.GetBoundingBox();
            if (!float.IsNaN(graphElementBoundingBox.width) && !float.IsNaN(graphElementBoundingBox.height))
            {
                var elementToRepartition = CreateSpacePartitioningElement(view, graphElementBoundingBox);

                if (!elementsToRepartitionByContainer.TryGetValue(container, out var elementsToRepartition))
                {
                    elementsToRepartition = ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Get();
                    elementsToRepartitionByContainer[container] = elementsToRepartition;
                }
                elementsToRepartition.Add(elementToRepartition);
            }
        }

        static void RemoveElementFromSpacePartitioningByContainer(GraphElementSpacePartitioningKey elementKey, VisualElement container, Dictionary<VisualElement, List<GraphElementSpacePartitioningKey>> elementsToRemoveByContainer)
        {
            if (container == null)
                return;

            if (!elementsToRemoveByContainer.TryGetValue(container, out var elementsToRemove))
            {
                elementsToRemove = ListPool<GraphElementSpacePartitioningKey>.Get();
                elementsToRemoveByContainer[container] = elementsToRemove;
            }
            elementsToRemove.Add(elementKey);
        }

        protected internal enum SpacePartitioningType
        {
            General,
            PlacematLayout,
            PlacematBoundingBox
        };

        /// <summary>
        /// Gets or create the <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/> for a specific container. Each <see cref="GraphElement"/>'s container has its own
        /// space partitioning, since they are independent from each other and could move independently from each other.
        /// </summary>
        /// <param name="container">A <see cref="GraphElement"/> container.</param>
        /// <param name="spacePartitioningType">the type of space partitioning.</param>
        /// <returns>The space partitioning for the container.</returns>
        protected BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey> GetSpacePartitioningByContainer(VisualElement container, SpacePartitioningType spacePartitioningType)
        {
            Dictionary<VisualElement, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>> dictionary;
            switch (spacePartitioningType)
            {
                case SpacePartitioningType.PlacematLayout:
                    dictionary = m_PlacematsUsingLayoutSpacePartitioningByContainer;
                    break;
                case SpacePartitioningType.PlacematBoundingBox:
                    dictionary = m_PlacematsUsingBoundingBoxSpacePartitioningByContainer;
                    break;
                default:
                    dictionary = m_SpacePartitioningByContainer;
                    break;
            }

            if (!dictionary.TryGetValue(container, out var spacePartitioning))
            {
                spacePartitioning = CreateSpacePartitioningForContainer(container);
                dictionary[container] = spacePartitioning;
            }
            return spacePartitioning;
        }

        /// <summary>
        /// Creates a <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/> for a specific container. Each <see cref="GraphElement"/>'s container has its own
        /// space partitioning, since they are independent from each other and could move independently from each other.
        /// </summary>
        /// <param name="container">A <see cref="GraphElement"/> container.</param>
        /// <returns>The space partitioning for the container.</returns>
        protected virtual BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey> CreateSpacePartitioningForContainer(VisualElement container)
        {
            return new BoundingBoxKdTreePartitioning<GraphElementSpacePartitioningKey>();
        }

        /// <summary>
        /// Sets and replaces the <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/> for a specific container. <see cref="GraphElement"/>s belonging
        /// in that container are repartitioned in the new <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/>.
        /// </summary>
        /// <param name="container">The <see cref="GraphElement"/> container.</param>
        /// <param name="spacePartitioning">The new <see cref="BaseBoundingBoxSpacePartitioning{TElementKey}"/> for the container.</param>
        protected void SetSpacePartitioningForContainer(VisualElement container, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey> spacePartitioning)
        {
            if (m_SpacePartitioningByContainer.TryGetValue(container, out var oldSpacePartitioning))
                oldSpacePartitioning.Clear();

            m_SpacePartitioningByContainer[container] = spacePartitioning;

            // Partition already existing GraphElements
            if (GraphModel == null)
                return;

            using var pooledChildViewList = ListPool<ChildView>.Get(out var childViewList);
            GraphModel.GetGraphElementModels().GetAllViews(this, modelView => modelView is GraphElement ge && ge.parent == container, childViewList);

            using var pooledList = ListPool<BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement>.Get(out var elementsToPartition);
            foreach (var modelView in childViewList)
            {
                if (modelView is not GraphElement ge || !ge.CanBePartitioned())
                    continue;

                // Skip views with invalid layout.
                var graphElementBoundingBox = ge.GetBoundingBox();
                if (float.IsNaN(graphElementBoundingBox.width) || float.IsNaN(graphElementBoundingBox.height))
                    continue;

                elementsToPartition.Add(CreateSpacePartitioningElement(ge, graphElementBoundingBox));
            }

            spacePartitioning.AddOrUpdateElements(elementsToPartition);
        }

        static BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement CreateSpacePartitioningElement(GraphElement graphElement, Rect graphElementBoundingBox)
        {
            return new BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey>.BoundingBoxElement(new GraphElementSpacePartitioningKey(graphElement), graphElementBoundingBox);
        }

        /// <summary>
        /// The mode of partitioning.
        /// </summary>
        [UnityRestricted]
        internal enum PartitioningMode
        {
            /// <summary>
            /// Include placemats based on their title element.
            /// </summary>
            PlacematTitle,

            /// <summary>
            /// Include placemats based on their body.
            /// </summary>
            PlacematBody
        }

        /// <summary>
        /// Gets all <see cref="GraphElement"/>s inside a specific region.
        /// </summary>
        /// <param name="region">The region in which to find the <see cref="GraphElement"/>s. This region must be specified in the <see cref="ContentViewContainer"/>'s space.</param>
        /// <param name="partitioningMode">The mode of partitioning, for the placemats.</param>
        /// <param name="allowOverlap">Set to true (default) to allow <see cref="GraphElement"/>s that overlap the region. Otherwise, only <see cref="GraphElement"/>s that are entirely inside the region will be returned.</param>
        /// <returns>A collection of <see cref="GraphElement"/>s found inside the region.</returns>
        public IEnumerable<GraphElement> GetGraphElementsInRegion(Rect region, PartitioningMode partitioningMode, bool allowOverlap = true)
        {
            if (m_SpacePartitioningByContainer == null)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return Enumerable.Empty<GraphElement>();
#pragma warning restore RS0030

            var elementsInRegion = new List<GraphElement>();
            GetGraphElementsInRegion(region, elementsInRegion, partitioningMode, allowOverlap);

            return elementsInRegion;
        }

        /// <summary>
        /// Gets all <see cref="GraphElement"/>s inside a specific region.
        /// </summary>
        /// <param name="region">The region in which to find the <see cref="GraphElement"/>s. This region must be specified in the <see cref="ContentViewContainer"/>'s space.</param>
        /// <param name="outGraphElementsInRegion">The list that will contain the <see cref="GraphElement"/>s found in the specified region.</param>
        /// <param name="partitioningMode">The mode of partitioning, for the placemats.</param>
        /// <param name="allowOverlap">Set to true (default) to allow <see cref="GraphElement"/>s that overlap the region. Otherwise, only <see cref="GraphElement"/>s that are entirely inside the region will be returned.</param>
        public void GetGraphElementsInRegion(Rect region, List<GraphElement> outGraphElementsInRegion, PartitioningMode partitioningMode, bool allowOverlap = true)
        {
            if (m_SpacePartitioningByContainer == null)
                return;

            foreach (var kvp in m_SpacePartitioningByContainer)
            {
                var container = kvp.Key;
                var spacePartitioning = kvp.Value;
                GetGraphElementsInRegionForContainer(region, allowOverlap, container, spacePartitioning, outGraphElementsInRegion, partitioningMode);
            }

            foreach (var kvp in partitioningMode == PartitioningMode.PlacematBody ? m_PlacematsUsingLayoutSpacePartitioningByContainer : m_PlacematsUsingBoundingBoxSpacePartitioningByContainer)
            {
                var container = kvp.Key;
                var spacePartitioning = kvp.Value;
                GetGraphElementsInRegionForContainer(region, allowOverlap, container, spacePartitioning, outGraphElementsInRegion, partitioningMode);
            }
        }

        void GetGraphElementsInRegionForContainer(Rect region, bool allowOverlap, VisualElement container, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey> spacePartitioning, List<GraphElement> elementsInRegion, PartitioningMode partitioningMode)
        {
            if (spacePartitioning == null)
                return;

            var localRegion = ContentViewContainer.ChangeCoordinatesTo(container, region);
            using var pooledList = ListPool<GraphElementSpacePartitioningKey>.Get(out var containerElementKeys);
            spacePartitioning.GetElementsInRegion(localRegion, allowOverlap, containerElementKeys);
            foreach (var elementKey in containerElementKeys)
            {
                var graphElementGuid = elementKey.ModelGuid;
                var graphElementContext = elementKey.ViewContext;
                var graphElement = graphElementGuid.GetView<GraphElement>(this, graphElementContext);
                if (graphElement == null)
                    continue;


                if (partitioningMode != PartitioningMode.PlacematBody)
                {
                    localRegion = ContentViewContainer.ChangeCoordinatesTo(graphElement, region);

                    if (graphElement.Overlaps(localRegion))
                        elementsInRegion.Add(graphElement);
                }
                else
                {
                    localRegion = ContentViewContainer.ChangeCoordinatesTo(graphElement.parent, region);

                    if (graphElement.layout.Overlaps(localRegion))
                        elementsInRegion.Add(graphElement);
                }
            }
        }

        void GetGraphElementKeysWithBoundingBoxInRegion(Rect region, bool allowOverlap, HashSet<GraphElementSpacePartitioningKey> elementKeysInRegion)
        {
            if (m_SpacePartitioningByContainer == null)
                return;

            foreach (var kvp in m_SpacePartitioningByContainer)
            {
                var container = kvp.Key;
                var spacePartitioning = kvp.Value;
                GetGraphElementKeysWithBoundingBoxInRegionForContainer(region, allowOverlap, container, spacePartitioning, elementKeysInRegion);
            }
        }

        void GetGraphElementKeysWithBoundingBoxInRegionForContainer(Rect region, bool allowOverlap, VisualElement container, BaseBoundingBoxSpacePartitioning<GraphElementSpacePartitioningKey> spacePartitioning, HashSet<GraphElementSpacePartitioningKey> elementKeysInRegion)
        {
            if (spacePartitioning == null)
                return;

            var localRegion = ContentViewContainer.ChangeCoordinatesTo(container, region);
            var containerElementKeys = spacePartitioning.GetElementsInRegion(localRegion, allowOverlap);
            elementKeysInRegion.UnionWith(containerElementKeys);
        }

        /// <summary>
        /// Updates the <see cref="GraphViewCullingState"/> of the <see cref="GraphElement"/>s in view. <see cref="GraphElement"/>s
        /// not in view are culled, and <see cref="GraphElement"/>s in view are revealed. If culling is disabled, this method does nothing.
        /// </summary>
        protected void UpdateGraphElementsInView()
        {
            if (CullingState == GraphViewCullingState.Disabled || GraphViewModel?.GraphViewCullingState == null)
                return;

            var viewport = ViewportCullingRect;

            var localViewport = this.ChangeCoordinatesTo(ContentViewContainer, viewport);
            var elementKeysInRegion = new HashSet<GraphElementSpacePartitioningKey>();
            GetGraphElementKeysWithBoundingBoxInRegion(localViewport, true, elementKeysInRegion);

            using var updater = GraphViewModel.GraphViewCullingState.UpdateScope;
            foreach (var keyInRegion in elementKeysInRegion)
            {
                if (!m_GraphElementsInView.Contains(keyInRegion))
                    updater.MarkGraphElementAsRevealed(keyInRegion, GraphViewCullingSource.OutOfView);
            }

            foreach (var keyInRegion in m_GraphElementsInView)
            {
                if (!elementKeysInRegion.Contains(keyInRegion))
                    updater.MarkGraphElementAsCulled(keyInRegion, GraphViewCullingSource.OutOfView);
            }

            m_GraphElementsInView = elementKeysInRegion;
        }

        /// <summary>
        /// Enables culling for the entire <see cref="GraphView"/>. This method should only be called if culling is disabled.
        /// It sets all active culling state on all <see cref="GraphElement"/>s.
        /// </summary>
        protected virtual void EnableCulling()
        {
            if (GraphModel?.GetGraphElementModels() == null || GraphViewModel?.GraphViewCullingState == null)
                return;

            m_GraphElementsInView.Clear();
            UpdateGraphElementsInView();

            using var updater = GraphViewModel.GraphViewCullingState.UpdateScope;
            var allCullingSources = GetAllCullingSources();
            using var childViewPooledList = ListPool<ChildView>.Get(out var childViews);
            using var activeSourcePooledList = ListPool<GraphViewCullingSource>.Get(out var activeSources);
            GraphModel.GetGraphElementModels().GetAllViewsRecursively(this, view => view is GraphElement, childViews);
            foreach (var childView in childViews)
            {
                if (childView is not GraphElement ge)
                    continue;

                activeSources.Clear();
                if (ShouldGraphElementBeCulled(ge, allCullingSources, activeSources))
                    updater.MarkGraphElementAsCulled(ge, activeSources);
            }
        }

        /// <summary>
        /// Disables culling for the entire <see cref="GraphView"/>. This method should only be called if culling is enabled.
        /// It clears the culling state of all <see cref="GraphElement"/>s.
        /// </summary>
        protected virtual void DisableCulling()
        {
            if (GraphModel?.GetGraphElementModels() == null)
                return;

            m_GraphElementsInView.Clear();
            using var pooledList = ListPool<ChildView>.Get(out var childViews);
            GraphModel.GetGraphElementModels().GetAllViewsRecursively(this, view => view is GraphElement, childViews);
            foreach (var childView in childViews)
            {
                if (childView is not GraphElement ge)
                    continue;
                ge.ClearCulling();
            }
        }

        static List<GraphViewCullingSource> s_CullingSourcesCache;
        /// <summary>
        /// Gets all culling sources used in this <see cref="GraphView"/>.
        /// </summary>
        /// <returns>A list of <see cref="GraphViewCullingSource"/>.</returns>
        public virtual IReadOnlyList<GraphViewCullingSource> GetAllCullingSources()
        {
            if (s_CullingSourcesCache == null)
            {
                s_CullingSourcesCache = new List<GraphViewCullingSource>();
                var cullingSources = Enumeration.GetDeclared<GraphViewCullingSource>();
                foreach (var graphViewCullingSource in cullingSources)
                {
                    s_CullingSourcesCache.Add(graphViewCullingSource);
                }
            }
            return s_CullingSourcesCache;
        }

        /// <summary>
        /// Gets the viewport culling rect.
        /// </summary>
        /// <value>The rect used to culled <see cref="GraphElement"/>s outside the viewport.</value>
        protected virtual Rect ViewportCullingRect => layout;

        /// <summary>
        /// Checks if a <see cref="GraphElement"/> should be culled based on the culling sources.
        /// The culling sources that should be active are returned in <paramref name="outActiveCullingSources"/> if not null.
        /// </summary>
        /// <param name="graphElement">The graph element.</param>
        /// <param name="cullingSources">The culling sources.</param>
        /// <param name="outActiveCullingSources">The active culling sources that should be applied on the graph element. It is populated by this method if the value is not null.</param>
        /// <returns>True if the graph element should be culled, false otherwise.</returns>
        /// <remarks>This method should be overridden to support custom culling sources.</remarks>
        public virtual bool ShouldGraphElementBeCulled(GraphElement graphElement, IReadOnlyList<GraphViewCullingSource> cullingSources, IList<GraphViewCullingSource> outActiveCullingSources)
        {
            if (graphElement == null || cullingSources == null || CullingState == GraphViewCullingState.Disabled)
                return false;

            var culled = false;
            for (var i = 0; i < cullingSources.Count; i++)
            {
                var cullingSource = cullingSources[i];

                if (!graphElement.SupportsCulling(cullingSource))
                    continue;

                if ((cullingSource == GraphViewCullingSource.OutOfView && !m_GraphElementsInView.Contains(new GraphElementSpacePartitioningKey(graphElement)))
                    || (HasCullingOnZoom && cullingSource == GraphViewCullingSource.Zoom && IsZoomCullingSize(m_ZoomMode)))
                {
                    culled = true;
                    if (outActiveCullingSources == null)
                        return true;
                    outActiveCullingSources.Add(cullingSource);
                }
            }

            return culled;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateGraphElementsInView();
        }

        /// <inheritdoc />
        public override void HandleGlobalValidateCommand(ValidateCommandEvent evt)
        {
            ViewSelection.OnValidateCommand(evt);
        }

        /// <inheritdoc />
        public override void HandleGlobalExecuteCommand(ExecuteCommandEvent evt)
        {
            ViewSelection.OnExecuteCommand(evt);
        }

        public virtual IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.addNodeItem,
            ContextualMenuHelpers.createPlacematItem,
            ContextualMenuHelpers.createStickyNoteItem,
            ContextualMenuHelpers.createEmptyLocalSubgraphItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.selectAllItem,
        };

        internal class TestAccess
        {
            readonly GraphView m_GraphView;
            public TestAccess(GraphView graphView)
            {
                m_GraphView = graphView;
            }

            public void AppendInsertBlockItemMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, bool insertAbove, string itemName = "") => m_GraphView.AppendInsertBlockItemMenuItem(evt, selection, insertAbove, itemName);
            public void AppendFontSizeAndThemeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendFontSizeAndThemeMenuItem(evt, selection);
            public void AppendCreatePlacematMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendCreatePlacematMenuItem(evt, selection);
            public void AppendColorMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendColorMenuItem(evt, selection);
            public void AppendAlignAndDistributeElementsMenuItems(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendAlignAndDistributeElementsMenuItems(evt, selection);
            public void AppendCreateEmptyLocalSubgraph(ContextualMenuPopulateEvent evt) => m_GraphView.AppendCreateEmptyLocalSubgraph(evt);
            public void AppendToggleCollapseMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendToggleCollapseMenuItem(evt, selection);
            public void AppendDisableNodeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendDisableNodeMenuItem(evt, selection);
            public void AppendSetAsDefaultStateMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendSetAsDefaultStateMenuItem(evt, selection);
            public void AppendCreateTransitionMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, TransitionSupportKind transitionKind) => m_GraphView.AppendCreateTransitionMenuItem(evt, selection, transitionKind);
            public void AppendCreateLocalSubgraphFromSelectionMenuItem(ContextualMenuPopulateEvent evt) => m_GraphView.AppendCreateLocalSubgraphFromSelectionMenuItem(evt);
            public void AppendExtractContentsToPlacematMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendExtractContentsToPlacematMenuItem(evt, selection);
            public void AppendOpenSubgraphMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendOpenSubgraphMenuItem(evt, selection);
            public void AppendConvertToAssetSubgraphMenuItem(ContextualMenuPopulateEvent evt) => m_GraphView.AppendConvertToAssetSubgraphMenuItem(evt);
            public void AppendUnpackToLocalSubgraphMenuItem(ContextualMenuPopulateEvent evt) => m_GraphView.AppendUnpackToLocalSubgraphMenuItem(evt);
            public void AppendFindAssetInProjectMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendFindAssetInProjectMenuItem(evt, selection);
            public void AppendConvertToConstantMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendConvertToConstantMenuItem(evt, selection);
            public void AppendConvertToPortalsMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendConvertToPortalsMenuItem(evt, selection);
            public void AppendInsertNodeMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendInsertNodeMenuItem(evt, selection);
            public void AppendCreateOppositePortalMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection) => m_GraphView.AppendCreateOppositePortalMenuItem(evt, selection);
            public void AppendRevertWiresMenuItem(ContextualMenuPopulateEvent evt, List<GraphElementModel> selection, bool revertAll) => m_GraphView.AppendRevertWiresMenuItem(evt, selection, revertAll);
        }
    }
}
