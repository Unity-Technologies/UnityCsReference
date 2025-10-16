// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ProjectAuditor.Editor.Core
{
    internal interface IIssueFilter
    {
        bool Match(ReportItem issue);
        bool PackageFilterMatch(ReportItem issue);
    }
}
