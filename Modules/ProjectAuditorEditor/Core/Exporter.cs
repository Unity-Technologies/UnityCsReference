// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal abstract class Exporter : IDisposable
    {
        readonly Report m_Report;

        protected StreamWriter m_StreamWriter;

        protected Exporter(Report report)
        {
            m_Report = report;
        }

        public void Export(string path, IssueCategory category, Func<ReportItem, bool> predicate = null)
        {
            m_StreamWriter = new StreamWriter(path);
            var issues = m_Report.FindByCategory(category);
            if (predicate != null)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                issues = new List<ReportItem>(issues.Where(predicate));
#pragma warning restore UA2001
            var layout = m_Report.GetLayout(category);
            if (layout == null)
                Debug.LogWarning($"Could not find issue layout for category {category}");
            else
            {
                WriteHeader(layout);
                foreach (var issue in issues)
                    WriteIssue(layout, issue);
                WriteFooter(layout);
            }
        }

        public void Dispose()
        {
            if (m_StreamWriter == null)
                return;

            m_StreamWriter.Flush();
            m_StreamWriter.Close();
        }

        public virtual void WriteFooter(IssueLayout layout) {}

        public abstract void WriteHeader(IssueLayout layout);

        protected abstract void WriteIssue(IssueLayout layout, ReportItem issue);
    }
}
