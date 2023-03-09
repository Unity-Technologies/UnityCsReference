// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UnityRegistryPage : SimplePage
    {
        public const string k_Id = "UnityRegistry";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("Unity Registry");
        public override RefreshOptions refreshOptions => RefreshOptions.UpmList | RefreshOptions.UpmSearch;

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
    }
}
