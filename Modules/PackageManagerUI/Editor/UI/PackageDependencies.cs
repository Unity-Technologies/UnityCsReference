// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDependencies : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDependencies> {}

        private ResourceLoader m_ResourceLoader;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        public PackageDependencies()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageDependencies.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            SetExpanded(m_PackageManagerPrefs.dependenciesExpanded);
            dependenciesExpander.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void SetExpanded(bool expanded)
        {
            if (dependenciesExpander.value != expanded)
                dependenciesExpander.value = expanded;
            if (m_PackageManagerPrefs.dependenciesExpanded != expanded)
                m_PackageManagerPrefs.dependenciesExpanded = expanded;
            UIUtils.SetElementDisplay(dependenciesOuterContainer, expanded);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            float newWidth = evt.newRect.width;
            ToggleLowWidthDependencyView(newWidth);
        }

        private void ToggleLowWidthDependencyView(float width)
        {
            if (width <= 420)
            {
                UIUtils.SetElementDisplay(dependenciesListLowWidth, true);
                UIUtils.SetElementDisplay(reverseDependenciesListLowWidth, true);
                UIUtils.SetElementDisplay(dependenciesNormalView, false);
                UIUtils.SetElementDisplay(reverseDependenciesNormalView, false);
            }
            else
            {
                UIUtils.SetElementDisplay(dependenciesListLowWidth, false);
                UIUtils.SetElementDisplay(reverseDependenciesListLowWidth, false);
                UIUtils.SetElementDisplay(dependenciesNormalView, true);
                UIUtils.SetElementDisplay(reverseDependenciesNormalView, true);
            }
        }

        private static Label BuildLabel(string text, string clazz)
        {
            var label = new Label(text);
            label.AddToClassList(clazz);
            label.tooltip = text;
            return label;
        }

        private static SelectableLabel BuildSelectableLabel(string text, string clazz)
        {
            var label = new SelectableLabel();
            label.SetValueWithoutNotify(text);
            label.AddToClassList(clazz);
            label.tooltip = text;
            return label;
        }

        private string GetNameText(DependencyInfo dependency, IPackage package, IPackageVersion version)
        {
            return version?.displayName ?? package?.displayName ?? dependency.name;
        }

        private string GetVersionText(DependencyInfo dependency, IPackage package)
        {
            if (package == null || package.Is(PackageType.Feature))
                return string.Empty;
            if (package.Is(PackageType.BuiltIn))
                return "---";
            return dependency.version;
        }

        private string GetVersionText(IPackageVersion packageVersion)
        {
            if (packageVersion == null || packageVersion.HasTag(PackageTag.Feature))
                return string.Empty;
            if (packageVersion.HasTag(PackageTag.BuiltIn))
                return "---";
            return packageVersion.version.ToString();
        }

        private string GetStatusText(DependencyInfo dependency, IPackageVersion installedVersion)
        {
            if (installedVersion == null)
                return string.Empty;

            if (installedVersion.HasTag(PackageTag.Custom))
                return L10n.Tr("(custom)");

            if (installedVersion.HasTag(PackageTag.Local))
                return L10n.Tr("(local)");

            var statusText = installedVersion.HasTag(PackageTag.BuiltIn)
                ? L10n.Tr("enabled") : L10n.Tr("installed");
            return installedVersion.version?.ToString() == dependency.version
                ? string.Format("({0})", statusText) : string.Format("({0} {1})", installedVersion.version, statusText);
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            if (!m_SettingsProxy.enablePackageDependencies || version?.HasTag(PackageTag.Feature) == true)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }

            var dependencies = version?.dependencies;
            var reverseDependencies = m_PackageDatabase.GetReverseDependencies(version);
            var showDependency = dependencies != null || reverseDependencies != null;
            UIUtils.SetElementDisplay(this, showDependency);
            if (!showDependency)
                return;

            UpdateDependencies(dependencies);
            UpdateReverseDependencies(reverseDependencies);
        }

        private void UpdateDependencies(DependencyInfo[] dependencies)
        {
            dependenciesNames.Clear();
            dependenciesVersions.Clear();
            dependenciesStatuses.Clear();
            dependenciesListLowWidth.Clear();

            var hasDependencies = dependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noDependencies, !hasDependencies);
            UIUtils.SetElementDisplay(dependenciesNames, hasDependencies);
            UIUtils.SetElementDisplay(dependenciesVersions, hasDependencies);

            if (!hasDependencies)
                return;

            foreach (var dependency in dependencies)
            {
                m_PackageDatabase.GetPackageAndVersion(dependency, out var package, out var version);

                var nameText = GetNameText(dependency, package, version);
                var versionText = GetVersionText(dependency, package);
                var statusText = GetStatusText(dependency, package?.versions.installed);

                dependenciesNames.Add(BuildSelectableLabel(nameText, "text"));
                dependenciesVersions.Add(BuildSelectableLabel(versionText, "text"));
                dependenciesStatuses.Add(BuildLabel(statusText, "text"));

                var dependencyLowWidthItem = new PackageDependencySampleItemLowWidth(nameText, versionText, BuildLabel(statusText, "text"));
                dependenciesListLowWidth.Add(dependencyLowWidthItem);
            }
        }

        private void UpdateReverseDependencies(IEnumerable<IPackageVersion> reverseDependencies)
        {
            reverseDependenciesNames.Clear();
            reverseDependenciesVersions.Clear();
            reverseDependenciesListLowWidth.Clear();

            var hasReverseDependencies = reverseDependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noReverseDependencies, !hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesNames, hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesVersions, hasReverseDependencies);

            if (!hasReverseDependencies)
                return;

            foreach (var version in reverseDependencies)
            {
                var nameText = version.displayName ?? string.Empty;
                var versionText = GetVersionText(version);

                reverseDependenciesNames.Add(BuildSelectableLabel(nameText, "text"));
                reverseDependenciesVersions.Add(BuildSelectableLabel(versionText, "text"));

                var reverseDependencyLowWidthItem = new PackageDependencySampleItemLowWidth(nameText, versionText, null);
                reverseDependenciesListLowWidth.Add(reverseDependencyLowWidthItem);
            }
        }

        private VisualElementCache cache { get; set; }
        private Toggle dependenciesExpander { get { return cache.Get<Toggle>("dependenciesExpander"); } }
        private VisualElement dependenciesOuterContainer { get { return cache.Get<VisualElement>("dependenciesOuterContainer"); } }
        private VisualElement dependenciesNormalView { get { return cache.Get<VisualElement>("dependenciesNormalView"); } }
        private Label noDependencies { get { return cache.Get<Label>("noDependencies"); } }
        private VisualElement dependenciesNames { get { return cache.Get<VisualElement>("dependenciesNames"); } }
        private VisualElement dependenciesVersions { get { return cache.Get<VisualElement>("dependenciesVersions"); } }
        private VisualElement dependenciesStatuses { get { return cache.Get<VisualElement>("dependenciesStatuses"); } }
        private VisualElement dependenciesListLowWidth { get { return cache.Get<VisualElement>("dependenciesListLowWidth"); } }

        private VisualElement reverseDependenciesNormalView { get { return cache.Get<VisualElement>("reverseDependenciesNormalView"); } }
        private Label noReverseDependencies { get { return cache.Get<Label>("noReverseDependencies"); } }
        private VisualElement reverseDependenciesNames { get { return cache.Get<VisualElement>("reverseDependenciesNames"); } }
        private VisualElement reverseDependenciesVersions { get { return cache.Get<VisualElement>("reverseDependenciesVersions"); } }
        private VisualElement reverseDependenciesListLowWidth { get { return cache.Get<VisualElement>("reverseDependenciesListLowWidth"); } }
    }
}
