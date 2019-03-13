// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    public class SplashScreen
    {
        public enum StopBehavior
        {
            StopImmediate = 0,
            FadeOut = 1
        }

        extern public static bool isFinished {[FreeFunction("IsSplashScreenFinished")] get; }

        [FreeFunction]
        extern static void CancelSplashScreen();

        [FreeFunction]
        extern static void BeginSplashScreenFade();

        [FreeFunction("BeginSplashScreen_Binding")]
        extern public static void Begin();

        public static void Stop(StopBehavior stopBehavior)
        {
            if (stopBehavior == StopBehavior.FadeOut)
                BeginSplashScreenFade();
            else
                CancelSplashScreen();
        }

        [FreeFunction("DrawSplashScreen_Binding")]
        extern public static void Draw();

        [FreeFunction("SetSplashScreenTime")]
        extern internal static void SetTime(float time);
    }
} // namespace UnityEngine.Rendering
