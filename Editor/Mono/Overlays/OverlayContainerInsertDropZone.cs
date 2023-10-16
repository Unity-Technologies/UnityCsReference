// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Overlays
{
    sealed class OverlayContainerInsertDropZone : OverlayContainerDropZone
    {
        public OverlayContainerInsertDropZone(OverlayContainer container, Placement placement) : base(container, placement)
        {
            style.flexGrow = 1;
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);

            SetHidden(true);
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return targetContainer.GetLastVisible(GetSection()) != draggedOverlay;
        }

        public override void UpdateHover(OverlayDropZoneBase hovered) {}

        public override void BeginHover()
        {
            base.BeginHover();

            targetContainer.GetSectionElement(GetSection()).Add(insertIndicator);
            insertIndicator.Setup(targetContainer.isHorizontal, true);
        }

        public override void EndHover()
        {
            base.EndHover();

            insertIndicator.RemoveFromHierarchy();
        }

        public override void DropOverlay(Overlay overlay)
        {
            overlay.DockAt(targetContainer, GetSection());
        }
    }
}
