// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    class ContentViewContainer_Internal : VisualElement
    {
        public override bool Overlaps(Rect r)
        {
            return true;
        }
    }

    /// <summary>
    /// Display modes for the GraphView.
    /// </summary>
    enum GraphViewDisplayMode
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
    class GraphView : RootView, IDragSource
    {
        public const int frameBorder = 30;
        const float k_VerySmallZoom = 0.11f;
        const float k_SmallZoom = 0.20f;
        const float k_MediumZoom = 0.75f;

        GraphViewZoomMode m_ZoomMode;

        /// <summary>
        /// GraphView elements are organized into layers to ensure some type of graph elements
        /// are always drawn on top of others.
        /// </summary>
        public class Layer : VisualElement {}

        static readonly List<ChildView> k_UpdateAllUIs = new();

        public new static readonly string ussClassName = "ge-graph-view";
        public static readonly string ussNonInteractiveModifierClassName = ussClassName.WithUssModifier("non-interactive");

        /// <summary>
        /// The uss modifier that appears when the graph view is zoomed out to the small level.
        /// </summary>
        public static readonly string smallModifier = "small";

        /// <summary>
        /// The uss modifier that appears when the graph view is zoomed out to the very small level.
        /// </summary>
        public static readonly string verySmallModifier = "very-small";

        /// <summary>
        /// The uss modifier that appears when the graph view is at 100% or zoomed in;
        /// </summary>
        public static readonly string mediumModifier = "medium";

        /// <summary>
        /// The class name of the graphview with the small modifier.
        /// </summary>
        public static readonly string ussSmallModifierClassName = ussClassName.WithUssModifier(smallModifier);

        /// <summary>
        /// The class name of the graphview with the very small modifier.
        /// </summary>
        public static readonly string ussVerySmallModifierClassName = ussClassName.WithUssModifier(verySmallModifier);

        /// <summary>
        /// The class name of the graphview with the medium modifier.
        /// </summary>
        public static readonly string ussMediumModifierClassName = ussClassName.WithUssModifier(mediumModifier);

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        ContextualMenuManipulator m_ContextualMenuManipulator;
        ContentZoomer m_Zoomer;

        AutoAlignmentHelper_Internal m_AutoAlignmentHelper;
        AutoDistributingHelper_Internal m_AutoDistributingHelper;

        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_MaxScaleOnFrame = 1.0f;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        readonly VisualElement m_GraphViewContainer;
        readonly VisualElement m_MarkersParent;

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        protected IDragAndDropHandler m_CurrentDragAndDropHandler;
        protected IDragAndDropHandler m_BlackboardDragAndDropHandler;

        protected bool m_SelectionDraggerWasActive;

        GraphViewStateComponent.GraphLoadedObserver m_GraphViewGraphLoadedObserver;
        GraphModelStateComponent.GraphAssetLoadedObserver m_GraphModelGraphLoadedAssetObserver;
        SelectionStateComponent.GraphLoadedObserver m_SelectionGraphLoadedObserver;
        GraphProcessingStateComponent.GraphLoadedObserver m_ProcessingGraphLoadedObserver;
        GraphProcessingErrorsStateComponent.GraphLoadedObserver m_ProcessingErrorsGraphLoadedObserver;
        ModelViewUpdater m_UpdateObserver;
        WireOrderObserver_Internal m_WireOrderObserver;
        DeclarationHighlighter m_DeclarationHighlighter;
        AutoPlacementObserver m_AutoPlacementObserver;
        AutomaticGraphProcessingObserver m_AutomaticGraphProcessingObserver;
        GraphProcessingErrorObserver m_GraphProcessingErrorObserver;

        ViewSelection m_ViewSelection;

        /// <summary>
        /// The display mode.
        /// </summary>
        public GraphViewDisplayMode DisplayMode { get; }

        /// <summary>
        /// The VisualElement that contains all the views.
        /// </summary>
        public VisualElement ContentViewContainer { get; }

        GridBackground GridBackground { get; }

        /// <summary>
        /// The transform of the ContentViewContainer.
        /// </summary>
        public ITransform ViewTransform => ContentViewContainer.transform;

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

        public GraphViewModel GraphViewModel => (GraphViewModel)Model;

        /// <summary>
        /// The graph model displayed by the graph view.
        /// </summary>
        public GraphModel GraphModel => GraphViewModel.GraphModelState?.GraphModel;

        public ViewSelection ViewSelection
        {
            get => m_ViewSelection;
            set
            {
                if (m_ViewSelection != null)
                {
                    m_ViewSelection.DetachFromView();
                }

                m_ViewSelection = value;
                m_ViewSelection.AttachToView();
            }
        }

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

        internal PositionDependenciesManager_Internal PositionDependenciesManager_Internal { get; }

        /// <summary>
        /// Instantiates another <see cref="GraphView"/> meant for display only.
        /// </summary>
        /// <remarks>Override this for the previews in Item Library to use your own class.</remarks>
        /// <returns>A new instance of <see cref="GraphView"/>.</returns>
        public virtual GraphView CreateSimplePreview()
        {
            return Create(null, null, "", GraphViewDisplayMode.NonInteractive);
        }

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        /// <param name="window">The window to which the GraphView belongs.</param>
        /// <param name="graphTool">The tool for this GraphView.</param>
        /// <param name="graphViewName">The name of the GraphView.</param>
        /// <param name="displayMode">The display mode for the graph view.</param>
        public static GraphView Create(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
        {
            var view = new GraphView(window, graphTool, graphViewName, displayMode);
            view.Initialize();
            return view;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GraphView" /> class. Call <see cref="RootView.Initialize"/> to initialize it.
        /// </summary>
        /// <param name="window">The window to which the GraphView belongs.</param>
        /// <param name="graphTool">The tool for this GraphView.</param>
        /// <param name="graphViewName">The name of the GraphView.</param>
        /// <param name="displayMode">The display mode for the graph view.</param>
        protected GraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
        : base(window, graphTool)
        {
            DisplayMode = displayMode;
            graphViewName ??= "GraphView_" + UnityEngine.Random.Range(0, Int32.MaxValue);

            if (GraphTool != null)
            {
                GraphModel graphModel = GraphTool.ToolState.GraphModel;
                Model = new GraphViewModel(graphViewName, graphModel); // PF FIXME this is not great for derived classes that want to add state components.

                if (DisplayMode == GraphViewDisplayMode.Interactive)
                {
                    ProcessOnIdleAgent = new ProcessOnIdleAgent(GraphTool.Preferences);
                    GraphTool.State.AddStateComponent(ProcessOnIdleAgent.StateComponent);

                    GraphViewCommandsRegistrar.RegisterCommands(this, GraphTool);
                }

                ViewSelection = new GraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);
            }

            name = graphViewName;

            AddToClassList(ussClassName);
            EnableInClassList(ussNonInteractiveModifierClassName, DisplayMode == GraphViewDisplayMode.NonInteractive);

            renderHints = RenderHints.ClipWithScissors;

            m_GraphViewContainer = new VisualElement() { name = "graph-view-container" };
            m_GraphViewContainer.pickingMode = PickingMode.Ignore;
            hierarchy.Add(m_GraphViewContainer);

            ContentViewContainer = new ContentViewContainer_Internal
            {
                name = "content-view-container",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.GroupTransform
            };
            ContentViewContainer.style.transformOrigin = new TransformOrigin(0, 0, 0);
            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            m_GraphViewContainer.Add(ContentViewContainer);

            m_MarkersParent = new VisualElement { name = "marker-container" };

            this.AddStylesheet_Internal("GraphView.uss");

            GridBackground = new GridBackground();
            Insert(0, GridBackground);

            PlacematContainer = new PlacematContainer(this);
            AddLayer(PlacematContainer, PlacematContainer.PlacematsLayer);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            PositionDependenciesManager_Internal = new PositionDependenciesManager_Internal(this, GraphTool?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper_Internal(this);
            m_AutoDistributingHelper = new AutoDistributingHelper_Internal(this);

            if (DisplayMode == GraphViewDisplayMode.Interactive)
            {
                ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

                Clickable = new Clickable(OnDoubleClick);
                Clickable.activators.Clear();
                Clickable.activators.Add(
                    new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

                ContentDragger = new ContentDragger();
                SelectionDragger = new SelectionDragger(this);
                RectangleSelector = new RectangleSelector();
                FreehandSelector = new FreehandSelector();

                RegisterCallback<ValidateCommandEvent>(OnValidateCommand_Internal);
                RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand_Internal);

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
                RegisterCallback<ShortcutFramePreviousEvent>(OnShortcutFramePreviousEvent);
                RegisterCallback<ShortcutFrameNextEvent>(OnShortcutFrameNextEvent);
                RegisterCallback<ShortcutDeleteEvent>(OnShortcutDeleteEvent);
                RegisterCallback<ShortcutShowItemLibraryEvent>(OnShortcutShowItemLibraryEvent);
                RegisterCallback<ShortcutConvertConstantAndVariableEvent>(OnShortcutConvertVariableAndConstantEvent);
                // TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
                // RegisterCallback<ShortcutAlignNodesEvent>(OnShortcutAlignNodesEvent);
                // RegisterCallback<ShortcutAlignNodeHierarchiesEvent>(OnShortcutAlignNodeHierarchyEvent);
                RegisterCallback<ShortcutCreateStickyNoteEvent>(OnShortcutCreateStickyNoteEvent);
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

            PositionDependenciesManager_Internal.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element);
            }
        }

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
            GridBackground.MarkDirtyRepaint();
            float validateFloat = pan.x + pan.y + pan.z + zoom.x + zoom.y + zoom.z;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
                return;

            pan.x = GUIUtility.RoundToPixelGrid(pan.x);
            pan.y = GUIUtility.RoundToPixelGrid(pan.y);

            ContentViewContainer.transform.position = pan;

            Vector3 oldScale = ContentViewContainer.transform.scale;

            if (oldScale != zoom)
            {
                GraphViewZoomMode oldMode = m_ZoomMode;
                if (zoom.x < k_VerySmallZoom)
                    m_ZoomMode = GraphViewZoomMode.VerySmall;
                else if (zoom.x < k_SmallZoom)
                    m_ZoomMode = GraphViewZoomMode.Small;
                else if (zoom.x < k_MediumZoom)
                    m_ZoomMode = GraphViewZoomMode.Medium;
                else
                    m_ZoomMode = GraphViewZoomMode.Normal;

                EnableInClassList(ussSmallModifierClassName, m_ZoomMode is GraphViewZoomMode.Small or GraphViewZoomMode.VerySmall);
                EnableInClassList(ussVerySmallModifierClassName, m_ZoomMode == GraphViewZoomMode.VerySmall);
                EnableInClassList(ussMediumModifierClassName, m_ZoomMode is GraphViewZoomMode.Medium or GraphViewZoomMode.Small or GraphViewZoomMode.VerySmall );

                ContentViewContainer.transform.scale = zoom;

                RectangleSelector?.MarkDirtyRepaint();
                FreehandSelector?.MarkDirtyRepaint();

                if (GraphModel != null)
                {
                    GraphModel.GraphElementModels.GetAllViewsRecursivelyInList_Internal(this, _ => true, k_UpdateAllUIs);
                    foreach (var graphElement in k_UpdateAllUIs.OfType<GraphElement>())
                    {
                        graphElement.SetLevelOfDetail(zoom.x, m_ZoomMode, oldMode);
                    }

                    foreach (var marker in m_MarkersParent.Children().OfType<Marker>())
                    {
                        marker.SetLevelOfDetail(zoom.x, m_ZoomMode, oldMode);
                    }

                    k_UpdateAllUIs.Clear();
                }
            }
        }

        /// <summary>
        /// Base speed for panning, made internal to disable panning in tests.
        /// </summary>
        internal static float BasePanSpeed_Internal { get; set; } = 0.4f;
        internal const int panIntervalMs_Internal = 10; // interval between each pan in milliseconds
        internal static float MinPanSpeed_Internal => GraphViewSettings.panMinSpeedFactor_Internal * BasePanSpeed_Internal;
        internal static float MaxPanSpeed_Internal => GraphViewSettings.panMaxSpeedFactor_Internal * BasePanSpeed_Internal;

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
        internal static float GetPanSpeed_Internal(float distanceRatio)
        {
            distanceRatio = ApplyEasing(distanceRatio, GraphViewSettings.panEasingFunction_Internal);
            return distanceRatio * (MaxPanSpeed_Internal - MinPanSpeed_Internal) + MinPanSpeed_Internal;
        }

        internal static float GetPanBorderSize_Internal(Vector2 viewSize)
        {
            if (!GraphViewSettings.PanUsePercentage_Internal)
                return GraphViewSettings.panAreaSize_Internal;

            var minLayoutSize = Mathf.Min(viewSize.x, viewSize.y);
            return GraphViewSettings.panAreaSize_Internal / 100f * minLayoutSize;
        }

        internal float GetPanBorderSize_Internal()
        {
            return GetPanBorderSize_Internal(layout.size);
        }

        internal static Vector2 GetPanSpeed_Internal(Vector2 mousePos, Vector2 viewSize)
        {
            var effectiveSpeed = Vector2.zero;

            var panAreaSize = GetPanBorderSize_Internal(viewSize);
            if (mousePos.x <= panAreaSize)
                effectiveSpeed.x = -GetPanSpeed_Internal((panAreaSize - mousePos.x) / panAreaSize);
            else if (mousePos.x >= viewSize.x - panAreaSize)
                effectiveSpeed.x = GetPanSpeed_Internal((mousePos.x - (viewSize.x - panAreaSize)) / panAreaSize);

            if (mousePos.y <= panAreaSize)
                effectiveSpeed.y = -GetPanSpeed_Internal((panAreaSize - mousePos.y) / panAreaSize);
            else if (mousePos.y >= viewSize.y - panAreaSize)
                effectiveSpeed.y = GetPanSpeed_Internal((mousePos.y - (viewSize.y - panAreaSize)) / panAreaSize);

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, MaxPanSpeed_Internal);

            return effectiveSpeed;
        }

        internal Vector2 GetEffectivePanSpeed_Internal(Vector2 worldMousePos)
        {
            var localMouse = contentContainer.WorldToLocal(worldMousePos);
            return GetPanSpeed_Internal(localMouse, contentContainer.layout.size);
        }

        /// <summary>
        /// Gets a <see cref="IDragAndDropHandler"/> that can handle dragged and dropped objects from the blackboard.
        /// </summary>
        /// <returns>A <see cref="IDragAndDropHandler"/> that can handle dragged and dropped objects from the blackboard.</returns>
        protected virtual IDragAndDropHandler GetBlackboardDragAndDropHandler()
        {
            return m_BlackboardDragAndDropHandler ??= new SelectionDropperDropHandler(this);
        }

        /// <summary>
        /// Find an appropriate drag and drop handler for the current drag and drop operation.
        /// </summary>
        /// <returns>The <see cref="IDragAndDropHandler"/> that can handle the objects being dragged.</returns>
        protected virtual IDragAndDropHandler GetDragAndDropHandler()
        {
            var selectionDropperDropHandler = GetBlackboardDragAndDropHandler();
            if (selectionDropperDropHandler?.CanHandleDrop() ?? false)
                return selectionDropperDropHandler;

            return null;
        }

        void AddLayer(Layer layer, int index)
        {
            m_ContainerLayers.Add(index, layer);

            int indexOfLayer = m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value).ToList().IndexOf(layer);

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

        internal void ChangeLayer_Internal(GraphElement element)
        {
            if (!m_ContainerLayers.ContainsKey(element.Layer))
                AddLayer(element.Layer);

            GetLayer(element.Layer).Add(element);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, maxScaleOnFrame, m_ScaleStep, m_ReferenceScale);
        }

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
                    minScale = m_MinScale,
                    maxScale = m_MaxScale,
                    scaleStep = m_ScaleStep,
                    referenceScale = m_ReferenceScale
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
            Vector3 transformScale = ViewTransform.scale;

            transformScale.x = Mathf.Clamp(transformScale.x, m_MinScale, m_MaxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, m_MinScale, m_MaxScale);

            ViewTransform.scale = transformScale;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            var selection = GetSelection().ToList();
            if (!selection.Any(e => e is IPlaceholder || e is IHasDeclarationModel hasDeclarationModel && hasDeclarationModel.DeclarationModel is IPlaceholder))
            {
                evt.menu.AppendAction("Create Node", menuAction =>
                {
                    Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                    ShowItemLibrary(mousePosition);
                });
                var nodesAndNotes = selection.
                    Where(e => e is AbstractNodeModel || e is StickyNoteModel).
                    Select(m => m.GetView<GraphElement>(this)).
                    Where(v => v != null).ToList();

                bool hasNodeOnGraph = nodesAndNotes.Any(t => !t.GraphElementModel.NeedsContainer());

                evt.menu.AppendAction("Create Placemat", menuAction =>
                {
                    Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                    Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

                    if (hasNodeOnGraph)
                    {
                        Rect bounds = new Rect();
                        if (Placemat.ComputeElementBounds_Internal(ref bounds, nodesAndNotes.Where(t => !t.GraphElementModel.NeedsContainer())))
                        {
                            Dispatch(new CreatePlacematCommand(bounds));
                        }
                        else
                        {
                            Dispatch(new CreatePlacematCommand(graphPosition));
                        }
                    }
                    else
                    {
                        Dispatch(new CreatePlacematCommand(graphPosition));
                    }
                });
                evt.menu.AppendAction("Create Sticky Note", menuAction =>
                {
                    Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                    Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

                    Dispatch(new CreateStickyNoteCommand(graphPosition));
                });

                if (selection.Any())
                {

                    /* Actions on selection */

                    evt.menu.AppendSeparator();

                    if (hasNodeOnGraph)
                    {
                        // TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
                        // var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Items", GraphTool.Name, ShortcutAlignNodesEvent.id);
                        // evt.menu.AppendAction(itemName, _ =>
                        // {
                        //     Dispatch(new AlignNodesCommand(this, false, GetSelection()));
                        // });
                        //
                        // itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Hierarchy", GraphTool.Name, ShortcutAlignNodeHierarchiesEvent.id);
                        // evt.menu.AppendAction(itemName, _ =>
                        // {
                        //     Dispatch(new AlignNodesCommand(this, true, GetSelection()));
                        // });

                        var selectionUI = selection.Select(m => m.GetView<GraphElement>(this));
                        if (selectionUI.Count(elem => elem != null && !(elem.Model is WireModel) && elem.visible) > 1)
                        {
                            evt.menu.AppendAction("Align Elements/Top",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference.Top));

                            evt.menu.AppendAction("Align Elements/Bottom",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference.Bottom));

                            evt.menu.AppendAction("Align Elements/Left",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference.Left));

                            evt.menu.AppendAction("Align Elements/Right",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference.Right));

                            evt.menu.AppendAction("Align Elements/Horizontal Center",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference
                                    .HorizontalCenter));

                            evt.menu.AppendAction("Align Elements/Vertical Center",
                                _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper_Internal.AlignmentReference
                                    .VerticalCenter));

                            evt.menu.AppendAction("Distribute Elements/Horizontal",
                                _ => m_AutoDistributingHelper.SendDistributeCommand(PortOrientation.Horizontal));

                            evt.menu.AppendAction("Distribute Elements/Vertical",
                                _ => m_AutoDistributingHelper.SendDistributeCommand(PortOrientation.Vertical));
                        }
                    }

                    var nodes = selection.OfType<AbstractNodeModel>().ToList();
                    if (nodes.Count > 0)
                    {
                        var connectedNodes = nodes
                            .Where(m => m.GetConnectedWires().Any())
                            .ToList();

                        evt.menu.AppendAction("Disconnect Wires", _ =>
                        {
                            Dispatch(new DisconnectWiresCommand(connectedNodes));
                        }, connectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                        var ioConnectedNodes = connectedNodes
                            .OfType<InputOutputPortsNodeModel>()
                            .Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected()) &&
                                x.OutputsByDisplayOrder.Any(y => y.IsConnected())).ToList();

                        evt.menu.AppendAction("Bypass Nodes", _ =>
                        {
                            Dispatch(new BypassNodesCommand(ioConnectedNodes, nodes));
                        }, ioConnectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                        var willDisable = nodes.Any(n => n.State == ModelState.Enabled);
                        evt.menu.AppendAction(willDisable ? "Disable Nodes" : "Enable Nodes", _ =>
                        {
                            Dispatch(new ChangeNodeStateCommand(willDisable ? ModelState.Disabled : ModelState.Enabled, nodes));
                        });
                    }

                    if (selection.Count == 2)
                    {
                        // PF: FIXME check conditions correctly for this actions (exclude single port nodes, check if already connected).
                        if (selection.FirstOrDefault(x => x is WireModel) is WireModel wireModel &&
                            selection.FirstOrDefault(x => x is InputOutputPortsNodeModel) is InputOutputPortsNodeModel nodeModel)
                        {
                            evt.menu.AppendAction("Insert Node on Wire", _ => Dispatch(new SplitWireAndInsertExistingNodeCommand(wireModel, nodeModel)),
                                _ => DropdownMenuAction.Status.Normal);
                        }
                    }

                    var variableNodes = nodes.OfType<VariableNodeModel>().ToList();
                    var constants = nodes.OfType<ConstantNodeModel>().ToList();
                    if (variableNodes.Count > 0)
                    {
                        // TODO JOCE We might want to bring the concept of Get/Set variable from VS down to GTF
                        var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Variable/Convert", GraphTool.Name, ShortcutConvertConstantAndVariableEvent.id);
                        evt.menu.AppendAction(itemName,
                            _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(null, variableNodes)),
                            variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                        evt.menu.AppendAction("Variable/Itemize",
                            _ => Dispatch(new ItemizeNodeCommand(variableNodes.OfType<ISingleOutputPortNodeModel>().ToList())),
                            variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    }

                    if (constants.Count > 0)
                    {
                        var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Constant/Convert", GraphTool.Name, ShortcutConvertConstantAndVariableEvent.id);
                        evt.menu.AppendAction(itemName,
                            _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constants, null)), _ => DropdownMenuAction.Status.Normal);

                        evt.menu.AppendAction("Constant/Itemize",
                            _ => Dispatch(new ItemizeNodeCommand(constants.OfType<ISingleOutputPortNodeModel>().ToList())),
                            constants.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                        evt.menu.AppendAction("Constant/Lock",
                            _ => Dispatch(new LockConstantNodeCommand(constants, true)),
                            _ =>
                                constants.Any(e => !e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                        );

                        evt.menu.AppendAction("Constant/Unlock",
                            _ => Dispatch(new LockConstantNodeCommand(constants, false)),
                            _ =>
                                constants.Any(e => e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                        );
                    }

                    if (GraphModel.Stencil.AllowPortalCreation)
                    {
                        var portals = nodes.OfType<WirePortalModel>().ToList();
                        if (portals.Count > 0)
                        {
                            var canCreate = portals.Where(p => p.CanCreateOppositePortal()).ToList();
                            evt.menu.AppendAction("Create Opposite Portal",
                                _ =>
                                {
                                    Dispatch(new CreateOppositePortalCommand(canCreate));
                                }, canCreate.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                        }
                    }

                    var colorables = selection.Where(s => s.IsColorable()).ToList();
                    if (colorables.Any())
                    {
                        evt.menu.AppendAction("Color/Change...", _ =>
                        {
                            void ChangeNodesColor(Color pickedColor)
                            {
                                Dispatch(new ChangeElementColorCommand(pickedColor, colorables));
                            }

                            var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                            if (colorables.Count == 1 && colorables[0].HasUserColor)
                            {
                                defaultColor = colorables[0].Color;
                            }

                            bool showAlpha = colorables.All(t => t.UseColorAlpha);

                            ColorPicker.Show(ChangeNodesColor, defaultColor, showAlpha);
                        });

                        evt.menu.AppendAction("Color/Reset", _ =>
                        {
                            Dispatch(new ResetElementColorCommand(colorables));
                        });
                    }
                    else
                    {
                        evt.menu.AppendAction("Color", _ => {}, _ => DropdownMenuAction.Status.Disabled);
                    }

                    if (GraphModel.Stencil.AllowPortalCreation)
                    {
                        var wires = selection.OfType<WireModel>().ToList();
                        if (wires.Count > 0)
                        {
                            evt.menu.AppendSeparator();

                            var wireData = Wire.GetPortalsWireData(wires, this);
                            evt.menu.AppendAction("Add Portals", _ =>
                            {
                                Dispatch(new ConvertWiresToPortalsCommand(wireData, this));
                            });
                        }
                    }

                    var stickyNotes = selection.OfType<StickyNoteModel>().ToList();

                    if (stickyNotes.Count > 0)
                    {
                        evt.menu.AppendSeparator();

                        DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
                        {
                            if (stickyNotes.Any(noteModel => noteModel.Theme != stickyNotes.First().Theme))
                            {
                                // Values are not all the same.
                                return DropdownMenuAction.Status.Normal;
                            }

                            return stickyNotes.First().Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                        }

                        DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
                        {
                            if (stickyNotes.Any(noteModel => noteModel.TextSize != stickyNotes.First().TextSize))
                            {
                                // Values are not all the same.
                                return DropdownMenuAction.Status.Normal;
                            }

                            return stickyNotes.First().TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                        }

                        foreach (var value in StickyNote.GetThemes())
                        {
                            evt.menu.AppendAction("Sticky Note Theme/" + value,
                                menuAction => Dispatch(new UpdateStickyNoteThemeCommand(menuAction.userData as string, stickyNotes)),
                                GetThemeStatus, value);
                        }

                        foreach (var value in StickyNote.GetSizes())
                        {
                            evt.menu.AppendAction("Sticky Note Text Size/" + value,
                                menuAction => Dispatch(new UpdateStickyNoteTextSizeCommand(menuAction.userData as string, stickyNotes)),
                                GetSizeStatus, value);
                        }
                    }
                }
            }

            ViewSelection?.BuildContextualMenu(evt);

            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ =>
                {
                    using (var updater = GraphViewModel.GraphViewState.UpdateScope)
                    {
                        updater.ForceCompleteUpdate();
                    }
                });

                if (selection.Any())
                {
                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            using (var graphUpdater = GraphViewModel.GraphModelState.UpdateScope)
                            {
                                graphUpdater.MarkChanged(selection);
                            }
                        });
                }
            }
        }

        /// <summary>
        /// Shows the Item Library to add graph elements at the mouse position.
        /// </summary>
        /// <param name="position">The position where to show the Item Library</param>
        public virtual void ShowItemLibrary(Vector2 position)
        {
            var graphPosition = ContentViewContainer.WorldToLocal(position);
            var element = panel.Pick(position).GetFirstOfType<ModelView>();
            var stencil = (Stencil)GraphModel.Stencil;

            var current = element as VisualElement;
            while (current != null && current != this)
            {
                if (current is IShowItemLibraryUI_Internal dssUI)
                    if (dssUI.ShowItemLibrary(position))
                        return;

                current = current.parent;
            }

            ItemLibraryService.ShowGraphNodes(stencil, this, position, item =>
            {
                if (item is GraphNodeModelLibraryItem nodeItem)
                    Dispatch(CreateNodeCommand.OnGraph(nodeItem, graphPosition));
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
                m_GraphViewGraphLoadedObserver = new GraphViewStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphViewState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphViewGraphLoadedObserver);
            }

            if (m_GraphModelGraphLoadedAssetObserver == null)
            {
                m_GraphModelGraphLoadedAssetObserver = new GraphModelStateComponent.GraphAssetLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphModelGraphLoadedAssetObserver);
            }

            if (m_SelectionGraphLoadedObserver == null)
            {
                m_SelectionGraphLoadedObserver = new SelectionStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_SelectionGraphLoadedObserver);
            }

            if (m_ProcessingGraphLoadedObserver == null)
            {
                m_ProcessingGraphLoadedObserver = new GraphProcessingStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphProcessingState);
                GraphTool.ObserverManager.RegisterObserver(m_ProcessingGraphLoadedObserver);
            }

            if (m_ProcessingErrorsGraphLoadedObserver == null)
            {
                m_ProcessingErrorsGraphLoadedObserver = new GraphProcessingErrorsStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.ProcessingErrorsState);
                GraphTool.ObserverManager.RegisterObserver(m_ProcessingErrorsGraphLoadedObserver);
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
                    GraphViewModel.GraphViewState,
                    GraphViewModel.GraphModelState,
                    GraphViewModel.SelectionState,
                    GraphViewModel.AutoPlacementState,
                    GraphViewModel.ProcessingErrorsState,
                    GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }

            if (m_WireOrderObserver == null)
            {
                m_WireOrderObserver = new WireOrderObserver_Internal(GraphViewModel.SelectionState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_WireOrderObserver);
            }

            if (m_DeclarationHighlighter == null)
            {
                m_DeclarationHighlighter = new DeclarationHighlighter(GraphTool.ToolState, GraphViewModel.SelectionState, GraphTool.HighlighterState,
                    model => model is IHasDeclarationModel hasDeclarationModel ? hasDeclarationModel.DeclarationModel : null);
                GraphTool.ObserverManager.RegisterObserver(m_DeclarationHighlighter);
            }

            if (m_AutoPlacementObserver == null)
            {
                m_AutoPlacementObserver = new AutoPlacementObserver(this, GraphViewModel.AutoPlacementState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_AutoPlacementObserver);
            }

            if (m_AutomaticGraphProcessingObserver == null && DisplayMode == GraphViewDisplayMode.Interactive)
            {
                m_AutomaticGraphProcessingObserver = new AutomaticGraphProcessingObserver(
                    GraphViewModel.GraphModelState, ProcessOnIdleAgent.StateComponent,
                    GraphViewModel.GraphProcessingState, GraphTool.Preferences);
                GraphTool?.ObserverManager.RegisterObserver(m_AutomaticGraphProcessingObserver);
            }

            if (m_GraphProcessingErrorObserver == null)
            {
                m_GraphProcessingErrorObserver = new GraphProcessingErrorObserver(GraphViewModel.GraphModelState, GraphViewModel.GraphProcessingState, GraphViewModel.ProcessingErrorsState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphProcessingErrorObserver);
            }

            SelectionDragger?.RegisterObservers(GraphTool.ObserverManager);
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
        }

        /// <inheritdoc />
        protected override void UnregisterViewObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            SelectionDragger?.UnregisterObservers(GraphTool.ObserverManager);

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
        }

        internal void OnValidateCommand_Internal(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNames.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        internal void OnExecuteCommand_Internal(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNames.FrameSelected)
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
                    if (graphElement is Node || graphElement is Wire)
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

                foreach (var child in e.hierarchy.Children())
                {
                    IgnorePickingRecursive(child);
                }
            }

            graphElement.SetLevelOfDetail(ViewTransform.scale.x, m_ZoomMode, GraphViewZoomMode.Unknown);
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

            if (graphElement is Node || graphElement is Wire)
                graphElement.UnregisterCallback<MouseOverEvent>(OnMouseOver);

            graphElement.RemoveFromHierarchy();
            graphElement.RemoveFromRootView();
        }

        static readonly List<ChildView> k_CalculateRectToFitAllAllUIs = new();

        public Rect CalculateRectToFitAll()
        {
            Rect rectToFit = ContentViewContainer.layout;
            bool reachedFirstChild = false;

            GraphModel?.GraphElementModels.GetAllViewsInList_Internal(this, null, k_CalculateRectToFitAllAllUIs);
            foreach (var ge in k_CalculateRectToFitAllAllUIs)
            {
                if (ge is null || (ge is ModelView modelView && modelView.Model is WireModel))
                    continue;

                if (!reachedFirstChild)
                {
                    rectToFit = ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout);
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils_Internal.Encompass(rectToFit, ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout));
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
            PositionDependenciesManager_Internal.AddPositionDependency(model);
        }

        protected void RemovePositionDependency(WireModel wireModel)
        {
            PositionDependenciesManager_Internal.Remove(wireModel.FromNodeGuid, wireModel.ToNodeGuid);
            PositionDependenciesManager_Internal.LogDependencies();
        }

        protected void AddPortalDependency(WirePortalModel model)
        {
            PositionDependenciesManager_Internal.AddPortalDependency(model);
        }

        protected void RemovePortalDependency(WirePortalModel model)
        {
            PositionDependenciesManager_Internal.RemovePortalDependency(model);
            PositionDependenciesManager_Internal.LogDependencies();
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
        /// Callback for the ShortcutFramePreviousEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFramePreviousEvent(ShortcutFramePreviousEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFramePrevCommand(_ => true);
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutFrameNextEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameNextEvent(ShortcutFrameNextEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFrameNextCommand(_ => true);
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutDeleteEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutDeleteEvent(ShortcutDeleteEvent e)
        {
            var selectedNodes = GetSelection().OfType<AbstractNodeModel>().ToList();

            if (selectedNodes.Count == 0)
                return;

            var connectedNodes = selectedNodes
                .OfType<InputOutputPortsNodeModel>()
                .Where(x => x.InputsById.Values
                    .Any(y => y.IsConnected()) && x.OutputsById.Values.Any(y => y.IsConnected()))
                .ToList();

            var canSelectionBeBypassed = connectedNodes.Any();
            if (canSelectionBeBypassed)
                Dispatch(new BypassNodesCommand(connectedNodes, selectedNodes));
            else
                Dispatch(new DeleteElementsCommand(selectedNodes));

            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the <see cref="ShortcutShowItemLibraryEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutShowItemLibraryEvent(ShortcutShowItemLibraryEvent e)
        {
            ShowItemLibrary(e.MousePosition);
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutConvertConstantToVariableEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutConvertVariableAndConstantEvent(ShortcutConvertConstantAndVariableEvent e)
        {
            var constantModels = GetSelection().OfType<ConstantNodeModel>().ToList();
            var variableModels = GetSelection().OfType<VariableNodeModel>().ToList();

            if (constantModels.Any() || variableModels.Any())
            {
                Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constantModels, variableModels));
                e.StopPropagation();
            }
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
            var atPosition = new Rect(this.ChangeCoordinatesTo(ContentViewContainer, this.WorldToLocal(e.MousePosition)), StickyNote.defaultSize);
            Dispatch(new CreateStickyNoteCommand(atPosition));
            e.StopPropagation();
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
                    var renamableSelection = GetSelection().Where(x => x.IsRenamable());
                    var lastSelectedItem = renamableSelection.LastOrDefault();
                    var lastSelectedItemUI = lastSelectedItem?.GetView<GraphElement>(this);

                    lastSelectedItemUI?.OnRenameKeyDown_Internal(e);
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
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            Selection.activeObject = GraphViewModel.GraphModelState?.GraphModel.Asset;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManager_Internal.CancelMove();
            }
            else if (!m_SelectionDraggerWasActive && SelectionDragger.IsActive) // started
            {
                m_SelectionDraggerWasActive = true;

                var elemModel = GetSelection().OfType<AbstractNodeModel>().FirstOrDefault();
                var elem = elemModel?.GetView<GraphElement>(this);
                if (elem == null)
                    return;

                Vector2 elemPos = elemModel.Position;
                Vector2 startPos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                bool requireShiftToMoveDependencies = !(((Stencil)elemModel.GraphModel?.Stencil)?.MoveNodeDependenciesByDefault).GetValueOrDefault();
                bool hasShift = evt.modifiers.HasFlag(EventModifiers.Shift);
                bool moveNodeDependencies = requireShiftToMoveDependencies == hasShift;

                if (moveNodeDependencies)
                    PositionDependenciesManager_Internal.StartNotifyMove(GetSelection(), startPos);

                // schedule execute because the mouse won't be moving when the graph view is panning
                schedule.Execute(() =>
                {
                    if (SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.layout.position);
                        PositionDependenciesManager_Internal.ProcessMovedNodes(pos);
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
        public override void UpdateFromModel()
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

        protected virtual UpdateType DoUpdate(Observation graphModelObservation, Observation selectionObservation, Observation highlighterObservation)
        {
            var rebuildType = graphModelObservation.UpdateType.Combine(selectionObservation.UpdateType);

            if (rebuildType == UpdateType.Complete)
            {

                // Sad. We lose the focused element.
                Focus();

                BuildUI();
            }
            else if (rebuildType == UpdateType.Partial || highlighterObservation.UpdateType != UpdateType.None)
            {
                PartialUpdate(graphModelObservation, selectionObservation, highlighterObservation);
            }

            return rebuildType;
        }

        protected virtual void PartialUpdate(Observation graphModelObservation, Observation selectionObservation, Observation highlighterObservation)
        {
            var focusedElement = panel.focusController.focusedElement as VisualElement;
            while (focusedElement != null && !(focusedElement is ModelView))
            {
                focusedElement = focusedElement.parent;
            }

            var focusedModelView = focusedElement as ModelView;

            var modelChangeSet = GraphViewModel.GraphModelState.GetAggregatedChangeset(graphModelObservation.LastObservedVersion);
            var selectionChangeSet = GraphViewModel.SelectionState.GetAggregatedChangeset(selectionObservation.LastObservedVersion);

            if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
            {
                Debug.Log($"Partial GraphView Update {modelChangeSet?.NewModels.Count() ?? 0} new {modelChangeSet?.ChangedModels.Count() ?? 0} changed {modelChangeSet?.DeletedModels.Count() ?? 0} deleted");
            }

            var changedModels = new HashSet<Hash128>();
            var shouldUpdatePlacematContainer = false;
            var newPlacemats = new List<GraphElement>();
            if (modelChangeSet != null)
            {
                DeleteElementsFromChangeSet(modelChangeSet, focusedModelView);

                AddElementsFromChangeSet(modelChangeSet, newPlacemats);

                shouldUpdatePlacematContainer = newPlacemats.Any();

                //Update new and deleted node containers
                foreach (var modelGuid in modelChangeSet.NewModels.Concat(modelChangeSet.DeletedModels))
                {
                    if (GraphModel.TryGetModelFromGuid(modelGuid, out var model) &&
                        model.Container is GraphElementModel container)
                    {
                        changedModels.Add(container.Guid);
                    }
                }
            }

            if (modelChangeSet != null && selectionChangeSet != null)
            {
                var combinedSet = new HashSet<Hash128>(modelChangeSet.ChangedModels);
                combinedSet.UnionWith(selectionChangeSet.ChangedModels.Except(modelChangeSet.DeletedModels));
                changedModels.UnionWith(combinedSet);
            }
            else if (modelChangeSet != null)
            {
                changedModels.UnionWith(modelChangeSet.ChangedModels);
            }
            else if (selectionChangeSet != null)
            {
                changedModels.UnionWith(selectionChangeSet.ChangedModels);
            }

            if (highlighterObservation.UpdateType == UpdateType.Complete)
            {
                changedModels.UnionWith(GraphModel.NodeModels.OfType<IHasDeclarationModel>().OfType<AbstractNodeModel>().Select(m => m.Guid));
            }
            else if (highlighterObservation.UpdateType == UpdateType.Partial)
            {
                var changeset = GraphTool.HighlighterState.GetAggregatedChangeset(highlighterObservation.LastObservedVersion);
                changedModels.UnionWith(changeset.ChangedModels.SelectMany(guid =>
                {
                    var declarations = GraphModel.VariableDeclarations.Union(GraphModel.PortalDeclarations);
                    var variableDeclarationPlaceholders = GraphModel.Placeholders.OfType<VariableDeclarationModel>();
                    var declarationModel = declarations.Union(variableDeclarationPlaceholders).FirstOrDefault(d => d != null && d.Guid == guid);
                    return declarationModel != null ? GraphModel.FindReferencesInGraph(declarationModel) : Enumerable.Empty<IHasDeclarationModel>();
                }).OfType<AbstractNodeModel>().Select(m => m.Guid));
            }

            UpdateChangedModels(changedModels, shouldUpdatePlacematContainer, newPlacemats);

            // PF FIXME: node state (enable/disabled, used/unused) should be part of the State.
            if (GraphTool.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                PositionDependenciesManager_Internal.UpdateNodeState();

            HideAutoPlacedElements();

            var lastSelectedNode = GetSelection().OfType<AbstractNodeModel>().LastOrDefault();
            if (lastSelectedNode != null && lastSelectedNode.IsAscendable())
            {
                var nodeUI = lastSelectedNode.GetView<GraphElement>(this);
                nodeUI?.BringToFront();
            }

            var lastSelectedWire = GetSelection().OfType<WireModel>().LastOrDefault();
            if (lastSelectedWire != null && lastSelectedWire.IsAscendable())
            {
                var wireUI = lastSelectedWire.GetView<GraphElement>(this);
                wireUI?.BringToFront();
            }

            if (modelChangeSet?.RenamedModel != null)
            {
                var modelUis = new List<ChildView>();
                modelChangeSet.RenamedModel.GetAllViews(this, _ => true, modelUis);
                foreach (var ui in modelUis)
                {
                    if (ui is ModelView modelView)
                        modelView.ActivateRename();
                }
            }
        }

        void HideAutoPlacedElements()
        {
            // Models that need repositioning are hidden until they are moved by the observer next frame.
            using (var autoPlacementObservation = m_UpdateObserver.ObserveState(GraphViewModel.AutoPlacementState))
            {
                var autoPlacementChangeset = GraphViewModel.AutoPlacementState.GetAggregatedChangeset(autoPlacementObservation.LastObservedVersion);
                if (autoPlacementChangeset?.ModelsToRepositionAtCreation.Any() ?? false)
                {
                    foreach (var elementToHide in autoPlacementChangeset.ModelsToHideDuringAutoPlacement)
                    {
                        var modelUI = elementToHide.GetView_Internal(this);
                        if (modelUI != null)
                            modelUI.visible = false;
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

                guid.GetAllViews(this, null, k_UpdateAllUIs);
                foreach (var ui in k_UpdateAllUIs.OfType<GraphElement>())
                {
                    RemoveElement(ui);
                }

                k_UpdateAllUIs.Clear();

                // ToList is needed to bake the dependencies.
                foreach (var ui in guid.GetDependencies().ToList())
                {
                    ui.UpdateFromModel();
                }
            }
        }

        protected virtual void AddElementsFromChangeSet(GraphModelStateComponent.Changeset modelChangeSet, List<GraphElement> newPlacemats)
        {
            var newModels = modelChangeSet.NewModels.Select(GraphModel.GetModel).Where(m => m != null).ToList();

            foreach (var model in newModels.Where(m => !(m is WireModel) && !(m is PlacematModel) && !(m is DeclarationModel) && !(m is NodePreviewModel)))
            {
                if (model.Container != GraphModel)
                    continue;
                var ui = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (ui != null)
                    AddElement(ui);
            }

            foreach (var model in newModels.OfType<WireModel>())
            {
                CreateWireUI_Internal(model);
            }

            foreach (var model in newModels.OfType<PlacematModel>())
            {
                var placemat = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (placemat != null)
                {
                    newPlacemats.Add(placemat);
                    AddElement(placemat);
                }
            }

            foreach (var model in newModels.OfType<NodePreviewModel>())
            {
                var preview = ModelViewFactory.CreateUI<NodePreview>(this, model);
                if (preview != null)
                {
                    AddElement(preview);
                }
            }
        }

        protected virtual void UpdateChangedModels(IEnumerable<Hash128> changedModels, bool shouldUpdatePlacematContainer, List<GraphElement> placemats)
        {
            foreach (var guid in changedModels)
            {
                guid.GetAllViews(this, null, k_UpdateAllUIs);
                foreach (var ui in k_UpdateAllUIs)
                {
                    ui.UpdateFromModel();

                    if (ui.parent == PlacematContainer)
                        shouldUpdatePlacematContainer = true;
                }

                k_UpdateAllUIs.Clear();

                // ToList is needed to bake the dependencies.
                foreach (var ui in guid.GetDependencies().ToList())
                {
                    ui.UpdateFromModel();
                }
            }

            if (shouldUpdatePlacematContainer)
                PlacematContainer?.UpdateElementsOrder();

            foreach (var placemat in placemats)
            {
                placemat.UpdateFromModel();
            }
        }

        protected virtual void DoUpdateProcessingErrorMarkers(UpdateType rebuildType)
        {
            // Update processing error markers.
            using (var processingStateObservation = m_UpdateObserver.ObserveState(GraphViewModel.ProcessingErrorsState))
            {
                if (processingStateObservation.UpdateType != UpdateType.None || rebuildType == UpdateType.Partial)
                {
                    ConsoleWindowHelper_Internal.RemoveLogEntries();
                    var graphAsset = GraphViewModel.GraphModelState.GraphModel?.Asset;

                    foreach (var error in GraphViewModel.ProcessingErrorsState.Errors)
                    {
                        if (graphAsset is Object asset)
                        {
                            string graphAssetPath;
                            if (asset is GraphAsset serializedGraphAsset)
                            {
                                graphAssetPath = serializedGraphAsset.FilePath;
                            }
                            else
                            {
                                graphAssetPath = "<unknown>";
                            }
                            ConsoleWindowHelper_Internal.LogSticky(
                                $"{graphAssetPath}: {error.ErrorMessage}",
                                $"{graphAssetPath}@{error.ParentNodeGuid}",
                                error.ErrorType,
                                LogOption.None,
                                asset.GetInstanceID());
                        }
                    }

                    var markersToRemove = m_MarkersParent.Children().OfType<Marker>().Where(b => b.Model is GraphProcessingErrorModel).ToList();

                    foreach (var marker in markersToRemove)
                    {
                        RemoveElement(marker);

                        // ToList is needed to bake the dependencies.
                        foreach (var ui in marker.GraphElementModel.GetDependencies().ToList())
                        {
                            ui.UpdateFromModel();
                        }
                    }

                    foreach (var model in GraphViewModel.ProcessingErrorsState.Errors)
                    {
                        if (model.GetParentModel(GraphModel) == null)
                            return;

                        var marker = ModelViewFactory.CreateUI<Marker>(this, model);
                        if (marker != null)
                        {
                            AddElement(marker);
                        }
                    }
                }
            }
        }

        public override void BuildUI()
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
                        CreateWireUI_Internal(wirePlaceholder);
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
                if (!CreateWireUI_Internal(wire))
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
                placemat.UpdateFromModel();
            }

            HideAutoPlacedElements();

            UpdateViewTransform(GraphViewModel.GraphViewState.Position, GraphViewModel.GraphViewState.Scale);
        }

        internal bool CreateWireUI_Internal(WireModel wire)
        {
            if (wire == null)
                return false;

            if (wire.ToPort != null && wire.FromPort != null)
            {
                AddWireUI(wire);
                return true;
            }

            var (inputResult, outputResult) = wire.AddMissingPorts(out var inputNode, out var outputNode);

            if (inputResult == PortMigrationResult.MissingPortAdded && inputNode != null)
            {
                var inputNodeUi = inputNode.GetView_Internal(this);
                inputNodeUi?.UpdateFromModel();
            }

            if (outputResult == PortMigrationResult.MissingPortAdded && outputNode != null)
            {
                var outputNodeUi = outputNode.GetView_Internal(this);
                outputNodeUi?.UpdateFromModel();
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
            AddElement(wire);
            AddPositionDependency(wireModel);
        }

        bool CheckIntegrity(out string message)
        {
            message = "";

            if (GraphModel == null)
                return true;

            var invalidNodeCount = GraphModel.NodeModels.Count(n => n == null);
            var invalidWireCount = GraphModel.WireModels.Count(n => n == null);
            var invalidStickyCount = GraphModel.StickyNoteModels.Count(n => n == null);
            var invalidVariableCount = GraphModel.VariableDeclarations.Count(v => v == null);
            var invalidPlacematCount = GraphModel.PlacematModels.Count(p => p == null);
            var invalidPortalCount = GraphModel.PortalDeclarations.Count(p => p == null);
            var invalidSectionCount = GraphModel.SectionModels.Count(s => s == null);

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
        /// Populates the option menu.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        public virtual void BuildOptionMenu(GenericMenu menu)
        {
            var prefs = GraphTool?.Preferences;

            if (prefs != null)
            {
                if (Unsupported.IsDeveloperMode())
                {
                    if (CheckIntegrity(out var countMessage))
                        menu.AddItem(new GUIContent("Clean Up Graph"), false, () =>
                        {
                            if (EditorUtility.DisplayDialog("Invalid graph",
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

                    menu.AddItem(new GUIContent("Leave Item Library open on focus lost"),
                        prefs.GetBool(BoolPref.ItemLibraryStaysOpenOnBlur),
                        () =>
                        {
                            prefs.ToggleBool(BoolPref.ItemLibraryStaysOpenOnBlur);
                        });

                    menu.AddSeparator("");

                    bool graphLoaded = GraphTool?.ToolState.GraphModel != null;

                    // ReSharper disable once RedundantCast : needed in 2020.3.
                    menu.AddItem(new GUIContent("Reload Graph"), false, !graphLoaded ? (GenericMenu.MenuFunction)null : () =>
                    {
                        if (GraphTool?.ToolState.GraphModel != null)
                        {
                            var openedGraph = GraphTool.ToolState.CurrentGraph;
                            Resources.UnloadAsset(openedGraph.GetGraphAsset());
                            GraphTool?.Dispatch(new LoadGraphCommand(openedGraph.GetGraphModel()));
                        }
                    });

                    // ReSharper disable once RedundantCast : needed in 2020.3.
                    menu.AddItem(new GUIContent("Rebuild UI"), false, !graphLoaded ? (GenericMenu.MenuFunction)null : () =>
                    {
                        using (var updater = GraphViewModel.GraphModelState.UpdateScope)
                        {
                            updater.ForceCompleteUpdate();
                        }
                    });

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Evaluate Graph Only When Idle"),
                        prefs.GetBool(BoolPref.OnlyProcessWhenIdle), () =>
                        {
                            prefs.ToggleBool(BoolPref.OnlyProcessWhenIdle);
                        });
                }
            }
        }

        List<ChildView> k_OnFocus_GraphElementList = new();

        /// <inheritdoc />
        protected override void OnFocus(FocusInEvent e)
        {
            base.OnFocus(e);
            RefreshBorders();
        }

        /// <inheritdoc />
        protected override void OnLostFocus(FocusOutEvent e)
        {
            base.OnLostFocus(e);
            RefreshBorders();
        }

        void RefreshBorders()
        {
            if (GraphModel == null)
                return;

            k_OnFocus_GraphElementList.Clear();

            var views = GraphModel.GraphElementModels
                .GetAllViewsInList_Internal(this, e => e is GraphElement ge && ge.Border != null, k_OnFocus_GraphElementList);

            foreach (var element in k_OnFocus_GraphElementList.OfType<GraphElement>())
            {
                element.RefreshBorder();
            }
        }
    }
}
