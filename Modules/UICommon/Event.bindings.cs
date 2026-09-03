// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>A UnityGUI event.</summary>
    ///<remarks>Events correspond to user input (key presses, mouse actions), or are UnityGUI layout or rendering events.
    ///
    ///For each event <see cref="M:UnityEngine.MonoBehaviour.OnGUI" /> is called in the scripts; so OnGUI is potentially
    ///called multiple times per frame.  <see cref="Event.current" /> corresponds to "current" event inside OnGUI call.</remarks>
    ///<seealso href="xref:comp:GUI Scripting Guide">GUI Scripting Guide</seealso>
    ///<seealso cref="EventType" />
    [NativeHeader("Modules/UICommon/Event.bindings.h"),
     StaticAccessor("GUIEvent", StaticAccessorType.DoubleColon)]
    public sealed partial class Event
    {
        // These functions are set if the IMGUI module is present (see EventIMGUIBindings.cs)
        // Each hook is null when the IMGUI module is absent, in which case Event falls back to standalone behavior (current => null,
        // type => rawType), so Event is fully usable without IMGUI

        [NoAutoStaticsCleanup]
        internal static Func<Event> currentGetter { get; [VisibleToOtherModules("UnityEngine.IMGUIModule")] set; }

        [NoAutoStaticsCleanup]
        internal static Action<Event> currentSetter { get; [VisibleToOtherModules("UnityEngine.IMGUIModule")] set; }

        [NoAutoStaticsCleanup]
        internal static Func<IntPtr, EventType> typeResolver { get; [VisibleToOtherModules("UnityEngine.IMGUIModule")] set; }

        [NoAutoStaticsCleanup]
        internal static Func<IntPtr, int, EventType> typeForControlResolver { get; [VisibleToOtherModules("UnityEngine.IMGUIModule")] set; }
        
        ///<exclude />
        [NativeProperty("type", false, TargetType.Field)] public extern EventType rawType { get; }
        ///<summary>The mouse position.</summary>
        ///<remarks>Used in <see cref="EventType.MouseMove" /> and <see cref="EventType.MouseDrag" /> events.  The top-left of the window returns (0, 0).  The bottom-right returns (Screen.width, Screen.height).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // print the mousePosition every 10th of a second
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private float range = 0.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        range = range + Time.deltaTime;
        ///
        ///        if (range > 0.1f)
        ///        {
        ///            Event e = Event.current;
        ///            Debug.Log(e.mousePosition);
        ///            range = 0.0f;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Event.delta" />
        [NativeProperty("mousePosition", false, TargetType.Field)] public extern Vector2 mousePosition { get; set; }
        ///<summary>The relative movement of the mouse compared to last event.</summary>
        ///<remarks>Used in <see cref="EventType.MouseMove" />, <see cref="EventType.MouseDrag" />, <see cref="EventType.ScrollWheel" /> events.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Move the scroll wheel to determine
        ///    // the X & Y scrolling amount.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isMouse)
        ///        {
        ///            Debug.Log(e.delta);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Event.mousePosition" />
        [NativeProperty("delta", false, TargetType.Field)] public extern Vector2 delta { get; set; }
        ///<summary>The type of pointer that created this event (for example, mouse, touch screen, pen).</summary>
        ///<remarks>When a user uses a pen, some mouse events are often mixed with pen events in the event stream, and you can't distinguish them by type because mouse and pen events share the same <see cref="EventType" />. Instead, use <see cref="PointerType" /> to distinguish them. Otherwise, Unity processes all incoming mouse events as pen events, which can lead to unexpected behavior because the mouse events (pointerType = Mouse) do not have pen event fields, like <see cref="PenStatus" />, set.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event m_Event = Event.current;
        ///
        ///        if (m_Event.type == EventType.MouseDown)
        ///        {
        ///            if (m_Event.pointerType == PointerType.Pen)     //Check if it's a pen event.
        ///                Debug.Log("Pen Down.");
        ///            else 
        ///                Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        [NativeProperty("pointerType", false, TargetType.Field)] public extern PointerType pointerType { get; set; }
        ///<summary>Which mouse button was pressed.</summary>
        ///<remarks>0 means left mouse button, 1 means right mouse button, 2 means middle mouse button.
        ///Used in <see cref="EventType.MouseDown" /> and <see cref="EventType.MouseUp" /> events.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detect which mouse button is currently pressed
        ///    // and print it.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.button == 0 && e.isMouse)
        ///        {
        ///            Debug.Log("Left Click");
        ///        }
        ///        else if (e.button == 1)
        ///        {
        ///            Debug.Log("Right Click");
        ///        }
        ///        else if (e.button == 2)
        ///        {
        ///            Debug.Log("Middle Click");
        ///        }
        ///        else if (e.button > 2)
        ///        {
        ///            Debug.Log("Another button in the mouse clicked");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("button", false, TargetType.Field)] public extern int button { get; set; }
        ///<summary>Which modifier keys are held down.</summary>
        [NativeProperty("modifiers", false, TargetType.Field)] public extern EventModifiers modifiers { get; set; }
        ///<summary>How hard pen pressure is applied, normalized between 0 (no pressure) and 1 (maximum pressure).</summary>
        ///<remarks>Pressure is always 1 on devices where pressure is not supported.</remarks>
        [NativeProperty("pressure", false, TargetType.Field)] public extern float pressure { get; set; }
        ///<summary>Specifies the rotation of the pen around its axis, expressed in radians. The default value is 0.</summary>
        [NativeProperty("twist", false, TargetType.Field)] public extern float twist { get; set; }
        ///<summary>Specifies the angle of the pen relative to the X and Y axes, expressed in radians.</summary>
        [NativeProperty("tilt", false, TargetType.Field)] public extern Vector2 tilt { get; set; }
        ///<summary>Specifies the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed.</summary>
        ///<remarks>On macOS, penStatus will not reflect changes to button mappings.
        ///
        ///Before you process an event as a pen event, you should check the <see cref="PointerType" /> of a mouse event (e.g. <see cref="EventType.MouseDown" />). 
        ///
        ///When a user uses a pen, some mouse events are often mixed with pen events in the event stream, and you can't distinguish them by type because mouse and pen events share the same <see cref="EventType" />. Instead, use <see cref="PointerType" /> to distinguish them. Otherwise, Unity processes all incoming mouse events as pen events, which can lead to unexpected behavior because the mouse events (pointerType = Mouse) do not have pen event fields, like <see cref="PenStatus" />, set.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event m_Event = Event.current;
        ///
        ///        if (m_Event.type == EventType.MouseDown)
        ///        {
        ///            if (m_Event.pointerType == PointerType.Pen)     //Check if it's a pen event.
        ///            {
        ///                if (m_Event.penStatus == PenStatus.Contact)
        ///                    Debug.Log("Pen is in contact with screen or tablet.");
        ///            }
        ///        else
        ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        [NativeProperty("penStatus", false, TargetType.Field)] public extern PenStatus penStatus { get; set; }
        ///<summary>How many consecutive mouse clicks have we received.</summary>
        ///<remarks>Used in <see cref="EventType.MouseDown" /> event; use this to differentiate between a single and double clicks.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isMouse)
        ///        {
        ///            Debug.Log("Mouse clicks: " + e.clickCount);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("clickCount", false, TargetType.Field)] public extern int clickCount { get; set; }
        ///<summary>The character typed.</summary>
        ///<remarks>Used in <see cref="EventType.KeyDown" /> event. Note that <see cref="EventType.KeyUp" /> events might not contain
        ///a character, only <see cref="Event.keyCode" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isKey)
        ///        {
        ///            Debug.Log("Detected character: " + e.character);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Event.keyCode" />
        [NativeProperty("character", false, TargetType.Field)] public extern char character { get; set; }
        [NativeProperty("keycode", false, TargetType.Field)] extern KeyCode Internal_keyCode { get; set; }
        ///<summary>The raw key code for keyboard events.</summary>
        ///<remarks>Used in <see cref="EventType.KeyDown" /> and <see cref="EventType.KeyUp" /> events; this returns <see cref="KeyCode" /> value
        ///that matches the physical keyboard key. Use this for handling cursor keys,
        ///function keys etc.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Detects keys pressed and prints their keycode
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///        if (e.isKey)
        ///        {
        ///            Debug.Log("Detected key code: " + e.keyCode);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Event.character" />
        public KeyCode keyCode
        {
            get
            {
                var key = isMouse ? KeyCode.Mouse0 + button : Internal_keyCode;

                if(isScrollWheel)
                    key = delta.y < 0 || delta.y == 0 && delta.x < 0 ? KeyCode.WheelUp : KeyCode.WheelDown;

                return key;
            }
            set => Internal_keyCode = value;
        }
        ///<summary>Index of display that the event belongs to.</summary>
        ///<remarks>Not all platforms support multi-display GUI. On such platforms this property is always zero.</remarks>
        [NativeProperty("displayIndex", false, TargetType.Field)] public extern int displayIndex { get; set; }

        ///<summary>The type of event.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the current event detected.
        ///    void OnGUI()
        ///    {
        ///        Debug.Log("Current event detected: " + Event.current.type);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EventType" />
        ///<seealso href="xref:GUI Scripting Guide">GUI Scripting Guide</seealso>

        // The event type, resolved against IMGUI's active GUIState (control-aware) when IMGUI is present;
        // otherwise the raw stored type. The setter writes the raw field directly (core, no IMGUI state).
        public EventType type
        {
            get => typeResolver?.Invoke(m_Ptr) ?? rawType;
            set => SetTypeInternal(value);
        }
        
        [FreeFunction("GUIEvent::SetType", HasExplicitThis = true)]
        private extern void SetTypeInternal(EventType value);

        ///<summary>The name of an ExecuteCommand or ValidateCommand Event.</summary>
        ///<remarks>Available commands are:
        ///
        ///"Copy", "Cut", "Paste",
        ///"Delete", "SoftDelete", "Duplicate",
        ///"FrameSelected", "FrameSelectedWithLock",
        ///"SelectAll", "Find" and "FocusProjectWindow".
        ///
        ///Sent only in the editor.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class EventCmdNameExample : MonoBehaviour
        ///{
        ///    // Detects commands executed and prints them.
        ///    void OnGUI()
        ///    {
        ///        Event e = Event.current;
        ///
        ///        if (e.commandName != "")
        ///            Debug.Log("Command recognized: " + e.commandName);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EventType.ExecuteCommand" />
        ///<seealso cref="EventType.ValidateCommand" />
        public extern string commandName
        {
            [FreeFunction("GUIEvent::GetCommandName", HasExplicitThis = true)] get;
            [FreeFunction("GUIEvent::SetCommandName", HasExplicitThis = true)] set;
        }

        [NativeMethod("Use")]
        private extern void Internal_Use();

        [FreeFunction("GUIEvent::Internal_Create", IsThreadSafe = true)]
        private static extern IntPtr Internal_Create(int displayIndex);

        [FreeFunction("GUIEvent::Internal_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("GUIEvent::Internal_Copy", IsThreadSafe = true)]
        private static extern IntPtr Internal_Copy(IntPtr otherPtr);

        ///<summary>Get a filtered event type for a given control ID.</summary>
        ///<remarks>This function is used to implement mouse locking and keyboard focus.
        ///                The controlID can be obtained from <see cref="M:UnityEngine.GUIUtility.GetControlID" />.</remarks>
        ///<param name="controlID">The ID of the control you are querying from.</param>
        ///<seealso cref="EventType" />
        public EventType GetTypeForControl(int controlID)
        {
            // GUIState-resolved type for a given control; IMGUI-provided, falls back to the raw type without IMGUI.
            return typeForControlResolver?.Invoke(m_Ptr, controlID) ?? rawType;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule"),
         FreeFunction("GUIEvent::CopyFromPtr", IsThreadSafe = true, HasExplicitThis = true)]
        internal extern void CopyFromPtr(IntPtr ptr);

        ///<summary>Get the next queued [Event] from the event system.</summary>
        ///<param name="outEvent">Next Event.</param>
        public static extern bool PopEvent([NotNull] Event outEvent);
        internal static extern void QueueEvent([NotNull] Event outEvent);
        [VisibleToOtherModules("UnityEngine.InputForUIModule")]
        internal static extern void GetEventAtIndex(int index, [NotNull] Event outEvent);
        ///<summary>Returns the current number of events that are stored in the event queue.</summary>
        ///<returns>Current number of events currently in the event queue.</returns>
        public static extern int GetEventCount();
        internal static extern void ClearEvents();
        
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.InputForUIModule")]
        internal static extern int GetDoubleClickTime();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Event e) => e.m_Ptr;
        }
    }
}
