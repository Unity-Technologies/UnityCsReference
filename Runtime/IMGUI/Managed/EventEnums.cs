// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Types of UnityGUI input and processing events.
    public enum EventType
    {
        // Mouse button was pressed.
        MouseDown = 0,
        // Mouse button was released.
        MouseUp = 1,
        // Mouse was moved (editor views only).
        MouseMove = 2,
        // Mouse was dragged.
        MouseDrag = 3,
        // A keyboard key was pressed.
        KeyDown = 4,
        // A keyboard key was released.
        KeyUp = 5,
        // The scroll wheel was moved.
        ScrollWheel = 6,
        // A repaint event. One is sent every frame.
        Repaint = 7,
        // A layout event.
        Layout = 8,

        // Editor only: drag & drop operation updated.
        DragUpdated = 9,
        // Editor only: drag & drop operation performed.
        DragPerform = 10,
        // Editor only: drag & drop operation exited.
        DragExited = 15,

        // [[Event]] should be ignored.
        Ignore = 11,

        // Already processed event.
        Used = 12,

        // Validates a special command (e.g. copy & paste)
        ValidateCommand = 13,

        // Execute a special command (eg. copy & paste)
        ExecuteCommand = 14,

        // User has right-clicked (or control-clicked on the mac).
        ContextClick = 16,

        // Mouse entered a window
        MouseEnterWindow = 20,
        // Mouse left a window
        MouseLeaveWindow = 21,

        [Obsolete("Use MouseDown instead (UnityUpgradable) -> MouseDown", true)]
        mouseDown = 0,
        [Obsolete("Use MouseUp instead (UnityUpgradable) -> MouseUp", true)]
        mouseUp = 1,
        [Obsolete("Use MouseMove instead (UnityUpgradable) -> MouseMove", true)]
        mouseMove = 2,
        [Obsolete("Use MouseDrag instead (UnityUpgradable) -> MouseDrag", true)]
        mouseDrag = 3,
        [Obsolete("Use KeyDown instead (UnityUpgradable) -> KeyDown", true)]
        keyDown = 4,
        [Obsolete("Use KeyUp instead (UnityUpgradable) -> KeyUp", true)]
        keyUp = 5,
        [Obsolete("Use ScrollWheel instead (UnityUpgradable) -> ScrollWheel", true)]
        scrollWheel = 6,
        [Obsolete("Use Repaint instead (UnityUpgradable) -> Repaint", true)]
        repaint = 7,
        [Obsolete("Use Layout instead (UnityUpgradable) -> Layout", true)]
        layout = 8,
        [Obsolete("Use DragUpdated instead (UnityUpgradable) -> DragUpdated", true)]
        dragUpdated = 9,
        [Obsolete("Use DragPerform instead (UnityUpgradable) -> DragPerform", true)]
        dragPerform = 10,
        [Obsolete("Use Ignore instead (UnityUpgradable) -> Ignore", true)]
        ignore = 11,
        [Obsolete("Use Used instead (UnityUpgradable) -> Used", true)]
        used = 12
    }

    // Types of modifier key that can be active during a keystroke event.
    [Flags]
    public enum EventModifiers
    {
        // None
        None = 0,

        // Shift key
        Shift = 1,

        // Control key
        Control = 2,

        // Alt key
        Alt = 4,

        // Command key (Mac)
        Command = 8,

        // Num lock key
        Numeric = 16,

        // Caps lock key
        CapsLock = 32,

        // Function key
        FunctionKey = 64
    }
}
