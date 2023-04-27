// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An element that displays the preview of a node.
    /// </summary>
    class NodePreview : Marker
    {
        static readonly CustomStyleProperty<Color> k_BackgroundColorProperty = new CustomStyleProperty<Color>("--background-color");
        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new CustomStyleProperty<Color>("--border-color");
        static readonly Vector2 k_DefaultDistanceValue = new Vector2(0, 12);

        const string k_HidePreviewTooltip = "Hide Node preview";
        const string k_PreviewRenderingStr = "Node preview is rendering.";
        const string k_PreviewRenderFailureStr = "Node preview failed to render.";

        public new static readonly string ussClassName = "ge-node-preview";
        public static readonly string previewContainerUssClassName = ussClassName.WithUssElement("preview-container");
        public static readonly string previewUssClassName = ussClassName.WithUssElement("preview");
        public static readonly string hasPreviewUssClassName = "ge-has-node-preview";

        public static readonly string statusContainerUssClassName = ussClassName.WithUssElement("status-container");
        public static readonly string loadingIconUssClassName = statusContainerUssClassName.WithUssElement("loading-icon");
        public static readonly string failureIconUssClassName = statusContainerUssClassName.WithUssElement("failure-icon");

        public static readonly string closePreviewButton = ussClassName.WithUssElement("close-button");
        public static readonly string showHideButtonUssClassName = closePreviewButton.WithUssModifier("visible");
        public static readonly string iconElementUssClassName = closePreviewButton.WithUssElement("icon");
        public static readonly string hiddenUssModifier = "hidden";

        Color m_BackgroundColor;
        Color m_BorderColor;

        Image m_CloseButtonIcon;
        Image m_FailureIcon;
        SpinningLoadingIcon m_LoadingIcon;

        protected VisualElement m_PreviewElement;
        protected VisualElement m_PreviewContainer;
        protected VisualElement m_StatusContainer;

        float m_LastZoom;
        bool m_LastShowPreview;
        Vector2 m_LastModelSize;
        bool m_PreviewVisualContentAlreadyDrawn;

        /// <summary>
        /// The <see cref="NodePreviewModel"/> this <see cref="NodePreview"/> displays.
        /// </summary>
        public NodePreviewModel NodePreviewModel => Model as NodePreviewModel;

        /// <inheritdoc />
        public override GraphElementModel ParentModel => NodePreviewModel.NodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodePreview"/> class.
        /// </summary>
        public NodePreview()
        {
            m_Distance = k_DefaultDistanceValue;
        }

        /// <summary>
        /// Shows the close button on the preview.
        /// </summary>
        /// <param name="show">Whether the close preview button should be visible or not.</param>
        public void ShowClosePreviewButtonElement(bool show)
        {
            m_CloseButtonIcon.EnableInClassList(showHideButtonUssClassName, show);
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            const int minSize = 12;
            const int normalSize = 16;
            var scale = 1.0f / zoom;

            switch (newZoomMode)
            {
                case GraphViewZoomMode.Normal:
                    m_CloseButtonIcon.style.width = normalSize;
                    m_CloseButtonIcon.style.height = normalSize;
                    break;
                case GraphViewZoomMode.Medium:
                case GraphViewZoomMode.Small:
                case GraphViewZoomMode.VerySmall:
                    m_CloseButtonIcon.style.width = minSize * scale;
                    m_CloseButtonIcon.style.height = minSize * scale;
                    break;
            }

            m_LastZoom = zoom;
        }

        /// <inheritdoc />
        protected override void Attach()
        {
            var node = NodePreviewModel?.NodeModel?.GetView<GraphElement>(RootView);
            if (node != null)
                AttachTo(node, SpriteAlignment.TopRight);
            m_Target?.AddToClassList(hasPreviewUssClassName);
        }

        /// <inheritdoc />
        protected override void Detach()
        {
            base.Detach();
            m_Target?.RemoveFromClassList(hasPreviewUssClassName);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            name = "node-preview";

            // Container
            m_PreviewContainer = new VisualElement { name = "preview-container" };
            Add(m_PreviewContainer);
            m_PreviewContainer.AddToClassList(previewContainerUssClassName);

            // Preview
            m_PreviewElement = new VisualElement { name = "preview" };
            m_PreviewContainer.Add(m_PreviewElement);
            m_PreviewElement.AddToClassList(previewUssClassName);

            // Preview loading status
            m_StatusContainer = new VisualElement { name = "status-container" };
            m_StatusContainer.AddToClassList(statusContainerUssClassName);
            m_PreviewContainer.Add(m_StatusContainer);

            m_LoadingIcon = new SpinningLoadingIcon { name = "spinning-loading-icon" };
            m_LoadingIcon.AddToClassList(loadingIconUssClassName);
            m_LoadingIcon.AddToClassList(hiddenUssModifier);
            m_StatusContainer.Add(m_LoadingIcon);

            m_FailureIcon = new Image { name = "failure-icon" };
            m_FailureIcon.AddToClassList(failureIconUssClassName);
            m_FailureIcon.AddToClassList(hiddenUssModifier);
            m_StatusContainer.Add(m_FailureIcon);

            // Close button
            m_CloseButtonIcon = new Image { name = "close-button", tooltip = k_HidePreviewTooltip };
            m_PreviewContainer.Add(m_CloseButtonIcon);
            m_CloseButtonIcon.AddToClassList(closePreviewButton);
            m_CloseButtonIcon.AddToClassList(iconElementUssClassName);
            m_CloseButtonIcon.RegisterCallback<MouseDownEvent>(OnClickCloseButton);

            EnableInClassList(hiddenUssModifier, !NodePreviewModel.ShowNodePreview);
            SetNodePreviewSize(NodePreviewModel.Size);
            SetPreviewTooltip(NodePreviewModel.PreviewStatus);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        }

        /// <inheritdoc />
        protected override DynamicBorder CreateDynamicBorder()
        {
            // We do not want any selection or hover border on the preview.
            return null;
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            if (m_LastZoom != 0.0f)
                schedule.Execute(() => SetElementLevelOfDetail(m_LastZoom, GraphViewZoomMode.Unknown, GraphViewZoomMode.Unknown)).ExecuteLater(0);

            if (e.customStyle.TryGetValue(k_BackgroundColorProperty, out var backgroundColorValue))
                m_BackgroundColor = backgroundColorValue;
            if (e.customStyle.TryGetValue(k_BorderColorProperty, out var borderColorValue))
                m_BorderColor = borderColorValue;
        }

        /// <inheritdoc />
        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            base.OnGeometryChanged(evt);
            if (!m_PreviewVisualContentAlreadyDrawn && Attacher != null)
            {
                m_PreviewContainer.generateVisualContent = OnGenerateNodePreviewVisualContent;
                m_PreviewVisualContentAlreadyDrawn = true;
            }
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("NodePreview.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (m_LastShowPreview != NodePreviewModel.ShowNodePreview)
            {
                EnableInClassList(hiddenUssModifier, !NodePreviewModel.ShowNodePreview);
                m_LastShowPreview = NodePreviewModel.ShowNodePreview;
            }

            switch (NodePreviewModel.PreviewStatus)
            {
                case NodePreviewStatus.Updated:
                    OnPreviewUpdated();
                    break;
                case NodePreviewStatus.Processing:
                    OnPreviewProcessing();
                    break;
                case NodePreviewStatus.Failure:
                    OnPreviewFailure();
                    break;
            }

            if (m_LastModelSize != NodePreviewModel.Size)
                SetNodePreviewSize(NodePreviewModel.Size);
        }

        /// <summary>
        /// Performs UI tasks that need to be done when the preview has been updated.
        /// </summary>
        protected virtual void OnPreviewUpdated()
        {
            UpdatePreviewFromStatus(NodePreviewStatus.Updated);
        }

        /// <summary>
        /// Performs UI tasks that need to be done when the preview is processing.
        /// </summary>
        protected virtual void OnPreviewProcessing()
        {
            UpdatePreviewFromStatus(NodePreviewStatus.Processing);
        }

        /// <summary>
        /// Performs UI tasks that need to be done when the preview has failed to be processed.
        /// </summary>
        protected virtual void OnPreviewFailure()
        {
            UpdatePreviewFromStatus(NodePreviewStatus.Failure);
        }

        void OnGenerateNodePreviewVisualContent(MeshGenerationContext mgc)
        {
            if (m_Target is not Node node)
                return;

            const int tipHeight = 8;
            var previewRect = m_PreviewContainer.localBound;
            previewRect.position = Vector2.zero;

            var togglePreviewButton = node.SafeQ(IconTitleProgressPart.previewButtonPartName);
            var tipRect = new Rect(previewRect.width - (node.layout.width - togglePreviewButton.layout.xMin),
                previewRect.yMax, togglePreviewButton.layout.width, tipHeight);

            mgc.painter2D.fillColor = m_BackgroundColor;
            mgc.painter2D.strokeColor = m_BorderColor;
            DrawNodePreview(mgc.painter2D, previewRect, tipRect);
            mgc.painter2D.Fill();
            mgc.painter2D.Stroke();
        }

        void UpdatePreviewFromStatus(NodePreviewStatus status)
        {
            m_LoadingIcon.EnableInClassList(hiddenUssModifier, status != NodePreviewStatus.Processing);
            m_FailureIcon.EnableInClassList(hiddenUssModifier, status != NodePreviewStatus.Failure);
            m_PreviewElement.EnableInClassList(hiddenUssModifier, status != NodePreviewStatus.Updated);

            SetPreviewTooltip(status);
        }

        void SetPreviewTooltip(NodePreviewStatus status)
        {
            m_StatusContainer.tooltip = status switch
            {
                NodePreviewStatus.Updated => "",
                NodePreviewStatus.Processing => k_PreviewRenderingStr,
                NodePreviewStatus.Failure => k_PreviewRenderFailureStr,
                _ => m_StatusContainer.tooltip
            };
        }

        void SetNodePreviewSize(Vector2 size)
        {
            if (PositionIsOverriddenByManipulator)
                return;

            GraphView.SelectionDragger.SetSelectionDirty();
            m_LastModelSize = new Vector2(size.x, size.y);
            m_PreviewContainer.style.height = size.y;
            m_PreviewContainer.style.width = size.x;
        }

        void OnClickCloseButton(MouseDownEvent e)
        {
            GraphView.Dispatch(new ShowNodePreviewCommand(false, NodePreviewModel.NodeModel));
            e.StopImmediatePropagation();
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            OnHover(true);
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            OnHover(false);
        }

        void OnHover(bool isHovering)
        {
            if (m_Target is not Node node)
                return;

            node.ShowButtons(isHovering);
            node.Border.Hovered = isHovering;
        }

        static void DrawNodePreview(Painter2D p2d, Rect previewRect, Rect tipRect)
        {
            const int previewRadius = 3;
            const float tipRadius = 1.5f;

            p2d.BeginPath();
            p2d.MoveTo(new Vector2(previewRect.xMin, previewRect.yMax - previewRadius));
            p2d.ArcTo(new Vector2(previewRect.xMin, previewRect.yMin), new Vector2(previewRect.xMin + previewRadius, previewRect.yMin), previewRadius);
            p2d.ArcTo(new Vector2(previewRect.xMax, previewRect.yMin), new Vector2(previewRect.xMax, previewRect.yMin + previewRadius), previewRadius);
            p2d.ArcTo(new Vector2(previewRect.xMax, previewRect.yMax), new Vector2(previewRect.xMax - previewRadius, previewRect.yMax), previewRadius);

            // Draw the tip
            p2d.LineTo(new Vector2(tipRect.xMax, previewRect.yMax));
            p2d.ArcTo(new Vector2(tipRect.center.x, tipRect.yMax), new Vector2(tipRect.center.x - tipRadius, tipRect.yMax - tipRadius), tipRadius);
            p2d.LineTo(new Vector2(tipRect.xMin, tipRect.yMin));

            p2d.ArcTo(new Vector2(previewRect.xMin, previewRect.yMax), new Vector2(previewRect.xMin, previewRect.yMax - previewRadius), previewRadius);
            p2d.ClosePath();
        }
    }
}
