// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageFiltering
    {
        static IPackageFiltering s_Instance = null;
        public static IPackageFiltering instance { get { return s_Instance ?? PackageFilteringInternal.instance; } }

        internal static bool FilterByTab(IPackage package, PackageFilterTab tab)
        {
            switch (tab)
            {
                case PackageFilterTab.Modules:
                    return package.Is(PackageType.BuiltIn);
                case PackageFilterTab.Unity:
                    return package.Is(PackageType.Installable) && package.Is(PackageType.Unity) && (package.isDiscoverable || (package.installedVersion?.isDirectDependency ?? false));
                case PackageFilterTab.Other:
                    return package.Is(PackageType.Installable) && package.Is(PackageType.ScopedRegistry) && (package.isDiscoverable || (package.installedVersion?.isDirectDependency ?? false));
                case PackageFilterTab.Local:
                    return !package.Is(PackageType.BuiltIn) && (package.installedVersion?.isDirectDependency ?? false);
                case PackageFilterTab.AssetStore:
                    return ApplicationUtil.instance.isUserLoggedIn && package.Is(PackageType.AssetStore);
                case PackageFilterTab.Custom:
                    return package.installedVersion?.HasTag(PackageTag.Custom | PackageTag.Local) ?? false;
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
            if (version.version != null && version.version.Prerelease.IndexOf(prerelease, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.HasTag(PackageTag.Preview) && PackageTag.Preview.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.HasTag(PackageTag.Verified) && PackageTag.Verified.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (version.version.StripTag().StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
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

        [Serializable]
        private class PackageFilteringInternal : ScriptableSingleton<PackageFilteringInternal>, IPackageFiltering
        {
            public event Action<PackageFilterTab> onFilterTabChanged = delegate {};
            public event Action<string> onSearchTextChanged = delegate {};

            public PackageFilterTab? previousFilterTab { get; private set; } = null;

            private PackageFilterTab m_CurrentFilterTab;
            public PackageFilterTab currentFilterTab
            {
                get { return m_CurrentFilterTab; }

                set
                {
                    if (value != m_CurrentFilterTab)
                    {
                        previousFilterTab = m_CurrentFilterTab;
                        m_CurrentFilterTab = value;
                        onFilterTabChanged?.Invoke(m_CurrentFilterTab);
                    }
                }
            }

            private string m_CurrentSearchText;
            public string currentSearchText
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

            private PackageFilteringInternal() {}

            public bool FilterByCurrentSearchText(IPackage package)
            {
                if (string.IsNullOrEmpty(currentSearchText))
                    return true;

                var trimText = currentSearchText.Trim(' ', '\t');
                trimText = Regex.Replace(trimText, @"[ ]{2,}", " ");
                return string.IsNullOrEmpty(trimText) || FilterByText(package, package.primaryVersion, trimText);
            }

            public bool FilterByCurrentTab(IPackage package)
            {
                return FilterByTab(package, currentFilterTab);
            }
        }
    }
}
