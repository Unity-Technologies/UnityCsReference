// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    [UxmlElement]
    internal partial class CanvasOverlayManager : OverlayManager
    {
        UQueryState<CanvasOverlay> m_AllOverlays;

        public ICanvas canvas { get; set; }

        public CanvasOverlayManager()
        {
            m_AllOverlays = this.Query<CanvasOverlay>().Build();
        }

        public CanvasOverlayManager(ICanvas canvas) : this()
        {
            this.canvas = canvas;
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is GeometryChangedEvent)
            {
                UpdateOverlays();
            }
        }

        public bool IsGeometryValid()
        {
            return layout.width > 0f;
        }

        public void UpdateOverlays()
        {
            if (!IsGeometryValid())
                return;

            foreach (CanvasOverlay overlay in m_AllOverlays)
            {
                if (overlay.isShown)
                    overlay.ForceUpdate();
            }
        }
    }
}
