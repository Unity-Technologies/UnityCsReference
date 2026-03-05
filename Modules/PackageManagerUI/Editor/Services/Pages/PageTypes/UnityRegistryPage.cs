// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UnityRegistryPage : SimplePageWithPackages
    {
        public const string k_Id = "UnityRegistry";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("Unity Registry");
        public override Icon icon => Icon.UnityRegistryPage;
        public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.UpmSearch;

        public UnityRegistryPage(IPackageDatabase packageDatabase) : base(packageDatabase)
        {
            UpdateSupportedStatuses(new [] { PageFilterStatus.UpdateAvailable }, false);
        }

        public override bool ShouldInclude(IPackage package)
        {
            return package?.isDiscoverable == true
                   && package.versions.AnyMatches(v => v.availableRegistry == RegistryType.UnityRegistry)
                   && !package.versions.AllMatches(v => v.HasTag(PackageTag.BuiltIn));
        }

        public override string GetGroupName(IPackage package)
        {
            return package.versions.AllMatches(v => v.HasTag(PackageTag.Feature)) ? L10n.Tr("Features") : L10n.Tr("Packages");
        }

        protected override void RebuildVisualStateList()
        {
            base.RebuildVisualStateList();

            var hasEntitlementPackagesInPage = visualStates.AnyMatches(v => m_PackageDatabase.GetPackage(v.itemUniqueId)?.hasEntitlements == true);
            var newSupportedStatusFilters = hasEntitlementPackagesInPage
                ? new[] { PageFilterStatus.UpdateAvailable, PageFilterStatus.SubscriptionBased }
                : new[] { PageFilterStatus.UpdateAvailable };
            UpdateSupportedStatuses(newSupportedStatusFilters, true);
        }
    }
}
