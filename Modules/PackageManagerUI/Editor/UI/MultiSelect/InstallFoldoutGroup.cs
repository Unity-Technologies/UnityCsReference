// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class InstallFoldoutGroup : MultiSelectFoldoutGroup
    {
        public InstallFoldoutGroup(IApplicationProxy applicationProxy, IPackageDatabase packageDatabase, IPackageOperationDispatcher operationDispatcher)
            : base(new AddAction(operationDispatcher, applicationProxy, packageDatabase))
        {
        }

        public override void Refresh()
        {
            if (mainFoldout.packages.Count > 0 && mainFoldout.packages[0].versions.primary.HasTag(PackageTag.BuiltIn))
                mainFoldout.headerTextTemplate = L10n.Tr("Enable {0}");
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Install {0}");

            if (inProgressFoldout.packages.Count > 0 && inProgressFoldout.packages[0].versions.primary.HasTag(PackageTag.BuiltIn))
                inProgressFoldout.headerTextTemplate = L10n.Tr("Enabling {0}");
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Installing {0}");

            base.Refresh();
        }

        public override bool AddPackage(IPackage package)
        {
            if (!package.versions.primary.HasTag(PackageTag.UpmFormat))
                return false;
            return base.AddPackage(package);
        }
    }
}
