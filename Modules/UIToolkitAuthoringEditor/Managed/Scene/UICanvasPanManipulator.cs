// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class UICanvasPanManipulator : PointerManipulator
{
    const float k_MovementThreshold = 3f;

    readonly UIViewport m_Viewport;
    readonly UICanvas m_Canvas;
    bool m_Panning;
    bool m_Tracking;
    int m_ActivatingButton;
    Vector2 m_InitialPointerPosition;

    public UICanvasPanManipulator(UIViewport viewport)
    {
        m_Viewport = viewport;
        m_Viewport.AddManipulator(this);
        m_Canvas = m_Viewport.Q<UICanvas>();
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (CanStartManipulation(evt))
        {
            m_Tracking = true;
            m_ActivatingButton = evt.button;
            m_InitialPointerPosition = evt.position;
        }
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (m_ActivatingButton != evt.button || (!m_Panning && !m_Tracking))
            return;

        if (m_Panning)
        {
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        m_Panning = false;
        m_Tracking = false;
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (m_Tracking && !m_Panning)
        {
            var delta = new Vector2(evt.position.x, evt.position.y) - m_InitialPointerPosition;
            if (delta.magnitude >= k_MovementThreshold)
            {
                m_Panning = true;
                m_Tracking = false;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        if (m_Panning)
        {
            m_Canvas.Offset += new Vector2(evt.deltaPosition.x, evt.deltaPosition.y);
            evt.StopPropagation();
        }
    }
}
