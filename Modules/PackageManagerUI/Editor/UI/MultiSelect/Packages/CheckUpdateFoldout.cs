// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
namespace UnityEditor.PackageManager.UI.Internal
{
    internal class CheckUpdateFoldout : PackageMultiSelectFoldout
    {
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;

        public CheckUpdateFoldout(IPageManager pageManager, IAssetStoreCache assetStoreCache, IBackgroundFetchHandler backgroundFetchHandler)
            : base(new DeselectPackageAction(pageManager, "deselectCheckUpdate"))
        {
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;

            headerTextTemplate = L10n.Tr("Checking update for {0}...", null);
        }

        public override bool AddItem(IPackage package)
        {
            var product = package.product;
            if (product == null)
                return false;

            if (m_AssetStoreCache.GetLocalInfo(product.id) != null && m_AssetStoreCache.GetUpdateInfo(product.id) == null)
            {
                m_BackgroundFetchHandler.PushToCheckUpdateStack(product.id);
                return base.AddItem(package);
            }
            return false;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
