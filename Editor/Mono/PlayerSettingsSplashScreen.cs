// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    public partial class PlayerSettings : UnityEngine.Object
    {
        public partial class SplashScreen
        {
            public enum AnimationMode
            {
                Static = 0,
                Dolly = 1,
                Custom = 2
            }

            public enum DrawMode
            {
                UnityLogoBelow = 0,
                AllSequential = 1
            }

            public enum UnityLogoStyle
            {
                DarkOnLight = 0,
                LightOnDark = 1
            }
        }
    }
}
