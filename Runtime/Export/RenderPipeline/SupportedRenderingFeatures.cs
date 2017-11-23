// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.Rendering
{
    public class SupportedRenderingFeatures
    {
        private static SupportedRenderingFeatures s_Active = new SupportedRenderingFeatures();
        public static SupportedRenderingFeatures active
        {
            get
            {
                if (s_Active == null)
                    s_Active = new SupportedRenderingFeatures();
                return s_Active;
            }
            set { s_Active = value; }
        }

        [System.Flags]
        public enum ReflectionProbeSupportFlags
        {
            None = 0,
            Rotation = 1
        }

        public ReflectionProbeSupportFlags reflectionProbeSupportFlags { get; set; } = ReflectionProbeSupportFlags.None;

        public bool rendererSupportsLightProbeProxyVolumes { get; set; } = true;
        public bool rendererSupportsMotionVectors { get; set; } = true;
        public bool rendererSupportsReceiveShadows { get; set; } = true;
        public bool rendererSupportsReflectionProbes { get; set; } = true;
    }
}
