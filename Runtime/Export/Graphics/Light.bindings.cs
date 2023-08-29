// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    // Script interface for [[wiki:class-Light|light components]].
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Export/Graphics/Light.bindings.h")]
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


        public void AddCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.AddCommandBuffer only with the built-in renderer.");
            }
            AddCommandBufferInternal(evt, buffer, shadowPassMask);
        }

        [FreeFunction("Light_Bindings::AddCommandBuffer", HasExplicitThis = true)]
        internal extern void AddCommandBufferInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask);

        public void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ComputeQueueType queueType)
        {
            AddCommandBufferAsync(evt, buffer, UnityEngine.Rendering.ShadowMapPass.All, queueType);
        }

        public void AddCommandBufferAsync(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.AddCommandBufferAsync only with the built-in renderer.");
            }
            AddCommandBufferAsyncInternal(evt, buffer, shadowPassMask, queueType);
        }

        [FreeFunction("Light_Bindings::AddCommandBufferAsync", HasExplicitThis = true)]
        internal extern void AddCommandBufferAsyncInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType);

        public void RemoveCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.RemoveCommandBuffer only with the built-in renderer.");
            }
            RemoveCommandBufferInternal(evt, buffer);
        }
        [NativeMethod("RemoveCommandBuffer")]extern internal void RemoveCommandBufferInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer);

        public void RemoveCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.RemoveCommandBuffer only with the built-in renderer.");
            }
            RemoveCommandBuffersInternal(evt);
        }
        [NativeMethod("RemoveCommandBuffers")] extern internal void RemoveCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        public void RemoveAllCommandBuffers()
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.RemoveAllCommandBuffers only with the built-in renderer.");
            }
            RemoveAllCommandBuffersInternal();
        }
        [NativeMethod("RemoveAllCommandBuffers")] extern internal void RemoveAllCommandBuffersInternal();

        public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if(RenderPipelineManager.currentPipeline != null)
            {
                Debug.LogWarning("Your project uses a scriptable render pipeline. You can use Light.GetCommandBuffers only with the built-in renderer.");
            }
            return GetCommandBuffersInternal(evt);
        }
        [FreeFunction("Light_Bindings::GetCommandBuffers", HasExplicitThis = true)]
        extern internal UnityEngine.Rendering.CommandBuffer[] GetCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        extern public int commandBufferCount { get; }


        [System.Obsolete("Use QualitySettings.pixelLightCount instead.")]
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
        public float shadowConstantBias { get { return 0.0f; } set {} }

        [Obsolete("light.shadowObjectSizeBias was removed, use light.shadowBias", true)]
        public float shadowObjectSizeBias { get { return 0.0f; } set {} }

        [Obsolete("light.attenuate was removed; all lights always attenuate now", true)]
        public bool attenuate { get { return true; } set {} }
    }
}
