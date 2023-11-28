// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    public class Watermark
    {
        [FreeFunction("IsAnyWatermarkVisible")]
        extern public static bool IsVisible();
    }
} // namespace UnityEngine.Rendering
