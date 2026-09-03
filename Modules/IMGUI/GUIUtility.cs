// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIFramework not yet converted
// Use this define to debug who grabs and releases hotcontrol
//#define DEBUG_HOTCONTROL
// Use this define to debug controlID consistency together with 's_LogControlID' (default false) to enable logging in
// a codepath thats needs tested for consistency. E.g:
//  if (Event.current.rawType == EventType.MouseUp)
//      GUIUtility.s_LogControlID = true;
// And remember to set s_LogControlID to false at end of section of interest.
//#define DEBUG_CONTROLID
using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;



namespace UnityEngine
{
    ///<summary>An exception that will prevent all subsequent immediate mode GUI functions from evaluating for the remainder of the GUI loop.</summary>
    ///<remarks>If you have exception handling in your <see cref="GUI" /> code, it should not catch this exception type, as Unity's immediate mode GUI system relies on this exception to exit the current <see cref="GUI" /> loop properly in some cases.
    ///
    ///If you need to exit the immediate mode GUI loop in your own code, you should call <see cref="GUIUtility.ExitGUI" /> rather than throwing this exception directly.</remarks>
    public sealed class ExitGUIException : Exception
    {
        ///<exclude />
#pragma warning disable UAL0015 // thrown and caught synchronously within the same GUI call; guiIsExiting is a transient dispatch signal, never persisted across a reload boundary
        public ExitGUIException()
        {
            GUIUtility.guiIsExiting = true;
        }
#pragma warning restore UAL0015

#pragma warning disable UAL0015 // thrown and caught synchronously within the same GUI call; guiIsExiting is a transient dispatch signal, never persisted across a reload boundary
        internal ExitGUIException(string message)
            : base(message)
        {
            GUIUtility.guiIsExiting = true;
            Console.WriteLine(message);
        }
#pragma warning restore UAL0015
    }

    // Utility class for making new GUI controls.
    public partial class GUIUtility
    {
        [AutoStaticsCleanupOnCodeReload]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static int s_SkinMode;
        ///<exclude />
        [AutoStaticsCleanupOnCodeReload]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static EntityId s_OriginalID;

        // IoC callbacks for UIElements
        [NoAutoStaticsCleanup] // wired once by IMGUIContainer's static ctor (UIElementsModule.dll, never reloaded); clearing on reload orphans it permanently
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Action takeCapture;
        [NoAutoStaticsCleanup] // wired once by IMGUIContainer's static ctor (UIElementsModule.dll, never reloaded); clearing on reload orphans it permanently
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Action releaseCapture;
        [NoAutoStaticsCleanup] // set by non-reloaded engine/editor code; no reload hook re-populates it once cleared
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Func<EntityId, IntPtr, bool> processEvent;
        [NoAutoStaticsCleanup] // wired once by IMGUIContainer's static ctor (UIElementsModule.dll, never reloaded); clearing on reload orphans it permanently
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Func<Exception, bool> endContainerGUIFromException;
        [NoAutoStaticsCleanup] // wired once by IMGUIContainer's static ctor (UIElementsModule.dll, never reloaded); clearing on reload orphans it permanently
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Action guiChanged;





        [RequiredByNativeCode]
        private static void MarkGUIChanged()
        {
            guiChanged?.Invoke();
        }

        ///<summary>Get a unique ID for a control.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints a not used ID that can be assigned to a control
        ///
        ///    void OnGUI()
        ///    {
        ///        // Gets a ID for a control that cannot receive keyboard focus (A button)
        ///        Debug.Log("Available id: " + GUIUtility.GetControlID(FocusType.Passive));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int GetControlID(FocusType focus)
        {
            return GetControlID(0, focus);
        }

        ///<summary>Get a unique ID for a control, using a the label content as a hint to help ensure correct matching of IDs to controls.</summary>
        public static int GetControlID(GUIContent contents, FocusType focus)
        {
            return GetControlID(contents.hash, focus);
        }

        ///<summary>Get a unique ID for a control.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints a not used ID that can be assigned to a control
        ///
        ///    void OnGUI()
        ///    {
        ///        // Gets a ID for a control that cannot receive keyboard focus (A button)
        ///        Debug.Log("Available id: " + GUIUtility.GetControlID(FocusType.Passive));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int GetControlID(FocusType focus, Rect position)
        {
            return GetControlID(0, focus, position);
        }

        ///<summary>Get a unique ID for a control, using a the label content as a hint to help ensure correct matching of IDs to controls.</summary>
        public static int GetControlID(GUIContent contents, FocusType focus, Rect position)
        {
            return GetControlID(contents.hash, focus, position);
        }

        ///<summary>Get a unique ID for a control, using an integer as a hint to help ensure correct matching of IDs to controls.</summary>
        public static int GetControlID(int hint, FocusType focus)
        {
            GUIUtility.CheckOnGUI();
            return GetControlID(hint, focus, Rect.zero);
        }

        ///<summary>Get a state object from a controlID.</summary>
        ///<remarks>This will return a recycled state object that is unique for <c>controlID</c>.
        ///        If there is no state object then a new one will be created and hooked up to the <c>controlID</c>.
        ///
        ///
        ///
        ///On the first call into <see cref="GetStateObject" /> a new state object will be created.
        ///The <c>controlID</c> uniquely refers to this object.  On subsequent calls the stored object will be returned.</remarks>
        ///<seealso cref="GUIUtility.QueryStateObject" />
        public static object GetStateObject(Type t, int controlID)     { return GUIStateObjects.GetStateObject(t, controlID); }

        ///<summary>Get an existing state object from a controlID.</summary>
        ///<remarks>This will return a recycled state object that is unique for <c>controlID</c>.
        ///        If the state object has not been created by calling <see cref="GetStateObject" /> then it
        ///        cannot be accessed using <see cref="QueryStateObject" />.  A call into <see cref="QueryStateObject" />
        ///        with the state object not created is invalid.  A null may be returned, but is not
        ///        guaranteed.  An exception may happen instead.</remarks>
        ///<seealso cref="GUIUtility.GetStateObject" />
        public static object QueryStateObject(Type t, int controlID)       { return GUIStateObjects.QueryStateObject(t, controlID); }

        [AutoStaticsCleanupOnCodeReload]
        internal static bool guiIsExiting { get; set; }



        ///<summary>The controlID of the current hot control.</summary>
        ///<remarks>The hot control is one that is temporarily active. When the user mousedown's on a button, it becomes hot. 
        ///
        ///No other controls are allowed to respond to mouse events while some other control is hot.
        ///
        ///once the user mouseup's, the control sets <c>hotControl</c> to 0 in order to indicate that other controls can now respond to user input.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Click on the button to see the id
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Button("Press Me!");
        ///        Debug.Log("id: " + GUIUtility.hotControl);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int hotControl
        {
            get { return Internal_GetHotControl(); }
            set
            {
                // Some place set it back to 0 on focus changes preventively, especially in tests.
                if (value != 0)
                    WarnOnGUI();
                Internal_SetHotControl(value);
            }
        }

        [RequiredByNativeCode]
        internal static void TakeCapture()
        {
            WarnOnGUI();
            takeCapture?.Invoke();
        }

        [RequiredByNativeCode]
        internal static void RemoveCapture()
        {
            releaseCapture?.Invoke();
        }

        ///<summary>The controlID of the control that has keyboard focus.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Click on the text field to see the id of the control.
        ///
        ///    string str = "A String!";
        ///    void OnGUI()
        ///    {
        ///        str = GUILayout.TextField(str, 10);
        ///        Debug.Log("id: " + GUIUtility.keyboardControl);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int keyboardControl
        {
            get { WarnOnGUI(); return Internal_GetKeyboardControl(); }
            set
            {
                WarnOnGUI();
                Internal_SetKeyboardControl(value);
            }
        }

        [NoAutoStaticsCleanup] // wired once by EditorGUIUtility's static ctor (UnityEditor.dll, never reloaded); clearing on reload orphans it permanently
        internal static Func<bool> s_HasCurrentWindowKeyFocusFunc;

        internal static bool HasKeyFocus(int controlID)
        {
            // No need for WarnOnGUI() : keyboardControl already has the check and it would fire twice
            return controlID == GUIUtility.keyboardControl &&
                (s_HasCurrentWindowKeyFocusFunc != null ? s_HasCurrentWindowKeyFocusFunc() : true);
        }

        ///<summary>Puts the GUI in a state that will prevent all subsequent immediate mode GUI functions from evaluating for the remainder of the GUI loop by throwing an <see cref="ExitGUIException" />.</summary>
        ///<remarks>In Unity's immediate mode GUI system, the GUI loop procedes by calling <see cref="GUI" /> methods during a sequence of <see cref="Event" />s and these methods take action according to the <see cref="Event.type" />. For example, when using <see cref="GUILayout" />, controls will first receive a <see cref="EventType.Layout" /> event to determine how much space they need, and then later receive a <see cref="EventType.Repaint" /> event to actually draw into the space allocated for them.
        ///
        ///In this sequence, it is expected that control IDs are requested and used in the same order for each <see cref="Event" /> that is processed during the GUI loop, and that the event loop does not re-enter itself. Use <see cref="GUIUtility.ExitGUI" /> in situations that might violate these assumptions, such as when a change in some value might change what controls are displayed next. Using this method can prevent errors such as <c>ArgumentException: Getting control 0's position in a group with only 0 controls when doing Repaint</c>.</remarks>
        ///<seealso cref="GetControlID" />
        public static void ExitGUI()
        {
            WarnOnGUI();
            // We have to always throw the ExitGUIException otherwise the exiting out of recursive on GUI will not work.
            throw new ExitGUIException();
        }

        internal static GUISkin GetDefaultSkin(int skinMode)
        {
            return Internal_GetDefaultSkin(skinMode) as GUISkin;
        }

        internal static GUISkin GetDefaultSkin()
        {
            return Internal_GetDefaultSkin(s_SkinMode) as GUISkin;
        }

        // internal so we can get to it from EditorGUIUtility.GetBuiltinSkin
        internal static GUISkin GetBuiltinSkin(int skin)
        {
            return Internal_GetBuiltinSkin(skin) as GUISkin;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void EndContainer()
        {
            Internal_EndContainer();
            Internal_ExitGUI();
        }

        [RequiredByNativeCode]
        internal static void BeginGUI(int skinMode, EntityId entityId, int useGUILayout)
        {
            s_SkinMode = skinMode;
            s_OriginalID = entityId;

            ResetGlobalState();

            // Switch to the correct ID list & clear keyboard loop if we're about to layout (we rebuild it during layout, so we want it cleared beforehand)
            if (useGUILayout != 0)
            {
                GUILayoutUtility.Begin(entityId);
            }
        }

        [RequiredByNativeCode]
        internal static void DestroyGUI(EntityId entityId)
        {
            GUILayoutUtility.RemoveSelectedIdListLayout(entityId);
        }

        [RequiredByNativeCode]
        internal static void SetSkin(int skinMode)
        {
            s_SkinMode = skinMode;
            GUI.DoSetSkin(null);
        }


        [RequiredByNativeCode]
        internal static void EndGUI()
        {
            try
            {
                // Layout events only reach this point when BeginGUI was called with useGUILayout != 0
                // (i.e. kGameLayout), so current.topLevel is always valid here.
                if (Event.current.type == EventType.Layout)
                    GUILayoutUtility.Layout();

                GUILayoutUtility.SelectIDListLayout(s_OriginalID);
                GUIContent.ClearStaticCache();
            }
            finally
            {
                Internal_ExitGUI();
            }
        }

        // End the 2D GUI.
        [RequiredByNativeCode]
        internal static bool EndGUIFromException(Exception exception)
        {
            Internal_ExitGUI();

            return ShouldRethrowException(exception);
        }

        [RequiredByNativeCode]
        internal static bool EndContainerGUIFromException(Exception exception)
        {
            if (endContainerGUIFromException != null)
                return endContainerGUIFromException(exception);
            return false;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void ResetGlobalState()
        {
            GUI.skin = null;
            guiIsExiting = false;
            GUI.changed = false;
            GUI.scrollViewStates.Clear();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool IsExitGUIException(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null)
                exception = exception.InnerException;

            return exception is ExitGUIException;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool ShouldRethrowException(Exception exception)
        {
            return IsExitGUIException(exception);
        }

        [AutoStaticsCleanupOnCodeReload]
        private static Lazy<bool> s_DetectIMGUICallsInUITK = new Lazy<bool>(() => (bool)Debug.GetDiagnosticSwitch("DetectIMGUICallsInUITK").value);
        internal static bool DetectIMGUICallsInUITK => s_DetectIMGUICallsInUITK.Value;
        [AutoStaticsCleanupOnCodeReload]
        private static Lazy<bool> s_DetectInvalidImguiAPIUsages = new Lazy<bool>(() => (bool)Debug.GetDiagnosticSwitch("DetectInvalidImguiAPIUsages").value);
        internal static bool DetectInvalidImguiAPIUsages => s_DetectInvalidImguiAPIUsages.Value;

        /// <summary>
        /// This boolean is meant to indicate that we are in a section of UITK's code where using IMGUI api that rely on a global state would be suspicious.
        /// For example, calling GUIUtility.PixelPerPoint in a geometryChange event would probably work in the editor but not at runtime.
        /// The boolean is not necessarily set to true when we are in UITK code. At the time of writing this, the scheduler, bindings, inpsector throtling are not covered.
        /// It is set to true during GeometryChangedEvent, for Event Dispatching and during Repaints.
        /// As this is used along DetectInvalidImguiAPIUsages above, new invalid calls will be detected over time as the scope expands.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NoAutoStaticsCleanup] // UITK context guard flag; false (default) is the safe post-reload state — no IMGUI call will be spuriously suppressed
        internal static bool isUITK { get; set; } = false;

        // Only allow calling GUI functions from inside OnGUI
        internal static void CheckOnGUI()
        {
            if ( guiDepth <= 0)
                throw new ArgumentException("You can only call GUI functions from inside OnGUI.");

            if (DetectIMGUICallsInUITK && isUITK)
                Debug.LogWarning("IMGUI method called from insde UI Toolkit.\n " +
                    "You can only call IMGUI method inside an OnGUI method or inside an IMGUIContainer.\n" +
                    "While IMGUI and UI Toolkit are tightly coupled in the editor right now, it may change in future versions of Unity.");
        }

        // Only allow calling GUI functions from inside OnGUI
        internal static void WarnOnGUI()
        {
            if (DetectInvalidImguiAPIUsages && guiDepth <= 0)
            {
                Debug.LogWarning("You can only call IMGUI method inside an OnGUI method or inside an IMGUIContainer.");
                return;
            }

            if ( DetectIMGUICallsInUITK && isUITK )
                Debug.LogWarning("IMGUI method called from insde UI Toolkit.\n " +
                    "You can only call IMGUI method inside an OnGUI method or inside an IMGUIContainer.\n" +
                    "While IMGUI and UI Toolkit are tightly coupled in the editor right now, it may change in future versions of Unity.");
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static float RoundToPixelGrid(float v)
        {
            WarnOnGUI();
            // Using same rounding constant as GUITexture::AlignPointToDevice
            const float kNearestRoundingOffset = 0.48f;
            return Mathf.Floor((v * GUIUtility.pixelsPerPoint) + kNearestRoundingOffset) / GUIUtility.pixelsPerPoint;
        }

        internal static float RoundToPixelGrid(float v, float scale)
        {
            // Using same rounding constant as GUITexture::AlignPointToDevice
            const float kNearestRoundingOffset = 0.48f;
            return Mathf.Floor((v * scale) + kNearestRoundingOffset) / scale;
        }

        ///<summary>Convert a point from GUI position to screen space.</summary>
        ///<remarks>**Note:** In Unity the screen space **y** coordinate varies from zero at the top
        ///edge of the window to a maximum at the bottom edge of the window.  This is
        ///different from what you might expect.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Converts a GUICoordinate (affected by a group) to a Screen coordinate.
        ///
        ///    void OnGUI()
        ///    {
        ///        Vector2 gPos = new Vector2(10, 10);
        ///        GUI.BeginGroup(new Rect(10, 10, 100, 100));
        ///        Vector2 convertedGUIPos = GUIUtility.GUIToScreenPoint(gPos);
        ///        GUI.EndGroup();
        ///        Debug.Log("GUI: " + gPos + " Screen: " + convertedGUIPos);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUIUtility.ScreenToGUIPoint" />
        public static Vector2 GUIToScreenPoint(Vector2 guiPoint)
        {
            WarnOnGUI();
            return InternalWindowToScreenPoint(GUIClip.UnclipToWindow(guiPoint));
        }

        ///<summary>Convert a rect from GUI position to screen space.</summary>
        public static Rect GUIToScreenRect(Rect guiRect)
        {
            WarnOnGUI();
            Vector2 screenPoint = GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            guiRect.x = screenPoint.x;
            guiRect.y = screenPoint.y;
            return guiRect;
        }

        ///<summary>Convert a point from screen space to GUI position.</summary>
        ///<remarks>Used for reconverting values calculated from <see cref="GUIToScreenPoint" />
        ///
        ///**Note:** In Unity the screen space **y** coordinate varies from zero at the top
        ///edge of the window to a maximum at the bottom edge of the window. This is
        ///different from what you might expect.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Check the difference between the mouse position (Screen) and
        ///    // the converted GUI positions because of the group.
        ///
        ///    void OnGUI()
        ///    {
        ///        Vector2 screenPos = Event.current.mousePosition;
        ///        GUI.BeginGroup(new Rect(10, 10, 100, 100));
        ///        Vector2 convertedGUIPos = GUIUtility.ScreenToGUIPoint(screenPos);
        ///        GUI.EndGroup();
        ///        Debug.Log("Screen: " + screenPos + " GUI: " + convertedGUIPos);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUIUtility.GUIToScreenPoint" />
        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
            WarnOnGUI();
            return GUIClip.ClipToWindow(InternalScreenToWindowPoint(screenPoint));
        }

        ///<summary>Convert a rect from screen space to GUI position.</summary>
        public static Rect ScreenToGUIRect(Rect screenRect)
        {
            WarnOnGUI();
            Vector2 guiPoint = ScreenToGUIPoint(new Vector2(screenRect.x, screenRect.y));
            screenRect.x = guiPoint.x;
            screenRect.y = guiPoint.y;
            return screenRect;
        }

        ///<summary>Helper function to rotate the GUI around a point.</summary>
        ///<remarks>Modifies <see cref="GUI.matrix" /> to rotate all GUI elements <c>angle</c> degrees around <c>pivotPoint</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Rotate a button 10 degrees clockwise when pressed.
        ///
        ///    float rotAngle = 0;
        ///    Vector2 pivotPoint;
        ///
        ///    void OnGUI()
        ///    {
        ///        pivotPoint = new Vector2(Screen.width / 2, Screen.height / 2);
        ///        GUIUtility.RotateAroundPivot(rotAngle, pivotPoint);
        ///        if (GUI.Button(new Rect(Screen.width / 2 - 25, Screen.height / 2 - 25, 50, 50), "Rotate"))
        ///        {
        ///            rotAngle += 10;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.matrix" />
        ///<seealso cref="ScaleAroundPivot" />
        public static void RotateAroundPivot(float angle, Vector2 pivotPoint)
        {
            WarnOnGUI();
            Matrix4x4 mat = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector2 point = GUIClip.Unclip(pivotPoint);
            Matrix4x4 newMat =  Matrix4x4.TRS(point, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.TRS(-point, Quaternion.identity, Vector3.one);
            GUI.matrix = newMat * mat;
        }

        ///<summary>Helper function to scale the GUI around a point.</summary>
        ///<remarks>Modifies <see cref="GUI.matrix" /> to scale all GUI elements around a <c>pivotPoint</c>.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Scale a button by 1.5 times each time is pressed.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private Vector2 scale = new Vector2(1, 1);
        ///    private Vector2 pivotPoint;
        ///
        ///    void OnGUI()
        ///    {
        ///        pivotPoint = new Vector2(Screen.width / 2, Screen.height / 2);
        ///        GUIUtility.ScaleAroundPivot(scale, pivotPoint);
        ///
        ///        if (GUI.Button(new Rect(Screen.width / 2 - 25, Screen.height / 2 - 25, 50, 50), "Big!"))
        ///        {
        ///            scale += new Vector2(0.5F, 0.5F);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.matrix" />
        ///<seealso cref="RotateAroundPivot" />
        public static void ScaleAroundPivot(Vector2 scale, Vector2 pivotPoint)
        {
            WarnOnGUI();
            Matrix4x4 mat = GUI.matrix;
            Vector2 point = GUIClip.Unclip(pivotPoint);
            Matrix4x4 newMat =  Matrix4x4.TRS(point, Quaternion.identity, new Vector3(scale.x, scale.y, 1)) * Matrix4x4.TRS(-point, Quaternion.identity, Vector3.one);
            GUI.matrix = newMat * mat;
        }

        ///<summary>Align a local space rectangle to the pixel grid.</summary>
        ///<remarks>Aligns the top-left and bottom-right corners of the provided local space rectangle to the pixel grid and returns the local space axis-aligned bounding box that encompasses those points.</remarks>
        ///<returns>The aligned rectangle in local space.</returns>
        public static Rect AlignRectToDevice(Rect rect)
        {
            WarnOnGUI();
            int width, height;
            return AlignRectToDevice(rect, out width, out height);
        }

        internal static bool HitTest(Rect rect, Vector2 point, int offset)
        {
            return (point.x >= rect.xMin - offset) && (point.x < rect.xMax + offset) && (point.y >= rect.yMin - offset) && (point.y < rect.yMax + offset);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool HitTest(Rect rect, Vector2 point, bool isDirectManipulationDevice)
        {
            // Increased picking zone for touch is reverted following this bug 1272071, it seems too impactful for pen user at the moment, need to add this when being able to differentiate between Pen and Finger
            int offset = 0; // isDirectManipulationDevice ? 3 : 0;
            return HitTest(rect, point, offset);
        }

        internal static bool HitTest(Rect rect, Event evt)
        {
            return HitTest(rect, evt.mousePosition, evt.isDirectManipulationDevice);
        }
    }


    internal sealed partial class GUIClip
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal struct ParentClipScope : IDisposable
        {
            private bool m_Disposed;

            public ParentClipScope(Matrix4x4 objectTransform, Rect clipRect)
            {
                m_Disposed = false;
                Internal_PushParentClip(objectTransform, clipRect);
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                Internal_PopParentClip();
            }
        }

        // Push a clip rect to the stack with pixel offsets.
        internal static void Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
        {
            Internal_Push(screenRect, scrollOffset, renderOffset, resetOffset);
        }

        // Removes the topmost clipping rectangle, undoing the effect of the latest GUIClip.Push
        internal static void Pop()
        {
            Internal_Pop();
        }

        // Unclips /pos/ to IMGUI container coordinates.
        public static Vector2 Unclip(Vector2 pos)
        {
            return Unclip_Vector2(pos);
        }

        // Unclips /rect/ to IMGUI container coordinates.
        public static Rect Unclip(Rect rect)
        {
            return Unclip_Rect(rect);
        }

        // Clips /absolutePos/ to IMGUI container coordinates
        public static Vector2 Clip(Vector2 absolutePos)
        {
            return Clip_Vector2(absolutePos);
        }

        // Convert /absoluteRect/ to IMGUI container coordinates
        public static Rect Clip(Rect absoluteRect)
        {
            return Internal_Clip_Rect(absoluteRect);
        }

        // Unclips /pos/ to window coordinator.
        public static Vector2 UnclipToWindow(Vector2 pos)
        {
            return UnclipToWindow_Vector2(pos);
        }

        // Unclips /rect/ to window coordinates.
        public static Rect UnclipToWindow(Rect rect)
        {
            return UnclipToWindow_Rect(rect);
        }

        // Clips /absolutePos/ to window coordinates
        public static Vector2 ClipToWindow(Vector2 absolutePos)
        {
            return ClipToWindow_Vector2(absolutePos);
        }

        // Convert /absoluteRect/ to window coordinates
        public static Rect ClipToWindow(Rect absoluteRect)
        {
            return ClipToWindow_Rect(absoluteRect);
        }

        public static Vector2 GetAbsoluteMousePosition()
        {
            return Internal_GetAbsoluteMousePosition();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
