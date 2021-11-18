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
            : base(new PackageDeselectButton(pageManager))
        {
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreCallQueue = assetStoreCallQueue;

            headerTextTemplate = L10n.Tr("Checking update for {0}...");
        }

        public override bool AddPackageVersion(IPackageVersion version)
        {
            var localInfo = m_AssetStoreCache.GetLocalInfo(version.packageUniqueId);
            if (localInfo?.updateInfoFetched == false)
            {
                m_AssetStoreCallQueue.InsertToCheckUpdateQueue(version.packageUniqueId);
                return base.AddPackageVersion(version);
            }
            return false;
        }
    }
}
