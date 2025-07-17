// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Overlays
{
    sealed class OverlayContainerInsertDropZone : OverlayContainerDropZone
    {
        OverlayContainerSection m_TargetSection;

        public OverlayContainerInsertDropZone(OverlayContainer container, OverlayContainerSection section, Placement placement) : base(container, placement)
        {
            m_TargetSection = section;
            style.flexGrow = 1;
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);

            SetHidden(true);
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return targetContainer.GetContainerSection(GetSection()).GetLastVisible() != draggedOverlay;
        }

        public override void UpdateHover(OverlayDropZoneBase hovered) {}

        public override void BeginHover()
        {
            base.BeginHover();

            if (placement == Placement.Start)
                targetContainer.GetContainerSection(GetSection()).Insert(0, insertIndicator);
            else
                targetContainer.GetContainerSection(GetSection()).Add(insertIndicator);

            var insertIndicatorStyle = targetContainer is ToolbarOverlayContainer || targetContainer is DynamicPanelOverlayContainer
                ? OverlayInsertIndicator.InsertIndicatorStyle.Toolbar
                : OverlayInsertIndicator.InsertIndicatorStyle.Normal;

            insertIndicator.Setup(targetContainer.isHorizontal, insertIndicatorStyle, true); //Horizontal container has vertical insert indicators
        }

        public override void EndHover()
        {
            base.EndHover();

            insertIndicator.RemoveFromHierarchy();
        }

        public override void DropOverlay(Overlay overlay)
        {
            if (placement == Placement.Start)
                overlay.DockAt(targetContainer, GetSection(), 0);
            else
                overlay.DockAt(targetContainer, GetSection());
        }

        protected override OverlayContainerSection GetSection()
        {
            return m_TargetSection;
        }
    }
}
