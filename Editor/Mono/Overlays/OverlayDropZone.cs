// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Overlays
{
    class OverlayDropZone : OverlayDropZoneBase
    {
        public enum Placement
        {
            Before,
            After
        }

        readonly Overlay m_TargetOverlay;
        readonly Placement m_Placement;
        OverlayContainer m_TargetContainer;
        OverlayContainerSection m_TargetSection;
        Overlay m_DraggedOverlay;

        public override OverlayContainer targetContainer => m_TargetOverlay.container;
        public override OverlayContainerSection targetSection => m_TargetSection;

        public OverlayDropZone(Overlay target, Placement placement)
        {
            m_TargetOverlay = target;
            m_Placement = placement;
            style.flexGrow = 1;
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return !m_TargetOverlay.floating && m_TargetOverlay != draggedOverlay;
        }

        public override bool CanAcceptTarget(Overlay overlay)
        {
            return m_TargetOverlay != overlay;
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);

            m_DraggedOverlay = draggedOverlay;
            m_TargetContainer = m_TargetOverlay.container;
            m_TargetContainer.GetOverlayIndex(m_TargetOverlay, out m_TargetSection, out var _);
            SetHidden(true);
        }

        public override void BeginHover()
        {
            base.BeginHover();

            var parent = m_TargetOverlay.rootVisualElement.parent;
            var index = parent.IndexOf(m_TargetOverlay.rootVisualElement);

            bool dockAfter = ShouldDockAfter(m_TargetSection);
            if (dockAfter)
                ++index;

            parent.Insert(index, insertIndicator);
            insertIndicator.Setup(m_TargetOverlay.container.isHorizontal,
                (dockAfter && m_TargetOverlay.container.GetLastVisible(m_TargetSection) == m_TargetOverlay)
                || (!dockAfter && m_TargetOverlay.container.GetFirstVisible(m_TargetSection) == m_TargetOverlay));

            // When adding an overlay in one of the 2 columns we use the current width of the overlay as preview
            if (m_TargetContainer is not ToolbarOverlayContainer)
                insertIndicator.style.width = m_DraggedOverlay.rootVisualElement.layout.width;
        }

        public override void EndHover()
        {
            base.EndHover();

            insertIndicator.RemoveFromHierarchy();
        }

        public override void DropOverlay(Overlay overlay)
        {
            
            m_TargetOverlay.container.GetOverlayIndex(m_TargetOverlay, out var section, out _);
            if (ShouldDockAfter(section))
            {
                overlay.DockAfter(m_TargetOverlay);
            }
            else
            {
                overlay.DockBefore(m_TargetOverlay);
            }

            overlay.floating = false;
        }

        bool ShouldDockAfter(OverlayContainerSection targetSection)
        {
            // The drop zone before the element should place after the next overlay when after the spacer.
            // Overlay after the spacer are listed from bottom to spacer instead of spacer to bottom.
            return targetSection == OverlayContainerSection.BeforeSpacer && m_Placement == Placement.After
                || targetSection == OverlayContainerSection.AfterSpacer && m_Placement == Placement.Before;
        }
    }
}
