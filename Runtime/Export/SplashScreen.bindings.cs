// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    public class SplashScreen
    {
        extern public static bool isFinished {[FreeFunction("IsSplashScreenFinished")] get; }

        [FreeFunction("BeginSplashScreen_Binding")]
        extern public static void Begin();

        [FreeFunction("DrawSplashScreen_Binding")]
        extern public static void Draw();
    }
} // namespace UnityEngine.Rendering
