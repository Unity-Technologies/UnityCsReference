// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class DeselectAction : PackageAction
{
    private readonly string m_AnalyticsEventName;

    private readonly IPageManager m_PageManager;
    public DeselectAction(IPageManager pageManager, string analyticsEventName = null)
    {
        m_PageManager = pageManager;
        m_AnalyticsEventName = analyticsEventName;
    }

    protected override bool TriggerActionImplementation(IList<IPackageVersion> versions)
    {
        m_PageManager.activePage.RemoveSelection(versions.Select(v => new PackageAndVersionIdPair(v.package.uniqueId, v.uniqueId)));
        if (!string.IsNullOrEmpty(m_AnalyticsEventName))
            PackageManagerWindowAnalytics.SendEvent(m_AnalyticsEventName, packageIds: versions.Select(v => v.package.uniqueId));
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_PageManager.activePage.RemoveSelection(new[] { new PackageAndVersionIdPair(version.package.uniqueId, version.uniqueId) });
        return true;
    }

    public override bool IsVisible(IPackageVersion version) => true;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Click to deselect these items from the list.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Deselect");
    }

    public override bool IsInProgress(IPackageVersion version) => false;
}
