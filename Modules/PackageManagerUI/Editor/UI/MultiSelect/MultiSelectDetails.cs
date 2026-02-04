// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectDetails : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new MultiSelectDetails(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPackageOperationDispatcher>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IAssetStoreClient>(),
                    container.Resolve<IAssetStoreDownloadManager>(),
                    container.Resolve<IAssetStoreCache>(),
                    container.Resolve<IBackgroundFetchHandler>(),
                    container.Resolve<IUnityConnectProxy>());
            }
        }



        private UnlockFoldout m_UnlockFoldout;
        private NoActionsFoldout m_NoActionFoldout;
        private CheckUpdateFoldout m_CheckUpdateFoldout;

        private MultiSelectFoldout[] m_StandaloneFoldouts;

        private InstallFoldoutGroup m_InstallFoldoutGroup;
        private UpdateFoldoutGroup m_UpdateFoldoutGroup;
        private RemoveFoldoutGroup m_RemoveFoldoutGroup;
        private OpenManifestExternallyFoldoutGroup m_OpenManifestExternallyFoldoutGroup;

        private DownloadFoldoutGroup m_DownloadFoldoutGroup;
        private DownloadUpdateFoldoutGroup m_DownloadUpdateFoldoutGroup;
        private RemoveImportedFoldoutGroup m_RemoveImportedFoldoutGroup;

        private MultiSelectFoldoutGroup[] m_FoldoutGroups;

        private IEnumerable<IMultiSelectFoldoutElement> EnumerateAllFoldoutElements()
        {
            foreach (var element in m_StandaloneFoldouts ?? Array.Empty<IMultiSelectFoldoutElement>())
                yield return element;
            foreach (var element in m_FoldoutGroups  ?? Array.Empty<IMultiSelectFoldoutElement>())
                yield return element;
        }

        private readonly IApplicationProxy m_Application;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IAssetStoreClient m_AssetStoreClient;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        private readonly IUnityConnectProxy m_UnityConnect;

        public MultiSelectDetails(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPageManager pageManager,
            IPackageManagerPrefs packageManagerPrefs,
            IAssetStoreClient assetStoreClient,
            IAssetStoreDownloadManager assetStoreDownloadManager,
            IAssetStoreCache assetStoreCache,
            IBackgroundFetchHandler backgroundFetchHandler,
            IUnityConnectProxy unityConnect)
        {
            m_Application = application;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;
            m_UnityConnect = unityConnect;

            var root = resourceLoader.GetTemplate("MultiSelectDetails.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            lockedPackagesInfoBox.buttonText = L10n.Tr("Deselect all locked packages");
            lockedPackagesInfoBox.onButtonClicked += OnDeselectLockedSelectionsClicked;

            InitializeFoldouts();
        }

        public void OnEnable()
        {
            m_AssetStoreClient.onUpdateChecked += OnUpdateChecked;
        }

        public void OnDisable()
        {
            m_AssetStoreClient.onUpdateChecked -= OnUpdateChecked;
        }

        private void InitializeFoldouts()
        {
            // Standalone foldouts
            m_UnlockFoldout = new UnlockFoldout(m_PageManager);
            m_UnlockFoldout.action.onActionTriggered += Refresh;

            m_NoActionFoldout = new NoActionsFoldout(m_PageManager);
            m_CheckUpdateFoldout = new CheckUpdateFoldout(m_PageManager, m_AssetStoreCache, m_BackgroundFetchHandler);
            m_StandaloneFoldouts = new MultiSelectFoldout[] { m_UnlockFoldout, m_NoActionFoldout, m_CheckUpdateFoldout };

            // Foldout groups
            m_InstallFoldoutGroup = new InstallFoldoutGroup(m_Application, m_PackageDatabase, m_OperationDispatcher);
            m_RemoveFoldoutGroup = new RemoveFoldoutGroup(m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager);
            m_UpdateFoldoutGroup = new UpdateFoldoutGroup(m_Application, m_PackageDatabase, m_OperationDispatcher, m_PageManager);
            m_DownloadFoldoutGroup = new DownloadFoldoutGroup(m_AssetStoreDownloadManager, m_OperationDispatcher, m_UnityConnect, m_Application);
            m_DownloadUpdateFoldoutGroup = new DownloadUpdateFoldoutGroup(m_AssetStoreDownloadManager, m_AssetStoreCache, m_OperationDispatcher, m_UnityConnect, m_Application);
            m_RemoveImportedFoldoutGroup = new RemoveImportedFoldoutGroup(m_Application, m_OperationDispatcher);
            m_OpenManifestExternallyFoldoutGroup = new OpenManifestExternallyFoldoutGroup();

            m_FoldoutGroups = new MultiSelectFoldoutGroup[]
            {
                m_DownloadFoldoutGroup,
                m_DownloadUpdateFoldoutGroup,
                m_OpenManifestExternallyFoldoutGroup,
                m_RemoveImportedFoldoutGroup,
                m_InstallFoldoutGroup,
                m_RemoveFoldoutGroup,
                m_UpdateFoldoutGroup
            };

            // Add foldouts to the UI in the correct order. Note that the order here is not the same as the initialization order from above.
            foldoutsContainer.Add(m_UnlockFoldout);

            foldoutsContainer.Add(m_InstallFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_InstallFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_UpdateFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_UpdateFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_DownloadFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_DownloadFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_DownloadUpdateFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_DownloadUpdateFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_OpenManifestExternallyFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_OpenManifestExternallyFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_RemoveFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_RemoveFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_RemoveImportedFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_RemoveImportedFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_CheckUpdateFoldout);
            foldoutsContainer.Add(m_NoActionFoldout);
        }

        private void OnUpdateChecked(IEnumerable<long> productIds)
        {
            var selection = m_PageManager.activePage.GetSelection();
            if (productIds.AnyMatches(id => selection.Contains(id.ToString())))
                Refresh(selection);
        }

        public bool Refresh(PageSelection selections)
        {
            if (selections.Count <= 1)
                return false;

            title.text = string.Format(L10n.Tr("{0} {1} selected"), selections.Count, selections.Count > 1 ? L10n.Tr("items") : L10n.Tr("item"));

            foreach (var foldoutElement in EnumerateAllFoldoutElements())
                foldoutElement.ClearPackages();

            // We get the versions from the visual states instead of directly from the selection to keep the ordering of packages
            foreach (var visualState in m_PageManager.activePage.visualStates)
            {
                if (!selections.Contains(visualState.packageUniqueId))
                    continue;

                var package = m_PackageDatabase.GetPackage(visualState.packageUniqueId);
                if (m_UnlockFoldout.AddPackage(package))
                    continue;

                if (m_CheckUpdateFoldout.AddPackage(package))
                    continue;

                var isActionable = false;
                foreach (var foldoutGroup in m_FoldoutGroups)
                    isActionable |= foldoutGroup.AddPackage(package);

                if (!isActionable)
                    m_NoActionFoldout.AddPackage(package);
            }

            foreach (var foldoutElement in EnumerateAllFoldoutElements())
                foldoutElement.Refresh();

            UIUtils.SetElementDisplay(infoBoxContainer, m_UnlockFoldout.packages.Count > 0);
            return true;
        }

        private void Refresh()
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnDeselectLockedSelectionsClicked()
        {
            var packageUniqueIds = m_UnlockFoldout.packages.SelectToNewArray(p => p.uniqueId);
            m_PageManager.activePage.RemoveSelection(packageUniqueIds);
            PackageManagerWindowAnalytics.SendEvent("deselectLocked", packageIds: packageUniqueIds);
        }

        private VisualElementCache cache { get; }
        private Label title => cache.Get<Label>("multiSelectTitle");
        private VisualElement infoBoxContainer => cache.Get<VisualElement>("multiSelectInfoBoxContainer");
        private HelpBox lockedPackagesInfoBox => cache.Get<HelpBox>("lockedPackagesInfoBox");
        private VisualElement foldoutsContainer => cache.Get<VisualElement>("multiSelectFoldoutsContainer");
    }
}
