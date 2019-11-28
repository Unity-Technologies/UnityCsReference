// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/RawFrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class RawFrameDataView : FrameDataView
    {
        internal RawFrameDataView(int frameIndex, int threadIndex)
        {
            m_Ptr = Internal_Create(frameIndex, threadIndex);
        }

        [ThreadSafe]
        static extern IntPtr Internal_Create(int frameIndex, int threadIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern double GetSampleStartTimeMs(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern ulong GetSampleStartTimeNs(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern float GetSampleTimeMs(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern ulong GetSampleTimeNs(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSampleMarkerId(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern MarkerFlags GetSampleFlags(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern ushort GetSampleCategoryIndex(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetSampleName(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSampleChildrenCount(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSampleChildrenCountRecursive(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSampleMetadataCount(int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ProfilerMarkerDataType GetSampleMetadataType(int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetSampleMetadataAsString(int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSampleMetadataAsInt(int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern long GetSampleMetadataAsLong(int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern float GetSampleMetadataAsFloat(int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern double GetSampleMetadataAsDouble(int sampleIndex, int metadataIndex);

        public void GetSampleCallstack(int sampleIndex, List<ulong> outCallstack)
        {
            if (outCallstack == null)
                throw new ArgumentNullException(nameof(outCallstack));

            GetSampleCallstackInternal(sampleIndex, outCallstack);
        }

        [NativeMethod(Name = "GetSampleCallstack", IsThreadSafe = true, ThrowsException = true)]
        extern void GetSampleCallstackInternal(int sampleIndex, List<ulong> outCallstack);

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct FlowEvent
        {
            public int ParentSampleIndex;
            public uint FlowId;
            public ProfilerFlowEventType FlowEventType;
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetSampleFlowEvents(int sampleIndex, List<FlowEvent> outFlowEvents);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetFlowEvents(List<FlowEvent> outFlowEvents);

        public override bool Equals(object obj)
        {
            var dataViewObj = obj as RawFrameDataView;
            if (dataViewObj == null)
                return false;

            if (m_Ptr == dataViewObj.m_Ptr)
                return true;
            if (m_Ptr == IntPtr.Zero || dataViewObj.m_Ptr == IntPtr.Zero)
                return false;

            return frameIndex.Equals(dataViewObj.frameIndex) &&
                threadIndex.Equals(dataViewObj.threadIndex);
        }

        public override int GetHashCode()
        {
            return frameIndex.GetHashCode() ^
                (threadIndex.GetHashCode() << 8);
        }
    }
}
