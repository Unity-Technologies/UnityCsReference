// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public static class MouseCaptureController
    {
#pragma warning disable 414
        static bool m_IsMouseCapturedWarningEmitted = false;
        static bool m_ReleaseMouseWarningEmitted = false;
#pragma warning restore 414

        // TODO 2020.1 [Obsolete("Use PointerCaptureHelper.GetCapturingElement() instead.")]
        public static bool IsMouseCaptured()
        {
            return EventDispatcher.editorDispatcher.pointerState.GetCapturingElement(PointerId.mousePointerId) != null;
        }

        public static bool HasMouseCapture(this IEventHandler handler)
        {
            VisualElement ve = handler as VisualElement;
            return ve.HasPointerCapture(PointerId.mousePointerId);
        }

        public static void CaptureMouse(this IEventHandler handler)
        {
            VisualElement ve = handler as VisualElement;
            if (ve != null)
            {
                ve.CapturePointer(PointerId.mousePointerId);
                ve.panel.ProcessPointerCapture(PointerId.mousePointerId);
            }
        }

        public static void ReleaseMouse(this IEventHandler handler)
        {
            VisualElement ve = handler as VisualElement;
            if (ve != null)
            {
                ve.ReleasePointer(PointerId.mousePointerId);
                ve.panel.ProcessPointerCapture(PointerId.mousePointerId);
            }
        }

        // TODO 2020.1 [Obsolete("Use PointerCaptureHelper.ReleasePointer() instead.")]
        public static void ReleaseMouse()
        {
            PointerCaptureHelper.ReleaseEditorMouseCapture();
        }
    }
}
