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

        [Serializable]
        private class PackageFilteringInternal : ScriptableSingleton<PackageFilteringInternal>, IPackageFiltering
        {
            private PackageFilterTab m_CurrentFilterTab;
            public PackageFilterTab currentFilterTab
            {
                get { return m_CurrentFilterTab; }

                set
                {
                    if (value != m_CurrentFilterTab)
                    {
                        m_CurrentFilterTab = value;
                        onFilterTabChanged(m_CurrentFilterTab);
                    }
                }
            }
            public event Action<PackageFilterTab> onFilterTabChanged = delegate {};

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
                        onSearchTextChanged(m_CurrentSearchText);
                    }
                }
            }
            public event Action<string> onSearchTextChanged = delegate {};

            private PackageFilteringInternal() {}

            private static bool FilterByText(IPackageVersion version, string text)
            {
                if (version == null)
                    return false;

                if (version.name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    return true;

                if (!string.IsNullOrEmpty(version.displayName) && version.displayName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    return true;

                if (!version.HasTag(PackageTag.BuiltIn))
                {
                    var prerelease = text.StartsWith("-") ? text.Substring(1) : text;
                    if (version.version != null && version.version.Prerelease.IndexOf(prerelease, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        return true;

                    if (version.version.StripTag().StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (version.HasTag(PackageTag.Preview))
                    {
                        if (PackageTag.Preview.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            return true;
                    }

                    if (version.HasTag(PackageTag.Verified))
                    {
                        if (PackageTag.Verified.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            return true;
                    }

                    if (version.HasTag(PackageTag.Core))
                    {
                        if (PackageTag.BuiltIn.ToString().IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            return true;
                    }
                }

                return false;
            }

            public bool FilterByCurrentSearchText(IPackage package)
            {
                if (string.IsNullOrEmpty(currentSearchText))
                    return true;

                var trimText = currentSearchText.Trim(' ', '\t');
                trimText = Regex.Replace(trimText, @"[ ]{2,}", " ");
                return string.IsNullOrEmpty(trimText) || FilterByText(package.primaryVersion, trimText);
            }

            public bool FilterByCurrentTab(IPackage package)
            {
                switch (currentFilterTab)
                {
                    case PackageFilterTab.Modules:
                        return package.versions.Any(v => v.HasTag(PackageTag.BuiltIn));
                    case PackageFilterTab.All:
                        return package.versions.Any(v => !v.HasTag(PackageTag.BuiltIn))
                            && (package.isDiscoverable || (package.installedVersion?.isDirectDependency ?? false));
                    case PackageFilterTab.Local:
                        return package.versions.Any(v => !v.HasTag(PackageTag.BuiltIn))
                            && (package.installedVersion?.isDirectDependency ?? false);
                    case PackageFilterTab.InDevelopment:
                        return package.installedVersion?.HasTag(PackageTag.InDevelopment) ?? false;
                    default:
                        break;
                }
                return false;
            }
        }
    }
}
