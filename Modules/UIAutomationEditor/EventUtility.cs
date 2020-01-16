// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.UIAutomation
{
    // Note: all input mouse positions should be in GUIView coordinates as the EventUtility class dispatches events as
    // they were coming from GUIView. Use ConvertEditorWindowCoordsToGuiViewCoords and ConvertGuiViewCoordsToEditorWindowCoords
    // to convert between coordic


    static class EventUtility
    {
        internal static bool Click(EditorWindow window, Vector2 mousePosition)
        {
            return Click(window, mousePosition, EventModifiers.None);
        }

        internal static bool Click(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            return Click(window, mousePosition, modifiers, 0);
        }

        internal static bool Click(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers, int mouseButton)
        {
            var anyEventsUsed = false;

            var evt = new Event()
            {
                mousePosition = mousePosition,
                modifiers = modifiers,
                button = mouseButton
            };

            evt.type = EventType.MouseDown;
            anyEventsUsed |= window.SendEvent(evt);

            evt.type = EventType.MouseUp;
            anyEventsUsed |= window.SendEvent(evt);

            UpdateFakeMouseCursorPosition(window, mousePosition);

            return anyEventsUsed;
        }

        public static bool KeyDownAndUp(EditorWindow window, KeyCode keyCode)
        {
            return KeyDownAndUp(window, keyCode, EventModifiers.None);
        }

        public static bool KeyDownAndUp(EditorWindow window, KeyCode keyCode, EventModifiers modifiers)
        {
            var anyEventsUsed = false;

            anyEventsUsed |= KeyDown(window, keyCode, modifiers);
            anyEventsUsed |= KeyUp(window, keyCode, modifiers);

            return anyEventsUsed;
        }

        public static bool KeyDown(EditorWindow window, KeyCode keyCode)
        {
            return KeyDown(window, keyCode, EventModifiers.None);
        }

        public static bool KeyDown(EditorWindow window, KeyCode keyCode, EventModifiers modifiers)
        {
            var anyEventsUsed = false;

            var evt = new Event()
            {
                type = EventType.KeyDown,
                keyCode = keyCode,
                modifiers = modifiers
            };

            anyEventsUsed |= window.SendEvent(evt);

            evt.character = (char)keyCode;
            evt.keyCode = KeyCode.None;

            anyEventsUsed |= window.SendEvent(evt);

            return anyEventsUsed;
        }

        public static bool KeyUp(EditorWindow window, KeyCode keyCode)
        {
            return KeyUp(window, keyCode, EventModifiers.None);
        }

        public static bool KeyUp(EditorWindow window, KeyCode keyCode, EventModifiers modifiers)
        {
            var evt = new Event()
            {
                type = EventType.KeyUp,
                keyCode = keyCode,
                modifiers = modifiers
            };

            return window.SendEvent(evt);
        }

        public static void UpdateMouseMove(EditorWindow window, Vector2 mousePosition)
        {
            var evt = new Event()
            {
                type = EventType.MouseMove,
            };
            window.SendEvent(evt);

            UpdateFakeMouseCursorPosition(window, mousePosition);
        }

        // ----------------------------------------------------------
        // Drag and drop (simulates the drag and drop handling in the editor)
        // ----------------------------------------------------------

        public static void DragAndDrop(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd)
        {
            DragAndDrop(window, mousePositionStart, mousePositionEnd, EventModifiers.None);
        }

        public static void DragAndDrop(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd, EventModifiers modifiers)
        {
            BeginDragAndDrop(window, mousePositionStart, modifiers);
            UpdateDragAndDrop(window, (mousePositionStart + mousePositionStart) * 0.5f);
            EndDragAndDrop(window, mousePositionEnd, modifiers);
        }

        public static bool BeginDragAndDrop(EditorWindow window, Vector2 mousePosition)
        {
            return BeginDragAndDrop(window, mousePosition, EventModifiers.None);
        }

        public static bool BeginDragAndDrop(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            var anyEventsUsed = false;

            InternalEditorUtility.PrepareDragAndDropTesting(window);

            var evt = new Event()
            {
                mousePosition = mousePosition,
                modifiers = modifiers
            };

            evt.type = EventType.MouseDown;
            evt.clickCount = 1;
            anyEventsUsed |= window.SendEvent(evt);
            evt.clickCount = 0;

            evt.type = EventType.MouseDrag;
            anyEventsUsed |= window.SendEvent(evt);

            // Some drag and drop handling requires to small mouse drag distance before starting a drag and drop session
            evt.mousePosition = new Vector2(evt.mousePosition.x, evt.mousePosition.y + 20);
            anyEventsUsed |= window.SendEvent(evt);

            // Send first DragUpdated event
            evt.type = EventType.DragUpdated;
            anyEventsUsed |= window.SendEvent(evt);

            window.Repaint();

            return anyEventsUsed;
        }

        public static bool EndDragAndDrop(EditorWindow window, Vector2 mousePosition)
        {
            return EndDragAndDrop(window, mousePosition, EventModifiers.None);
        }

        public static bool EndDragAndDrop(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            var anyEventsUsed = false;

            var evt = new Event()
            {
                mousePosition = mousePosition,
                modifiers = modifiers
            };

            evt.type = EventType.DragUpdated;
            anyEventsUsed |= window.SendEvent(evt);

            evt.type = EventType.DragPerform;
            anyEventsUsed |= window.SendEvent(evt);

            return anyEventsUsed;
        }

        public static bool UpdateDragAndDrop(EditorWindow window, Vector2 mousePosition)
        {
            return UpdateDragAndDrop(window, mousePosition, EventModifiers.None);
        }

        public static bool UpdateDragAndDrop(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            var anyEventsUsed = false;

            var evt = new Event()
            {
                type = EventType.DragUpdated,
                mousePosition = mousePosition,
                modifiers = modifiers
            };

            anyEventsUsed |= window.SendEvent(evt);

            UpdateFakeMouseCursorPosition(window, mousePosition);

            return anyEventsUsed;
        }

        // ----------------------------------------------------------
        // Mouse dragging (simulates simple mousedown, mouse drag and mouse up)
        // ----------------------------------------------------------

        static Vector2 s_PrevMousePosition;

        public static void Drag(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd)
        {
            Drag(window, mousePositionStart, mousePositionEnd, EventModifiers.None);
        }

        public static void Drag(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd, EventModifiers modifiers)
        {
            BeginDrag(window, mousePositionStart, modifiers);
            EndDrag(window, mousePositionEnd, modifiers);
        }

        public static void BeginDrag(EditorWindow window, Vector2 mousePosition)
        {
            BeginDrag(window, mousePosition, EventModifiers.None);
        }

        public static void BeginDrag(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            s_PrevMousePosition = mousePosition;
            var evt = new Event()
            {
                mousePosition = mousePosition,
                modifiers = modifiers
            };

            evt.type = EventType.MouseDown;
            evt.clickCount = 1;
            window.SendEvent(evt);
            evt.clickCount = 0;

            evt.type = EventType.MouseDrag;
            window.SendEvent(evt);
        }

        public static void EndDrag(EditorWindow window, Vector2 mousePosition)
        {
            EndDrag(window, mousePosition, EventModifiers.None);
        }

        public static void EndDrag(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            var evt = new Event()
            {
                mousePosition = mousePosition,
                modifiers = modifiers,
                delta = mousePosition - s_PrevMousePosition
            };
            s_PrevMousePosition = mousePosition;

            evt.type = EventType.MouseDrag;
            window.SendEvent(evt);

            evt.type = EventType.MouseUp;
            window.SendEvent(evt);
        }

        public static void UpdateDrag(EditorWindow window, Vector2 mousePosition)
        {
            UpdateDrag(window, mousePosition, EventModifiers.None);
        }

        public static void UpdateDrag(EditorWindow window, Vector2 mousePosition, EventModifiers modifiers)
        {
            window.SendEvent(new Event()
            {
                type = EventType.MouseDrag,
                mousePosition = mousePosition,
                modifiers = modifiers,
                delta = mousePosition - s_PrevMousePosition
            });
            s_PrevMousePosition = mousePosition;
        }

        // These are not public so hardcoded here
        const float kDockAreaTopMarginAndTabHeight = DockArea.kFloatingWindowTopBorderWidth + DockArea.kTabHeight + 1;

        public static Vector2 ConvertEditorWindowCoordsToGuiViewCoords(Vector2 editorWindowPosition)
        {
            return new Vector2(editorWindowPosition.x, editorWindowPosition.y + kDockAreaTopMarginAndTabHeight);
        }

        public static Vector2 ConvertGuiViewCoordsToEditorWindowCoords(Vector2 guiViewPosition)
        {
            return new Vector2(guiViewPosition.x, guiViewPosition.y - kDockAreaTopMarginAndTabHeight);
        }

        static void UpdateFakeMouseCursorPosition(EditorWindow window, Vector2 mousePosition)
        {
            TestEditorWindow testWindow = window as TestEditorWindow;
            if (testWindow != null)
                testWindow.fakeCursor.position = ConvertGuiViewCoordsToEditorWindowCoords(mousePosition);
        }
    }


    internal class Easing
    {
        public static float Linear(float k)
        {
            return k;
        }

        public class Quadratic
        {
            public static float In(float k)
            {
                return k * k;
            }

            public static float Out(float k)
            {
                return k * (2f - k);
            }

            public static float InOut(float k)
            {
                var eased = 2 * k * k;
                if (k > 0.5)
                    eased = 4 * k - eased - 1;
                return eased;
            }
        }
    }


    struct Wait
    {
        readonly double endTime;

        public Wait(double seconds)
        {
            endTime = EditorApplication.timeSinceStartup + seconds;
        }

        public bool keepWaiting
        {
            get { return EditorApplication.timeSinceStartup < endTime; }
        }


        // static utility methods
        public static void Seconds(double seconds)
        {
            s_Wait = new Wait(seconds);
        }

        public static bool waiting
        {
            get { return s_Wait.keepWaiting; }
        }

        static Wait s_Wait;
    }
}
