// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class OverlayContainerDropZone : OverlayDropZoneBase
    {
        public enum Placement
        {
            Start,
            End
        }

        readonly Placement m_Placement;
        readonly OverlayContainer m_Container;
        readonly OverlayDropZoneBase[] m_HideIfHovered;

        public override OverlayContainer targetContainer => m_Container;
        public override OverlayContainerSection targetSection => GetSection();
        protected Placement placement => m_Placement;

        public OverlayContainerDropZone(OverlayContainer container, Placement placement, params OverlayDropZoneBase[] hideIfHovered)
        {
            m_Placement = placement;
            m_Container = container;
            m_HideIfHovered = hideIfHovered;
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);
            style.display = ShouldEnable(draggedOverlay) ? DisplayStyle.Flex : DisplayStyle.None;
            SetHidden(false);
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return !m_Container.GetContainerSection(GetSection()).HasVisibleOverlays()
                && !m_Container.ContainsOverlay(draggedOverlay, GetSection());
        }

        protected virtual OverlayContainerSection GetSection()
        {
            switch (placement)
            {
                case Placement.Start: return OverlayContainerSection.BeforeSpacer;
                case Placement.End: return OverlayContainerSection.AfterSpacer;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public override void DropOverlay(Overlay overlay)
        {
            overlay.DockAt(targetContainer, GetSection(), 0);
            overlay.floating = false;
        }
    }
}
