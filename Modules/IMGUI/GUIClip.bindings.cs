// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUIClip.h"),
     NativeHeader("Modules/IMGUI/GUIState.h")]
    internal partial class GUIClip
    {
        internal static extern bool enabled {[FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetEnabled")] get; }

        // The visible rectangle.
        internal static extern Rect visibleRect {[FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetVisibleRect")] get; }

        // The topmost physical rect in unclipped coordinates
        // Used in editor to clip cursor rects inside scroll views
        internal static extern Rect topmostRect
        {
            [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetTopMostPhysicalRect")]
            get;
        }

        // Push a clip rect to the stack with pixel offsets.
        internal static extern void Internal_Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset);

        // Removes the topmost clipping rectangle, undoing the effect of the latest GUIClip.Push
        internal static extern void Internal_Pop();

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetCount")]
        internal static extern int Internal_GetCount();

        // Get the topmost rectangle
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetTopRect")]
        internal static extern Rect GetTopRect();

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Unclip")]
        private static extern Vector2 Unclip_Vector2(Vector2 pos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Unclip")]
        private static extern Rect Unclip_Rect(Rect rect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Clip")]
        private static extern Vector2 Clip_Vector2(Vector2 absolutePos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.Clip")]
        private static extern Rect Internal_Clip_Rect(Rect absoluteRect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.UnclipToWindow")]
        private static extern Vector2 UnclipToWindow_Vector2(Vector2 pos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.UnclipToWindow")]
        private static extern Rect UnclipToWindow_Rect(Rect rect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.ClipToWindow")]
        private static extern Vector2 ClipToWindow_Vector2(Vector2 absolutePos);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.ClipToWindow")]
        private static extern Rect ClipToWindow_Rect(Rect absoluteRect);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetAbsoluteMousePosition")]
        private static extern Vector2 Internal_GetAbsoluteMousePosition();

        // Reapply the clipping info.
        internal static extern void Reapply();

        // Set the GUIMatrix. This is here as this class handles all coordinate transforms anyways.
        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetUserMatrix")]
        internal static extern Matrix4x4 GetMatrix();

        internal static extern void SetMatrix(Matrix4x4 m);

        [FreeFunction("GetGUIState().m_CanvasGUIState.m_GUIClipState.GetParentTransform")]
        internal static extern Matrix4x4 GetParentMatrix();

        internal static extern void Internal_PushParentClip(Matrix4x4 objectTransform, Rect clipRect);

        internal static extern void Internal_PopParentClip();
    }
}
