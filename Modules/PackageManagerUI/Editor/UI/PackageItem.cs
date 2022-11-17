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
                if (package != null && m_PageManager.GetPage().GetSelection().TryGetValue(package.uniqueId, out var selection))
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

        private readonly PageManager m_PageManager;
        private readonly PackageDatabase m_PackageDatabase;

        public PackageItem(PageManager pageManager, PackageDatabase packageDatabase)
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

            m_DependencyIcon = new Label { name = "dependencyIcon" };
            m_DependencyIcon.tooltip = "Installed as dependency";
            m_LeftContainer.Add(m_DependencyIcon);

            m_LockedIcon = new Label { name = "lockedIcon" };
            m_LeftContainer.Add(m_LockedIcon);

            m_DeprecationIcon = new Label { name = "deprecatedIcon" };
            m_DeprecationIcon.tooltip = L10n.Tr("Deprecated");
            m_LeftContainer.Add(m_DeprecationIcon);

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

            var tagContainer = new VisualElement { name = "tagContainer" };
            m_TagLabel = new PackageDynamicTagLabel();
            tagContainer.Add(m_TagLabel);
            m_RightContainer.Add(tagContainer);

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
            visualState = newVisualState?.Clone() ?? visualState ?? new VisualState(package?.uniqueId, string.Empty, false);

            EnableInClassList("invisible", !visualState.visible);
            m_NameLabel.text = targetVersion?.displayName ?? string.Empty;
            m_VersionLabel.text = targetVersion.versionString ?? string.Empty;

            if (m_NumPackagesInFeature != null)
                m_NumPackagesInFeature.text = string.Format(L10n.Tr("{0} packages"), package.versions.primary?.dependencies?.Length ?? 0);

            var showVersionLabel = !package.versions.primary.HasTag(PackageTag.BuiltIn | PackageTag.Feature);
            UIUtils.SetElementDisplay(m_VersionLabel, showVersionLabel);

            m_TagLabel.Refresh(package.versions.primary);

            RefreshLeftStateIcons();
            RefreshRightStateIcons();
            RefreshSelection();
            RefreshEntitlement();
        }

        public void SetPackage(IPackage package)
        {
            this.package = package;
            name = package?.displayName ?? package?.uniqueId ?? string.Empty;
        }

        private void RefreshLeftStateIcons()
        {
            var showLockIcon = visualState.isLocked;
            var showDeprecationIcon = package.isDeprecated;

            var targetVersion = this.targetVersion;
            var showDependencyIcon = targetVersion != null &&
                                     !showLockIcon &&
                                     targetVersion.isInstalled &&
                                     !targetVersion.isDirectDependency &&
                                     !targetVersion.HasTag(PackageTag.Feature);

            var showExpanderHidden = !showLockIcon && !showDeprecationIcon && !showDependencyIcon;

            UIUtils.SetElementDisplay(m_LockedIcon, showLockIcon);
            UIUtils.SetElementDisplay(m_DeprecationIcon, showDeprecationIcon);
            UIUtils.SetElementDisplay(m_DependencyIcon, showDependencyIcon);
            UIUtils.SetElementDisplay(m_ExpanderHidden, showExpanderHidden);
        }

        public void RefreshRightStateIcons()
        {
            if (RefreshSpinner())
                return;

            var state = package?.state ?? PackageState.None;
            var stateClass = state != PackageState.None ? state.ToString().ToLower() : null;
            if (!string.IsNullOrEmpty(m_CurrentStateClass))
                m_StateIcon.RemoveFromClassList(m_CurrentStateClass);
            if (!string.IsNullOrEmpty(stateClass))
                m_StateIcon.AddToClassList(stateClass);
            m_CurrentStateClass = stateClass;

            m_StateIcon.tooltip = GetTooltipByState(state);

            if (state == PackageState.Installed && package.versions.primary.HasTag(PackageTag.Feature))
                RefreshFeatureState();
        }

        // Returns true if package is in progress and spinner is visible
        private bool RefreshSpinner()
        {
            var progress = package?.progress ?? PackageProgress.None;
            var isInProgress = progress != PackageProgress.None && package?.state == PackageState.InProgress;
            if (isInProgress)
                StartSpinner();
            else
                StopSpinner();
            return isInProgress;
        }

        private void RefreshFeatureState()
        {
            var featureState = FeatureState.None;
            foreach (var dependency in targetVersion.dependencies)
            {
                var packageVersion = m_PackageDatabase.GetLifecycleOrPrimaryVersion(dependency.name);
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

        private void RefreshEntitlement()
        {
            var showEntitlement = package.hasEntitlements;
            UIUtils.SetElementDisplay(m_EntitlementLabel, showEntitlement);
            m_EntitlementLabel.text = showEntitlement ? "E" : string.Empty;
            m_EntitlementLabel.tooltip = showEntitlement ? L10n.Tr("This is an Entitlement package.") : string.Empty;
        }

        public void SelectMainItem()
        {
            m_PageManager.GetPage().SetNewSelection(package, null, true);
        }

        public void ToggleSelectMainItem()
        {
            m_PageManager.GetPage().ToggleSelection(package?.uniqueId, true);
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
        private PackageDynamicTagLabel m_TagLabel;
        private VisualElement m_MainItem;
        private VisualElement m_StateIcon;
        private VisualElement m_InfoStateIcon;
        private VisualElement m_StateContainer;
        private Label m_EntitlementLabel;
        private Label m_VersionLabel;
        private LoadingSpinner m_Spinner;
        private Label m_LockedIcon;
        private Label m_DeprecationIcon;
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
            L10n.Tr("There are errors with this {0}. Read the {0} details for further guidance."),
            L10n.Tr("There are warnings with this {0}. Read the {0} details for further guidance.")
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
            L10n.Tr("{0} removing in progress.")
        };

        public string GetTooltipByProgress(PackageProgress progress)
        {
            return string.Format(k_TooltipsByProgress[(int)progress], package.versions.primary.GetDescriptor(true));
        }
    }
}
