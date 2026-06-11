// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    class JobManager : IDisposable
    {
        const string k_JobManagerName = $"Renderer.{nameof(JobManager)}";

        NativePagedList<NudgeJobData> m_NudgeJobs = new NativePagedList<NudgeJobData>(64, k_JobManagerName);
        NativePagedList<ConvertMeshJobData> m_ConvertMeshJobs = new NativePagedList<ConvertMeshJobData>(64, k_JobManagerName);
        NativePagedList<ConvertMeshExtrasData> m_ConvertMeshExtras = new NativePagedList<ConvertMeshExtrasData>(16, k_JobManagerName);
        NativePagedList<CopyMeshJobData> m_CopyMeshJobs = new NativePagedList<CopyMeshJobData>(64, k_JobManagerName);

        JobMerger m_JobMerger = new JobMerger(128);

        public void Add(ref NudgeJobData job)
        {
            m_NudgeJobs.Add(ref job);
        }

        public void Add(ref ConvertMeshJobData job)
        {
            m_ConvertMeshJobs.Add(ref job);
        }

        public void Add(ref CopyMeshJobData job)
        {
            m_CopyMeshJobs.Add(ref job);
        }

        // Pointer is valid until CompleteConvertMeshJobs resets the underlying list.
        public unsafe ConvertMeshExtrasData* AllocConvertMeshExtras()
        {
            return m_ConvertMeshExtras.AllocLast();
        }

        public unsafe void CompleteNudgeJobs()
        {
            foreach (NativeSlice<NudgeJobData> page in m_NudgeJobs.GetPages())
                m_JobMerger.Add(JobProcessor.ScheduleNudgeJobs((IntPtr)page.GetUnsafePtr(), page.Length));
            m_JobMerger.MergeAndReset().Complete();
            m_NudgeJobs.Reset();
        }

        public unsafe void CompleteConvertMeshJobs()
        {
            foreach (NativeSlice<ConvertMeshJobData> page in m_ConvertMeshJobs.GetPages())
                m_JobMerger.Add(JobProcessor.ScheduleConvertMeshJobs((IntPtr)page.GetUnsafePtr(), page.Length));
            m_JobMerger.MergeAndReset().Complete();
            m_ConvertMeshJobs.Reset();
            m_ConvertMeshExtras.Reset();
        }

        public unsafe void CompleteCopyMeshJobs()
        {
            foreach (NativeSlice<CopyMeshJobData> page in m_CopyMeshJobs.GetPages())
                m_JobMerger.Add(JobProcessor.ScheduleCopyMeshJobs((IntPtr)page.GetUnsafePtr(), page.Length));
            m_JobMerger.MergeAndReset().Complete();
            m_CopyMeshJobs.Reset();
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_NudgeJobs.Dispose();
                m_ConvertMeshJobs.Dispose();
                m_ConvertMeshExtras.Dispose();
                m_CopyMeshJobs.Dispose();

                m_JobMerger.Dispose();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }

    enum TextCoreSettingsMode
    {
        None = 0,
        PerElement = 1,
        PerGlyph = 2,
    }

    // *** The following structs must remain in sync with those defined in UIRendererJobProcessor ***
    [StructLayout(LayoutKind.Sequential)]
    struct NudgeJobData
    {
        public IntPtr headSrc;
        public IntPtr headDst;
        public int headCount;

        public IntPtr tailSrc;
        public IntPtr tailDst;
        public int tailCount;

        public int vertStride; // Vertex + Extra (bytes)

        public Matrix4x4 transform;

        public int keepZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ConvertMeshJobData
    {
        public IntPtr vertSrc;
        public IntPtr vertDst;
        public int vertCount;
        public int vertStride; // Vertex + Extra (bytes)
        public Matrix4x4 transform;
        public ushort clipRectId;
        public ushort transformId;
        public ushort dynamicColorOrTextCoreId;
        public ushort opacityId;
        public VertexFlags flags;
        public ushort textureId;
        public ushort gradientSettingsIndexOffset;
        public TextCoreSettingsMode usesTextCoreSettings;

        public IntPtr indexSrc;
        public IntPtr indexDst;
        public int indexCount;
        public int indexOffset;

        public int flipIndices;
        public int forceZ;
        public float positionZ;

        public int remapUVs;
        public Rect atlasRect;
        public Vector2 layoutSize;

        public IntPtr extras; // Can be null
    }

    // Side struct referenced by ConvertMeshJobData.extras. Allocated once per draw that carries extras
    [StructLayout(LayoutKind.Sequential)]
    struct ConvertMeshExtrasData
    {
        public IntPtr texCoord1Src;  public int texCoord1SrcStride;  public int texCoord1DstOffset;
        public IntPtr texCoord2Src;  public int texCoord2SrcStride;  public int texCoord2DstOffset;
        public IntPtr texCoord3Src;  public int texCoord3SrcStride;  public int texCoord3DstOffset;
        public IntPtr normalSrc;     public int normalSrcStride;     public int normalDstOffset;
        public IntPtr tangentSrc;    public int tangentSrcStride;    public int tangentDstOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CopyMeshJobData
    {
        public IntPtr vertSrc;
        public IntPtr vertDst;
        public int vertCount;
        public int vertStride; // Vertex + Extra (bytes)

        public IntPtr indexSrc;
        public IntPtr indexDst;
        public int indexCount;
        public int indexOffset;
    }
}
