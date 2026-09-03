// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // A UnityGUI event.
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class Event
    {
        ///<exclude />
        public Event()
        {
            m_Ptr = Internal_Create(0);
        }

        public Event(int displayIndex)
        {
            m_Ptr = Internal_Create(displayIndex);
        }

        // Copy an event
        ///<exclude />
        public Event(Event other)
        {
            if (other == null)
                throw new ArgumentException("Event to copy from is null.");
            m_Ptr = Internal_Copy(other.m_Ptr);
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~Event()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }
#pragma warning restore UA5000

        ///<exclude />
        [NonSerialized]
        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal IntPtr m_Ptr;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void CopyFrom(Event e)
        {
            // Copies the event data without allocating a new event on the native side.
            if (e.m_Ptr != m_Ptr)
            {
                CopyFromPtr(e.m_Ptr);
            }
        }

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);", true)]
        public Ray mouseRay { get { return new Ray(Vector3.up, Vector3.up); } set {}}

        ///<summary>Is Shift held down? (RO)</summary>
        ///<remarks>Returns true if any Shift key is held down.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects if the shift key was pressed
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.shift)
        ///        {
        ///            Debug.Log("Shift was pressed :O");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool shift
        {
            get { return (modifiers & EventModifiers.Shift)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Shift; else modifiers |= EventModifiers.Shift; }
        }

        ///<summary>Is Control key held down? (RO)</summary>
        ///<remarks>Returns true if any Control key is held down.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.control)
        ///        {
        ///            Debug.Log("Control was pressed.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool control
        {
            get {return (modifiers & EventModifiers.Control)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Control; else modifiers |= EventModifiers.Control; }
        }

        ///<summary>Is Alt/Option key held down? (RO)</summary>
        ///<remarks>On Windows, this returns true if any Alt key is held down. 
        ///
        ///On Mac, this returns true if any Option key is held down.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints Option or Alt key was pressed depending on the
        ///    // platform where this script is running.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.alt)
        ///        {
        ///            if (Application.platform == RuntimePlatform.OSXEditor)
        ///            {
        ///                Debug.Log("Option key was pressed");
        ///            }
        ///            else if (Application.platform == RuntimePlatform.WindowsEditor)
        ///            {
        ///                Debug.Log("Alt Key was pressed!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool alt
        {
            get { return (modifiers & EventModifiers.Alt)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Alt; else modifiers |= EventModifiers.Alt; }
        }

        ///<summary>Is Command/Windows key held down? (RO)</summary>
        ///<remarks>On Windows, this returns true if any Windows key is held down. 
        ///
        ///On Mac, this returns true if any Command key is held down.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints Command/Windows key was pressed depending on the
        ///    // platform where this script is running.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.command)
        ///        {
        ///            if (Application.platform == RuntimePlatform.OSXEditor)
        ///            {
        ///                Debug.Log("Command key was pressed");
        ///            }
        ///            else if (Application.platform == RuntimePlatform.WindowsEditor)
        ///            {
        ///                Debug.Log("Windows Key was pressed!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool command
        {
            get { return (modifiers & EventModifiers.Command)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Command; else modifiers |= EventModifiers.Command; }
        }

        ///<summary>Is Caps Lock on? (RO)</summary>
        ///<remarks>Returns true if Caps Lock is switched on.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Creates a Label and prints CapsLock on/off
        ///    // depending on the state of the capslock key.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.capsLock)
        ///        {
        ///            GUI.Label(new Rect(10, 10, 100, 20), "CapsLock on.");
        ///        }
        ///        else
        ///        {
        ///            GUI.Label(new Rect(10, 10, 100, 20), "CapsLock off.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool capsLock
        {
            get { return (modifiers & EventModifiers.CapsLock)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.CapsLock; else modifiers |= EventModifiers.CapsLock; }
        }

        ///<summary>Is the current keypress on the numeric keyboard? (RO)</summary>
        ///<remarks>Use this flag to destinguish between main &amp; numeric keys.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Creates a Label and prints Numeric Key pad is on/off
        ///    // depending on the state of the numlock key.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.numeric)
        ///        {
        ///            GUI.Label(new Rect(10, 10, 150, 20), "Numeric Key pad is on");
        ///        }
        ///        else
        ///        {
        ///            GUI.Label(new Rect(10, 10, 150, 20), "Numeric Key pad is off");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool numeric
        {
            get { return (modifiers & EventModifiers.Numeric)  != 0; }
            set { if (!value) modifiers &= ~EventModifiers.Numeric; else modifiers |= EventModifiers.Numeric; }
        }

        ///<summary>Is the current keypress a function key? (RO)</summary>
        ///<remarks>Returns true if the current keypress is an arrow key, page up, page down, backspace, etc. key.
        ///If this key needs special processing in order to work in text editing, <c>functionKey</c> is on.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects if a function Key was pressed. If a
        ///    // function key was pressed, prints its key code.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.functionKey)
        ///        {
        ///            Debug.Log("Pressed: " + e.keyCode);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool functionKey => (modifiers & EventModifiers.FunctionKey)  != 0;

        // The magnitude of Event.delta that corresponds to exactly one tick of the scroll wheel.
        internal const float scrollWheelDeltaPerTick = 3.0f;

        // The current event that's being processed right now.
        // TODO: set this to null outside the event loop.
        //

        ///<summary>The current event that's being processed right now.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Debug.Log("Current detected event: " + Event.current);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Event current
        {
            // The event currently being processed. This is IMGUI ambient state, so it is provided by the IMGUI
            // module through the currentGetter/currentSetter hooks (see EventIMGUIBindings). When the IMGUI
            // module is absent the getter returns null and the setter is a no-op.
            
            get => currentGetter?.Invoke();
            set => currentSetter?.Invoke(value);
        }

        ///<summary>Is this event a keyboard event? (RO)</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects any keyboard event
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isKey)
        ///        {
        ///            Debug.Log("Detected a keyboard event!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool isKey
        {
            get { EventType t = type; return t == EventType.KeyDown || t == EventType.KeyUp; }
        }

        ///<summary>Is this event a mouse event? (RO)</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects any mouse event
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isMouse)
        ///        {
        ///            Debug.Log("Detected a mouse event!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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
        ///<exclude />
        public bool isScrollWheel
        {
            get { EventType t = type; return t == EventType.ScrollWheel; }
        }

        // Is this event comes from a direct manipulation device?
        // A direct manipulation device is a device where the user directly manipulates elements
        // (like a touch screen), without any cursor acting as an intermediate.
        internal bool isDirectManipulationDevice
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
            get
            {
                return pointerType == PointerType.Pen
                    || pointerType == PointerType.Touch;
            }
        }

        ///<summary>Create a keyboard event.</summary>
        ///<remarks>This is useful when you need to check if a certain key has been pressed - possibly with modifiers. The syntax for the key string is a key name
        ///(same as in the Input Manager), optionally prefixed by any number of modifiers: 
        ///
        ///&amp; = Alternate, ^ = Control, % = Command/Windows key, # = Shift 
        ///
        ///Examples: &amp;f12 = Alternate + F12,    "^[0]" = Control + keypad0 .
        ///
        ///
        ///See the [Input Manager](xref:class-InputManager) manual page for more information on key names.</remarks>
        ///<param name="key">A string representing keyboard keys and modifiers.</param>
        ///<returns>A new Event with <see cref="EventType.KeyDown" /> and the requested <see cref="KeyCode" /> and optional <see cref="EventModifiers" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects if the Enter key was pressed
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Label("Press Enter To Start Game");
        ///
        ///        if (Event.current.Equals(Event.KeyboardEvent("[enter]")))
        ///        {
        ///            Application.LoadLevel(1);
        ///        }
        ///
        ///        if (Event.current.Equals(Event.KeyboardEvent("return")))
        ///        {
        /// 		Debug.Log("I said enter, not return - try the keypad");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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
                            Debug.LogError(string.Format("Unable to find key name that matches '{0}'", subStr));
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
                    return string.Format("Event:{0}   Character:\\0   Modifiers:{1}   KeyCode:{2}", type, modifiers, keyCode);

                return "Event:" + type + "   Character:" + (int)(character) + "   Modifiers:" + modifiers + "   KeyCode:" + keyCode;
            }
            if (isMouse)
                return string.Format("Event: {0}   Position: {1} Modifiers: {2}", type, mousePosition, modifiers);

            if (type == EventType.ExecuteCommand || type == EventType.ValidateCommand)
                return string.Format("Event: {0}  \"{1}\"", type, commandName);

            return "" + type;
        }

        ///<summary>Use this event.</summary>
        ///<remarks>
        ///  <para>Call this method when you've used an event. The event's type will be set to <see cref="EventType.Used" />, causing other GUI elements to ignore it.
        ///
        ///Events of type <see cref="EventType.Repaint" /> and <see cref="EventType.Layout" /> should not be used.
        ///Attempting to call this method on such events will issue a warning.
        ///
        ///The following example demonstrates how events are consumed and used up. Copy this code into a script, and open the Example Window this sample creates from the Window menu.</para>
        ///  <para>The following example demonstrates how handles such as <see cref="M:UnityEditor.Handles.PositionHandle" /> and <see cref="M:UnityEditor.Handles.FreeMoveHandle" /> might use events.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEditor;
        ///using UnityEngine;
        ///
        ///public class ExampleWindow : EditorWindow
        ///{
        ///    [MenuItem("Window/Show Example Window")]
        ///    public static void ShowWindow()
        ///    {
        ///        GetWindow(typeof(ExampleWindow));
        ///    }
        ///
        ///    private void OnGUI()
        ///    {
        ///        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        ///        {
        ///            Debug.Log("Left clicked at: " + Event.current.mousePosition);
        ///            // This if statement Uses up the current MouseDown event so that
        ///            // subsequent code or GUI elements ignore this MouseDown event. 
        ///            Event.current.Use();
        ///        }
        ///
        ///        // This if statement does not check Event.current.button, but it only triggers
        ///        // when Event.current.button is not 0 because the previous if statement will
        ///        // Use up the MouseDown event if it is. 
        ///        if (Event.current.type == EventType.MouseDown) 
        ///        {
        ///            Debug.Log("This only prints when we right click!");
        ///            Event.current.Use();
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using UnityEditor;
        ///using UnityEngine;
        ///
        ///public static class CustomHandle
        ///{
        ///    public static bool DoHandle(Vector3 worldpos, float size, float pickSize)
        ///    {
        ///        int id = GUIUtility.GetControlID(FocusType.Passive);
        ///        Event evt = Event.current;
        ///
        ///        bool clicked = false;
        ///
        ///        switch (evt.GetTypeForControl(id))
        ///        {
        ///            case EventType.MouseDown:
        ///                if (evt.button == 0 && HandleUtility.nearestControl == id)
        ///                {
        ///                    GUIUtility.hotControl = id;
        ///
        ///                    evt.Use(); // Using the MouseDown event
        ///                    clicked = true;
        ///                }
        ///                break;
        ///
        ///            case EventType.MouseMove:
        ///                HandleUtility.Repaint(); 
        ///                evt.Use(); // Using the MouseMove event
        ///                break;
        ///
        ///            case EventType.MouseUp:
        ///                if (evt.button == 0 && HandleUtility.nearestControl == id)
        ///                {
        ///                    GUIUtility.hotControl = 0;
        ///                    evt.Use(); // Using the MouseUp event
        ///                }
        ///                break;
        ///
        ///            case EventType.Layout:
        ///                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldpos, pickSize));
        ///                // Keep in mind Layout events should not be Used!
        ///                break;
        ///
        ///            case EventType.Repaint:
        ///                // Draw the handle here
        ///                // Keep in mind Repaint events should not be Used!
        ///                break;
        ///        }
        ///
        ///        return clicked;
        ///    }
        ///}]]></code>
        ///</example>
        public void Use()
        {
            if (type == EventType.Repaint || type == EventType.Layout)
                Debug.LogWarning(string.Format("Event.Use() should not be called for events of type {0}", type));
            Internal_Use();
        }
    }
}
