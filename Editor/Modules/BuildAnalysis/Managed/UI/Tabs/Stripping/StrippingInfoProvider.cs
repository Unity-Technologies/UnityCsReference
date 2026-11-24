// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Build.Reporting;

namespace UnityEditor.Build.Analysis;

internal class StrippingInfoProvider : IStrippingInfoProvider
{
    private readonly IBuildHistoryProvider m_BuildHistoryProvider;
    private BuildEntry m_BuildEntry;
    private BuildReport m_BuildReport;
    private StrippingInfo m_StrippingInfo;

    public StrippingInfoProvider(IBuildHistoryProvider buildHistoryProvider)
    {
        m_BuildHistoryProvider = buildHistoryProvider;
    }

    public void Apply(BuildEntry build)
    {
        m_StrippingInfo = null;
        m_BuildReport = null;
        m_BuildEntry = build;
        if (m_BuildEntry == null)
            return;

        // Note: This should be moved to be async as part of the BuildAnalysis generation once it's no longer internal
        // However, while we're iterating on the data we'll load it here
        if (!m_BuildHistoryProvider.TryLoadBuildReport(m_BuildEntry.BuildSessionGUID, out var buildReport)) 
            return;
        
        m_BuildReport = buildReport;
        m_StrippingInfo = m_BuildReport.strippingInfo;
    }

    public bool BuildHasStrippingInfo()
    {
        return m_StrippingInfo != null;
    }

    public IEnumerable<string> GetIncludedModules()
    {
        return m_StrippingInfo?.includedModules ?? [];
    }

    public IEnumerable<string> GetReasonsForIncluding(string item)
    {
        return m_StrippingInfo?.GetReasonsForIncluding(item) ?? [];
    }
}
 
