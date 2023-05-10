// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsDependenciesTab : PackageDetailsTabElement
    {
        public const string k_Id = "dependencies";

        private const int k_DependencyViewSwitchWidthBreakpoint = 420;

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.HasTag(PackageTag.UpmFormat) && !version.HasTag(PackageTag.Feature) && !version.HasTag(PackageTag.Placeholder);
        }

        private readonly PackageDatabase m_PackageDatabase;
        public PackageDetailsDependenciesTab(UnityConnectProxy unityConnect, ResourceLoader resourceLoader,
            PackageDatabase packageDatabase) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Dependencies");
            m_PackageDatabase = packageDatabase;

            var root = resourceLoader.GetTemplate("PackageDetailsDependenciesTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            UpdateDependencies(version?.dependencies);
            UpdateReverseDependencies(m_PackageDatabase.GetReverseDependencies(version));
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Math.Abs(evt.newRect.width - evt.oldRect.width) > 1.0f)
                ToggleLowWidthDependencyView(evt.newRect.width);
        }

        private void ToggleLowWidthDependencyView(float width)
        {
            if (width <= k_DependencyViewSwitchWidthBreakpoint)
            {
                dependenciesNames.Query<SelectableLabel>().ForEach(x => x.AddToClassList("dependencyLabelLowWidth"));
                reverseDependenciesNames.Query<SelectableLabel>().ForEach(x => x.AddToClassList("dependencyLabelLowWidth"));
            }
            else
            {
                dependenciesNames.Query<SelectableLabel>().ForEach(x => x.RemoveFromClassList("dependencyLabelLowWidth"));
                reverseDependenciesNames.Query<SelectableLabel>().ForEach(x => x.RemoveFromClassList("dependencyLabelLowWidth"));
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

        public static string GetNameText(DependencyInfo dependency, IPackage package, IPackageVersion version)
        {
            return version?.displayName ?? package?.displayName ?? dependency.name;
        }

        public static string GetVersionText(DependencyInfo dependency, IPackageVersion version)
        {
            if (version == null)
                return dependency.version;
            if (version.HasTag(PackageTag.Feature))
                return string.Empty;
            if (version.HasTag(PackageTag.BuiltIn))
                return "-";
            return dependency.version;
        }

        private static string GetVersionText(IPackageVersion packageVersion)
        {
            if (packageVersion == null || packageVersion.HasTag(PackageTag.Feature))
                return string.Empty;
            if (packageVersion.HasTag(PackageTag.BuiltIn))
                return "-";
            return packageVersion.version.ToString();
        }

        public static string GetStatusText(DependencyInfo dependency, IPackageVersion installedVersion)
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
                m_PackageDatabase.GetPackageAndVersion(dependency, out var package, out var version);

                var nameText = GetNameText(dependency, package, version);
                var versionText = GetVersionText(dependency, version);
                var statusText = GetStatusText(dependency, package?.versions.installed);

                dependenciesNames.Add(BuildSelectableLabel(nameText, "text"));
                dependenciesVersions.Add(BuildSelectableLabel(versionText, "text"));
                dependenciesStatuses.Add(BuildLabel(statusText, "text"));
            }

            ToggleLowWidthDependencyView(rect.width);
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
                var nameText = version.displayName ?? string.Empty;
                var versionText = GetVersionText(version);

                reverseDependenciesNames.Add(BuildSelectableLabel(nameText, "text"));
                reverseDependenciesVersions.Add(BuildSelectableLabel(versionText, "text"));
            }

            ToggleLowWidthDependencyView(rect.width);
        }

        private readonly VisualElementCache m_Cache;
        private Label noDependencies => m_Cache.Get<Label>("noDependencies");
        private VisualElement dependenciesNames => m_Cache.Get<VisualElement>("dependenciesNames");
        private VisualElement dependenciesVersions => m_Cache.Get<VisualElement>("dependenciesVersions");
        private VisualElement dependenciesStatuses => m_Cache.Get<VisualElement>("dependenciesStatuses");

        private Label noReverseDependencies => m_Cache.Get<Label>("noReverseDependencies");
        private VisualElement reverseDependenciesNames => m_Cache.Get<VisualElement>("reverseDependenciesNames");
        private VisualElement reverseDependenciesVersions => m_Cache.Get<VisualElement>("reverseDependenciesVersions");
    }
}
