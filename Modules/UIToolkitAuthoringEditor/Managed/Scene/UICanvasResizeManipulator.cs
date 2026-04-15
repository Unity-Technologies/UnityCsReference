// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class UICanvasResizeManipulator : PointerManipulator
{
    readonly UICanvas m_Canvas;
    readonly UICanvasResizerHandle m_CanvasResizer;
    bool m_Resizing;
    int m_ActivatingButton;

    public UICanvasResizeManipulator(UICanvas canvas, UICanvasResizerHandle resizer)
    {
        m_Canvas = canvas;
        m_CanvasResizer = resizer;
        m_CanvasResizer.AddManipulator(this);
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
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
            m_Resizing = true;
            m_ActivatingButton = evt.button;
            target.CaptureMouse();
            evt.StopImmediatePropagation();
        }
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (m_ActivatingButton != evt.button || !CanStopManipulation(evt))
            return;

        if (m_Resizing)
        {
            target.ReleaseMouse();
            evt.StopPropagation();
        }
        m_Resizing = false;
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!m_Resizing)
            return;

        Vector2 deltaPosition = evt.deltaPosition;
        using (m_Canvas.ManipulationScope())
        {
            switch (m_CanvasResizer.Position)
            {
                case UICanvasResizerHandle.Anchor.Top:
                    m_Canvas.Offset += new Vector2(0, deltaPosition.y);
                    m_Canvas.Size += new Vector2(0, -deltaPosition.y);
                    break;
                case UICanvasResizerHandle.Anchor.TopRight:
                    m_Canvas.Offset += new Vector2(0, deltaPosition.y);
                    m_Canvas.Size += new Vector2(deltaPosition.x, -deltaPosition.y);
                    break;
                case UICanvasResizerHandle.Anchor.Right:
                    m_Canvas.Size += new Vector2(deltaPosition.x, 0);
                    break;
                case UICanvasResizerHandle.Anchor.BottomRight:
                    m_Canvas.Size += new Vector2(deltaPosition.x, deltaPosition.y);
                    break;
                case UICanvasResizerHandle.Anchor.Bottom:
                    m_Canvas.Size += new Vector2(0, deltaPosition.y);
                    break;
                case UICanvasResizerHandle.Anchor.BottomLeft:
                    m_Canvas.Offset += new Vector2(deltaPosition.x, 0);
                    m_Canvas.Size += new Vector2(-deltaPosition.x, deltaPosition.y);
                    break;
                case UICanvasResizerHandle.Anchor.Left:
                    m_Canvas.Offset += new Vector2(deltaPosition.x, 0);
                    m_Canvas.Size += new Vector2(-deltaPosition.x, 0);
                    break;
                case UICanvasResizerHandle.Anchor.TopLeft:
                    m_Canvas.Offset += new Vector2(deltaPosition.x, deltaPosition.y);
                    m_Canvas.Size += new Vector2(-deltaPosition.x, -deltaPosition.y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        evt.StopPropagation();
    }
}
