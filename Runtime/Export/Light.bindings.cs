// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Script interface for [[wiki:class-Light|light components]].
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Export/Light.bindings.h")]
    public sealed partial class Light : Behaviour
    {
        extern public void Reset();

        // How this light casts shadows?
        extern public LightShadows shadows
        {
            [NativeMethod("GetShadowType")] get;
            [FreeFunction("Light_Bindings::SetShadowType", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // Strength of light's shadows
        extern public float shadowStrength
        {
            get;
            [FreeFunction("Light_Bindings::SetShadowStrength", HasExplicitThis = true)] set;
        }

        // Shadow resolution
        extern public UnityEngine.Rendering.LightShadowResolution shadowResolution
        {
            get;
            [FreeFunction("Light_Bindings::SetShadowResolution", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // Note: do not remove (so that projects with assembly-only scritps using this will continue working),
        // just make it do nothing.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Shadow softness is removed in Unity 5.0+", true)]
        public float shadowSoftness
        {
            get { return 4.0f; }
            set {}
        }

        // Note: do not remove (so that projects with assembly-only scritps using this will continue working),
        // just make it do nothing.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Shadow softness is removed in Unity 5.0+", true)]
        public float shadowSoftnessFade
        {
            get { return 1.0f; }
            set {}
        }

        extern public float[] layerShadowCullDistances
        {
            [FreeFunction("Light_Bindings::GetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = false)]
            get;
            [FreeFunction("Light_Bindings::SetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = true)]
            set;
        }

        extern public float cookieSize { get; set; }

        // The cookie texture projected by the light.
        extern public Texture cookie { get; set; }

        // How to render the light.
        extern public LightRenderMode renderMode
        {
            get;
            [FreeFunction("Light_Bindings::SetRenderMode", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // This index was used to denote lights which contribution was baked in lightmaps and/or lightprobes.
        private int m_BakedIndex;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("warning bakedIndex has been removed please use bakingOutput.isBaked instead.", true)]
        public int bakedIndex { get { return m_BakedIndex; } set { m_BakedIndex = value; } }

        // The size of the area light. Editor only.
        extern public Vector2 areaSize { get; set; }

        // Table defining the falloff curve for baked light sources.
        [FreeFunction("Light_Bindings::SetFalloffTable", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetFalloffTable([NotNull] float[] input);

        [FreeFunction("Light_Bindings::SetAllLightsFalloffToInverseSquared")]
        extern private static void SetAllLightsFalloffToInverseSquared();

        [FreeFunction("Light_Bindings::SetAllLightsFalloffToUnityLegacy")]
        extern private static void SetAllLightsFalloffToUnityLegacy();

        // Lightmapping mode. Editor only.
        extern public LightmapBakeType lightmapBakeType
        {
            [NativeMethod("GetBakeType")] get;
            [NativeMethod("SetBakeType")] set;
        }

        extern public void SetLightDirty();

        public void AddCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer)
        {
            AddCommandBuffer(evt, buffer, UnityEngine.Rendering.ShadowMapPass.All);
        }

        [FreeFunction("Light_Bindings::AddCommandBuffer", HasExplicitThis = true)]
        public extern void AddCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask);

        public void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ComputeQueueType queueType)
        {
            AddCommandBufferAsync(evt, buffer, UnityEngine.Rendering.ShadowMapPass.All, queueType);
        }

        [FreeFunction("Light_Bindings::AddCommandBufferAsync", HasExplicitThis = true)]
        public extern void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType);

        extern public void RemoveCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer);

        extern public void RemoveCommandBuffers(UnityEngine.Rendering.LightEvent evt);

        extern public void RemoveAllCommandBuffers();

        [FreeFunction("Light_Bindings::GetCommandBuffers", HasExplicitThis = true)]
        extern public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.LightEvent evt);

        extern public int commandBufferCount { get; }


        [System.Obsolete("Use QualitySettings.pixelLightCount instead.")]
        public static int pixelLightCount
        {
            get { return QualitySettings.pixelLightCount; }
            set { QualitySettings.pixelLightCount = value; }
        }

        //*undocumented For terrain engine only
        [FreeFunction("Light_Bindings::GetLights")]
        extern public static Light[] GetLights(LightType type, int layer);

        [Obsolete("light.shadowConstantBias was removed, use light.shadowBias", true)]
        public float shadowConstantBias { get { return 0.0f; } set {} }

        [Obsolete("light.shadowObjectSizeBias was removed, use light.shadowBias", true)]
        public float shadowObjectSizeBias { get { return 0.0f; } set {} }

        [Obsolete("light.attenuate was removed; all lights always attenuate now", true)]
        public bool attenuate { get { return true; } set {} }
    }
}
