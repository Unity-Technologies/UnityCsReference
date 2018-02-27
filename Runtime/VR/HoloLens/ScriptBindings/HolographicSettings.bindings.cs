// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.XR.WSA
{
    [StaticAccessor("HolographicSettings::GetInstance()", StaticAccessorType.Dot)]
    [NativeHeader("Runtime/VR/HoloLens/HolographicSettings.h")]
    public partial class HolographicSettings
    {
        public static void SetFocusPointForFrame(Vector3 position)
        {
            InternalSetFocusPointForFrameP(position);
        }

        public static void SetFocusPointForFrame(Vector3 position, Vector3 normal)
        {
            InternalSetFocusPointForFramePN(position, normal);
        }

        public static void SetFocusPointForFrame(Vector3 position, Vector3 normal, Vector3 velocity)
        {
            InternalSetFocusPointForFramePNV(position, normal, velocity);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("SetFocusPointForFrame")]
        private static extern void InternalSetFocusPointForFrameP(Vector3 position);

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("SetFocusPointForFrame")]
        private static extern void InternalSetFocusPointForFramePN(Vector3 position, Vector3 normal);


        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("SetFocusPointForFrame")]
        private static extern void InternalSetFocusPointForFramePNV(Vector3 position, Vector3 normal, Vector3 velocity);

        // This method returns whether or not the display is opaque by asking WinRT if the device family is Windows.Holographic or Windows.Desktop.
        //
        // "true" is the default value returned in case of error.
        public static bool IsDisplayOpaque { get { return true; }  }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        public static extern bool IsContentProtectionEnabled { get; set; }

        public enum HolographicReprojectionMode
        {
            PositionAndOrientation = 0,
            OrientationOnly = 1,
            Disabled = 2,
        };

        public static HolographicReprojectionMode ReprojectionMode
        {
            get { return HolographicReprojectionMode.Disabled; }
            set {}
        }
    }
}
