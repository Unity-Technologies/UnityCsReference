// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class UnlockAction : PackageAction
{
    private readonly IPageManager m_PageManager;
    public UnlockAction(IPageManager pageManager)
    {
        m_PageManager = pageManager;
    }

    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages)
    {
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        var packageUniqueIds = packages.Select(p => p.uniqueId).ToArray();
#pragma warning restore RS0030
        m_PageManager.activePage.SetPackagesUserUnlockedState(packageUniqueIds, true);
        PackageManagerWindowAnalytics.SendEvent("unlock", packageIds: packageUniqueIds);
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_PageManager.activePage.SetPackagesUserUnlockedState(new string[1] { version.package.uniqueId }, true);
        PackageManagerWindowAnalytics.SendEvent("unlock", version.package.uniqueId);
        return true;
    }

    public override bool IsVisible(IPackageVersion version) => m_PageManager.activePage.visualStates.Get(version?.package?.uniqueId)?.isLocked == true;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Unlock to make changes");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Unlock");
    }

    public override bool IsInProgress(IPackageVersion version) => false;
}
