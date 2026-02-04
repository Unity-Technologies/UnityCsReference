// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RemoveFoldoutGroup : MultiSelectFoldoutGroup
    {
        public RemoveFoldoutGroup(IApplicationProxy applicationProxy,
                                  IPackageManagerPrefs packageManagerPrefs,
                                  IPackageDatabase packageDatabase,
                                  IPackageOperationDispatcher operationDispatcher,
                                  IPageManager pageManager)
            : base(new RemoveAction(operationDispatcher, applicationProxy, packageManagerPrefs, packageDatabase, pageManager))
        {
        }

        public override void Refresh()
        {
            if (mainFoldout.packages.Count > 0 && mainFoldout.packages[0].versions.primary.HasTag(PackageTag.BuiltIn))
                mainFoldout.headerTextTemplate = L10n.Tr("Disable {0}");
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Remove {0}");

            if (inProgressFoldout.packages.Count > 0 && inProgressFoldout.packages[0].versions.primary.HasTag(PackageTag.BuiltIn))
                inProgressFoldout.headerTextTemplate = L10n.Tr("Disabling {0}");
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Removing {0}");

            base.Refresh();
        }

        public override bool AddPackage(IPackage package)
        {
            return package.versions.primary.HasTag(PackageTag.UpmFormat) && base.AddPackage(package);
        }
    }
}
