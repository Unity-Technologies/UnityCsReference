// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    ///<summary>Types of UnityGUI input and processing events.</summary>
    ///<remarks>Use this to tell which type of event has taken place in the GUI. Types of Events include mouse clicking, mouse dragging, button pressing, the mouse entering or exiting the window, and the scroll wheel as well as others mentioned below.
    ///
    ///Events can be used to prevent other GUI elements from responding to that event. Refer to <see cref="Event.Use" />.</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// //Attach this script to a GameObject
    /// //This script is a basic overview of some of the Event Types available. It outputs messages depending on the current Event Type.
    ///
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    void OnGUI()
    ///    {
    ///        Event m_Event = Event.current;
    ///
    ///        if (m_Event.type == EventType.MouseDown)
    ///        {
    ///            Debug.Log("Mouse Down.");
    ///        }
    ///
    ///        if (m_Event.type == EventType.MouseDrag)
    ///        {
    ///            Debug.Log("Mouse Dragged.");
    ///        }
    ///
    ///        if (m_Event.type == EventType.MouseUp)
    ///        {
    ///            Debug.Log("Mouse Up.");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
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
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Event.type" />
    ///<seealso cref="Event" />
    ///<seealso href="xref:GUI Scripting Guide">GUI Scripting Guide</seealso>
    public enum EventType
    {
        ///<summary>Mouse button was pressed.</summary>
        ///<remarks>This event gets sent when any mouse button is pressed. Use <see cref="Event.button" /> to determine which button was pressed down.</remarks>
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
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EventType" />
        ///<seealso cref="Event.Use" />
        MouseDown = 0,
        ///<summary>Mouse button was released.</summary>
        ///<remarks>This event gets sent when any mouse button is released. Use <see cref="Event.button" /> to determine which button was pressed down.</remarks>
        ///<seealso cref="EventType" />
        ///<seealso cref="Event.Use" />
        MouseUp = 1,
        ///<summary>Mouse was moved (Editor views only).</summary>
        ///<remarks>The mouse was moved without any buttons being held down. Use <see cref="Event.mousePosition" /> and <see cref="Event.delta" /> to determine mouse motion.
        ///
        ///Note that this event is only sent in the Editor for <see cref="T:UnityEditor.EditorWindow" /> windows which have <see cref="P:UnityEditor.EditorWindow.wantsMouseMove" />
        ///set to true. Mouse move events are never sent in the games.</remarks>
        MouseMove = 2,
        ///<summary>Mouse was dragged.</summary>
        ///<remarks>The mouse was moved with a button held down - a mouse drag. Use <see cref="Event.mousePosition" /> and <see cref="Event.delta" /> to determine mouse motion.</remarks>
        MouseDrag = 3,
        ///<summary>A keyboard key was pressed.</summary>
        ///<remarks>Use <see cref="Event.character" /> to find out what has been typed. Use <see cref="Event.keyCode" /> to handle arrow, home/end or other function keys, or to find
        ///out which physical key has been pressed.
        ///This event is sent repeatedly depending on the end user's keyboard repeat settings.
        ///
        ///Note that key presses can come as separate events, one with valid <see cref="Event.keyCode" />, and another with valid <see cref="Event.character" />.
        ///In case of keyboard layouts with dead keys, multiple <see cref="Event.keyCode" /> events can generate a single <see cref="Event.character" /> event.</remarks>
        KeyDown = 4,
        ///<summary>A keyboard key was released.</summary>
        ///<remarks>Use <see cref="Event.keyCode" /> to find which physical key was released. Note that depending on the system
        ///and keyboard layout, <see cref="Event.character" /> might not contain any character for a key release event.</remarks>
        KeyUp = 5,
        ///<summary>The scroll wheel was moved.</summary>
        ///<remarks>Use <see cref="Event.delta" /> to determine X &amp; Y scrolling amount.</remarks>
        ScrollWheel = 6,
        ///<summary>A repaint event. One is sent every frame.</summary>
        ///<remarks>All other events are processed first, then the repaint event is sent.</remarks>
        Repaint = 7,
        ///<summary>A layout event.</summary>
        ///<remarks>This event is sent prior to anything else - this is a chance to perform any initialization.
        ///It is used by the automatic layout system.</remarks>
        Layout = 8,

        ///<summary>Editor only: drag &amp; drop operation updated.</summary>
        ///<seealso cref="T:UnityEditor.DragAndDrop" />
        DragUpdated = 9,
        ///<summary>Editor only: drag &amp; drop operation performed.</summary>
        ///<seealso cref="T:UnityEditor.DragAndDrop" />
        DragPerform = 10,
        ///<summary>Editor only: drag &amp; drop operation exited.</summary>
        ///<seealso cref="T:UnityEditor.DragAndDrop" />
        DragExited = 15,

        ///<summary>
        ///  <see cref="Event" /> should be ignored.</summary>
        ///<remarks>This event is temporarily disabled and should be ignored.</remarks>
        Ignore = 11,

        ///<summary>Already processed event.</summary>
        ///<remarks>This event has been used by some other control and should be ignored.</remarks>
        Used = 12,

        ///<summary>Validates a special command (e.g. copy &amp; paste).</summary>
        ///<remarks>
        ///  <para>"Copy", "Cut", "Paste", "Delete", "FrameSelected", "Duplicate", "SelectAll" and so on.
        ///Sent only in the editor.
        ///
        ///Example: Make pasting work in current window or control:</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEditor;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        //implement frame selection
        ///        Event e = Event.current;
        ///        if (e.type == EventType.ValidateCommand && e.commandName == "Paste")
        ///        {
        ///            Debug.Log("validate paste");
        ///            e.Use(); // without this line we won't get ExecuteCommand
        ///        }
        ///
        ///        if (e.type == EventType.ExecuteCommand && e.commandName == "Paste")
        ///        {
        ///            Debug.Log("Pasting: " + EditorGUIUtility.systemCopyBuffer);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EventType.ExecuteCommand" />
        ///<seealso cref="Event.commandName" />
        ValidateCommand = 13,

        ///<summary>Execute a special command (eg. copy &amp; paste).</summary>
        ///<remarks>
        ///  <para>"Copy", "Cut", "Paste", "Delete", "FrameSelected", "Duplicate", "SelectAll" and so on.
        ///Sent only in the editor.
        ///Example.  Checking that that a frame has the focus:</para>
        ///  <para />
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        //implement frame selection
        ///        Event e = Event.current;
        ///        if (e.type == EventType.ExecuteCommand ||
        ///            e.type == EventType.ValidateCommand)
        ///        {
        ///            if (Event.current.commandName == "FrameSelected")
        ///                Debug.Log("frame selected");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Event.commandName" />
        ///<seealso cref="EventType.ValidateCommand" />
        ExecuteCommand = 14,

        ///<summary>User has right-clicked (or control-clicked on the mac).</summary>
        ///<remarks>Window should show a context menu if applicable.
        ///Sent only in the editor.</remarks>
        ContextClick = 16,

        ///<summary>Mouse entered a window (Editor views only).</summary>
        ///<remarks>The user hovered the mouse over a window without any buttons being held down.  This event is sent once, as the mouse enters the window.
        ///
        ///Note that this event is only sent in the Editor for <see cref="T:UnityEditor.EditorWindow" /> windows which have <see cref="P:UnityEditor.EditorWindow.wantsMouseEnterLeaveWindow" />
        ///set to true. Mouse enter or leave window events are never sent in the games.</remarks>
        MouseEnterWindow = 20,
        ///<summary>Mouse left a window (Editor views only).</summary>
        ///<remarks>The user moved the mouse out of a window without any buttons being held down.  This event is sent once, as the mouse leaves the window.
        ///
        ///Note that this event is only sent in the Editor for <see cref="T:UnityEditor.EditorWindow" /> windows which have <see cref="P:UnityEditor.EditorWindow.wantsMouseEnterLeaveWindow" />
        ///set to true. Mouse enter or leave window events are never sent in the games.</remarks>
        MouseLeaveWindow = 21,

        ///<summary>Direct manipulation device (finger, pen) touched the screen.</summary>
        ///<remarks>This event gets sent when the device moves or leaves the screen.</remarks>
        TouchDown = 30,
        ///<summary>Direct manipulation device (finger, pen) left the screen.</summary>
        ///<remarks>This event gets sent when the device leaves the screen. If there was no movement, the down event will be sent right before.</remarks>
        TouchUp = 31,
        ///<summary>Direct manipulation device (finger, pen) moved on the screen (drag).</summary>
        ///<remarks>This event gets sent when the device moves. For the first move event, a down event might be sent right before.</remarks>
        TouchMove = 32,
        ///<summary>Direct manipulation device (finger, pen) moving into the window (drag).</summary>
        TouchEnter = 33,
        ///<summary>Direct manipulation device (finger, pen) moved out of the window (drag).</summary>
        TouchLeave = 34,
        ///<summary>Direct manipulation device (finger, pen) stationary event (long touch down).</summary>
        TouchStationary = 35,

        ///<summary>An event that is called when the mouse is clicked.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject
        /// //This script is a basic overview of some of the Event Types available. It outputs messages depending on the current Event Type.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event m_Event = Event.current;
        ///
        ///        if (m_Event.type == EventType.MouseDown)
        ///        {
        ///            Debug.Log("Mouse Down.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseDrag)
        ///        {
        ///            Debug.Log("Mouse Dragged.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseUp)
        ///        {
        ///            Debug.Log("Mouse Up.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use MouseDown instead (UnityUpgradable) -> MouseDown", true)]
        mouseDown = 0,
        ///<summary>An event that is called when the mouse is no longer being clicked.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject
        /// //This script is a basic overview of some of the Event Types available. It outputs messages depending on the current Event Type.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event m_Event = Event.current;
        ///
        ///        if (m_Event.type == EventType.MouseDown)
        ///        {
        ///            Debug.Log("Mouse Down.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseDrag)
        ///        {
        ///            Debug.Log("Mouse Dragged.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseUp)
        ///        {
        ///            Debug.Log("Mouse Up.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use MouseUp instead (UnityUpgradable) -> MouseUp", true)]
        mouseUp = 1,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use MouseMove instead (UnityUpgradable) -> MouseMove", true)]
        mouseMove = 2,
        ///<summary>An event that is called when the mouse is clicked and dragged.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject
        /// //This script is a basic overview of some of the Event Types available. It outputs messages depending on the current Event Type.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        Event m_Event = Event.current;
        ///
        ///        if (m_Event.type == EventType.MouseDown)
        ///        {
        ///            Debug.Log("Mouse Down.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseDrag)
        ///        {
        ///            Debug.Log("Mouse Dragged.");
        ///        }
        ///
        ///        if (m_Event.type == EventType.MouseUp)
        ///        {
        ///            Debug.Log("Mouse Up.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use MouseDrag instead (UnityUpgradable) -> MouseDrag", true)]
        mouseDrag = 3,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use KeyDown instead (UnityUpgradable) -> KeyDown", true)]
        keyDown = 4,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use KeyUp instead (UnityUpgradable) -> KeyUp", true)]
        keyUp = 5,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use ScrollWheel instead (UnityUpgradable) -> ScrollWheel", true)]
        scrollWheel = 6,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Repaint instead (UnityUpgradable) -> Repaint", true)]
        repaint = 7,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Layout instead (UnityUpgradable) -> Layout", true)]
        layout = 8,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use DragUpdated instead (UnityUpgradable) -> DragUpdated", true)]
        dragUpdated = 9,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use DragPerform instead (UnityUpgradable) -> DragPerform", true)]
        dragPerform = 10,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Ignore instead (UnityUpgradable) -> Ignore", true)]
        ignore = 11,
        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Used instead (UnityUpgradable) -> Used", true)]
        used = 12
    }

    ///<summary>Pointer types.</summary>
    public enum PointerType
    {
        ///<summary>The pointer type for mouse events.</summary>
        Mouse = 0,
        ///<summary>The pointer type for touch events.</summary>
        Touch = 1,
        ///<summary>The pointer type for pen events.</summary>
        ///<remarks>Before you process an event as a pen event, you should check the <see cref="PointerType" /> of a mouse event (e.g. <see cref="EventType.MouseDown" />). 
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
        ///                Debug.Log("Pen Down.");
        ///            else 
        ///                Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Pen = 2,
    }
}
