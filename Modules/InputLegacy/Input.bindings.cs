// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>Describes the phase of a finger touch.</summary>
    ///<remarks>TouchPhase is an enum type that contains the states of possible finger touches. The states represent each action the finger can take on the most recent frame update. Because the device tracks a touch over its lifetime, the start and end of a touch and movements in between can be reported on the frames they occur.</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// //Attach this script to an empty GameObject
    /// //Create some UI Text by going to __Create__>__UI__>__Text__.
    /// //Drag this GameObject into the Text field to the Inspector window of your GameObject.
    ///
    ///using UnityEngine;
    ///using System.Collections;
    ///using UnityEngine.UI;
    ///
    ///public class TouchPhaseExample : MonoBehaviour
    ///{
    ///    public Vector2 startPos;
    ///    public Vector2 direction;
    ///
    ///    public Text m_Text;
    ///    string message;
    ///
    ///    void Update()
    ///    {
    ///        //Update the Text on the screen depending on current TouchPhase, and the current direction vector
    ///        m_Text.text = "Touch : " + message + "in direction" + direction;
    ///
    ///        // Track a single touch as a direction control.
    ///        if (Input.touchCount > 0)
    ///        {
    ///            Touch touch = Input.GetTouch(0);
    ///
    ///            // Handle finger movements based on TouchPhase
    ///            switch (touch.phase)
    ///            {
    ///                //When a touch has first been detected, change the message and record the starting position
    ///                case TouchPhase.Began:
    ///                    // Record initial touch position.
    ///                    startPos = touch.position;
    ///                    message = "Begun ";
    ///                    break;
    ///
    ///                //Determine if the touch is a moving touch
    ///                case TouchPhase.Moved:
    ///                    // Determine direction by comparing the current touch position with the initial one
    ///                    direction = touch.position - startPos;
    ///                    message = "Moving ";
    ///                    break;
    ///
    ///                case TouchPhase.Ended:
    ///                    // Report that the touch has ended when it ends
    ///                    message = "Ending ";
    ///                    break;
    ///            }
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public enum TouchPhase
    {
        ///<summary>A finger touched the screen.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to an empty GameObject
        /// //Create some UI Text by going to Create>UI>Text.
        /// //Drag this GameObject into the Text field of your GameObject’s Inspector window.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI;
        ///
        ///public class TouchPhaseExample : MonoBehaviour
        ///{
        ///    public Vector2 startPos;
        ///    public Vector2 direction;
        ///
        ///    public Text m_Text;
        ///    string message;
        ///
        ///    void Update()
        ///    {
        ///        //Update the Text on the screen depending on current TouchPhase, and the current direction vector
        ///        m_Text.text = "Touch : " + message + "in direction" + direction;
        ///
        ///        // Track a single touch as a direction control.
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Handle finger movements based on TouchPhase
        ///            switch (touch.phase)
        ///            {
        ///                //When a touch is detected for the first time, change the message and record the starting position
        ///                case TouchPhase.Began:
        ///                    // Record initial touch position.
        ///                    startPos = touch.position;
        ///                    message = "Begun ";
        ///                    break;
        ///
        ///                //Determine if the touch is a moving touch
        ///                case TouchPhase.Moved:
        ///                    // Determine direction by comparing the current touch position with the initial one
        ///                    direction = touch.position - startPos;
        ///                    message = "Moving ";
        ///                    break;
        ///
        ///                case TouchPhase.Ended:
        ///                    // Report that the touch has ended when it ends
        ///                    message = "Ending ";
        ///                    break;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Began = 0,
        ///<summary>A finger moved on the screen.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to an empty GameObject
        /// //Create some UI Text by going to Create>UI>Text.
        /// //Drag this GameObject into the Text field of your GameObject’s Inspector window.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI;
        ///
        ///public class TouchPhaseExample : MonoBehaviour
        ///{
        ///    public Vector2 startPos;
        ///    public Vector2 direction;
        ///
        ///    public Text m_Text;
        ///    string message;
        ///
        ///    void Update()
        ///    {
        ///        //Update the Text on the screen depending on current TouchPhase, and the current direction vector
        ///        m_Text.text = "Touch : " + message + "in direction" + direction;
        ///
        ///        // Track a single touch as a direction control.
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Handle finger movements based on TouchPhase
        ///            switch (touch.phase)
        ///            {
        ///                //When a touch is detected for the first time, change the message and record the starting position
        ///                case TouchPhase.Began:
        ///                    // Record initial touch position.
        ///                    startPos = touch.position;
        ///                    message = "Begun ";
        ///                    break;
        ///
        ///                //Determine if the touch is a moving touch
        ///                case TouchPhase.Moved:
        ///                    // Determine direction by comparing the current touch position with the initial one
        ///                    direction = touch.position - startPos;
        ///                    message = "Moving ";
        ///                    break;
        ///
        ///                case TouchPhase.Ended:
        ///                    // Report that the touch has ended when it ends
        ///                    message = "Ending ";
        ///                    break;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Moved = 1,
        ///<summary>A finger is touching the screen but hasn't moved.</summary>
        ///<remarks>A touch enters this phase when it stays on the screen but its position doesn't change between frames. The threshold that separates a stationary touch from a <see cref="TouchPhase.Moved" /> touch is platform-dependent and isn't an exact zero-distance test. Platforms that use Unity's built-in touch emulation, such as Android, keep a touch stationary until it moves more than a small distance. On iOS, the operating system reports this phase directly.</remarks>
        Stationary = 2,
        ///<summary>A finger was lifted from the screen. This is the final phase of a touch.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to an empty GameObject
        /// //Create some UI Text by going to Create>UI>Text.
        /// //Drag this GameObject into the Text field of your GameObject’s Inspector window.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI;
        ///
        ///public class TouchPhaseExample : MonoBehaviour
        ///{
        ///    public Vector2 startPos;
        ///    public Vector2 direction;
        ///
        ///    public Text m_Text;
        ///    string message;
        ///
        ///    void Update()
        ///    {
        ///        //Update the Text on the screen depending on current TouchPhase, and the current direction vector
        ///        m_Text.text = "Touch : " + message + "in direction" + direction;
        ///
        ///        // Track a single touch as a direction control.
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Handle finger movements based on TouchPhase
        ///            switch (touch.phase)
        ///            {
        ///                //When a touch is detected for the first time, change the message and record the starting position
        ///                case TouchPhase.Began:
        ///                    // Record initial touch position.
        ///                    startPos = touch.position;
        ///                    message = "Begun ";
        ///                    break;
        ///
        ///                //Determine if the touch is a moving touch
        ///                case TouchPhase.Moved:
        ///                    // Determine direction by comparing the current touch position with the initial one
        ///                    direction = touch.position - startPos;
        ///                    message = "Moving ";
        ///                    break;
        ///
        ///                case TouchPhase.Ended:
        ///                    // Report that the touch has ended when it ends
        ///                    message = "Ending ";
        ///                    break;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Ended = 3,
        ///<summary>The system cancelled tracking for the touch.</summary>
        ///<remarks>This might happen if, for example, the user puts the device to their face or simultaneously applies more touches than the system can track (the exact number varies between platforms). This is the final phase of a touch.</remarks>
        Canceled = 4
    }

    // Controls IME input
    ///<summary>Controls IME input.</summary>
    public enum IMECompositionMode
    {
        ///<summary>Enable IME input only when a text field is selected (default).</summary>
        Auto = 0,
        ///<summary>Enable IME input.</summary>
        On = 1,
        ///<summary>Disable IME input.</summary>
        Off = 2
    }

    ///<summary>Describes whether a touch is direct, indirect (or remote), or from a stylus.</summary>
    public enum TouchType
    {
        ///<summary>A direct touch on a device.</summary>
        ///<remarks>Touch coordinates correspond to screen coordinates.</remarks>
        Direct,
        ///<summary>An Indirect, or remote, touch on a device.</summary>
        ///<remarks>Touch coordinates don't correspond to screen coordinates.</remarks>
        Indirect,
        ///<summary>A touch from a stylus on a device.</summary>
        ///<remarks>Touch coordinates correspond to screen coordinates.</remarks>
        Stylus
    }

    ///<summary>Structure describing the status of a finger touching the screen.</summary>
    ///<remarks>Devices can track several pieces of data about a touch on a touchscreen, including its <c>phase</c> of the touch lifecycle, its position, and whether the touch was a single contact or several taps. Furthermore, the device can detect the continuity of a touch between frame updates, so a consistent ID number can be reported across frames and used to determine how a particular finger is moving.
    ///
    ///The touch lifecycle describes the state of a touch in any given frame:
    ///
    ///* Began - A user has touched their finger to the screen this frame
    ///* Stationary - A finger is on the screen but the user has not moved it this frame
    ///* Moved - A user moved their finger this frame
    ///* Ended - A user lifted their finger from the screen this frame
    ///* Cancelled - The touch was interrupted this frame
    ///
    ///Unity uses the Touch struct to store data relating to a single touch instance. The struct is returned by the <see cref="Input.GetTouch" /> function. Fresh calls to GetTouch are required on each frame update to obtain the latest touch information from the device but the <see cref="fingerId" /> property can be used to identify the same touch between frames.
    ///
    ///**Note**: On iOS devices, any Touch information being held in memory (for example, when you are part-way through the touch lifecycle) is lost if the application is minimized. This happens because iOS calls ResetTouch() and wipes all touch data from memory. The lifecycle of that touch ends there and any functionality that relies on later phases of the touch lifecycle isn't executed. If you experience this problem, you should move the functionality that isn't being executed into <c>OnApplicationFocus</c> or <c>OnApplicationPause</c>.</remarks>
    ///<seealso cref="Input.GetTouch" />
    ///<seealso cref="TouchPhase" />
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public struct Touch
    {
        private int m_FingerId;
        private Vector2 m_Position;
        private Vector2 m_RawPosition;
        private Vector2 m_PositionDelta;
        private float m_TimeDelta;
        private int m_TapCount;
        private TouchPhase m_Phase;
        private TouchType m_Type;
        private float m_Pressure;
        private float m_maximumPossiblePressure;
        private float m_Radius;
        private float m_RadiusVariance;
        private float m_AltitudeAngle;
        private float m_AzimuthAngle;

        ///<summary>The unique index for the touch.</summary>
        ///<remarks>All current touches are reported in the <see cref="Input.touches" /> array or by using the <see cref="Input.GetTouch" /> function with the equivalent array index. However, the array index isn't guaranteed to be the same from one frame to the next. The <c>fingerId</c> value, however, consistently refers to the same touch across frames. Use the ID value when analysing gestures; it's more reliable than identifying fingers by their proximity to previous position, etc.
        ///
        ///<see cref="Touch.fingerId" /> isn't the same as first touch, second touch and so on. It's just a unique ID for each gesture. You can't make any assumptions about fingerId and the number of fingers actually on screen, since virtual touches are introduced to handle the fact that the touch structure is constant for an entire frame (while in reality the number of touches might not be true, for example if multiple tappings occur within a single frame).</remarks>
        public int fingerId { get { return m_FingerId; } set { m_FingerId = value; } }
        ///<summary>The position of the touch in screen space pixel coordinates.</summary>
        ///<remarks>Returns the current position of a touch contact as it's dragged. If you need the original position of the touch, refer to <see cref="Touch.rawPosition" />.
        ///
        ///The bottom-left of the screen or window is at (0, 0). The top-right of the screen or window is at (Screen.width, Screen.height).</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // This script outputs the position of an active touch contact
        ///
        /// // Attach this script to a GameObject
        /// // Create a Text GameObject (GameObject>UI>Text)
        /// // Attach the Text to the Text field in the Inspector window of your GameObject
        ///
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///
        ///public class TouchPositionExample : MonoBehaviour
        ///{
        ///    public Text m_Text;
        ///
        ///    void Update()
        ///    {
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Update the Text on the screen depending on current position of the touch each frame
        ///            m_Text.text = "Touch Position : " + touch.position;
        ///        }
        ///        else
        ///        {
        ///            m_Text.text = "No touch contacts";
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector2 position { get { return m_Position; } set { m_Position = value; }  }
        ///<summary>The first position of the touch contact in screen space pixel coordinates.</summary>
        ///<remarks>Raw position returns the original position of a touch contact and doesn't change as the touch is dragged. If you need the current position of the touch, use <see cref="Touch.position" />.
        ///
        ///The bottom-left of the screen or window is at (0, 0). The top-right of the screen or window is at (Screen.width, Screen.height).</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // This script outputs the raw position of an active touch contact
        ///
        /// // Attach this script to a GameObject
        /// // Create a Text GameObject (GameObject>UI>Text)
        /// // Attach the Text to the Text field in the Inspector window of your GameObject
        ///
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///
        ///public class TouchRawPositionExample : MonoBehaviour
        ///{
        ///    public Text m_Text;
        ///
        ///    void Update()
        ///    {
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Update the Text on the screen depending on the raw position of the touch
        ///            // NOTE: rawPosition doesn't change when the touch contact is dragged
        ///            m_Text.text = "Raw Position : " + touch.rawPosition;
        ///        }
        ///        else
        ///        {
        ///            m_Text.text = "No touch contacts";
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector2 rawPosition { get { return m_RawPosition; } set { m_RawPosition = value; }  }
        ///<summary>The position delta since last change in pixel coordinates.</summary>
        ///<remarks>The absolute position of the touch is recorded periodically and available in the <see cref="position" /> property. The deltaPosition value is a Vector2 in pixel coordinates that represents the difference between the touch position recorded on the most recent update and that recorded on the previous update. The <see cref="deltaTime" /> value gives the time that elapsed between the previous and current updates; you can calculate the touch's speed of motion by dividing deltaPosition.magnitude by <see cref="deltaTime" />.</remarks>
        ///<seealso cref="deltaTime" />
        public Vector2 deltaPosition { get { return m_PositionDelta; } set { m_PositionDelta = value; }  }
        ///<summary>Amount of time that has passed since the last recorded change in Touch values.</summary>
        ///<remarks>Values for the various touch properties are updated periodically. The deltaTime value is simply the amount of time that elapsed between the previous update and the current one. This is primarily useful for determining the movement speed of the touch position with reference to <see cref="deltaPosition" />.</remarks>
        public float deltaTime { get { return m_TimeDelta; } set { m_TimeDelta = value; }  }
        ///<summary>Number of consecutive taps that the touch is part of.</summary>
        ///<remarks>The value is 1 for a single tap. Each additional tap that occurs within a short time and distance of the previous tap increases the value by 1, so a double tap reports a value of 2. Use this property to detect double taps and other multi-tap gestures at a particular position.
        ///
        ///In some circumstances, the device might incorrectly register two fingers tapped alternately as a single finger tapping and simultaneously moving.</remarks>
        public int tapCount { get { return m_TapCount; } set { m_TapCount = value; }  }
        ///<summary>Describes the phase of the touch.</summary>
        ///<remarks>The touch <c>phase</c> refers to the action the finger has taken on the most recent frame update. Since a touch is tracked over its "lifetime" by the device, the start and end of a touch and movements in between can be reported on the frames they occur. The <c>phase</c> property can be used as the basis of a "switch' statement or as part of a more sophisticated state handling system.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Vector2 startPos;
        ///    public Vector2 direction;
        ///    public bool directionChosen;
        ///    void Update()
        ///    {
        ///        // Track a single touch as a direction control.
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Handle finger movements based on touch phase.
        ///            switch (touch.phase)
        ///            {
        ///                // Record initial touch position.
        ///                case TouchPhase.Began:
        ///                    startPos = touch.position;
        ///                    directionChosen = false;
        ///                    break;
        ///
        ///                // Determine direction by comparing the current touch position with the initial one.
        ///                case TouchPhase.Moved:
        ///                    direction = touch.position - startPos;
        ///                    break;
        ///
        ///                // Report that a direction has been chosen when the finger is lifted.
        ///                case TouchPhase.Ended:
        ///                    directionChosen = true;
        ///                    break;
        ///            }
        ///        }
        ///        if (directionChosen)
        ///        {
        ///            // Something that uses the chosen direction...
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public TouchPhase phase { get { return m_Phase; } set { m_Phase = value; }  }
        ///<summary>The current amount of pressure being applied to a touch.  1.0f is considered to be the pressure of an average touch.  If <see cref="Input.touchPressureSupported" /> returns false, the value of this property will always be 1.0f.</summary>
        public float pressure { get { return m_Pressure; } set { m_Pressure = value; }  }
        ///<summary>The maximum possible pressure value for a platform.  If <see cref="Input.touchPressureSupported" /> returns false, the value of this property will always be 1.0f.</summary>
        public float maximumPossiblePressure { get { return m_maximumPossiblePressure; } set { m_maximumPossiblePressure = value; }  }

        ///<summary>A value that indicates whether a touch was of Direct, Indirect (or remote), or Stylus type.</summary>
        public TouchType type { get { return m_Type; } set { m_Type = value; }  }
        ///<summary>Value of 0 radians indicates that the stylus is parallel to the surface, pi/2 indicates that it is perpendicular.</summary>
        public float altitudeAngle { get { return m_AltitudeAngle; } set { m_AltitudeAngle = value; }  }
        ///<summary>Value of 0 radians indicates that the stylus is pointed along the x-axis of the device.</summary>
        public float azimuthAngle { get { return m_AzimuthAngle; } set { m_AzimuthAngle = value; }  }
        ///<summary>An estimated value of the radius of a touch.  Add radiusVariance to get the maximum touch size, subtract it to get the minimum touch size.</summary>
        ///<remarks>**Android**: Works correctly on a limited set of devices.</remarks>
        public float radius { get { return m_Radius; } set { m_Radius = value; }  }
        ///<summary>This value determines the accuracy of the touch radius. Add this value to the radius to get the maximum touch size, subtract it to get the minimum touch size.</summary>
        ///<remarks>**Android**: Returns 0 for the majority of devices.</remarks>
        public float radiusVariance { get { return m_RadiusVariance; } set { m_RadiusVariance = value; }  }
    }

    // Matches PenData::PenStatusEnum in native code
    ///<summary>Options for specifying the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed. You can combine states using bitwise OR operators.</summary>
    ///<remarks>Before you process an event as a pen event, you should check the <see cref="T:UnityEngine.PointerType" /> of a mouse event (e.g. <see cref="F:UnityEngine.EventType.MouseDown" />).
    ///
    ///When a user uses a pen, some mouse events are often mixed with pen events in the event stream, and you can't distinguish them by type because mouse and pen events share the same <see cref="T:UnityEngine.EventType" />. Instead, use <see cref="T:UnityEngine.PointerType" /> to distinguish them. Otherwise, Unity processes all incoming mouse events as pen events, which can lead to unexpected behavior because the mouse events (pointerType = Mouse) do not have pen event fields, like <see cref="PenStatus" />, set.</remarks>
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
    ///                if (m_Event.penStatus == PenStatus.None)
    ///                    Debug.Log("Pen is in a neutral state.");
    ///                else if (m_Event.penStatus == PenStatus.Inverted)
    ///                    Debug.Log("The pen is inverted.");
    ///                else if (m_Event.penStatus == PenStatus.Barrel)
    ///                    Debug.Log("Barrel button on pen is down.");
    ///                else if (m_Event.penStatus == PenStatus.Contact)
    ///                    Debug.Log("Pen is in contact with screen or tablet.");
    ///                else if (m_Event.penStatus == PenStatus.Eraser)
    ///                    Debug.Log("Pen is in erase mode.");
    ///            }
    ///        else
    ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [Flags]
    public enum PenStatus
    {
        ///<summary>The pen is in a neutral state.</summary>
        ///<example>
        ///  <code><![CDATA[
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
        ///            if (m_Event.pointerType == PointerType.Pen)     //Check if it's a pen event.
        ///            {
        ///                if (m_Event.penStatus == PenStatus.None)
        ///                    Debug.Log("Pen is in a neutral state.");
        ///            }
        ///        else
        ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        None = 0x0,
        ///<summary>The pen is in contact with the screen or tablet.</summary>
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
        ///}
        ///]]></code>
        ///</example>
        Contact = 0x1,
        ///<summary>The barrel button on the pen is currently pressed.</summary>
        ///<remarks>On macOS, this flag will always correspond to the lower barrel button.</remarks>
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
        ///                if (m_Event.penStatus == PenStatus.Barrel)
        ///                    Debug.Log("Barrel button on pen is down.");
        ///            }
        ///        else
        ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Barrel = 0x2,
        ///<summary>The pen is inverted.</summary>
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
        ///                if (m_Event.penStatus == PenStatus.Inverted)
        ///                    Debug.Log("The pen is inverted.");
        ///            }
        ///        else
        ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Inverted = 0x4,
        ///<summary>The pen is in erase mode.</summary>
        ///<example>
        ///  <code><![CDATA[
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
        ///            if (m_Event.pointerType == PointerType.Pen)     //Check if it's a pen event.
        ///            {
        ///                if (m_Event.penStatus == PenStatus.Eraser)
        ///                    Debug.Log("Pen is in erase mode.");
        ///            }
        ///        else
        ///            Debug.Log("Mouse Down.");                   //If it's not a pen event, it's a mouse event. 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Eraser = 0x8,
    }

    ///<summary>Indicates the type of action of a pen event.</summary>
    public enum PenEventType
    {
        ///<summary>No pen contact.</summary>
        NoContact,
        ///<summary>A pen down event.</summary>
        PenDown,
        ///<summary>A pen up event.</summary>
        PenUp
    }

    ///<summary>Structure describing the status of a pen event.</summary>
    ///<remarks>The PenData struct is used by Unity to store data relating to a pen event. PenData contains information such as the position, pressure, and tilt of the pen for a given pen input event.</remarks>
    public struct PenData
    {
        ///<summary>Specifies the position of the pen.</summary>
        public Vector2 position;
        ///<summary>Specifies the angle of the pen relative to the X and Y axes, expressed in radians.</summary>
        public Vector2 tilt;
        ///<summary>Specifies the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed.</summary>
        ///<remarks>On macOS, penStatus will not reflect changes to button mappings.</remarks>
        public PenStatus penStatus;
        ///<summary>Specifies the rotation of the pen around its axis, expressed in radians.</summary>
        ///<remarks>The default value is 0.</remarks>
        public float twist;
        ///<summary>How hard pen pressure is applied, normalized between 0 (no pressure) and 1 (maximum pressure).</summary>
        ///<remarks>Pressure is always 1 on devices where pressure is not supported.</remarks>
        public float pressure;
        ///<summary>Contact type of a pen event, can be pen up, pen down, or no contact.</summary>
        public PenEventType contactType;
        ///<summary>The position delta since last pointer event in pixel coordinates.</summary>
        public Vector2 deltaPos;
    }

    ///<summary>Describes physical orientation of the device as determined by the OS.</summary>
    ///<remarks>If device is physically situated between discrete positions, as when (for
    ///example) rotated diagonally, system will report Unknown orientation.</remarks>
    public enum DeviceOrientation
    {
        ///<summary>The orientation of the device cannot be determined.</summary>
        Unknown = 0,
        ///<summary>The device is in portrait mode, with the device held upright and the home button at the bottom.</summary>
        Portrait = 1,
        ///<summary>The device is in portrait mode but upside down, with the device held upright and the home button at the top.</summary>
        PortraitUpsideDown = 2,
        ///<summary>The device is in landscape mode, with the device held upright and the home button on the right side.</summary>
        LandscapeLeft = 3,
        ///<summary>The device is in landscape mode, with the device held upright and the home button on the left side.</summary>
        LandscapeRight = 4,
        ///<summary>The device is held parallel to the ground with the screen facing upwards.</summary>
        FaceUp = 5,
        ///<summary>The device is held parallel to the ground with the screen facing downwards.</summary>
        FaceDown = 6
    }

    ///<summary>Structure describing acceleration status of the device.</summary>
    public struct AccelerationEvent
    {
        internal float x;
        internal float y;
        internal float z;
        internal float m_TimeDelta;

        ///<summary>Value of acceleration.</summary>
        public Vector3 acceleration { get { return new Vector3(x, y, z); } }
        ///<summary>Amount of time passed since last accelerometer measurement.</summary>
        public float deltaTime { get { return m_TimeDelta; } }
    }

    ///<summary>Interface into the Gyroscope.</summary>
    ///<remarks>
    ///  <para>Use this class to access the gyroscope. The example script below shows how the Gyroscope class can be used to view the orientation in space of the device.
    ///
    ///Underlying sensors used for data population:
    ///
    ///**Android**: Gravity, Linear Acceleration, Rotation Vector. &lt;a href="https://developer.android.com/guide/topics/sensors/sensors_motion"&gt; More information&lt;/a&gt;.
    ///
    ///**iOS**: Gyroscope, Device-Motion. &lt;a href="https://developer.apple.com/documentation/coremotion/cmmotionmanager"&gt; More information&lt;/a&gt;.</para>
    ///  <para>
    ///    <img src="iOSgyroscope.png" />
    ///
    ///iOS Screen-shot showing +Z, +Y and -X.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// // Create a cube with camera vector names on the faces.
    /// // Allow the device to show named faces as it is oriented.
    ///
    ///using UnityEngine;
    ///
    ///public class ExampleScript : MonoBehaviour
    ///{
    ///    // Faces for 6 sides of the cube
    ///    private GameObject[] quads = new GameObject[6];
    ///
    ///    // Textures for each quad, should be +X, +Y etc
    ///    // with appropriate colors, red, green, blue, etc
    ///    public Texture[] labels;
    ///
    ///    void Start()
    ///    {
    ///        Input.gyro.enabled = true;
    ///        
    ///        // make camera solid colour and based at the origin
    ///        GetComponent<Camera>().backgroundColor = new Color(49.0f / 255.0f, 77.0f / 255.0f, 121.0f / 255.0f);
    ///        GetComponent<Camera>().transform.position = new Vector3(0, 0, 0);
    ///        GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
    ///
    ///        // create the six quads forming the sides of a cube
    ///        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
    ///
    ///        quads[0] = createQuad(quad, new Vector3(1,   0,   0), new Vector3(0,  90, 0), "plus x",
    ///            new Color(0.90f, 0.10f, 0.10f, 1), labels[0]);
    ///        quads[1] = createQuad(quad, new Vector3(0,   1,   0), new Vector3(-90,   0, 0), "plus y",
    ///            new Color(0.10f, 0.90f, 0.10f, 1), labels[1]);
    ///        quads[2] = createQuad(quad, new Vector3(0,   0,   1), new Vector3(0,   0, 0), "plus z",
    ///            new Color(0.10f, 0.10f, 0.90f, 1), labels[2]);
    ///        quads[3] = createQuad(quad, new Vector3(-1,   0,   0), new Vector3(0, -90, 0), "neg x",
    ///            new Color(0.90f, 0.50f, 0.50f, 1), labels[3]);
    ///        quads[4] = createQuad(quad, new Vector3(0,  -1,  0), new Vector3(90,   0,  0), "neg y",
    ///            new Color(0.50f, 0.90f, 0.50f, 1), labels[4]);
    ///        quads[5] = createQuad(quad, new Vector3(0,   0, -1), new Vector3(0, 180,  0), "neg z",
    ///            new Color(0.50f, 0.50f, 0.90f, 1), labels[5]);
    ///
    ///        GameObject.Destroy(quad);
    ///    }
    ///
    ///    // make a quad for one side of the cube
    ///    GameObject createQuad(GameObject quad, Vector3 pos, Vector3 rot, string name, Color col, Texture t)
    ///    {
    ///        Quaternion quat = Quaternion.Euler(rot);
    ///        GameObject GO = Instantiate(quad, pos, quat);
    ///        GO.name = name;
    ///        GO.GetComponent<Renderer>().material.color = col;
    ///        GO.GetComponent<Renderer>().material.mainTexture = t;
    ///        GO.transform.localScale += new Vector3(0.25f, 0.25f, 0.25f);
    ///        return GO;
    ///    }
    ///
    ///    protected void Update()
    ///    {
    ///        GyroModifyCamera();
    ///    }
    ///
    ///    protected void OnGUI()
    ///    {
    ///        GUI.skin.label.fontSize = Screen.width / 40;
    ///
    ///        GUILayout.Label("Orientation: " + Screen.orientation);
    ///        GUILayout.Label("input.gyro.attitude: " + Input.gyro.attitude);
    ///        GUILayout.Label("iphone width/font: " + Screen.width + " : " + GUI.skin.label.fontSize);
    ///    }
    ///
    ///    /********************************************/
    ///
    ///    // The Gyroscope is right-handed.  Unity is left handed.
    ///    // Make the necessary change to the camera.
    ///    void GyroModifyCamera()
    ///    {
    ///        transform.rotation = GyroToUnity(Input.gyro.attitude);
    ///    }
    ///
    ///    private static Quaternion GyroToUnity(Quaternion q)
    ///    {
    ///        return new Quaternion(q.x, q.y, -q.z, -q.w);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Runtime/Input/GetInput.h")]
    public class Gyroscope
    {
        internal Gyroscope(int index)
        {
            m_GyroIndex = index;
        }

        private int m_GyroIndex;

        [FreeFunction("GetGyroRotationRate")]
        extern private static Vector3 rotationRate_Internal(int idx);
        [FreeFunction("GetGyroRotationRateUnbiased")]
        extern private static Vector3 rotationRateUnbiased_Internal(int idx);
        [FreeFunction("GetGravity")]
        extern private static Vector3 gravity_Internal(int idx);
        [FreeFunction("GetUserAcceleration")]
        extern private static Vector3 userAcceleration_Internal(int idx);
        [FreeFunction("GetAttitude")]
        extern private static Quaternion attitude_Internal(int idx);
        [FreeFunction("IsGyroEnabled")]
        extern private static bool getEnabled_Internal(int idx);
        [FreeFunction("SetGyroEnabled")]
        extern private static void setEnabled_Internal(int idx, bool enabled);
        [FreeFunction("GetGyroUpdateInterval")]
        extern private static float getUpdateInterval_Internal(int idx);
        [FreeFunction("SetGyroUpdateInterval")]
        extern private static void setUpdateInterval_Internal(int idx, float interval);

        ///<summary>Returns rotation rate as measured by the device's gyroscope.</summary>
        ///<remarks>The rotation rate is given as a Vector3 representing the speed of rotation around each of the three
        /// axes in radians per second. This is the value as it is reported by the gyroscope hardware - a more
        /// accurate measurement that has been processed to remove "bias" can be obtained with the
        /// <see cref="rotationRateUnbiased" /> property.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float shakeSpeed;
        ///    public AudioClip shakeSound;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.gyro.rotationRate.y > shakeSpeed && !audioSource.isPlaying)
        ///            audioSource.PlayOneShot(shakeSound);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 rotationRate { get { return rotationRate_Internal(m_GyroIndex); } }
        ///<summary>Returns unbiased rotation rate as measured by the device's gyroscope.</summary>
        ///<remarks>The rotation rate is given as a Vector3 representing the speed of rotation around each of the three
        /// axes in radians per second. This value has been processed to remove "bias" and give a more accurate
        /// measurement. The raw value reported by the gyroscope hardware can be obtained with the
        /// <see cref="rotationRate" /> property.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float shakeSpeed;
        ///    public AudioClip shakeSound;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.gyro.rotationRateUnbiased.y > shakeSpeed && !audioSource.isPlaying)
        ///            audioSource.PlayOneShot(shakeSound);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 rotationRateUnbiased { get { return rotationRateUnbiased_Internal(m_GyroIndex); } }
        ///<summary>Returns the gravity acceleration vector expressed in the device's reference frame.</summary>
        ///<remarks>This property returns <see cref="Vector3.zero" /> until the device delivers its first motion sample after the gyroscope is enabled. Read the gravity vector on a later frame, after the sensor has produced data.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    float movementScale;
        ///
        ///    void Start()
        ///    {
        ///        Input.gyro.enabled = true;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // A "spirit level" - the dot product of the gravity and the Y axis (ie, Vector3.up)
        ///        // is a measure of how far the device is from level on that axis (it will be zero
        ///        // if the device is perfectly level). This value can be used to position an object
        ///        // to act as the "bubble".
        ///        Vector3 pos = transform.position;
        ///        pos.y = Vector3.Dot(Input.gyro.gravity, Vector3.up) * movementScale;
        ///        transform.position = pos;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 gravity { get { return gravity_Internal(m_GyroIndex); } }
        ///<summary>Returns the acceleration that the user is giving to the device.</summary>
        ///<remarks>The significance of this value is that the effect of gravity (which is also detected by the accelerometer)
        /// has been removed to leave just the acceleration from the user's movements.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Vector3 forceVec;
        ///    Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        // Apply forces to an object to match the side-to-side acceleration
        ///        // the user is giving to the device.
        ///        rb.AddForce(Input.gyro.userAcceleration.x * forceVec);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 userAcceleration { get { return userAcceleration_Internal(m_GyroIndex); } }
        ///<summary>Returns the attitude (ie, orientation in space) of the device.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Rotate the object to match the device's orientation
        ///    // in space.
        ///    void Update()
        ///    {
        ///        transform.rotation = Input.gyro.attitude;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Quaternion attitude { get { return attitude_Internal(m_GyroIndex); } }
        ///<summary>Sets or retrieves the enabled status of this gyroscope.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    bool enableTilt;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (Input.gyro.enabled)
        ///        {
        ///            GUILayout.Toggle(enableTilt, "Enable tilt control");
        ///        }
        ///
        ///        // Other GUI elements...
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool enabled { get { return getEnabled_Internal(m_GyroIndex); } set { setEnabled_Internal(m_GyroIndex, value); } }
        ///<summary>Sets or retrieves gyroscope interval in seconds.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Input.gyro.updateInterval = 0.01f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float updateInterval { get { return getUpdateInterval_Internal(m_GyroIndex); } set { setUpdateInterval_Internal(m_GyroIndex, value); } }
    }

    ///<summary>Structure describing device location.</summary>
    public struct LocationInfo
    {
        internal double m_Timestamp;
        internal float m_Latitude;
        internal float m_Longitude;
        internal float m_Altitude;
        internal float m_HorizontalAccuracy;
        internal float m_VerticalAccuracy;

        ///<summary>Geographical device location latitude.</summary>
        public float latitude { get { return m_Latitude; } }
        ///<summary>Geographical device location longitude.</summary>
        public float longitude { get { return m_Longitude; } }
        ///<summary>The altitude of the device's geographical location in meters.</summary>
        ///<remarks>The altitude value can vary per platform based on the coordinate reference system each platform uses.
        ///
        ///* **Android**, **UWP**, **Web**: The altitude is measured relative to the WGS84 (World Geodetic System 1984) ellipsoid.
        ///* **iOS**: The altitude is measured relative to mean sea level, as provided by the &lt;a href="https://developer.apple.com/documentation/corelocation/cllocation"&gt;CLLocation API (Apple)&lt;/a&gt;.</remarks>
        public float altitude { get { return m_Altitude; } }
        ///<summary>Horizontal accuracy radius of the location in meters.</summary>
        public float horizontalAccuracy { get { return m_HorizontalAccuracy; } }
        ///<summary>Vertical accuracy radius of the location in meters.</summary>
        public float verticalAccuracy { get { return m_VerticalAccuracy; } }
        ///<summary>Timestamp of when the location data was last updated.</summary>
        ///<remarks>The unit and epoch of the timestamp value vary per platform:
        ///
        ///* **Android**, **iOS**: The timestamp is the number of seconds since the Unix epoch (January 1, 1970, UTC).
        ///* **Web**: The timestamp is the number of milliseconds since the Unix epoch (January 1, 1970, UTC).
        ///* **UWP**: The timestamp is the number of 100-nanosecond intervals since January 1, 1601 (UTC).</remarks>
        public double timestamp { get { return m_Timestamp; } }
    }

    ///<summary>Describes the location service status for a device.</summary>
    public enum LocationServiceStatus
    {
        ///<summary>The location service is not running.</summary>
        Stopped = 0,
        ///<summary>The location service is initializing.</summary>
        ///<remarks>Depending on whether the user gives access to the location service, this transitions to <see cref="LocationServiceStatus.Running" /> or <see cref="LocationServiceStatus.Failed" />.</remarks>
        Initializing = 1,
        ///<summary>The location service is running and the application can query for locations.</summary>
        Running = 2,
        ///<summary>Location service initialization failed. The user denied access to the location service.</summary>
        Failed = 3
    }

    ///<summary>Provides methods that allow an application to access the device's location.</summary>
    [NativeHeader("Runtime/Input/LocationService.h")]
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public class LocationService
    {
        internal struct HeadingInfo
        {
            public float magneticHeading;
            public float trueHeading;
            public float headingAccuracy;
            public Vector3 raw;
            public double timestamp;
        }

        [FreeFunction("LocationService::IsServiceEnabledByUser")]
        internal extern static bool IsServiceEnabledByUser();
        [FreeFunction("LocationService::GetLocationStatus")]
        internal extern static LocationServiceStatus GetLocationStatus();
        [FreeFunction("LocationService::GetLastLocation")]
        internal extern static LocationInfo GetLastLocation();
        [FreeFunction("LocationService::SetDesiredAccuracy")]
        internal extern static void SetDesiredAccuracy(float value);
        [FreeFunction("LocationService::SetDistanceFilter")]
        internal extern static void SetDistanceFilter(float value);
        [FreeFunction("LocationService::StartUpdatingLocation")]
        internal extern static void StartUpdatingLocation();
        [FreeFunction("LocationService::StopUpdatingLocation")]
        internal extern static void StopUpdatingLocation();
        [FreeFunction("LocationService::GetLastHeading")]
        internal extern static HeadingInfo GetLastHeading();
        [FreeFunction("LocationService::IsHeadingUpdatesEnabled")]
        internal extern static bool IsHeadingUpdatesEnabled();
        [FreeFunction("LocationService::SetHeadingUpdatesEnabled")]
        internal extern static void SetHeadingUpdatesEnabled(bool value);

        ///<summary>Indicates whether the device allows the application to access the location service.</summary>
        ///<remarks>Check this property before you start location updates to
        ///determine if the device has location services enabled and that the application has access to them.
        ///
        ///**Android**: The property returns false if the application has no permission to access location. If you start the location service updates, the user receives location permission request (unless already granted or permanently denied). Before starting the location service updates, you can query to check whether the application has location permission.
        ///
        ///**iOS**: The property returns false if the application has no permission to access location. If you start the location updates anyway, the device prompts the user with a confirmation panel asking whether to enable location services for the application. For more information, refer to &lt;a href="https://developer.apple.com/documentation/corelocation/cllocationmanager/locationservicesenabled()?language=objc"&gt;Apple's developer documentation&lt;/a&gt;.
        ///
        ///**WebGL**: The property is false until you start location updates. Once location updates start, it reflects the permissions granted by the user in the browser.</remarks>
        public bool isEnabledByUser { get { return IsServiceEnabledByUser(); } }
        ///<summary>Returns the location service status.</summary>
        ///<seealso cref="LocationServiceStatus" />
        public LocationServiceStatus status { get { return GetLocationStatus(); } }
        ///<summary>The last geographical location that the device registered.</summary>
        ///<remarks>Before you access this property, call <see cref="LocationService.Start">Start()</see> in <see cref="Input.location" /> to start the location service.
        ///
        ///**Note**: On WebGL, this value is 0 or null if the browser has no implementation for it.</remarks>
        public LocationInfo lastData
        {
            get
            {
                if (status != LocationServiceStatus.Running)
                    Debug.Log("Location service updates are not enabled. Check LocationService.status before querying last location.");

                return GetLastLocation();
            }
        }

        ///<summary>Starts location service updates.</summary>
        ///<remarks>After you call this function, you can access the device's last location coordinates by checking <see cref="LocationService.lastData">lastData</see> in <see cref="Input.location" />.
        ///
        ///**Note**: The location service doesn't start to send location data immediately. The <see cref="LocationService.status">service status</see> in <see cref="Input.location" /> remains <see cref="LocationServiceStatus.Initializing" /> until the device returns the first location update. The time this takes depends on the device and how quickly it can determine its position. If the user denies location access, the status changes to <see cref="LocationServiceStatus.Failed" /> instead. Check the status before you query <see cref="LocationService.lastData">lastData</see>.
        ///
        ///
        ///On Android, using this method in scripts automatically adds the <c>ACCESS_FINE_LOCATION</c> permission to the Android manifest. If you use low accuracy values like 500 or higher, select **Low Accuracy Location** in [Player Settings](xref:class-PlayerSettings) to add the <c>ACCESS_COARSE_LOCATION</c> permission instead.
        ///
        ///On WebGL, this method must be invoked as a response to a user gesture (such as a mouse click) within a coroutine.  **Note:** Geolocation services are available only with an HTTPS connection, except during development when you might use http://localhost. The use of <c>desiredAccuracyInMeters</c> and <c>updateDistanceInMeters</c> isn't supported since the user device determines those two values.</remarks>
        ///<param name="desiredAccuracyInMeters">The service accuracy you want to use, in meters. This determines the accuracy of the device's last location coordinates. Higher values like 500 don't require the device to use its GPS chip and
        ///                    thus save battery power. Lower values like 5-10 provide the best accuracy but require the GPS chip and thus use more battery power. The default value is 10 meters.</param>
        ///<param name="updateDistanceInMeters">The minimum distance, in meters, that the device must move laterally before Unity updates <see cref="Input.location" />. Higher values like 500 produce fewer updates and are less resource intensive to process. The default is 10 meters.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class TestLocationService : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        // Check if the user has location service enabled.
        ///        if (!Input.location.isEnabledByUser)
        ///            Debug.Log("Location not enabled on device or app does not have permission to access location");
        ///
        ///        // Starts the location service.
        ///        
        ///        float desiredAccuracyInMeters = 10f;
        ///        float updateDistanceInMeters = 10f;
        ///
        ///        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        ///
        ///        // Waits until the location service initializes
        ///        int maxWait = 20;
        ///        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        ///        {
        ///            yield return new WaitForSeconds(1);
        ///            maxWait--;
        ///        }
        ///
        ///        // If the service didn't initialize in 20 seconds this cancels location service use.
        ///        if (maxWait < 1)
        ///        {
        ///            Debug.Log("Timed out");
        ///            yield break;
        ///        }
        ///
        ///        // If the connection failed this cancels location service use.
        ///        if (Input.location.status == LocationServiceStatus.Failed)
        ///        {
        ///            Debug.LogError("Unable to determine device location");
        ///            yield break;
        ///        }
        ///        else
        ///        {
        ///            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
        ///            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        ///        }
        ///
        ///        // Stops the location service if there is no need to query location updates continuously.
        ///        Input.location.Stop();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            SetDesiredAccuracy(desiredAccuracyInMeters);
            SetDistanceFilter(updateDistanceInMeters);
            StartUpdatingLocation();
        }

        ///<summary>Starts location service updates.</summary>
        ///<remarks>After you call this function, you can access the device's last location coordinates by checking <see cref="LocationService.lastData">lastData</see> in <see cref="Input.location" />.
        ///
        ///**Note**: The location service doesn't start to send location data immediately. The <see cref="LocationService.status">service status</see> in <see cref="Input.location" /> remains <see cref="LocationServiceStatus.Initializing" /> until the device returns the first location update. The time this takes depends on the device and how quickly it can determine its position. If the user denies location access, the status changes to <see cref="LocationServiceStatus.Failed" /> instead. Check the status before you query <see cref="LocationService.lastData">lastData</see>.
        ///
        ///
        ///On Android, using this method in scripts automatically adds the <c>ACCESS_FINE_LOCATION</c> permission to the Android manifest. If you use low accuracy values like 500 or higher, select **Low Accuracy Location** in [Player Settings](xref:class-PlayerSettings) to add the <c>ACCESS_COARSE_LOCATION</c> permission instead.
        ///
        ///On WebGL, this method must be invoked as a response to a user gesture (such as a mouse click) within a coroutine.  **Note:** Geolocation services are available only with an HTTPS connection, except during development when you might use http://localhost. The use of <c>desiredAccuracyInMeters</c> and <c>updateDistanceInMeters</c> isn't supported since the user device determines those two values.</remarks>
        ///<param name="desiredAccuracyInMeters">The service accuracy you want to use, in meters. This determines the accuracy of the device's last location coordinates. Higher values like 500 don't require the device to use its GPS chip and
        ///                    thus save battery power. Lower values like 5-10 provide the best accuracy but require the GPS chip and thus use more battery power. The default value is 10 meters.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class TestLocationService : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        // Check if the user has location service enabled.
        ///        if (!Input.location.isEnabledByUser)
        ///            Debug.Log("Location not enabled on device or app does not have permission to access location");
        ///
        ///        // Starts the location service.
        ///        
        ///        float desiredAccuracyInMeters = 10f;
        ///        float updateDistanceInMeters = 10f;
        ///
        ///        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        ///
        ///        // Waits until the location service initializes
        ///        int maxWait = 20;
        ///        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        ///        {
        ///            yield return new WaitForSeconds(1);
        ///            maxWait--;
        ///        }
        ///
        ///        // If the service didn't initialize in 20 seconds this cancels location service use.
        ///        if (maxWait < 1)
        ///        {
        ///            Debug.Log("Timed out");
        ///            yield break;
        ///        }
        ///
        ///        // If the connection failed this cancels location service use.
        ///        if (Input.location.status == LocationServiceStatus.Failed)
        ///        {
        ///            Debug.LogError("Unable to determine device location");
        ///            yield break;
        ///        }
        ///        else
        ///        {
        ///            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
        ///            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        ///        }
        ///
        ///        // Stops the location service if there is no need to query location updates continuously.
        ///        Input.location.Stop();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Start(float desiredAccuracyInMeters)
        {
            Start(desiredAccuracyInMeters, 10f);
        }

        ///<summary>Starts location service updates.</summary>
        ///<remarks>After you call this function, you can access the device's last location coordinates by checking <see cref="LocationService.lastData">lastData</see> in <see cref="Input.location" />.
        ///
        ///**Note**: The location service doesn't start to send location data immediately. The <see cref="LocationService.status">service status</see> in <see cref="Input.location" /> remains <see cref="LocationServiceStatus.Initializing" /> until the device returns the first location update. The time this takes depends on the device and how quickly it can determine its position. If the user denies location access, the status changes to <see cref="LocationServiceStatus.Failed" /> instead. Check the status before you query <see cref="LocationService.lastData">lastData</see>.
        ///
        ///
        ///On Android, using this method in scripts automatically adds the <c>ACCESS_FINE_LOCATION</c> permission to the Android manifest. If you use low accuracy values like 500 or higher, select **Low Accuracy Location** in [Player Settings](xref:class-PlayerSettings) to add the <c>ACCESS_COARSE_LOCATION</c> permission instead.
        ///
        ///On WebGL, this method must be invoked as a response to a user gesture (such as a mouse click) within a coroutine.  **Note:** Geolocation services are available only with an HTTPS connection, except during development when you might use http://localhost. The use of <c>desiredAccuracyInMeters</c> and <c>updateDistanceInMeters</c> isn't supported since the user device determines those two values.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class TestLocationService : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        // Check if the user has location service enabled.
        ///        if (!Input.location.isEnabledByUser)
        ///            Debug.Log("Location not enabled on device or app does not have permission to access location");
        ///
        ///        // Starts the location service.
        ///        
        ///        float desiredAccuracyInMeters = 10f;
        ///        float updateDistanceInMeters = 10f;
        ///
        ///        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        ///
        ///        // Waits until the location service initializes
        ///        int maxWait = 20;
        ///        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        ///        {
        ///            yield return new WaitForSeconds(1);
        ///            maxWait--;
        ///        }
        ///
        ///        // If the service didn't initialize in 20 seconds this cancels location service use.
        ///        if (maxWait < 1)
        ///        {
        ///            Debug.Log("Timed out");
        ///            yield break;
        ///        }
        ///
        ///        // If the connection failed this cancels location service use.
        ///        if (Input.location.status == LocationServiceStatus.Failed)
        ///        {
        ///            Debug.LogError("Unable to determine device location");
        ///            yield break;
        ///        }
        ///        else
        ///        {
        ///            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
        ///            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        ///        }
        ///
        ///        // Stops the location service if there is no need to query location updates continuously.
        ///        Input.location.Stop();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void Start()
        {
            Start(10f, 10f);
        }

        ///<summary>Stops location service updates. This is useful to save battery power when the application doesn't require the location service.</summary>
        ///<remarks>Do not drive the location service from both this legacy API and the Input System <c>LocationSensor</c> in the same project. Both share the same underlying platform location service, so stopping it here also stops updates for the <c>LocationSensor</c> (and vice versa). Use a single location API per project.</remarks>
        public void Stop()
        {
            StopUpdatingLocation();
        }
    }

    ///<summary>Interface into compass functionality.</summary>
    public class Compass
    {
        ///<summary>The heading in degrees relative to the magnetic North Pole. (RO)</summary>
        ///<remarks>The value in this property is always measured relative to the top
        ///of the screen in its current orientation.
        /// The heading of magnetic
        /// north is not exactly the same as true geographical north - to get
        /// the exact heading, use the <see cref="trueHeading" /> property.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Orient an object to point to magnetic north.
        ///        transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float magneticHeading
        {
            get { return LocationService.GetLastHeading().magneticHeading; }
        }
        ///<summary>The heading in degrees relative to the geographic North Pole. (RO)</summary>
        ///<remarks>The value in this property is always measured relative to the top
        ///of the screen in its current orientation.
        ///Note, that if you want this property to contain a valid value, you
        ///must also enable location updates by calling
        ///<c>Input.location.Start()</c>. (RO)
        ///
        ///**Note:** On the web platform this property will return the magnetic heading.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Orient an object to point northward.
        ///        transform.rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float trueHeading
        {
            get { return LocationService.GetLastHeading().trueHeading; }
        }
        ///<summary>Accuracy of heading reading in degrees.</summary>
        ///<remarks>Negative value means unreliable reading. If accuracy is not supported or not available, 0 is returned.
        ///Not all platforms support this precise accuracy, so the value may vary between few constant values.</remarks>
        public float headingAccuracy
        {
            get { return LocationService.GetLastHeading().headingAccuracy; }
        }
        ///<summary>The raw geomagnetic data measured in microteslas. (RO)</summary>
        ///<remarks>The compass is actually a magnetometer that measures the magnetic
        /// field in the device's XYZ coordinates - in the absence of a stronger
        /// magnet, it will measure the Earth's field from which the compass heading
        /// can be found. This property can be used if you want to make non-standard
        /// use of the compass (eg, find the heading from the X or Z axis of the device).
        ///
        ///**Note:** This property is currently not supported on the web platform.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Label("Magnetometer reading: " + Input.compass.rawVector.ToString());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 rawVector
        {
            get { return LocationService.GetLastHeading().raw; }
        }
        ///<summary>Indicates the time elapsed since the compass heading was last updated. (RO)</summary>
        ///<remarks>**Android**: The time elapsed is represented in seconds since the device was last turned on.
        ///
        ///**iOS**: The time elapsed is represented in seconds since the Unix epoch January 1, 1970.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class CompassTimeStamp : MonoBehaviour
        ///{
        ///    private double previousTimeStamp = 0;
        ///
        ///    private void Start()
        ///    {
        ///        Input.location.Start();
        ///        Input.compass.enabled = true;
        ///        Debug.Log($"compass.enabled: {Input.compass.enabled}");
        ///        RecordPreviousTimeStamp();
        ///    }
        ///    void Update()
        ///    {
        ///        Debug.Log($"frame delta: {Time.deltaTime} compass timestamp: {Input.compass.timestamp} compass delta: {Input.compass.timestamp - previousTimeStamp}");
        ///        RecordPreviousTimeStamp();
        ///    }
        ///
        ///    private void RecordPreviousTimeStamp()
        ///    {
        ///        previousTimeStamp = Input.compass.timestamp;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public double timestamp
        {
            get { return LocationService.GetLastHeading().timestamp; }
        }
        ///<summary>Use to enable or disable compass. Note, that if you want <c>Input.compass.trueHeading</c> property to contain a valid value, you must also enable location updates. To do this, call <c>Input.location.Start()</c>.
        ///
        ///**Note**: On the web platform,  the compass is available only with an HTTPS connection, except during development when you might use http://localhost.</summary>
        public bool enabled
        {
            get { return LocationService.IsHeadingUpdatesEnabled(); }
            set { LocationService.SetHeadingUpdatesEnabled(value); }
        }
    }

    // Burst-compatible unmanaged string calls can not be in UnityEngine namespace (UnityEngine.Internal is okay)
    namespace Internal
    {
        [NativeHeader("Runtime/Input/InputBindings.h")]
        internal static class InputUnsafeUtility
        {
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyUpString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyUpString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetKeyDownString(string name);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetKeyDownString__Unmanaged(byte* name, int nameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static float GetAxis(string axisName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe float GetAxis__Unmanaged(byte* axisName, int axisNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static float GetAxisRaw(string axisName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe float GetAxisRaw__Unmanaged(byte* axisName, int axisNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButton(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe bool GetButton__Unmanaged(byte* buttonName, int buttonNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButtonDown(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]

            internal extern static unsafe byte GetButtonDown__Unmanaged(byte* buttonName, int buttonNameLen);
            [NativeMethod(ThrowsException = true)]
            internal extern static bool GetButtonUp(string buttonName);
            // Burst shadow
            [NativeMethod(ThrowsException = true)]
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe bool GetButtonUp__Unmanaged(byte* buttonName, int buttonNameLen);
            internal extern static bool IsJoystickPreconfigured(string joystickName);
            // This will only be referenced from Burst-generated code, in place of the version without the
            // __Managed suffix. So we need to make sure it will not get stripped.
            [RequiredMember]
            internal extern static unsafe bool IsJoystickPreconfigured__Unmanaged(byte* joystickName, int joystickNameLen);
        }
    }

    ///<summary>Interface into the Legacy Input system.</summary>
    ///<remarks>
    ///  <para>**Note**: The <c>Input</c> class is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html"&gt;new Input System&lt;/a&gt; package.
    ///
    ///
    ///<see cref="KeyCode" /> maps to physical keys only if "Use Physical Keys" is enabled in [Input Manager settings](xref:class-InputManager), otherwise it maps to layout and platform dependent key mapping. Starting from 2022.1 "Use Physical Keys" is enabled by default.
    ///
    ///Use this class to read the axes set up in the [Input Manager](xref:class-InputManager), and to access multi-touch/accelerometer data on mobile devices.
    ///
    ///To read an axis use <see cref="Input.GetAxis" /> with one of the following default axes:
    ///"Horizontal" and "Vertical" are mapped to joystick, <c>A</c>, <c>W</c>, <c>S</c>, <c>D</c> and the arrow keys.
    ///"Mouse X" and "Mouse Y" are mapped to the mouse delta.
    ///"Fire1", "Fire2" "Fire3" are mapped to <c>Ctrl</c>, <c>Alt</c>, <c>Cmd</c> keys and three mouse or joystick buttons.
    ///New input axes can be added. See [Input Manager](xref:class-InputManager) for this.
    ///
    ///If you are using input for any kind of movement behaviour use <see cref="Input.GetAxis" />.
    ///It gives you smoothed and configurable input that can be mapped to a keyboard, joystick or mouse.
    ///Use <see cref="Input.GetButton" /> for action-like events only. Do not use it for movement. <see cref="Input.GetAxis" /> will make the script code smaller and simpler.
    ///
    ///**Note:** <see cref="Input" /> flags are not reset until <c>Update</c>. You should make all the <see cref="Input" /> calls in the <c>Update</c> Loop.
    ///
    ///
    ///
    ///**Mobile Devices:**
    ///
    ///iOS and Android devices are capable of tracking multiple fingers touching the screen simultaneously.
    ///You can access data on the status of each finger touching screen during the last frame by using the <see cref="Input.touches" /> property array.
    ///
    ///As a device moves, its accelerometer hardware reports linear acceleration changes along the three primary axes in three-dimensional space.
    ///You can use this data to detect both the current orientation of the device (relative to the ground) and any immediate changes to that orientation.
    ///
    ///Acceleration along each axis is reported directly by the hardware as G-force values.
    ///A value of 1.0 represents a load of about +1g along a given axis while a value of -1.0 represents -1g.
    ///If you hold the device upright (with the home button at the bottom) in front of you, the X axis is positive along the right,
    ///the Y axis is positive directly up, and the Z axis is positive pointing toward you.
    ///
    ///You can use the <see cref="Input.acceleration" /> property to get the accelerometer reading.
    ///You can also use the <see cref="Input.deviceOrientation" /> property to get a discrete evaluation of the device's orientation in three-dimensional space.
    ///Detecting a change in orientation can be useful if you want to create game behaviors when the user rotates the device to hold it differently.
    ///
    ///Note that the accelerometer hardware can be polled more than once per frame.
    ///To access all accelerometer samples since the last frame, you can use the <see cref="Input.accelerationEvents" /> property array.
    ///This can be useful when reconstructing player motions, feeding acceleration data into a predictor, or implementing other precise motion analysis.</para>
    ///  <para>This component relates to legacy methods for drawing UI textures and images to
    ///the screen. You should instead use UI system. This is also
    ///unrelated to the IMGUI system.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public void Update()
    ///    {
    ///        if (Input.GetButtonDown("Fire1"))
    ///        {
    ///            Debug.Log(Input.mousePosition);
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="KeyCode" />
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public partial class Input
    {
        ///<summary>Returns the value of the virtual axis identified by <c>axisName</c>.</summary>
        ///<remarks>
        ///  <para>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The value will be in the range -1...1 for keyboard and joystick input devices.
        ///
        ///The meaning of this value depends on the type of input control, for example with a joystick's horizontal axis a value of 1 means the stick is pushed all the way to the right and a value of -1 means it's all the way to the left; a value of 0 means the joystick is in its neutral position.
        ///
        ///If the axis is mapped to the mouse, the value is different and will not be in the range of -1...1. Instead it'll be the current mouse delta multiplied by the axis sensitivity. Typically a positive value means the mouse is moving right/down and a negative value means the mouse is moving left/up.
        ///
        ///This is frame-rate independent; you do not need to be concerned about varying frame-rates when using this value.
        ///
        ///To set up your input or view the options for <c>axisName</c>, go to **Edit** &gt; **Project Settings** &gt; **Input Manager**. This brings up the Input Manager. Expand **Axis** to see the list of your current inputs. You can use one of these as the <c>axisName</c>. To rename the input or change the positive button etc., expand one of the options, and change the name in the **Name** field or **Positive Button** field. Also, change the **Type** to **Joystick Axis**. To add a new input, add 1 to the number in the **Size** field.</para>
        ///  <para>**Note:** The Horizontal and Vertical ranges change from 0 to +1 or -1 with increase/decrease in 0.05f steps.  <see cref="GetAxisRaw" /> has changes from 0 to 1 or -1 immediately, so with no steps.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // A very simplistic car driving on the x-z plane.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float speed = 10.0f;
        ///    public float rotationSpeed = 100.0f;
        ///
        ///    void Update()
        ///    {
        ///        // Get the horizontal and vertical axis.
        ///        // By default they are mapped to the arrow keys.
        ///        // The value is in the range -1 to 1
        ///        float translation = Input.GetAxis("Vertical") * speed;
        ///        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        ///
        ///        // Make it move 10 meters per second instead of 10 meters per frame...
        ///        translation *= Time.deltaTime;
        ///        rotation *= Time.deltaTime;
        ///
        ///        // Move translation along the object's z-axis
        ///        transform.Translate(0, 0, translation);
        ///
        ///        // Rotate around our y-axis
        ///        transform.Rotate(0, rotation, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Performs a mouse look.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    float horizontalSpeed = 2.0f;
        ///    float verticalSpeed = 2.0f;
        ///
        ///    void Update()
        ///    {
        ///        // Get the mouse delta. This is not in the range -1...1
        ///        float h = horizontalSpeed * Input.GetAxis("Mouse X");
        ///        float v = verticalSpeed * Input.GetAxis("Mouse Y");
        ///
        ///        transform.Rotate(v, h, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float GetAxis(string axisName) => Internal.InputUnsafeUtility.GetAxis(axisName);
        ///<summary>Returns the value of the virtual axis identified by <c>axisName</c> with no smoothing filtering applied.</summary>
        ///<remarks>
        ///  <para>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The value will be in the range -1...1 for keyboard and joystick input.
        ///Since input is not smoothed, keyboard input will always be either -1, 0 or 1.
        ///This is useful if you want to do all smoothing of keyboard input processing yourself.</para>
        ///  <para>The <see cref="GetAxis" /> page describes in detail what the <c>axisName</c> for <see cref="GetAxisRaw" /> means.  For example the <c>Horizontal</c> axis is managed by <c>Left</c> and <c>Right</c>, and <c>a</c> and <c>d</c> keys.  Other Input Axes can be seen in the <c>Edit-&gt;Settings-&gt;Input</c> window.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        float speed = Input.GetAxisRaw("Horizontal") * Time.deltaTime;
        ///        transform.Rotate(0, speed, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float GetAxisRaw(string axisName) => Internal.InputUnsafeUtility.GetAxisRaw(axisName);
        ///<summary>Returns true while the virtual button identified by <c>buttonName</c> is held down.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Think auto fire - this will return true as long as the button is held down.
        ///                Use this only when implementing events that trigger an action, eg, shooting a weapon.
        ///                The <c>buttonName</c> argument will normally be one of the names in [InputManager](xref:class-InputManager) such as Jump or
        ///                Fire1.  <see cref="GetButton" /> will return to <c>false</c> when it is released.
        ///
        ///**Note:** Use <see cref="GetAxis" /> for input that controls continuous movement.</remarks>
        ///<param name="buttonName">The name of the button such as Jump.</param>
        ///<returns>True when an axis has been pressed and not released.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Instantiates a projectile every 0.5 seconds,
        /// // if the Fire1 button (default is Ctrl) is pressed.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GameObject projectile;
        ///    public float fireDelta = 0.5F;
        ///
        ///    private float nextFire = 0.5F;
        ///    private GameObject newProjectile;
        ///    private float myTime = 0.0F;
        ///
        ///    void Update()
        ///    {
        ///        myTime = myTime + Time.deltaTime;
        ///
        ///        if (Input.GetButton("Fire1") && myTime > nextFire)
        ///        {
        ///            nextFire = myTime + fireDelta;
        ///            newProjectile = Instantiate(projectile, transform.position, transform.rotation) as GameObject;
        ///
        ///            // create code here that animates the newProjectile
        ///
        ///            nextFire = nextFire - myTime;
        ///            myTime = 0.0F;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetButton(string buttonName) => Internal.InputUnsafeUtility.GetButton(buttonName);
        ///<summary>Returns true during the frame the user pressed down the virtual button identified by <c>buttonName</c>.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has released the key and pressed it again.
        ///
        ///Use this only when implementing action like events IE: shooting a weapon.
        ///
        ///Use <see cref="Input.GetAxis" /> for any kind of movement behaviour.
        ///
        ///To edit, set up, or remove buttons and their names (such as "Fire1"):
        ///1. Go to **Edit** &gt; **Project Settings** &gt; **Input Manager** to bring up the Input Manager.
        ///2. Expand **Axis** by clicking the arrow next to it. This shows the list of the current buttons you have. You can use one of these as the parameter "buttonName".
        ///3. Expand one of the items in the list to access and change aspects such as the button's name and the key, joystick or mouse movement that triggers it.
        ///4. For more information about buttons, see the [Input Manager](xref:class-InputManager) page.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GameObject projectile;
        ///    void Update()
        ///    {
        ///        if (Input.GetButtonDown("Fire1"))
        ///            Instantiate(projectile, transform.position, transform.rotation);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetButtonDown(string buttonName) => Internal.InputUnsafeUtility.GetButtonDown(buttonName);
        ///<summary>Returns true the first frame the user releases the virtual button identified by <c>buttonName</c>.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has pressed the button and released it again.
        ///
        ///Use this only when implementing action like events IE: shooting a weapon.
        ///
        ///Use <see cref="Input.GetAxis" /> for any kind of movement behaviour.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GameObject projectile;
        ///    void Update()
        ///    {
        ///        if (Input.GetButtonUp("Fire1"))
        ///            Instantiate(projectile, transform.position, transform.rotation);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetButtonUp(string buttonName) => Internal.InputUnsafeUtility.GetButtonUp(buttonName);

        [NativeMethod(ThrowsException = true)]
        private extern static bool GetKeyInt(KeyCode key);
        [NativeMethod(ThrowsException = true)]
        private extern static bool GetKeyUpInt(KeyCode key);
        [NativeMethod(ThrowsException = true)]
        private extern static bool GetKeyDownInt(KeyCode key);
        ///<summary>Returns whether the given mouse button is held down.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The button values are: 0 for the left button, 1 for the right button, 2 for the middle button.  The return is <c>true</c> when the mouse button is pressed down, and <c>false</c> when released.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Detects clicks from the mouse and prints a message
        /// // depending on the click detected.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetMouseButton(0))
        ///        {
        ///            Debug.Log("The left mouse button is being held down.");
        ///        }
        ///
        ///        if (Input.GetMouseButton(1))
        ///        {
        ///            Debug.Log("The right mouse button is being held down.");
        ///        }
        ///
        ///        if (Input.GetMouseButton(2))
        ///        {
        ///            Debug.Log("The middle mouse button is being held down.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static bool GetMouseButton(int button);
        ///<summary>Returns true during the frame the user pressed the given mouse button.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has released the mouse button and pressed it again.
        ///button values are 0 for left button, 1 for right button, 2 for the middle button.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Detects clicks from the mouse and prints a message
        /// // depending on the click detected.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetMouseButtonDown(0))
        ///            Debug.Log("Pressed left-click.");
        ///
        ///        if (Input.GetMouseButtonDown(1))
        ///            Debug.Log("Pressed right-click.");
        ///
        ///        if (Input.GetMouseButtonDown(2))
        ///            Debug.Log("Pressed middle-click.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static bool GetMouseButtonDown(int button);
        ///<summary>Returns true during the frame the user releases the given mouse button.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has pressed the mouse button and released it again.
        ///button values are 0 for left button, 1 for right button, 2 for the middle button.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects clicks from the mouse and prints a message
        ///    // depending on the click detected.
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetMouseButtonUp(0))
        ///        {
        ///            Debug.Log("Pressed left click.");
        ///        }
        ///        if (Input.GetMouseButtonUp(1))
        ///        {
        ///            Debug.Log("Pressed right click.");
        ///        }
        ///        if (Input.GetMouseButtonUp(2))
        ///        {
        ///            Debug.Log("Pressed middle click.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static bool GetMouseButtonUp(int button);
        ///<summary>Resets all input. After ResetInputAxes all axes return to 0 and all buttons return to 0 for one frame.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///This can be useful when respawning the player and you don't want any input from keys that might still be held down.</remarks>
        [FreeFunction("ResetInput")]
        public extern static void ResetInputAxes();
        ///<summary>Determine whether a particular joystick model has been preconfigured by Unity. (Linux-only).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Preconfigured joysticks report indices for buttons and axes in the following order.
        ///Buttons: A, B, X, Y, left bumper, right bumper, select, start, guide, left stick press, right stick press
        ///Axes: left stick x, left stick y, left trigger, right stick x, right stick y, right trigger, dpad horizontal, dpad vertical.</remarks>
        ///<param name="joystickName">The name of the joystick to check (returned by <see cref="Input.GetJoystickNames" />).</param>
        ///<returns>True if the joystick layout has been preconfigured; false otherwise.</returns>
        public static bool IsJoystickPreconfigured(string joystickName) => Internal.InputUnsafeUtility.IsJoystickPreconfigured(joystickName);
        ///<summary>Retrieves a list of input device names corresponding to the index of an Axis configured within Input Manager.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The strings returned are taken from the connected device's "friendly name" as reported by the operating system. That is, the names are not fixed and will likely vary between devices, drivers, and the OS itself.
        ///
        ///These strings are intended for use within input configuration screens
        ///- this way, instead of showing labels like "Joystick 1", you can show more meaningful names like "Logitech WingMan".
        ///To read values from different joysticks, you need to assign respective axes for the number of joysticks you
        ///want to support in the Input Manager.
        ///
        ///The position of a joystick in this array corresponds to the joystick number, i.e. the name in position 0 of this array is
        ///for the joystick that feeds data into 'Joystick 1' in the Input Manager, the name in position 1 corresponds to 'Joystick 2',
        ///and so on. Note that some entries in the array may be blank if no device is connected for that joystick number.</remarks>
        ///<returns>Returns an array of joystick and gamepad device names.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints a joystick name if movement is detected.
        ///
        ///    void Update()
        ///    {
        ///        // requires you to set up axes "Joy0X" - "Joy3X" and "Joy0Y" - "Joy3Y" in the Input Manager
        ///        for (int i = 0; i < 4; i++)
        ///        {
        ///            if (Mathf.Abs(Input.GetAxis("Joy" + i + "X")) > 0.2 ||
        ///                Mathf.Abs(Input.GetAxis("Joy" + i + "Y")) > 0.2)
        ///            {
        ///                Debug.Log(Input.GetJoystickNames()[i] + " is moved");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static string[] GetJoystickNames();
        ///<summary>Call <see cref="Input.GetTouch" /> to obtain a <see cref="Touch" /> struct.</summary>
        ///<remarks>
        ///  <para>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///<see cref="Input.GetTouch" /> returns <see cref="Touch" /> for a selected screen touch (for example, from a finger or stylus). <see cref="Touch" /> describes the screen touch. The <paramref name="index" /> argument selects the screen touch.
        ///
        ///<see cref="Input.touchCount" /> provides the current number of screen touches. If <see cref="Input.touchCount" /> is greater than zero, the <see cref="GetTouch" /><paramref name="index" /> sets which screen touch to check. <see cref="Touch" /> returns a <c>struct</c> with the screen touch details. Each extra screen touch uses an increasing <see cref="Input.touchCount" />.
        ///
        ///::ref::GetTouch returns a <see cref="Touch" /> struct. Use zero to obtain the first screen touch. As an example, <see cref="Touch" /> includes <see cref="Touch.position" /> in pixels.
        ///
        ///No temporary variables are allocated.</para>
        ///  <para>A second example:</para>
        ///  <para>A third example:</para>
        ///</remarks>
        ///<param name="index">The touch input on the device screen.</param>
        ///<returns>Touch details in the struct.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.iOS;
        ///
        /// // Input.GetTouch example.
        /// //
        /// // Attach to an origin based cube.
        /// // A screen touch moves the cube on an iPhone or iPad.
        /// // A second screen touch reduces the cube size.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private Vector3 position;
        ///    private float width;
        ///    private float height;
        ///
        ///    void Awake()
        ///    {
        ///        width = (float)Screen.width / 2.0f;
        ///        height = (float)Screen.height / 2.0f;
        ///
        ///        // Position used for the cube.
        ///        position = new Vector3(0.0f, 0.0f, 0.0f);
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Compute a fontSize based on the size of the screen width.
        ///        GUI.skin.label.fontSize = (int)(Screen.width / 25.0f);
        ///
        ///        GUI.Label(new Rect(20, 20, width, height * 0.25f),
        ///            "x = " + position.x.ToString("f2") +
        ///            ", y = " + position.y.ToString("f2"));
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Handle screen touches.
        ///        if (Input.touchCount > 0)
        ///        {
        ///            Touch touch = Input.GetTouch(0);
        ///
        ///            // Move the cube if the screen has the finger moving.
        ///            if (touch.phase == TouchPhase.Moved)
        ///            {
        ///                Vector2 pos = touch.position;
        ///                pos.x = (pos.x - width) / width;
        ///                pos.y = (pos.y - height) / height;
        ///                position = new Vector3(-pos.x, pos.y, 0.0f);
        ///
        ///                // Position the cube.
        ///                transform.position = position;
        ///            }
        ///
        ///            if (Input.touchCount == 2)
        ///            {
        ///                touch = Input.GetTouch(1);
        ///
        ///                if (touch.phase == TouchPhase.Began)
        ///                {
        ///                    // Halve the size of the cube.
        ///                    transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        ///                }
        ///
        ///                if (touch.phase == TouchPhase.Ended)
        ///                {
        ///                    // Restore the regular size of the cube.
        ///                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        ///                }
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GameObject projectile;
        ///    public GameObject clone;
        ///
        ///    void Update()
        ///    {
        ///        for (int i = 0; i < Input.touchCount; ++i)
        ///        {
        ///            if (Input.GetTouch(i).phase == TouchPhase.Began)
        ///            {
        ///                clone = Instantiate(projectile, transform.position, transform.rotation) as GameObject;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GameObject particle;
        ///    void Update()
        ///    {
        ///        for (int i = 0; i < Input.touchCount; ++i)
        ///        {
        ///            if (Input.GetTouch(i).phase == TouchPhase.Began)
        ///            {
        ///                // Construct a ray from the current touch coordinates
        ///                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
        ///
        ///                // Create a particle if hit
        ///                if (Physics.Raycast(ray))
        ///                {
        ///                    Instantiate(particle, transform.position, transform.rotation);
        ///                }
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static Touch GetTouch(int index);
        ///<summary>Returns the <see cref="PenData" /> for the pen event at the given index in the pen event queue.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///On Windows, the pen event queue holds, in chronological order, any missed pen events as provided by GetPointerPenInfoHistory. The queue is cleared at the end of each frame. On all other platforms the queue will always be empty.</remarks>
        ///<returns>Pen event details in the struct.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///
        ///public class Example : EditorWindow
        ///{
        ///    [MenuItem("Window/Pen Window")]
        ///    public static void ShowWindow()
        ///    {
        ///        EditorWindow win = EditorWindow.GetWindow(typeof(Example));
        ///        win.titleContent = new GUIContent("Pen Window");
        ///        win.wantsMouseMove = true;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        var e = Event.current;
        ///        if ((e.type == EventType.MouseDown
        ///             || e.type == EventType.MouseDrag
        ///             || e.type == EventType.MouseDown
        ///             || e.type == EventType.MouseUp
        ///             || e.type == EventType.MouseMove)
        ///            && (e.pointerType == PointerType.Pen))
        ///        {
        ///            int count = Input.penEventCount;
        ///            for (int i = 0; i < count; i++)
        ///            {
        ///                // Log data from queued pen events
        ///                PenData p = Input.GetPenEvent(i);
        ///                Debug.Log($"Pen position {p.position}, pen pressure {p.pressure}, pen twist {p.twist}, pen tilt {p.tilt}, pen status - barrel {(p.penStatus & PenStatus.Barrel) != 0}");
        ///            }
        ///            Input.ResetPenEvents();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static PenData GetPenEvent(int index);
        ///<summary>Returns the <see cref="PenData" /> for the last stored pen up or down event.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Use <see cref="GetPenEvent(int)" /> to retrieve previous pen events.</remarks>
        ///<returns>Pen event details in the struct.</returns>
        [NativeMethod(ThrowsException = true)]
        public extern static PenData GetLastPenContactEvent();
        ///<summary>Clears the pen event queue.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The queue is automatically emptied at the end of each frame.</remarks>
        [NativeMethod(ThrowsException = true)]
        public extern static void ResetPenEvents();
        ///<summary>Clears the last stored pen event.
        ///Calling this function may impact event handling for UIToolKit elements.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        [NativeMethod(ThrowsException = true)]
        public extern static void ClearLastPenContactEvent();
        ///<summary>Returns specific acceleration measurement which occurred during last frame. (Does not allocate temporary variables).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Calculates weighted sum of acceleration measurements which occurred during the last frame
        ///    // Might be handy if you want to get more precise measurements
        ///    void Update()
        ///    {
        ///        Vector3 acceleration = Vector3.zero;
        ///        for (var i = 0; i < Input.accelerationEventCount; ++i)
        ///        {
        ///            AccelerationEvent accEvent = Input.GetAccelerationEvent(i);
        ///            acceleration += accEvent.acceleration * accEvent.deltaTime;
        ///        }
        ///        print(acceleration);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static AccelerationEvent GetAccelerationEvent(int index);

        ///<summary>Returns true while the user holds down the key identified by the <c>key</c><see cref="KeyCode" /> enum parameter.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKey(KeyCode.UpArrow))
        ///        {
        ///            print("up arrow key is held down");
        ///        }
        ///
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///        {
        ///            print("down arrow key is held down");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKey(KeyCode key) => GetKeyInt(key);
        ///<summary>Returns true while the user holds down the key identified by <c>name</c>.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///::ref::GetKey will report the status of the named key.  This might be
        ///                used to confirm a key is used for auto fire.  For the list of key identifiers
        ///                see [Input Manager](xref:class-InputManager).
        ///When dealing with input it is recommended to use Input.GetAxis and Input.GetButton instead
        ///since it allows end-users to configure the keys.
        ///
        ///**iOS, tvOS**: Due platform limitations,  <see cref="GetKeyUp" /> event for keyboard events is delayed by about half a second, see UnityView+Keyboard.mm in the generated Xcode project for more information.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKey("up"))
        ///        {
        ///            print("up arrow key is held down");
        ///        }
        ///
        ///        if (Input.GetKey("down"))
        ///        {
        ///            print("down arrow key is held down");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKey(string name) => Internal.InputUnsafeUtility.GetKeyString(name);
        ///<summary>Returns true during the frame the user releases the key identified by the <c>key</c><see cref="KeyCode" /> enum parameter.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyUp(KeyCode.Space))
        ///        {
        ///            print("space key was released");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKeyUp(KeyCode key) => GetKeyUpInt(key);
        ///<summary>Returns true during the frame the user releases the key identified by <c>name</c>.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has pressed the key and released it again.
        ///
        ///For the list of key identifiers see [Input Manager](xref:class-InputManager).
        ///When dealing with input it is recommended to use <see cref="Input.GetAxis" /> and <see cref="Input.GetButton" /> instead since it allows end-users to configure the keys.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyUp("space"))
        ///        {
        ///            print("Space key was released");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKeyUp(string name) => Internal.InputUnsafeUtility.GetKeyUpString(name);
        ///<summary>Returns true during the frame the user starts pressing down the key identified by the <c>key</c><see cref="KeyCode" /> enum parameter.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            Debug.Log("space key was pressed");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKeyDown(KeyCode key) => GetKeyDownInt(key);
        ///<summary>Returns true during the frame the user starts pressing down the key identified by <c>name</c>.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Call this function from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has released the key and pressed it again.
        ///
        ///When dealing with input it is recommended to use <see cref="Input.GetAxis" /> and <see cref="Input.GetButton" /> instead
        ///since it allows end-users to configure the keys.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown("space"))
        ///        {
        ///            Debug.Log("space key was pressed");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool GetKeyDown(string name) => Internal.InputUnsafeUtility.GetKeyDownString(name);

        [Conditional("UNITY_EDITOR")]
        internal static void SimulateTouch(Touch touch)
        {
            SimulateTouchInternal(touch, DateTime.Now.Ticks);
        }

        [Conditional("UNITY_EDITOR")]
        [NativeConditional("UNITY_EDITOR")]
        [FreeFunction("SimulateTouch")]
        private extern static void SimulateTouchInternal(Touch touch, long timestamp);

        ///<summary>Enables/Disables mouse simulation with touches. By default this option is enabled.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///If enabled, up to three concurrent touches are translated to state on the respective mouse buttons (example: a two-finger tap will be equal to a right-button mouse click).</remarks>
        public extern static bool simulateMouseWithTouches { get; set; }
        ///<summary>Is any key or mouse button currently held down? (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects if any key has been pressed.
        ///
        ///    void Update()
        ///    {
        ///        if (Input.anyKey)
        ///        {
        ///            Debug.Log("A key or mouse click has been detected");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static bool anyKey { get; }
        ///<summary>Returns true the first frame the user hits any key or mouse button. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///You should be polling this variable from the <c>Update</c> function, since the state gets reset each frame.
        ///It will not return true until the user has released all keys / buttons and pressed any key / buttons again.
        ///This does not detect touches. For touches, use <see cref="Input.touchCount" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Detects if any key has been pressed down.
        ///
        ///    void Update()
        ///    {
        ///        if (Input.anyKeyDown)
        ///        {
        ///            Debug.Log("A key or mouse click has been detected");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static bool anyKeyDown { get; }
        ///<summary>Returns the keyboard input entered this frame. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Only ASCII characters are contained in the <c>inputString</c>.
        ///
        ///The string can contain two special characters which should be handled:
        ///Character <c>"\b"</c> represents backspace.
        ///
        ///Character <c>"\n"</c> represents return or enter.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Text  gt;
        ///
        ///    void Start()
        ///    {
        ///        gt = GetComponent<Text>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        foreach (char c in Input.inputString)
        ///        {
        ///            if (c == '\b') // has backspace/delete been pressed?
        ///            {
        ///                if (gt.text.Length != 0)
        ///                {
        ///                    gt.text = gt.text.Substring(0, gt.text.Length - 1);
        ///                }
        ///            }
        ///            else if ((c == '\n') || (c == '\r')) // enter/return
        ///            {
        ///                print("User entered their name: " + gt.text);
        ///            }
        ///            else
        ///            {
        ///                gt.text += c;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static string inputString { get; }
        ///<summary>The current mouse position in pixel coordinates. (Read Only).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///<see cref="Input.mousePosition" /> is a <see cref="Vector3" /> for compatibility with functions that have <see cref="Vector3" /> arguments. The z component of the <see cref="Vector3" /> is always 0.
        ///
        ///The bottom-left of the screen or window is at (0, 0).
        ///The top-right of the screen or window is at (<see cref="Screen.width" />, <see cref="Screen.height" />).
        ///
        ///Note: <see cref="Input.mousePosition" /> reports the position of the mouse even when it is not inside the Game View, such as when <see cref="Cursor.lockState" /> is set to <see cref="CursorLockMode.None" />. When running in windowed mode with an unconfined cursor, position values smaller than 0 or greater than the screen dimensions (<see cref="Screen.width" />,<see cref="Screen.height" />) indicate that the mouse cursor is outside of the game window.
        ///
        ///In the following example, the x and y coordinates of the mouse position are printed when the “Fire1” button is clicked.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.GetButtonDown("Fire1"))
        ///        {
        ///            Vector3 mousePos = Input.mousePosition;
        ///            {
        ///                Debug.Log(mousePos.x);
        ///                Debug.Log(mousePos.y);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static Vector3 mousePosition { get; }
        ///<summary>The current mouse position delta in pixel coordinates. (Read Only).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///<see cref="Input.mousePositionDelta" /> is a <see cref="Vector3" /> for compatibility with functions that have <see cref="Vector3" /> arguments. The z component of the <see cref="Vector3" /> is always 0.
        ///
        ///Note: You should use <see cref="Input.mousePositionDelta" /> instead of <see cref="Input.mousePosition" /> when <see cref="Cursor.lockState" /> is set to <see cref="CursorLockMode.Locked" />, since when cursor is locked, the mouse position remains stationary when moving the mouse, thus only position delta gives you the information about mouse movement.</remarks>
        [NativeMethod(ThrowsException = true)]
        public extern static Vector3 mousePositionDelta { get; }
        ///<summary>The current mouse scroll delta. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///<see cref="Input.mouseScrollDelta" /> is stored in a <see cref="Vector2.y" /> property. (The <see cref="Vector2.x" /> value is ignored.) <see cref="Input.mouseScrollDelta" /> can be positive (up) or negative (down). The value is zero when the mouse scroll is not rotated. Note that a mouse with a center scroll wheel is typical on a PC. Modern <c>macOS</c> uses double finger movement up and down on the trackpad to emulate center scrolling. The value returned by <see cref="mouseScrollDelta" /> will need to be adjusted according to the scroll rate. In the example below a <c>scale</c> of /0.1f/ is used.
        ///
        ///Note that <see cref="mouseScrollDelta" /> is read-only.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        /// // Input.mouseScrollDelta example
        /// //
        /// // Create a sphere moved by a mouse scrollwheel or two-finger
        /// // slide on a Mac trackpad.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private Transform sphere;
        ///    private float scale;
        ///
        ///    void Awake()
        ///    {
        ///        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ///        sphere = go.transform;
        ///
        ///        // create a yellow quad
        ///        go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ///        go.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
        ///        go.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
        ///        go.GetComponent<Renderer>().material.color = new Color(0.75f, 0.75f, 0.0f, 0.5f);
        ///
        ///        // change the camera color and position
        ///        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        ///        Camera.main.transform.position = new Vector3(2, 1, 5);
        ///        Camera.main.transform.Rotate(0, -160, 0);
        ///
        ///        scale = 0.1f;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        Vector3 pos = sphere.position;
        ///        pos.y += Input.mouseScrollDelta.y * scale;
        ///        sphere.position = pos;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(ThrowsException = true)]
        public extern static Vector2 mouseScrollDelta { get; }
        ///<summary>Controls enabling and disabling of IME input composition.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Some languages use complex input methods which involve opening windows to insert characters.
        ///Typically, this is not desirable while playing a game, as games may just interpret key strokes
        ///as game input, not as text. By default, Unity will enable IME composition when in text fields,
        ///and disable it otherwise. However, when you want to implement your own input GUI, you may want
        ///to have control over this yourself, which is possible using the imeCompositionMode property. Set
        ///it to <c>Auto</c> for the default behavior, or <c>On</c> or <c>Off</c> to explicitly enable or disable IME
        ///composition.</remarks>
        public extern static IMECompositionMode imeCompositionMode { get; set; }
        ///<summary>The current IME composition string being typed by the user.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///In some languages such as Chinese, Japanese or Korean, text is input by typing multiple keys to generate
        ///one or multiple characters. These characters are visually composed on the screen as the user types.
        ///When using Unity's built in GUI system for text input, Unity will take care of displaying the composition
        ///strings as the users types. If you want to implement your own GUI, however, you need to take care of
        ///displaying the string at the current cursor position. The composition string is only updated when IME
        ///compositing is used. See <see cref="Input.imeCompositionMode" /> for more info.</remarks>
        ///<seealso cref="Input.imeCompositionMode" />
        ///<seealso cref="Input.compositionCursorPos" />
        public extern static string compositionString { get; }
        ///<summary>Does the user have an IME keyboard input source selected?</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///This returns true if the users keyboard is currently configured for IME input, and false otherwise.
        ///Since users of asian languages can typically turn IME conversion on or off using a keystroke, it is useful
        ///to provide some visual indication of IME being enabled. This can be done by checking <see cref="Input.imeIsSelected" />.</remarks>
        public extern static bool imeIsSelected { get; }
        ///<summary>The current text input position used by IMEs to open windows.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Some language IMEs such as Japanese will open windows while the user is typing text, to aid the user
        ///in picking the correct input strings. These windows are expected to pop up at the current cursor position,
        ///so the IME needs to know where input is displayed. When using Unity's built in GUI system for text input,
        ///Unity will take care of setting the cursor position for the IME. However, if you wish to implement your
        ///own GUI for text input, you need to set this to the current text input position for IME windows to
        ///show up correctly.</remarks>
        ///<seealso cref="Input.imeCompositionMode" />
        ///<seealso cref="Input.compositionString" />
        public extern static Vector2 compositionCursorPos { get; set; }
        ///<summary>Property indicating whether keypresses are eaten by a textinput if it has focus (default true).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///This will avoid keypresses seeping through to the underlying gameview.
        ///This property must be set to false for anyKey or GetKey to work while a textfield has focus.
        ///IME input in the macOS web plugin is disabled when this is set to false.</remarks>
        [Obsolete("eatKeyPressOnTextFieldFocus property is deprecated, and only provided to support legacy behavior.")]
        public extern static bool eatKeyPressOnTextFieldFocus { get; set; }

        [AutoStaticsCleanupOnCodeReload]
        internal static bool simulateTouchEnabled { get; set; }

        [FreeFunction("GetMousePresent")]
        private extern static bool GetMousePresentInternal();

        [FreeFunction("IsTouchSupported")]
        private extern static bool GetTouchSupportedInternal();

        ///<summary>Indicates if a mouse device is detected.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///On Windows, Android and Metro platforms, this function does actual mouse presence detection, so may return true or false.
        ///On Linux, Mac, WebGL, this function will always return true.
        ///On iOS and console platforms, this function will always return false.</remarks>
        public static bool mousePresent => !simulateTouchEnabled && GetMousePresentInternal();
        ///<summary>Returns whether the device on which application is currently running supports touch input.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Rather than checking the platform, use this property to determine whether your game should expect touch input, as some platforms can support multiple input methods.</remarks>
        public static bool touchSupported => simulateTouchEnabled || GetTouchSupportedInternal();

        ///<summary>Returns the number of queued pen events that can be accessed by calling <see cref="GetPenEvent(int)" />.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///The queue is cleared at the end of each frame.</remarks>
        public extern static int penEventCount
        {
            [FreeFunction("GetPenEventCount")]
            get;
        }

        ///<summary>Number of touches. Guaranteed not to change throughout the frame. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.touchCount > 0)
        ///        {
        ///            print(Input.touchCount);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static int touchCount
        {
            [FreeFunction("GetTouchCount")]
            get;
        }
        ///<summary>Bool value which let's users check if touch pressure is supported.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public extern static bool touchPressureSupported
        {
            [FreeFunction("IsTouchPressureSupported")]
            get;
        }
        ///<summary>Returns true when Stylus Touch is supported by a device or platform.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public extern static bool stylusTouchSupported
        {
            [FreeFunction("IsStylusTouchSupported")]
            get;
        }
        ///<summary>Property indicating whether the system handles multiple touches.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public extern static bool multiTouchEnabled
        {
            [FreeFunction("IsMultiTouchEnabled")]
            get;
            [FreeFunction("SetMultiTouchEnabled")]
            set;
        }
        ///<exclude />
        [Obsolete("isGyroAvailable property is deprecated. Please use SystemInfo.supportsGyroscope instead.")]
        public extern static bool isGyroAvailable
        {
            [FreeFunction("IsGyroAvailable")]
            get;
        }
        ///<summary>Device physical orientation as reported by OS. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.deviceOrientation == DeviceOrientation.FaceDown)
        ///            audioSource.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static DeviceOrientation deviceOrientation
        {
            [FreeFunction("GetDeviceOrientation")]
            get;
        }
        ///<summary>Last measured linear acceleration of a device in three-dimensional space. (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Move object using accelerometer
        ///    float speed = 10.0f;
        ///
        ///    void Update()
        ///    {
        ///        Vector3 dir = Vector3.zero;
        ///
        ///        // we assume that device is held parallel to the ground
        ///        // and Home button is in the right hand
        ///
        ///        // remap device acceleration axis to game coordinates:
        ///        //  1) XY plane of the device is mapped onto XZ plane
        ///        //  2) rotated 90 degrees around Y axis
        ///        dir.x = -Input.acceleration.y;
        ///        dir.z = Input.acceleration.x;
        ///
        ///        // clamp acceleration vector to unit sphere
        ///        if (dir.sqrMagnitude > 1)
        ///            dir.Normalize();
        ///
        ///        // Make it move 10 meters per second instead of 10 meters per frame...
        ///        dir *= Time.deltaTime;
        ///
        ///        // Move object
        ///        transform.Translate(dir * speed);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static Vector3 acceleration
        {
            [FreeFunction("GetAcceleration")]
            get;
        }
        ///<summary>This property controls if input sensors should be compensated for screen orientation.</summary>
        ///<remarks>Compensated sensors are accelerometer, compass, gyroscope.
        ///
        ///**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public extern static bool compensateSensors
        {
            [FreeFunction("IsCompensatingSensors")]
            get;
            [FreeFunction("SetCompensatingSensors")]
            set;
        }
        ///<summary>Number of acceleration measurements which occurred during last frame.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Check if we got any acceleration measurements during last frame
        ///
        ///    void Update()
        ///    {
        ///        if (Input.accelerationEventCount > 0)
        ///        {
        ///            print("We got new acceleration measurements");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static int accelerationEventCount
        {
            [FreeFunction("GetAccelerationCount")]
            get;
        }
        ///<summary>Should  **Back** button quit the application?</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Only usable on Android or Universal Windows Platform (UWP).
        ///
        ///By default this property is set to false, which means you're responsible for responding to **Back** button. You can do this by calling <see cref="Input.GetKey" /> and passing <see cref="KeyCode.Escape" />.
        ///
        ///If you set this property to true, clicking the **Back** button:
        ///
        /// * minimizes the application on Android.
        ///
        /// * suspends the application on UWP.</remarks>
        public extern static bool backButtonLeavesApp
        {
            [FreeFunction("GetBackButtonLeavesApp")]
            get;
            [FreeFunction("SetBackButtonLeavesApp")]
            set;
        }

        [AutoStaticsCleanupOnCodeReload]
        private static LocationService locationServiceInstance;
        ///<summary>Property for accessing device location (handheld devices only). (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public static LocationService location
        {
            get
            {
                if (locationServiceInstance == null)
                    locationServiceInstance = new LocationService();
                return locationServiceInstance;
            }
        }
        [AutoStaticsCleanupOnCodeReload]
        private static Compass compassInstance;
        ///<summary>Property for accessing compass (handheld devices only). (RO)</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        public static Compass compass
        {
            get
            {
                if (compassInstance == null)
                    compassInstance = new Compass();
                return compassInstance;
            }
        }
        [FreeFunction("GetGyro")]
        private extern static int GetGyroInternal();
        [AutoStaticsCleanupOnCodeReload]
        private static Gyroscope s_MainGyro;
        ///<summary>Returns default gyroscope.</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Use this to return the gyroscope details of your device. Ensure first that your device has a gyroscope. Use Input.gyro.enabled to check this.
        ///
        ///Knowing the gyroscope details of a device enables you the ability to include features that need to know a device’s orientation. Common uses include changing camera angles or GameObject’s positions when a user rotates and moves their device.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject in your Scene.
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///
        ///public class InputGyroExample : MonoBehaviour
        ///{
        ///    Gyroscope m_Gyro;
        ///
        ///    void Start()
        ///    {
        ///        //Set up and enable the gyroscope (check your device has one)
        ///        m_Gyro = Input.gyro;
        ///        m_Gyro.enabled = true;
        ///    }
        ///
        /// //This is a legacy function, check out the UI section for other ways to create your UI
        ///    void OnGUI()
        ///    {
        ///        //Output the rotation rate, attitude and the enabled state of the gyroscope as a Label
        ///        GUI.Label(new Rect(500, 300, 200, 40), "Gyro rotation rate " + m_Gyro.rotationRate);
        ///        GUI.Label(new Rect(500, 350, 200, 40), "Gyro attitude" + m_Gyro.attitude);
        ///        GUI.Label(new Rect(500, 400, 200, 40), "Gyro enabled : " + m_Gyro.enabled);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Gyroscope gyro
        {
            get
            {
                if (s_MainGyro == null)
                    s_MainGyro = new Gyroscope(GetGyroInternal());
                return s_MainGyro;
            }
        }

        ///<summary>Returns list of objects representing status of all touches during last frame. (RO) (Allocates temporary variables).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).
        ///
        ///Each entry represents a status of a finger touching the screen.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints number of fingers touching the screen
        ///    void Update()
        ///    {
        ///        var fingerCount = 0;
        ///        foreach (Touch touch in Input.touches)
        ///        {
        ///            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
        ///            {
        ///                fingerCount++;
        ///            }
        ///        }
        ///        if (fingerCount > 0)
        ///        {
        ///            print("User has " + fingerCount + " finger(s) touching the screen");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Touch[] touches
        {
            get
            {
                int count = touchCount;
                Touch[] touches = new Touch[count];
                for (int q = 0; q < count; ++q)
                    touches[q] = GetTouch(q);
                return touches;
            }
        }

        ///<summary>Returns list of acceleration measurements which occurred during the last frame. (RO) (Allocates temporary variables).</summary>
        ///<remarks>**Note**: This API is part of the legacy Input Manager. The recommended best practice is that you don't use this API in new projects. For new projects, use the Input System package. To learn more about input, refer to [Input](xref:Input).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Calculates weighted sum of acceleration measurements which occurred during the last frame
        ///    // Might be handy if you want to get more precise measurements
        ///
        ///    void Update()
        ///    {
        ///        Vector3 acceleration = Vector3.zero;
        ///        foreach (AccelerationEvent accEvent in Input.accelerationEvents)
        ///        {
        ///            acceleration += accEvent.acceleration * accEvent.deltaTime;
        ///        }
        ///        print(acceleration);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static AccelerationEvent[] accelerationEvents
        {
            get
            {
                int count = accelerationEventCount;
                AccelerationEvent[] events = new AccelerationEvent[count];
                for (int q = 0; q < count; ++q)
                    events[q] = GetAccelerationEvent(q);
                return events;
            }
        }

        internal extern static bool CheckDisabled();
    }
}
