// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

abstract class VisualElementTransformer : VisualElementManipulator
{
    const string k_DragHoverCoverLayerName = "drag-hover-cover-layer";
    const string k_ScaledOrRotatedMessage = "Cannot transform an element with a scale or rotation applied.";

    internal const string k_ActiveHandleUssClass = "unity-ve-canvas-transformer--active";
    internal const string k_DisabledHandleUssClass = "unity-ve-canvas-transformer--disabled";

    Rect m_TargetRectOnStartDrag;
    Rect m_OverlayRectOnStartDrag;
    float m_TargetCorrectedBottomOnStartDrag;
    float m_TargetCorrectedRightOnStartDrag;
    // Converts host-panel drag pixels to sub-panel CSS pixels; derived from overlay/target ratio at drag start.
    float m_HostToSubPanelScale = 1f;

    protected Rect TargetRectOnStartDrag => m_TargetRectOnStartDrag;
    protected Rect OverlayRectOnStartDrag => m_OverlayRectOnStartDrag;
    protected float TargetCorrectedBottomOnStartDrag => m_TargetCorrectedBottomOnStartDrag;
    protected float TargetCorrectedRightOnStartDrag => m_TargetCorrectedRightOnStartDrag;
    protected float HostToSubPanelScale => m_HostToSubPanelScale;

    VisualElement m_DragHoverCoverLayer;

    protected bool IsTargetScaledOrRotated =>
        Target == null ||
        !Mathf.Approximately(Target.computedStyle.rotate.angle.value, 0) ||
        Target.computedStyle.scale.value != Vector3.one;

    protected VisualElementTransformer()
    {
        this.StretchToParentSize();
    }

    protected void InitializeDragHoverCoverLayer()
    {
        m_DragHoverCoverLayer = this.Q(k_DragHoverCoverLayerName);
    }

    protected virtual void OnStartDrag(VisualElement handle)
    {
        if (Target == null || IsTargetScaledOrRotated)
        {
            if (IsTargetScaledOrRotated)
                NotifyMessage(k_ScaledOrRotatedMessage);
            return;
        }

        m_TargetRectOnStartDrag = Target.layout;
        m_OverlayRectOnStartDrag = hierarchy.parent?.layout ?? Rect.zero;

        // The overlay's CSS width differs from the target's layout width by the host-to-sub-panel scale factor.
        // Dividing target width by overlay width recovers that factor to convert host drag pixels to sub-panel CSS pixels.
        m_HostToSubPanelScale = m_OverlayRectOnStartDrag.width > 0f
            ? m_TargetRectOnStartDrag.width / m_OverlayRectOnStartDrag.width
            : 1f / ZoomScale;

        var targetMarginTop = Target.resolvedStyle.marginTop;
        var targetMarginLeft = Target.resolvedStyle.marginLeft;
        var targetMarginBottom = Target.resolvedStyle.marginBottom;
        var targetMarginRight = Target.resolvedStyle.marginRight;
        m_TargetRectOnStartDrag.y -= targetMarginTop;
        m_TargetRectOnStartDrag.x -= targetMarginLeft;

        var parentBorderTop = Target.parent?.resolvedStyle.borderTopWidth ?? 0f;
        var parentBorderLeft = Target.parent?.resolvedStyle.borderLeftWidth ?? 0f;
        var parentBorderBottom = Target.parent?.resolvedStyle.borderBottomWidth ?? 0f;
        var parentBorderRight = Target.parent?.resolvedStyle.borderRightWidth ?? 0f;
        m_TargetRectOnStartDrag.y -= parentBorderTop;
        m_TargetRectOnStartDrag.x -= parentBorderLeft;

        var parentRect = Target.parent?.layout ?? Rect.zero;
        m_TargetCorrectedBottomOnStartDrag =
            parentRect.height - m_TargetRectOnStartDrag.yMax
                              - targetMarginTop - targetMarginBottom - parentBorderTop - parentBorderBottom;
        m_TargetCorrectedRightOnStartDrag =
            parentRect.width - m_TargetRectOnStartDrag.xMax
                             - targetMarginLeft - targetMarginRight - parentBorderLeft - parentBorderRight;

        if (m_DragHoverCoverLayer != null)
        {
            if (m_DragHoverCoverLayer.parent.IndexOf(m_DragHoverCoverLayer) !=
                m_DragHoverCoverLayer.parent.childCount - 1)
                m_DragHoverCoverLayer.BringToFront();
            m_DragHoverCoverLayer.style.display = DisplayStyle.Flex;
            m_DragHoverCoverLayer.style.cursor = handle.computedStyle.cursor;
        }
    }

    protected virtual void OnEndDrag()
    {
        if (m_DragHoverCoverLayer != null)
        {
            m_DragHoverCoverLayer.style.display = DisplayStyle.None;
            m_DragHoverCoverLayer.RemoveFromClassList(k_ActiveHandleUssClass);
        }
    }

    public virtual void OnProcessChangeOnTarget()
    {
    }

    protected sealed class DragManipulator : PointerManipulator
    {
        Vector3 m_Start;
        bool m_Active;

        readonly Action<VisualElement> m_StartDrag;
        readonly Action m_EndDrag;
        readonly Action<Vector2> m_DragAction;

        public DragManipulator(
            Action<VisualElement> startDrag,
            Action endDrag,
            Action<Vector2> dragAction)
        {
            m_StartDrag = startDrag;
            m_EndDrag = endDrag;
            m_DragAction = dragAction;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (m_Active || target.ClassListContains(k_DisabledHandleUssClass))
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            // Double-click is reserved for text editing of the selected element.
            if (e.clickCount == 2)
                return;

            m_StartDrag(target);
            m_Start = e.position;
            m_Active = true;
            target.CaptureMouse();
            e.StopPropagation();
            target.AddToClassList(k_ActiveHandleUssClass);
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_Active || !target.HasMouseCapture())
                return;

            m_DragAction(e.position - m_Start);
            e.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                return;

            target.ReleaseMouse();
            e.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent e)
        {
            if (!m_Active) return;

            m_Active = false;
            target.RemoveFromClassList(k_ActiveHandleUssClass);
            m_EndDrag();
        }
    }
}
