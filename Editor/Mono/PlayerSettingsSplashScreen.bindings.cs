// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEditor
{
    public partial class PlayerSettings
    {
        partial struct SplashScreenLogo
        {
            [ExcludeFromDocs]
            public static PlayerSettings.SplashScreenLogo Create(float duration)
            {
                Sprite logo = null;
                return PlayerSettings.SplashScreenLogo.Create(duration, logo);
            }

            [ExcludeFromDocs]
            public static PlayerSettings.SplashScreenLogo Create()
            {
                Sprite logo = null;
                float duration = k_MinLogoTime;
                return PlayerSettings.SplashScreenLogo.Create(duration, logo);
            }

            public static PlayerSettings.SplashScreenLogo Create([DefaultValue("k_MinLogoTime")] float duration, [DefaultValue("null")] Sprite logo)
            {
                return new PlayerSettings.SplashScreenLogo
                {
                    m_Duration = duration,
                    m_Logo = logo
                };
            }

            [ExcludeFromDocs]
            public static PlayerSettings.SplashScreenLogo CreateWithUnityLogo()
            {
                float duration = k_MinLogoTime;
                return PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(duration);
            }

            public static PlayerSettings.SplashScreenLogo CreateWithUnityLogo([DefaultValue("k_MinLogoTime")] float duration)
            {
                return new PlayerSettings.SplashScreenLogo
                {
                    m_Duration = duration,
                    m_Logo = PlayerSettings.SplashScreenLogo.s_UnityLogo
                };
            }
        }

        [StaticAccessor("GetPlayerSettings().GetSplashScreenSettings()", StaticAccessorType.Dot)]
        [NativeHeader("Editor/Mono/PlayerSettingsSplashScreen.bindings.h")]
        public partial class SplashScreen
        {
            [NativeName("SplashScreenAnimation")]
            public static extern AnimationMode animationMode { get; set; }

            [NativeName("SplashScreenBackgroundZoom")]
            public static extern float animationBackgroundZoom { get; set; }

            [NativeName("SplashScreenLogoZoom")]
            public static extern float animationLogoZoom { get; set; }

            public static extern Sprite background
            {
                [FreeFunction("GetSplashScreenBackgroundSourceLandscape")]
                get;
                [FreeFunction("SetSplashScreenBackgroundSourceLandscape")]
                set;
            }

            public static extern Sprite backgroundPortrait
            {
                [FreeFunction("GetSplashScreenBackgroundSourcePortrait")]
                get;
                [FreeFunction("SetSplashScreenBackgroundSourcePortrait")]
                set;
            }

            [NativeName("SplashScreenBackgroundColor")]
            public static extern Color backgroundColor { get; set; }

            [NativeName("SplashScreenDrawMode")]
            public static extern DrawMode drawMode { get; set; }

            [NativeName("SplashScreenLogos")]
            public static extern SplashScreenLogo[] logos { get; [FreeFunction("SetLogos")] set; }

            [NativeName("SplashScreenOverlayOpacity")]
            public static extern float overlayOpacity { get; set; }

            [NativeName("ShowUnitySplashScreen")]
            public static extern bool show { get; set; }

            [NativeName("ShowUnitySplashLogo")]
            public static extern bool showUnityLogo { get; set; }

            [NativeName("SplashScreenLogoStyle")]
            public static extern UnityLogoStyle unityLogoStyle { get; set; }
        }
    }
}
