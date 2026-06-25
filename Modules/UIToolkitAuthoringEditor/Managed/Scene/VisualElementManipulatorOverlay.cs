// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Overlay element containing manipulators that can move and/or resize an element.
sealed class VisualElementManipulatorOverlay : VisualElement
{
    readonly VisualElementResizer m_Resizer;
    readonly VisualElementMover m_Mover;

    VisualElement m_Target;

    public float ZoomScale { get; set; } = 1f;

    public VisualElement Target
    {
        get => m_Target;
        set
        {
            if (m_Target == value)
                return;
            m_Target?.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetached);
            m_Target = value;
            m_Target?.RegisterCallback<DetachFromPanelEvent>(OnTargetDetached);
        }
    }

    public bool IsReadOnly { get; set; }

    public VisualElementManipulatorOverlay()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;

        // Mover below resizer so resize handles receive pointer events first.
        m_Mover = new VisualElementMover();
        m_Resizer = new VisualElementResizer();
        Add(m_Mover);
        Add(m_Resizer);

        m_Mover.MessageNotified += OnManipulatorMessage;
        m_Resizer.MessageNotified += OnManipulatorMessage;
    }

    public void Activate()
    {
        m_Mover.Activate(m_Target, IsReadOnly);
        m_Resizer.Activate(m_Target, IsReadOnly);
        UpdateLayout();
    }

    public void Deactivate()
    {
        Target = null;
        IsReadOnly = false;
        m_Mover.Deactivate();
        m_Resizer.Deactivate();
        ClearLayout();
    }

    public void OnProcessChangeOnTarget()
    {
        m_Mover.OnProcessChangeOnTarget();
        m_Resizer.OnProcessChangeOnTarget();
    }

    public void UpdateLayout()
    {
        if (m_Target == null || m_Target.resourcesReleased)
        {
            ClearLayout();
            return;
        }

        var ppp = ((Panel)m_Target.panel)?.pixelsPerPoint ?? 1.0f;
        var targetLayout = m_Target.layout;

        // worldBound is an AABB; use the parent transform to recover pre-rotation
        // origin and extents, then mirror the element's CSS transforms onto the overlay.
        var pt = m_Target.parent?.worldTransform ?? Matrix4x4.identity;
        var origin = pt.MultiplyPoint3x4(new Vector3(targetLayout.x, targetLayout.y, 0));

        style.left = origin.x / ppp;
        style.top = origin.y / ppp;
        style.width = pt.MultiplyVector(new Vector3(targetLayout.width, 0, 0)).magnitude / ppp;
        style.height = pt.MultiplyVector(new Vector3(0, targetLayout.height, 0)).magnitude / ppp;

        style.rotate = m_Target.computedStyle.rotate;
        style.scale = m_Target.computedStyle.scale.value;
        style.transformOrigin = m_Target.computedStyle.transformOrigin;

        m_Mover.ZoomScale = ZoomScale;
        m_Resizer.ZoomScale = ZoomScale;

        m_Resizer.UpdateStyles();
        m_Mover.UpdateStyles();
    }

    void ClearLayout()
    {
        style.left = StyleKeyword.Null;
        style.top = StyleKeyword.Null;
        style.width = StyleKeyword.Null;
        style.height = StyleKeyword.Null;
        style.rotate = StyleKeyword.Null;
        style.scale = StyleKeyword.Null;
        style.transformOrigin = StyleKeyword.Null;
    }

    void OnTargetDetached(DetachFromPanelEvent _) => Deactivate();

    void OnManipulatorMessage(string message)
    {
        using var evt = CanvasManipulatorMessageEvent.GetPooled(message);
        evt.target = this;
        SendEvent(evt);
    }
}
