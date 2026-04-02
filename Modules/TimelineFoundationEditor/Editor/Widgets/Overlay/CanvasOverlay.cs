// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    abstract class CanvasOverlay : Overlay
    {
        CanvasOverlayManager m_Manager;
        protected ICanvas Canvas => m_Manager.canvas;

        protected CanvasOverlay(PickingMode pickingMode = PickingMode.Ignore)
            : base(pickingMode)
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_Manager = GetFirstAncestorOfType<CanvasOverlayManager>();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Manager = null;
        }

        public void ForceUpdate()
        {
            if (m_Manager != null && m_Manager.IsGeometryValid())
                Update(m_Manager.canvas);
        }

        protected float WorldToLocalX(float x)
        {
            return parent.WorldToLocal(new Vector2(x, 0f)).x;
        }

        protected virtual void Update(ICanvas canvas) { }
    }
}
