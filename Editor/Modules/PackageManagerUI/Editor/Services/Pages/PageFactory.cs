// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPageFactory : IService
    {
        IPage CreatePageFromId(string pageId);
        IPage CreateExtensionPage(ExtensionPageArgs args);
        IPage CreateMyRegistriesPage();
        IPage CreateScopedRegistryPage(RegistryInfo registryInfo);
        void ResolveDependenciesForPage(IPage page);
    }

    internal class PageFactory : BaseService<IPageFactory>, IPageFactory
    {
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IAssetStoreClient m_AssetStoreClient;
        private readonly IAssetStoreRestAPI m_AssetStoreRestAPI;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IUpmCache m_UpmCache;
        [ExcludeFromCodeCoverage]
        public PageFactory(IUnityConnectProxy unityConnect,
                           IPackageManagerPrefs packageManagerPrefs,
                           IAssetStoreClient assetStoreClient,
                           IAssetStoreRestAPI assetStoreRestAPI,
                           IPackageDatabase packageDatabase,
                           IUpmCache upmCache)
        {
            m_UnityConnect = RegisterDependency(unityConnect);
            m_PackageManagerPrefs = RegisterDependency(packageManagerPrefs);
            m_AssetStoreClient = RegisterDependency(assetStoreClient);
            m_AssetStoreRestAPI = RegisterDependency(assetStoreRestAPI);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_UpmCache = RegisterDependency(upmCache);
        }

        public IPage CreatePageFromId(string pageId)
        {
            return pageId switch
            {
                UnityRegistryPage.k_Id => new UnityRegistryPage(m_PackageDatabase),
                InProjectPage.k_Id => new InProjectPage(m_PackageDatabase),
                InProjectUpdatesPage.k_Id => new InProjectUpdatesPage(m_PackageDatabase),
                InProjectNonCompliancePage.k_Id => new InProjectNonCompliancePage(m_PackageDatabase),
                InProjectErrorsAndWarningsPage.k_Id => new InProjectErrorsAndWarningsPage(m_PackageDatabase),
                SamplesPage.k_Id => new SamplesPage(m_PackageDatabase),
                BuiltInPage.k_Id => new BuiltInPage(m_PackageDatabase),
                MyAssetsPage.k_Id => new MyAssetsPage(m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient, m_AssetStoreRestAPI),
                _ => null
            };
        }

        public IPage CreateExtensionPage(ExtensionPageArgs args)
        {
            return new ExtensionPage(m_PackageDatabase, args);
        }

        public IPage CreateMyRegistriesPage()
        {
            return new MyRegistriesPage(m_PackageDatabase);
        }

        public IPage CreateScopedRegistryPage(RegistryInfo registryInfo)
        {
            return new ScopedRegistryPage(m_PackageDatabase, m_UpmCache, registryInfo);
        }

        [ExcludeFromCodeCoverage]
        public void ResolveDependenciesForPage(IPage page)
        {
            switch (page)
            {
                case MyAssetsPage myAssetsPage:
                    myAssetsPage.ResolveDependencies(m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient, m_AssetStoreRestAPI);
                    break;
                case ScopedRegistryPage scopedRegistryPage:
                    scopedRegistryPage.ResolveDependencies(m_PackageDatabase, m_UpmCache);
                    break;
                case SimplePageWithPackages simplePage:
                    simplePage.ResolveDependencies(m_PackageDatabase);
                    break;
                case SamplesPage samplesPage:
                    samplesPage.ResolveDependencies(m_PackageDatabase);
                    break;
            }
        }
    }
}
