// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;

namespace Unity.ProjectAuditor.Editor.Core
{
    class CsvExporter : Exporter
    {
        readonly StringBuilder m_StringBuilder = new StringBuilder();

        public CsvExporter(Report report) : base(report) {}

        public override void WriteHeader(IssueLayout layout)
        {
            m_StringBuilder.Clear();
            for (var i = 0; i < layout.Properties.Length; i++)
            {
                m_StringBuilder.Append(layout.Properties[i].Name);
                if (i + 1 < layout.Properties.Length)
                    m_StringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(m_StringBuilder);
        }

        protected override void WriteIssue(IssueLayout layout, ReportItem issue)
        {
            m_StringBuilder.Clear();
            for (var i = 0; i < layout.Properties.Length; i++)
            {
                var columnType = layout.Properties[i].Type;
                var prop = issue.GetProperty(columnType);

                m_StringBuilder.Append('"');
                m_StringBuilder.Append(prop);
                m_StringBuilder.Append('"');

                if (i + 1 < layout.Properties.Length)
                    m_StringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(m_StringBuilder);
        }
    }
}
