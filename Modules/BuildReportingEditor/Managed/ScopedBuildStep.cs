// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Reporting
{
    internal struct ScopedBuildStep : IDisposable
    {
        private readonly BuildReport m_Report;
        private readonly int m_Step;

        public ScopedBuildStep(BuildReport report, string stepName)
        {
            if (report == null)
                throw new ArgumentNullException("report");

            m_Report = report;
            m_Step = report.BeginBuildStep(stepName);
        }

        public void Resume()
        {
            m_Report.ResumeBuildStep(m_Step);
        }

        public void Dispose()
        {
            m_Report.EndBuildStep(m_Step);
        }
    }
}
