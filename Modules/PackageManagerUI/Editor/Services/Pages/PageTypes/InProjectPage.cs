// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class InProjectPage : SimplePageWithPackages
{
    public const string k_Id = "InProject";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("All Packages");
    public override Icon icon => Icon.InProjectPage;

    public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.ImportedAssets | RefreshOptions.LocalInfo | RefreshOptions.ImportedSamples;

    public InProjectPage(IPackageDatabase packageDatabase) : base(packageDatabase)
    {
        UpdateSupportedStatuses(Array.Empty<PageFilterStatus>(), false);
    }

    public override bool ShouldInclude(IPackage package)
    {
        return package != null
            && !package.versions.AllMatches(v => v.HasTag(PackageTag.BuiltIn))
            && (package.progress == PackageProgress.Installing || package.versions.installed != null || package.versions.imported != null);
    }

    public override string GetGroupName(IPackage package)
    {
        if (package.product != null)
            return L10n.Tr("Packages - Asset Store");
        var version = package.versions.primary;
        if (version.HasTag(PackageTag.Unity))
            return version.HasTag(PackageTag.Feature) ? L10n.Tr("Features") : L10n.Tr("Packages - Unity");
        return string.IsNullOrEmpty(version.author?.name) ? L10n.Tr("Packages - Other") : string.Format(L10n.Tr("Packages - {0}"), version.author.name);
    }

    protected override void RebuildVisualStateList()
    {
        base.RebuildVisualStateList();

        var hasEntitlementPackagesInPage = visualStates.AnyMatches(v => m_PackageDatabase.GetPackage(v.itemUniqueId)?.hasEntitlements == true);
        var newSupportedStatusFilters = hasEntitlementPackagesInPage
            ? new[] { PageFilterStatus.SubscriptionBased }
            : Array.Empty<PageFilterStatus>();
        UpdateSupportedStatuses(newSupportedStatusFilters, true);
    }
}
