// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RemoveFoldoutGroup : PackageMultiSelectFoldoutGroup
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
            if (mainFoldout.items.Count > 0 && mainFoldout.items[0].versions.primary.HasTag(PackageTag.BuiltIn))
                mainFoldout.headerTextTemplate = L10n.Tr("Disable {0}", null);
            else
                mainFoldout.headerTextTemplate = L10n.Tr("Remove {0}", null);

            if (inProgressFoldout.items.Count > 0 && inProgressFoldout.items[0].versions.primary.HasTag(PackageTag.BuiltIn))
                inProgressFoldout.headerTextTemplate = L10n.Tr("Disabling {0}", null);
            else
                inProgressFoldout.headerTextTemplate = L10n.Tr("Removing {0}", null);

            base.Refresh();
        }

        public override bool AddItem(IPackage package)
        {
            return package.versions.primary.HasTag(PackageTag.UpmFormat) && base.AddItem(package);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
