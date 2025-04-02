// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpdateFoldout : MultiSelectFoldout
    {
        private static readonly string k_UpdateInfoTextFormat = L10n.Tr("Version {0} available");

        public UpdateFoldout(IApplicationProxy applicationProxy,
                             IPackageDatabase packageDatabase,
                             IPackageOperationDispatcher operationDispatcher,
                             IPageManager pageManager)
            : base(new UpdateAction(operationDispatcher, applicationProxy, packageDatabase, pageManager))
        {
        }

        protected override MultiSelectItem CreateMultiSelectItem(IPackage package)
        {
            var rightInfoText = string.Format(k_UpdateInfoTextFormat, ((UpdateAction)action).GetUpdateTarget(package.versions.primary).version);
            return new MultiSelectItem(package, rightInfoText);
        }
    }
}
