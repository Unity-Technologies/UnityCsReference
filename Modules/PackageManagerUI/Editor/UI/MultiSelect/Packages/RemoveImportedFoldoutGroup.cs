// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RemoveImportedFoldoutGroup : PackageMultiSelectFoldoutGroup
    {
        public RemoveImportedFoldoutGroup(IApplicationProxy applicationProxy, IPackageOperationDispatcher operationDispatcher)
            : base(new RemoveImportedAction(operationDispatcher, applicationProxy))
        {
        }

        public override void Refresh()
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Remove imported assets from {0}", null);
            inProgressFoldout.headerTextTemplate = L10n.Tr("Removing imported assets from {0}", null);
            base.Refresh();
        }

        public override bool AddItem(IPackage package)
        {
            return package.versions.imported != null && base.AddItem(package);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
