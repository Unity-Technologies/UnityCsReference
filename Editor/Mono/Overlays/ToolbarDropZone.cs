// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Overlays
{
    sealed class ToolbarDropZone : OverlayContainerDropZone
    {
        public ToolbarDropZone(OverlayContainer container) : base(container, Placement.Start) { }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return targetContainer != originContainer && !targetContainer.HasVisibleOverlays();
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);

            SetHidden(true);
        }

        public override void UpdateHover(OverlayDropZoneBase hovered)
        {
            base.UpdateHover(hovered);

            SetHidden(hovered != this);
        }
    }
}
