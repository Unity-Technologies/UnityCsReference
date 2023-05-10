// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UnityRegistryPage : SimplePage
    {
        public const string k_Id = "UnityRegistry";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("Unity Registry");
        public override PageIcon icon => PageIcon.UnityRegistry;
        public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.UpmSearch;
        public override PageCapability capability => PageCapability.DynamicEntitlementStatus | PageCapability.SupportLocalReordering;

        [SerializeField]
        private PageFilters.Status[] m_SupportedStatusFilters = { PageFilters.Status.UpdateAvailable };
        public override IEnumerable<PageFilters.Status> supportedStatusFilters => m_SupportedStatusFilters;

        public UnityRegistryPage(PackageDatabase packageDatabase) : base(packageDatabase) {}

        public override bool ShouldInclude(IPackage package)
        {
            return package?.isDiscoverable == true
                   && package.versions.Any(v => v.availableRegistry == RegistryType.UnityRegistry)
                   && !package.versions.All(v => v.HasTag(PackageTag.BuiltIn));
        }

        public override string GetGroupName(IPackage package)
        {
            return package.versions.All(v => v.HasTag(PackageTag.Feature)) ? L10n.Tr("Features") : L10n.Tr("Packages");
        }

        public override bool RefreshSupportedStatusFiltersOnEntitlementPackageChange()
        {
            var oldSupportedStatusFilters = m_SupportedStatusFilters;
            m_SupportedStatusFilters = visualStates.Any(v => m_PackageDatabase.GetPackage(v.packageUniqueId)?.hasEntitlements == true)
                ? new[] { PageFilters.Status.UpdateAvailable, PageFilters.Status.SubscriptionBased }
                : new[] { PageFilters.Status.UpdateAvailable };

            return !m_SupportedStatusFilters.SequenceEqual(oldSupportedStatusFilters);
        }
    }
}
