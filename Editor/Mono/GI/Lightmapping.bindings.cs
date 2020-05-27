// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using Scene = UnityEngine.SceneManagement.Scene;
using NativeArrayUnsafeUtility = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;
using Unity.Collections;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [UsedByNativeCode]
    [NativeHeader("Editor/Src/GI/Progressive/PVRData.h")]
    internal struct LightmapConvergence
    {
        public bool       IsConverged() { return convergedDirectTexelCount == occupiedTexelCount && convergedGITexelCount == occupiedTexelCount; }
        public bool       IsValid() { return -1 != visibleConvergedDirectTexelCount; }

        [NativeName("m_CullingHash")]                      public Hash128 cullingHash;
        [NativeName("m_VisibleConvergedDirectTexelCount")] public int     visibleConvergedDirectTexelCount;
        [NativeName("m_VisibleConvergedGITexelCount")]     public int     visibleConvergedGITexelCount;
        [NativeName("m_VisibleConvergedEnvTexelCount")]    public int     visibleConvergedEnvTexelCount;
        [NativeName("m_VisibleTexelCount")]                public int     visibleTexelCount;

        [NativeName("m_ConvergedDirectTexelCount")]        public int     convergedDirectTexelCount;
        [NativeName("m_ConvergedGITexelCount")]            public int     convergedGITexelCount;
        [NativeName("m_ConvergedEnvTexelCount")]           public int     convergedEnvTexelCount;
        [NativeName("m_OccupiedTexelCount")]               public int     occupiedTexelCount;

        [NativeName("m_MinDirectSamples")]                 public int     minDirectSamples;
        [NativeName("m_MinGISamples")]                     public int     minGISamples;
        [NativeName("m_MinEnvSamples")]                    public int     minEnvSamples;
        [NativeName("m_MaxDirectSamples")]                 public int     maxDirectSamples;
        [NativeName("m_MaxGISamples")]                     public int     maxGISamples;
        [NativeName("m_MaxEnvSamples")]                    public int     maxEnvSamples;
        [NativeName("m_AvgDirectSamples")]                 public int     avgDirectSamples;
        [NativeName("m_AvgGISamples")]                     public int     avgGISamples;
        [NativeName("m_AvgEnvSamples")]                    public int     avgEnvSamples;

        [NativeName("m_ForceStop")]                        public bool     avgGIForceStop;

        [NativeName("m_Progress")]                         public float   progress;
    }

    [UsedByNativeCode]
    [NativeHeader("Editor/Src/GI/Progressive/PVRData.h")]
    internal struct LightProbesConvergence
    {
        public bool IsConverged() { return probeSetCount == convergedProbeSetCount; }
        public bool IsValid() { return -1 != probeSetCount; }

        [NativeName("m_ProbeSetCount")]             public int  probeSetCount;
        [NativeName("m_ConvergedProbeSetCount")]    public int  convergedProbeSetCount;
    }

    [UsedByNativeCode]
    [NativeHeader("Editor/Src/GI/Progressive/PVRData.h")]
    internal struct LightmapMemory
    {
        [NativeName("m_LightmapDataSizeCPU")]   public float lightmapDataSizeCPU;
        [NativeName("m_LightmapTexturesSize")]  public float lightmapTexturesSize;
        [NativeName("m_LightmapDataSizeGPU")]   public float lightmapDataSizeGPU;
    }

    internal struct MemLabels
    {
        public string[] labels;
        public float[] sizes;
    }

    internal struct GeoMemLabels
    {
        public string[] labels;
        public float[] sizes;
        public UInt64[] triCounts;
    }

    internal struct TetrahedralizationData
    {
        public int[] indices;
        public Vector3[] positions;
    }

    [NativeHeader("Editor/Src/GI/Progressive/BakeContextManager.h")]
    internal struct EnvironmentSamplesData
    {
        public Vector4[] directions;
        public Vector4[] intensities;
    }

    [NativeHeader("Editor/Src/GI/Progressive/PVRHelpers.h")]
    internal struct DeviceAndPlatform
    {
        public int deviceId;
        public int platformId;
        public string name;
    };

    [NativeHeader("Editor/Mono/GI/Lightmapping.bindings.h")]
    public static partial class Lightmapping
    {
        [NativeHeader("Editor/Src/JobManager/QueueJobTypes.h")]
        internal enum ConcurrentJobsType
        {
            Min = 0,
            Low = 1,
            High = 2,
        }

        [NativeHeader("Runtime/Graphics/LightmapEnums.h")]
        public enum GIWorkflowMode
        {
            // Data is automatically precomputed for dynamic and static GI.
            Iterative = 0,

            // Data is only precomputed for dynamic and static GI when the bake button is pressed.
            OnDemand = 1,

            // Lightmaps are calculated in the same way as in Unity 4.x.
            Legacy = 2
        }

        // Obsolete, please use Actions instead
        public delegate void OnStartedFunction();
        public delegate void OnCompletedFunction();

//        [Obsolete("Lightmapping.giWorkflowMode is obsolete, use Lightmapping.lightingSettings.autoGenerate instead. ", false)]
        public static GIWorkflowMode giWorkflowMode
        {
            get { return GetLightingSettingsOrDefaultsFallback().autoGenerate ? GIWorkflowMode.Iterative : GIWorkflowMode.OnDemand; }
            set { GetOrCreateLightingsSettings().autoGenerate = (value == GIWorkflowMode.Iterative); }
        }

//        [Obsolete("Lightmapping.realtimeGI is obsolete, use Lightmapping.lightingSettings.realtimeGI instead. ", false)]
        public static bool realtimeGI
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeGI; }
            set { GetOrCreateLightingsSettings().realtimeGI = value; }
        }

//        [Obsolete("Lightmapping.bakedGI is obsolete, use Lightmapping.lightingSettings.bakedGI instead. ", false)]
        public static bool bakedGI
        {
            get { return GetLightingSettingsOrDefaultsFallback().bakedGI; }
            set { GetOrCreateLightingsSettings().bakedGI = value; }
        }

        [Obsolete("Lightmapping.indirectOutputScale is obsolete, use Lightmapping.lightingSettings.indirectScale instead. ", false)]
        public static float indirectOutputScale
        {
            get { return GetLightingSettingsOrDefaultsFallback().indirectScale; }
            set { GetOrCreateLightingsSettings().indirectScale = value; }
        }

        [Obsolete("Lightmapping.bounceBoost is obsolete, use Lightmapping.lightingSettings.albedoBoost instead. ", false)]
        public static float bounceBoost
        {
            get { return GetLightingSettingsOrDefaultsFallback().albedoBoost; }
            set { GetOrCreateLightingsSettings().albedoBoost = value; }
        }

        // Set concurrent jobs type. Warning, high priority can impact Editor performance
        [StaticAccessor("EnlightenPrecompManager::Get()", StaticAccessorType.Arrow)]
        internal static extern ConcurrentJobsType concurrentJobsType { get; set; }

        // Clears disk cache and recreates cache directories.
        [StaticAccessor("GICache", StaticAccessorType.DoubleColon)]
        public static extern void ClearDiskCache();

        // Updates cache path from Editor preferences.
        [StaticAccessor("GICache", StaticAccessorType.DoubleColon)]
        internal static extern void UpdateCachePath();

        // Get the disk cache size in Mb.
        [StaticAccessor("GICache", StaticAccessorType.DoubleColon)]
        [NativeName("LastKnownCacheSize")]
        internal static extern long diskCacheSize { get; }

        // Get the disk cache path.
        [StaticAccessor("GICache", StaticAccessorType.DoubleColon)]
        [NativeName("CachePath")]
        internal static extern string diskCachePath { get; }

        [Obsolete("Lightmapping.enlightenForceWhiteAlbedo is obsolete, use Lightmapping.lightingSettings.realtimeForceWhiteAlbedo instead. ", false)]
        internal static bool enlightenForceWhiteAlbedo
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeForceWhiteAlbedo; }
            set { GetOrCreateLightingsSettings().realtimeForceWhiteAlbedo = value; }
        }

        [Obsolete("Lightmapping.enlightenForceUpdates is obsolete, use Lightmapping.lightingSettings.realtimeForceUpdates instead. ", false)]
        internal static bool enlightenForceUpdates
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeForceUpdates; }
            set { GetOrCreateLightingsSettings().realtimeForceUpdates = value; }
        }

        [Obsolete("Lightmapping.filterMode is obsolete, use Lightmapping.lightingSettings.lightmapFilterMode instead. ", false)]
        internal static FilterMode filterMode
        {
            get { return GetLightingSettingsOrDefaultsFallback().lightmapFilterMode; }
            set { GetOrCreateLightingsSettings().lightmapFilterMode = value; }
        }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern bool isProgressiveLightmapperDone {[NativeName("IsDone")] get; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern ulong occupiedTexelCount { get; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern ulong GetVisibleTexelCount(int lightmapIndex);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern int atlasCount { [NativeName("GetAtlasCount")] get; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern LightmapConvergence GetLightmapConvergence(int lightmapIndex);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern LightProbesConvergence GetLightProbesConvergence();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern LightmapMemory GetLightmapMemory(int lightmapIndex);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern bool GetGBufferHash(int lightmapIndex, out Hash128 gbufferHash);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float GetGBufferMemory(ref Hash128 gbufferHash);

        [FreeFunction]
        internal static extern MemLabels GetLightProbeMemLabels();

        [FreeFunction]
        internal static extern MemLabels GetTransmissionTexturesMemLabels();

        [FreeFunction]
        internal static extern MemLabels GetMaterialTexturesMemLabels();

        [FreeFunction]
        internal static extern MemLabels GetNotShownMemLabels();

        [StaticAccessor("PVRMemoryLabelTracker::Get()", StaticAccessorType.Arrow)]
        internal static extern void ResetExplicitlyShownMemLabels();

        [StaticAccessor("PVROpenRLMemoryTracker::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetGeometryMemory")]
        internal static extern GeoMemLabels GetGeometryMemory();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float ComputeTotalCPUMemoryUsageInBytes();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float ComputeTotalGPUMemoryUsageInBytes();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern void LogGPUMemoryStatistics();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float GetLightmapBakeTimeRaw();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float GetLightmapBakeTimeTotal();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetLightmapBakePerformance")]
        internal static extern float GetLightmapBakePerformanceTotal();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float GetLightmapBakePerformance(int lightmapIndex);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern string GetLightmapBakeGPUDeviceName();

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        extern internal static DeviceAndPlatform[] GetLightmappingGpuDevices();

        // Exports the current state of the scene to the dynamic GI workflow.
        [FreeFunction]
        internal static extern void PrintStateToConsole();

        // Starts an asynchronous bake job.
        [FreeFunction]
        public static extern bool BakeAsync();

        // Stars a synchronous bake job.
        [FreeFunction]
        public static extern bool Bake();

        // Cancels the currently running asynchronous bake job.
        [FreeFunction("CancelLightmapping")]
        public static extern void Cancel();

        // Stops the current bake at the state it has reached so far.
        [FreeFunction]
        public static extern void ForceStop();

        // Returns true when the bake job is running, false otherwise (RO).
        public static extern bool isRunning {[FreeFunction("IsRunningLightmapping")] get; }

        [System.Obsolete("OnStartedFunction.started is obsolete, please use bakeStarted instead. ", false)]
        public static event OnStartedFunction started;

        public static event Action bakeStarted;

        private static void Internal_CallBakeStartedFunctions()
        {
            if (bakeStarted != null)
                bakeStarted();

#pragma warning disable 0618
            if (started != null)
                started();
#pragma warning restore 0618
        }

        internal static event Action startedRendering;

        internal static void Internal_CallStartedRenderingFunctions()
        {
            if (startedRendering != null)
                startedRendering();
        }

        public static event Action lightingDataUpdated;

        internal static void Internal_CallLightingDataUpdatedFunctions()
        {
            if (lightingDataUpdated != null)
                lightingDataUpdated();
        }

        public static event Action lightingDataCleared;

        internal static void Internal_CallLightingDataCleared()
        {
            if (lightingDataCleared != null)
                lightingDataCleared();
        }

        public static event Action lightingDataAssetCleared;

        internal static void Internal_CallLightingDataAssetCleared()
        {
            if (lightingDataAssetCleared != null)
                lightingDataAssetCleared();
        }

        internal static event Action wroteLightingDataAsset;

        internal static void Internal_CallOnWroteLightingDataAsset()
        {
            if (wroteLightingDataAsset != null)
                wroteLightingDataAsset();
        }

        [System.Obsolete("OnCompletedFunction.completed is obsolete, please use event bakeCompleted instead. ", false)]
        public static OnCompletedFunction completed;

        public static event Action bakeCompleted;

        private static void Internal_CallBakeCompletedFunctions()
        {
            if (bakeCompleted != null)
                bakeCompleted();

#pragma warning disable 0618
            if (completed != null)
                completed();
#pragma warning restore 0618
        }

        // Returns the progress of a build when the bake job is running, returns 0 when no bake job is running.
        public static extern float buildProgress {[FreeFunction] get; }

        // Deletes all runtime lighting data for the current scene.
        [FreeFunction]
        public static extern void Clear();

        // Deletes the lighting data asset for the current scene.
        [FreeFunction]
        public static extern void ClearLightingDataAsset();

        // Calculates a Delaunay Tetrahedralization of the 'positions' point set - the same way the lightmapper
        public static void Tetrahedralize(Vector3[] positions, out int[] outIndices, out Vector3[] outPositions)
        {
            TetrahedralizationData data = TetrahedralizeInternal(positions);
            outIndices = data.indices;
            outPositions = data.positions;
        }

        [NativeName("LightProbeUtils::Tetrahedralize")]
        [FreeFunction]
        private static extern TetrahedralizationData TetrahedralizeInternal(Vector3[] positions);

        internal static void GetEnvironmentSamples(out Vector4[] outDirections, out Vector4[] outIntensities)
        {
            EnvironmentSamplesData data = GetEnvironmentSamplesInternal();
            outDirections = data.directions;
            outIntensities = data.intensities;
        }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetEnvironmentSamples")]
        private static extern EnvironmentSamplesData GetEnvironmentSamplesInternal();

        [FreeFunction]
        public static extern bool BakeReflectionProbe(ReflectionProbe probe, string path);

        // Used to quickly update baked reflection probes without GI computations.
        [FreeFunction]
        internal static extern bool BakeReflectionProbeSnapshot(ReflectionProbe probe);

        // Used to quickly update all baked reflection probes without GI computations.
        [FreeFunction]
        internal static extern bool BakeAllReflectionProbesSnapshots();

        // Called when the user changes the Lightmap Encoding option:
        // - reload shaders to set correct lightmap decoding keyword
        // - reimport lightmaps with the new encoding
        // - rebake reflection probes because the lightmaps may look different
        [FreeFunction]
        internal static extern void OnUpdateLightmapEncoding(BuildTargetGroup target);

        // Called when the user changes the Lightmap streaming settings:
        [FreeFunction]
        internal static extern void OnUpdateLightmapStreaming(BuildTargetGroup target);

        [FreeFunction]
        public static extern void GetTerrainGIChunks([NotNull] Terrain terrain, ref int numChunksX, ref int numChunksY);

        [StaticAccessor("GetLightmapSettings()")]
        public static extern LightingDataAsset lightingDataAsset { get; set; }

        public static bool TryGetLightingSettings(out LightingSettings settings)
        {
            settings = lightingSettingsInternal;

            return (settings != null);
        }

        public static LightingSettings lightingSettings
        {
            get
            {
                var settings = lightingSettingsInternal;

                if (settings == null)
                {
                    throw new Exception("Lightmapping.lightingSettings is null. Please assign it to an existing asset or a new instance. ");
                }

                return settings;
            }
            set
            {
                lightingSettingsInternal = value;
            }
        }

        [StaticAccessor("GetLightmapSettings()")]
        [NativeName("LightingSettings")]
        internal static extern LightingSettings lightingSettingsInternal { get; set; }

        [StaticAccessor("GetLightmapSettings()")]
        [NativeName("LightingSettingsDefaults_Scripting")]
        public static extern LightingSettings lightingSettingsDefaults { get; }

        // To be used by internal code when just reading settings, not settings them
        internal static LightingSettings GetLightingSettingsOrDefaultsFallback()
        {
            var lightingSettings = Lightmapping.lightingSettingsInternal;

            if (lightingSettings != null)
                return lightingSettings;

            return Lightmapping.lightingSettingsDefaults;
        }

        // used to make sure that the old APIs work. The user should not be required to manually create an asset, so we do it for them.
        internal static LightingSettings GetOrCreateLightingsSettings()
        {
            if (Lightmapping.lightingSettingsInternal == null)
            {
                Lightmapping.lightingSettingsInternal = new LightingSettings();
            }

            return Lightmapping.lightingSettingsInternal;
        }

        public static void BakeMultipleScenes(string[] paths)
        {
            if (paths.Length == 0)
                return;

            for (int i = 0; i < paths.Length; i++)
            {
                for (int j = i + 1; j < paths.Length; j++)
                {
                    if (paths[i] == paths[j])
                        throw new System.Exception("no duplication of scenes is allowed");
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            // Restore old scene setup once the bake finishes
            Action OnBakeFinish = null;
            OnBakeFinish = () =>
            {
                EditorSceneManager.SaveOpenScenes();
                if (sceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                Lightmapping.bakeCompleted -= OnBakeFinish;
            };

            // Call BakeAsync when all scenes are loaded and attach cleanup delegate
            EditorSceneManager.SceneOpenedCallback BakeOnAllOpen = null;
            BakeOnAllOpen = (UnityEngine.SceneManagement.Scene scene, SceneManagement.OpenSceneMode loadSceneMode) =>
            {
                if (EditorSceneManager.loadedSceneCount == paths.Length)
                {
                    BakeAsync();
                    Lightmapping.bakeCompleted += OnBakeFinish;
                    EditorSceneManager.sceneOpened -= BakeOnAllOpen;
                }
            };

            EditorSceneManager.sceneOpened += BakeOnAllOpen;

            EditorSceneManager.OpenScene(paths[0]);
            for (int i = 1; i < paths.Length; i++)
                EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
        }

        // Reset lightmapping settings
        [StaticAccessor("GetLightingSettings()")]
        extern internal static void Reset();

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool IsLightmappedOrDynamicLightmappedForRendering([NotNull] Renderer renderer);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool IsOptixDenoiserSupported();

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool IsRadeonDenoiserSupported();

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool IsOpenImageDenoiserSupported();

        // Packing for realtime GI may fail of the mesh has zero UV or surface area. This is the outcome for the given renderer.
        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasZeroAreaMesh(Renderer renderer);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasUVOverlaps(Renderer renderer);

        // Packing for realtime GI may clamp the output resolution. This is the outcome for the given renderer.
        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasClampedResolution(Renderer renderer);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetSystemResolution(Renderer renderer, out int width, out int height);

        [FreeFunction("GetSystemResolution")]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetTerrainSystemResolution(Terrain terrain, out int width, out int height, out int numChunksInX, out int numChunksInY);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetInstanceResolution(Renderer renderer, out int width, out int height);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetInputSystemHash(int instanceID, out Hash128 inputSystemHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetLightmapIndex(int instanceID, out int lightmapIndex);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal Hash128[] GetMainSystemHashes();

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetInstanceHash(Renderer renderer, out Hash128 instanceHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetPVRInstanceHash(int instanceID, out Hash128 instanceHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetPVRAtlasHash(int instanceID, out Hash128 atlasHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetPVRAtlasInstanceOffset(int instanceID, out int atlasInstanceOffset);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetGeometryHash(Renderer renderer, out Hash128 geometryHash);
    }
}

namespace UnityEditor.Experimental
{
    public sealed partial class Lightmapping
    {
        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        public static extern bool probesIgnoreDirectEnvironment { get; set; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        public static extern void SetCustomBakeInputs(Vector4[] inputData, int sampleCount);

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        public static extern bool GetCustomBakeResults([Out] Vector4[] results);

        [Obsolete("UnityEditor.Experimental.Lightmapping.extractAmbientOcclusion is obsolete, use Lightmapping.lightingSettings.extractAO instead. ", false)]
        public static bool extractAmbientOcclusion
        {
            get { return UnityEditor.Lightmapping.GetLightingSettingsOrDefaultsFallback().extractAO; }
            set { UnityEditor.Lightmapping.GetOrCreateLightingsSettings().extractAO = value; }
        }

        [NativeThrows]
        [FreeFunction]
        public static extern bool BakeAsync(Scene targetScene);

        [NativeThrows]
        [FreeFunction]
        public static extern bool Bake(Scene targetScene);

        public static event Action additionalBakedProbesCompleted;

        internal static void Internal_CallAdditionalBakedProbesCompleted()
        {
            if (additionalBakedProbesCompleted != null)
                additionalBakedProbesCompleted();
        }

        [FreeFunction]
        internal unsafe static extern bool GetAdditionalBakedProbes(int id, void* outBakedProbeSH, void* outBakedProbeValidity, void* outBakedProbeOctahedralDepth, int outBakedProbeCount);

        [Obsolete("Please use the new GetAdditionalBakedProbes with added octahedral depth map data.", false)]
        public unsafe static bool GetAdditionalBakedProbes(int id, NativeArray<SphericalHarmonicsL2> outBakedProbeSH, NativeArray<float> outBakedProbeValidity)
        {
            const int octahedralDepthMapTexelCount = 64; // 8*8
            var outBakedProbeOctahedralDepth = new NativeArray<float>(outBakedProbeSH.Length * octahedralDepthMapTexelCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            bool success = GetAdditionalBakedProbes(id, outBakedProbeSH, outBakedProbeValidity, outBakedProbeOctahedralDepth);
            outBakedProbeOctahedralDepth.Dispose();
            return success;
        }

        public unsafe static bool GetAdditionalBakedProbes(int id, NativeArray<SphericalHarmonicsL2> outBakedProbeSH, NativeArray<float> outBakedProbeValidity, NativeArray<float> outBakedProbeOctahedralDepth)
        {
            if (outBakedProbeSH == null || !outBakedProbeSH.IsCreated ||
                outBakedProbeValidity == null || !outBakedProbeValidity.IsCreated ||
                outBakedProbeOctahedralDepth == null || !outBakedProbeOctahedralDepth.IsCreated)
            {
                Debug.LogError("Output arrays need to be properly initialized.");
                return false;
            }

            const int octahedralDepthMapTexelCount = 64; // 8*8

            int numEntries = outBakedProbeSH.Length;

            if (outBakedProbeOctahedralDepth.Length != numEntries * octahedralDepthMapTexelCount)
            {
                Debug.LogError("Octahedral array must provide " + numEntries * octahedralDepthMapTexelCount + " floats.");
                return false;
            }

            if (outBakedProbeValidity.Length != numEntries)
            {
                Debug.LogError("All output arrays must have equal size.");
                return false;
            }

            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(outBakedProbeSH);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(outBakedProbeValidity);
            void* octahedralDepthPtr = NativeArrayUnsafeUtility.GetUnsafePtr(outBakedProbeOctahedralDepth);

            return GetAdditionalBakedProbes(id, shPtr, validityPtr, octahedralDepthPtr, outBakedProbeSH.Length);
        }

        [FreeFunction]
        public static extern void SetAdditionalBakedProbes(int id, Vector3[] positions);
    }
}
