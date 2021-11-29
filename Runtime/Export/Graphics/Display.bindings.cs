// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/DisplayManager.h")]
    public class Display
    {
        internal IntPtr  nativeDisplay;
        internal Display()
        {
            this.nativeDisplay = new IntPtr(0);
        }

        internal Display(IntPtr nativeDisplay)   { this.nativeDisplay = nativeDisplay; }

        public int    renderingWidth
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
        public int    renderingHeight
        {
            get
            {
                int w = 0, h = 0;
                GetRenderingExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }

        public int    systemWidth
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return w;
            }
        }
        public int    systemHeight
        {
            get
            {
                int w = 0, h = 0;
                GetSystemExtImpl(nativeDisplay, out w, out h);
                return h;
            }
        }

        public RenderBuffer colorBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return color;
            }
        }

        public RenderBuffer depthBuffer
        {
            get
            {
                RenderBuffer color, depth;
                GetRenderingBuffersImpl(nativeDisplay, out color, out depth);
                return depth;
            }
        }

        public bool active
        {
            get
            {
                return GetActiveImpl(nativeDisplay);
            }
        }

        public bool requiresBlitToBackbuffer
        {
            get
            {
                int displayIndex = nativeDisplay.ToInt32();
                if (displayIndex < HDROutputSettings.displays.Length)
                {
                    bool active = HDROutputSettings.displays[displayIndex].available && HDROutputSettings.displays[displayIndex].active;
                    if (active)
                        return true;
                }
                return RequiresBlitToBackbufferImpl(nativeDisplay);
            }
        }

        public bool requiresSrgbBlitToBackbuffer
        {
            get
            {
                return RequiresSrgbBlitToBackbufferImpl(nativeDisplay);
            }
        }

        public void Activate()
        {
            ActivateDisplayImpl(nativeDisplay, 0, 0, 60);
        }

        public void Activate(int width, int height, int refreshRate)
        {
            ActivateDisplayImpl(nativeDisplay, width, height, refreshRate);
        }

        public void SetParams(int width, int height, int x, int y)
        {
            SetParamsImpl(nativeDisplay, width, height, x, y);
        }

        public void SetRenderingResolution(int w, int h)
        {
            SetRenderingResolutionImpl(nativeDisplay, w, h);
        }

        [System.Obsolete("MultiDisplayLicense has been deprecated.", false)]
        public static bool MultiDisplayLicense()
        {
            return true;
        }

        public static Vector3 RelativeMouseAt(Vector3 inputMouseCoordinates)
        {
            Vector3 vec;
            int rx = 0, ry = 0;
            int x = (int)inputMouseCoordinates.x;
            int y = (int)inputMouseCoordinates.y;
            vec.z = (int)RelativeMouseAtImpl(x, y, out rx, out ry);
            vec.x = rx;
            vec.y = ry;
            return vec;
        }

        public static Display[] displays    = new Display[1] { new Display() };
        private static Display _mainDisplay = displays[0];
        public static Display   main        { get {return _mainDisplay; } }

        private static int m_ActiveEditorGameViewTarget = -1;
        public static int activeEditorGameViewTarget  { get { return m_ActiveEditorGameViewTarget; } internal set { m_ActiveEditorGameViewTarget = value; } }

        [RequiredByNativeCode]
        internal static void RecreateDisplayList(IntPtr[] nativeDisplay)
        {
            if (nativeDisplay.Length == 0) // case 1017288
                return;

            Display.displays = new Display[nativeDisplay.Length];
            for (int i = 0; i < nativeDisplay.Length; ++i)
                Display.displays[i] = new Display(nativeDisplay[i]);

            _mainDisplay = displays[0];
        }

        [RequiredByNativeCode]
        internal static void FireDisplaysUpdated()
        {
            if (onDisplaysUpdated != null)
                onDisplaysUpdated();
        }

        public delegate void DisplaysUpdatedDelegate();
        public static event DisplaysUpdatedDelegate onDisplaysUpdated = null;


        internal delegate void GetSystemExtDelegate(IntPtr nativeDisplay, out int w, out int h);
        internal delegate void GetRenderingExtDelegate(IntPtr nativeDisplay, out int w, out int h);
        internal delegate void GetRenderingBuffersDelegate(IntPtr nativeDisplay, out RenderBuffer color,
            out RenderBuffer depth);
        internal delegate void SetRenderingResolutionDelegate(IntPtr nativeDisplay, int w, int h);
        internal delegate void ActivateDisplayDelegate(IntPtr nativeDisplay, int width, int height, int refreshRate);
        internal delegate void SetParamsDelegate(IntPtr nativeDisplay, int width, int height, int x, int y);
        internal delegate int RelativeMouseAtDelegate(int x, int y, out int rx, out int ry);
        internal delegate bool GetActiveDelegate(IntPtr nativeDisplay);
        internal delegate bool RequiresBlitToBackbufferDelegate(IntPtr nativeDisplay);

        internal delegate bool RequiresSrgbBlitToBackbufferDelegate(IntPtr nativeDisplay);
        internal static event GetSystemExtDelegate onGetSystemExt = null;
        internal static event GetRenderingExtDelegate onGetRenderingExt = null;
        internal static event GetRenderingBuffersDelegate onGetRenderingBuffers = null;
        internal static event SetRenderingResolutionDelegate onSetRenderingResolution = null;
        internal static event ActivateDisplayDelegate onActivateDisplay = null;
        internal static event SetParamsDelegate onSetParams = null;
        internal static event RelativeMouseAtDelegate onRelativeMouseAt = null;
        internal static event GetActiveDelegate onGetActive = null;
        internal static event RequiresBlitToBackbufferDelegate onRequiresBlitToBackbuffer = null;
        internal static event RequiresSrgbBlitToBackbufferDelegate onRequiresSrgbBlitToBackbuffer = null;


        private static void GetSystemExtImpl(IntPtr nativeDisplay, out int w, out int h)
        {
            if (onGetSystemExt != null)
            {
                onGetSystemExt(nativeDisplay, out w, out h);
            }
            else
            {
                w = 0;
                h = 0;
            }
        }

        private static void GetRenderingExtImpl(IntPtr nativeDisplay, out int w, out int h)
        {
            if (onGetRenderingExt != null)
            {
                onGetRenderingExt(nativeDisplay, out w, out h);
            }
            else
            {
                w = 0;
                h = 0;
            }
        }

        private static void GetRenderingBuffersImpl(IntPtr nativeDisplay, out RenderBuffer color,
            out RenderBuffer depth)
        {
            if (onGetRenderingBuffers != null)
            {
                onGetRenderingBuffers(nativeDisplay, out color, out depth);
            }
            else
            {
                color = new RenderBuffer();
                depth = new RenderBuffer();
            }
        }

        private static void SetRenderingResolutionImpl(IntPtr nativeDisplay, int w, int h)
        {
            onSetRenderingResolution?.Invoke(nativeDisplay, w, h);
        }

        private static void ActivateDisplayImpl(IntPtr nativeDisplay, int width, int height, int refreshRate)
        {
            onActivateDisplay?.Invoke(nativeDisplay, width, height, refreshRate);
        }

        private static void SetParamsImpl(IntPtr nativeDisplay, int width, int height, int x, int y)
        {
            onSetParams?.Invoke(nativeDisplay, width, height, x, y);
        }

        private static int RelativeMouseAtImpl(int x, int y, out int rx, out int ry)
        {
            if (onRelativeMouseAt != null)
            {
                return onRelativeMouseAt(x, y, out rx, out ry);
            }
            rx = 0;
            ry = 0;

            return 0;
        }

        private static bool GetActiveImpl(IntPtr nativeDisplay)
        {
            return onGetActive != null && onGetActive(nativeDisplay);
        }

        private static bool RequiresBlitToBackbufferImpl(IntPtr nativeDisplay)
        {
            return onRequiresBlitToBackbuffer != null && onRequiresBlitToBackbuffer(nativeDisplay);
        }

        private static bool RequiresSrgbBlitToBackbufferImpl(IntPtr nativeDisplay)
        {
            return onRequiresSrgbBlitToBackbuffer != null && onRequiresSrgbBlitToBackbuffer(nativeDisplay);
        }

    }
}
