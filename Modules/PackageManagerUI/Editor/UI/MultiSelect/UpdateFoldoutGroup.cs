// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpdateFoldoutGroup : MultiSelectFoldoutGroup
    {
        public UpdateFoldoutGroup(IApplicationProxy applicationProxy,
                                  IPackageDatabase packageDatabase,
                                  IPackageOperationDispatcher operationDispatcher,
                                  IPageManager pageManager)
            : base(new UpdateFoldout(applicationProxy, packageDatabase, operationDispatcher, pageManager), new MultiSelectFoldout())
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Update {0}");
            inProgressFoldout.headerTextTemplate = L10n.Tr("Updating {0}...");
        }

        public override bool AddPackage(IPackage package)
        {
            if (!package.versions.primary.HasTag(PackageTag.UpmFormat))
                return false;
            return base.AddPackage(package);
        }
    }
}
