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
