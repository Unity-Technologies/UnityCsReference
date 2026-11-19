// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An element that displays the preview of a node.
    /// </summary>
    [UnityRestricted]
    internal class NodePreview : Marker
    {
        static readonly CustomStyleProperty<Color> k_BackgroundColorProperty = new CustomStyleProperty<Color>("--background-color");
        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new CustomStyleProperty<Color>("--border-color");

        const string k_HidePreviewTooltip = "Hide Node preview";
        const string k_PreviewRenderingStr = "Node preview is rendering.";
        const string k_PreviewRenderFailureStr = "Node preview failed to render.";
        public const string k_CloseButtonName = "close-button";

        /// <summary>
        /// The USS class name added to a <see cref="NodePreview"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-node-preview";

        /// <summary>
        /// The USS class name added to the preview container.
        /// </summary>
        public static readonly string previewContainerUssClassName = ussClassName.WithUssElement("preview-container");

        /// <summary>
        /// The USS class name added to the preview.
        /// </summary>
        public static readonly string previewUssClassName = ussClassName.WithUssElement("preview");

        /// <summary>
        /// The USS class name added to elements that have a <see cref="NodePreview"/> attached to them.
        /// </summary>
        public static readonly string hasPreviewUssClassName = "ge-has-node-preview";

        /// <summary>
        /// The USS class name added to the container showing the processing status of the preview.
        /// </summary>
        public static readonly string statusContainerUssClassName = ussClassName.WithUssElement("status-container");

        /// <summary>
        /// The USS class name added to the loading icon.
        /// </summary>
        public static readonly string loadingIconUssClassName = statusContainerUssClassName.WithUssElement("loading-icon");

        /// <summary>
        /// The USS class name added to the failure icon.
        /// </summary>
        public static readonly string failureIconUssClassName = statusContainerUssClassName.WithUssElement("failure-icon");

        /// <summary>
        /// The USS class name added to the close button of the preview.
        /// </summary>
        public static readonly string closePreviewButton = ussClassName.WithUssElement("close-button");

        /// <summary>
        /// The USS class name added to the close button icon.
        /// </summary>
        public static readonly string closeButtonIconUssClassName = closePreviewButton.WithUssElement(GraphElementHelper.iconName);

        static readonly string k_CloseButtonIconHoveredUssClassName = closeButtonIconUssClassName.WithUssElement("hovered");

        Color m_BackgroundColor;
        Color m_BorderColor;

        Image m_CloseButtonIcon;
        Image m_FailureIcon;
        SpinningLoadingIcon m_LoadingIcon;

        protected VisualElement m_PreviewElement;
        protected VisualElement m_PreviewContainer;
        protected VisualElement m_StatusContainer;

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
            m_Distance = new Vector2(0, 12);
        }

        /// <summary>
        /// Shows the close button on the preview.
        /// </summary>
        /// <param name="show">Whether the close preview button should be visible or not.</param>
        public void ShowClosePreviewButtonElement(bool show)
        {
            m_CloseButtonIcon.EnableInClassList(visibleUssClassName, show);
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (NodePreviewModel.ShowNodePreview)
                UpdateButtonsLOD();
        }

        void UpdateButtonsLOD()
        {
            const int minSize = 12;
            const int normalSize = 16;
            var scale = 1.0f / GraphView.Zoom;

            if (GraphView.Zoom > GraphView.MediumZoom)
            {
                m_CloseButtonIcon.style.width = normalSize;
                m_CloseButtonIcon.style.height = normalSize;
            }
            else
            {
                m_CloseButtonIcon.style.width = minSize * scale;
                m_CloseButtonIcon.style.height = minSize * scale;
            }
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
        protected override void BuildUI()
        {
            base.BuildUI();
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
            m_LoadingIcon.AddToClassList(hiddenUssClassName);
            m_StatusContainer.Add(m_LoadingIcon);

            m_FailureIcon = new Image { name = "failure-icon" };
            m_FailureIcon.AddToClassList(failureIconUssClassName);
            m_FailureIcon.AddToClassList(hiddenUssClassName);
            m_StatusContainer.Add(m_FailureIcon);

            // Close button
            m_CloseButtonIcon = new Image { name = k_CloseButtonName, tooltip = k_HidePreviewTooltip };
            m_PreviewContainer.Add(m_CloseButtonIcon);
            m_CloseButtonIcon.AddToClassList(closePreviewButton);
            m_CloseButtonIcon.AddToClassList(closeButtonIconUssClassName);
            m_CloseButtonIcon.RegisterCallback<MouseDownEvent>(OnClickCloseButton);

            EnableInClassList(hiddenUssClassName, !NodePreviewModel.ShowNodePreview);
            SetNodePreviewSize(NodePreviewModel.Size);
            SetPreviewTooltip(NodePreviewModel.PreviewStatus);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerLeaveEvent>(OnMouseLeave);
            RegisterCallback<PointerEnterEvent>(OnMouseEnter);
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

            schedule.Execute(() => SetElementLevelOfDetail(GraphView.Zoom, GraphViewZoomMode.Unknown, GraphViewZoomMode.Unknown)).ExecuteLater(0);

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
            this.AddPackageStylesheet("NodePreview.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                if (m_LastShowPreview != NodePreviewModel.ShowNodePreview)
                {
                    EnableInClassList(hiddenUssClassName, !NodePreviewModel.ShowNodePreview);

                    if (NodePreviewModel.ShowNodePreview)
                    {
                        UpdateButtonsLOD();
                    }
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
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                if (m_LastModelSize != NodePreviewModel.Size)
                    SetNodePreviewSize(NodePreviewModel.Size);
            }
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
            if (m_Target is not NodeView node)
                return;

            const int tipHeight = 8;
            var previewRect = m_PreviewContainer.localBound;
            previewRect.position = Vector2.zero;

            var togglePreviewButton = node.GetNodeToolbarButton(ShowPreviewButton.previewButtonName);
            if (togglePreviewButton is not null)
            {
                var previewButtonPos = togglePreviewButton.parent.ChangeCoordinatesTo(node, togglePreviewButton.layout);
                var tipRect = new Rect(previewRect.width - (node.layout.width - previewButtonPos.xMin),
                    previewRect.yMax, togglePreviewButton.layout.width, tipHeight);

                mgc.painter2D.fillColor = m_BackgroundColor;
                mgc.painter2D.strokeColor = m_BorderColor;
                DrawNodePreview(mgc.painter2D, previewRect, tipRect);
                mgc.painter2D.Fill();
                mgc.painter2D.Stroke();
            }
        }

        void UpdatePreviewFromStatus(NodePreviewStatus status)
        {
            m_LoadingIcon.EnableInClassList(hiddenUssClassName, status != NodePreviewStatus.Processing);
            m_FailureIcon.EnableInClassList(hiddenUssClassName, status != NodePreviewStatus.Failure);
            m_PreviewElement.EnableInClassList(hiddenUssClassName, status != NodePreviewStatus.Updated);

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

            m_LastModelSize = new Vector2(size.x, size.y);
            m_PreviewContainer.style.height = size.y;
            m_PreviewContainer.style.width = size.x;
        }

        void OnClickCloseButton(MouseDownEvent e)
        {
            GraphView.Dispatch(new ShowNodePreviewCommand(false, NodePreviewModel.NodeModel));
            e.StopImmediatePropagation();
        }

        void OnMouseEnter(PointerEnterEvent _)
        {
            OnHover(true);
        }

        void OnMouseLeave(PointerLeaveEvent _)
        {
            OnHover(false);
        }

        void OnHover(bool isHovering)
        {
            if (m_Target is not NodeView node)
                return;

            node.ShowNodeToolbarButtons(isHovering);
            node.Border.Hovered = isHovering;

            if (isHovering)
            {
                m_CloseButtonIcon.AddToClassList(k_CloseButtonIconHoveredUssClassName);
            }
            else
            {
                m_CloseButtonIcon.RemoveFromClassList(k_CloseButtonIconHoveredUssClassName);
            }
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
