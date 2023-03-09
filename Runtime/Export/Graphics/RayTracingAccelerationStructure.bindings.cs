// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// RayTracingMode enum will be moved into UnityEngine.Rendering in the future.
using RayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/RayTracing/RayTracingAccelerationStructure.h")]
    [NativeHeader("Runtime/Export/Graphics/RayTracingAccelerationStructure.bindings.h")]

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    [Flags]
    public enum RayTracingSubMeshFlags
    {
        Disabled            = 0,
        Enabled             = (1 << 0),
        ClosestHitOnly      = (1 << 1),
        UniqueAnyHitCalls   = (1 << 2),
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    [Flags]
    public enum RayTracingInstanceCullingFlags
    {
        None                    = 0,
        EnableSphereCulling     = (1 << 0),
        EnablePlaneCulling      = (1 << 1),
        EnableLODCulling        = (1 << 2),
        ComputeMaterialsCRC     = (1 << 3),
        IgnoreReflectionProbes  = (1 << 4),
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceCullingTest
    {
        public uint instanceMask;
        public int layerMask;
        public int shadowCastingModeMask;
        public bool allowOpaqueMaterials;
        public bool allowTransparentMaterials;
        public bool allowAlphaTestedMaterials;
        public bool allowVisualEffects;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceCullingShaderTagConfig
    {
        public ShaderTagId tagId;
        public ShaderTagId tagValueId;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceMaterialConfig
    {
        public int renderQueueLowerBound;
        public int renderQueueUpperBound;

        public RayTracingInstanceCullingShaderTagConfig[] optionalShaderTags;

        public string[] optionalShaderKeywords;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceCullingMaterialTest
    {
        public string[] deniedShaderPasses;

        public RayTracingInstanceCullingShaderTagConfig[] requiredShaderTags;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceTriangleCullingConfig
    {
        public string[] optionalDoubleSidedShaderKeywords;

        public bool frontTriangleCounterClockwise;

        public bool checkDoubleSidedGIMaterial;

        public bool forceDoubleSided;
    };

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingSubMeshFlagsConfig
    {
        public RayTracingSubMeshFlags opaqueMaterials;
        public RayTracingSubMeshFlags transparentMaterials;
        public RayTracingSubMeshFlags alphaTestedMaterials;
    };

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceCullingConfig
    {
        public RayTracingInstanceCullingFlags flags;

        public Vector3 sphereCenter;
        public float sphereRadius;

        public Plane[] planes;

        public RayTracingInstanceCullingTest[] instanceTests;

        public RayTracingInstanceCullingMaterialTest materialTest;

        public RayTracingInstanceMaterialConfig transparentMaterialConfig;
        public RayTracingInstanceMaterialConfig alphaTestedMaterialConfig;

        public RayTracingSubMeshFlagsConfig subMeshFlagsConfig;

        public RayTracingInstanceTriangleCullingConfig triangleCullingConfig;

        public LODParameters lodParameters;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceMaterialCRC
    {
        public int instanceID;
        public int crc;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingInstanceCullingResults
    {
        public RayTracingInstanceMaterialCRC[] materialsCRC;
        public bool transformsChanged;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public struct RayTracingMeshInstanceConfig
    {
        public RayTracingMeshInstanceConfig(Mesh mesh, uint subMeshIndex, Material material)
        {
            this.mesh = mesh;
            this.subMeshIndex = subMeshIndex;
            this.material = material;
            subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
            dynamicGeometry = false;
            materialProperties = null;
            enableTriangleCulling = true;
            frontTriangleCounterClockwise = false;
            layer = 0;
            renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask;
            mask = 0xFF;
            motionVectorMode = MotionVectorGenerationMode.Camera;
            lightProbeUsage = LightProbeUsage.Off;
            lightProbeProxyVolume = null;

        }
        public Mesh mesh;
        public uint subMeshIndex;
        public RayTracingSubMeshFlags subMeshFlags;
        public bool dynamicGeometry;
        public Material material;
        public MaterialPropertyBlock materialProperties;
        public bool enableTriangleCulling;
        public bool frontTriangleCounterClockwise;
        public int layer;
        public uint renderingLayerMask;
        public uint mask;
        public MotionVectorGenerationMode motionVectorMode;
        public LightProbeUsage lightProbeUsage;
        public LightProbeProxyVolume lightProbeProxyVolume;
    }

    [MovedFrom("UnityEngine.Experimental.Rendering")]
    public sealed class RayTracingAccelerationStructure : IDisposable
    {
        [Flags]
        public enum RayTracingModeMask
        {
            Nothing             = 0,
            Static              = (1 << RayTracingMode.Static),
            DynamicTransform    = (1 << RayTracingMode.DynamicTransform),
            DynamicGeometry     = (1 << RayTracingMode.DynamicGeometry),
            Everything          = (Static | DynamicTransform | DynamicGeometry)
        }

        public enum ManagementMode
        {
            Manual      = 0,    // Manual management of Renderers in the Raytracing Acceleration Structure.
            Automatic   = 1,    // New renderers are added automatically based on a RayTracingModeMask.
        }

        [System.Obsolete(@"RayTracingAccelerationStructure.RASSettings is deprecated. Use RayTracingAccelerationStructure.Settings instead. (UnityUpgradable) -> RayTracingAccelerationStructure/Settings", false)]
        public struct RASSettings
        {
            public ManagementMode managementMode;
            public RayTracingModeMask rayTracingModeMask;
            public int layerMask;
            public RASSettings(ManagementMode sceneManagementMode, RayTracingModeMask rayTracingModeMask, int layerMask)
            {
                this.managementMode = sceneManagementMode;
                this.rayTracingModeMask = rayTracingModeMask;
                this.layerMask = layerMask;
            }
        }
		
        public struct Settings
        {
            public ManagementMode managementMode;
            public RayTracingModeMask rayTracingModeMask;
            public int layerMask;
            public Settings(ManagementMode sceneManagementMode, RayTracingModeMask rayTracingModeMask, int layerMask)
            {
                this.managementMode = sceneManagementMode;
                this.rayTracingModeMask = rayTracingModeMask;
                this.layerMask = layerMask;
            }
        }

        // --------------------------------------------------------------------
        // IDisposable implementation, with Release() for explicit cleanup.

        ~RayTracingAccelerationStructure()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release native resources
                Destroy(this);
            }

            m_Ptr = IntPtr.Zero;
        }

        // --------------------------------------------------------------------
        // Actual API
        public RayTracingAccelerationStructure(Settings settings)
        {
            m_Ptr = Create(settings);
        }

        public RayTracingAccelerationStructure()
        {
            Settings settings = new Settings();
            settings.rayTracingModeMask = RayTracingModeMask.Everything;
            settings.managementMode = ManagementMode.Manual;
            settings.layerMask = -1;
            m_Ptr = Create(settings);
        }

        [FreeFunction("RayTracingAccelerationStructure_Bindings::Create")]
        extern private static IntPtr Create(Settings desc);

        [FreeFunction("RayTracingAccelerationStructure_Bindings::Destroy")]
        extern private static void Destroy(RayTracingAccelerationStructure accelStruct);

        public void Release()
        {
            Dispose();
        }

#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        public void Build()
        {
            Build(Vector3.zero);
        }

        public int AddInstance(Renderer targetRenderer, RayTracingSubMeshFlags[] subMeshFlags, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF)
        {
            return AddInstanceSubMeshFlagsArray(targetRenderer, subMeshFlags, enableTriangleCulling, frontTriangleCounterClockwise, mask, id);
        }

        public int AddInstance(GraphicsBuffer aabbBuffer, uint aabbCount, bool dynamicData, Matrix4x4 matrix, Material material, bool opaqueMaterial, MaterialPropertyBlock properties, uint mask = 0xFF, uint id = 0xFFFFFFFF)
        {
            return AddInstance_Procedural(aabbBuffer, aabbCount, dynamicData, matrix, material, opaqueMaterial, properties, mask, id);
        }

        public unsafe int AddInstance(in RayTracingMeshInstanceConfig config, Matrix4x4 matrix, [DefaultValue("null")] Matrix4x4? prevMatrix = null, uint id = 0xFFFFFFFF)
        {
            if (config.mesh == null)
                throw new ArgumentNullException("config.mesh");
            if (config.subMeshIndex >= config.mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("config.subMeshIndex", "config.subMeshIndex is out of range.");
            if (config.lightProbeUsage == LightProbeUsage.UseProxyVolume && config.lightProbeProxyVolume == null)
                throw new ArgumentException("config.lightProbeProxyVolume must not be null if config.lightProbeUsage is set to UseProxyVolume.");

            if (prevMatrix.HasValue)
            {
                Matrix4x4 temp = prevMatrix.Value;
                return AddMeshInstance(config, matrix, &temp, id);
            }
            return AddMeshInstance(config, matrix, null, id);
        }

        public unsafe int AddInstances<T>(in RayTracingMeshInstanceConfig config, T[] instanceData, [DefaultValue("-1")] int instanceCount = -1, [DefaultValue("0")] int startInstance = 0, uint id = 0xFFFFFFFF) where T : unmanaged
        {
            if (instanceData == null)
                throw new ArgumentNullException("instanceData");
            if (config.material == null)
                throw new ArgumentNullException("config.material");
            if (!config.material.enableInstancing)
                throw new InvalidOperationException("config.material needs to enable instancing for use with AddInstances.");
            if (config.mesh == null)
                throw new ArgumentNullException("config.mesh");
            if (config.subMeshIndex >= config.mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("config.subMeshIndex", "config.subMeshIndex is out of range.");
            if (config.lightProbeUsage == LightProbeUsage.UseProxyVolume && config.lightProbeProxyVolume == null)
                throw new ArgumentException("config.lightProbeProxyVolume argument must not be null if config.lightProbeUsage is set to UseProxyVolume.");

            RenderInstancedDataLayout layout = new RenderInstancedDataLayout(typeof(T));

            instanceCount = instanceCount == -1 ? instanceData.Length : instanceCount;
            startInstance = Math.Clamp(startInstance, 0, Math.Max(0, instanceData.Length - 1));
            instanceCount = Math.Clamp(instanceCount, 0, Math.Max(0, instanceData.Length - startInstance));

            if (instanceCount > Graphics.kMaxDrawMeshInstanceCount)
                throw new InvalidOperationException(String.Format("Instance count cannot exceed {0}.", Graphics.kMaxDrawMeshInstanceCount));

            fixed (T* data = instanceData) { return AddMeshInstances(config, (IntPtr)(data + startInstance), layout, (uint)instanceCount, id); }
        }

        public unsafe int AddInstances<T>(in RayTracingMeshInstanceConfig config, List<T> instanceData, [DefaultValue("-1")] int instanceCount = -1, [DefaultValue("0")] int startInstance = 0, uint id = 0xFFFFFFFF) where T : unmanaged
        {
            if (instanceData == null)
                throw new ArgumentNullException("instanceData");
            if (config.material == null)
                throw new ArgumentNullException("config.material");
            if (!config.material.enableInstancing)
                throw new InvalidOperationException("config.material needs to enable instancing for use with AddInstances.");
            if (config.mesh == null)
                throw new ArgumentNullException("config.mesh");
            if (config.subMeshIndex >= config.mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("config.subMeshIndex", "config.subMeshIndex is out of range.");
            if (config.lightProbeUsage == LightProbeUsage.UseProxyVolume && config.lightProbeProxyVolume == null)
                throw new ArgumentException("config.lightProbeProxyVolume argument must not be null if config.lightProbeUsage is set to UseProxyVolume.");

            RenderInstancedDataLayout layout = new RenderInstancedDataLayout(typeof(T));

            instanceCount = instanceCount == -1 ? instanceData.Count : instanceCount;
            startInstance = Math.Clamp(startInstance, 0, Math.Max(0, instanceData.Count - 1));
            instanceCount = Math.Clamp(instanceCount, 0, Math.Max(0, instanceData.Count - startInstance));

            if (instanceCount > Graphics.kMaxDrawMeshInstanceCount)
                throw new InvalidOperationException(String.Format("Instance count cannot exceed {0}.", Graphics.kMaxDrawMeshInstanceCount));

            fixed (T* data = NoAllocHelpers.ExtractArrayFromListT(instanceData)) { return AddMeshInstances(config, (IntPtr)(data + startInstance), layout, (uint)instanceCount, id); }
        }

        public unsafe int AddInstances<T>(in RayTracingMeshInstanceConfig config, NativeArray<T> instanceData, [DefaultValue("-1")] int instanceCount = -1, [DefaultValue("0")] int startInstance = 0, uint id = 0xFFFFFFFF) where T : unmanaged
        {
            if (config.material == null)
                throw new ArgumentNullException("config.material");
            if (!config.material.enableInstancing)
                throw new InvalidOperationException("config.material needs to enable instancing for use with AddInstances.");
            if (config.mesh == null)
                throw new ArgumentNullException("config.mesh");
            if (config.subMeshIndex >= config.mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("config.subMeshIndex", "config.subMeshIndex is out of range.");
            if (config.lightProbeUsage == LightProbeUsage.UseProxyVolume && config.lightProbeProxyVolume == null)
                throw new ArgumentException("config.lightProbeProxyVolume argument must not be null if config.lightProbeUsage is set to UseProxyVolume.");

            RenderInstancedDataLayout layout = new RenderInstancedDataLayout(typeof(T));

            instanceCount = instanceCount == -1 ? instanceData.Length : instanceCount;
            startInstance = Math.Clamp(startInstance, 0, Math.Max(0, instanceData.Length - 1));
            instanceCount = Math.Clamp(instanceCount, 0, Math.Max(0, instanceData.Length - startInstance));

            if (instanceCount > Graphics.kMaxDrawMeshInstanceCount)
                throw new InvalidOperationException(String.Format("Instance count cannot exceed {0}.", Graphics.kMaxDrawMeshInstanceCount));

            return AddMeshInstances(config, (IntPtr)((T*)instanceData.GetUnsafeReadOnlyPtr() + startInstance), layout, (uint)instanceCount, id);
        }

        public unsafe int AddInstances<T>(in RayTracingMeshInstanceConfig config, NativeSlice<T> instanceData, uint id = 0xFFFFFFFF) where T : unmanaged
        {
            if (config.material == null)
                throw new ArgumentNullException("config.material");
            if (!config.material.enableInstancing)
                throw new InvalidOperationException("config.material needs to enable instancing for use with AddInstances.");
            if (config.mesh == null)
                throw new ArgumentNullException("config.mesh");
            if (config.subMeshIndex >= config.mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("config.subMeshIndex", "config.subMeshIndex is out of range.");
            if (config.lightProbeUsage == LightProbeUsage.UseProxyVolume && config.lightProbeProxyVolume == null)
                throw new ArgumentException("config.lightProbeProxyVolume argument must not be null if config.lightProbeUsage is set to UseProxyVolume.");

            RenderInstancedDataLayout layout = new RenderInstancedDataLayout(typeof(T));

            if (instanceData.Length > Graphics.kMaxDrawMeshInstanceCount)
                throw new InvalidOperationException(String.Format("Instance count cannot exceed {0}.", Graphics.kMaxDrawMeshInstanceCount));

            return AddMeshInstances(config, (IntPtr)(T*)instanceData.GetUnsafeReadOnlyPtr(), layout, (uint)instanceData.Length, id);
        }

        public void RemoveInstance(Renderer targetRenderer)
        {
            RemoveInstance_Renderer(targetRenderer);
        }

        public void RemoveInstance(int handle)
        {
            RemoveInstance_InstanceID(handle);
        }

        public void UpdateInstanceTransform(Renderer renderer)
        {
            UpdateInstanceTransform_Renderer(renderer);
        }
        public void UpdateInstanceTransform(int handle, Matrix4x4 matrix)
        {
            UpdateInstanceTransform_Handle(handle, matrix);
        }

        public void UpdateInstanceID(Renderer renderer, uint instanceID)
        {
            UpdateInstanceID_Renderer(renderer, instanceID);
        }

        public void UpdateInstanceID(int handle, uint instanceID)
        {
            UpdateInstanceID_Handle(handle, instanceID);
        }

        public void UpdateInstanceMask(Renderer renderer, uint mask)
        {
            UpdateInstanceMask_Renderer(renderer, mask);
        }

        public void UpdateInstanceMask(int handle, uint mask)
        {
            UpdateInstanceMask_Handle(handle, mask);
        }

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::Build", HasExplicitThis = true)]
        extern public void Build(Vector3 relativeOrigin);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddInstance", HasExplicitThis = true)]
        extern private int AddInstance_Procedural([NotNull] GraphicsBuffer aabbBuffer, uint aabbCount, bool dynamicData, Matrix4x4 matrix, [NotNull] Material material, bool opaqueMaterial, MaterialPropertyBlock properties, uint mask = 0xFF, uint id = 0xFFFFFFFF);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::RemoveInstance", HasExplicitThis = true)]
        extern private void RemoveInstance_Renderer([NotNull] Renderer targetRenderer);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::RemoveInstance", HasExplicitThis = true)]
        extern private void RemoveInstance_InstanceID(int instanceID);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceTransform", HasExplicitThis = true)]
        extern private void UpdateInstanceTransform_Renderer([NotNull] Renderer renderer);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceTransform", HasExplicitThis = true)]
        extern private void UpdateInstanceTransform_Handle(int handle, Matrix4x4 matrix);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceMask", HasExplicitThis = true)]
        extern private void UpdateInstanceMask_Renderer([NotNull] Renderer renderer, uint mask);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddVFXInstances", HasExplicitThis = true)]
        extern public void AddVFXInstances([NotNull] Renderer targetRenderer, uint[] vfxSystemMasks);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::RemoveVFXInstances", HasExplicitThis = true)]
        extern public void  RemoveVFXInstances([NotNull]Renderer targetRenderer);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceMask", HasExplicitThis = true)]
        extern private void UpdateInstanceMask_Handle(int handle, uint mask);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceID", HasExplicitThis = true)]
        extern private void UpdateInstanceID_Renderer([NotNull] Renderer renderer, uint id);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstanceID", HasExplicitThis = true)]
        extern private void UpdateInstanceID_Handle(int handle, uint id);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::UpdateInstancePropertyBlock", HasExplicitThis = true)]
        extern public void UpdateInstancePropertyBlock(int handle, MaterialPropertyBlock properties);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::GetSize", HasExplicitThis = true)]
        extern public UInt64 GetSize();

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::GetInstanceCount", HasExplicitThis = true)]
        extern public UInt32 GetInstanceCount();

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::ClearInstances", HasExplicitThis = true)]
        extern public void ClearInstances();

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddInstanceSubMeshFlagsArray", HasExplicitThis = true)]
        extern private int AddInstanceSubMeshFlagsArray([NotNull] Renderer targetRenderer, RayTracingSubMeshFlags[] subMeshFlags, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF);

        [FreeFunction("RayTracingAccelerationStructure_Bindings::AddMeshInstance", HasExplicitThis = true, ThrowsException = true)]
        extern unsafe private int AddMeshInstance(RayTracingMeshInstanceConfig config, Matrix4x4 matrix, Matrix4x4* prevMatrix, uint id = 0xFFFFFFFF);
		
        [FreeFunction("RayTracingAccelerationStructure_Bindings::AddMeshInstances", HasExplicitThis = true, ThrowsException = true)]
		extern unsafe private int AddMeshInstances(RayTracingMeshInstanceConfig config, IntPtr instancedData, RenderInstancedDataLayout layout, uint instanceCount, uint id = 0xFFFFFFFF);

        public RayTracingInstanceCullingResults CullInstances(ref RayTracingInstanceCullingConfig cullingConfig) => Internal_CullInstances(in cullingConfig);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::CullInstances", HasExplicitThis = true)]
        extern private RayTracingInstanceCullingResults Internal_CullInstances(in RayTracingInstanceCullingConfig cullingConfig);


        // Obsolete methods. To be removed in the future.
        const string obsoleteBuildMsg1 = "Method Update is deprecated. Use Build instead (UnityUpgradable) -> Build()";
        [Obsolete(obsoleteBuildMsg1, true)]
        public void Update() => new NotSupportedException(obsoleteBuildMsg1);

        const string obsoleteBuildMsg2 = "Method Update is deprecated. Use Build instead (UnityUpgradable) -> Build(*)";
        [Obsolete(obsoleteBuildMsg2, true)]
        public void Update(Vector3 relativeOrigin) => new NotSupportedException(obsoleteBuildMsg2);

        const string obsoleteRendererMsg = "This AddInstance method is deprecated and will be removed in a future version. Please use the alternate AddInstance method for adding Renderers to the acceleration structure.";
        [Obsolete(obsoleteRendererMsg, true)]
        public void AddInstance(Renderer targetRenderer, bool[] subMeshMask = null, bool[] subMeshTransparencyFlags = null, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF) => new NotSupportedException(obsoleteRendererMsg);

        const string obsoleteAABBMsg = "This AddInstance method is deprecated and will be removed in a future version. Please use the alternate AddInstance method for adding procedural geometry (AABBs) to the acceleration structure.";
        [Obsolete(obsoleteAABBMsg, true)]
        public void AddInstance(GraphicsBuffer aabbBuffer, uint numElements, Material material, bool isCutOff, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, bool reuseBounds = false, uint id = 0xFFFFFFFF) => new NotSupportedException(obsoleteAABBMsg);

        [Obsolete(obsoleteAABBMsg, true)]
        public void AddInstance(GraphicsBuffer aabbBuffer, uint numElements, Material material, Matrix4x4 instanceTransform, bool isCutOff, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, bool reuseBounds = false, uint id = 0xFFFFFFFF) => new NotSupportedException(obsoleteAABBMsg);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(RayTracingAccelerationStructure rayTracingAccelerationStructure) => rayTracingAccelerationStructure.m_Ptr;
        }
    }
}
