// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DownloadUpdateFoldoutGroup : PackageMultiSelectFoldoutGroup
    {
        public DownloadUpdateFoldoutGroup(IAssetStoreDownloadManager assetStoreDownloadManager,
                                          IAssetStoreCache assetStoreCache,
                                          IPackageOperationDispatcher operationDispatcher,
                                          IUnityConnectProxy unityConnect,
                                          IApplicationProxy application)
            : base(new DownloadUpdateAction(operationDispatcher, assetStoreDownloadManager, unityConnect, application),
                   new CancelDownloadAction(operationDispatcher, assetStoreDownloadManager, application))
        {
            mainFoldout.headerTextTemplate = L10n.Tr("Download update for {0}", null);
            inProgressFoldout.headerTextTemplate = L10n.Tr("Downloading updates for {0}...", null);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
