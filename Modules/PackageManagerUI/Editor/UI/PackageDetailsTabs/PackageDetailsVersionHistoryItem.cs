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
        public PackageAction action { get; }

        private IPackageVersion m_Version;
        private readonly IPackageToolBarButton m_Button;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IUpmCache m_UpmCache;
        private readonly IPackageLinkFactory m_PackageLinkFactory;

        private PackageDynamicTagLabel m_VersionHistoryItemTag;

        public event Action<bool> onToggleChanged = delegate {};

        public PackageDetailsVersionHistoryItem(IResourceLoader resourceLoader,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IUpmCache upmCache,
            IApplicationProxy applicationProxy,
            IPackageLinkFactory packageLinkFactory,
            IPackageVersion version,
            bool multipleVersionsVisible,
            bool isLatestVersion,
            bool expanded,
            PackageAction action)
        {
            m_Version = version;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_UpmCache = upmCache;
            m_PackageLinkFactory = packageLinkFactory;

            var root = resourceLoader.GetTemplate("PackageDetailsVersionHistoryItem.uxml");
            Add(root);
            m_Cache = new VisualElementCache(root);

            m_VersionHistoryItemTag = new PackageDynamicTagLabel(true);
            versionHistoryItemToggleLeftContainer.Insert(0, m_VersionHistoryItemTag);

            var versionHistoryChangelogLink = m_PackageLinkFactory.CreateVersionHistoryChangelogLink(version);
            if (versionHistoryChangelogLink?.isVisible == true)
                versionHistoryItemChangeLogContainer.Add(new PackageLinkButton(applicationProxy, versionHistoryChangelogLink));

            SetExpanded(expanded);
            versionHistoryItemToggle.RegisterValueChangedCallback(evt =>
            {
                SetExpanded(evt.newValue);
                onToggleChanged?.Invoke(evt.newValue);
            });

            this.action = action;
            if (action != null)
            {
                m_Button = new PackageToolBarSimpleButton(action);
                versionHistoryItemToggleRightContainer.Add(m_Button.element);
            }
            Refresh(multipleVersionsVisible, isLatestVersion);
        }

        public void StopSpinner()
        {
            if (m_Version?.isFullyFetched == false)
                versionHistoryItemToggleSpinner?.Stop();
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
            versionHistoryItemToggle.text = m_Version?.versionString;
            m_VersionHistoryItemTag.Refresh(m_Version);

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
                else if (m_Version == recommended && multipleVersionsVisible && m_Version.HasTag(PackageTag.Unity))
                    stateText = L10n.Tr("Recommended");
                else if (!m_Version.HasTag(PackageTag.Unity) && multipleVersionsVisible && isLatestVersion)
                    stateText = L10n.Tr("Latest");
            }
            else if (versionInManifest == m_Version.versionString)
                stateText = L10n.Tr("Requested");
            else if (m_Version == recommended && m_Version.HasTag(PackageTag.Unity))
                stateText = L10n.Tr("Recommended");
            else if ((primary.isInstalled || !m_Version.HasTag(PackageTag.Unity)) && isLatestVersion)
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

            RefreshDeprecatedVersionErrorInfoBox();
            RefreshReleaseDate();
            RefreshChangeLog();
            RefreshDependencies();
            RefreshMetaDataChanges();
        }

        private void RefreshDeprecatedVersionErrorInfoBox()
        {
            var showVersionDeprecation = m_Version.HasTag(PackageTag.Deprecated);
            UIUtils.SetElementDisplay(deprecatedVersionErrorInfoBox, showVersionDeprecation);
            if (showVersionDeprecation)
                deprecatedVersionErrorInfoBox.text = L10n.Tr("This version is deprecated. ") + L10n.Tr(m_Version.deprecationMessage);
        }

        private void RefreshReleaseDate()
        {
            var releasedDate = m_Version.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty;
            UIUtils.SetElementDisplay(versionHistoryItemReleaseContainer, !string.IsNullOrEmpty(releasedDate));
            versionHistoryItemReleaseDate.text = releasedDate;
        }

        private void RefreshChangeLog()
        {
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogTitle, false);
            UIUtils.SetElementDisplay(versionHistoryItemChangeLogLabel, false);

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
        private VisualElement versionHistoryItemToggleLeftContainer => m_Cache.Get<VisualElement>("versionHistoryItemToggleLeftContainer");
        private SelectableLabel versionHistoryItemState => m_Cache.Get<SelectableLabel>("versionHistoryItemState");
        private VisualElement versionHistoryItemToggleRightContainer => m_Cache.Get<VisualElement>("versionHistoryItemToggleRightContainer");
        private HelpBox deprecatedVersionErrorInfoBox => m_Cache.Get<HelpBox>("deprecatedVersionErrorInfoBox");
        private VisualElement versionHistoryItemContainer => m_Cache.Get<VisualElement>("versionHistoryItemContainer");
        private VisualElement versionHistoryItemReleaseContainer => m_Cache.Get<VisualElement>("versionHistoryItemReleaseContainer");
        private SelectableLabel versionHistoryItemReleaseDate => m_Cache.Get<SelectableLabel>("versionHistoryItemReleaseDate");
        private VisualElement versionHistoryItemChangeLogContainer => m_Cache.Get<VisualElement>("versionHistoryItemChangeLogContainer");
        private Label versionHistoryItemChangeLogTitle => m_Cache.Get<Label>("versionHistoryItemChangeLogTitle");
        private SelectableLabel versionHistoryItemChangeLogLabel => m_Cache.Get<SelectableLabel>("versionHistoryItemChangeLogLabel");
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
