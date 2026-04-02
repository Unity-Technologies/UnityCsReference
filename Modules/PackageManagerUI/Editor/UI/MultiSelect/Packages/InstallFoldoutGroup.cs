// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class InstallFoldoutGroup : PackageMultiSelectFoldoutGroup
    {
        public InstallFoldoutGroup(IApplicationProxy applicationProxy, IPackageDatabase packageDatabase, IPackageOperationDispatcher operationDispatcher)
            : base(new AddAction(operationDispatcher, applicationProxy, packageDatabase))
        {
        }

        public override void Refresh()
        {
            if (mainFoldout.items.Count > 0 && mainFoldout.items[0].versions.primary.HasTag(PackageTag.BuiltIn))
                mainFoldout.headerTextTemplate = L10n.Tr("Enable {0}");
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Install {0}");

            if (inProgressFoldout.items.Count > 0 && inProgressFoldout.items[0].versions.primary.HasTag(PackageTag.BuiltIn))
                inProgressFoldout.headerTextTemplate = L10n.Tr("Enabling {0}");
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Installing {0}");

            base.Refresh();
        }

        public override bool AddItem(IPackage package)
        {
            if (!package.versions.primary.HasTag(PackageTag.UpmFormat))
                return false;
            return base.AddItem(package);
        }
    }
}
