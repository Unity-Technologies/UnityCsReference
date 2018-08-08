// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Custom mouse cursor shapes used with EditorGUIUtility.AddCursorRect.
    // Must Match EditorWindow::MouseCursor
    public enum MouseCursor
    {
        // Normal pointer arrow
        Arrow = 0,
        // Text cursor
        Text = 1,
        // Vertical resize arrows
        ResizeVertical = 2,
        // Horizontal resize arrows
        ResizeHorizontal = 3,
        // Arrow with a Link badge (for assigning pointers)
        Link = 4,
        // Arrow with small arrows for indicating sliding at number fields
        SlideArrow = 5,
        // Resize up-right for window edges
        ResizeUpRight = 6,
        // Resize up-Left for window edges.
        ResizeUpLeft = 7,
        // Arrow with the move symbol next to it for the sceneview
        MoveArrow = 8,
        // Arrow with the rotate symbol next to it for the sceneview
        RotateArrow = 9,
        // Arrow with the scale symbol next to it for the sceneview
        ScaleArrow = 10,
        // Arrow with the plus symbol next to it
        ArrowPlus = 11,
        // Arrow with the minus symbol next to it
        ArrowMinus = 12,
        // Cursor with a dragging hand for pan
        Pan = 13,
        // Cursor with an eye for orbit
        Orbit = 14,
        // Cursor with a magnifying glass for zoom
        Zoom = 15,
        // Cursor with an eye and stylized arrow keys for FPS navigation
        FPS = 16,
        // The current user defined cursor
        CustomCursor = 17,
        // Split resize up down arrows
        SplitResizeUpDown = 18,
        // Split resize left right arrows
        SplitResizeLeftRight = 19
    }

    // User message types.
    public enum MessageType
    {
        // Neutral message
        None = 0,
        // Info message
        Info = 1,
        // Warning message
        Warning = 2,
        // Error message
        Error = 3
    }

    // Enum that selects which skin to return from EditorGUIUtility.GetBuiltinSkin
    public enum EditorSkin
    {
        // The skin used for game views.
        Game = 0,
        // The skin used for inspectors.
        Inspector = 1,
        // The skin used for scene views.
        Scene = 2
    }

    [NativeHeader("Editor/Src/EditorResources.h"),
     NativeHeader("Runtime/Graphics/Texture2D.h"),
     NativeHeader("Runtime/Graphics/RenderTexture.h"),
     NativeHeader("Modules/TextRendering/Public/Font.h"),
     NativeHeader("Editor/Src/Utility/EditorGUIUtility.h")]
    public partial class EditorGUIUtility
    {
        public static extern string SerializeMainMenuToString();
        public static extern void SetMenuLocalizationTestMode(bool onoff);

        // Set icons rendered as part of [[GUIContent]] to be rendered at a specific size.
        public static extern void SetIconSize(Vector2 size);

        // Get a white texture.
        public static extern Texture2D whiteTexture {[NativeMethod("GetWhiteTexture")] get; }

        // The system copy buffer.
        public new static extern string systemCopyBuffer
        {
            [NativeMethod("GetSystemCopyBuffer")] get;
            [NativeMethod("SetSystemCopyBuffer")] set;
        }

        internal static extern int skinIndex
        {
            [FreeFunction("GetEditorResources().GetSkinIdx")] get;
            [FreeFunction("GetEditorResources().SetSkinIdx")] set;
        }

        public static extern void SetWantsMouseJumping(int wantz);

        // Iterates over cameras and counts the ones that would render to a specified screen (doesn't involve culling)
        public static extern bool IsDisplayReferencedByCameras(int displayIndex);

        // Send an input event into the game.
        public static extern void QueueGameViewInputEvent(Event evt);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RenderGameViewCameras is no longer supported.Consider rendering cameras manually.", true)]
        public static void RenderGameViewCameras(RenderTexture target, int targetDisplay, Rect screenRect, Vector2 mousePosition, bool gizmos) {}

        internal static extern Object GetScript(string scriptClass);
        internal static extern void SetIconForObject(Object obj, Texture2D icon);
        internal static extern Object GetBuiltinExtraResource(Type type, string path);
        internal static extern BuiltinResource[] GetBuiltinResourceList(int classID);
        internal static extern AssetBundle GetEditorAssetBundle();
        internal static extern void SetRenderTextureNoViewport(RenderTexture rt);
        internal static extern void SetVisibleLayers(int layers);
        internal static extern void SetLockedLayers(int layers);
        internal static extern bool IsGizmosAllowedForObject(Object obj);
        internal static extern void SetPasteboardColor(Color color);
        internal static extern bool HasPasteboardColor();
        internal static extern Color GetPasteboardColor();
        internal static extern void SetCurrentViewCursor(Texture2D texture, Vector2 hotspot, MouseCursor type);
        internal static extern void ClearCurrentViewCursor();
        internal static extern void CleanCache(string text);
        internal static extern void SetSearchIndexOfControlIDList(int index);
        internal static extern int GetSearchIndexOfControlIDList();
        internal static extern bool CanHaveKeyboardFocus(int id);

        // Duplicate of SetDefaultFont in UnityEngine. We need to call it from editor code as well,
        // while keeping both internal.
        internal static extern void SetDefaultFont(Font font);

        // Remember to call CopyMonoScriptIconToImporters when data should be copied to mono importer and mono assembly importer.
        internal static extern Texture2D GetIconForObject(Object obj);

        // Render all ingame cameras bound to a specific Display.
        internal static extern void RenderGameViewCamerasInternal(RenderTexture target, int targetDisplay, Rect screenRect, Vector2 mousePosition, bool gizmos, bool sendInput);

        private static extern Texture2D FindTextureByName(string name);
        private static extern Texture2D FindTextureByType([NotNull] Type type);
        private static extern string GetObjectNameWithInfo(Object obj);
        private static extern string GetTypeNameWithInfo(string typeName);
        private static extern void Internal_SetupEventValues(object evt);
        private static extern Vector2 Internal_GetIconSize();
        private static extern bool Internal_GetKeyboardRect(int id, out Rect rect);
        private static extern void Internal_MoveKeyboardFocus(bool forward);
        private static extern int Internal_GetNextKeyboardControlID(bool forward);
        private static extern void Internal_AddCursorRect(Rect r, MouseCursor m, int controlID);
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Editor/Src/OptimizedGUIBlock.h")]
    internal class OptimizedGUIBlock
    {
        [NonSerialized]
        private IntPtr m_Ptr;

        // Are we currently recording commands?
        bool m_Recording;

        // if we're inside an OptimizedBlock and code in there uses an event, we invalidate the block to handle e.g. a mousedown changing a button
        bool m_WatchForUsed;

        // The keyboard control we were generated with
        int m_KeyboardControl;

        // The last search index when ended recording the block, so we can restore that index after executing the block
        int m_LastSearchIndex;

        int m_ActiveDragControl;

        Color m_GUIColor;

        // Rect we're sorta inside. Used so we invalidate when scrolling and getting moved about (needed for clipping - DOH!)
        Rect m_Rect;

        // Do we have a valid capture of Draw commands?
        public bool valid { get; set; }

        public OptimizedGUIBlock()
        {
            m_Ptr = Internal_Create();
        }

        ~OptimizedGUIBlock()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public bool Begin(bool hasChanged, Rect position)
        {
            if (hasChanged)
                valid = false;

            if (Event.current.type == EventType.Repaint)
            {
                if (GUIUtility.keyboardControl != m_KeyboardControl)
                {
                    valid = false;
                    m_KeyboardControl = GUIUtility.keyboardControl;
                }

                if (DragAndDrop.activeControlID != m_ActiveDragControl)
                {
                    valid = false;
                    m_ActiveDragControl = DragAndDrop.activeControlID;
                }

                if (GUI.color != m_GUIColor)
                {
                    valid = false;
                    m_GUIColor = GUI.color;
                }

                position = GUIClip.Unclip(position);
                if (valid && position != m_Rect)
                {
                    m_Rect = position;
                    valid = false;
                }

                if (EditorGUI.isCollectingTooltips)
                    return true;

                if (valid)
                    return false;

                m_Recording = true;
                BeginRecording();
                return true;
            }
            if (Event.current.type == EventType.Used)
                return false;

            // If this event has not been used yet, we want to check if it gets used inside this optimized block
            // (if the event gets used, we assume that the block is no longer valid.
            if (Event.current.type != EventType.Used)
                m_WatchForUsed = true;

            return true;
        }

        public void End()
        {
            bool wasRecording = m_Recording;
            if (m_Recording)
            {
                EndRecording();
                m_Recording = false;
                valid = true;
                m_LastSearchIndex = EditorGUIUtility.GetSearchIndexOfControlIDList();
            }

            if (Event.current == null)
            {
                Debug.LogError("Event.current is null");
                return;
            }

            if (Event.current.type == EventType.Repaint && !EditorGUI.isCollectingTooltips)
            {
                Execute();
                if (!wasRecording)
                    EditorGUIUtility.SetSearchIndexOfControlIDList(m_LastSearchIndex);
            }

            if (m_WatchForUsed && Event.current.type == EventType.Used)
            {
                valid = false;
            }
            m_WatchForUsed = false;
        }

        private void BeginRecording() { Internal_BeginRecording(this); }
        private void EndRecording() { Internal_EndRecording(this); }
        private void Execute() { Internal_Execute(this); }

        internal static extern void Internal_Destroy(IntPtr ptr);
        internal static extern IntPtr Internal_Create();
        internal static extern void Internal_BeginRecording(OptimizedGUIBlock self);
        internal static extern void Internal_EndRecording(OptimizedGUIBlock self);
        internal static extern void Internal_Execute(OptimizedGUIBlock self);
    }

    [NativeHeader("Editor/Src/InspectorExpandedState.h"),
     StaticAccessor("GetInspectorExpandedState().GetSessionState()", StaticAccessorType.Dot)]
    public class SessionState
    {
        [ExcludeFromDocs] public SessionState() {}

        public static extern void SetBool(string key, bool value);
        public static extern bool GetBool(string key, bool defaultValue);
        public static extern void EraseBool(string key);
        public static extern void SetFloat(string key, float value);
        public static extern float GetFloat(string key, float defaultValue);
        public static extern void EraseFloat(string key);
        public static extern void SetInt(string key, int value);
        public static extern int GetInt(string key, int defaultValue);
        public static extern void EraseInt(string key);
        public static extern void SetString(string key, string value);
        public static extern string GetString(string key, string defaultValue);
        public static extern void EraseString(string key);
        public static extern void SetVector3(string key, Vector3 value);
        public static extern Vector3 GetVector3(string key, Vector3 defaultValue);
        public static extern void EraseVector3(string key);
        public static extern void EraseIntArray(string key);
        public static extern void SetIntArray(string key, int[] value);
        public static extern int[] GetIntArray(string key, int[] defaultValue);
    }
}
