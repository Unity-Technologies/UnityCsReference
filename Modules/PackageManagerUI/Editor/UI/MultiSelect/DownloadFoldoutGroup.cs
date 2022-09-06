// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DownloadFoldoutGroup : MultiSelectFoldoutGroup
    {
        public DownloadFoldoutGroup(AssetStoreDownloadManager assetStoreDownloadManager,
                                    AssetStoreCache assetStoreCache,
                                    PackageOperationDispatcher operationDispatcher)
            : base(new PackageDownloadButton(assetStoreDownloadManager, assetStoreCache, operationDispatcher),
                   new PackageCancelDownloadButton(assetStoreDownloadManager, operationDispatcher))
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Download {0}");
            inProgressFoldout.headerTextTemplate = L10n.Tr("Downloading {0}...");
        }
    }
}
