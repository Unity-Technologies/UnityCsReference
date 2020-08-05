// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        private string BuildNameText(DependencyInfo dependency)
        {
            var packageVersion = PackageDatabase.instance.GetPackageVersion(dependency);
            return packageVersion != null ? packageVersion.displayName : UpmPackageVersion.ExtractDisplayName(dependency.name);
        }

        // TODO: In the original RebuildDependenciesDictionnary function there's some handling for dependencies that looks like this:
        //  foreach (var dependency in dependencies) {if (dependency.version.StartsWith("file:")) }
        // need to figure out what is that used for
        private string BuildStatusText(DependencyInfo dependency)
        {
            var installedVersion = PackageDatabase.instance.GetPackage(dependency.name)?.installedVersion;
            if (installedVersion == null)
                return string.Empty;

            if (installedVersion.HasTag(PackageTag.InDevelopment))
                return "(in development)";

            if (installedVersion.HasTag(PackageTag.Local))
                return "(local)";

            var statusText = installedVersion.HasTag(PackageTag.BuiltIn) ? "enabled" : "installed";
            return installedVersion.version == dependency.version
                ? $"({statusText} \u2714)" : $"({installedVersion.version} {statusText} \u2714)";
        }

        public void SetDependencies(DependencyInfo[] dependencies)
        {
            var showDependency = PackageManagerPrefs.instance.showPackageDependencies && dependencies != null;
            UIUtils.SetElementDisplay(this, showDependency);

            if (!showDependency || dependencies.Length == 0)
            {
                ClearDependencies();
                return;
            }

            dependenciesNames.Clear();
            dependenciesVersions.Clear();
            dependenciesStatuses.Clear();
            foreach (var dependency in dependencies)
            {
                dependenciesNames.Add(BuildLabel(BuildNameText(dependency), "text"));
                dependenciesVersions.Add(BuildLabel(dependency.version, "text"));
                dependenciesStatuses.Add(BuildLabel(BuildStatusText(dependency), "text"));
            }

            UIUtils.SetElementDisplay(noDependencies, false);
            UIUtils.SetElementDisplay(dependenciesNames, true);
            UIUtils.SetElementDisplay(dependenciesVersions, true);
        }

        private void ClearDependencies()
        {
            dependenciesNames.Clear();
            dependenciesVersions.Clear();
            dependenciesStatuses.Clear();

            UIUtils.SetElementDisplay(noDependencies, true);
            UIUtils.SetElementDisplay(dependenciesNames, false);
            UIUtils.SetElementDisplay(dependenciesVersions, false);
        }

        private VisualElementCache cache { get; set; }

        private VisualElement dependenciesContainer { get { return cache.Get<VisualElement>("dependenciesContainer"); } }
        private Label noDependencies { get { return cache.Get<Label>("noDependencies"); } }
        private VisualElement dependenciesNames { get { return cache.Get<VisualElement>("dependenciesNames"); } }
        private VisualElement dependenciesVersions { get { return cache.Get<VisualElement>("dependenciesVersions"); } }
        private VisualElement dependenciesStatuses { get { return cache.Get<VisualElement>("dependenciesStatuses"); } }
    }
}
