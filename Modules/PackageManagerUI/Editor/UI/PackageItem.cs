// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageItem : VisualElement, ISelectableItem
    {
        // Note that the height here is only the height of the main item (i.e, version list is not expanded)
        public const int k_MainItemHeight = 25;
        private const string k_SelectedClassName = "selected";

        private string m_CurrentStateClass;

        public IPackage package { get; private set; }
        public VisualState visualState { get; private set; }

        public IPackageVersion targetVersion => package?.versions.primary;
        public VisualElement element => this;

        // Since the layout for Feature and Non-Feature are different, we want to keep track of the current layout
        // and only call BuildMainItem again when there's a layout change
        private bool? m_IsFeatureLayout = null;

        public PackageGroup packageGroup { get; set; }

        private readonly IPageManager m_PageManager;
        private readonly IPackageDatabase m_PackageDatabase;

        public PackageItem(IPageManager pageManager, IPackageDatabase packageDatabase)
        {
            m_PageManager = pageManager;
            m_PackageDatabase = packageDatabase;
        }

        public void SetPackageAndVisualState(IPackage package, VisualState state)
        {
            var isFeature = package?.versions.primary.HasTag(PackageTag.Feature) == true;
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

            m_PackageTypeIcon = new Label { name = "packageTypeIcon" };
            m_LeftContainer.Add(m_PackageTypeIcon);

            m_NameLabel = new Label { name = "packageName", classList = {"name"} };
            m_LeftContainer.Add(m_NameLabel);

            if (isFeature)
            {
                m_MainItem.AddToClassList("feature");
                m_FeaturePackageNumberLabel = new Label { name = "featurePackageNumber" };
                m_LeftContainer.Add(m_FeaturePackageNumberLabel);
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

            var tagContainer = new VisualElement { name = "tagContainer" };
            m_TagLabel = new PackageDynamicTagLabel();
            tagContainer.Add(m_TagLabel);
            m_RightContainer.Add(tagContainer);

            m_Spinner = null;

            m_StateContainer = new VisualElement { name = "statesContainer" };
            m_MainItem.Add(m_StateContainer);

            m_StateIcon = new VisualElement { name = "stateIcon", classList = { "status" } };
            m_StateContainer.Add(m_StateIcon);

            m_LockedIcon = new VisualElement { name = "lockedIcon", classList = { "lock" } };
            m_MainItem.Add(m_LockedIcon);

            UIUtils.SetElementDisplay(m_PackageTypeIcon, true);
        }

        public void UpdateVisualState(VisualState newVisualState)
        {
            if (targetVersion == null)
                return;

            Refresh(newVisualState);
        }

        public void Refresh(VisualState newVisualState = null)
        {
            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty, false);

            EnableInClassList("invisible", !visualState.visible);
            m_NameLabel.text = targetVersion?.displayName ?? string.Empty;

            if (m_FeaturePackageNumberLabel != null)
                m_FeaturePackageNumberLabel.text = "(" + (package?.versions.primary?.dependencies?.Length ?? 0) + ")";

            m_VersionLabel.text = targetVersion?.versionString ?? string.Empty;

            var showVersionLabel = !package.versions.primary.HasTag(PackageTag.BuiltIn | PackageTag.Feature);
            UIUtils.SetElementDisplay(m_VersionLabel, showVersionLabel);

            m_TagLabel.Refresh(package.versions.primary);

            RefreshIconsOnTheRight();
            RefreshSelection();
            RefreshEntitlement();
        }

        public void SetPackage(IPackage package)
        {
            this.package = package;
            name = package?.displayName ?? package?.uniqueId ?? string.Empty;
        }

        private void RefreshIconsOnTheRight()
        {
            UIUtils.SetElementDisplay(m_Spinner, false);
            UIUtils.SetElementDisplay(m_StateIcon, false);
            UIUtils.SetElementDisplay(m_LockedIcon, false);

            if (RefreshSpinner())
                return;

            var state = package?.state ?? PackageState.None;
            var isHighPriorityState =
                state == PackageState.Error ||
                state == PackageState.Warning ||
                state == PackageState.Restricted;

            // The lock and state icon occupy the same space. Showing the lock icon hides the package state.
            // We prioritize displaying error, warning, or restricted state icons over the lock icon since it's more important information.
            if (isHighPriorityState || !RefreshLockIcons())
                RefreshRightStateIcons();
        }

        // Returns true if package is in progress and spinner is visible
        private bool RefreshSpinner()
        {
            var progress = package?.progress ?? PackageProgress.None;
            var isInProgress = progress != PackageProgress.None;
            if (isInProgress)
                StartSpinner();
            else
                StopSpinner();
            return isInProgress;
        }

        private bool RefreshLockIcons()
        {
            const string k_Locked = "locked";
            const string k_UnlockedByUser = "unlockedbyuser";

            m_LockedIcon.RemoveFromClassList(k_Locked);
            m_LockedIcon.RemoveFromClassList(k_UnlockedByUser);

            if (!visualState.userUnlocked && !visualState.isLocked)
               return false;

            UIUtils.SetElementDisplay(m_LockedIcon, true);
            if (visualState.userUnlocked)
            {
                m_LockedIcon.AddToClassList(k_UnlockedByUser);
                m_LockedIcon.tooltip = string.Format(L10n.Tr("This {0} is unlocked. You can now change its version."),
                                        package.versions.primary.GetDescriptor());
            }
            else if (visualState.isLocked)
            {
                m_LockedIcon.AddToClassList(k_Locked);
                m_LockedIcon.tooltip = string.Format(L10n.Tr("This {0} is installed by a feature."),
                                        package.versions.primary.GetDescriptor());
            }

            return true;
        }

        public void RefreshRightStateIcons()
        {
            UIUtils.SetElementDisplay(m_StateIcon, true);

            var state = package?.state ?? PackageState.None;
            var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
            if (!string.IsNullOrEmpty(m_CurrentStateClass))
                m_StateIcon.RemoveFromClassList(m_CurrentStateClass);
            if (!string.IsNullOrEmpty(stateClass))
                m_StateIcon.AddToClassList(stateClass);
            m_CurrentStateClass = stateClass;

            m_StateIcon.tooltip = GetTooltipByState(state);

            RefreshFeatureCustomizedIcon();
        }

        private void RefreshFeatureCustomizedIcon()
        {
            var primaryVersion = package?.versions.primary;
            if (primaryVersion is not { isInstalled: true } || !primaryVersion.HasTag(PackageTag.Feature))
                return;

            var featureCustomized = m_PackageDatabase.HasCustomizedDependencies(targetVersion, CustomizedDependencyType.All);
            m_StateIcon.EnableInClassList("customized", featureCustomized);
            m_StateIcon.tooltip = featureCustomized ? L10n.Tr("This feature has been manually customized") : string.Empty;
        }

        public void RefreshSelection()
        {
            var enable = package != null && m_PageManager.activePage.GetSelection().Contains(package.uniqueId);
            EnableInClassList(k_SelectedClassName, enable);
            m_MainItem.EnableInClassList(k_SelectedClassName, enable);
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
            m_PageManager.activePage.SetNewSelection(package, true);
        }

        public void ToggleSelectMainItem()
        {
            m_PageManager.activePage.ToggleSelection(package?.uniqueId, true);
        }

        private void StartSpinner()
        {
            UIUtils.SetElementDisplay(m_Spinner, true);

            if (m_Spinner == null)
            {
                m_Spinner = new LoadingSpinner {name = "packageSpinner"};
                m_StateContainer.Insert(0, m_Spinner);
            }

            m_Spinner.Start();
            m_Spinner.tooltip = GetTooltipByProgress(package.progress);
        }

        private void StopSpinner()
        {
            m_Spinner?.Stop();
        }

        private Label m_NameLabel;
        private Label m_FeaturePackageNumberLabel;
        private PackageDynamicTagLabel m_TagLabel;
        private VisualElement m_MainItem;
        private VisualElement m_StateIcon;
        private VisualElement m_LockedIcon;
        private VisualElement m_StateContainer;
        private Label m_EntitlementLabel;
        private Label m_VersionLabel;
        private LoadingSpinner m_Spinner;
        private Label m_PackageTypeIcon;
        private VisualElement m_LeftContainer;
        private VisualElement m_RightContainer;

        private static readonly string[] k_TooltipsByState =
        {
            "",
            L10n.Tr("This {0} is installed."),
            L10n.Tr("This {0} is installed as a dependency."),
            L10n.Tr("This {0} is available for download."),
            L10n.Tr("This {0} is available for import."),
            L10n.Tr("There are assets in your project that are imported from this {0}."),
            L10n.Tr("This {0} is in development."),
            L10n.Tr("A newer version of this {0} is available."),
            L10n.Tr("There are errors with this {0}. Read the {0} details for further guidance."),
            L10n.Tr("There are warnings with this {0}. Read the {0} details for further guidance."),
            L10n.Tr("This {0} is restricted. Read the {0} details for further guidance."),
        };

        public string GetTooltipByState(PackageState state)
        {
            return string.Format(k_TooltipsByState[(int)state], package.versions.primary.GetDescriptor());
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
            L10n.Tr("{0} removing in progress."),
            L10n.Tr("{0} exporting in progress."),
        };

        public string GetTooltipByProgress(PackageProgress progress)
        {
            return string.Format(k_TooltipsByProgress[(int)progress], package.versions.primary.GetDescriptor(true));
        }
    }
}
