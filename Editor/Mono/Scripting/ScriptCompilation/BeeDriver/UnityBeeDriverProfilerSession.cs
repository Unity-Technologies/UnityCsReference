// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using NiceIO;
using Unity.TinyProfiling;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriverProfilerSession
    {
        private static NPath m_CurrentPlayerBuildProfilerOutputFile;
        private static int m_BeeDriverForCurrentPlayerBuildIndex;
        private static Stack<IDisposable> m_ProfilerSections = new Stack<IDisposable>();

        static public void Start(NPath path)
        {
            m_CurrentPlayerBuildProfilerOutputFile = path;
            m_BeeDriverForCurrentPlayerBuildIndex = 0;
            TinyProfiler.ConfigureOutput(m_CurrentPlayerBuildProfilerOutputFile, "Unity", -100);
        }

        static public void Finish()
        {
            m_CurrentPlayerBuildProfilerOutputFile = null;
        }

        static public void BeginSection(string name)
        {
            m_ProfilerSections.Push(TinyProfiler.Section(name));
        }

        static public void EndSection()
        {
            m_ProfilerSections.Pop().Dispose();
        }

        static public NPath GetTraceEventsOutputForNewBeeDriver()
        {
            if (m_CurrentPlayerBuildProfilerOutputFile == null)
                return null;
            NPath path = $"{m_CurrentPlayerBuildProfilerOutputFile.Parent}/{m_CurrentPlayerBuildProfilerOutputFile.FileName}_{m_BeeDriverForCurrentPlayerBuildIndex++}.traceevents";
            TinyProfiler.AddExternalTraceEventFile(path);
            return path;
        }
    }
}
