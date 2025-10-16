// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using Unity.Profiling;
using System.Diagnostics;


namespace UnityEditor
{
    class StatsField : VisualElement, IPrefixLabel
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData),
                    new UxmlAttributeNames[]
                    {
                        new(nameof(label), "label"), new(nameof(value), "value"),
                    }, true);
            }
#pragma warning disable 649
            [SerializeField, MultilineTextField] string label;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags label_UxmlAttributeFlags;
            [SerializeField, MultilineTextField] string value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;

#pragma warning restore 649

            public override object CreateInstance() => new StatsField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (StatsField)obj;
                if (ShouldWriteAttributeValue(label_UxmlAttributeFlags))
                    e.label = label;
                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                    e.value = value;
                
            }
        }

        private Label m_LabelText;
        private Label m_ValueText;

        public string label
        {
            get => m_LabelText.text;
            set => m_LabelText.text = value;
        }

        public Label labelElement => m_LabelText;

        [CreateProperty]
        public string value
        {
            get => m_ValueText.text;
            set => m_ValueText.text = value;
        }

        public StatsField()
        {
            AddToClassList("statsfield");
            Add(m_LabelText = new Label());
            m_LabelText.name = "labelName";
            Add(new Label(": "));
            Add(m_ValueText = new Label());
            m_ValueText.name = "valueName";
            style.flexDirection = FlexDirection.Row;
        }
    }

    class StatsData: IDisposable
    {
        // --- Constants ---
        private const float k_BytesToMegabytes = 1f / (1024f * 1024f);
        private const int k_FpsUpdateFrameThreshold = 30;
        private const float k_FpsUpdateTimeThreshold = 0.3f;
        private const float k_MemoryUpdateInterval = 0.5f;
        private const int k_MemoryUpdateFrameCount = 30;

        // --- Frame Timing ---
        private int m_FrameCounter = 0;
        private float m_MainThreadTimeAccumulator = 0.0f;
        private float m_RenderThreadTimeAccumulator = 0.0f;
        private float m_MaxTimeAccumulator = 0.0f;
        private float m_AveragedMainFrameTime = 0.0f;
        private float m_AveragedRenderFrameTime = 0.0f;
        private float m_MaxFrameTime = 0.0f;
        private float m_GpuTimeAccumulator = 0.0f;
        private float m_AveragedGpuFrameTime = 0.0f;
        private float m_CurrentFPS = 0f;
        private float m_CurrentFrameTimeMS = 0f;
        private FrameTiming[] m_FrameTimings = new FrameTiming[1];

        // --- Memory Timing ---
        private ProfilerRecorder m_TotalMemoryRecorder;
        private ProfilerRecorder m_GfxMemoryRecorder;
        private float m_MemoryUpdateTimeAccumulator = 0.0f;
        private int m_MemoryUpdateFrameCounter = 0;

        // --- String Fields ---
        private string m_FpsText = "- FPS (Playmode Off)";
        private string m_GlobalFrametimeText = "- ms (Playmode Off)";
        private string m_ThreadComparisonText = "- vs - (Playmode Off)";
        private string m_GPUFrametimeText = "- ms (Playmode Off)";
        private string m_TotalAllocatedMemoryString = "- MB";
        private string m_EstimatedGfxMemoryString = "- MB";
        private string m_CurrentTextureMemoryString = "- MB";
        private string m_TotalTextureMemoryString = "- MB";
        private string m_StreamingTextureMemoryComparisonString = "0.0% vs 0.0% (Mipmap Streaming Off)";


        public StatsData()
        {
            m_TotalMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            m_GfxMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");
        }

        // FPS and Memory properties
        [CreateProperty] public string fps => m_FpsText;
        [CreateProperty] public string globalFrametime => m_GlobalFrametimeText;
        [CreateProperty] public string threadComparisonText => m_ThreadComparisonText;
        [CreateProperty] public string gpuFrametimeText => m_GPUFrametimeText;
        [CreateProperty] public string totalTextureMemory => m_TotalTextureMemoryString;
        [CreateProperty] public string currentTextureMemory => m_CurrentTextureMemoryString;
        [CreateProperty] public string totalAllocatedMemory => m_TotalAllocatedMemoryString;
        [CreateProperty] public string estimatedGfxMemory => m_EstimatedGfxMemoryString;
        [CreateProperty] public string streamingTextureMemoryComparison => m_StreamingTextureMemoryComparisonString;

        // Calculations
        [CreateProperty] public string totalDrawInfo => $"{UnityStats.drawCalls} ({UnityStats.instances} instances)";
        [CreateProperty] public string srpBatcherDrawInfo => $"{UnityStats.srpBatcherDrawCalls} draw calls ({UnityStats.srpBatcherInstances} instances)";
        [CreateProperty] public string hybridBatcherDrawInfo => $"{UnityStats.hybridBatcherDrawCalls} draw calls ({UnityStats.hybridBatcherInstances} instances)";
        [CreateProperty] public string standardDrawInfo => $"{UnityStats.standardDrawCalls} draw calls ({UnityStats.standardInstances} instances)";
        [CreateProperty] public string standardInstancedDrawInfo => $"{UnityStats.standardInstancedDrawCalls} draw calls ({UnityStats.standardInstancedInstances} instances)";
        [CreateProperty] public string triangles => FormatCounts(UnityStats.triangles);
        [CreateProperty] public string vertices => FormatCounts(UnityStats.vertices);
        [CreateProperty] public string desiredTextureMemory => $"{Texture.desiredTextureMemory * k_BytesToMegabytes:F1} MB";

        // UnityStats
        [CreateProperty] public int drawCall => UnityStats.drawCalls;
        [CreateProperty] public int instances => UnityStats.instances;
        [CreateProperty] public int dynamicBatchedDrawCalls => UnityStats.dynamicBatchedDrawCalls;
        [CreateProperty] public int staticBatchedDrawCalls => UnityStats.staticBatchedDrawCalls;
        [CreateProperty] public int instancedBatchedDrawCalls => UnityStats.instancedBatchedDrawCalls;
        [CreateProperty] public int setPassCalls => UnityStats.setPassCalls;
        [CreateProperty] public int shadowCasters => UnityStats.shadowCasters;
        [CreateProperty] public int renderTextureChanges => UnityStats.renderTextureChanges;
        [CreateProperty] public float audioLevel => UnityStats.audioLevel;
        [CreateProperty] public float audioClippingAmount => UnityStats.audioClippingAmount;
        [CreateProperty] public float audioDSPLoad => UnityStats.audioDSPLoad;
        [CreateProperty] public float audioStreamLoad => UnityStats.audioStreamLoad;
        [CreateProperty] public int renderTextureCount => UnityStats.renderTextureCount;
        [CreateProperty] public int renderTextureBytes => UnityStats.renderTextureBytes;
        [CreateProperty] public int usedTextureMemorySize => UnityStats.usedTextureMemorySize;
        [CreateProperty] public int usedTextureCount => UnityStats.usedTextureCount;
        [CreateProperty] public string screenRes => UnityStats.screenRes;
        [CreateProperty] public int screenBytes => UnityStats.screenBytes;
        [CreateProperty] public int vboTotal => UnityStats.vboTotal;
        [CreateProperty] public int vboTotalBytes => UnityStats.vboTotalBytes;
        [CreateProperty] public int vboUploads => UnityStats.vboUploads;
        [CreateProperty] public int vboUploadBytes => UnityStats.vboUploadBytes;
        [CreateProperty] public int ibUploads => UnityStats.ibUploads;
        [CreateProperty] public int ibUploadBytes => UnityStats.ibUploadBytes;
        [CreateProperty] public int visibleSkinnedMeshes => UnityStats.visibleSkinnedMeshes;
        [CreateProperty] public int updatedOffscreenMeshes => UnityStats.updatedOffscreenMeshes;
        [CreateProperty] public int animationComponentsPlaying => UnityStats.animationComponentsPlaying;
        [CreateProperty] public int animatorComponentsPlaying => UnityStats.animatorComponentsPlaying;


        private string FormatCounts(int value)
        {
            if (value >= 1000)
            {
                return $"{value / 1000.0f:F1}k";
            }
            return value.ToString();
        }

        public void UpdateFPS()
        {
            if (!EditorApplication.isPlaying)
            {
                m_FpsText = "- FPS (Playmode Off)";
                m_GlobalFrametimeText = "- ms (Playmode Off)";
                m_ThreadComparisonText = "- vs - (Playmode Off)";
                m_GPUFrametimeText = "- ms (Playmode Off)";
                return;
            }

            FrameTimingManager.CaptureFrameTimings();
            uint frameCount = FrameTimingManager.GetLatestTimings(1, m_FrameTimings);

            if (frameCount > 0)
            {
                FrameTiming latest = m_FrameTimings[0];

                m_MainThreadTimeAccumulator += (float)latest.cpuMainThreadFrameTime;
                m_RenderThreadTimeAccumulator += (float)latest.cpuRenderThreadFrameTime;
                m_GpuTimeAccumulator += (float)latest.gpuFrameTime;
                m_MaxTimeAccumulator += (float)latest.cpuFrameTime;

                ++m_FrameCounter;
            }

            bool needsFirstTimeUpdate = m_AveragedMainFrameTime == 0.0f;
            float timeThresholdInMs = k_FpsUpdateTimeThreshold * 1000.0f;
            bool needsRegularUpdate = m_FrameCounter > k_FpsUpdateFrameThreshold || m_MaxTimeAccumulator > timeThresholdInMs;

            if (needsFirstTimeUpdate || needsRegularUpdate)
            {
                if (m_FrameCounter == 0) return;

                m_AveragedMainFrameTime = m_MainThreadTimeAccumulator / m_FrameCounter;
                m_AveragedRenderFrameTime = m_RenderThreadTimeAccumulator / m_FrameCounter;
                m_AveragedGpuFrameTime = m_GpuTimeAccumulator / m_FrameCounter;
                m_MaxFrameTime = m_MaxTimeAccumulator / m_FrameCounter;

                m_CurrentFPS = 1000.0f / Mathf.Max(m_MaxFrameTime, 1.0e-5f);
                m_CurrentFrameTimeMS = m_MaxFrameTime;

                m_FpsText = $"{m_CurrentFPS:F1} FPS";
                m_GlobalFrametimeText = $"{m_CurrentFrameTimeMS:F1} ms";
                m_GPUFrametimeText = $"{m_AveragedGpuFrameTime:F1} ms";

                float totalCpuTime = m_AveragedMainFrameTime + m_AveragedRenderFrameTime;
                if (totalCpuTime > 1e-5f) // Avoid division by zero
                {
                    float mainPercent = (m_AveragedMainFrameTime / totalCpuTime) * 100f;
                    float renderPercent = (m_AveragedRenderFrameTime / totalCpuTime) * 100f;
                    m_ThreadComparisonText = $"{mainPercent:F1}% vs {renderPercent:F1}%";
                }
                else
                {
                    m_ThreadComparisonText = "0.0% vs 0.0%";
                }
            }

            if (needsRegularUpdate)
            {
                m_MainThreadTimeAccumulator = 0.0f;
                m_RenderThreadTimeAccumulator = 0.0f;
                m_GpuTimeAccumulator = 0.0f;
                m_MaxTimeAccumulator = 0.0f;
                m_FrameCounter = 0;
            }
        }

        public void UpdateMemory()
        {
            m_MemoryUpdateTimeAccumulator += Time.deltaTime;
            ++m_MemoryUpdateFrameCounter;

            bool timeToUpdateByTime = m_MemoryUpdateTimeAccumulator > k_MemoryUpdateInterval;
            bool timeToUpdateByFrames = m_MemoryUpdateFrameCounter > k_MemoryUpdateFrameCount;

            if (timeToUpdateByTime || timeToUpdateByFrames)
            {
                ulong totalTexMem = UnityEngine.Texture.totalTextureMemory;
                ulong currentTexMem = UnityEngine.Texture.currentTextureMemory;
                m_TotalAllocatedMemoryString = $"{m_TotalMemoryRecorder.LastValue / (1024f * 1024f):F1} MB";
                m_EstimatedGfxMemoryString = $"{m_GfxMemoryRecorder.LastValue / (1024f * 1024f):F1} MB";
                m_TotalTextureMemoryString = $"{totalTexMem / (1024f * 1024f):F1} MB";
                m_CurrentTextureMemoryString = $"{currentTexMem / (1024f * 1024f):F1} MB";

                if (totalTexMem > 0)
                {
                    float nonStreamingPercent = (float)Texture.nonStreamingTextureMemory / totalTexMem * 100f;
                    float streamingPercent = 100f - nonStreamingPercent;
                    m_StreamingTextureMemoryComparisonString = $"{streamingPercent:F1}% vs {nonStreamingPercent:F1}%";
                }
                else
                {
                    m_StreamingTextureMemoryComparisonString = "0.0% vs 0.0%";
                }

                m_MemoryUpdateTimeAccumulator = 0.0f;
                m_MemoryUpdateFrameCounter = 0;
            }
        }

        public void Dispose()
        {
            m_TotalMemoryRecorder.Dispose(); 
            m_GfxMemoryRecorder.Dispose();
        }
    }

}
