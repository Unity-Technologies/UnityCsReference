// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageFiltering
    {
        public virtual event Action<PackageFilterTab> onFilterTabChanged = delegate {};
        public virtual event Action<string> onSearchTextChanged = delegate {};

        [SerializeField]
        private PackageFilterTab m_CurrentFilterTab;
        public virtual PackageFilterTab currentFilterTab
        {
            get { return m_CurrentFilterTab; }

            set
            {
                if (value != m_CurrentFilterTab)
                {
                    m_CurrentFilterTab = value;
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
        private PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(UnityConnectProxy unityConnect, PackageManagerPrefs packageManagerPrefs)
        {
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        internal static bool FilterByTab(IPackage package, PackageFilterTab tab, bool showDependencies, bool isLoggedIn)
        {
            switch (tab)
            {
                case PackageFilterTab.BuiltIn:
                    return package.Is(PackageType.BuiltIn);
                case PackageFilterTab.All:
                    return package.Is(PackageType.Installable) && (package.isDiscoverable || (package.versions.installed?.isDirectDependency ?? false));
                case PackageFilterTab.InProject:
                    return !package.Is(PackageType.BuiltIn) && package.versions.installed != null
                        && (showDependencies || package.versions.installed.isDirectDependency);
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

            if (version.HasTag(PackageTag.Preview) && PackageTag.Preview.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.HasTag(PackageTag.Verified) && PackageTag.Verified.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
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
            return FilterByTab(package, currentFilterTab, m_PackageManagerPrefs.showPackageDependencies, m_UnityConnect.isUserLoggedIn);
        }

        public virtual void SetCurrentFilterTabWithoutNotify(PackageFilterTab tab)
        {
            m_CurrentFilterTab = tab;
        }
    }
}
