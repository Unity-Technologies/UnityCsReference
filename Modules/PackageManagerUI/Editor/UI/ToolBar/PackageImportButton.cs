// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageImportButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageOperationDispatcher m_OperationDispatcher;
        public PackageImportButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageOperationDispatcher operationDispatcher)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_OperationDispatcher = operationDispatcher;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_OperationDispatcher.Import(version.package);
            PackageManagerWindowAnalytics.SendEvent("import", version.package.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            return version?.HasTag(PackageTag.LegacyFormat) == true
                && version.isAvailableOnDisk
                && version.package.progress == PackageProgress.None
                && m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id)?.isProgressVisible != true;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to import assets from the {0} into your project."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Import");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and can not be imported anymore."));
        }
    }
}
