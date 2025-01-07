// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.UIElements
{
    /// <summary>
    /// A static class to capture and release pointers.
    /// </summary>
    public static class PointerCaptureHelper
    {
        private static PointerDispatchState GetStateFor(IEventHandler handler)
        {
            VisualElement v = handler as VisualElement;
            return v?.panel?.dispatcher?.pointerState;
        }

        /// <summary>
        /// Tests whether the element has captured the pointer.
        /// </summary>
        /// <param name="handler">The VisualElement being tested.</param>
        /// <param name="pointerId">The captured pointer.</param>
        /// <returns>True if element captured the pointer.</returns>
        public static bool HasPointerCapture(this IEventHandler handler, int pointerId)
        {
            return GetStateFor(handler)?.HasPointerCapture(handler, pointerId) ?? false;
        }

        /// <summary>
        /// Captures the pointer.
        /// </summary>
        /// <param name="handler">The VisualElement that captures the pointer.</param>
        /// <param name="pointerId">The pointer to capture.</param>
        /// <remarks>
        /// When a VisualElement captures a pointer, all pointer events—except PointerOverEvent, PointerOutEvent, PointerEnterEvent, PointerLeaveEvent—
        /// are sent to the element, regardless of which element is under the pointer.
        ///
        /// Pointer events that would normally bubble up or trickle down are instead sent exclusively
        /// to the capturing element.
        ///
        /// If a pointer capture is requested during the propagation of an existing event, then the capture
        /// only takes effect after the ongoing event has been fully dispatched and propagated.
        ///\\
        /// __Note__: Events determine their target and propagation path when they are dispatched,
        /// not necessarily when they are sent. If an event is sent during another event's propagation,
        /// then the new event is added to an event queue and processed after the current event and all other events
        /// in the queue before it have been fully dispatched and propagated.
        /// </remarks>
        public static void CapturePointer(this IEventHandler handler, int pointerId)
        {
            GetStateFor(handler)?.CapturePointer(handler, pointerId);
        }

        /// <summary>
        /// Tests whether an element captured a pointer and, if so, tells the element to release the pointer.
        /// </summary>
        /// <remarks>
        /// If a pointer release is requested during the propagation of an existing event, the release
        /// takes effect only after the ongoing event has been fully dispatched and propagated.
        /// </remarks>
        /// <param name="handler">The element which potentially captured the pointer.</param>
        /// <param name="pointerId">The captured pointer.</param>
        public static void ReleasePointer(this IEventHandler handler, int pointerId)
        {
            GetStateFor(handler)?.ReleasePointer(handler, pointerId);
        }

        /// <summary>
        /// Returns the element that is capturing the pointer.
        /// </summary>
        /// <param name="panel">The panel that holds the element.</param>
        /// <param name="pointerId">The captured pointer.</param>
        /// <returns>The element that is capturing the pointer.</returns>
        public static IEventHandler GetCapturingElement(this IPanel panel, int pointerId)
        {
            return panel?.dispatcher?.pointerState.GetCapturingElement(pointerId);
        }

        /// <summary>
        /// Releases the pointer.
        /// </summary>
        /// <remarks>
        /// If a pointer release is requested during the propagation of an existing event, the release
        /// takes effect only after the ongoing event has been fully dispatched and propagated.
        /// </remarks>
        /// <param name="panel">The panel that holds the element that captured the pointer.</param>
        /// <param name="pointerId">The captured pointer.</param>
        public static void ReleasePointer(this IPanel panel, int pointerId)
        {
            panel?.dispatcher?.pointerState.ReleasePointer(pointerId);
        }

        internal static void ActivateCompatibilityMouseEvents(this IPanel panel, int pointerId)
        {
            panel?.dispatcher?.pointerState.ActivateCompatibilityMouseEvents(pointerId);
        }

        internal static void PreventCompatibilityMouseEvents(this IPanel panel, int pointerId)
        {
            panel?.dispatcher?.pointerState.PreventCompatibilityMouseEvents(pointerId);
        }

        internal static bool ShouldSendCompatibilityMouseEvents(this IPanel panel, IPointerEvent evt)
        {
            return panel?.dispatcher?.pointerState.ShouldSendCompatibilityMouseEvents(evt) ?? true;
        }

        internal static void ProcessPointerCapture(this IPanel panel, int pointerId)
        {
            panel?.dispatcher?.pointerState.ProcessPointerCapture(pointerId);
        }

        internal static void ReleaseEditorMouseCapture()
        {
            EventDispatcher.editorDispatcher.pointerState.ReleasePointer(PointerId.mousePointerId);
            EventDispatcher.editorDispatcher.pointerState.ProcessPointerCapture(PointerId.mousePointerId);
        }


        // Used in tests
        internal static void ResetPointerDispatchState(this IPanel panel)
        {
            panel?.dispatcher?.pointerState.Reset();
        }
    }

    internal class PointerDispatchState
    {
        private IEventHandler[] m_PendingPointerCapture = new IEventHandler[PointerId.maxPointers];
        private IEventHandler[] m_PointerCapture = new IEventHandler[PointerId.maxPointers];
        private bool[] m_ShouldSendCompatibilityMouseEvents = new bool[PointerId.maxPointers];

        public PointerDispatchState()
        {
            Reset();
        }

        internal void Reset()
        {
            for (var i = 0; i < m_PointerCapture.Length; i++)
            {
                m_PendingPointerCapture[i] = null;
                m_PointerCapture[i] = null;
                m_ShouldSendCompatibilityMouseEvents[i] = true;
            }
        }

        public IEventHandler GetCapturingElement(int pointerId)
        {
            return m_PendingPointerCapture[pointerId];
        }

        public bool HasPointerCapture(IEventHandler handler, int pointerId)
        {
            return m_PendingPointerCapture[pointerId] == handler;
        }

        public void CapturePointer(IEventHandler handler, int pointerId)
        {
            if (pointerId == PointerId.mousePointerId && m_PendingPointerCapture[pointerId] != handler && GUIUtility.hotControl != 0)
            {
                // Note that this will release the mouse and call ProcessPointerCapture immediately
                GUIUtility.hotControl = 0;
            }

            m_PendingPointerCapture[pointerId] = handler;
        }

        public void ReleasePointer(int pointerId)
        {
            m_PendingPointerCapture[pointerId] = null;
        }

        public void ReleasePointer(IEventHandler handler, int pointerId)
        {
            if (handler == m_PendingPointerCapture[pointerId])
            {
                m_PendingPointerCapture[pointerId] = null;
            }
        }

        public void ProcessPointerCapture(int pointerId)
        {
            var capture = m_PointerCapture[pointerId];

            if (capture == m_PendingPointerCapture[pointerId])
                return;

            if (capture != null)
            {
                using (var e = PointerCaptureOutEvent.GetPooled(capture, m_PendingPointerCapture[pointerId], pointerId))
                {
                    capture.SendEvent(e);
                }

                // Don't send MouseCaptureOutEvent if PointerCaptureOutEvent callbacks changed the capture in-between
                if (pointerId == PointerId.mousePointerId && m_PointerCapture[pointerId] == capture)
                {
                    using (var e = MouseCaptureOutEvent.GetPooled(capture, m_PendingPointerCapture[pointerId], pointerId))
                    {
                        capture.SendEvent(e);
                    }
                }
            }

            var pendingCapture = m_PendingPointerCapture[pointerId];
            if (pendingCapture != null)
            {
                using (var e = PointerCaptureEvent.GetPooled(pendingCapture, m_PointerCapture[pointerId], pointerId))
                {
                    pendingCapture.SendEvent(e);
                }

                if (pointerId == PointerId.mousePointerId && m_PendingPointerCapture[pointerId] == pendingCapture)
                {
                    using (var e = MouseCaptureEvent.GetPooled(pendingCapture, m_PointerCapture[pointerId], pointerId))
                    {
                        pendingCapture.SendEvent(e);
                    }
                }
            }

            m_PointerCapture[pointerId] = m_PendingPointerCapture[pointerId];
        }

        public void ActivateCompatibilityMouseEvents(int pointerId)
        {
            m_ShouldSendCompatibilityMouseEvents[pointerId] = true;
        }

        public void PreventCompatibilityMouseEvents(int pointerId)
        {
            m_ShouldSendCompatibilityMouseEvents[pointerId] = false;
        }

        public bool ShouldSendCompatibilityMouseEvents(IPointerEvent evt)
        {
            return evt.isPrimary && m_ShouldSendCompatibilityMouseEvents[evt.pointerId];
        }
    }
}
