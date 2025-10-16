// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [Obsolete("LightmappingMode has been deprecated. Use LightmapBakeType instead (UnityUpgradable) -> LightmapBakeType", true)]
    public enum LightmappingMode
    {
        [Obsolete("LightmappingMode.Realtime has been deprecated. Use LightmapBakeType.Realtime instead (UnityUpgradable) -> LightmapBakeType.Realtime", true)]
        Realtime = 4,
        [Obsolete("LightmappingMode.Baked has been deprecated. Use LightmapBakeType.Baked instead (UnityUpgradable) -> LightmapBakeType.Baked", true)]
        Baked = 2,
        [Obsolete("LightmappingMode.Mixed has been deprecated. Use LightmapBakeType.Mixed instead (UnityUpgradable) -> LightmapBakeType.Mixed", true)]
        Mixed = 1
    }

    public partial class Light
    {
        // Note: do not remove (so that projects with assembly-only scritps using this will continue working),
        // just make it do nothing.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Shadow softness is removed in Unity 5.0+", true)]
        public float shadowSoftness
        {
            get { return 4.0f; }
            set { }
        }

        // Note: do not remove (so that projects with assembly-only scritps using this will continue working),
        // just make it do nothing.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Shadow softness is removed in Unity 5.0+", true)]
        public float shadowSoftnessFade
        {
            get { return 1.0f; }
            set { }
        }

        // This index was used to denote lights which contribution was baked in lightmaps and/or lightprobes.
        private int m_BakedIndex;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("warning bakedIndex has been removed please use bakingOutput.isBaked instead.", true)]
        public int bakedIndex { get { return m_BakedIndex; } set { m_BakedIndex = value; } }

        [Obsolete("Light.cookieSize has been deprecated. Use Light.cookieSize2D instead.", false)]
        public float cookieSize
        {
            get { return cookieSize2D.x; }
            set { cookieSize2D = new Vector2(value, value); }
        }

        [Obsolete("This property has been deprecated. Use shapeRadius instead. (UnityUpgradable) -> shapeRadius")]
        public float shadowRadius
        {
            get => shapeRadius;
            set => shapeRadius = value;
        }

        [Obsolete("This property has been deprecated. Use Light.type instead.")] public LightShape shape { get; set; }

        [Obsolete("Use QualitySettings.pixelLightCount instead.")]
        public static int pixelLightCount
        {
            get { return QualitySettings.pixelLightCount; }
            set { QualitySettings.pixelLightCount = value; }
        }

        //*undocumented For terrain engine only
        [Obsolete("Light.GetLights has been deprecated, use FindObjectsOfType in combination with light.cullingmask/light.type", false)]
        [FreeFunction("Light_Bindings::GetLights")]
        extern public static Light[] GetLights(LightType type, int layer);

        [Obsolete("light.shadowConstantBias was removed, use light.shadowBias", true)]
        public float shadowConstantBias { get { return 0.0f; } set { } }

        [Obsolete("light.shadowObjectSizeBias was removed, use light.shadowBias", true)]
        public float shadowObjectSizeBias { get { return 0.0f; } set { } }

        [Obsolete("light.attenuate was removed; all lights always attenuate now", true)]
        public bool attenuate { get { return true; } set { } }

        [Obsolete("Light.lightmappingMode has been deprecated. Use Light.lightmapBakeType instead (UnityUpgradable) -> lightmapBakeType", true)]
        public LightmappingMode lightmappingMode
        {
            get { return LightmappingMode.Realtime; }
            set {}
        }

        [Obsolete("Light.isBaked is no longer supported. Use Light.bakingOutput.isBaked (and other members of Light.bakingOutput) instead.", false)]
        public bool isBaked
        {
            get { return bakingOutput.isBaked; }
        }

        [Obsolete("Light.alreadyLightmapped is no longer supported. Use Light.bakingOutput instead. Allowing to describe mixed light on top of realtime and baked ones.", false)]
        public bool alreadyLightmapped
        {
            get { return bakingOutput.isBaked; }
            set
            {
                var lightBakingOutput = new LightBakingOutput
                {
                    probeOcclusionLightIndex = -1,
                    occlusionMaskChannel = -1,
                    lightmapBakeType = (value) ? LightmapBakeType.Baked : LightmapBakeType.Realtime,
                    isBaked = value
                };
                bakingOutput = lightBakingOutput;
            }
        }
    }
}
