// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsVersionHistoryItem : VisualElement
    {
        public IPackageVersion version => m_Version;
        public bool expanded => versionHistoryItemToggle.value;
        public PackageToolBarButton button => m_Button;

        private IPackageVersion m_Version;
        private readonly PackageToolBarRegularButton m_Button;
        private readonly PackageDatabase m_PackageDatabase;
        private readonly PackageOperationDispatcher m_OperationDispatcher;
        private readonly UpmCache m_UpmCache;
        private readonly ApplicationProxy m_ApplicationProxy;
        private readonly IOProxy m_IOProxy;

        public event Action<bool> onToggleChanged = delegate {};

        public PackageDetailsVersionHistoryItem(ResourceLoader resourceLoader,
            PackageDatabase packageDatabase,
            PackageOperationDispatcher operationDispatcher,
            UpmCache upmCache,
            ApplicationProxy applicationProxy,
            IOProxy ioProxy,
            IPackageVersion version,
            bool multipleVersionsVisible,
            bool isLatestVersion,
            bool expanded,
            PackageToolBarRegularButton button)
        {
            m_Version = version;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_UpmCache = upmCache;
            m_ApplicationProxy = applicationProxy;
            m_IOProxy = ioProxy;

            var root = resourceLoader.GetTemplate("PackageDetailsVersionHistoryItem.uxml");
            Add(root);
            m_Cache = new VisualElementCache(root);

            SetExpanded(expanded);
            versionHistoryItemToggle.RegisterValueChangedCallback(evt =>
            {
                SetExpanded(evt.newValue);
                onToggleChanged?.Invoke(evt.newValue);
            });

            m_Button = button;
            if (m_Button != null)
                versionHistoryItemToggleRightContainer.Add(m_Button.element);

            versionHistoryItemChangeLogLink.clickable.clicked += VersionHistoryItemChangeLogClicked;

            Refresh(multipleVersionsVisible, isLatestVersion);
        }

        public void StopSpinner()
        {
            if (m_Version?.isFullyFetched == false)
                versionHistoryItemToggleSpinner?.Stop();
        }

        private void VersionHistoryItemChangeLogClicked()
        {
            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            var isUnityPackage = m_Version?.isUnityPackage == true;
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetChangelogUrl(packageInfo, isUnityPackage), UpmPackageDocs.GetOfflineChangelog(m_IOProxy, packageInfo), L10n.Tr("changelog"), "viewChangelog", m_Version, m_Version.package, m_ApplicationProxy);
        }

        private void Refresh(bool multipleVersionsVisible, bool isLatestVersion)
        {
            var isVisible = m_Version != null;
            UIUtils.SetElementDisplay(this, isVisible);
            if (!isVisible)
                return;

            RefreshHeader(multipleVersionsVisible, isLatestVersion);
            RefreshContent();
            m_Button?.Refresh(m_Version);
        }

        private void RefreshHeader(bool multipleVersionsVisible, bool isLatestVersion)
        {
            versionHistoryItemToggle.text = m_Version?.version?.ToString() ?? m_Version?.versionString;
            versionHistoryItemTag.Refresh(m_Version, true);
            UIUtils.SetElementDisplay(versionHistoryItemTag, versionHistoryItemTag.tag != PackageTag.None);

            RefreshState(multipleVersionsVisible, isLatestVersion);
        }

        private void RefreshState(bool multipleVersionsVisible, bool isLatestVersion)
        {
            if (m_Version == null)
            {
                versionHistoryItemState.text = string.Empty;
                return;
            }

            var primary = m_Version.package.versions.primary;
            var recommended = m_Version.package.versions.recommended;
            var versionInManifest = primary?.versionInManifest;
            var stateText = string.Empty;

            if (m_Version == primary)
            {
                if (m_Version.isInstalled)
                    stateText = L10n.Tr(m_Version.isDirectDependency ? "Installed" : "Installed as dependency");
                else if (m_Version == recommended && multipleVersionsVisible && m_Version.isUnityPackage)
                    stateText = L10n.Tr("Recommended");
                else if (!m_Version.isUnityPackage && multipleVersionsVisible && isLatestVersion)
                    stateText = L10n.Tr("Latest");
            }
            else if (versionInManifest == m_Version.versionString)
                stateText = L10n.Tr("Requested");
            else if (m_Version == recommended && m_Version.isUnityPackage)
                stateText = L10n.Tr("Recommended");
            else if ((primary.isInstalled || !m_Version.isUnityPackage) && isLatestVersion)
                stateText = L10n.Tr("Latest");

            versionHistoryItemState.text = stateText;
        }

        private void RefreshContent()
        {
            if (!versionHistoryItemToggle.value)
                return;

            var fullyFetched = m_Version.isFullyFetched;
            UIUtils.SetElementDisplay(versionHistoryItemContainer, fullyFetched);
            if (!fullyFetched)
            {
                // If version is not fully fetched, we need to fetch extra information, meanwhile we need to start a spinner and disable the UI
                // We don't need to stop spinner since FetchExtraInfo raises a PackageVersionUpdated event,
                // and PackageDetailsVersionTabs will be refresh because PackageDetails is refreshed.
                SetEnabled(false);
                versionHistoryItemToggleSpinner.Start();
                m_OperationDispatcher.FetchExtraInfo(m_Version);
                return;
            }

            RefreshReleaseDate();
            RefreshChangeLog();
            RefreshDependencies();
            RefreshMetaDataChanges();
        }

        private void RefreshReleaseDate()
        {
            var releasedDate = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty;
            UIUtils.SetElementDisplay(versionHistoryItemReleaseContainer, !string.IsNullOrEmpty(releasedDate));
            versionHistoryItemReleaseDate.text = releasedDate;
        }

        private void RefreshChangeLog()
        {
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogContainer, false);
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogTitle, false);
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogLabel, false);
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogLink, false);

            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            var upmReserved = m_UpmCache.ParseUpmReserved(packageInfo);
            var changeLog = upmReserved?.GetString("changelog");
            var hasChangeLogInInfo = !string.IsNullOrEmpty(changeLog);
            if (hasChangeLogInInfo)
            {
                versionHistoryItemChangeLogLabel.text = changeLog;
                UIUtils.SetElementDisplay(versionHistoryItemChangeLogTitle, true);
                UIUtils.SetElementDisplay(versionHistoryItemChangeLogLabel, true);
                UIUtils.SetElementDisplay(versionHistoryItemChangeLogContainer, true);
            }

            if (UpmPackageDocs.HasChangelog(packageInfo))
            {
                versionHistoryItemChangeLogLink.text = hasChangeLogInInfo ? L10n.Tr("See full changelog") : L10n.Tr("See changelog");
                UIUtils.SetElementDisplay(versionHistoryItemChangeLogLink, true);
                UIUtils.SetElementDisplay(versionHistoryItemChangeLogContainer, true);

                var disableIfNotInstall =  m_Version?.isInstalled != true && m_Version?.package.product != null && string.IsNullOrEmpty(packageInfo?.changelogUrl);
                versionHistoryItemChangeLogLink.SetEnabled(!disableIfNotInstall);
                versionHistoryItemChangeLogLink.tooltip = disableIfNotInstall ? PackageDetailsLinks.k_InstallToViewChangelogTooltip : string.Empty;
            }
        }

        private void RefreshDependencies()
        {
            var primary = m_Version?.package?.versions?.primary;
            if (!HasDependenciesDifference(primary, m_Version))
            {
                UIUtils.SetElementDisplay(versionHistoryItemDependenciesContainer, false);
                return;
            }

            UIUtils.SetElementDisplay(versionHistoryItemDependenciesContainer, true);
            var hasDependencies = m_Version?.dependencies?.Any() ?? false;
            if (!hasDependencies)
            {
                versionHistoryItemDependenciesLabel.text = L10n.Tr("No dependencies");
                UIUtils.SetElementDisplay(versionHistoryItemDependenciesList, false);
            }
            else
            {
                versionHistoryItemDependenciesLabel.text = $"<b>{L10n.Tr("Is using")}</b>";
                versionHistoryItemDependenciesNames.Clear();
                versionHistoryItemDependenciesVersions.Clear();
                versionHistoryItemDependenciesStatuses.Clear();

                foreach (var dependency in m_Version.dependencies)
                {
                    m_PackageDatabase.GetPackageAndVersion(dependency, out var package, out var packageVersion);

                    var nameText = PackageDetailsDependenciesTab.GetNameText(dependency, package, packageVersion);
                    var versionText = PackageDetailsDependenciesTab.GetVersionText(dependency, packageVersion);
                    if (string.IsNullOrEmpty(versionText))
                        versionText = dependency.version;
                    var statusText = PackageDetailsDependenciesTab.GetStatusText(dependency, package?.versions.installed);

                    versionHistoryItemDependenciesNames.Add(new SelectableLabel { text = nameText });
                    versionHistoryItemDependenciesVersions.Add(new SelectableLabel { text = versionText });
                    versionHistoryItemDependenciesStatuses.Add(new SelectableLabel { text = statusText });
                }

                UIUtils.SetElementDisplay(versionHistoryItemDependenciesList, true);
            }
        }

        private static bool HasDependenciesDifference(IPackageVersion left, IPackageVersion right)
        {
            if (left == null || right == null)
                return false;

            var leftDependencies = left.dependencies ?? new DependencyInfo[0];
            var rightDependencies = right.dependencies ?? new DependencyInfo[0];
            if (leftDependencies.Length != rightDependencies.Length)
                return true;

            var comparer = new DependencyInfoComparer();
            return leftDependencies.Except(rightDependencies, comparer).Any() || rightDependencies.Except(leftDependencies, comparer).Any();
        }

        private void RefreshMetaDataChanges()
        {
            var primary = m_Version?.package?.versions?.primary;
            if (primary == null || m_Version == primary)
            {
                UIUtils.SetElementDisplay(versionHistoryItemMetaDataContainer, false);
                return;
            }

            var hasDisplayNameChange = string.Compare(m_Version.displayName, primary.displayName, StringComparison.InvariantCultureIgnoreCase) != 0;
            var hasAuthorChange = string.Compare(m_Version.author, primary.author, StringComparison.InvariantCultureIgnoreCase) != 0;
            var hasDescriptionChange = string.Compare(m_Version.description, primary.description, StringComparison.InvariantCultureIgnoreCase) != 0;

            UIUtils.SetElementDisplay(versionHistoryItemMetaDataContainer, hasDisplayNameChange || hasDescriptionChange || hasAuthorChange);
            if (hasDisplayNameChange || hasDescriptionChange || hasAuthorChange)
            {
                UIUtils.SetElementDisplay(versionHistoryItemMetaDataTitle, hasDisplayNameChange);
                versionHistoryItemMetaDataTitle.text = $"<b>{L10n.Tr("Title")}:</b> {m_Version.displayName}";

                UIUtils.SetElementDisplay(versionHistoryItemMetaDataAuthor, hasAuthorChange);
                versionHistoryItemMetaDataAuthor.text = $"<b>{L10n.Tr("Author")}:</b> {m_Version.author}";

                UIUtils.SetElementDisplay(versionHistoryItemMetaDataDescription, hasDescriptionChange);
                versionHistoryItemMetaDataDescription.text = $"<b>{L10n.Tr("Description")}:</b>\n{m_Version.description}";
            }
        }

        private void SetExpanded(bool expanded)
        {
            UIUtils.SetElementDisplay(versionHistoryItemContainer, expanded);
            if (expanded)
                RefreshContent();

            if (versionHistoryItemToggle.value != expanded)
                versionHistoryItemToggle.SetValueWithoutNotify(expanded);
        }

        private class DependencyInfoComparer : IEqualityComparer<DependencyInfo>
        {
            public bool Equals(DependencyInfo leftDependencyInfo, DependencyInfo rightDependencyInfo)
            {
                return string.Equals(leftDependencyInfo.name, rightDependencyInfo.name, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(leftDependencyInfo.version, rightDependencyInfo.version, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(DependencyInfo dependencyInfo)
            {
                var hashCode = new HashCode();
                hashCode.Add(dependencyInfo.name, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(dependencyInfo.version, StringComparer.InvariantCultureIgnoreCase);
                return hashCode.ToHashCode();
            }
        }

        private readonly VisualElementCache m_Cache;

        private Toggle versionHistoryItemToggle => m_Cache.Get<Toggle>("versionHistoryItemToggle");
        private PackageTagLabel versionHistoryItemTag => m_Cache.Get<PackageTagLabel>("versionHistoryItemTag");
        private SelectableLabel versionHistoryItemState => m_Cache.Get<SelectableLabel>("versionHistoryItemState");
        private VisualElement versionHistoryItemToggleRightContainer => m_Cache.Get<VisualElement>("versionHistoryItemToggleRightContainer");
        private VisualElement versionHistoryItemContainer => m_Cache.Get<VisualElement>("versionHistoryItemContainer");
        private VisualElement versionHistoryItemReleaseContainer => m_Cache.Get<VisualElement>("versionHistoryItemReleaseContainer");
        private SelectableLabel versionHistoryItemReleaseDate => m_Cache.Get<SelectableLabel>("versionHistoryItemReleaseDate");
        private VisualElement versionHistoryItemChangeLogContainer => m_Cache.Get<VisualElement>("versionHistoryItemChangeLogContainer");
        private Label versionHistoryItemChangeLogTitle => m_Cache.Get<Label>("versionHistoryItemChangeLogTitle");
        private SelectableLabel versionHistoryItemChangeLogLabel => m_Cache.Get<SelectableLabel>("versionHistoryItemChangeLogLabel");
        private Button versionHistoryItemChangeLogLink => m_Cache.Get<Button>("versionHistoryItemChangeLogLink");
        private VisualElement versionHistoryItemDependenciesContainer => m_Cache.Get<VisualElement>("versionHistoryItemDependenciesContainer");
        private Label versionHistoryItemDependenciesLabel => m_Cache.Get<Label>("versionHistoryItemDependenciesLabel");
        private VisualElement versionHistoryItemDependenciesList => m_Cache.Get<VisualElement>("versionHistoryItemDependenciesList");
        private VisualElement versionHistoryItemDependenciesNames => m_Cache.Get<VisualElement>("versionHistoryItemDependenciesNames");
        private VisualElement versionHistoryItemDependenciesVersions => m_Cache.Get<VisualElement>("versionHistoryItemDependenciesVersions");
        private VisualElement versionHistoryItemDependenciesStatuses => m_Cache.Get<VisualElement>("versionHistoryItemDependenciesStatuses");
        private VisualElement versionHistoryItemMetaDataContainer => m_Cache.Get<VisualElement>("versionHistoryItemMetaDataContainer");
        private SelectableLabel versionHistoryItemMetaDataTitle => m_Cache.Get<SelectableLabel>("versionHistoryItemMetaDataTitle");
        private SelectableLabel versionHistoryItemMetaDataAuthor => m_Cache.Get<SelectableLabel>("versionHistoryItemMetaDataAuthor");
        private SelectableLabel versionHistoryItemMetaDataDescription => m_Cache.Get<SelectableLabel>("versionHistoryItemMetaDataDescription");
        private LoadingSpinner versionHistoryItemToggleSpinner => m_Cache.Get<LoadingSpinner>("versionHistoryItemToggleSpinner");
    }
}
