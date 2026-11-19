// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.Profiling
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/RawFrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class RawFrameDataView : FrameDataView
    {
        public const int invalidSampleIndex = -1;

        internal RawFrameDataView(int frameIndex, int threadIndex)
        {
            m_Ptr = Internal_Create(frameIndex, threadIndex);
        }

        // Index-based enumeration of a frame's threads stops before the GPU thread.
        internal static RawFrameDataView CreateForCpuThread(int frameIndex, int threadIndex)
        {
            return new RawFrameDataView(Internal_CreateCpuThread(frameIndex, threadIndex));
        }

        RawFrameDataView(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create(int frameIndex, int threadIndex);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_CreateCpuThread(int frameIndex, int threadIndex);

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

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        private unsafe extern FrameDataView.Data GetSampleMetadataAsSpanInternal(int sampleIndex, int metadataIndex);

        public Span<T> GetSampleMetadataAsSpan<T>(int sampleIndex, int metadataIndex) where T : unmanaged
        {
            unsafe
            {
                var data = GetSampleMetadataAsSpanInternal(sampleIndex, metadataIndex);
                int sizeT = UnsafeUtility.SizeOf<T>();
                // As len is given with size in bytes and we want number of elments in span
                // we need to divide by the size of the type
                return new Span<T>((void*)data.ptr, data.size / sizeT);
            }
        }

        public void GetSampleCallstack(int sampleIndex, List<ulong> outCallstack)
        {
            if (outCallstack == null)
                throw new ArgumentNullException(nameof(outCallstack));

            GetSampleCallstackInternal(sampleIndex, outCallstack);
        }

        [NativeMethod(Name = "GetSampleCallstack", IsThreadSafe = true, ThrowsException = true)]
        extern void GetSampleCallstackInternal(int sampleIndex, [NotNull] List<ulong> outCallstack);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern EntityId GetSampleEntityId(int sampleIndex);

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct FlowEvent
        {
            public int ParentSampleIndex;
            public uint FlowId;
            public ProfilerFlowEventType FlowEventType;
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetSampleFlowEvents(int sampleIndex, [NotNull] List<FlowEvent> outFlowEvents);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetFlowEvents([NotNull] List<FlowEvent> outFlowEvents);

        [NativeMethod(IsThreadSafe = true)]
        internal extern void GetSamplesStartedInPreviousFrame([NotNull] List<int> outSampleIndexList);

        [NativeMethod(IsThreadSafe = true)]
        internal extern void GetSamplesContinuedInNextFrame([NotNull] List<int> outSampleIndexList);

        // Async marker tracks (see AsyncProfilerMarker). Thread-indifferent: independent of threadIndex.
        // 'track' is in [0, GetAsyncTrackCount()) in stable asyncId order.
        [NativeMethod(IsThreadSafe = true)]
        internal extern int GetAsyncTrackCount();

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern int GetAsyncSampleMarkerId(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ulong GetAsyncSampleStartTimeNs(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ulong GetAsyncSampleTimeNs(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ulong GetAsyncSampleTotalTimeNs(int track);

        // True only in the frame the span ended in — each closed span reports closed in exactly one frame,
        // which makes "closed tracks only" a duplicate-free way to enumerate completed spans across frames.
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern bool GetAsyncSampleClosed(int track);

        // Whole-op extent in absolute nanoseconds (same time base as sample start times).
        // End is only meaningful when the span is closed; while ongoing it is the latest finalized frame end.
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ulong GetAsyncSampleBeginTimeNs(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern ulong GetAsyncSampleEndTimeNs(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern int GetAsyncSampleMetadataCount(int track);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern string GetAsyncSampleMetadataAsString(int track, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern long GetAsyncSampleMetadataAsLong(int track, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern double GetAsyncSampleMetadataAsDouble(int track, int metadataIndex);

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

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(RawFrameDataView frameDataView) => frameDataView.m_Ptr;
        }
    }
}
