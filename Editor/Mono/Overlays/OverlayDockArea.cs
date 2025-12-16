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

        // Used in tests.
        internal const string leftDynamicPanelDropZone = "DropZone-LeftDynamicPanel";
        internal const string rightDynamicPanelDropZone = "DropZone-RightDynamicPanel";

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

        readonly VisualElement m_DockAreasContainer;
        readonly VisualElement m_SceneViewContainer;
        readonly VisualElement m_LeftAnchoredContainer;
        readonly VisualElement m_RightAnchoredContainer;


        public DefaultModeDockArea(OverlayCanvas canvas, VisualElement root)
        {
            // Set up main container.
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore;
            var container = root.Q<VisualElement>("overlay-container-group--horizontal");
            var sceneContainer = root.Q<VisualElement>("displaced-panel--container");
            SetupContainer(this, sceneContainer);

            // Dock Areas container.
            m_DockAreasContainer = new VisualElement { name = "DockAreasContainer" };
            Add(m_DockAreasContainer);
            m_DockAreasContainer.Add(m_TopToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.TopToolbar), true) { name = topToolbarDropZone });
            m_DockAreasContainer.pickingMode = PickingMode.Ignore;

            // Scene view container (holds left/right columns).
            m_SceneViewContainer = new VisualElement { name = "SceneViewContainer" };
            m_DockAreasContainer.Add(m_SceneViewContainer);
            m_SceneViewContainer.pickingMode = PickingMode.Ignore;

            // Set up left column.
            var dynamicPanelSpacerLeft = new VisualElement() { name = "DynamicPanelSpacerLeft" };
            var toolbarLeft = container.Q<VisualElement>("overlay-toolbar__left");
            var dynamicPanelLeft = root.Q<VisualElement>("overlay-dynamic-panel--left");
            m_LeftAnchoredContainer = new VisualElement { name = "LeftAnchoredDockArea" };
            m_LeftAnchoredContainer.pickingMode = PickingMode.Ignore;
            m_SceneViewContainer.Add(m_LeftToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.LeftToolbar), false) { name = leftToolbarDropZone });
            m_SceneViewContainer.Add(dynamicPanelSpacerLeft);
            m_SceneViewContainer.Add(m_LeftAnchoredContainer);
            SetupSpacer(dynamicPanelSpacerLeft, dynamicPanelLeft, toolbarLeft);

            // Set up right column.
            var dynamicPanelSpacerRight = new VisualElement() { name = "DynamicPanelSpacerRight" };
            var toolbarRight = container.Q<VisualElement>("overlay-toolbar__right");
            var dynamicPanelRight = root.Q<VisualElement>("overlay-dynamic-panel--right");
            m_RightAnchoredContainer = new VisualElement { name = "RightAnchoredDockArea" };
            m_RightAnchoredContainer.pickingMode = PickingMode.Ignore;
            m_SceneViewContainer.Add(m_RightAnchoredContainer);
            m_SceneViewContainer.Add(dynamicPanelSpacerRight);
            m_SceneViewContainer.Add(m_RightToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.RightToolbar), false) { name = rightToolbarDropZone });
            SetupSpacer(dynamicPanelSpacerRight, dynamicPanelRight, toolbarRight);

            // Bottom toolbar drop zone.
            m_DockAreasContainer.Add(m_BottomToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.BottomToolbar), true) { name = bottomToolbarDropZone });

            // Anchored corner drop zones
            // Left: top, dynamic panel, bottom
            m_LeftAnchoredContainer.Add(m_TopLeftAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_LeftToolbar, m_LeftDynamicPanel) { name = topLeftColumnDropZone });
            m_LeftAnchoredContainer.Add(m_LeftDynamicPanel = new DynamicPanelDropZone(canvas.GetDockZoneContainer(DockZone.LeftDynamicPanel)) { name = leftDynamicPanelDropZone });
            m_LeftAnchoredContainer.Add(m_BottomLeftAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_LeftToolbar, m_LeftDynamicPanel) { name = bottomLeftColumnDropZone });

            // Right: top, dynamic panel, bottom
            m_RightAnchoredContainer.Add(m_TopRightAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_RightToolbar, m_RightDynamicPanel) { name = topRightColumnDropZone });
            m_RightAnchoredContainer.Add(m_RightDynamicPanel = new DynamicPanelDropZone(canvas.GetDockZoneContainer(DockZone.RightDynamicPanel)) { name = rightDynamicPanelDropZone });
            m_RightAnchoredContainer.Add(m_BottomRightAnchored = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_RightToolbar, m_RightDynamicPanel) { name = bottomRightColumnDropZone });
        }

        void SetupContainer(VisualElement container, VisualElement parent)
        {
            container.style.position = Position.Absolute;
            container.pickingMode = PickingMode.Ignore;
            parent.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                var worldPos = parent.parent.LocalToWorld(evt.newRect.position);
                container.style.translate = container.parent.WorldToLocal(worldPos);
                container.style.width = evt.newRect.width;
                container.style.height = evt.newRect.height;
            });
        }

        void SetupSpacer(VisualElement spacer, VisualElement panel, VisualElement toolbar)
        {
            spacer.pickingMode = PickingMode.Ignore;
            panel.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                spacer.style.width = evt.newRect.width + toolbar.resolvedStyle.width;
                spacer.style.height = evt.newRect.height;
            });
            toolbar.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                spacer.style.width = panel.resolvedStyle.width + evt.newRect.width;
                spacer.style.height = panel.resolvedStyle.height;
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
