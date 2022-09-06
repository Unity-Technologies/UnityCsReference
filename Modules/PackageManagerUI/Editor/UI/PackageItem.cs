// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageItem : VisualElement, ISelectableItem
    {
        // Note that the height here is only the height of the main item (i.e, version list is not expanded)
        internal const int k_MainItemHeight = 25;
        private const string k_SelectedClassName = "selected";

        private string m_CurrentStateClass;
        private string m_CurrentFeatureState;

        public IPackage package { get; private set; }
        public VisualState visualState { get; private set; }

        public IPackageVersion targetVersion => package?.versions.primary;
        public VisualElement element => this;

        // Since the layout for Feature and Non-Feature are different, we want to keep track of the current layout
        // and only call BuildMainItem again when there's a layout change
        private bool? m_IsFeatureLayout = null;

        internal PackageGroup packageGroup { get; set; }

        private IPackageVersion selectedVersion
        {
            get
            {
                if (package != null && m_PageManager.GetSelection().TryGetValue(package.uniqueId, out var selection))
                {
                    if (string.IsNullOrEmpty(selection.versionUniqueId))
                        return package.versions.primary;
                    return package.versions.FirstOrDefault(v => v.uniqueId == selection.versionUniqueId);
                }
                return null;
            }
        }

        internal bool isLocked => m_LockedIcon?.visible ?? false;
        internal bool isDependency => !package?.versions.installed?.isDirectDependency ?? false;

        private readonly ResourceLoader m_ResourceLoader;
        private readonly PageManager m_PageManager;
        private readonly PackageManagerProjectSettingsProxy m_SettingsProxy;
        private readonly PackageDatabase m_PackageDatabase;

        public PackageItem(ResourceLoader resourceLoader, PageManager pageManager, PackageManagerProjectSettingsProxy settingsProxy, PackageDatabase packageDatabase)
        {
            m_ResourceLoader = resourceLoader;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_PackageDatabase = packageDatabase;
        }

        public void SetPackageAndVisualState(IPackage package, VisualState state)
        {
            var isFeature = package?.Is(PackageType.Feature) == true;
            if (m_IsFeatureLayout != isFeature)
            {
                Clear();
                BuildMainItem(isFeature);
            }

            SetPackage(package);
            UpdateVisualState(state);
        }

        private void BuildMainItem(bool isFeature)
        {
            m_IsFeatureLayout = isFeature;

            m_MainItem = new VisualElement {name = "mainItem"};
            Add(m_MainItem);

            m_LeftContainer = new VisualElement {name = "leftContainer", classList = {"left"}};
            m_MainItem.Add(m_LeftContainer);

            m_DependencyIcon = new Label { name = "dependencyIcon" };
            m_DependencyIcon.tooltip = "Installed as dependency";
            m_LeftContainer.Add(m_DependencyIcon);

            m_LockedIcon = new Label { name = "lockedIcon" };
            m_LeftContainer.Add(m_LockedIcon);

            m_ExpanderHidden = new Label {name = "expanderHidden", classList = {"expanderHidden"}};
            m_LeftContainer.Add(m_ExpanderHidden);

            m_NameLabel = new Label {name = "packageName", classList = {"name"}};
            if (isFeature)
            {
                m_MainItem.AddToClassList("feature");
                m_NumPackagesInFeature = new Label() { name = "numPackages" };

                var leftMiddleContainer = new VisualElement() { name = "leftMiddleContainer" };
                leftMiddleContainer.Add(m_NameLabel);
                leftMiddleContainer.Add(m_NumPackagesInFeature);
                m_LeftContainer.Add(leftMiddleContainer);
            }
            else
            {
                m_LeftContainer.Add(m_NameLabel);
            }

            m_EntitlementLabel = new Label {name = "entitlementLabel"};
            UIUtils.SetElementDisplay(m_EntitlementLabel, false);
            m_LeftContainer.Add(m_EntitlementLabel);

            m_VersionLabel = new Label {name = "versionLabel", classList = {"version", "middle"}};
            m_MainItem.Add(m_VersionLabel);

            m_RightContainer = new VisualElement {name = "rightContainer", classList = {"right"}};
            m_MainItem.Add(m_RightContainer);

            m_TagContainer = new VisualElement {name = "tagContainer"};
            m_RightContainer.Add(m_TagContainer);

            m_Spinner = null;

            m_StateContainer = new VisualElement { name = "statesContainer" };
            m_MainItem.Add(m_StateContainer);

            m_StateIcon = new VisualElement { name = "stateIcon", classList = { "status" } };
            m_StateContainer.Add(m_StateIcon);

            if (isFeature)
            {
                m_InfoStateIcon = new VisualElement { name = "versionState" };
                m_StateContainer.Add(m_InfoStateIcon);
            }
        }

        public void UpdateVisualState(VisualState newVisualState)
        {
            if (targetVersion == null)
                return;

            Refresh(newVisualState);
        }

        public void Refresh(VisualState newVisualState = null)
        {
            var previousVisualState = visualState?.Clone() ?? new VisualState(package?.uniqueId, string.Empty, false);
            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty, false);

            EnableInClassList("invisible", !visualState.visible);
            m_NameLabel.text = targetVersion?.displayName ?? string.Empty;
            m_VersionLabel.text = targetVersion.versionString ?? string.Empty;

            if (m_NumPackagesInFeature != null)
                m_NumPackagesInFeature.text = string.Format(L10n.Tr("{0} packages"), package.versions.primary?.dependencies?.Length ?? 0);

            UIUtils.SetElementDisplay(m_ExpanderHidden, !visualState.isLocked);

            var showVersionLabel = !package.Is(PackageType.BuiltIn | PackageType.Feature);
            UIUtils.SetElementDisplay(m_VersionLabel, showVersionLabel);

            UIUtils.SetElementDisplay(m_LockedIcon, false);
            UIUtils.SetElementDisplay(m_DependencyIcon, false);

            UpdateLockedUI(visualState.isLocked);
            UpdateDependencyUI(visualState.isLocked);

            var version = selectedVersion;
            if (version != null && version != targetVersion)
                visualState.seeAllVersions = visualState.seeAllVersions || !package.versions.key.Contains(version);

            RefreshState();
            RefreshSelection();
            RefreshTags();
            RefreshEntitlement();
        }

        public void SetPackage(IPackage package)
        {
            this.package = package;
            name = package?.displayName ?? package?.uniqueId ?? string.Empty;
        }

        public void RefreshState()
        {
            var state = package?.state ?? PackageState.None;
            var progress = package?.progress ?? PackageProgress.None;
            if (state != PackageState.InProgress || progress == PackageProgress.None)
            {
                var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
                if (!string.IsNullOrEmpty(m_CurrentStateClass))
                    m_StateIcon.RemoveFromClassList(m_CurrentStateClass);
                if (!string.IsNullOrEmpty(stateClass))
                    m_StateIcon.AddToClassList(stateClass);
                m_CurrentStateClass = stateClass;

                m_StateIcon.tooltip = GetTooltipByState(state);
                StopSpinner();

                if (package.Is(PackageType.Feature) && state == PackageState.Installed)
                    GetState();
            }
            else
            {
                StartSpinner();
            }
        }

        private void GetState()
        {
            var featureState = FeatureState.None;
            foreach (var dependency in targetVersion.dependencies)
            {
                var packageVersion = m_PackageDatabase.GetPackageInFeatureVersion(dependency.name);
                if (packageVersion == null)
                    continue;

                var installedVersion = packageVersion.package?.versions.installed;
                if (installedVersion == null)
                    continue;
                // User manually decide to install a different version
                else if ((installedVersion.isDirectDependency && package.versions.isNonLifecycleVersionInstalled) || installedVersion.HasTag(PackageTag.InDevelopment))
                {
                    featureState = FeatureState.Customized;
                    break;
                }
            }

            if (featureState == FeatureState.Customized)
            {
                m_CurrentFeatureState = featureState.ToString().ToLower();
                m_InfoStateIcon.AddToClassList(m_CurrentFeatureState);
                m_InfoStateIcon.tooltip = L10n.Tr("This feature has been manually customized");
            }
            else
            {
                m_InfoStateIcon.RemoveFromClassList(m_CurrentFeatureState);
                m_CurrentFeatureState = null;
            }
        }

        public void RefreshSelection()
        {
            var enable = selectedVersion != null;
            EnableInClassList(k_SelectedClassName, enable);
            m_MainItem.EnableInClassList(k_SelectedClassName, enable);
        }

        private void RefreshTags()
        {
            m_TagContainer.Clear();
            var showTags = !package.Is(PackageType.AssetStore) && !package.Is(PackageType.BuiltIn);
            if (showTags)
            {
                var tagLabel = PackageTagLabel.CreateTagLabel(targetVersion);
                if (tagLabel != null)
                    m_TagContainer.Add(tagLabel);
            }
        }

        private void RefreshEntitlement()
        {
            var showEntitlement = package.hasEntitlements;
            UIUtils.SetElementDisplay(m_EntitlementLabel, showEntitlement);
            m_EntitlementLabel.text = showEntitlement ? "E" : string.Empty;
            m_EntitlementLabel.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        public void SelectMainItem()
        {
            m_PageManager.SetSelected(package, null, true);
        }

        public void ToggleSelectMainItem()
        {
            m_PageManager.ToggleSelected(package?.uniqueId, true);
        }

        internal void UpdateLockedUI(bool showLock)
        {
            UIUtils.SetElementDisplay(m_LockedIcon, showLock);
        }

        internal void UpdateDependencyUI(bool showLock)
        {
            if (targetVersion == null)
                return;

            var showDependencyIcon = targetVersion.isInstalled &&
                                     !targetVersion.isDirectDependency &&
                                     !targetVersion.HasTag(PackageTag.Feature) &&
                                     !showLock;

            UIUtils.SetElementDisplay(m_DependencyIcon, showDependencyIcon);
            if (showDependencyIcon)
                UIUtils.SetElementDisplay(m_ExpanderHidden, false);
        }

        private void SeeAllVersionsClick()
        {
            m_PageManager.SetSeeAllVersions(package, true);
            PackageManagerWindowAnalytics.SendEvent("seeAllVersions", targetVersion?.uniqueId);
        }

        private void StartSpinner()
        {
            if (m_Spinner == null)
            {
                m_Spinner = new LoadingSpinner {name = "packageSpinner"};
                m_StateContainer.Insert(0, m_Spinner);
            }

            m_Spinner.Start();
            m_Spinner.tooltip = GetTooltipByProgress(package.progress);
            UIUtils.SetElementDisplay(m_StateIcon, false);
        }

        private void StopSpinner()
        {
            m_Spinner?.Stop();
            UIUtils.SetElementDisplay(m_StateIcon, true);
        }

        private Label m_NameLabel;
        private VisualElement m_TagContainer;
        private VisualElement m_MainItem;
        private VisualElement m_StateIcon;
        private VisualElement m_InfoStateIcon;
        private VisualElement m_StateContainer;
        private Label m_EntitlementLabel;
        private Label m_VersionLabel;
        private LoadingSpinner m_Spinner;
        private Label m_LockedIcon;
        private Label m_DependencyIcon;
        private Label m_ExpanderHidden;
        private VisualElement m_LeftContainer;
        private VisualElement m_RightContainer;
        private Label m_NumPackagesInFeature;

        private static readonly string[] k_TooltipsByState =
        {
            "",
            L10n.Tr("This {0} is installed."),
            // Keep the error message for `installed` and `installedAsDependency` the same for now as requested by the designer
            L10n.Tr("This {0} is installed."),
            L10n.Tr("This {0} is available for download."),
            L10n.Tr("This {0} is available for import."),
            L10n.Tr("This {0} is in development."),
            L10n.Tr("A newer version of this {0} is available."),
            "",
            L10n.Tr("There are errors with this {0}. Please read the {0} details for further guidance."),
            L10n.Tr("There are warnings with this {0}. Please read the {0} details for further guidance.")
        };

        public string GetTooltipByState(PackageState state)
        {
            return string.Format(k_TooltipsByState[(int)state], package.GetDescriptor());
        }

        private static readonly string[] k_TooltipsByProgress =
        {
            "",
            L10n.Tr("{0} refreshing in progress."),
            L10n.Tr("{0} downloading in progress."),
            L10n.Tr("{0} pausing in progress."),
            L10n.Tr("{0} resuming in progress."),
            L10n.Tr("{0} installing in progress."),
            L10n.Tr("{0} resetting in progress."),
            L10n.Tr("{0} removing in progress.")
        };

        public string GetTooltipByProgress(PackageProgress progress)
        {
            return string.Format(k_TooltipsByProgress[(int)progress], package.GetDescriptor(true));
        }
    }
}
