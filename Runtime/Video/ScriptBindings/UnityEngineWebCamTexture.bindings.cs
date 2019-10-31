// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{

    //*undocumented*
    public enum WebCamFlags
    {
        // Camera faces the same direction as screen
        FrontFacing = 1,
        // Camera supports arbitrary focus point
        AutoFocusPointSupported = 2,
    }

    public enum WebCamKind
    {
        // WideAngle (default) camera
        WideAngle = 1,
        // Telephoto camera
        Telephoto = 2,
        // Camera which supports synchronized color and depth data (Dual or TrueDepth)
        ColorAndDepth = 3,
        // Ultra WideAngle camera
        UltraWideAngle = 4,
    }

    // A structure describing the webcam device.
    [UsedByNativeCode]
    public struct WebCamDevice
    {
        // A human-readable name of the device. Varies across different systems.
        public string name { get { return m_Name; } }

        // True if camera faces the same direction a screen does, false otherwise.
        public bool isFrontFacing { get { return (m_Flags & ((int)WebCamFlags.FrontFacing)) != 0; } }

        // Kind of the WebCamDevice.
        public WebCamKind kind { get { return m_Kind; } }

        // A human-readable name of the paired device which provides depth data. Returns null if device doesn't support depth data.
        public string depthCameraName { get { return m_DepthCameraName == "" ? null : m_DepthCameraName; } }

        // True if camera device supports arbitrary focus point
        public bool isAutoFocusPointSupported { get { return (m_Flags & ((int)WebCamFlags.AutoFocusPointSupported)) != 0; } }

        public Resolution[] availableResolutions { get { return m_Resolutions; } }

        [NativeName("name")]
        internal string m_Name;

        [NativeName("depthCameraName")]
        internal string m_DepthCameraName;

        [NativeName("flags")]
        internal int m_Flags;

        [NativeName("kind")]
        internal WebCamKind m_Kind;

        [NativeName("resolutions")]
        internal Resolution[] m_Resolutions;
    }

    [NativeHeader("Runtime/Video/BaseWebCamTexture.h")]
    [NativeHeader("Runtime/Video/ScriptBindings/WebCamTexture.bindings.h")]
    [NativeHeader("AudioScriptingClasses.h")]
    public sealed class WebCamTexture : Texture
    {
        public extern static WebCamDevice[] devices
        {
            [StaticAccessor("WebCamTextureBindings", StaticAccessorType.DoubleColon)]
            [NativeName("Internal_GetDevices")]
            get;
        }

        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight, int requestedFPS)
        {
            Internal_CreateWebCamTexture(this, deviceName, requestedWidth, requestedHeight, requestedFPS);
        }

        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight)
        {
            Internal_CreateWebCamTexture(this, deviceName, requestedWidth, requestedHeight, 0);
        }

        public WebCamTexture(string deviceName)
        {
            Internal_CreateWebCamTexture(this, deviceName, 0, 0, 0);
        }

        public WebCamTexture(int requestedWidth, int requestedHeight, int requestedFPS)
        {
            Internal_CreateWebCamTexture(this, "", requestedWidth, requestedHeight, requestedFPS);
        }

        public WebCamTexture(int requestedWidth, int requestedHeight)
        {
            Internal_CreateWebCamTexture(this, "", requestedWidth, requestedHeight, 0);
        }

        public WebCamTexture()
        {
            Internal_CreateWebCamTexture(this, "", 0, 0, 0);
        }

        public extern void Play();
        public extern void Pause();
        public extern void Stop();

        public extern bool isPlaying
        {
            [NativeName("IsPlaying")]
            get;
        }

        [NativeName("Device")]
        public extern string deviceName { get; set; }

        public extern float requestedFPS { get; set; }
        public extern int requestedWidth { get; set; }
        public extern int requestedHeight { get; set; }

        public extern int videoRotationAngle { get; }
        public extern bool videoVerticallyMirrored
        {
            [NativeName("IsVideoVerticallyMirrored")]
            get;
        }
        public extern bool didUpdateThisFrame
        {
            [NativeName("DidUpdateThisFrame")]
            get;
        }

        public extern Color GetPixel(int x, int y);
        public Color[] GetPixels()
        {
            return GetPixels(0, 0, width, height);
        }

        [FreeFunction("WebCamTextureBindings::Internal_GetPixels", HasExplicitThis = true)]
        public extern Color[] GetPixels(int x, int y, int blockWidth, int blockHeight);

        [UnityEngine.Internal.ExcludeFromDocs]
        public Color32[] GetPixels32()
        {
            return GetPixels32(null);
        }

        [FreeFunction("WebCamTextureBindings::Internal_GetPixels32", HasExplicitThis = true)]
        public extern Color32[] GetPixels32([UnityEngine.Internal.DefaultValue("null")] Color32[] colors);

        // Arbitrary focus point (relative x/y values in 0..1 range), null for automatic/continious focusing (default)
        public Vector2? autoFocusPoint
        {
            get { return internalAutoFocusPoint.x < 0 ? null : new Vector2 ? (internalAutoFocusPoint); }
            set { internalAutoFocusPoint = (value == null) ? new Vector2(-1, -1) : value.Value; }
        }
        internal extern Vector2 internalAutoFocusPoint { get; set; }

        // Does this WebCamTexture instance provide depth data
        public extern bool isDepth { get; }

        [StaticAccessor("WebCamTextureBindings", StaticAccessorType.DoubleColon)]
        private static extern void Internal_CreateWebCamTexture([Writable] WebCamTexture self, string scriptingDevice, int requestedWidth, int requestedHeight, int maxFramerate);
    }

}
