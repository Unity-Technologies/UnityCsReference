// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDependencies : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDependencies> {}

        public PackageDependencies()
        {
            var root = Resources.GetTemplate("PackageDependencies.uxml");
            Add(root);
            cache = new VisualElementCache(root);
        }

        private static Label BuildLabel(string text, string clazz)
        {
            var label = new Label(text);
            label.AddToClassList(clazz);
            return label;
        }

        private static string GetNameText(DependencyInfo dependency)
        {
            var packageVersion = PackageDatabase.instance.GetPackageVersion(dependency);
            return packageVersion != null ? packageVersion.displayName : UpmPackageVersion.ExtractDisplayName(dependency.name);
        }

        private static string GetStatusText(DependencyInfo dependency)
        {
            var installedVersion = PackageDatabase.instance.GetPackage(dependency.name)?.versions.installed;
            if (installedVersion == null)
                return string.Empty;

            if (installedVersion.HasTag(PackageTag.InDevelopment))
                return "(in development)";

            if (installedVersion.HasTag(PackageTag.Local))
                return "(local)";

            return installedVersion.version == dependency.version
                ? "(installed \u2714)" : $"({installedVersion.version} installed \u2714)";
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            var dependencies = version?.isInstalled == true ? version?.resolvedDependencies : version?.dependencies;
            var reverseDependencies = PackageDatabase.instance.GetReverseDependencies(version);
            var showDependency = PackageManagerPrefs.instance.showPackageDependencies && (dependencies != null || reverseDependencies != null);
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

            var hasDependencies = dependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noDependencies, !hasDependencies);
            UIUtils.SetElementDisplay(dependenciesNames, hasDependencies);
            UIUtils.SetElementDisplay(dependenciesVersions, hasDependencies);

            if (!hasDependencies)
                return;

            foreach (var dependency in dependencies)
            {
                dependenciesNames.Add(BuildLabel(GetNameText(dependency), "text"));
                dependenciesVersions.Add(BuildLabel(dependency.version, "text"));
                dependenciesStatuses.Add(BuildLabel(GetStatusText(dependency), "text"));
            }
        }

        private void UpdateReverseDependencies(IEnumerable<IPackageVersion> reverseDependencies)
        {
            reverseDependenciesNames.Clear();
            reverseDependenciesVersions.Clear();

            var hasReverseDependencies = reverseDependencies?.Any() ?? false;
            UIUtils.SetElementDisplay(noReverseDependencies, !hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesNames, hasReverseDependencies);
            UIUtils.SetElementDisplay(reverseDependenciesVersions, hasReverseDependencies);

            if (!hasReverseDependencies)
                return;

            foreach (var version in reverseDependencies)
            {
                reverseDependenciesNames.Add(BuildLabel(version.displayName ?? string.Empty, "text"));
                reverseDependenciesVersions.Add(BuildLabel(version.version.ToString(), "text"));
            }
        }

        private VisualElementCache cache { get; set; }

        private Label noDependencies { get { return cache.Get<Label>("noDependencies"); } }
        private VisualElement dependenciesNames { get { return cache.Get<VisualElement>("dependenciesNames"); } }
        private VisualElement dependenciesVersions { get { return cache.Get<VisualElement>("dependenciesVersions"); } }
        private VisualElement dependenciesStatuses { get { return cache.Get<VisualElement>("dependenciesStatuses"); } }

        private Label noReverseDependencies { get { return cache.Get<Label>("noReverseDependencies"); } }
        private VisualElement reverseDependenciesNames { get { return cache.Get<VisualElement>("reverseDependenciesNames"); } }
        private VisualElement reverseDependenciesVersions { get { return cache.Get<VisualElement>("reverseDependenciesVersions"); } }
    }
}
