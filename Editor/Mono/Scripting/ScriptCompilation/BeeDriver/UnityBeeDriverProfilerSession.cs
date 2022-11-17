// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bee.Core;
using NiceIO;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriverProfilerSession
    {
        private static NPath m_CurrentPlayerBuildProfilerOutputFile;
        private static int m_BeeDriverForCurrentPlayerBuildIndex;
        private static TinyProfiler2 _tinyProfiler;
        private static Stack<IDisposable> m_ProfilerSections = new Stack<IDisposable>();
        private static List<Task> m_TasksToWaitForBeforeFinishing = new();

        public static TinyProfiler2 ProfilerInstance => _tinyProfiler;

        static public void Start(NPath path)
        {
            m_CurrentPlayerBuildProfilerOutputFile = path;
            m_BeeDriverForCurrentPlayerBuildIndex = 0;
            m_TasksToWaitForBeforeFinishing.Clear();
            _tinyProfiler = new TinyProfiler2();
        }

        static public void Finish()
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return;

            foreach (var task in m_TasksToWaitForBeforeFinishing)
                task.Wait();

            _tinyProfiler.Write(m_CurrentPlayerBuildProfilerOutputFile.ToString(), new ChromeTraceOptions
            {
                ProcessName = "Unity",
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                ProcessSortIndex = -100
            });
            m_CurrentPlayerBuildProfilerOutputFile = null;
            _tinyProfiler = null;
        }

        static public void BeginSection(string name)
        {
            if (m_CurrentPlayerBuildProfilerOutputFile != null)
            {
                m_ProfilerSections.Push(_tinyProfiler.Section(name));
            }
        }

        static public void EndSection()
        {
            if (m_CurrentPlayerBuildProfilerOutputFile != null)
            {
                m_ProfilerSections.Pop().Dispose();
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
    }
}
