// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectDetails : BaseDetailsView
    {
        private UnlockFoldout m_UnlockFoldout;
        private NoActionsOnPackagesFoldout m_NoActionOnPackagesFoldout;
        private CheckUpdateFoldout m_CheckUpdateFoldout;

        private PackageMultiSelectFoldout[] m_PackageStandaloneFoldouts;

        private NoActionsOnSamplesFoldout m_NoActionOnSamplesFoldout;

        private SampleMultiSelectFoldout[] m_SampleStandaloneFoldouts;

        private InstallFoldoutGroup m_InstallFoldoutGroup;
        private UpdateFoldoutGroup m_UpdateFoldoutGroup;
        private RemoveFoldoutGroup m_RemoveFoldoutGroup;
        private OpenManifestExternallyFoldoutGroup m_OpenManifestExternallyFoldoutGroup;

        private DownloadFoldoutGroup m_DownloadFoldoutGroup;
        private DownloadUpdateFoldoutGroup m_DownloadUpdateFoldoutGroup;
        private RemoveImportedFoldoutGroup m_RemoveImportedFoldoutGroup;

        private PackageMultiSelectFoldoutGroup[] m_FoldoutGroups;

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
        public MultiSelectDetails(IResourceLoader resourceLoader,
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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void InitializeFoldouts()
        {
            // Package Standalone foldouts
            m_UnlockFoldout = new UnlockFoldout(m_PageManager);

            m_NoActionOnPackagesFoldout = new NoActionsOnPackagesFoldout(m_PageManager);
            m_CheckUpdateFoldout = new CheckUpdateFoldout(m_PageManager, m_AssetStoreCache, m_BackgroundFetchHandler);
            m_PackageStandaloneFoldouts = new PackageMultiSelectFoldout[] { m_UnlockFoldout, m_NoActionOnPackagesFoldout, m_CheckUpdateFoldout };

            // Sample Standalone foldouts
            m_NoActionOnSamplesFoldout = new NoActionsOnSamplesFoldout(m_PageManager);
            m_SampleStandaloneFoldouts = new SampleMultiSelectFoldout[] { m_NoActionOnSamplesFoldout };

            // Foldout groups
            m_InstallFoldoutGroup = new InstallFoldoutGroup(m_Application, m_PackageDatabase, m_OperationDispatcher);
            m_RemoveFoldoutGroup = new RemoveFoldoutGroup(m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager);
            m_UpdateFoldoutGroup = new UpdateFoldoutGroup(m_Application, m_PackageDatabase, m_OperationDispatcher, m_PageManager);
            m_DownloadFoldoutGroup = new DownloadFoldoutGroup(m_AssetStoreDownloadManager, m_OperationDispatcher, m_UnityConnect, m_Application);
            m_DownloadUpdateFoldoutGroup = new DownloadUpdateFoldoutGroup(m_AssetStoreDownloadManager, m_AssetStoreCache, m_OperationDispatcher, m_UnityConnect, m_Application);
            m_RemoveImportedFoldoutGroup = new RemoveImportedFoldoutGroup(m_Application, m_OperationDispatcher);
            m_OpenManifestExternallyFoldoutGroup = new OpenManifestExternallyFoldoutGroup();

            m_FoldoutGroups = new PackageMultiSelectFoldoutGroup[]
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
            foldoutsContainer.Add(m_NoActionOnPackagesFoldout);

            foldoutsContainer.Add(m_NoActionOnSamplesFoldout);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_AssetStoreClient.onUpdateChecked += OnUpdateChecked;

            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_AssetStoreClient.onUpdateChecked -= OnUpdateChecked;

            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;
        }

        private void OnListRebuild(IPage page)
        {
            if (page.isActive)
                Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnListUpdate(ListUpdateArgs args)
        {
            if (args.page.isActive)
                Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnUpdateChecked(IReadOnlyCollection<long> productIds)
        {
            var selection = m_PageManager.activePage.GetSelection();
            if (productIds.Exists(id => selection.Contains(id.ToString())))
                Refresh(selection);
        }

        public override void Refresh(PageSelection selections)
        {
            title.text = string.Format(L10n.Tr("{0} {1} selected"), selections.Count, selections.Count > 1 ? L10n.Tr("items") : L10n.Tr("item"));

            ClearAllFoldouts();

            if (m_PageManager.activePage.id == SamplesPage.k_Id)
                RefreshSampleFoldoutElements(selections);
            else
                RefreshPackageFoldoutElements(selections);

            RefreshAllFoldouts();

            UIUtils.SetElementDisplay(infoBoxContainer, m_UnlockFoldout.items.Count > 0);
        }

        private void ClearAllFoldouts()
        {
            foreach (var foldout in m_SampleStandaloneFoldouts)
                foldout.ClearItems();

            foreach (var foldout in m_PackageStandaloneFoldouts)
                foldout.ClearItems();

            foreach (var foldout in m_FoldoutGroups)
                foldout.ClearItems();
        }

        private void RefreshAllFoldouts()
        {
            foreach (var foldout in m_SampleStandaloneFoldouts)
                foldout.Refresh();

            foreach (var foldout in m_PackageStandaloneFoldouts)
                foldout.Refresh();

            foreach (var foldout in m_FoldoutGroups)
                foldout.Refresh();
        }

        private void RefreshPackageFoldoutElements(PageSelection selections)
        {
            // We get the versions from the visual states instead of directly from the selection to keep the ordering of packages
            foreach (var visualState in m_PageManager.activePage.visualStates)
            {
                if (!selections.Contains(visualState.itemUniqueId))
                    continue;

                var package = m_PackageDatabase.GetPackage(visualState.itemUniqueId);

                if (package == null)
                    continue;

                if (m_UnlockFoldout.AddItem(package))
                    continue;

                if (m_CheckUpdateFoldout.AddItem(package))
                    continue;

                var isActionable = false;
                foreach (var foldoutGroup in m_FoldoutGroups)
                    isActionable |= foldoutGroup.AddItem(package);

                if (!isActionable)
                    m_NoActionOnPackagesFoldout.AddItem(package);
            }
        }

        private void RefreshSampleFoldoutElements(PageSelection selections)
        {
            foreach (var visualState in m_PageManager.activePage.visualStates)
            {
                if (!selections.Contains(visualState.itemUniqueId))
                    continue;

                var sample = m_PackageDatabase.GetSample(visualState.itemUniqueId);
                if (!sample.isDefault)
                    m_NoActionOnSamplesFoldout.AddItem(sample);
            }
        }

        private void OnDeselectLockedSelectionsClicked()
        {
            var packageUniqueIds = m_UnlockFoldout.items.SelectToNewArray(p => p.uniqueId);
            m_PageManager.activePage.RemoveSelection(packageUniqueIds, false);
            PackageManagerWindowAnalytics.SendEvent("deselectLocked", packageIds: packageUniqueIds);
        }

        private VisualElementCache cache { get; }
        private Label title => cache.Get<Label>("multiSelectTitle");
        private VisualElement infoBoxContainer => cache.Get<VisualElement>("multiSelectInfoBoxContainer");
        private HelpBox lockedPackagesInfoBox => cache.Get<HelpBox>("lockedPackagesInfoBox");
        private VisualElement foldoutsContainer => cache.Get<VisualElement>("multiSelectFoldoutsContainer");
    }
}
