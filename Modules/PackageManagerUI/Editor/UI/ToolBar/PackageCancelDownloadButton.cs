// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageCancelDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageCancelDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            m_PackageDatabase.AbortDownload(versions.Select(v => v.package));
            PackageManagerWindowAnalytics.SendEvent("abortDownload", packageIds: versions.Select(v => v.packageUniqueId));
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PackageDatabase.AbortDownload(version.package);
            PackageManagerWindowAnalytics.SendEvent("abortDownload", version.packageUniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.Downloadable) != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId);
            return operation?.isProgressVisible == true;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId);
            var resumeRequested = operation?.state == DownloadState.ResumeRequested;
            yield return new ButtonDisableCondition(resumeRequested,
                L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed."));
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to cancel the download of this {0}."), version.package.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Cancel");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
