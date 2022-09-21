// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class CheckUpdateFoldout : MultiSelectFoldout
    {
        private AssetStoreCache m_AssetStoreCache;
        private AssetStoreCallQueue m_AssetStoreCallQueue;

        public CheckUpdateFoldout(PageManager pageManager, AssetStoreCache assetStoreCache, AssetStoreCallQueue assetStoreCallQueue)
            : base(new PackageDeselectButton(pageManager, "deselectCheckUpdate"))
        {
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreCallQueue = assetStoreCallQueue;

            headerTextTemplate = L10n.Tr("Checking update for {0}...");
        }

        public override bool AddPackageVersion(IPackageVersion version)
        {
            var product = version.package.product;
            if(product == null)
                return false;

            if (m_AssetStoreCache.GetLocalInfo(product.id) != null && m_AssetStoreCache.GetUpdateInfo(product.id) == null)
            {
                m_AssetStoreCallQueue.InsertToCheckUpdateQueue(product.id);
                return base.AddPackageVersion(version);
            }
            return false;
        }
    }
}
