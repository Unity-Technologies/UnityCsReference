// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditorInternal
{
    internal class FrameDebuggerHelper
    {
        internal static bool IsOnLinuxOpenGL => Application.platform == RuntimePlatform.LinuxEditor && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        internal static bool IsAValidFrame(int curEventIndex, int descsLength) => (curEventIndex >= 0 && curEventIndex < descsLength);
        internal static bool IsAClearEvent(FrameEventType eventType) => eventType >= FrameEventType.ClearNone && eventType <= FrameEventType.ClearAll;
        internal static bool IsAResolveEvent(FrameEventType eventType) => eventType == FrameEventType.ResolveRT || eventType == FrameEventType.ResolveDepth;
        internal static bool IsAComputeEvent(FrameEventType eventType) => eventType == FrameEventType.ComputeDispatch;
        internal static bool IsARayTracingEvent(FrameEventType eventType) => eventType == FrameEventType.RayTracingDispatch;
        internal static bool IsAHiddenEvent(FrameEventType eventType) => eventType == FrameEventType.BeginSubpass;
        internal static bool IsAHierarchyLevelBreakEvent(FrameEventType eventType) => eventType == FrameEventType.HierarchyLevelBreak;
        internal static bool IsCurrentEventMouseDown() => Event.current.type == EventType.MouseDown;
        internal static bool IsClickingRect(Rect rect) => rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown;

        internal static string GetStencilString(int stencil)
        {
            const int k_NumOfUsedStencilBits = 8;
            return $"(0b{Convert.ToString(stencil, 2).PadLeft(k_NumOfUsedStencilBits, '0')}) {stencil}";
        }

        internal static string GetFormat(Texture texture)
        {
            if (texture == null)
            {
                return string.Empty;
            }

            if (texture is Texture2D)
            {
                return (texture as Texture2D).format.ToString();
            }
            else if (texture is Cubemap)
            {
                return (texture as Cubemap).format.ToString();
            }
            else if (texture is Texture2DArray)
            {
                return (texture as Texture2DArray).format.ToString();
            }
            else if (texture is Texture3D)
            {
                return (texture as Texture3D).format.ToString();
            }
            else if (texture is CubemapArray)
            {
                return (texture as CubemapArray).format.ToString();
            }
            else if (texture is RenderTexture)
            {
                return (texture as RenderTexture).graphicsFormat.ToString();
            }

            return string.Empty;
        }
    }
}
