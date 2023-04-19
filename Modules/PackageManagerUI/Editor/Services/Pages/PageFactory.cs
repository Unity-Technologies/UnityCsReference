// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PageFactory
    {
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [ExcludeFromCodeCoverage]
        public void ResolveDependencies(UnityConnectProxy unityConnect,
                                        PackageManagerPrefs packageManagerPrefs,
                                        AssetStoreClientV2 assetStoreClient,
                                        PackageDatabase packageDatabase,
                                        UpmCache upmCache)
        {
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_PackageDatabase = packageDatabase;
            m_UpmCache = upmCache;
        }

        public virtual IPage CreatePageFromId(string pageId)
        {
            return pageId switch
            {
                UnityRegistryPage.k_Id => new UnityRegistryPage(m_PackageDatabase),
                InProjectPage.k_Id => new InProjectPage(m_PackageDatabase),
                InProjectUpdatesPage.k_Id => new InProjectUpdatesPage(m_PackageDatabase),
                BuiltInPage.k_Id => new BuiltInPage(m_PackageDatabase),
                MyRegistriesPage.k_Id => new MyRegistriesPage(m_PackageDatabase),
                MyAssetsPage.k_Id => new MyAssetsPage(m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient),
                _ => null
            };
        }

        public virtual IPage CreateExtensionPage(ExtensionPageArgs args)
        {
            return new ExtensionPage(m_PackageDatabase, args);
        }

        public virtual IPage CreateScopedRegistryPage(RegistryInfo registryInfo)
        {
            return new ScopedRegistryPage(m_PackageDatabase, m_UpmCache, registryInfo);
        }

        [ExcludeFromCodeCoverage]
        public virtual void ResolveDependenciesForPage(IPage page)
        {
            if (m_PackageDatabase == null || m_PackageManagerPrefs == null || m_UnityConnect == null || m_AssetStoreClient == null || m_UpmCache == null)
            {
                Debug.LogError("PageFactory's dependencies need to resolved before ResolveDependenciesForPage is called.");
                return;
            }
            switch (page)
            {
                case MyAssetsPage myAssetsPage:
                    myAssetsPage.ResolveDependencies(m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient);
                    break;
                case ScopedRegistryPage scopedRegistryPage:
                    scopedRegistryPage.ResolveDependencies(m_PackageDatabase, m_UpmCache);
                    break;
                case SimplePage simplePage:
                    simplePage.ResolveDependencies(m_PackageDatabase);
                    break;
            }
        }
    }
}
