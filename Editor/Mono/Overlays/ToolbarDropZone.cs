// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class ToolbarDropZone : OverlayContainerDropZone
    {
        public override OverlayContainer targetContainer => m_CurrentContainer;

        OverlayContainer m_CurrentContainer;

        bool m_ShouldAllowPanelOverlay;

        public ToolbarDropZone(OverlayContainer toolbarContainer, bool shouldAllowPanelOverlay) : base(null, Placement.Start)
        {
            m_CurrentContainer = toolbarContainer;
            m_ShouldAllowPanelOverlay = shouldAllowPanelOverlay;
        }

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return !m_CurrentContainer.HasVisibleOverlays()
                   && !m_CurrentContainer.ContainsOverlay(draggedOverlay, GetSection());
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);
            style.display = ShouldEnable(draggedOverlay) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
