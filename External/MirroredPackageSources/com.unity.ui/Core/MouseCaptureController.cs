using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Class that manages capturing mouse events.
    /// </summary>
    public static class MouseCaptureController
    {
#pragma warning disable 414
        static bool m_IsMouseCapturedWarningEmitted = false;
        static bool m_ReleaseMouseWarningEmitted = false;
#pragma warning restore 414

        // TODO 2020.1 [Obsolete("Use PointerCaptureHelper.GetCapturingElement() instead.")]
        /// <summary>
        /// Checks if there is a handler capturing the mouse.
        /// </summary>
        /// <returns>True if a handler is capturing the mouse, false otherwise.</returns>
        public static bool IsMouseCaptured()
        {
            return EventDispatcher.editorDispatcher.pointerState.GetCapturingElement(PointerId.mousePointerId) != null;
        }

        /// <summary>
        /// Checks if the event handler is capturing the mouse.
        /// </summary>
        /// <param name="handler">Event handler to check.</param>
        /// <returns>True if the handler captures the mouse.</returns>
        public static bool HasMouseCapture(this IEventHandler handler)
        {
            VisualElement ve = handler as VisualElement;
            return ve.HasPointerCapture(PointerId.mousePointerId);
        }

        /// <summary>
        /// Assigns an event handler to capture mouse events.
        /// </summary>
        /// <param name="handler">The event handler that captures mouse events.</param>
        /// <remarks>
        /// If an event handler is already set to capture mouse events, the event handler is replaced with the handler specified by this method.
        /// </remarks>
        public static void CaptureMouse(this IEventHandler handler)
        {
            VisualElement ve = handler as VisualElement;
            if (ve != null)
            {
                ve.CapturePointer(PointerId.mousePointerId);
                ve.panel.ProcessPointerCapture(PointerId.mousePointerId);
            }
        }

        /// <summary>
        /// Stops an event handler from capturing the mouse.
        /// </summary>
        /// <param name="handler">The event handler to stop capturing the mouse. If this handler is not assigned to capturing the mouse, nothing happens.</param>
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
        /// <summary>
        /// Stops an event handler from capturing the mouse.
        /// </summary>
        public static void ReleaseMouse()
        {
            PointerCaptureHelper.ReleaseEditorMouseCapture();
        }
    }
}
