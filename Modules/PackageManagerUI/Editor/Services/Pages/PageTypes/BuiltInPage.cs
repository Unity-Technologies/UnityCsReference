// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class BuiltInPage : SimplePage
    {
        public const string k_Id = "BuiltIn";

        public static readonly PageSortOption[] k_SupportedSortOptions = { PageSortOption.NameAsc, PageSortOption.NameDesc };

        public override string id => k_Id;
        public override string displayName => L10n.Tr("Built-in");
        public override Icon icon =>  Icon.BuiltInPage;

        public override RefreshOptions refreshOptions => RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline;

        public override IEnumerable<PageFilters.Status> supportedStatusFilters => Enumerable.Empty<PageFilters.Status>();
        public override IEnumerable<PageSortOption> supportedSortOptions => k_SupportedSortOptions;

        public BuiltInPage(IPackageDatabase packageDatabase) : base(packageDatabase) {}

        public override bool ShouldInclude(IPackage package)
        {
            return package?.versions.All(v => v.HasTag(PackageTag.BuiltIn)) == true;
        }
    }
}
