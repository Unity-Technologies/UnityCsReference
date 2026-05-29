// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageItem : BaseListItem
    {
        // Note that the height here is only the height of the main item (i.e, version list is not expanded)
        public const int k_MainItemHeight = 24;

        private readonly IPackageDatabase m_PackageDatabase;
        public PackageItem(IPageManager pageManager, IPackageDatabase packageDatabase) : base(pageManager)
        {
            m_PackageDatabase = packageDatabase;

            m_LeftContainer = new VisualElement {name = "leftContainer"}.WithClassList("left");
            Add(m_LeftContainer);

            m_PackageTypeIcon = new VisualElement { name = "packageTypeIcon" };
            m_LeftContainer.Add(m_PackageTypeIcon);

            m_NameLabel = new Label { name = "packageName" }.WithClassList("name");
            m_LeftContainer.Add(m_NameLabel);

            m_FeaturePackageNumberLabel = new Label { name = "featurePackageNumber" };
            m_LeftContainer.Add(m_FeaturePackageNumberLabel);

            m_EnterpriseLabel = new Label {name = "entitlementLabel"};
            m_LeftContainer.Add(m_EnterpriseLabel);

            m_VersionLabel = new Label {name = "versionLabel"}.WithClassList("version", "middle");
            Add(m_VersionLabel);

            m_RightContainer = new VisualElement {name = "rightContainer"}.WithClassList("right");
            Add(m_RightContainer);

            var tagContainer = new VisualElement { name = "tagContainer" };
            m_TagLabel = new PackageDynamicTagLabel();
            tagContainer.Add(m_TagLabel);
            m_RightContainer.Add(tagContainer);

            m_StateIcon = new VisualElement { name = "stateIcon" };
            m_RightContainer.Add(m_StateIcon);
        }

        public override void BindVisualState(VisualState newVisualState)
        {
            base.BindVisualState(newVisualState);

            var package = m_PackageDatabase.GetPackage(newVisualState?.itemUniqueId);
            name = package?.displayName ?? package?.uniqueId ?? string.Empty;

            var version = package?.versions.primary;
            if (version == null)
                return;

            m_NameLabel.text = version.displayName ?? string.Empty;

            m_VersionLabel.text = version.versionString ?? string.Empty;
            UIUtils.SetElementDisplay(m_VersionLabel, !version.HasTag(PackageTag.BuiltIn | PackageTag.Feature));

            var isFeature = version.HasTag(PackageTag.Feature);
            m_FeaturePackageNumberLabel.text = $"({version.dependencies?.Length ?? 0})";
            UIUtils.SetElementDisplay(m_FeaturePackageNumberLabel, isFeature);

            m_PackageTypeIcon.EnableInClassList("feature", isFeature);

            m_TagLabel.Refresh(version);

            RefreshEntitlement(package);
            RefreshProgressAndStateIcon(version);
        }

        private void RefreshProgressAndStateIcon(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(m_StateIcon, false);

            if (RefreshSpinner(version))
                return;

            UIUtils.SetElementDisplay(m_StateIcon, true);
            m_StateIcon.ClearClassList();

            var state = version.package?.state ?? PackageState.None;
            if (ShouldOverrideStateIcon(m_PackageDatabase, visualState, version, state, out var iconClass, out var iconTooltip))
            {
                m_StateIcon.AddToClassList(iconClass);
                m_StateIcon.tooltip = iconTooltip;
            }
            else
            {
                m_StateIcon.AddToClassList(state.ToString().ToLower());
                m_StateIcon.tooltip = GetTooltipByState(version, state);
            }
        }

        private static bool ShouldOverrideStateIcon(IPackageDatabase packageDatabase, VisualState visualState, IPackageVersion version, PackageState state, out string iconClass, out string iconTooltip)
        {
            // We prioritize displaying error, warning, or restricted state icons over any icon overrides since it's more important information.
            var isHighPriorityState = state is PackageState.Error or PackageState.Warning or PackageState.Restricted;
            if (!isHighPriorityState)
            {
                if (visualState.isLocked)
                {
                    iconClass = "locked";
                    iconTooltip = string.Format(L10n.Tr("This {0} is installed by a feature."), version.GetDescriptor());
                    return true;
                }

                if (visualState.userUnlocked)
                {
                    iconClass = "unlockedbyuser";
                    iconTooltip = string.Format(L10n.Tr("This {0} is unlocked. You can now change its version."), version.GetDescriptor());
                    return true;
                }

                var overrideByCustomizedIcon = version is { isInstalled: true } && version.HasTag(PackageTag.Feature) &&
                                               packageDatabase.HasCustomizedDependencies(version, CustomizedDependencyType.All);
                if (overrideByCustomizedIcon)
                {
                    iconClass = "customized";
                    iconTooltip = string.Format(L10n.Tr("This {0} has been manually customized."), version.GetDescriptor());
                    return true;
                }
            }
            iconClass = null;
            iconTooltip = null;
            return false;
        }

        // Returns true if package is in progress and spinner is visible
        private bool RefreshSpinner(IPackageVersion version)
        {
            var progress = version.package?.progress ?? PackageProgress.None;
            var isInProgress = progress != PackageProgress.None;
            if (isInProgress)
            {
                if (m_Spinner == null)
                {
                    m_Spinner = new LoadingSpinner {name = "packageSpinner"};
                    m_RightContainer.Add(m_Spinner);
                }
                m_Spinner.Start();
                m_Spinner.tooltip = GetTooltipByProgress(version, progress);
            }
            else
            {
                m_Spinner?.Stop();
            }
            return isInProgress;
        }

        private void RefreshEntitlement(IPackage package)
        {
            var showEnterpriseLabel = package.isEnterprise;
            UIUtils.SetElementDisplay(m_EnterpriseLabel, showEnterpriseLabel);

            if (!showEnterpriseLabel)
                return;
            m_EnterpriseLabel.text = "E";
            m_EnterpriseLabel.tooltip = L10n.Tr("This is an entitled package.");
        }

        private readonly Label m_NameLabel;
        private readonly Label m_FeaturePackageNumberLabel;
        private readonly PackageDynamicTagLabel m_TagLabel;
        private readonly VisualElement m_StateIcon;
        private readonly Label m_EnterpriseLabel;
        private readonly Label m_VersionLabel;
        private readonly VisualElement m_PackageTypeIcon;
        private readonly VisualElement m_LeftContainer;
        private readonly VisualElement m_RightContainer;

        private LoadingSpinner m_Spinner;

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

        public string GetTooltipByState(IPackageVersion version, PackageState state)
        {
            return string.Format(k_TooltipsByState[(int)state], version.GetDescriptor());
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

        public string GetTooltipByProgress(IPackageVersion version, PackageProgress progress)
        {
            return string.Format(k_TooltipsByProgress[(int)progress], version.GetDescriptor(true));
        }
    }
}
