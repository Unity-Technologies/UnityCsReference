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
        public override Icon icon => Icon.UnityRegistryPage;
        public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.UpmSearch;
        public override PageCapability capability => PageCapability.DynamicEntitlementStatus | PageCapability.SupportLocalReordering;

        [SerializeField]
        private PageFilters.Status[] m_SupportedStatusFilters = { PageFilters.Status.UpdateAvailable };
        public override IReadOnlyCollection<PageFilters.Status> supportedStatusFilters => m_SupportedStatusFilters;

        public UnityRegistryPage(IPackageDatabase packageDatabase) : base(packageDatabase) {}

        public override bool ShouldInclude(IPackage package)
        {
            return package?.isDiscoverable == true
                   #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                   && package.versions.Any(v => v.availableRegistry == RegistryType.UnityRegistry)
#pragma warning restore RS0030
                   #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                   && !package.versions.All(v => v.HasTag(PackageTag.BuiltIn));
#pragma warning restore RS0030
        }

        public override string GetGroupName(IPackage package)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return package.versions.All(v => v.HasTag(PackageTag.Feature)) ? L10n.Tr("Features") : L10n.Tr("Packages");
#pragma warning restore RS0030
        }

        public override bool RefreshSupportedStatusFiltersOnEntitlementPackageChange()
        {
            var oldSupportedStatusFilters = m_SupportedStatusFilters;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SupportedStatusFilters = m_PackageDatabase.allPackages.Any(p => ShouldInclude(p) && p.hasEntitlements)
#pragma warning restore RS0030
                ? new[] { PageFilters.Status.UpdateAvailable, PageFilters.Status.SubscriptionBased }
                : new[] { PageFilters.Status.UpdateAvailable };

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return !m_SupportedStatusFilters.SequenceEqual(oldSupportedStatusFilters);
#pragma warning restore RS0030
        }
    }
}
