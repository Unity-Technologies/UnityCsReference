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
        [NativeName("m_VisibleTexelCount")]                public int     visibleTexelCount;

        [NativeName("m_ConvergedDirectTexelCount")]        public int     convergedDirectTexelCount;
        [NativeName("m_ConvergedGITexelCount")]            public int     convergedGITexelCount;
        [NativeName("m_OccupiedTexelCount")]               public int     occupiedTexelCount;

        [NativeName("m_MinDirectSamples")]                 public int     minDirectSamples;
        [NativeName("m_MinGISamples")]                     public int     minGISamples;
        [NativeName("m_MaxDirectSamples")]                 public int     maxDirectSamples;
        [NativeName("m_MaxGISamples")]                     public int     maxGISamples;
        [NativeName("m_AvgDirectSamples")]                 public int     avgDirectSamples;
        [NativeName("m_AvgGISamples")]                     public int     avgGISamples;

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

    [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
    internal struct LightingStats
    {
        public uint realtimeLightsCount;
        public uint dynamicMeshesCount;
        public uint mixedLightsCount;
        public uint bakedLightsCount;
        public uint staticMeshesCount;
        public uint staticMeshesRealtimeEmissive;
        public uint staticMeshesBakedEmissive;
        public uint lightProbeGroupsCount;
        public uint reflectionProbesCount;

        internal void Reset()
        {
            realtimeLightsCount = 0;
            dynamicMeshesCount = 0;
            mixedLightsCount = 0;
            bakedLightsCount = 0;
            staticMeshesCount = 0;
            staticMeshesRealtimeEmissive = 0;
            staticMeshesBakedEmissive = 0;
            lightProbeGroupsCount = 0;
            reflectionProbesCount = 0;
        }
    }

    [NativeHeader("Editor/Src/GI/Enlighten/Visualisers/VisualisationManager.h")]
    internal enum GITextureAvailability
    {
        GITextureUnknown = 0,
        GITextureNotAvailable = 1,
        GITextureLoading = 2,
        GITextureAvailable = 3,
        GITextureAvailabilityCount = 4
    }

    [NativeHeader("Editor/Src/GI/Enlighten/Visualisers/VisualisationManager.h")]
    internal struct VisualisationGITexture
    {
        [NativeName("m_Type")]
        public GITextureType type;
        [NativeName("m_Availability")]
        public GITextureAvailability textureAvailability;
        [NativeName("m_Texture")]
        public Texture2D texture;
        [NativeName("m_Hash")]
        public Hash128 hash;
        [NativeName("m_ContentsHash")]
        public Hash128 contentHash;
    }

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

        // How is GI data created.
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

        public delegate void OnStartedFunction();
        public delegate void OnCompletedFunction();
        internal delegate void OnWroteLightingDataAsset();

        // How is GI data created: iteratively or on demand by Enlighten
        [StaticAccessor("GetLightmapSettings()")]
        [NativeName("GIWorkflowMode")]
        public static extern GIWorkflowMode giWorkflowMode { get; set; }

        [StaticAccessor("GetGISettings()")]
        [NativeName("EnableRealtimeLightmaps")]
        public static extern bool realtimeGI { get; set; }

        [StaticAccessor("GetGISettings()")]
        [NativeName("EnableBakedLightmaps")]
        public static extern bool bakedGI { get; set; }

        [StaticAccessor("GetGISettings()")]
        public static extern float indirectOutputScale { get; set; }

        [StaticAccessor("GetGISettings()")]
        [NativeName("AlbedoBoost")]
        public static extern float bounceBoost { get; set; }

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

        [StaticAccessor("GetLightmapEditorSettings()")]
        [NativeName("ForceWhiteAlbedo")]
        internal static extern bool enlightenForceWhiteAlbedo { get; set; }

        [StaticAccessor("GetLightmapEditorSettings()")]
        [NativeName("ForceUpdates")]
        internal static extern bool enlightenForceUpdates { get; set; }

        [StaticAccessor("GetLightmapEditorSettings()")]
        internal static extern FilterMode filterMode { get; set; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern bool isProgressiveLightmapperDone {[NativeName("IsDone")] get; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern ulong occupiedTexelCount { get; }

        [StaticAccessor("ProgressiveRuntimeManager::Get()", StaticAccessorType.Arrow)]
        internal static extern ulong GetVisibleTexelCount(int lightmapIndex);

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

        public static event OnStartedFunction started;

        private static void Internal_CallStartedFunctions()
        {
            if (started != null)
                started();
        }

        internal static event OnWroteLightingDataAsset wroteLightingDataAsset;

        internal static void Internal_CallOnWroteLightingDataAsset()
        {
            if (wroteLightingDataAsset != null)
                wroteLightingDataAsset();
        }

        public static OnCompletedFunction completed;

        private static void Internal_CallCompletedFunctions()
        {
            if (completed != null)
                completed();
        }

        // Returns the progress of a build when the bake job is running, returns 0 when no bake job is running.
        public static extern float buildProgress {[FreeFunction] get; }

        // Deletes all lightmap assets and makes all lights behave as if they weren't baked yet.
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
            Lightmapping.OnCompletedFunction OnBakeFinish = null;
            OnBakeFinish = () =>
            {
                EditorSceneManager.SaveOpenScenes();
                if (sceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                Lightmapping.completed -= OnBakeFinish;
            };

            // Call BakeAsync when all scenes are loaded and attach cleanup delegate
            EditorSceneManager.SceneOpenedCallback BakeOnAllOpen = null;
            BakeOnAllOpen = (UnityEngine.SceneManagement.Scene scene, SceneManagement.OpenSceneMode loadSceneMode) =>
            {
                if (EditorSceneManager.loadedSceneCount == paths.Length)
                {
                    BakeAsync();
                    Lightmapping.completed += OnBakeFinish;
                    EditorSceneManager.sceneOpened -= BakeOnAllOpen;
                }
            };

            EditorSceneManager.sceneOpened += BakeOnAllOpen;

            EditorSceneManager.OpenScene(paths[0]);
            for (int i = 1; i < paths.Length; i++)
                EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
        }
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
    }
}
