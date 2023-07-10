// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    static class PointerDeviceState
    {
        [Flags]
        internal enum LocationFlag
        {
            None = 0,
            // The location of the pointer was outside of the panel **when the location was set**. The window may have grown since then.
            // We need to know this to avoid elements to become hovered when the window grows, due to an out of date pointer location.
            OutsidePanel = 1,
        }

        private struct PointerLocation
        {
            // Pointer position in panel space. If Panel is null, position in screen space.
            internal Vector2 Position { get; private set; }
            // The Panel that handles events for this pointer.
            internal IPanel Panel { get; private set; }
            internal LocationFlag Flags { get; private set; }

            internal void SetLocation(Vector2 position, IPanel panel)
            {
                Position = position;
                Panel = panel;
                Flags = LocationFlag.None;

                if (panel == null || !panel.visualTree.layout.Contains(position))
                {
                    Flags |= LocationFlag.OutsidePanel;
                }
            }
        }

        private static PointerLocation[] s_EditorPointerLocations = new PointerLocation[PointerId.maxPointers];
        private static PointerLocation[] s_PlayerPointerLocations = new PointerLocation[PointerId.maxPointers];
        private static int[] s_PressedButtons = new int[PointerId.maxPointers];

        // When a pointer button is pressed on top of a runtime panel, that panel is flagged as having "soft capture" of that pointer,
        // that is, unless an element has an actual pointer capture, pointer move events should stay inside this panel until
        // all pointer buttons are released again. This is used by runtime panels to mimic GUIView.cpp window capture behavior.
        private static readonly IPanel[] s_PlayerPanelWithSoftPointerCapture = new IPanel[PointerId.maxPointers];

        // For test usage
        internal static void Reset()
        {
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                s_EditorPointerLocations[i].SetLocation(Vector2.zero, null);
                s_PlayerPointerLocations[i].SetLocation(Vector2.zero, null);
                s_PressedButtons[i] = 0;
                s_PlayerPanelWithSoftPointerCapture[i] = null;
            }
        }

        internal static void RemovePanelData(IPanel panel)
        {
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                if (s_EditorPointerLocations[i].Panel == panel)
                    s_EditorPointerLocations[i].SetLocation(Vector2.zero, null);
                if (s_PlayerPointerLocations[i].Panel == panel)
                   s_PlayerPointerLocations[i].SetLocation(Vector2.zero, null);
                if (s_PlayerPanelWithSoftPointerCapture[i] == panel)
                   s_PlayerPanelWithSoftPointerCapture[i] = null;
            }
        }

        public static void SavePointerPosition(int pointerId, Vector2 position, IPanel panel, ContextType contextType)
        {
            switch (contextType)
            {
                case ContextType.Editor:
                default:
                    s_EditorPointerLocations[pointerId].SetLocation(position, panel);
                    break;

                case ContextType.Player:
                    s_PlayerPointerLocations[pointerId].SetLocation(position, panel);
                    break;
            }
        }

        public static void PressButton(int pointerId, int buttonId)
        {
            Debug.Assert(buttonId >= 0, "PressButton expects buttonId >= 0");
            Debug.Assert(buttonId < 32, "PressButton expects buttonId < 32");
            s_PressedButtons[pointerId] |= (1 << buttonId);
        }

        public static void ReleaseButton(int pointerId, int buttonId)
        {
            Debug.Assert(buttonId >= 0, "ReleaseButton expects buttonId >= 0");
            Debug.Assert(buttonId < 32, "ReleaseButton expects buttonId < 32");
            s_PressedButtons[pointerId] &= ~(1 << buttonId);
        }

        public static void ReleaseAllButtons(int pointerId)
        {
            s_PressedButtons[pointerId] = 0;
        }

        public static Vector2 GetPointerPosition(int pointerId, ContextType contextType)
        {
            switch (contextType)
            {
                case ContextType.Editor:
                default:
                    return s_EditorPointerLocations[pointerId].Position;

                case ContextType.Player:
                    return s_PlayerPointerLocations[pointerId].Position;
            }
        }

        public static IPanel GetPanel(int pointerId, ContextType contextType)
        {
            switch (contextType)
            {
                case ContextType.Editor:
                default:
                    return s_EditorPointerLocations[pointerId].Panel;

                case ContextType.Player:
                    return s_PlayerPointerLocations[pointerId].Panel;
            }
        }

        static bool HasFlagFast(LocationFlag flagSet, LocationFlag flag)
        {
            return (flagSet & flag) == flag;
        }

        public static bool HasLocationFlag(int pointerId, ContextType contextType, LocationFlag flag)
        {
            switch (contextType)
            {
                case ContextType.Editor:
                default:
                    return HasFlagFast(s_EditorPointerLocations[pointerId].Flags, flag);

                case ContextType.Player:
                    return HasFlagFast(s_PlayerPointerLocations[pointerId].Flags, flag);
            }
        }

        public static int GetPressedButtons(int pointerId)
        {
            return s_PressedButtons[pointerId];
        }

        internal static bool HasAdditionalPressedButtons(int pointerId, int exceptButtonId)
        {
            return (s_PressedButtons[pointerId] & ~(1 << exceptButtonId)) != 0;
        }

        internal static void SetPlayerPanelWithSoftPointerCapture(int pointerId, IPanel panel)
        {
            s_PlayerPanelWithSoftPointerCapture[pointerId] = panel;
        }

        internal static IPanel GetPlayerPanelWithSoftPointerCapture(int pointerId)
        {
            return s_PlayerPanelWithSoftPointerCapture[pointerId];
        }
    }
}
