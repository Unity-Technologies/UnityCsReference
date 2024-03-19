// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class CheckUpdateFoldout : MultiSelectFoldout
    {
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;

        public CheckUpdateFoldout(IPageManager pageManager, IAssetStoreCache assetStoreCache, IBackgroundFetchHandler backgroundFetchHandler)
            : base(new DeselectAction(pageManager, "deselectCheckUpdate"))
        {
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;

            headerTextTemplate = L10n.Tr("Checking update for {0}...");
        }

        public override bool AddPackage(IPackage package)
        {
            var product = package.product;
            if(product == null)
                return false;

            if (m_AssetStoreCache.GetLocalInfo(product.id) != null && m_AssetStoreCache.GetUpdateInfo(product.id) == null)
            {
                m_BackgroundFetchHandler.PushToCheckUpdateStack(product.id);
                return base.AddPackage(package);
            }
            return false;
        }
    }
}
