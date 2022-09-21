// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/RayTracingAccelerationStructure.h")]
    [NativeHeader("Runtime/Export/Graphics/RayTracingAccelerationStructure.bindings.h")]

    [Flags]
    public enum RayTracingSubMeshFlags
    {
        Disabled            = 0,
        Enabled             = (1 << 0),
        ClosestHitOnly      = (1 << 1),
        UniqueAnyHitCalls   = (1 << 2),
    }

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

    public struct RayTracingInstanceCullingShaderTagConfig
    {
        public ShaderTagId tagId;
        public ShaderTagId tagValueId;
    }
    public struct RayTracingInstanceMaterialConfig
    {
        public int renderQueueLowerBound;
        public int renderQueueUpperBound;

        public RayTracingInstanceCullingShaderTagConfig[] optionalShaderTags;

        public string[] optionalShaderKeywords;
    }

    public struct RayTracingInstanceCullingMaterialTest
    {
        public string[] deniedShaderPasses;

        public RayTracingInstanceCullingShaderTagConfig[] requiredShaderTags;
    }

    public struct RayTracingInstanceTriangleCullingConfig
    {
        public string[] optionalDoubleSidedShaderKeywords;

        public bool frontTriangleCounterClockwise;

        public bool checkDoubleSidedGIMaterial;

        public bool forceDoubleSided;
    };

    public struct RayTracingSubMeshFlagsConfig
    {
        public RayTracingSubMeshFlags opaqueMaterials;
        public RayTracingSubMeshFlags transparentMaterials;
        public RayTracingSubMeshFlags alphaTestedMaterials;
    };

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
    public struct RayTracingInstanceMaterialCRC
    {
        public int instanceID;
        public int crc;
    }

    public struct RayTracingInstanceCullingResults
    {
        public RayTracingInstanceMaterialCRC[] materialsCRC;
        public bool transformsChanged;
    }

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
        public RayTracingAccelerationStructure(RASSettings settings)
        {
            m_Ptr = Create(settings);
        }

        public RayTracingAccelerationStructure()
        {
            RASSettings settings = new RASSettings();
            settings.rayTracingModeMask = RayTracingModeMask.Everything;
            settings.managementMode = ManagementMode.Manual;
            settings.layerMask = -1;
            m_Ptr = Create(settings);
        }

        [FreeFunction("RayTracingAccelerationStructure_Bindings::Create")]
        extern private static IntPtr Create(RASSettings desc);

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

        public void AddInstance(Renderer targetRenderer, RayTracingSubMeshFlags[] subMeshFlags, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF)
        {
            AddInstanceSubMeshFlagsArray(targetRenderer, subMeshFlags, enableTriangleCulling, frontTriangleCounterClockwise, mask, id);
        }

        public int AddInstance(GraphicsBuffer aabbBuffer, uint aabbCount, bool dynamicData, Matrix4x4 matrix, Material material, bool opaqueMaterial, MaterialPropertyBlock properties, uint mask = 0xFF, uint id = 0xFFFFFFFF)
        {
            return AddInstance_Procedural(aabbBuffer, aabbCount, dynamicData, matrix, material, opaqueMaterial, properties, mask, id);
        }

        unsafe public int AddInstance(in RayTracingMeshInstanceConfig config, Matrix4x4 matrix, [DefaultValue("null")] Matrix4x4? prevMatrix = null, uint id = 0xFFFFFFFF)
        {
            if (prevMatrix.HasValue)
            {
                Matrix4x4 temp = prevMatrix.Value;
                return AddMeshInstance(config, matrix, &temp, id);
            }
            else
                return AddMeshInstance(config, matrix, null, id);
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

        [Obsolete("Method Update has been deprecated. Use Build instead (UnityUpgradable) -> Build()", true)]
        public void Update()
        {
            Build(Vector3.zero);
        }

        [Obsolete("Method Update has been deprecated. Use Build instead (UnityUpgradable) -> Build(*)", true)]
        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::Update", HasExplicitThis = true)]
        extern public void Update(Vector3 relativeOrigin);

        [Obsolete("This AddInstance method has been deprecated and will be removed in a future version. Please use the alternate method for adding Renderers to the acceleration structure.", false)]
        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddInstanceDeprecated", HasExplicitThis = true)]
        extern public void AddInstance([NotNull] Renderer targetRenderer, bool[] subMeshMask = null, bool[] subMeshTransparencyFlags = null, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF);

        [Obsolete("This AddInstance method has been deprecated and will be removed in a future version. Please use the alternate method for adding procedural geometry (AABBs) to the acceleration structure.", false)]
        public void AddInstance(GraphicsBuffer aabbBuffer, uint numElements, Material material, bool isCutOff, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, bool reuseBounds = false, uint id = 0xFFFFFFFF)
        {
            AddInstance_Procedural_Deprecated(aabbBuffer, numElements, material, Matrix4x4.identity, isCutOff, enableTriangleCulling, frontTriangleCounterClockwise, mask, reuseBounds, id);
        }

        [Obsolete("This AddInstance method has been deprecated and will be removed in a future version. Please use the alternate method for adding procedural geometry (AABBs) to the acceleration structure.", false)]
        public void AddInstance(GraphicsBuffer aabbBuffer, uint numElements, Material material, Matrix4x4 instanceTransform, bool isCutOff, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, bool reuseBounds = false, uint id = 0xFFFFFFFF)
        {
            AddInstance_Procedural_Deprecated(aabbBuffer, numElements, material, instanceTransform, isCutOff, enableTriangleCulling, frontTriangleCounterClockwise, mask, reuseBounds, id);
        }

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::Build", HasExplicitThis = true)]
        extern public void Build(Vector3 relativeOrigin);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddInstanceDeprecated", HasExplicitThis = true)]
        extern private void AddInstance_Procedural_Deprecated([NotNull] GraphicsBuffer aabbBuffer, uint numElements, [NotNull] Material material, Matrix4x4 instanceTransform, bool isCutOff, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, bool reuseBounds = false, uint id = 0xFFFFFFFF);

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

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::CullInstances", HasExplicitThis = true)]
        extern public RayTracingInstanceCullingResults CullInstances(ref RayTracingInstanceCullingConfig cullingConfig);

        [FreeFunction(Name = "RayTracingAccelerationStructure_Bindings::AddInstanceSubMeshFlagsArray", HasExplicitThis = true)]
        extern private void AddInstanceSubMeshFlagsArray([NotNull] Renderer targetRenderer, RayTracingSubMeshFlags[] subMeshFlags, bool enableTriangleCulling = true, bool frontTriangleCounterClockwise = false, uint mask = 0xFF, uint id = 0xFFFFFFFF);

        [FreeFunction("RayTracingAccelerationStructure_Bindings::AddMeshInstance", HasExplicitThis = true, ThrowsException = true)]
        extern unsafe private int AddMeshInstance(RayTracingMeshInstanceConfig config, Matrix4x4 matrix, Matrix4x4* prevMatrix, uint id = 0xFFFFFFFF);
    }
}
