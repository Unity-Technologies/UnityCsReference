// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // A UnityGUI event.
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class Event
    {
        // *undocumented
        public Event()
        {
            Init(0);
        }

        public Event(int displayIndex)
        {
            Init(displayIndex);
        }

        // Copy an event
        public Event(Event other)
        {
            if (other == null)
                throw new ArgumentException("Event to copy from is null.");
            InitCopy(other);
        }

        // *undocumented
        private Event(IntPtr ptr)
        {
            InitPtr(ptr);
        }

        // *undocumented
        ~Event()
        {
            Cleanup();
        }

        static internal void CleanupRoots()
        {
            // Required for application quite, so we can force GC to collect root objects before Unity managers are destroyed
            s_Current = null;
            s_MasterEvent = null;
        }

        [NonSerialized]
        internal IntPtr m_Ptr;

        // The mouse position.
        public Vector2 mousePosition
        {
            get { Vector2 tmp; Internal_GetMousePosition(out tmp); return tmp; }
            set { Internal_SetMousePosition(value); }
        }

        // The relative movement of the mouse compared to last event.
        public Vector2 delta
        {
            get { Vector2 tmp; Internal_GetMouseDelta(out tmp); return tmp; }
            set { Internal_SetMouseDelta(value); }
        }

        [Obsolete("Use HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);", true)]
        public Ray mouseRay { get { return new Ray(Vector3.up, Vector3.up); } set {}}

        // Is Shift held down? (RO)
        public bool shift
        {
            get { return (modifiers & EventModifiers.Shift)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Shift; else modifiers |= EventModifiers.Shift; }
        }

        // Is Control key held down? (RO)
        public bool control
        {
            get {return (modifiers & EventModifiers.Control)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Control; else modifiers |= EventModifiers.Control; }
        }

        // Is Alt/Option key held down? (RO)
        public bool alt
        {
            get { return (modifiers & EventModifiers.Alt)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Alt; else modifiers |= EventModifiers.Alt; }
        }

        // Is Command/Windows key held down? (RO)
        public bool command
        {
            get { return (modifiers & EventModifiers.Command)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Command; else modifiers |= EventModifiers.Command; }
        }

        // Is Caps Lock on? (RO)
        public bool capsLock
        {
            get { return (modifiers & EventModifiers.CapsLock)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.CapsLock; else modifiers |= EventModifiers.CapsLock; }
        }

        // Is the current keypress on the numeric keyboard? (RO)
        public bool numeric
        {
            get { return (modifiers & EventModifiers.Numeric)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Numeric; else modifiers |= EventModifiers.Numeric; }
        }

        // Is the current keypress a function key? (RO)
        public bool functionKey { get { return (modifiers & EventModifiers.FunctionKey)  != 0; } }

        // The current event that's being processed right now.
        // TODO: set this to null outside the event loop.
        //
        public static Event current
        {
            get
            {
                // return null if Event.current is queried outside OnGUI
                // Only in editor because of backwards compat.
                if (GUIUtility.Internal_GetGUIDepth() > 0)
                    return s_Current;
                else
                    return null;
            }
            set
            {
                if (value != null)
                    s_Current = value;
                else
                    s_Current = s_MasterEvent;
                Internal_SetNativeEvent(s_Current.m_Ptr);
            }
        }
        static Event s_Current;
        static Event s_MasterEvent;

        [RequiredByNativeCode]
        static private void Internal_MakeMasterEventCurrent(int displayIndex)
        {
            if (s_MasterEvent == null)
                s_MasterEvent = new Event(displayIndex);
            s_MasterEvent.displayIndex = displayIndex;
            s_Current = s_MasterEvent;
            Internal_SetNativeEvent(s_MasterEvent.m_Ptr);
        }

        // Is this event a keyboard event? (RO)
        public bool isKey
        {
            get { EventType t = type; return t == EventType.KeyDown || t == EventType.KeyUp; }
        }

        // Is this event a mouse event? (RO)
        public bool isMouse
        {
            get
            {
                EventType t = type;
                return t == EventType.MouseMove
                    || t == EventType.MouseDown
                    || t == EventType.MouseUp
                    || t == EventType.MouseDrag
                    || t == EventType.ContextClick
                    || t == EventType.MouseEnterWindow
                    || t == EventType.MouseLeaveWindow;
            }
        }

        // Is this event a scroll wheel event? (RO)
        public bool isScrollWheel
        {
            get { EventType t = type; return t == EventType.ScrollWheel; }
        }

        // Create a keyboard event.
        public static Event KeyboardEvent(string key)
        {
            Event evt = new Event(0);
            evt.type = EventType.KeyDown;
            // Can't use string.IsNullOrEmpty because it's not supported in NET 1.1
            if (string.IsNullOrEmpty(key))
                return evt;
            int startIdx = 0;
            bool found = false;
            do
            {
                found = true;
                if (startIdx >= key.Length)
                {
                    found = false; break;
                }
                switch (key[startIdx])
                {
                    case '&': // Alt
                        evt.modifiers |= EventModifiers.Alt; startIdx++;
                        break;
                    case '^': // Ctrl
                        evt.modifiers |= EventModifiers.Control; startIdx++;
                        break;
                    case '%':
                        evt.modifiers |= EventModifiers.Command; startIdx++;
                        break;
                    case '#':
                        evt.modifiers |= EventModifiers.Shift; startIdx++;
                        break;
                    default:
                        found = false;
                        break;
                }
            }
            while (found);
            string subStr = key.Substring(startIdx, key.Length - startIdx).ToLower();
            switch (subStr)
            {
                case "[0]":         evt.character = '0'; evt.keyCode = KeyCode.Keypad0; break;
                case "[1]":         evt.character = '1'; evt.keyCode = KeyCode.Keypad1; break;
                case "[2]":         evt.character = '2'; evt.keyCode = KeyCode.Keypad2; break;
                case "[3]":         evt.character = '3'; evt.keyCode = KeyCode.Keypad3; break;
                case "[4]":         evt.character = '4'; evt.keyCode = KeyCode.Keypad4; break;
                case "[5]":         evt.character = '5'; evt.keyCode = KeyCode.Keypad5; break;
                case "[6]":         evt.character = '6'; evt.keyCode = KeyCode.Keypad6; break;
                case "[7]":         evt.character = '7'; evt.keyCode = KeyCode.Keypad7; break;
                case "[8]":         evt.character = '8'; evt.keyCode = KeyCode.Keypad8; break;
                case "[9]":         evt.character = '9'; evt.keyCode = KeyCode.Keypad9; break;
                case "[.]":         evt.character = '.'; evt.keyCode = KeyCode.KeypadPeriod; break;
                case "[/]":         evt.character = '/'; evt.keyCode = KeyCode.KeypadDivide; break;
                case "[-]":         evt.character = '-'; evt.keyCode = KeyCode.KeypadMinus; break;
                case "[+]":         evt.character = '+'; evt.keyCode = KeyCode.KeypadPlus; break;
                case "[=]":         evt.character = '='; evt.keyCode = KeyCode.KeypadEquals; break;
                case "[equals]":    evt.character = '='; evt.keyCode = KeyCode.KeypadEquals; break;
                case "[enter]":     evt.character = '\n'; evt.keyCode = KeyCode.KeypadEnter; break;
                case "up":          evt.keyCode = KeyCode.UpArrow; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "down":        evt.keyCode = KeyCode.DownArrow; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "left":        evt.keyCode = KeyCode.LeftArrow; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "right":       evt.keyCode = KeyCode.RightArrow; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "insert":      evt.keyCode = KeyCode.Insert; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "home":        evt.keyCode = KeyCode.Home; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "end":         evt.keyCode = KeyCode.End; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "pgup":        evt.keyCode = KeyCode.PageDown; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "page up":     evt.keyCode = KeyCode.PageUp; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "pgdown":      evt.keyCode = KeyCode.PageUp; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "page down":   evt.keyCode = KeyCode.PageDown; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "backspace":   evt.keyCode = KeyCode.Backspace; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "delete":      evt.keyCode = KeyCode.Delete; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "tab":         evt.keyCode = KeyCode.Tab; break;
                case "f1":          evt.keyCode = KeyCode.F1; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f2":          evt.keyCode = KeyCode.F2; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f3":          evt.keyCode = KeyCode.F3; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f4":          evt.keyCode = KeyCode.F4; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f5":          evt.keyCode = KeyCode.F5; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f6":          evt.keyCode = KeyCode.F6; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f7":          evt.keyCode = KeyCode.F7; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f8":          evt.keyCode = KeyCode.F8; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f9":          evt.keyCode = KeyCode.F9; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f10":         evt.keyCode = KeyCode.F10; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f11":         evt.keyCode = KeyCode.F11; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f12":         evt.keyCode = KeyCode.F12; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f13":         evt.keyCode = KeyCode.F13; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f14":         evt.keyCode = KeyCode.F14; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f15":         evt.keyCode = KeyCode.F15; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "[esc]":       evt.keyCode = KeyCode.Escape; break;
                case "return":      evt.character = '\n'; evt.keyCode = KeyCode.Return; evt.modifiers &= ~EventModifiers.FunctionKey; break;
                case "space":       evt.keyCode = KeyCode.Space; evt.character = ' '; evt.modifiers &= ~EventModifiers.FunctionKey; break;
                default:
                    if (subStr.Length != 1)
                    {
                        try
                        {
                            evt.keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), subStr, true);
                        }
                        catch (ArgumentException)
                        {
                            Debug.LogError(UnityString.Format("Unable to find key name that matches '{0}'", subStr));
                        }
                    }
                    else
                    {
                        evt.character = subStr.ToLower()[0];
                        evt.keyCode = (KeyCode)evt.character;
                        if ((int)evt.modifiers != 0)
                            evt.character = (char)0;
                    }
                    break;
            }
            return evt;
        }

        // Calculate the hash code
        public override int GetHashCode()
        {
            int hc = 1;
            if (isKey)
                hc =  (ushort)keyCode;
            if (isMouse)
                hc = mousePosition.GetHashCode();
            hc = hc * 37 | (int)modifiers;
            //      Debug.Log (hc + "  GetHashCode of " + ToString());
            return hc;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            Event rhs = (Event)obj;
            // We are ignoring Caps Lock for modifiers, so that Key Combinations will still work when Caps Lock is down.
            if (type != rhs.type || (modifiers & ~EventModifiers.CapsLock) != (rhs.modifiers & ~EventModifiers.CapsLock))
                return false;
            if (isKey)
                return keyCode == rhs.keyCode;
            if (isMouse)
                return mousePosition == rhs.mousePosition;
            return false;
        }

        public override string ToString()
        {
            if (isKey)
            {
                if ((int)character == 0)
                    return UnityString.Format("Event:{0}   Character:\\0   Modifiers:{1}   KeyCode:{2}", type, modifiers, keyCode);

                return "Event:" + type + "   Character:" + (int)(character) + "   Modifiers:" + modifiers + "   KeyCode:" + keyCode;
            }
            if (isMouse)
                return UnityString.Format("Event: {0}   Position: {1} Modifiers: {2}", type, mousePosition, modifiers);

            if (type == EventType.ExecuteCommand || type == EventType.ValidateCommand)
                return UnityString.Format("Event: {0}  \"{1}\"", type, commandName);

            return "" + type;
        }

        // Use this event.
        public void Use()
        {
            if (type == EventType.Repaint || type == EventType.Layout)
            {
                Debug.LogWarning(UnityString.Format("Event.Use() should not be called for events of type {0}", type));
            }
            Internal_Use();
        }
    }
}
