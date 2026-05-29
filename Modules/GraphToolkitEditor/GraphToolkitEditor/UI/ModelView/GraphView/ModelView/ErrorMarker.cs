// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A marker that displays an error message.
    /// </summary>
    [UnityRestricted]
    internal class ErrorMarker : Marker
    {
        /// <summary>
        /// The USS class name added to <see cref="ErrorMarker"/>'s.
        /// </summary>
        public new static readonly string ussClassName = "ge-error-marker";

        /// <summary>
        /// The USS class name added to the icon of the marker.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class name added to the text element of the marker.
        /// </summary>
        public static readonly string textUssClassName = ussClassName.WithUssElement(GraphElementHelper.textAreaName);

        /// <summary>
        /// The USS class name added to elements that have an <see cref="ErrorMarker"/> attached to them.
        /// </summary>
        public static readonly string hasErrorUssClassName = "ge-has-error-marker";

        /// <summary>
        /// The USS class name added to the marker element.
        /// </summary>
        public static readonly string markerElementUssClassName = ussClassName.WithUssElement("marker-element");

        /// <summary>
        /// The USS class name added to the marker's counter element.
        /// </summary>
        public static readonly string counterElementUssClassName = ussClassName.WithUssElement("counter-element");

        static readonly string k_DefaultStylePath = "ErrorMarker.uss";

        static CustomStyleProperty<float> s_TextSizeProperty = new("--error-text-size");
        static CustomStyleProperty<float> s_TextMaxWidthProperty = new("--error-text-max-width");
        static readonly CustomStyleProperty<float> k_IconWidthProperty = new("--error-icon-width");
        static readonly CustomStyleProperty<float> k_IconHeightProperty = new("--error-icon-height");
        static readonly CustomStyleProperty<Color> k_BackgroundColorProperty = new("--background-color");
        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new("--border-color");

        /// <summary>
        /// The model of the marker.
        /// </summary>
        public MarkerModel MarkerModel => Model as MarkerModel;

        /// <inheritdoc />
        public override GraphElementModel ParentModel => (Model as MarkerModel)?.GetParentModel(GraphView.GraphModel);

        protected Image m_TipElement;
        protected Image m_IconElement;
        protected Label m_TextElement;

        protected Color m_BackgroundColor;
        protected Color m_BorderColor;

        protected VisualElement m_MarkerElement;
        protected Label m_CounterElement;
        protected string m_MarkerType;

        float m_LastZoom;
        bool m_ErrorMarkerVisualContentAlreadyDrawn;

        float ErrorTextSize { get; set; } = 12;
        float ErrorTextMaxWidth { get; set; } = 240;

        float CounterTextSize { get; set; } = 9;

        Vector2 IconSize { get; set; } = new Vector2(12, 12);

        /// <summary>
        /// The style of the marker depending on the type of the marker (eg.: an error).
        /// </summary>
        protected string VisualStyle
        {
            set
            {
                if (m_MarkerType != value)
                {
                    RemoveFromClassList(ussClassName.WithUssModifier(m_MarkerType));

                    m_MarkerType = value;

                    AddToClassList(ussClassName.WithUssModifier(m_MarkerType));
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            m_Target.CaptureMouse();

            evt.menu.ClearItems();
            if (MarkerModel is MultipleGraphProcessingErrorsModel multipleErrorsModel)
            {
                var graphErrors = new Dictionary<Hash128, List<GraphProcessingErrorModel>>();
                foreach (var errorModel in multipleErrorsModel.Errors)
                {
                    var sourceGraphGuid = errorModel.SourceGraphReference.GraphModelGuid;
                    if (graphErrors.TryGetValue(sourceGraphGuid, out var errorModels))
                        errorModels.Add(errorModel);
                    else
                        graphErrors.Add(sourceGraphGuid, new List<GraphProcessingErrorModel> { errorModel });
                }

                var shouldAddSeparator = true;
                var currentGraph = GraphView.GraphModel;

                // Section 1 of contextual menu: Errors on the current element.
                if (graphErrors.TryGetValue(currentGraph.Guid, out var errorsOnGraph))
                {
                    for (var i = 0; i < errorsOnGraph.Count; i++)
                    {
                        var error = errorsOnGraph[i];
                        var actionName = $"{error.GetEntryPrefix()}: {FormatErrorItemName(error.ErrorMessage)}";
                        string prefix = $"{actionName}/";
                        if (!error.TryAppendAction(evt.menu, RootView, prefix))
                        {
                            evt.menu.AppendAction(actionName, _ => ShowConsoleWindow());
                        }
                    }

                    if (graphErrors.Count == 1)
                    {
                        // There is no section 2, no need for a separator.
                        shouldAddSeparator = false;
                    }
                }
                else
                {
                    // There is no section 1, no need for a separator.
                    shouldAddSeparator = false;
                }

                if (shouldAddSeparator)
                    evt.menu.AppendSeparator();

                // Section 2 of contextual menu: Errors on lower levels of sub graphs.
                if (graphErrors.Keys.Count > 0)
                {
                    var window = GraphView.Window as GraphViewEditorWindow;
                    foreach (var sourceGraphGuid in graphErrors.Keys)
                    {
                        if (currentGraph.Guid == sourceGraphGuid)
                            continue;

                        var subgraphErrors = graphErrors[sourceGraphGuid];
                        var errorCount = subgraphErrors.Count;
                        var sStr = errorCount > 1 ? "s" : "";

                        for (var i = 0; i < subgraphErrors.Count; i++)
                        {
                            var subgraphError = subgraphErrors[i];

                            // The error navigates to another graph.
                            var actionName =
                                $"Subgraph {GraphView.GraphModel.ResolveGraphModelFromReference(subgraphError.SourceGraphReference)?.Name ?? string.Empty} has ({errorCount}) error{sStr}/{i + 1}. " +
                                FormatErrorItemName(subgraphError.ErrorMessage);
                            evt.menu.AppendAction(actionName, _ => LoadGraphAndFrameElement(subgraphError, window));
                        }
                    }
                }
            }
            else if (MarkerModel is ErrorMarkerModel errorMarkerModel)
            {
                errorMarkerModel.TryAppendAction(evt.menu, RootView);
            }

            m_Target.ReleaseMouse();
            evt.StopImmediatePropagation();
        }

        static string FormatErrorItemName(string errorMessage)
        {
            const int maxCharCount = 100;

            var formattedMessage = errorMessage;
            if (formattedMessage.Length > maxCharCount)
                formattedMessage = formattedMessage[..maxCharCount] + "...";

            return formattedMessage.Replace('/', '\\');
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            var changed = false;
            if (e.customStyle.TryGetValue(s_TextSizeProperty, out var textSize))
            {
                ErrorTextSize = textSize;
                CounterTextSize = textSize * 0.75f;
                changed = true;
            }

            if (e.customStyle.TryGetValue(s_TextMaxWidthProperty, out var maxWidth))
            {
                ErrorTextMaxWidth = maxWidth;
                changed = true;
            }

            if (e.customStyle.TryGetValue(k_IconWidthProperty, out float iconWidth) && e.customStyle.TryGetValue(k_IconHeightProperty, out float iconHeight))
            {
                IconSize = new Vector2(iconWidth, iconHeight);
                changed = true;
            }

            if (e.customStyle.TryGetValue(k_BackgroundColorProperty, out var backgroundColorValue))
                m_BackgroundColor = backgroundColorValue;

            if (e.customStyle.TryGetValue(k_BorderColorProperty, out var borderColorValue))
                m_BorderColor = borderColorValue;


            if (changed && m_LastZoom != 0.0f)
                // recompute text scale next frame when the localBound is known.
                schedule.Execute(() => SetElementLevelOfDetail(m_LastZoom, GraphViewZoomMode.Unknown, GraphViewZoomMode.Unknown)).ExecuteLater(0);
        }

        void LoadGraphAndFrameElement(GraphProcessingErrorModel error, GraphViewEditorWindow window)
        {
            var targetGraph = error.SourceGraphReference;
            if (targetGraph == default || window is null)
                return;

            if (error.Context is { Count: > 0 })
            {
                // Load each graph leading to the target graph in the breadcrumbs.
                var alreadyVisited = new HashSet<Hash128> { window.GraphView.GraphModel.Guid };
                for (var i = 0; i < error.Context.Count; i++)
                {
                    var contextModel = error.Context[i];
                    if (contextModel.GraphModel.Guid != targetGraph.GraphModelGuid && alreadyVisited.Add(contextModel.GraphModel.Guid))
                    {
                        var title = contextModel is SubgraphNodeModel subgraphNode ? subgraphNode.Title : string.Empty;
                        window.GraphTool.Dispatch(new LoadGraphCommand(contextModel.GraphModel, LoadGraphCommand.LoadStrategies.PushOnStack, title: title));
                    }
                }
            }

            GraphViewEditorWindow.FrameGraphElement(error.ParentModelGuid, window, GraphView.GraphModel.ResolveGraphModelFromReference(targetGraph));
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            // If there is more than 1 issue, clicking on the marker should open a menu. Else, it should lead to the unity console.
            if (MarkerModel is MultipleGraphProcessingErrorsModel errorsModel)
            {
                if (ShouldOpenConsoleWindow(errorsModel.Errors))
                {
                    var window = GraphView.Window as GraphViewEditorWindow;
                    var errorGraphAsset = errorsModel.Errors[0].SourceGraphReference;
                    var clickable = errorGraphAsset.GraphModelGuid != GraphView.GraphModel.Guid
                        ? new Clickable(() => LoadGraphAndFrameElement(errorsModel.Errors[0], window))
                        : new Clickable(ShowConsoleWindow);
                    clickable.activators.Clear();
                    clickable.activators.Add(
                        new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 1 });
                    this.AddManipulator(clickable);
                }
                else if (errorsModel.Errors.Count != 0)
                {
                    ContextualMenuManipulator = new ErrorMarkerContextualMenuManipulator(BuildContextualMenu);
                }
            }

            base.BuildUI();
            name = "error-marker";

            m_MarkerElement = new VisualElement();
            m_MarkerElement.AddToClassList(markerElementUssClassName);
            Add(m_MarkerElement);

            m_IconElement = new Image { name = GraphElementHelper.iconName };
            m_IconElement.AddToClassList(iconUssClassName);
            m_MarkerElement.Add(m_IconElement);

            m_CounterElement = new Label { name = "issues-counter" };
            m_CounterElement.AddToClassList(counterElementUssClassName);
            m_CounterElement.AddToClassList(hiddenUssClassName);
            m_MarkerElement.Add(m_CounterElement);

            m_TextElement = new Label { name = GraphElementHelper.textAreaName };
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.EnableInClassList(hiddenUssClassName, true);
            Add(m_TextElement);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            return;

            bool ShouldOpenConsoleWindow(List<GraphProcessingErrorModel> errors)
            {
                if (errors.Count == 1)
                {
                    var error = errors[0];
                    if (error.Action == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        protected override void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            HideText();
            base.OnDetachedFromPanel(evt);
        }

        /// <inheritdoc />
        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            base.OnGeometryChanged(evt);

            if (!m_ErrorMarkerVisualContentAlreadyDrawn && Attacher != null)
            {
                m_MarkerElement.generateVisualContent = OnGenerateErrorMarkerVisualContent;
                m_ErrorMarkerVisualContentAlreadyDrawn = true;
            }
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddPackageStylesheet(k_DefaultStylePath);

            //we need to add the style sheet to the Text element as well since it will be parented elsewhere
            m_TextElement.AddPackageStylesheet(k_DefaultStylePath);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (Model is ErrorMarkerModel errorMarkerModel && visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                if (m_TextElement != null)
                {
                    m_TextElement.text = errorMarkerModel.ErrorMessage;
                }

                if (Model is MultipleGraphProcessingErrorsModel multipleErrorModel && m_CounterElement != null)
                {
                    var hasMoreThanOneError = multipleErrorModel.Errors.Count > 1;
                    if (hasMoreThanOneError)
                        m_CounterElement.text = multipleErrorModel.Errors.Count.ToString();
                    m_CounterElement.EnableInClassList(hiddenUssClassName, !hasMoreThanOneError);
                }

                VisualStyle = errorMarkerModel.ErrorType.ToString().ToLower();
            }
        }

        /// <inheritdoc />
        protected override void Attach()
        {
            if (ParentModel is null)
                return;

            // The marker should be aligned to the target using the marker as a reference, not including the text element.
            m_ReferenceElement = m_MarkerElement;

            switch (ParentModel)
            {
                case ContextNodeModel contextNodeModel:
                    var contextNodeUI = contextNodeModel.GetView(RootView);
                    if (contextNodeUI != null)
                        AttachTo(contextNodeUI, SpriteAlignment.TopRight);
                    break;
                case WireModel wireModel:
                    if (wireModel.GetView(RootView) is GraphElement wireUI)
                    {
                        if (wireModel is TransitionSupportModel transModel && transModel.IsSingleStateTransition)
                        {
                            m_Offset = new Vector2(0, -1);
                            AttachTo(wireUI.SizeElement, SpriteAlignment.TopCenter);
                        }
                        else
                        {
                            m_Offset = new Vector2(0, -20);
                            AttachTo(wireUI.SizeElement, SpriteAlignment.Center);
                        }
                    }
                    break;
                case PortModel portModel:
                    m_Distance = Vector2.zero;
                    var portUI = portModel.GetView(RootView);
                    if (portUI != null)
                    {
                        if (portModel.Orientation == PortOrientation.Horizontal)
                        {
                            AttachTo(portUI,
                                portModel.Direction == PortDirection.Input
                                ? SpriteAlignment.LeftCenter
                                : SpriteAlignment.RightCenter);
                        }
                        else
                        {
                            AttachTo(portUI,
                                portModel.Direction == PortDirection.Input
                                ? SpriteAlignment.TopCenter
                                : SpriteAlignment.BottomCenter);
                        }
                    }
                    break;

                default:
                    var visualElement = ParentModel.GetView(RootView);
                    if (visualElement != null)
                        AttachTo(visualElement, SpriteAlignment.RightCenter);
                    break;
            }

            m_Target?.AddToClassList(hasErrorUssClassName);

            // If the error marker is aligned on the left center, the text should appear on the left of the marker.
            if (Alignment == SpriteAlignment.LeftCenter)
            {
                m_TextElement.SendToBack();
            }
        }

        /// <inheritdoc />
        protected override void Detach()
        {
            m_Target?.RemoveFromClassList(hasErrorUssClassName);
            base.Detach();
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicErrorMarkerBorder(this);
        }

        /// <summary>
        /// Displays the text of the marker.
        /// </summary>
        protected void ShowText()
        {
            if (m_TextElement?.hierarchy.parent != null && m_TextElement.ClassListContains(hiddenUssClassName))
                m_TextElement?.EnableInClassList(hiddenUssClassName, false);
        }

        /// <summary>
        /// Hides the text of the marker.
        /// </summary>
        protected void HideText()
        {
            if (m_TextElement?.hierarchy.parent != null && !m_TextElement.ClassListContains(hiddenUssClassName))
                m_TextElement.EnableInClassList(hiddenUssClassName, true);
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            //we make sure we sit on top of whatever siblings we have
            BringToFront();
            ShowText();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            HideText();
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            var scale = 1.0f / zoom;

            m_CounterElement.style.fontSize = Math.Max(CounterTextSize * scale, CounterTextSize);

            m_IconElement.style.width = Math.Max(IconSize.x * scale, IconSize.x);
            m_IconElement.style.height = Math.Max(IconSize.y * scale, IconSize.y);

            m_TextElement.style.fontSize = ErrorTextSize * scale;
            m_TextElement.style.maxWidth = ErrorTextMaxWidth * scale;

            m_LastZoom = zoom;
        }

        void OnGenerateErrorMarkerVisualContent(MeshGenerationContext mgc)
        {
            DrawErrorMarker(mgc.painter2D);
            mgc.painter2D.fillColor = m_BackgroundColor;
            mgc.painter2D.strokeColor = m_BorderColor;
            mgc.painter2D.Fill();
            mgc.painter2D.Stroke();
        }

        internal void DrawErrorMarker(Painter2D p2d, bool isBorder = false)
        {
            //           +-----+
            // tip rect <      |
            //           +-----+
            //         marker rect

            const int tipHeight = 4;
            const int tipWidth = tipHeight * 2;
            var markerRect = m_MarkerElement.localBound;
            if (!isBorder)
                markerRect.position = Vector2.zero;

            Rect tipRect;
            switch (Alignment)
            {
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.TopCenter:
                case SpriteAlignment.TopRight:
                case SpriteAlignment.Center:
                    tipRect = new Rect(markerRect.center.x - tipWidth * 0.5f, markerRect.yMax, tipWidth, tipHeight);
                    break;
                case SpriteAlignment.LeftCenter:
                    tipRect = new Rect(markerRect.xMax, markerRect.center.y - tipHeight, tipHeight, tipWidth);
                    break;
                case SpriteAlignment.BottomLeft:
                case SpriteAlignment.BottomCenter:
                case SpriteAlignment.BottomRight:
                    tipRect = new Rect(markerRect.center.x - tipWidth * 0.5f, markerRect.yMin - tipHeight, tipWidth, tipHeight);
                    break;
                default:
                    tipRect = new Rect(markerRect.xMin - tipHeight, markerRect.center.y - tipHeight, tipHeight, tipWidth);
                    break;
            }
            DrawErrorMarker(p2d, markerRect, tipRect, Alignment);
        }

        static void DrawErrorMarker(Painter2D p2d, Rect rect, Rect tip, SpriteAlignment alignment)
        {
            const float rectRadius = 2f;

            p2d.BeginPath();

            // To bottom left corner
            p2d.MoveTo(new Vector2(rect.xMin, rect.yMax - rectRadius));

            // Align to right -> Draw the tip on the left of marker:
            if (alignment is SpriteAlignment.RightCenter)
            {
                p2d.LineTo(new Vector2(tip.xMax, tip.yMax));
                p2d.LineTo(new Vector2(tip.xMin, tip.center.y));
                p2d.LineTo(new Vector2(tip.xMax, tip.yMin));
            }

            // To top left corner
            p2d.ArcTo(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin + rectRadius, rect.yMin), rectRadius);

            // Align to bottom -> Draw the tip on top of marker:
            if (alignment is SpriteAlignment.BottomCenter or SpriteAlignment.BottomLeft or SpriteAlignment.BottomRight)
            {
                p2d.LineTo(new Vector2(tip.xMin, tip.yMax));
                p2d.LineTo(new Vector2(tip.center.x, tip.yMin));
                p2d.LineTo(new Vector2(tip.xMax, tip.yMax));
            }

            // To top right corner
            p2d.ArcTo(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMin + rectRadius), rectRadius);

            // Align to left -> Draw the tip on right of marker:
            if (alignment is SpriteAlignment.LeftCenter)
            {
                p2d.LineTo(new Vector2(tip.xMin, tip.yMin));
                p2d.LineTo(new Vector2(tip.xMax, tip.center.y));
                p2d.LineTo(new Vector2(tip.xMin, tip.yMax));
            }

            // To bottom right corner
            p2d.ArcTo(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax - rectRadius, rect.yMax), rectRadius);

            // Align to top -> Draw the tip on bottom of marker:
            if (alignment is SpriteAlignment.TopCenter or SpriteAlignment.TopLeft or SpriteAlignment.TopRight or SpriteAlignment.Center)
            {
                p2d.LineTo(new Vector2(tip.xMax, rect.yMax));
                p2d.LineTo(new Vector2(tip.center.x, tip.yMax));
                p2d.LineTo(new Vector2(tip.xMin, tip.yMin));
            }

            // To bottom left corner
            p2d.ArcTo(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMax - rectRadius), rectRadius);

            p2d.ClosePath();
        }

        static void ShowConsoleWindow()
        {
            ConsoleWindowHelper.ShowConsoleWindow();
        }
    }
}
