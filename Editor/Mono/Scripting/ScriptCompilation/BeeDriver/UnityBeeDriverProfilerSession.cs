// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bee.Core;
using NiceIO;
using UnityEditor.Build;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [NativeHeader("Editor/Mono/Scripting/ScriptCompilation/BeeDriver/UnityBeeDriverProfilerSession.bindings.h")]
    [StaticAccessor("ShaderTraceCapture", StaticAccessorType.DoubleColon)]
    static class UnityBeeDriverProfilerSession
    {
        [NoAutoStaticsCleanup] // transient per-build profiler session state, reset by Start(), safe to persist
        private static NPath m_CurrentPlayerBuildProfilerOutputFile;
        [NoAutoStaticsCleanup] // transient per-build profiler session state, reset by Start(), safe to persist
        private static int m_BeeDriverForCurrentPlayerBuildIndex;
        [NoAutoStaticsCleanup] // transient per-build profiler session state, reset by Start(), safe to persist
        private static TinyProfiler2 _tinyProfiler;
        // Baseline value of NativeProfilerClock.GetMicroseconds() sampled at Start(). Every managed timestamp is expressed relative to this so it lines up with events
        [NoAutoStaticsCleanup] // transient per-build profiler session state, safe to persist
        private static long m_NativeStartMicroseconds;
        // Section entries carry the C++ scope-captured trace event plus its native-µs start so EndSection can compute duration from the native-supplied end value (which arrived
        // as a callback parameter) rather than sampling ElapsedProfilerTime on Dispose. Trunk's Stack<IDisposable> shape assumed the "close at now" pattern and doesn't fit.
        [NoAutoStaticsCleanup] // transient per-build profiler session state, safe to persist
        private static Stack<(Bee.Core.Track track, Bee.Core.TraceEvent evt, long startMicroseconds)> m_ProfilerSections = new();
        [NoAutoStaticsCleanup] // transient per-build profiler session state, safe to persist
        private static List<Task> m_TasksToWaitForBeforeFinishing = new();
        [NoAutoStaticsCleanup] // transient per-build profiler session state, safe to persist
        private static string m_NativeTraceEventsFile;

        public static TinyProfiler2 ProfilerInstance => _tinyProfiler;

        static public void Start(NPath path)
        {
            m_CurrentPlayerBuildProfilerOutputFile = path;
            m_BeeDriverForCurrentPlayerBuildIndex = 0;
            m_TasksToWaitForBeforeFinishing.Clear();

            // Route TinyProfiler2's elapsed clock through NativeProfilerClock so managed events are timestamped from the exact same baselib clock native TEP uses.
            m_NativeStartMicroseconds = NativeProfilerClock.GetMicroseconds();
            _tinyProfiler = new TinyProfiler2(() => new TimeSpan((NativeProfilerClock.GetMicroseconds() - m_NativeStartMicroseconds) * 10));
        }

        // Called from BuildPlayer.cpp after PreparePlayerBuild returns. Deferred so nested content builds inside PrepareForBuild can claim TEP's single global session for
        // their own BuildContentTEP.json / worker log merge without conflicting with ours.
        static public void StartNativeTrace()
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return;

            m_NativeTraceEventsFile = m_CurrentPlayerBuildProfilerOutputFile.Parent.Combine(m_CurrentPlayerBuildProfilerOutputFile.FileNameWithoutExtension + "_native.traceevents").ToString();

            // Pass the managed-side anchors down so native can shift its event timestamps onto TinyProfiler2's unix-μs timeline without resampling either clock
            long anchorUnixUs = (_tinyProfiler.StartTime.UtcTicks - DateTime.UnixEpoch.Ticks) / 10;
            StartCapture(m_NativeTraceEventsFile, System.Threading.Thread.CurrentThread.ManagedThreadId, anchorUnixUs, m_NativeStartMicroseconds);
        }

        static public void Finish()
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return;

            if (m_NativeTraceEventsFile != null && StopCapture() && System.IO.File.Exists(m_NativeTraceEventsFile))
            {
                _tinyProfiler.AddExternalTraceEventsFile(m_NativeTraceEventsFile);
            }

            // Clear the session before writing, so a failed write still leaves it finished. Otherwise the
            // next Finish() retries the write and reports the same failure again.
            var outputFile = m_CurrentPlayerBuildProfilerOutputFile;
            var tinyProfiler = _tinyProfiler;
            m_CurrentPlayerBuildProfilerOutputFile = null;
            _tinyProfiler = null;

            foreach (var task in m_TasksToWaitForBeforeFinishing)
                task.Wait();

            tinyProfiler.Write(outputFile.ToString(), new ChromeTraceOptions
            {
                ProcessName = "Unity",
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                ProcessSortIndex = -100
            });
            m_NativeTraceEventsFile = null;
        }

        static public void BeginSection(string name, long beganAtProfilerUs)
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return;

            TimeSpan startElapsed = new TimeSpan((beganAtProfilerUs - m_NativeStartMicroseconds) * 10);
            Bee.Core.Track track = _tinyProfiler.CurrentThreadTrack;
            Bee.Core.TraceEvent evt = track.CreateEvent(startElapsed, name);
            m_ProfilerSections.Push((track, evt, beganAtProfilerUs));
        }

        static public void EndSection(long endedAtProfilerUs)
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return;

            (Bee.Core.Track track, Bee.Core.TraceEvent evt, long startMicroseconds) = m_ProfilerSections.Pop();
            if (track == null) // filtered section
                return;

            TimeSpan duration = new TimeSpan((endedAtProfilerUs - startMicroseconds) * 10);
            track.SetEventDuration(evt, duration);
        }

        // Emit a section directly on TinyProfiler2 sampled from NativeProfilerClock — used by hooks that fire before StartNativeTrace has activated the native TEP capture
        internal static IDisposable ScopedManagedSection(string name)
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return null;
            return new ManagedSection(name);
        }

        private sealed class ManagedSection : IDisposable
        {
            private readonly Bee.Core.Track m_Track;
            private readonly Bee.Core.TraceEvent m_Event;

            public ManagedSection(string name)
            {
                TimeSpan startElapsed = new TimeSpan((NativeProfilerClock.GetMicroseconds() - m_NativeStartMicroseconds) * 10);
                m_Track = _tinyProfiler.CurrentThreadTrack;
                m_Event = m_Track.CreateEvent(startElapsed, name);
            }

            public void Dispose()
            {
                m_Track.SetEventDurationBasedOnCurrentTime(m_Event);
            }
        }

        static public void AddTaskToWaitForBeforeFinishing(Task t) => m_TasksToWaitForBeforeFinishing.Add(t);
        
        static public bool PerformingPlayerBuild => m_CurrentPlayerBuildProfilerOutputFile != null;

        static public NPath GetTraceEventsOutputForPlayerBuild()
        {
            if (!PerformingPlayerBuild)
                throw new ArgumentException();

            NPath path = $"{m_CurrentPlayerBuildProfilerOutputFile.Parent}/{m_CurrentPlayerBuildProfilerOutputFile.FileName}_{m_BeeDriverForCurrentPlayerBuildIndex++}.traceevents";
            _tinyProfiler.AddExternalTraceEventsFile(path.ToString());
            return path;
        }

        private static extern void StartCapture(string outputPath, int mainManagedThreadId, long anchorUnixUs, long anchorProfilerUs);

        private static extern bool StopCapture();
    }
}
