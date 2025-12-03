// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
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

        protected override bool ShouldEnable(Overlay draggedOverlay)
        {
            return !m_CurrentContainer.GetContainerSection(GetSection()).HasVisibleOverlays()
                   && !m_CurrentContainer.ContainsOverlay(draggedOverlay, GetSection());
        }

        public override void Activate(Overlay draggedOverlay)
        {
            base.Activate(draggedOverlay);
            style.display = DisplayStyle.Flex;
            SetHidden(false);
        }
    }
}
