// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageImportButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageImportButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PackageDatabase.Import(version.package);
            PackageManagerWindowAnalytics.SendEvent("import", version.packageUniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.Downloadable) != true)
                return false;

            return version.HasTag(PackageTag.Importable)
                && version.isAvailableOnDisk
                && version.package.state != PackageState.InProgress
                && m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId)?.isProgressVisible != true;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to import assets from the {0} into your project."), version.package.GetDescriptor());
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
