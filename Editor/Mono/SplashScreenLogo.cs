// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public partial class PlayerSettings
    {
        public partial struct SplashScreenLogo
        {
            private const float k_MinLogoTime = 2.0f;
            private static Sprite s_UnityLogo;
            private Sprite m_Logo;
            private float m_Duration;

            static SplashScreenLogo()
            {
                s_UnityLogo = Resources.GetBuiltinResource<Sprite>("UnitySplash-cube.png");
            }

            public Sprite logo
            {
                get { return m_Logo; }
                set { m_Logo = value; }
            }

            public static Sprite unityLogo
            {
                get { return s_UnityLogo; }
            }

            public float duration
            {
                get { return Mathf.Max(m_Duration, k_MinLogoTime); }
                set { m_Duration = Mathf.Max(value, k_MinLogoTime); }
            }
        }
    }
}
