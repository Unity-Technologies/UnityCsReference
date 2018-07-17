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
    }

    // A structure describing the webcam device.
    public partial struct WebCamDevice
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

        internal string m_Name;
        internal string m_DepthCameraName;
        internal int m_Flags;
        internal WebCamKind m_Kind;
        internal Resolution[] m_Resolutions;
    }

    [NativeHeader("Runtime/Video/BaseWebCamTexture.h")]
    public partial class WebCamTexture : Texture
    {
        // Arbitrary focus point (relative x/y values in 0..1 range), null for automatic/continious focusing (default)
        public Vector2? autoFocusPoint
        {
            get { return internalAutoFocusPoint.x < 0 ? null : new Vector2 ? (internalAutoFocusPoint); }
            set { internalAutoFocusPoint = (value == null) ? new Vector2(-1, -1) : value.Value; }
        }
        internal extern Vector2 internalAutoFocusPoint { get; set; }

        // Does this WebCamTexture instance provide depth data
        public extern bool isDepth { get; }
    }

}
