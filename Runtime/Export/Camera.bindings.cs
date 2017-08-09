// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    public partial class Camera
    {
        public extern Matrix4x4 GetStereoNonJitteredProjectionMatrix(StereoscopicEye eye);

        public extern void CopyStereoDeviceProjectionMatrixToNonJittered(StereoscopicEye eye);
    }
}
