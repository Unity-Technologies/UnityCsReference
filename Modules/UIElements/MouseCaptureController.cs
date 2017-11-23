// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public static class MouseCaptureController
    {
        internal static IEventHandler mouseCapture { get; private set; }

        public static bool IsMouseCaptureTaken()
        {
            return mouseCapture != null;
        }

        public static bool HasMouseCapture(this IEventHandler handler)
        {
            return mouseCapture == handler;
        }

        public static void TakeMouseCapture(this IEventHandler handler)
        {
            if (mouseCapture == handler)
                return;
            if (GUIUtility.hotControl != 0)
            {
                Debug.Log("Should not be capturing when there is a hotcontrol");
                return;
            }

            // TODO: assign a reserved control id to hotControl so that repaint events in OnGUI() have their hotcontrol check behave normally
            ReleaseMouseCapture();

            mouseCapture = handler;

            using (MouseCaptureEvent e = MouseCaptureEvent.GetPooled(mouseCapture))
            {
                UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
            }
        }

        public static void ReleaseMouseCapture(this IEventHandler handler)
        {
            Debug.Assert(handler == mouseCapture, "Element releasing capture does not have capture");
            if (handler == mouseCapture)
            {
                ReleaseMouseCapture();
            }
        }

        public static void ReleaseMouseCapture()
        {
            if (mouseCapture != null)
            {
                using (MouseCaptureOutEvent e = MouseCaptureOutEvent.GetPooled(mouseCapture))
                {
                    UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
                }
            }
            mouseCapture = null;
        }
    }
}
