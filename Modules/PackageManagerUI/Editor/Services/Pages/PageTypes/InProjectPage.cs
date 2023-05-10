// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class InProjectPage : SimplePage
{
    public const string k_Id = "InProject";

    public override string id => k_Id;
    public override string displayName => L10n.Tr("In Project");
    public override PageIcon icon => PageIcon.InProject;

    [SerializeField]
    private PageFilters.Status[] m_SupportedStatusFilters = Array.Empty<PageFilters.Status>();
    public override IEnumerable<PageFilters.Status> supportedStatusFilters => m_SupportedStatusFilters;

    public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.ImportedAssets;
    public override PageCapability capability => PageCapability.DynamicEntitlementStatus | PageCapability.SupportLocalReordering;

    public InProjectPage(PackageDatabase packageDatabase) : base(packageDatabase) {}

    public override bool ShouldInclude(IPackage package)
    {
        return package != null
            && !package.versions.All(v => v.HasTag(PackageTag.BuiltIn))
            && (package.progress == PackageProgress.Installing || package.versions.installed != null || package.versions.Any(v => v.importedAssets?.Any() == true));
    }

    public override string GetGroupName(IPackage package)
    {
        if (package.product != null)
            return L10n.Tr("Packages - Asset Store");
        var version = package.versions.primary;
        if (version.HasTag(PackageTag.Unity))
            return version.HasTag(PackageTag.Feature) ? L10n.Tr("Features") : L10n.Tr("Packages - Unity");
        return string.IsNullOrEmpty(version.author) ? L10n.Tr("Packages - Other") : string.Format(L10n.Tr("Packages - {0}"), version.author);
    }

    public override bool RefreshSupportedStatusFiltersOnEntitlementPackageChange()
    {
        var oldSupportedStatusFilters = m_SupportedStatusFilters;
        m_SupportedStatusFilters = visualStates.Any(v => m_PackageDatabase.GetPackage(v.packageUniqueId)?.hasEntitlements == true)
            ? new[] { PageFilters.Status.SubscriptionBased }
            : Array.Empty<PageFilters.Status>();

        return !m_SupportedStatusFilters.SequenceEqual(oldSupportedStatusFilters);
    }
}
