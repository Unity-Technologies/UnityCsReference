// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Scripting;
// Use this define to debug who grabs and releases hotcontrol
//#define DEBUG_HOTCONTROL

// Use this define to debug controlID consistency together with 's_LogControlID' (default false) to enable logging in
// a codepath thats needs tested for consistency. E.g:
//  if (Event.current.rawType == EventType.MouseUp)
//      GUIUtility.s_LogControlID = true;
// And remember to set s_LogControlID to false at end of section of interest.
//#define DEBUG_CONTROLID

namespace UnityEngine
{
    // Throw this to immediately exit from GUI code.
    // *undocumented*
    public sealed class ExitGUIException : Exception
    {
    }

    // Utility class for making new GUI controls.
    public partial class GUIUtility
    {
        internal static int s_SkinMode;
        internal static int s_OriginalID;

        // IoC callbacks for UIElements
        internal static Action takeCapture;
        internal static Action releaseCapture;
        internal static Func<int, IntPtr, bool> processEvent;
        internal static Action cleanupRoots;
        internal static Func<Exception, bool> endContainerGUIFromException;

        /// The current UI scaling factor for high-DPI displays. For instance, 2.0 on a retina display
        internal static float pixelsPerPoint
        {
            get { return Internal_GetPixelsPerPoint(); }
        }

        /// *listonly*
        public static int GetControlID(FocusType focus)
        {
            return GetControlID(0, focus);
        }

        /// *listonly*
        public static int GetControlID(GUIContent contents, FocusType focus)
        {
            return GetControlID(contents.hash, focus);
        }

        /// *listonly*
        public static int GetControlID(FocusType focus, Rect position)
        {
            return Internal_GetNextControlID2(0, focus, position);
        }

        /// *listonly*
        public static int GetControlID(int hint, FocusType focus, Rect position)
        {
            return Internal_GetNextControlID2(hint, focus, position);
        }

        // Get a unique ID for a control.
        public static int GetControlID(GUIContent contents, FocusType focus, Rect position)
        {
            return Internal_GetNextControlID2(contents.hash, focus, position);
        }

        // Get a state object from a controlID.
        public static object GetStateObject(Type t, int controlID)     { return GUIStateObjects.GetStateObject(t, controlID); }

        // Get an existing state object from a controlID.
        public static object QueryStateObject(Type t, int controlID)       { return GUIStateObjects.QueryStateObject(t, controlID); }

        internal static bool guiIsExiting { get; set; }



        // The controlID of the current hot control.
        public static int hotControl
        {
            get { return Internal_GetHotControl(); }
            set
            {
                Internal_SetHotControl(value);
            }
        }

        [RequiredByNativeCode]
        internal static void TakeCapture()
        {
            if (takeCapture != null)
                takeCapture();
        }

        [RequiredByNativeCode]
        internal static void RemoveCapture()
        {
            if (releaseCapture != null)
                releaseCapture();
        }

        // The controlID of the control that has keyboard focus.
        public static int keyboardControl
        {
            get { return Internal_GetKeyboardControl(); }
            set
            {
                Internal_SetKeyboardControl(value);
            }
        }

        //*undocumented*
        public static void ExitGUI()
        {
            // Hint for scope helpers
            guiIsExiting = true;

            // We have to always throw the ExitGUIException otherwise the exiting out of recursive on GUI will not work.
            throw new ExitGUIException();
        }

        internal static GUISkin GetDefaultSkin(int skinMode)
        {
            return Internal_GetDefaultSkin(skinMode);
        }

        internal static GUISkin GetDefaultSkin()
        {
            return Internal_GetDefaultSkin(s_SkinMode);
        }

        // internal so we can get to it from EditorGUIUtility.GetBuiltinSkin
        internal static GUISkin GetBuiltinSkin(int skin)
        {
            return Internal_GetBuiltinSkin(skin) as GUISkin;
        }

        [RequiredByNativeCode]
        internal static bool ProcessEvent(int instanceID, IntPtr nativeEventPtr)
        {
            if (processEvent != null)
                return processEvent(instanceID, nativeEventPtr);
            return false;
        }

        internal static void EndContainer()
        {
            Internal_EndContainer();
            Internal_ExitGUI();
        }

        internal static void CleanupRoots()
        {
            if (cleanupRoots != null)
                cleanupRoots();
        }

        [RequiredByNativeCode]
        internal static void BeginGUI(int skinMode, int instanceID, int useGUILayout)
        {
            s_SkinMode = skinMode;
            s_OriginalID = instanceID;

            ResetGlobalState();

            // Switch to the correct ID list & clear keyboard loop if we're about to layout (we rebuild it during layout, so we want it cleared beforehand)
            if (useGUILayout != 0)
            {
                GUILayoutUtility.Begin(instanceID);
            }
        }

        [RequiredByNativeCode]
        internal static void SetSkin(int skinMode)
        {
            s_SkinMode = skinMode;
            GUI.DoSetSkin(null);
        }


        [RequiredByNativeCode]
        internal static void EndGUI(int layoutType)
        {
            try
            {
                if (Event.current.type == EventType.Layout)
                {
                    switch (layoutType)
                    {
                        case 0: // kNoLayout
                            break;
                        case 1: // kGameLayout
                            GUILayoutUtility.Layout();
                            break;
                        case 2: // kEditorLayout
                            GUILayoutUtility.LayoutFromEditorWindow();
                            break;
                    }
                }
                GUILayoutUtility.SelectIDList(s_OriginalID, false);
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

        internal static void ResetGlobalState()
        {
            GUI.skin = null;
            guiIsExiting = false;
            GUI.changed = false;
        }

        internal static bool IsExitGUIException(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null)
                exception = exception.InnerException;

            return exception is ExitGUIException;
        }

        internal static bool ShouldRethrowException(Exception exception)
        {
            return IsExitGUIException(exception);
        }

        // Only allow calling GUI functions from inside OnGUI
        internal static void CheckOnGUI()
        {
            if (Internal_GetGUIDepth() <= 0)
                throw new ArgumentException("You can only call GUI functions from inside OnGUI.");
        }

        internal static Vector2 s_EditorScreenPointOffset = Vector2.zero;

        // Convert a point from GUI position to screen space.
        public static Vector2 GUIToScreenPoint(Vector2 guiPoint)
        {
            return GUIClip.UnclipToWindow(guiPoint) + s_EditorScreenPointOffset;
        }

        // Convert a rect from GUI position to screen space.
        internal static Rect GUIToScreenRect(Rect guiRect)
        {
            Vector2 screenPoint = GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            guiRect.x = screenPoint.x;
            guiRect.y = screenPoint.y;
            return guiRect;
        }

        // Convert a point from screen space to GUI position.
        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
            return GUIClip.ClipToWindow(screenPoint) - s_EditorScreenPointOffset;
        }

        // Convert a rect from screen space to GUI position.
        public static Rect ScreenToGUIRect(Rect screenRect)
        {
            Vector2 guiPoint = ScreenToGUIPoint(new Vector2(screenRect.x, screenRect.y));
            screenRect.x = guiPoint.x;
            screenRect.y = guiPoint.y;
            return screenRect;
        }

        // Helper function to rotate the GUI around a point.
        public static void RotateAroundPivot(float angle, Vector2 pivotPoint)
        {
            Matrix4x4 mat = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector2 point = GUIClip.Unclip(pivotPoint);
            Matrix4x4 newMat =  Matrix4x4.TRS(point, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.TRS(-point, Quaternion.identity, Vector3.one);
            GUI.matrix = newMat * mat;
        }

        // Helper function to scale the GUI around a point.
        public static void ScaleAroundPivot(Vector2 scale, Vector2 pivotPoint)
        {
            Matrix4x4 mat = GUI.matrix;
            Vector2 point = GUIClip.Unclip(pivotPoint);
            Matrix4x4 newMat =  Matrix4x4.TRS(point, Quaternion.identity, new Vector3(scale.x, scale.y, 1)) * Matrix4x4.TRS(-point, Quaternion.identity, Vector3.one);
            GUI.matrix = newMat * mat;
        }
    }

    internal sealed partial class GUIClip
    {
        // Push a clip rect to the stack with pixel offsets.
        internal static void Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
        {
            GUIClip.Internal_Push(screenRect, scrollOffset, renderOffset, resetOffset);
        }

        // Removes the topmost clipping rectangle, undoing the effect of the latest GUIClip.Push
        internal static void Pop()
        {
            Internal_Pop();
        }

        // Unclips /pos/ to IMGUI container coordinates.
        public static Vector2 Unclip(Vector2 pos)
        {
            Unclip_Vector2(ref pos);
            return pos;
        }

        // Unclips /rect/ to IMGUI container coordinates.
        public static Rect Unclip(Rect rect)
        {
            Unclip_Rect(ref rect);
            return rect;
        }

        // Clips /absolutePos/ to IMGUI container coordinates
        public static Vector2 Clip(Vector2 absolutePos)
        {
            Clip_Vector2(ref absolutePos);
            return absolutePos;
        }

        // Convert /absoluteRect/ to IMGUI container coordinates
        public static Rect Clip(Rect absoluteRect)
        {
            Internal_Clip_Rect(ref absoluteRect);
            return absoluteRect;
        }

        // Unclips /pos/ to window coordinater.
        public static Vector2 UnclipToWindow(Vector2 pos)
        {
            UnclipToWindow_Vector2(ref pos);
            return pos;
        }

        // Unclips /rect/ to window coordinates.
        public static Rect UnclipToWindow(Rect rect)
        {
            UnclipToWindow_Rect(ref rect);
            return rect;
        }

        // Clips /absolutePos/ to window coordinates
        public static Vector2 ClipToWindow(Vector2 absolutePos)
        {
            ClipToWindow_Vector2(ref absolutePos);
            return absolutePos;
        }

        // Convert /absoluteRect/ to window coordinates
        public static Rect ClipToWindow(Rect absoluteRect)
        {
            ClipToWindow_Rect(ref absoluteRect);
            return absoluteRect;
        }

        public static Vector2 GetAbsoluteMousePosition()
        {
            Vector2 vec;
            Internal_GetAbsoluteMousePosition(out vec);
            return vec;
        }
    }
}
