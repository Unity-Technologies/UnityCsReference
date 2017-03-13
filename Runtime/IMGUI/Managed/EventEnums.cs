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

        //Planned obsolence
        //TODO: configure the ScriptUpdater!
        mouseDown = 0,
        mouseUp = 1,
        mouseMove = 2,
        mouseDrag = 3,
        keyDown = 4,
        keyUp = 5,
        scrollWheel = 6,
        repaint = 7,
        layout = 8,
        dragUpdated = 9,
        dragPerform = 10,
        ignore = 11,
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
