// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class FeatureDependencies : VisualElement
    {
        public const string k_SelectedClassName = "selected";

        internal new class UxmlFactory : UxmlFactory<FeatureDependencies> {}

        private ResourceLoader m_ResourceLoader;
        private PackageDatabase m_PackageDatabase;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        }

        private IPackageVersion m_FeatureVersion;

        public FeatureDependencies()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("FeatureDependencies.uxml");
            Add(root);

            cache = new VisualElementCache(root);
            SetExpanded(m_PackageManagerPrefs.featureDependenciesExpanded);
            dependenciesExpander?.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));
        }

        public FeatureState GetFeatureState(IPackageVersion version)
        {
            if (version == null)
                return FeatureState.None;

            var package = m_PackageDatabase.GetPackage(version);
            var installedVersion = package?.versions.installed;
            if (installedVersion == null)
                return FeatureState.None;

            var isNonLifecycleVersionInstalled = package.versions.isNonLifecycleVersionInstalled;
            // User manually decide to install a different version
            if ((installedVersion.isDirectDependency && isNonLifecycleVersionInstalled) || installedVersion.HasTag(PackageTag.InDevelopment))
                return FeatureState.Customized;
            // The installed version is changed by the SAT solver, not overridden by user
            if (isNonLifecycleVersionInstalled)
                return FeatureState.Info;

            return FeatureState.None;
        }

        public void RecalculateFillerHeight(float detailsHeight, float scrollViewHeight)
        {
            if (!dependenciesExpander.value || !UIUtils.IsElementVisible(this))
                return;
            var detailsContentHeight = detailsHeight - fillerLeftContainer.layout.height;
            var newFillerHeight = Mathf.Max(0, scrollViewHeight - detailsContentHeight);
            if ((int)fillerLeftContainer.layout.height != (int)newFillerHeight)
                fillerLeftContainer.style.height = newFillerHeight;
        }

        private void SetExpanded(bool expanded)
        {
            if (dependenciesExpander.value != expanded)
                dependenciesExpander.value = expanded;
            if (m_PackageManagerPrefs.featureDependenciesExpanded != expanded)
                m_PackageManagerPrefs.featureDependenciesExpanded = expanded;
            UIUtils.SetElementDisplay(dependenciesOuterContainer, expanded);
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            m_FeatureVersion = version;

            if (version?.dependencies?.Length <= 0 || version?.HasTag(PackageTag.Feature) != true)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }

            UIUtils.SetElementDisplay(this, true);

            dependencyList.Clear();
            foreach (var dependency in version.dependencies)
            {
                var packageVersion = m_PackageDatabase.GetPackageInFeatureVersion(dependency.name);
                var featureState = GetFeatureState(packageVersion);

                var item = new FeatureDependencyItem(version, packageVersion, featureState);
                item.OnLeftClick(() =>
                {
                    OnDependencyItemClicked(packageVersion);
                });
                dependencyList.Add(item);
            }

            RefreshSelection();
            RefreshHeaderText(dependencyList.childCount);
        }

        private void RefreshHeaderText(int numPackages)
        {
            dependenciesExpander.text = numPackages == 1 ? L10n.Tr("One Package Included") : string.Format(L10n.Tr("{0} Packages Included"), numPackages);
        }

        private void OnDependencyItemClicked(IPackageVersion version)
        {
            m_PackageManagerPrefs.selectedFeatureDependency = version.packageUniqueId;
            RefreshSelection(version);
        }

        private void RefreshSelection(IPackageVersion version = null)
        {
            if (version == null)
            {
                var dependencies = m_FeatureVersion?.dependencies;
                if (dependencies?.Any() != true)
                    return;

                var selectedDependencyPackageId = m_PackageManagerPrefs.selectedFeatureDependency;
                if (string.IsNullOrEmpty(selectedDependencyPackageId) || !dependencies.Any(d => d.name == selectedDependencyPackageId))
                {
                    selectedDependencyPackageId = dependencies[0].name;
                    m_PackageManagerPrefs.selectedFeatureDependency = selectedDependencyPackageId;
                }
                version = m_PackageDatabase.GetPackageInFeatureVersion(selectedDependencyPackageId);
            }
            foreach (var item in dependencyList.Children().OfType<FeatureDependencyItem>())
                item.EnableInClassList(k_SelectedClassName, item.packageVersion == version);

            dependencyTitle.value = version.displayName;

            var installedPackageVersion = m_PackageDatabase.GetPackage(version)?.versions.installed;
            dependencyVersion.value = installedPackageVersion != null && installedPackageVersion != version ? string.Format(L10n.Tr("Version {0} (Installed {1})"), version.versionString, installedPackageVersion.versionString) : string.Format(L10n.Tr("Version {0}"), version.versionString);

            var featureState = GetFeatureState(version);
            versionState.ClearClassList();
            if (featureState == FeatureState.Info)
            {
                versionState.AddToClassList(featureState.ToString().ToLower());
                versionState.tooltip = string.Format(L10n.Tr("Using version {0} because at least one other package or feature depends on it"), installedPackageVersion.versionString);
            }
            dependencyDesc.value = version.description;

            var tab = PackageFilterTab.UnityRegistry;
            if (version.isDirectDependency || m_SettingsProxy.enablePackageDependencies)
                tab = PackageFilterTab.InProject;
            dependencyLink.clickable.clicked += () => PackageManagerWindow.SelectPackageAndFilterStatic(version.name, tab);

            UIUtils.SetElementDisplay(dependencyInfoBox, featureState == FeatureState.Customized);
            dependencyInfoBox.text = installedPackageVersion?.HasTag(PackageTag.Custom) ?? false ?
                L10n.Tr("This package has been customized.") : L10n.Tr("This package has been manually changed.");
        }

        private VisualElementCache cache { get; set; }
        private Toggle dependenciesExpander => cache.Get<Toggle>("dependenciesExpander");
        private VisualElement dependenciesOuterContainer => cache.Get<VisualElement>("featureDependenciesOuterContainer");
        private VisualElement dependencyList => cache.Get<VisualElement>("featureDependenciesList");
        private SelectableLabel dependencyTitle => cache.Get<SelectableLabel>("featureDependencyTitle");
        private SelectableLabel dependencyVersion => cache.Get<SelectableLabel>("featureDependencyVersion");
        private Label versionState => cache.Get<Label>("versionState");
        private SelectableLabel dependencyDesc => cache.Get<SelectableLabel>("featureDependencyDesc");
        private HelpBox dependencyInfoBox => cache.Get<HelpBox>("featureDependencyInfoBox");
        private VisualElement fillerLeftContainer => cache.Get<VisualElement>("fillerLeftContainer");
        private Button dependencyLink => cache.Get<Button>("featureDependencyLink");
    }
}
