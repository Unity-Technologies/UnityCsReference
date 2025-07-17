// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    abstract class OverlayDockArea : VisualElement
    {
        public abstract IEnumerable<OverlayDropZoneBase> GetDropZones();
    }

    sealed class DefaultModeDockArea : OverlayDockArea
    {
        // Used in tests.
        internal const string topToolbarDropZone = "DropZone-TopToolbar";
        internal const string bottomToolbarDropZone = "DropZone-BottomToolbar";
        internal const string leftToolbarDropZone = "DropZone-LeftToolbar";
        internal const string rightToolbarDropZone = "DropZone-RightToolbar";

        const string leftDynamicPanelDropZone = "DropZone-LeftDynamicPanel";
        const string rightDynamicPanelDropZone = "DropZone-RightDynamicPanel";

        // Used in tests.
        internal const string topLeftColumnDropZone = "DropZone-TopLeftColumn";
        internal const string bottomLeftColumnDropZone = "DropZone-BottomLeftColumn";
        internal const string topRightColumnDropZone = "DropZone-TopRightColumn";
        internal const string bottomRightColumnDropZone = "DropZone-BottomRightColumn";

        readonly ToolbarDropZone m_TopToolbar;
        readonly ToolbarDropZone m_BottomToolbar;
        readonly ToolbarDropZone m_LeftToolbar;
        readonly ToolbarDropZone m_RightToolbar;

        readonly DynamicPanelDropZone m_LeftDynamicPanel;
        readonly DynamicPanelDropZone m_RightDynamicPanel;

        readonly OverlayContainerDropZone m_TopLeftAnchored;
        readonly OverlayContainerDropZone m_BottomLeftAnchored;
        readonly OverlayContainerDropZone m_TopRightAnchored;
        readonly OverlayContainerDropZone m_BottomRightAnchored;

        readonly VisualElement m_VerticalToolbarContainer;
        readonly VisualElement m_HorizontalToolbarContainer;
        readonly VisualElement m_AnchoredContainer;
        readonly VisualElement m_DynamicPanelContainer;

        public DefaultModeDockArea(OverlayCanvas canvas, VisualElement horizontalParent, VisualElement verticalParent, VisualElement anchoredParent)
        {
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore;
            m_AnchoredContainer = new VisualElement { name = "AnchoredDockArea" };
            SetupContainer(m_AnchoredContainer, anchoredParent);
            Add(m_AnchoredContainer);

            m_VerticalToolbarContainer = new VisualElement { name = "VerticalToolbarDockArea" };
            Add(m_VerticalToolbarContainer);
            SetupContainer(m_VerticalToolbarContainer, verticalParent);

            m_HorizontalToolbarContainer = new VisualElement { name = "HorizontalToolbarDockArea" };
            Add(m_HorizontalToolbarContainer);
            SetupContainer(m_HorizontalToolbarContainer, horizontalParent);

            m_DynamicPanelContainer = new VisualElement { name = "DynamicPanelDockArea" };
            Add(m_DynamicPanelContainer);
            SetupContainer(m_DynamicPanelContainer, verticalParent);

            m_HorizontalToolbarContainer.Add(m_TopToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.TopToolbar), true) { name = topToolbarDropZone });
            m_HorizontalToolbarContainer.Add(m_BottomToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.BottomToolbar), true) { name = bottomToolbarDropZone });
            m_VerticalToolbarContainer.Add(m_LeftToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.LeftToolbar), false) { name = leftToolbarDropZone });
            m_VerticalToolbarContainer.Add(m_RightToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.RightToolbar), false) { name = rightToolbarDropZone });

            m_DynamicPanelContainer.Add(m_LeftDynamicPanel = new DynamicPanelDropZone(canvas.GetDockZoneContainer(DockZone.LeftDynamicPanel)) { name = leftDynamicPanelDropZone });
            m_DynamicPanelContainer.Add(m_RightDynamicPanel = new DynamicPanelDropZone(canvas.GetDockZoneContainer(DockZone.RightDynamicPanel)) { name = rightDynamicPanelDropZone });

            m_AnchoredContainer.Add(m_TopLeftAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_LeftToolbar, m_LeftDynamicPanel) { name = topLeftColumnDropZone });
            m_AnchoredContainer.Add(m_BottomLeftAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_LeftToolbar, m_LeftDynamicPanel) { name = bottomLeftColumnDropZone });
            m_AnchoredContainer.Add(m_TopRightAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_RightToolbar, m_RightDynamicPanel) { name = topRightColumnDropZone });
            m_AnchoredContainer.Add(m_BottomRightAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_RightToolbar, m_RightDynamicPanel) { name = bottomRightColumnDropZone });
        }

        void SetupContainer(VisualElement container, VisualElement parent)
        {
            container.style.position = Position.Absolute;
            container.pickingMode = PickingMode.Ignore;
            parent.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                var worldPos = parent.parent.LocalToWorld(evt.newRect.position);
                container.style.translate = this.WorldToLocal(worldPos);
                container.style.width = evt.newRect.width;
                container.style.height = evt.newRect.height;
            });
        }

        public override IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            yield return m_TopToolbar;
            yield return m_BottomToolbar;
            yield return m_LeftToolbar;
            yield return m_RightToolbar;
            yield return m_LeftDynamicPanel;
            yield return m_RightDynamicPanel;
            yield return m_TopLeftAnchored;
            yield return m_BottomLeftAnchored;
            yield return m_TopRightAnchored;
            yield return m_BottomRightAnchored;
        }
    }
}
