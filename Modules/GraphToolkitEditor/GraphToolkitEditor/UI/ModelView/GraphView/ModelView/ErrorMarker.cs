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
    class ErrorMarker : Marker
    {
        const string k_IssuesCounterName = "issues-counter";
        const string k_ElementName = "error-marker";

        const string k_ErrorMarkerClassName = "ge-error-marker";
        static readonly string k_IconClassName = k_ErrorMarkerClassName.WithUssElement(GraphElementHelper.iconName);
        static readonly string k_TextClassName = k_ErrorMarkerClassName.WithUssElement(GraphElementHelper.textAreaName);
        const string k_HasErrorClassName = "ge-has-error-marker";
        static readonly string k_MarkerElementClassName = k_ErrorMarkerClassName.WithUssElement("marker-element");
        static readonly string k_CounterClassName = k_ErrorMarkerClassName.WithUssElement("counter-element");

        const string k_DefaultStylePath = "ErrorMarker.uss";

        static readonly CustomStyleProperty<float> s_TextSizeProperty = new("--error-text-size");
        static readonly CustomStyleProperty<float> s_TextMaxWidthProperty = new("--error-text-max-width");
        static readonly CustomStyleProperty<float> k_IconWidthProperty = new("--error-icon-width");
        static readonly CustomStyleProperty<float> k_IconHeightProperty = new("--error-icon-height");
        static readonly CustomStyleProperty<Color> k_BackgroundColorProperty = new("--background-color");
        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new("--border-color");

        public MarkerModel MarkerModel => Model as MarkerModel;

        /// <inheritdoc />
        public override GraphElementModel ParentModel => (Model as MarkerModel)?.GetParentModel(GraphView.GraphModel);

        protected Image m_TipElement;
        Image m_IconElement;
        protected Label m_TextElement;

        Color m_BackgroundColor;
        Color m_BorderColor;

        VisualElement m_MarkerElement;
        Label m_CounterElement;
        string m_MarkerType;

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
                    RemoveFromClassList(k_ErrorMarkerClassName.WithUssModifier(m_MarkerType));

                    m_MarkerType = value;

                    AddToClassList(k_ErrorMarkerClassName.WithUssModifier(m_MarkerType));
                }
            }
        }

        void OnClick()
        {
            if (MarkerModel == null || GraphView == null)
                return;

            if (MarkerModel is ErrorMarkerModel errorMarkerModel)
            {
                // Single subgraph error: navigate directly instead of opening the popup
                if (errorMarkerModel is GraphProcessingErrorModel gpError &&
                    gpError.SourceGraphReference != default &&
                    gpError.SourceGraphReference.GraphModelGuid != GraphView?.GraphModel.Guid)
                {
                    ErrorMarkerUtilities.LoadGraphAndFrameElement(gpError, GraphView);
                    return;
                }
            }

            List<ErrorMarkerModel> currentGraphErrors = new();
            Dictionary<GraphReference, List<ErrorMarkerModel>> subgraphErrorsDict = new();

            ErrorMarkerUtilities.PopulateDetailedGraphErrors(MarkerModel,
                GraphView,
                currentGraphErrors,
                subgraphErrorsDict);

            if (currentGraphErrors.Count == 0 && subgraphErrorsDict.Count == 0)
                return;

            if (currentGraphErrors.Count == 0 && subgraphErrorsDict.Count == 1)
            {
                GraphProcessingErrorModel singleSubgraphError = null;
                foreach (var v in subgraphErrorsDict.Values)
                {
                    foreach (var subGraphErrorModel in v)
                    {
                        if (subGraphErrorModel is GraphProcessingErrorModel graphProcessingErrorModel)
                        {
                            if (singleSubgraphError != null)
                            {
                                // if we've found a second GraphProcessingErrorModel then we should open the popup
                                singleSubgraphError = null;
                                break;
                            }

                            singleSubgraphError = graphProcessingErrorModel;
                        }
                        else
                        {
                            singleSubgraphError = null;
                            break;
                        }
                    }
                    break;
                }

                if (singleSubgraphError != null)
                {
                    ErrorMarkerUtilities.LoadGraphAndFrameElement(singleSubgraphError, GraphView);
                    return;
                }
            }

            var subgraphErrorGroups = ErrorMarkerUtilities.CreateSubgraphErrorGroups(subgraphErrorsDict);

            // Sort errors by severity
            currentGraphErrors.Sort((a, b) =>
                ErrorMarkerUtilities.GetSeverityPriority(a.ErrorType)
                    .CompareTo(ErrorMarkerUtilities.GetSeverityPriority(b.ErrorType)));

            new ErrorMarkerPopupWindow().Show(worldBound, currentGraphErrors, subgraphErrorGroups, GraphView);
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

        /// <inheritdoc />
        protected override void BuildUI()
        {
            var clickable = new Clickable(OnClick);
            clickable.activators.Clear();
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 1 });
            this.AddManipulator(clickable);

            base.BuildUI();
            name = k_ElementName;

            m_MarkerElement = new VisualElement();
            m_MarkerElement.AddToClassList(k_MarkerElementClassName);
            Add(m_MarkerElement);

            m_IconElement = new Image { name = GraphElementHelper.iconName };
            m_IconElement.AddToClassList(k_IconClassName);
            m_MarkerElement.Add(m_IconElement);

            m_CounterElement = new Label { name = k_IssuesCounterName };
            m_CounterElement.AddToClassList(k_CounterClassName);
            m_CounterElement.AddToClassList(hiddenUssClassName);
            m_MarkerElement.Add(m_CounterElement);

            m_TextElement = new Label { name = GraphElementHelper.textAreaName };
            m_TextElement.AddToClassList(k_TextClassName);
            m_TextElement.EnableInClassList(hiddenUssClassName, true);
            Add(m_TextElement);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
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

            AddToClassList(k_ErrorMarkerClassName);
            AddStyleSheetPath(k_DefaultStylePath);

            //we need to add the style sheet to the Text element as well since it will be parented elsewhere
            m_TextElement.AddStyleSheetPath(k_DefaultStylePath);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (Model is ErrorMarkerModel errorMarkerModel && visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                UpdateTextElement();

                if (Model is MultipleGraphProcessingErrorsModel multipleErrorModel && m_CounterElement != null)
                {
                    var hasMoreThanOneError = multipleErrorModel.Errors.Count > 1;
                    if (hasMoreThanOneError)
                        m_CounterElement.text = multipleErrorModel.Errors.Count.ToString();
                    m_CounterElement.EnableInClassList(hiddenUssClassName, !hasMoreThanOneError);
                }
                else
                {
                    m_CounterElement?.EnableInClassList(hiddenUssClassName, true);
                }

                VisualStyle = errorMarkerModel.ErrorType.ToString().ToLower();
            }
        }

        void UpdateTextElement()
        {
            if (m_TextElement == null)
                return;

            if (MarkerModel is ErrorMarkerModel errorMarkerModel)
            {
                // If only 1 error and its on a subgraph, text should summarize this
                if (errorMarkerModel is GraphProcessingErrorModel gpError &&
                    gpError.SourceGraphReference != default &&
                    gpError.SourceGraphReference.GraphModelGuid != GraphView?.GraphModel.Guid)
                {
                    m_TextElement.text = ErrorMarkerUtilities.GetSubgraphMessageString(
                        ErrorMarkerUtilities.GetGraphPath(gpError.SourceGraphReference),
                        1,
                        ErrorMarkerUtilities.GetLogString(errorMarkerModel.ErrorType));
                    return;
                }

                if (errorMarkerModel is MultipleGraphProcessingErrorsModel { Errors: [var singleError] } &&
                    singleError.SourceGraphReference != default &&
                    singleError.SourceGraphReference.GraphModelGuid != GraphView?.GraphModel.Guid)
                {
                    m_TextElement.text = ErrorMarkerUtilities.GetSubgraphMessageString(
                        ErrorMarkerUtilities.GetGraphPath(singleError.SourceGraphReference),
                        1,
                        ErrorMarkerUtilities.GetLogString(singleError.ErrorType));
                    return;
                }

                m_TextElement.text = errorMarkerModel.ErrorMessage;
            }
            else
                m_TextElement.text = string.Empty;
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

            m_Target?.AddToClassList(k_HasErrorClassName);

            // If the error marker is aligned on the left center, the text should appear on the left of the marker.
            if (Alignment == SpriteAlignment.LeftCenter)
            {
                m_TextElement.SendToBack();
            }
        }

        /// <inheritdoc />
        protected override void Detach()
        {
            m_Target?.RemoveFromClassList(k_HasErrorClassName);
            base.Detach();
        }

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return new DynamicErrorMarkerBorder(this);
        }

        protected void ShowText()
        {
            if (m_TextElement?.hierarchy.parent != null && m_TextElement.ClassListContains(hiddenUssClassName))
                m_TextElement?.EnableInClassList(hiddenUssClassName, false);
        }

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
    }
}
