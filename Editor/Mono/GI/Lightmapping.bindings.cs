// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using Scene = UnityEngine.SceneManagement.Scene;
using NativeArrayUnsafeUtility = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;
using Unity.Collections;
using UnityEditor.LightBaking;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [UsedByNativeCode]
    [NativeHeader("Editor/Src/GI/Progressive/PVRData.h")]
    internal struct LightmapSize
    {
        [NativeName("m_Width")]  public int width;
        [NativeName("m_Height")] public int height;
    }

    [UsedByNativeCode]
    [NativeHeader("Editor/Src/GI/Progressive/PVRData.h")]
    internal struct RunningBakeInfo
    {
        [NativeName("m_LightmapSizes")]  public LightmapSize[] lightmapSizes;
        [NativeName("m_ProbePositions")] public UInt64 probePositions;
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

        [NativeHeader("Runtime/Graphics/LightmapSettings.h")]
        public enum BakeOnSceneLoadMode
        {
            Never = 0,
            IfMissingLightingData = 1,
        };

        // Obsolete, please use Actions instead
        public delegate void OnStartedFunction();
        public delegate void OnCompletedFunction();

        [Obsolete("Lightmapping.giWorkflowMode is obsolete.", false)]
        public static GIWorkflowMode giWorkflowMode
        {
            get => GIWorkflowMode.OnDemand;
            set { }
        }

//        [Obsolete("Lightmapping.realtimeGI is obsolete, use LightingSettings.realtimeGI instead. ", false)]
        public static bool realtimeGI
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeGI; }
            set { GetOrCreateLightingsSettings().realtimeGI = value; }
        }

//        [Obsolete("Lightmapping.bakedGI is obsolete, use LightingSettings.bakedGI instead. ", false)]
        public static bool bakedGI
        {
            get { return GetLightingSettingsOrDefaultsFallback().bakedGI; }
            set { GetOrCreateLightingsSettings().bakedGI = value; }
        }

        [Obsolete("Lightmapping.indirectOutputScale is obsolete, use LightingSettings.indirectScale instead. ", false)]
        public static float indirectOutputScale
        {
            get { return GetLightingSettingsOrDefaultsFallback().indirectScale; }
            set { GetOrCreateLightingsSettings().indirectScale = value; }
        }

        [Obsolete("Lightmapping.bounceBoost is obsolete, use LightingSettings.albedoBoost instead. ", false)]
        public static float bounceBoost
        {
            get { return GetLightingSettingsOrDefaultsFallback().albedoBoost; }
            set { GetOrCreateLightingsSettings().albedoBoost = value; }
        }

        [RequiredByNativeCode]
        internal static bool ShouldBakeInteractively()
        {
            return SceneView.NeedsInteractiveBaking();
        }

        internal static bool shouldBakeInteractively
        {
            get { return ShouldBakeInteractively(); }
        }

        [RequiredByNativeCode]
        internal static void KickSceneViewsOutOfInteractiveMode()
        {
            foreach (SceneView sv in SceneView.sceneViews)
                sv.debugDrawModesUseInteractiveLightBakingData = false;
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

        [Obsolete("Lightmapping.enlightenForceWhiteAlbedo is obsolete, use LightingSettings.realtimeForceWhiteAlbedo instead. ", false)]
        internal static bool enlightenForceWhiteAlbedo
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeForceWhiteAlbedo; }
            set { GetOrCreateLightingsSettings().realtimeForceWhiteAlbedo = value; }
        }

        [Obsolete("Lightmapping.enlightenForceUpdates is obsolete, use LightingSettings.realtimeForceUpdates instead. ", false)]
        internal static bool enlightenForceUpdates
        {
            get { return GetLightingSettingsOrDefaultsFallback().realtimeForceUpdates; }
            set { GetOrCreateLightingsSettings().realtimeForceUpdates = value; }
        }

        [Obsolete("Lightmapping.filterMode is obsolete, use LightingSettings.lightmapFilterMode instead. ", false)]
        internal static FilterMode filterMode
        {
            get { return GetLightingSettingsOrDefaultsFallback().lightmapFilterMode; }
            set { GetOrCreateLightingsSettings().lightmapFilterMode = value; }
        }

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern bool isProgressiveLightmapperDone {[NativeName("IsBakedGIDone")] get; }

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern RunningBakeInfo GetRunningBakeInfo();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float ComputeTotalGPUMemoryUsageInBytes();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern void LogGPUMemoryStatistics();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern float GetLightmapBakeTimeTotal();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetLightmapBakePerformance")]
        internal static extern float GetLightmapBakePerformanceTotal();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern string GetLightmapBakeGPUDeviceName();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern int GetLightmapBakeGPUDeviceIndex();

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        internal static extern DeviceAndPlatform[] GetLightmappingGpuDevices();

        // Exports the current state of the scene to the dynamic GI workflow.
        [FreeFunction]
        internal static extern void PrintStateToConsole();

        // Starts an asynchronous bake job.
        [FreeFunction]
        internal static extern bool BakeAsyncImpl();

        // Starts a synchronous bake job.
        [FreeFunction]
        internal static extern bool BakeImpl();
                
        public static bool BakeAsync()
        {
            RenderPipelineManager.TryPrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
            return BakeAsyncImpl();
        }

        public static bool Bake()
        {
            RenderPipelineManager.TryPrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
            return BakeImpl();
        }
        
        // Cancels the currently running asynchronous bake job.
        [FreeFunction("CancelLightmapping")]
        public static extern void Cancel();

        // Stops the current bake at the state it has reached so far.
        [FreeFunction]
        [System.Obsolete("ForceStop is no longer available, use Cancel instead to stop a bake.", false)]
        public static extern void ForceStop();

        // Returns true when the bake job is running, false otherwise (RO).
        public static extern bool isRunning {[FreeFunction("IsRunningLightmapping")] get; }

        [System.Obsolete("OnStartedFunction.started is obsolete, please use bakeStarted instead. ", false)]
        public static event OnStartedFunction started;

        public static event Action bakeStarted;

        private static void OpenNestedSubScenes()
        {
            if (SceneHierarchyHooks.provideSubScenes == null)
                return;

            var subSceneInfos = SceneHierarchyHooks.provideSubScenes();
            if (subSceneInfos != null)
            {
                // Open all nested sub scenes.
                int subSceneCount = subSceneInfos.Length;
                foreach (var subSceneInfo in subSceneInfos)
                {
                    if (subSceneInfo.scene.isLoaded)
                        continue;

                    var path = AssetDatabase.GetAssetPath(subSceneInfo.sceneAsset);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    scene.isSubScene = true;
                }

                // Keep going deeper until sub scene count has stabilized.
                subSceneInfos = SceneHierarchyHooks.provideSubScenes();
                if (subSceneCount != subSceneInfos.Length)
                    OpenNestedSubScenes();
            }
        }

        [RequiredByNativeCode]
        private static void Internal_CallBakeStartedFunctions()
        {
            if (bakeStarted != null)
                bakeStarted();

            // Open all sub scenes before the bake so they can participate in the GI calculations.
            OpenNestedSubScenes();

#pragma warning disable 0618
            if (started != null)
                started();
#pragma warning restore 0618
        }

        internal static event Action startedRendering;

        [RequiredByNativeCode]
        internal static void Internal_CallStartedRenderingFunctions()
        {
            if (startedRendering != null)
                startedRendering();
        }

        public static event Action lightingDataUpdated;

        [RequiredByNativeCode]
        internal static void Internal_CallLightingDataUpdatedFunctions()
        {
            if (lightingDataUpdated != null)
                lightingDataUpdated();
        }

        public static event Action lightingDataCleared;

        [RequiredByNativeCode]
        internal static void Internal_CallLightingDataCleared()
        {
            if (lightingDataCleared != null)
                lightingDataCleared();
        }

        public static event Action lightingDataAssetCleared;

        [RequiredByNativeCode]
        internal static void Internal_CallLightingDataAssetCleared()
        {
            if (lightingDataAssetCleared != null)
                lightingDataAssetCleared();
        }

        internal static event Action wroteLightingDataAsset;

        [RequiredByNativeCode]
        internal static void Internal_CallOnWroteLightingDataAsset()
        {
            if (wroteLightingDataAsset != null)
                wroteLightingDataAsset();
        }

        // This event is fired when BakeInput has been populated, but before passing it to Bake().
        // Do not store and access BakeInput beyond the call-back.
        internal static event Action<LightBaker.BakeInput, LightBaker.LightmapRequests, LightBaker.LightProbeRequests, InputExtraction.SourceMap> createdBakeInput;

        internal static void Internal_CallOnCreatedBakeInput(IntPtr p_BakeInput, IntPtr p_LightmapRequests, IntPtr LightProbeRequests, IntPtr p_SourceMap)
        {
            if (createdBakeInput != null)
            {
                using var bakeInput = new LightBaker.BakeInput(p_BakeInput);
                using var lightmapRequests = new LightBaker.LightmapRequests(p_LightmapRequests);
                using var lightProbeRequests = new LightBaker.LightProbeRequests(LightProbeRequests);
                using var sourceMap = new InputExtraction.SourceMap(p_SourceMap);
                createdBakeInput(bakeInput, lightmapRequests, lightProbeRequests, sourceMap);
            }
        }

        [System.Obsolete("OnCompletedFunction.completed is obsolete, please use event bakeCompleted instead. ", false)]
        public static OnCompletedFunction completed;

        public static event Action bakeCompleted;

        [RequiredByNativeCode]
        private static void Internal_CallBakeCompletedFunctions()
        {
            if (bakeCompleted != null)
                bakeCompleted();

#pragma warning disable 0618
            if (completed != null)
                completed();
#pragma warning restore 0618
        }

        public static event Action bakeCancelled;

        private static void Internal_CallBakeCancelledFunctions()
        {
            if (bakeCancelled != null)
                bakeCancelled();
        }

        internal static event Action<string> bakeAnalytics;

        [RequiredByNativeCode]
        private static void Internal_CallBakeAnalyticsFunctions(string analytics)
        {
            if (bakeAnalytics != null)
                bakeAnalytics(analytics);
        }

        // Returns the progress of a build when the bake job is running, returns 0 when no bake job is running.
        public static extern float buildProgress {[FreeFunction] get; }

        // Deletes all stored runtime lighting data for the current scene, resets environment lighting to default values.
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

        // Called when the user changes the HDR Cubemap Encoding option,
        // will reimport HDR cubemaps with the new encoding.
        [FreeFunction]
        internal static extern void OnUpdateHDRCubemapEncoding(BuildTargetGroup target);

        // Called when the user changes the Lightmap streaming settings:
        [FreeFunction]
        internal static extern void OnUpdateLightmapStreaming(BuildTargetGroup target);

        [FreeFunction]
        public static extern void GetTerrainGIChunks([NotNull] Terrain terrain, ref int numChunksX, ref int numChunksY);

        [StaticAccessor("GetLightmapSettings()")]
        public static extern BakeOnSceneLoadMode bakeOnSceneLoad { get; set; }

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

        [StaticAccessor("GetLightmapSettingsManager()")]
        [NativeName("SetLightingSettingsForScene")]
        public static extern void SetLightingSettingsForScene(Scene scene, LightingSettings lightingSettings);

        [StaticAccessor("GetLightmapSettingsManager()")]
        [NativeName("SetLightingSettingsForScenes")]
        public static extern void SetLightingSettingsForScenes(Scene[] scenes, LightingSettings lightingSettings);

        [StaticAccessor("GetLightmapSettingsManager()")]
        [NativeName("GetLightingSettingsForScene")]
        public static extern LightingSettings GetLightingSettingsForScene(Scene scene);

        [FreeFunction]
        public static extern LightingDataAsset GetLightingDataAssetForScene(Scene scene);

        [FreeFunction(ThrowsException = true)]
        public static extern void SetLightingDataAssetForScene(Scene scene, LightingDataAsset lda);

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
                if (SceneManager.loadedSceneCount == paths.Length)
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
        extern static internal bool IsOpenImageDenoiserSupported();

        // Packing for realtime GI may fail of the mesh has zero UV or surface area. This is the outcome for the given renderer.
        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasZeroAreaMesh([NotNull] Renderer renderer);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasUVOverlaps([NotNull] Renderer renderer);

        // Packing for realtime GI may clamp the output resolution. This is the outcome for the given renderer.
        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool HasClampedResolution([NotNull] Renderer renderer);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetSystemResolution([NotNull] Renderer renderer, out int width, out int height);

        [FreeFunction("GetSystemResolution")]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetTerrainSystemResolution([NotNull] Terrain terrain, out int width, out int height, out int numChunksInX, out int numChunksInY);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetInstanceResolution([NotNull] Renderer renderer, out int width, out int height);

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
        extern static internal bool GetInstanceHash([NotNull] Renderer renderer, out Hash128 instanceHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/EditorHelpers.h")]
        extern static internal bool GetGeometryHash([NotNull] Renderer renderer, out Hash128 geometryHash);

        [FreeFunction]
        [NativeHeader("Editor/Src/GI/ExtractInstances.h")]
        extern static internal bool IsRendererValid([NotNull] Renderer renderer);
        
        public delegate void AdditionalBakeDelegate(ref float progress, ref bool done);

        [RequiredByNativeCode]
        public static void SetAdditionalBakeDelegate(AdditionalBakeDelegate del) { s_AdditionalBakeDelegate = del != null ? del : s_DefaultAdditionalBakeDelegate; }

        [RequiredByNativeCode]
        public static AdditionalBakeDelegate GetAdditionalBakeDelegate() { return s_AdditionalBakeDelegate; }
     
        [RequiredByNativeCode]
        public static void ResetAdditionalBakeDelegate() { s_AdditionalBakeDelegate = s_DefaultAdditionalBakeDelegate; }
     
        [RequiredByNativeCode]
        internal static void AdditionalBake(ref float progress, ref bool done)
        {
            s_AdditionalBakeDelegate(ref progress, ref done);
        }

        [RequiredByNativeCode]
        private static readonly AdditionalBakeDelegate s_DefaultAdditionalBakeDelegate = (ref float progress, ref bool done) =>
        {
            progress = 100.0f;
            done = true;
        };
        [RequiredByNativeCode]
        private static AdditionalBakeDelegate s_AdditionalBakeDelegate = s_DefaultAdditionalBakeDelegate;
    }
}

namespace UnityEditor.Experimental
{
    public sealed partial class Lightmapping
    {
        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        public static extern bool probesIgnoreDirectEnvironment { get; set; }

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        public static extern bool probesIgnoreIndirectEnvironment { get; set; }

        public static void SetCustomBakeInputs(Vector4[] inputData, int sampleCount)
        {
            SetCustomBakeInputs(inputData.AsSpan(), sampleCount);
        }
        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        public static extern void SetCustomBakeInputs(ReadOnlySpan<Vector4> inputData, int sampleCount);

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        private static extern unsafe bool GetCustomBakeResultsCopy(Span<Vector4> results);
        public static bool GetCustomBakeResults(Span<Vector4> results)
        {
            return GetCustomBakeResultsCopy(results);
        }
        public static bool GetCustomBakeResults([Out] Vector4[] results)
        {
            return GetCustomBakeResults(results.AsSpan());
        }

        [StaticAccessor("BakedGISceneManager::Get()", StaticAccessorType.Arrow)]
        public static extern ReadOnlySpan<Vector4> GetCustomBakeResultsNoCopy();

        [Obsolete("UnityEditor.Experimental.Lightmapping.extractAmbientOcclusion is obsolete, use LightingSettings.extractAO instead. ", false)]
        public static bool extractAmbientOcclusion
        {
            get { return UnityEditor.Lightmapping.GetLightingSettingsOrDefaultsFallback().extractAO; }
            set { UnityEditor.Lightmapping.GetOrCreateLightingsSettings().extractAO = value; }
        }

        public static bool BakeAsync(Scene targetScene)
        {
            RenderPipelineManager.TryPrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
            return BakeSceneAsync(targetScene);
        }

        [NativeThrows]
        [FreeFunction]
        [NativeName("BakeAsync")]
        static extern bool BakeSceneAsync(Scene targetScene);

        public static bool Bake(Scene targetScene)
        {
            RenderPipelineManager.TryPrepareRenderPipeline(GraphicsSettings.currentRenderPipeline);
            return BakeScene(targetScene);
        }

        [NativeThrows]
        [FreeFunction]
        [NativeName("Bake")]
        static extern bool BakeScene(Scene targetScene);

        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public static event Action additionalBakedProbesCompleted;

        [RequiredByNativeCode]
        internal static void Internal_CallAdditionalBakedProbesCompleted()
        {
            if (additionalBakedProbesCompleted != null)
                additionalBakedProbesCompleted();
        }

        [FreeFunction]
        internal unsafe static extern bool GetAdditionalBakedProbes(int id, void* outBakedProbeSH, void* outBakedProbeValidity, void* outBakedProbeOctahedralDepth, int outBakedProbeCount);

        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public unsafe static bool GetAdditionalBakedProbes(int id, NativeArray<SphericalHarmonicsL2> outBakedProbeSH, NativeArray<float> outBakedProbeValidity)
        {
            const int octahedralDepthMapTexelCount = 64; // 8*8
            var outBakedProbeOctahedralDepth = new NativeArray<float>(outBakedProbeSH.Length * octahedralDepthMapTexelCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            bool success = GetAdditionalBakedProbes(id, outBakedProbeSH, outBakedProbeValidity, outBakedProbeOctahedralDepth);
            outBakedProbeOctahedralDepth.Dispose();
            return success;
        }
        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public unsafe static bool GetAdditionalBakedProbes(int id, Span<SphericalHarmonicsL2> outBakedProbeSH, Span<float> outBakedProbeValidity, Span<float> outBakedProbeOctahedralDepth)
        {
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
            fixed (void* shPtr = outBakedProbeSH)
            fixed (void* validityPtr = outBakedProbeValidity)
            fixed (void* octahedralDepthPtr = outBakedProbeOctahedralDepth)
            {
                return GetAdditionalBakedProbes(id, shPtr, validityPtr, octahedralDepthPtr, outBakedProbeSH.Length);
            }
        }
        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
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
        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public static void SetAdditionalBakedProbes(int id, Vector3[] positions)
        {
            SetAdditionalBakedProbes(id, positions.AsSpan(), true);
        }
        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public static void SetAdditionalBakedProbes(int id, ReadOnlySpan<Vector3> positions)
        {
            SetAdditionalBakedProbes(id, positions, true);
        }
        [FreeFunction]
        [Obsolete("Please use UnityEngine.LightTransport.IProbeIntegrator instead.", false)]
        public static extern void SetAdditionalBakedProbes(int id, ReadOnlySpan<Vector3> positions, bool dering);

        [FreeFunction]
        public static extern void SetLightDirty(Light light);
    }
}
