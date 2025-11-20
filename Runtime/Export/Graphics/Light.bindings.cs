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
    [NativeHeader("Runtime/Camera/SharedLightData.h")]
    public struct LightBakingOutput
    {
        public int probeOcclusionLightIndex;
        public int occlusionMaskChannel;
        [NativeName("lightmapBakeMode.lightmapBakeType")]
        public LightmapBakeType lightmapBakeType;
        [NativeName("lightmapBakeMode.mixedLightingMode")]
        public MixedLightingMode mixedLightingMode;
        public bool isBaked;
    }

    [NativeHeader("Runtime/Camera/SharedLightData.h")]
    public enum LightShadowCasterMode
    {
        Default = 0,
        [Obsolete("This has been deprecated. Use ShadowMask instead. (UnityUpgradable) -> ShadowMask")] NonLightmappedOnly = 1,
        ShadowMask = 1,
        [Obsolete("This has been deprecated. Use DistanceShadowMaskMode instead. (UnityUpgradable) -> DistanceShadowMask")] Everything = 2,
        DistanceShadowMask = 2
    }

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
        public LightShadowResolution shadowResolution
        {
            get => ShadowResolution;
            set
            {
                if (RenderPipelineManager.currentPipeline != null)
                    LogWarningOnlyBuiltIn();
                ShadowResolution = value;
            }
        }

        static void LogWarningOnlyBuiltIn([CallerMemberName] string propertyName = "")
        {
            Debug.LogWarning($"Light.{propertyName} is compatible only with the Built-In Render Pipeline.");
        }

        extern LightShadowResolution ShadowResolution
        {
            get;
            [FreeFunction("Light_Bindings::SetShadowResolution", HasExplicitThis = true, ThrowsException = true)] set;
        }

        extern public float[] layerShadowCullDistances
        {
            [FreeFunction("Light_Bindings::GetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = false)]
            get;
            [FreeFunction("Light_Bindings::SetLayerShadowCullDistances", HasExplicitThis = true, ThrowsException = true)]
            set;
        }

        extern public Vector2 cookieSize2D { get; set; }

        // The cookie texture projected by the light.
        extern public Texture cookie { get; set; }

        // How to render the light.
        extern public LightRenderMode renderMode
        {
            get;
            [FreeFunction("Light_Bindings::SetRenderMode", HasExplicitThis = true, ThrowsException = true)] set;
        }

        // The size of the area light.
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
                LogWarningOnlyBuiltIn();
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
                LogWarningOnlyBuiltIn();
            }
            AddCommandBufferAsyncInternal(evt, buffer, shadowPassMask, queueType);
        }

        [FreeFunction("Light_Bindings::AddCommandBufferAsync", HasExplicitThis = true)]
        internal extern void AddCommandBufferAsyncInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer, UnityEngine.Rendering.ShadowMapPass shadowPassMask, UnityEngine.Rendering.ComputeQueueType queueType);

        public void RemoveCommandBuffer(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveCommandBufferInternal(evt, buffer);
        }
        [NativeMethod("RemoveCommandBuffer")]extern internal void RemoveCommandBufferInternal(UnityEngine.Rendering.LightEvent evt, UnityEngine.Rendering.CommandBuffer buffer);

        public void RemoveCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveCommandBuffersInternal(evt);
        }
        [NativeMethod("RemoveCommandBuffers")] extern internal void RemoveCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        public void RemoveAllCommandBuffers()
        {
            if (RenderPipelineManager.currentPipeline != null)
            {
                LogWarningOnlyBuiltIn();
            }
            RemoveAllCommandBuffersInternal();
        }
        [NativeMethod("RemoveAllCommandBuffers")] extern internal void RemoveAllCommandBuffersInternal();

        public UnityEngine.Rendering.CommandBuffer[] GetCommandBuffers(UnityEngine.Rendering.LightEvent evt)
        {
            if(RenderPipelineManager.currentPipeline != null)
            {
                LogWarningOnlyBuiltIn();
            }
            return GetCommandBuffersInternal(evt);
        }
        [FreeFunction("Light_Bindings::GetCommandBuffers", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal UnityEngine.Rendering.CommandBuffer[] GetCommandBuffersInternal(UnityEngine.Rendering.LightEvent evt);

        extern public int commandBufferCount { get; }

        [NativeProperty("LightType")] extern public LightType type { get; set; }

        extern public float spotAngle { get; set; }
        extern public float innerSpotAngle { get; set; }
        extern public Color color { get; set; }
        extern public float colorTemperature { get; set; }
        extern public bool useColorTemperature { get; set; }
        extern public float intensity { get; set; }
        extern public float bounceIntensity { get; set; }
        extern public LightUnit lightUnit { get; set; }
        extern public float luxAtDistance { get; set; }
        extern public bool enableSpotReflector { get; set; }

        extern public bool useBoundingSphereOverride { get; set; }
        extern public Vector4 boundingSphereOverride { get; set; }

        extern public bool useViewFrustumForShadowCasterCull { get; set; }
        extern public bool forceVisible { get; set; }
        extern public int shadowCustomResolution { get; set; }
        extern public float shadowBias { get; set; }
        extern public float shadowNormalBias { get; set; }
        extern public float shadowNearPlane { get; set; }
        extern public bool useShadowMatrixOverride { get; set; }
        extern public Matrix4x4 shadowMatrixOverride { get; set; }

        extern public float range { get; set; }
        extern public float dilatedRange { get; }
        extern public Flare flare { get; set; }

        extern public LightBakingOutput bakingOutput { get; set; }
        extern public int cullingMask { get; set; }
        extern public int renderingLayerMask { get; set; }
        extern public LightShadowCasterMode lightShadowCasterMode { get; set; }
        extern public float shapeRadius { get; set; }

        extern public float shadowAngle { get; set; }
    }
}
