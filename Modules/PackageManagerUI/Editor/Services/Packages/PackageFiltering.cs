// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PackageFiltering
    {
        public virtual event Action<PackageFilterTab> onFilterTabChanged = delegate {};
        public virtual event Action<string> onSearchTextChanged = delegate {};

        public const PackageFilterTab k_DefaultFilterTab = PackageFilterTab.InProject;

        public virtual PackageFilterTab? previousFilterTab { get; private set; } = null;

        [SerializeField]
        private PackageFilterTab m_CurrentFilterTab;
        [SerializeField]
        private bool m_CurrentFilterTabInitialized;
        public virtual PackageFilterTab currentFilterTab
        {
            get { return m_CurrentFilterTabInitialized ? m_CurrentFilterTab : k_DefaultFilterTab; }

            set
            {
                if (value != currentFilterTab)
                {
                    previousFilterTab = currentFilterTab;
                    m_CurrentFilterTab = value;
                    m_CurrentFilterTabInitialized = true;
                    onFilterTabChanged?.Invoke(m_CurrentFilterTab);
                }
            }
        }

        [SerializeField]
        private string m_CurrentSearchText;
        public virtual string currentSearchText
        {
            get { return m_CurrentSearchText; }

            set
            {
                value = value ?? string.Empty;
                if (value != m_CurrentSearchText)
                {
                    m_CurrentSearchText = value;
                    onSearchTextChanged?.Invoke(m_CurrentSearchText);
                }
            }
        }

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(UnityConnectProxy unityConnect, PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_UnityConnect = unityConnect;
            m_SettingsProxy = settingsProxy;
        }

        internal static bool FilterByTab(IPackage package, PackageFilterTab tab, bool showDependencies, bool isLoggedIn)
        {
            switch (tab)
            {
                case PackageFilterTab.BuiltIn:
                    return package.Is(PackageType.BuiltIn);
                case PackageFilterTab.UnityRegistry:
                    return !package.Is(PackageType.BuiltIn) && package.Is(PackageType.Upm) && package.versions.Any(v => v.availableRegistry == RegistryType.UnityRegistry) && (package.isDiscoverable || (package.versions.installed?.isDirectDependency ?? false));
                case PackageFilterTab.MyRegistries:
                    return package.Is(PackageType.Upm) && package.versions.Any(v => v.availableRegistry == RegistryType.MyRegistries) && (package.isDiscoverable || (package.versions.installed?.isDirectDependency ?? false));
                case PackageFilterTab.InProject:
                    return !package.Is(PackageType.BuiltIn) && (package.progress == PackageProgress.Installing || (package.versions.installed != null && (showDependencies || package.versions.installed.isDirectDependency)));
                case PackageFilterTab.AssetStore:
                    return isLoggedIn && package.Is(PackageType.AssetStore);
                default:
                    return false;
            }
        }

        internal static bool FilterByText(IPackage package, IPackageVersion version, string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (package == null || version == null)
                return false;

            if (version.name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (!string.IsNullOrEmpty(version.displayName) && version.displayName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            var prerelease = text.StartsWith("-") ? text.Substring(1) : text;
            if (version.version != null && ((SemVersion)version.version).Prerelease.IndexOf(prerelease, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            // searching for pre-release if search text matches with search term 'pre', case insensitive
            const string prereleaseSearchText = "Pre";
            if (version.HasTag(PackageTag.PreRelease) && prereleaseSearchText.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            // searching for experimental if search text matches with search term 'experimental', case insensitive
            const string experimentalSearchText = "Experimental";
            if (version.HasTag(PackageTag.Experimental) && experimentalSearchText.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.HasTag(PackageTag.Release) && PackageTag.Release.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.version?.StripTag().StartsWith(text, StringComparison.CurrentCultureIgnoreCase) == true)
                return true;

            if (!string.IsNullOrEmpty(version.category))
            {
                var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var categories = version.category.Split('/');
                if (words.All(word => word.Length >= 2 && categories.Any(category => category.StartsWith(word, StringComparison.CurrentCultureIgnoreCase))))
                    return true;
            }

            return false;
        }

        public virtual bool FilterByCurrentSearchText(IPackage package)
        {
            if (string.IsNullOrEmpty(currentSearchText))
                return true;

            var trimText = currentSearchText.Trim(' ', '\t');
            trimText = Regex.Replace(trimText, @"[ ]{2,}", " ");
            return string.IsNullOrEmpty(trimText) || FilterByText(package, package.versions.primary, trimText);
        }

        public virtual bool FilterByCurrentTab(IPackage package)
        {
            return FilterByTab(package, currentFilterTab, m_SettingsProxy.enablePackageDependencies, m_UnityConnect.isUserLoggedIn);
        }

        public virtual void SetCurrentFilterTabWithoutNotify(PackageFilterTab tab)
        {
            m_CurrentFilterTab = tab;
        }
    }
}
