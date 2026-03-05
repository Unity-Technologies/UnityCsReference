// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class BuiltInPage : SimplePageWithPackages
    {
        public const string k_Id = "BuiltIn";

        public static readonly PageSortOption[] k_SupportedSortOptions = { PageSortOption.NameAsc, PageSortOption.NameDesc };

        public override string id => k_Id;
        public override string displayName => L10n.Tr("Built-in");
        public override Icon icon =>  Icon.BuiltInPage;

        // We use UpmSearch instead of UpmSearchOffline, as search offline never returns packages from scoped registry right now.
        // Using UpmSearchOffline here will cause scoped registry packages to disappear when user refreshes the built in page.
        public override RefreshOptions refreshOptions => RefreshOptions.UpmListOffline | RefreshOptions.UpmSearch;

        public BuiltInPage(IPackageDatabase packageDatabase) : base(packageDatabase)
        {
            UpdateSupportedSortOptions(k_SupportedSortOptions, false);
            UpdateSupportedStatuses(Array.Empty<PageFilterStatus>(), false);
        }

        public override bool ShouldInclude(IPackage package)
        {
            return package != null && package.versions.AllMatches(v => v.HasTag(PackageTag.BuiltIn));
        }
    }
}
