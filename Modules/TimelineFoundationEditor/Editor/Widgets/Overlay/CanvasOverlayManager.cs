// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class CanvasOverlayManager : OverlayManager
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new CanvasOverlayManager();
        }

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
