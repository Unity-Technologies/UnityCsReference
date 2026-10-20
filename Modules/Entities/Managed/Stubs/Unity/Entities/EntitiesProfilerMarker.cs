// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;

namespace Unity.Entities
{
    // Entities code sits below the Core module that declares Unity.Profiling.ProfilerMarker, so this marker stands in
    // for it. It records through ManagedKernel's KernelProfiler, whose calls are not [Conditional("ENABLE_PROFILER")],
    // so they also run in module builds for players. Its name differs because the module's internals are visible to
    // package assemblies, where a second ProfilerMarker would collide with Unity.Profiling's.
    readonly struct EntitiesProfilerMarker
    {
        readonly IntPtr m_Ptr;

        public EntitiesProfilerMarker(string name)
            : this(KernelProfiler.CategoryScripts, name)
        {
        }

        public EntitiesProfilerMarker(ushort categoryId, string name)
        {
            m_Ptr = IntPtr.Zero;
            CreateMarker(ref m_Ptr, categoryId, name);
        }

        // Burst can't build the marker name, so Burst-side copies stay null and record nothing; KernelProfiler
        // ignores a null marker.
        [BurstDiscard]
        static void CreateMarker(ref IntPtr ptr, ushort categoryId, string name) =>
            ptr = KernelProfiler.CreateMarker(name, categoryId, 0, 0);

        public void Begin() => KernelProfiler.BeginSample(m_Ptr);

        public void End() => KernelProfiler.EndSample(m_Ptr);

        public AutoScope Auto() => new AutoScope(m_Ptr);

        public readonly struct AutoScope : IDisposable
        {
            readonly IntPtr m_Ptr;

            internal AutoScope(IntPtr markerPtr)
            {
                m_Ptr = markerPtr;
                KernelProfiler.BeginSample(markerPtr);
            }

            public void Dispose() => KernelProfiler.EndSample(m_Ptr);
        }
    }
}
