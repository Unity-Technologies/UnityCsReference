// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class DynamicPanelDropZone : OverlayContainerDropZone
    {
        public override OverlayContainer targetContainer => m_CurrentContainer;

        OverlayContainer m_CurrentContainer;

        public DynamicPanelDropZone(OverlayContainer container) : base(null, Placement.Start)
        {
            m_CurrentContainer = container;
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);

            SetHidden(true);
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            var draggedOverlayIsInPanelMode = draggedOverlay.activeLayout == Layout.Panel;

            return !m_CurrentContainer.HasVisibleOverlays() && draggedOverlayIsInPanelMode;
        }

        public override void BeginHover()
        {
            m_CurrentContainer.Insert(0, insertIndicator);

            insertIndicator.Setup(true, OverlayInsertIndicator.InsertIndicatorStyle.DynamicPanel, true);
            insertIndicator.style.height = new Length(100, LengthUnit.Percent);
        }

        public override void EndHover()
        {
            insertIndicator.RemoveFromHierarchy();
        }

        public override void UpdateHover(OverlayDropZoneBase hovered)
        {
            SetHidden(true);
        }
    }
}
