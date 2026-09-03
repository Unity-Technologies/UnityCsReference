// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpdateFoldout : PackageMultiSelectFoldout
    {
        private static readonly string k_UpdateInfoTextFormat = L10n.Tr("Version {0} available", null);

        public UpdateFoldout(IApplicationProxy applicationProxy,
                             IPackageDatabase packageDatabase,
                             IPackageOperationDispatcher operationDispatcher,
                             IPageManager pageManager)
            : base(new UpdateAction(operationDispatcher, applicationProxy, packageDatabase, pageManager))
        {
        }

        protected override MultiSelectItemBase<IPackage> CreateMultiSelectItem(IPackage package)
        {
            var rightInfoText = string.Format(k_UpdateInfoTextFormat, ((UpdateAction) action).GetUpdateTarget(package.versions.primary).version);
            return new PackageMultiSelectItem(package, rightInfoText);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
