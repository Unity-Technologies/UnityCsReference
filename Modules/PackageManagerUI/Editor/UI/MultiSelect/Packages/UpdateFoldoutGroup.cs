// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpdateFoldoutGroup : PackageMultiSelectFoldoutGroup
    {
        public UpdateFoldoutGroup(IApplicationProxy applicationProxy,
                                  IPackageDatabase packageDatabase,
                                  IPackageOperationDispatcher operationDispatcher,
                                  IPageManager pageManager)
            : base(new UpdateFoldout(applicationProxy, packageDatabase, operationDispatcher, pageManager), new PackageMultiSelectFoldout())
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Update {0}", null);
            inProgressFoldout.headerTextTemplate = L10n.Tr("Updating {0}...", null);
        }

        public override bool AddItem(IPackage package)
        {
            if (!package.versions.primary.HasTag(PackageTag.UpmFormat))
                return false;
            return base.AddItem(package);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
