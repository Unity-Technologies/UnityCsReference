// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Profiling
{
    [UsedByNativeCode]
    public sealed class Recorder
    {
        const ProfilerRecorderOptions s_RecorderDefaultOptions =
            ProfilerRecorder.SharedRecorder |
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
            ProfilerRecorderOptions.SumAllSamplesInFrame |
            ProfilerRecorderOptions.StartImmediately;
        static internal Recorder s_InvalidRecorder = new Recorder();

        ProfilerRecorder m_RecorderCPU;
        ProfilerRecorder m_RecorderGPU;

        // This class can't be explicitly created
        internal Recorder()
        {
        }

        internal Recorder(ProfilerRecorderHandle handle)
        {
            if (!handle.Valid)
                return;

            m_RecorderCPU = new ProfilerRecorder(handle, 1, s_RecorderDefaultOptions);

            var description = ProfilerRecorderHandle.GetDescription(handle);
            if ((description.Flags & MarkerFlags.SampleGPU) != 0)
                m_RecorderGPU = new ProfilerRecorder(handle, 1, s_RecorderDefaultOptions | ProfilerRecorderOptions.GpuRecorder);
        }

        ~Recorder()
        {
            m_RecorderCPU.Dispose();
            m_RecorderGPU.Dispose();
        }

        public static Recorder Get(string samplerName)
        {
            var handler = ProfilerRecorderHandle.Get(ProfilerCategory.Any, samplerName);
            if (!handler.Valid)
                return s_InvalidRecorder;
            return new Recorder(handler);
        }

        public bool isValid
        {
            get { return m_RecorderCPU.handle != 0; }
        }

        public bool enabled
        {
            get { return m_RecorderCPU.IsRunning; }
            set { SetEnabled(value); }
        }

        public long elapsedNanoseconds
        {
            get
            {
                if (!m_RecorderCPU.Valid)
                    return 0;
                return m_RecorderCPU.LastValue;
            }
        }

        public long gpuElapsedNanoseconds
        {
            get
            {
                if (!m_RecorderGPU.Valid)
                    return 0;
                return m_RecorderGPU.LastValue;
            }
        }

        public int sampleBlockCount
        {
            get
            {
                if (!m_RecorderCPU.Valid)
                    return 0;
                if (m_RecorderCPU.Count != 1)
                    return 0;
                return (int)m_RecorderCPU.GetSample(0).Count;
            }
        }

        public int gpuSampleBlockCount
        {
            get
            {
                if (!m_RecorderGPU.Valid)
                    return 0;
                if (m_RecorderGPU.Count != 1)
                    return 0;
                return (int)m_RecorderGPU.GetSample(0).Count;
            }
        }

        public void FilterToCurrentThread()
        {
            if (!m_RecorderCPU.Valid)
                return;
            m_RecorderCPU.FilterToCurrentThread();
        }

        public void CollectFromAllThreads()
        {
            if (!m_RecorderCPU.Valid)
                return;
            m_RecorderCPU.CollectFromAllThreads();
        }

        private void SetEnabled(bool state)
        {
            if (state)
            {
                m_RecorderCPU.Start();
                if (m_RecorderGPU.Valid)
                    m_RecorderGPU.Start();
            }
            else
            {
                m_RecorderCPU.Stop();
                if (m_RecorderGPU.Valid)
                    m_RecorderGPU.Stop();
            }
        }
    }
}
