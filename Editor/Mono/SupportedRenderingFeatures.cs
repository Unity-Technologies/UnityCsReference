// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    public struct SupportedRenderingFeatures
    {
        [System.Flags]
        public enum ReflectionProbe
        {
            None = 0,
            Rotation = 1
        }
        public ReflectionProbe reflectionProbe;


        private static SupportedRenderingFeatures s_Active = new SupportedRenderingFeatures();
        public static SupportedRenderingFeatures active
        {
            get { return s_Active; }
            set { s_Active = value; }
        }

        public static SupportedRenderingFeatures Default
        {
            get { return new SupportedRenderingFeatures(); }
        }
    }
}
