// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class InProgressDropdown : DropdownContent
    {
        internal const int k_Width = 220;
        internal const int k_LineHeight = 32;

        private int m_Height;
        internal override Vector2 windowSize => new Vector2(k_Width, m_Height);

        private class InProgressContainer : VisualElement
        {
            public Label label { get; private set; }
            public Button button { get; private set; }

            public InProgressContainer(Action buttonAction)
            {
                label = new Label();
                Add(label);

                button = new Button() { text = L10n.Tr("View") };
                button.clickable.clicked += buttonAction;
                Add(button);
            }
        }

        private InProgressContainer m_DownloadingContainer;
        private InProgressContainer downloadingContainer => m_DownloadingContainer ??= new InProgressContainer(ViewDownloading);
        private InProgressContainer m_InstallingContainer;
        private InProgressContainer installingContainer => m_InstallingContainer ??= new InProgressContainer(ViewInstalling);
        private InProgressContainer m_EnablingContainer;
        private InProgressContainer enablingContainer => m_EnablingContainer ??= new InProgressContainer(ViewEnabling);

        private ResourceLoader m_ResourceLoader;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private UpmClient m_UpmClient;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private void ResolveDependencies(ResourceLoader resourceLoader,
                                         PackageManagerPrefs packageManagerPrefs,
                                         UpmClient upmClient,
                                         AssetStoreDownloadManager assetStoreDownloadManager,
                                         PackageDatabase packageDatabase,
                                         PageManager packageManager)
        {
            m_ResourceLoader = resourceLoader;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_UpmClient = upmClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
            m_PageManager = packageManager;
        }

        public InProgressDropdown(ResourceLoader resourceLoader,
                                  PackageManagerPrefs packageManagerPrefs,
                                  UpmClient upmClient,
                                  AssetStoreDownloadManager assetStoreDownloadManager,
                                  PackageDatabase packageDatabase,
                                  PageManager packageManager)
        {
            ResolveDependencies(resourceLoader, packageManagerPrefs, upmClient, assetStoreDownloadManager, packageDatabase, packageManager);
            styleSheets.Add(m_ResourceLoader.inProgressDropdownStyleSheet);

            m_Height = 0;
            Refresh();
        }

        private bool Refresh()
        {
            Clear();

            var numDownloading = m_AssetStoreDownloadManager.DownloadInProgressCount();
            var numBuiltInPackagesInstalling = 0;
            var numRegularPackagesInstalling = 0;
            foreach (var idOrName in m_UpmClient.packageIdsOrNamesInstalling)
            {
                var version = m_PackageDatabase.GetPackageByIdOrName(idOrName)?.versions.primary;
                if (version == null)
                    continue;
                if (version.HasTag(PackageTag.BuiltIn))
                    ++numBuiltInPackagesInstalling;
                else
                    ++numRegularPackagesInstalling;
            }

            m_Height = 0;
            if (AddInProgressContainer(installingContainer, numRegularPackagesInstalling, L10n.Tr("Installing {0}")))
                m_Height += k_LineHeight;
            if (AddInProgressContainer(enablingContainer, numBuiltInPackagesInstalling, L10n.Tr("Enabling {0}")))
                m_Height += k_LineHeight;
            if (AddInProgressContainer(downloadingContainer, numDownloading, L10n.Tr("Downloading {0}")))
                m_Height += k_LineHeight;

            return m_Height != 0;
        }

        private bool AddInProgressContainer(InProgressContainer container, int numItemsInProgress, string textFormat)
        {
            if (numItemsInProgress <= 0)
                return false;

            var numItemsText = string.Format(numItemsInProgress > 1 ? L10n.Tr("{0} items") : L10n.Tr("{0} item"), numItemsInProgress);
            container.label.text = string.Format(textFormat, numItemsText);
            Add(container);
            return true;
        }

        private void ViewDownloading()
        {
            ViewPackagesOnTab(PackageFilterTab.AssetStore, PackageProgress.Downloading);
        }

        private void ViewInstalling()
        {
            ViewPackagesOnTab(PackageFilterTab.InProject, PackageProgress.Installing, ~PackageTag.BuiltIn);
        }

        private void ViewEnabling()
        {
            ViewPackagesOnTab(PackageFilterTab.BuiltIn, PackageProgress.Installing, PackageTag.BuiltIn);
        }

        private void ViewPackagesOnTab(PackageFilterTab tab, PackageProgress progress, PackageTag tag = PackageTag.None)
        {
            var packagesInProgress = m_PackageDatabase.allPackages.Where(p =>
                p.progress == progress && (tag == PackageTag.None || p.versions.primary.HasTag(tag))).ToArray();
            if (packagesInProgress.Any())
            {
                m_PackageManagerPrefs.currentFilterTab = tab;
                m_PageManager.GetPage().LoadExtraItems(packagesInProgress);
                m_PageManager.GetPage().SetNewSelection(packagesInProgress.Select(p => new PackageAndVersionIdPair(p.uniqueId)));
            }
            Close();
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            if (args.progressUpdated.Any() || args.added.Any() || args.removed.Any())
            {
                if (Refresh())
                    ShowWithNewWindowSize();
                else
                    Close();
            }
        }

        internal override void OnDropdownShown()
        {
            // Since OnDropdownShown might be called multiple times, we want to make sure the events are not registered multiple times
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        }

        internal override void OnDropdownClosed()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        }
    }
}
