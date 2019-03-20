// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDependencies : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDependencies> {}

        private readonly VisualElement root;
        private PackageCollection Collection;

        public PackageDependencies()
        {
            root = Resources.GetTemplate("PackageDependencies.uxml");
            Add(root);
            Cache = new VisualElementCache(root);
        }

        private static Label BuildLabel(string text, string clazz)
        {
            var label = new Label(text);
            label.AddToClassList(clazz);
            return label;
        }

        private string BuildNameText(DependencyInfo dependency)
        {
            var packageInfo = Collection.LatestSearchPackages.FirstOrDefault(p => p.Name == dependency.name);
            if (packageInfo != null)
            {
                return packageInfo.DisplayName;
            }

            return dependency.name;
        }

        private string BuildStatusText(DependencyInfo dependency)
        {
            SemVersion installedVersion;
            if (Collection.ProjectDependencies.TryGetValue(dependency.name, out installedVersion))
            {
                if (installedVersion == PackageCollection.EmbdeddedVersion)
                    return "(in development)";

                if (installedVersion == PackageCollection.LocalVersion)
                    return "(local)";

                return installedVersion == dependency.version
                    ? "(installed \u2714)"
                    : string.Format("({0} installed \u2714)", installedVersion);
            }

            return string.Empty;
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
        }

        public void SetDependencies(DependencyInfo[] dependencies)
        {
            if (dependencies == null || dependencies.Length == 0)
            {
                ClearDependencies();
                return;
            }

            DependenciesNames.Clear();
            DependenciesVersions.Clear();
            DependenciesStatuses.Clear();
            foreach (var dependency in dependencies)
            {
                DependenciesNames.Add(BuildLabel(BuildNameText(dependency), "text"));
                DependenciesVersions.Add(BuildLabel(dependency.version, "text"));
                DependenciesStatuses.Add(BuildLabel(BuildStatusText(dependency), "text"));
            }

            UIUtils.SetElementDisplay(NoDependencies, false);
            UIUtils.SetElementDisplay(DependenciesNames, true);
            UIUtils.SetElementDisplay(DependenciesVersions, true);
        }

        private void ClearDependencies()
        {
            DependenciesNames.Clear();
            DependenciesVersions.Clear();
            DependenciesStatuses.Clear();

            UIUtils.SetElementDisplay(NoDependencies, true);
            UIUtils.SetElementDisplay(DependenciesNames, false);
            UIUtils.SetElementDisplay(DependenciesVersions, false);
        }

        private VisualElementCache Cache { get; set; }

        private VisualElement DependenciesContainer { get { return Cache.Get<VisualElement>("dependenciesContainer"); } }
        private Label NoDependencies { get { return Cache.Get<Label>("noDependencies"); } }
        private VisualElement DependenciesNames { get { return Cache.Get<VisualElement>("dependenciesNames"); } }
        private VisualElement DependenciesVersions { get { return Cache.Get<VisualElement>("dependenciesVersions"); } }
        private VisualElement DependenciesStatuses { get { return Cache.Get<VisualElement>("dependenciesStatuses"); } }
    }
}
