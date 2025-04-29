// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class MyRegistriesPage : SimplePage
    {
        public const string k_Id = "MyRegistries";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("My Registries");
        public override Icon icon => Icon.MyRegistriesPage;

        public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.UpmSearch;

        public MyRegistriesPage(IPackageDatabase packageDatabase) : base(packageDatabase) {}

        public override bool ShouldInclude(IPackage package)
        {
            return package.versions.Any(v => v.availableRegistry == RegistryType.MyRegistries);
        }

        public override string GetGroupName(IPackage package)
        {
            var version = package.versions.primary;
            return string.IsNullOrEmpty(version.author) ? L10n.Tr("Other") : version.author;
        }
    }
}
