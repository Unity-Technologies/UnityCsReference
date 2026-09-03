// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/RendererUpdateManager.h")]
    [StaticAccessor("GetRendererUpdateManager()", StaticAccessorType.Dot)]
    internal static class RendererUpdateManagerBindings
    {
        [NativeMethod("GetMotionVectorFrameIndex")]
        public static extern int GetMotionVectorFrameIndex();
    }
}
