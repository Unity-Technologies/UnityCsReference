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
        public ExitGUIException()
        {
            GUIUtility.guiIsExiting = true;
        }

        internal ExitGUIException(string message)
            : base(message)
        {
            GUIUtility.guiIsExiting = true;
            Console.WriteLine(message);
        }
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
        internal static Action guiChanged;

        [RequiredByNativeCode]
        private static void MarkGUIChanged()
        {
            guiChanged?.Invoke();
        }

        public static int GetControlID(FocusType focus)
        {
            return GetControlID(0, focus);
        }

        public static int GetControlID(GUIContent contents, FocusType focus)
        {
            return GetControlID(contents.hash, focus);
        }

        public static int GetControlID(FocusType focus, Rect position)
        {
            return GetControlID(0, focus, position);
        }

        // Get a unique ID for a control.
        public static int GetControlID(GUIContent contents, FocusType focus, Rect position)
        {
            return GetControlID(contents.hash, focus, position);
        }

        public static int GetControlID(int hint, FocusType focus)
        {
            return GetControlID(hint, focus, Rect.zero);
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
            takeCapture?.Invoke();
        }

        [RequiredByNativeCode]
        internal static void RemoveCapture()
        {
            releaseCapture?.Invoke();
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

        internal static Func<bool> s_HasCurrentWindowKeyFocusFunc;

        internal static bool HasKeyFocus(int controlID)
        {
            return controlID == GUIUtility.keyboardControl &&
                (s_HasCurrentWindowKeyFocusFunc != null ? s_HasCurrentWindowKeyFocusFunc() : true);
        }

        //*undocumented*
        public static void ExitGUI()
        {
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

        [RequiredByNativeCode]
        internal static void ProcessEvent(int instanceID, IntPtr nativeEventPtr, out bool result)
        {
            result = false;
            if (processEvent != null)
                result = processEvent(instanceID, nativeEventPtr);
        }

        internal static void EndContainer()
        {
            Internal_EndContainer();
            Internal_ExitGUI();
        }

        internal static void CleanupRoots()
        {
            cleanupRoots?.Invoke();
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
            GUI.scrollViewStates.Clear();
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
            if (guiDepth <= 0)
                throw new ArgumentException("You can only call GUI functions from inside OnGUI.");
        }

        internal static float RoundToPixelGrid(float v)
        {
            // Using same rounding constant as GUITexture::AlignPointToDevice
            const float kNearestRoundingOffset = 0.48f;
            return Mathf.Floor((v * GUIUtility.pixelsPerPoint) + kNearestRoundingOffset) / GUIUtility.pixelsPerPoint;
        }

        // Convert a point from GUI position to screen space.
        public static Vector2 GUIToScreenPoint(Vector2 guiPoint)
        {
            return InternalWindowToScreenPoint(GUIClip.UnclipToWindow(guiPoint));
        }

        // Convert a rect from GUI position to screen space.
        public static Rect GUIToScreenRect(Rect guiRect)
        {
            Vector2 screenPoint = GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            guiRect.x = screenPoint.x;
            guiRect.y = screenPoint.y;
            return guiRect;
        }

        // Convert a point from screen space to GUI position.
        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
            return GUIClip.ClipToWindow(InternalScreenToWindowPoint(screenPoint));
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

        public static Rect AlignRectToDevice(Rect rect)
        {
            int width, height;
            return AlignRectToDevice(rect, out width, out height);
        }

        internal static bool HitTest(Rect rect, Vector2 point, int offset)
        {
            return (point.x >= rect.xMin - offset) && (point.x < rect.xMax + offset) && (point.y >= rect.yMin - offset) && (point.y < rect.yMax + offset);
        }

        internal static bool HitTest(Rect rect, Vector2 point, bool isDirectManipulationDevice)
        {
            int offset = isDirectManipulationDevice ? 3 : 0;
            return HitTest(rect, point, offset);
        }

        internal static bool HitTest(Rect rect, Event evt)
        {
            return HitTest(rect, evt.mousePosition, evt.isDirectManipulationDevice);
        }
    }


    internal sealed partial class GUIClip
    {
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
