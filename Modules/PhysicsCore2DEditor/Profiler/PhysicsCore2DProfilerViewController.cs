// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Profiling.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    partial class PhysicsCore2DProfilerViewController : ProfilerModuleViewController
    {
        private PhysicsCore2DProfilerView m_Root;

        public PhysicsCore2DProfilerViewController(ProfilerWindow profilerWindow)
            : base(profilerWindow)
        {
            profilerWindow.SelectedFrameIndexChanged += OnProfilerFrameChange;
        }

        internal static (PhysicsCore2DFrameData[] frameData, byte[] worldNames, Unity.U2D.Physics.PhysicsWorld.WorldCounters counter, float[])
            ExtractFrameData(long frameIndex)
        {
            int selectedFrameIndexInt32 = Convert.ToInt32(frameIndex);
            PhysicsCore2DFrameData[] capturedFrameData = Array.Empty<PhysicsCore2DFrameData>();
            byte[] capturedWorldNames = Array.Empty<byte>();
            Unity.U2D.Physics.PhysicsWorld.WorldCounters counter = default;
            float[] profilerMarkerValues = new float[PhysicsCore2DProfilerMarkers.markerNames.Length];
            try
            {
                using (RawFrameDataView frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
                {
                    // The selected frame can have aged out of the profiler's ring buffer, or carry no data for the main thread.
                    if (frameData == null || !frameData.valid)
                        return (capturedFrameData, capturedWorldNames, counter, profilerMarkerValues);

                    var data = frameData.GetFrameMetaData<PhysicsCore2DFrameData>(
                         PhysicsCore2DProfilerMarkers.k_PhysicsCore2DProfilerProjectId, PhysicsCore2DProfilerMarkers.k_PhysicsCore2DFrameDataTag);

                    // The names travel as one blob the rows slice into, so a row costs only its eight-byte slice.
                    var worldNames = frameData.GetFrameMetaData<byte>(
                         PhysicsCore2DProfilerMarkers.k_PhysicsCore2DProfilerProjectId, PhysicsCore2DProfilerMarkers.k_PhysicsCore2DWorldNamesTag);

                    // Extract counters from profiler markers
                    int markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_BodyCountCounterName);
                    counter.bodyCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_ShapeCountCounterName);
                    counter.shapeCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_ContactCountCounterName);
                    counter.contactCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_JointCountCounterName);
                    counter.jointCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_IslandCountCounterName);
                    counter.islandCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_StackUsedCounterName);
                    counter.stackUsed = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_MemoryUsedCounterName);
                    counter.usedMemory = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_TaskCountCounterName);
                    counter.taskCount = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_BroadphaseHeightCounterName);
                    counter.broadphaseHeight = frameData.GetCounterValueAsInt(markerId);

                    markerId = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.k_StaticBroadphaseHeightCounterName);
                    counter.staticBroadphaseHeight = frameData.GetCounterValueAsInt(markerId);


                    if (data.Length > 0)
                    {
                        capturedFrameData = data.ToArray();
                    }

                    if (worldNames.Length > 0)
                    {
                        capturedWorldNames = worldNames.ToArray();
                    }

                    // Ids belong to the capture, not to this process, so each name is resolved against the
                    // frame's own table. A name this capture never recorded resolves to the invalid id, which
                    // simply matches no sample below.
                    int[] profilerMarkerIds = new int[PhysicsCore2DProfilerMarkers.markerNames.Length];
                    for(int i = 0; i < PhysicsCore2DProfilerMarkers.markerNames.Length; ++i)
                        profilerMarkerIds[i] = frameData.GetMarkerId(PhysicsCore2DProfilerMarkers.markerNames[i]);

                    int sampleCount = frameData.sampleCount;
                    for (int i = 0; i < sampleCount; ++i)
                    {
                        markerId = frameData.GetSampleMarkerId(i);
                        float markerTime = frameData.GetSampleTimeMs(i);
                        for (int j = 0; j < profilerMarkerIds.Length; j++)
                        {
                            if (markerId == profilerMarkerIds[j])
                            {
                                profilerMarkerValues[j] += markerTime;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // The absent-frame cases return empty above, so anything arriving here is a real failure worth reporting.
                Debug.LogException(exception);
            }
            return (capturedFrameData, capturedWorldNames, counter, profilerMarkerValues);
        }

        void OnProfilerFrameChange(long frameIndex)
        {
            if (Event.current != null && Event.current.type == EventType.Layout)
                return;

            if (frameIndex < 0)
            {
                m_Root?.SetCapturedFrameData(Array.Empty<PhysicsCore2DFrameData>(), Array.Empty<byte>(), default);
                m_Root?.SetStatistic(default);
                return;
            }

            CreateRootIfNotExist();

            // Live Update off freezes the view on whatever it currently shows, whether or not the profiler is recording.
            if (!m_Root.IsLiveUpdateEnabled())
                return;

            var (modules, worldNames, counter, profilerMarkerValues) = ExtractFrameData(frameIndex);
            m_Root.SetCapturedFrameData(modules, worldNames, profilerMarkerValues);
            m_Root.SetStatistic(counter);
        }

        // Refresh straight away when Live Update is switched back on, so the view catches up without waiting for the next frame change.
        void OnLiveUpdateChanged()
        {
            if (m_Root != null && m_Root.IsLiveUpdateEnabled())
                OnProfilerFrameChange(ProfilerWindow.selectedFrameIndex);
        }

        PhysicsCore2DProfilerView CreateRootIfNotExist()
        {
            if (m_Root == null)
            {
                m_Root = new PhysicsCore2DProfilerView();
                m_Root.liveUpdateChanged += OnLiveUpdateChanged;
            }
            return m_Root;
        }

        protected override VisualElement CreateView()
        {
            CreateRootIfNotExist();
            OnProfilerFrameChange(this.ProfilerWindow.selectedFrameIndex);
            return m_Root;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && ProfilerWindow != null)
            {
                ProfilerWindow.SelectedFrameIndexChanged -= OnProfilerFrameChange;
            }
            m_Root = null;
            base.Dispose(disposing);
        }
    }
}
