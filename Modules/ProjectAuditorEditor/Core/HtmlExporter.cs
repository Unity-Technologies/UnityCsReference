// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor.Core
{
    class HtmlExporter : Exporter
    {
        public HtmlExporter(Report report) : base(report) {}

        public override void WriteHeader(IssueLayout layout)
        {
            m_StreamWriter.Write(@"<html>" + m_StreamWriter.NewLine + @"<body>" + m_StreamWriter.NewLine);
            m_StreamWriter.Write(@"<table width='50%' cellpadding='10' style='margin-top:10px' cellspacing='3' border='1' rules='all'>" + m_StreamWriter.NewLine + @"<tr>" + m_StreamWriter.NewLine);
            for (var i = 0; i < layout.Properties.Length; i++)
            {
                m_StreamWriter.WriteLine(@"<th>" + layout.Properties[i].Name + @"</th>");
            }
            m_StreamWriter.WriteLine(@"</tr>");
        }

        protected override void WriteIssue(IssueLayout layout, ReportItem issue)
        {
            m_StreamWriter.WriteLine(@"<tr>");
            for (var i = 0; i < layout.Properties.Length; i++)
            {
                var columnType = layout.Properties[i].Type;
                var prop = issue.GetProperty(columnType);
                m_StreamWriter.WriteLine(@"<td>" + prop + @"</td>");
            }
            m_StreamWriter.WriteLine(@"</tr>");
        }

        public override void WriteFooter(IssueLayout layout)
        {
            m_StreamWriter.Write(@"</body>" + m_StreamWriter.NewLine + @"</html>" + m_StreamWriter.NewLine);
        }
    }
}
