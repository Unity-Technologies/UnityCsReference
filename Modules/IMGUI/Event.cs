// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // A UnityGUI event.
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class Event
    {
        public Event()
        {
            m_Ptr = Internal_Create(0);
        }

        public Event(int displayIndex)
        {
            m_Ptr = Internal_Create(displayIndex);
        }

        // Copy an event
        public Event(Event other)
        {
            if (other == null)
                throw new ArgumentException("Event to copy from is null.");
            m_Ptr = Internal_Copy(other.m_Ptr);
        }

        ~Event()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        internal static void CleanupRoots()
        {
            // Required for application quite, so we can force GC to collect root objects before Unity managers are destroyed
            s_Current = null;
            s_MasterEvent = null;
        }

        [NonSerialized]
        internal IntPtr m_Ptr;

        internal void CopyFrom(Event e)
        {
            // Copies the event data without allocating a new event on the native side.
            if (e.m_Ptr != m_Ptr)
            {
                CopyFromPtr(e.m_Ptr);
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
        public bool functionKey => (modifiers & EventModifiers.FunctionKey)  != 0;

        // The current event that's being processed right now.
        // TODO: set this to null outside the event loop.
        //
        public static Event current
        {
            get
            {
                // return null if Event.current is queried outside OnGUI
                // Only in editor because of backwards compatible.
                if (GUIUtility.guiDepth > 0)
                    return s_Current;
                else
                    return null;
            }
            set
            {
                s_Current = value ?? s_MasterEvent;
                Internal_SetNativeEvent(s_Current.m_Ptr);
            }
        }
        static Event s_Current;
        static Event s_MasterEvent;

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

        // Is this event comes from a direct manipulation device?
        // A direct manipulation device is a device where the user directly manipulates elements
        // (like a touch screen), without any cursor acting as an intermediate.
        internal bool isDirectManipulationDevice
        {
            get
            {
                return pointerType == PointerType.Pen
                    || pointerType == PointerType.Touch;
            }
        }

        // Create a keyboard event.
        public static Event KeyboardEvent(string key)
        {
            Event evt = new Event(0) {type = EventType.KeyDown};
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
            string subStr = key.Substring(startIdx, key.Length - startIdx).ToLowerInvariant();
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
                case "f16":         evt.keyCode = KeyCode.F16; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f17":         evt.keyCode = KeyCode.F17; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f18":         evt.keyCode = KeyCode.F18; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f19":         evt.keyCode = KeyCode.F19; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f20":         evt.keyCode = KeyCode.F20; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f21":         evt.keyCode = KeyCode.F21; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f22":         evt.keyCode = KeyCode.F22; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f23":         evt.keyCode = KeyCode.F23; evt.modifiers |= EventModifiers.FunctionKey; break;
                case "f24":         evt.keyCode = KeyCode.F24; evt.modifiers |= EventModifiers.FunctionKey; break;
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
                        if (evt.modifiers != 0)
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
                if (character == 0)
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
                Debug.LogWarning(UnityString.Format("Event.Use() should not be called for events of type {0}", type));
            Internal_Use();
        }
    }
}
