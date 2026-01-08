// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal enum FeatureDependencyState
    {
        None,
        Info,
        Customized
    }

    internal class FeatureDependencyItem : VisualElement
    {
        public IPackageVersion packageVersion { get; }
        public string packageName { get; }

        public FeatureDependencyItem(IPackageVersion featureVersion, IPackageVersion featureDependencyVersion, FeatureDependencyState state = FeatureDependencyState.None)
        {
            packageVersion = featureDependencyVersion;
            packageName = featureDependencyVersion.package.uniqueId;

            m_Name = new Label { name = "name" };
            m_Name.text = featureDependencyVersion?.displayName ?? string.Empty;
            Add(m_Name);

            m_State = new VisualElement { name = "versionState" };
            if (state == FeatureDependencyState.Customized && featureVersion.isInstalled)
            {
                m_State.AddToClassList(state.ToString().ToLower());
                m_State.tooltip = L10n.Tr("This package has been manually customized");
            }

            Add(m_State);
        }

        public FeatureDependencyItem(string dependencyName)
        {
            packageName = dependencyName;

            m_Name = new Label { name = "name" };
            m_Name.text = dependencyName;
            Add(m_Name);
        }

        private Label m_Name;
        private VisualElement m_State;
    }

    internal class FeatureDependenciesTab : PackageDetailsTabElement
    {
        public const string k_SelectedClassName = "selected";
        public const string k_Id = "packagesincluded";

        private IPackageVersion m_FeatureVersion;

        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IApplicationProxy m_Application;
        public FeatureDependenciesTab(IUnityConnectProxy unityConnect,
                                      IResourceLoader resourceLoader,
                                      IPackageDatabase packageDatabase,
                                      IPackageManagerPrefs packageManagerPrefs,
                                      IApplicationProxy applicationProxy)  : base(unityConnect)
        {
            m_PackageDatabase = packageDatabase;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_Application = applicationProxy;

            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Packages Included");

            var root = resourceLoader.GetTemplate("DetailsTabs/FeatureDependencies.uxml");
            m_ContentContainer.Add(root);

            cache = new VisualElementCache(root);
        }

        public FeatureDependencyState GetFeatureDependencyState(IPackageVersion version)
        {
            if (version == null)
                return FeatureDependencyState.None;

            var package = version.package;
            var installedVersion = package?.versions.installed;
            if (installedVersion == null)
                return FeatureDependencyState.None;

            var recommendedVersionExistsButNotInstalled = package.versions.recommended?.isInstalled == false;
            // User manually decide to install a different version
            if ((installedVersion.isDirectDependency && recommendedVersionExistsButNotInstalled) || installedVersion.HasTag(PackageTag.InDevelopment))
                return FeatureDependencyState.Customized;
            // The installed version is changed by the SAT solver, not overridden by user
            if (recommendedVersionExistsButNotInstalled)
                return FeatureDependencyState.Info;

            return FeatureDependencyState.None;
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version?.HasTag(PackageTag.Feature) == true;
        }

        protected override void RefreshContent(IPackageVersion version)
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
                var dependencyPackage = m_PackageDatabase.GetPackage(dependency.name);
                var packageVersion = dependencyPackage?.versions.recommended ?? dependencyPackage?.versions.primary;
                var featureDependencyState = GetFeatureDependencyState(packageVersion);

                var item = packageVersion != null ? new FeatureDependencyItem(version, packageVersion, featureDependencyState) : new FeatureDependencyItem(dependency.name);
                item.OnLeftClick(() =>
                {
                    OnDependencyItemClicked(packageVersion, dependency.name);
                });
                dependencyList.Add(item);
            }

            RefreshSelection();
        }

        private void OnDependencyItemClicked(IPackageVersion version, string dependencyName)
        {
            m_PackageManagerPrefs.selectedFeatureDependency = dependencyName;
            RefreshSelection(version);
        }

        private void RefreshSelection(IPackageVersion version = null)
        {
            var selectedDependencyPackageName = m_PackageManagerPrefs.selectedFeatureDependency;
            if (version == null)
            {
                var dependencies = m_FeatureVersion?.dependencies;
                if (dependencies == null || dependencies.Length == 0)
                    return;

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (string.IsNullOrEmpty(selectedDependencyPackageName) || !dependencies.Any(d => d.name == selectedDependencyPackageName))
#pragma warning restore RS0030
                {
                    selectedDependencyPackageName = dependencies[0].name;
                    m_PackageManagerPrefs.selectedFeatureDependency = selectedDependencyPackageName;
                }
                var dependencyPackage = m_PackageDatabase.GetPackage(selectedDependencyPackageName);
                version = dependencyPackage?.versions.recommended ?? dependencyPackage?.versions.primary;
            }

            // If the package is not installed and not discoverable, we have to display the package's ID name (ex: com.unity.adaptiveperformance.samsung.android)
            // and hide other elements in the package view
            var showElementsInDetailsView = version != null;

            UIUtils.SetElementDisplay(dependencyVersion, showElementsInDetailsView);
            UIUtils.SetElementDisplay(dependencyLink, showElementsInDetailsView);
            UIUtils.SetElementDisplay(dependencyInfoBox, showElementsInDetailsView);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var item in dependencyList.Children().OfType<FeatureDependencyItem>())
#pragma warning restore RS0030
                item.EnableInClassList(k_SelectedClassName, item.packageName == selectedDependencyPackageName);

            dependencyTitle.text = version?.displayName ?? selectedDependencyPackageName;
            dependencyDesc.text =  version?.description ?? L10n.Tr("This package will be automatically installed with this feature.");

            if (!showElementsInDetailsView)
                return;

            var installedPackageVersion = version.package?.versions.installed;
            dependencyVersion.text = installedPackageVersion != null && installedPackageVersion.versionString != version?.versionString ? string.Format(L10n.Tr("Version {0} (Installed {1})"), version.versionString, installedPackageVersion.versionString) : string.Format(L10n.Tr("Version {0}"), version.versionString);

            var featureDependencyState = GetFeatureDependencyState(version);
            versionState.ClearClassList();
            if (featureDependencyState == FeatureDependencyState.Info)
            {
                versionState.AddToClassList(featureDependencyState.ToString().ToLower());
                versionState.tooltip = string.Format(L10n.Tr("Using version {0} because at least one other package or feature depends on it"), installedPackageVersion.versionString);
            }

            var pageId = version.isDirectDependency ? InProjectPage.k_Id : UnityRegistryPage.k_Id;
            dependencyLink.clickable.clicked += () => PackageManagerWindow.OpenAndSelectPackage(version.name, pageId);

            UIUtils.SetElementDisplay(dependencyInfoBox, featureDependencyState == FeatureDependencyState.Customized);
            if (installedPackageVersion?.HasTag(PackageTag.Custom) ?? false)
            {
                dependencyInfoBox.readMoreUrl= $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/fs-details.html";
                dependencyInfoBox.text = L10n.Tr("This package has been customized.");
            }
            else
            {
                dependencyInfoBox.readMoreUrl= $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-ui-remove.html";
                dependencyInfoBox.text = L10n.Tr("This package has been manually changed.");
            }
        }

        private VisualElementCache cache { get; }
        private VisualElement dependencyList => cache.Get<VisualElement>("featureDependenciesList");
        private SelectableLabel dependencyTitle => cache.Get<SelectableLabel>("featureDependencyTitle");
        private SelectableLabel dependencyVersion => cache.Get<SelectableLabel>("featureDependencyVersion");
        private Label versionState => cache.Get<Label>("versionState");
        private SelectableLabel dependencyDesc => cache.Get<SelectableLabel>("featureDependencyDesc");
        private ExtendedHelpBox dependencyInfoBox => cache.Get<ExtendedHelpBox>("featureDependencyInfoBox");
        private Button dependencyLink => cache.Get<Button>("featureDependencyButton");
    }
}
