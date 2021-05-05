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

        private string GetNameText(DependencyInfo dependency)
        {
            m_PackageDatabase.GetPackageAndVersion(dependency, out var package, out var packageVersion);
            return packageVersion?.displayName ?? package?.displayName ?? dependency.name;
        }

        private string GetStatusText(DependencyInfo dependency)
        {
            var installedVersion = m_PackageDatabase.GetPackage(dependency.name)?.versions.installed;
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
            var dependencies = version?.dependencies;
            var reverseDependencies = m_PackageDatabase.GetReverseDependencies(version);
            var showDependency = m_SettingsProxy.enablePackageDependencies && (dependencies != null || reverseDependencies != null);
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
                dependenciesNames.Add(BuildSelectableLabel(GetNameText(dependency), "text"));
                dependenciesVersions.Add(BuildSelectableLabel(dependency.version, "text"));
                dependenciesStatuses.Add(BuildLabel(GetStatusText(dependency), "text"));

                var dependencyLowWidthItem = new PackageDependencySampleItemLowWidth(GetNameText(dependency), dependency.version.ToString(), BuildLabel(GetStatusText(dependency), "text"));
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
                reverseDependenciesNames.Add(BuildSelectableLabel(version.displayName ?? string.Empty, "text"));
                reverseDependenciesVersions.Add(BuildSelectableLabel(version.version.ToString(), "text"));

                var reverseDependencyLowWidthItem = new PackageDependencySampleItemLowWidth(version.displayName ?? string.Empty, version.version.ToString(), null);
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
