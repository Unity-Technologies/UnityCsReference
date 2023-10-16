// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Overlays
{
    class OverlayGhostDropZone : OverlayDropZoneBase
    {
        public override OverlayContainer targetContainer => m_Container;
        public override OverlayContainerSection targetSection => m_Section;

        readonly OverlayContainer m_Container;
        readonly OverlayContainerSection m_Section;

        OverlayGhostDropZone(OverlayContainer container, OverlayContainerSection section)
        {
            m_Container = container;
            m_Section = section;
        }

        public static OverlayGhostDropZone Create(Overlay overlay)
        {
            bool found = overlay.container.GetOverlayIndex(overlay, out var section, out int index);
            OverlayGhostDropZone dropZone = new OverlayGhostDropZone(overlay.container, section);
            if (found)
            {
                overlay.container.GetSectionElement(section)?.Insert(index, dropZone);
                dropZone.style.width = overlay.rootVisualElement.layout.width;
                dropZone.style.height = overlay.rootVisualElement.layout.height;
            }

            return dropZone;
        }

        public override void DropOverlay(Overlay overlay) {} //This case is handled in the drag as a cancel
    }
}
