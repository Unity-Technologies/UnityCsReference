// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class AssetDatabase
    {
        public static string[] FindAssets(string filter)
        {
            return FindAssets(filter, null);
        }

        public static string[] FindAssets(string filter, string[] searchInFolders)
        {
            var searchFilter = new SearchFilter { searchArea = SearchFilter.SearchArea.AllAssets };
            SearchUtility.ParseSearchString(filter, searchFilter);
            if (searchInFolders != null && searchInFolders.Length > 0)
            {
                searchFilter.folders = searchInFolders;
                searchFilter.searchArea = SearchFilter.SearchArea.SelectedFolders;
            }

            return FindAssets(searchFilter);
        }

        internal static string[] FindAssets(SearchFilter searchFilter)
        {
            return FindAllAssets(searchFilter).Select(property => property.guid).ToArray();
        }

        internal static IEnumerable<HierarchyProperty> FindAllAssets(SearchFilter searchFilter)
        {
            var enumerator = EnumerateAllAssets(searchFilter);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        internal static IEnumerator<HierarchyProperty> EnumerateAllAssets(SearchFilter searchFilter)
        {
            if (searchFilter.folders != null && searchFilter.folders.Length > 0 && searchFilter.searchArea == SearchFilter.SearchArea.SelectedFolders)
                return FindInFolders(searchFilter, p => p);

            return FindEverywhere(searchFilter, p => p);
        }

        private static IEnumerator<T> FindInFolders<T>(SearchFilter searchFilter, Func<HierarchyProperty,  T> selector)
        {
            var folders = new List<string>();
            folders.AddRange(searchFilter.folders);
            if (folders.Remove(PackageManager.Folders.GetPackagesMountPoint()))
            {
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages();
                foreach (var package in packages)
                {
                    if (!folders.Contains(package.assetPath))
                        folders.Add(package.assetPath);
                }
            }

            foreach (string folderPath in folders)
            {
                var folderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(folderPath);
                var rootPath = "Assets";

                // Find the right rootPath if folderPath is part of a package
                var packageInfo = PackageManager.Packages.GetForAssetPath(folderPath);
                if (packageInfo != null)
                    rootPath = packageInfo.assetPath;

                // Set empty filter to ensure we search all assets to find folder
                var property = new HierarchyProperty(rootPath);
                property.SetSearchFilter(new SearchFilter());
                if (property.Find(folderInstanceID, null))
                {
                    // Set filter after we found the folder
                    property.SetSearchFilter(searchFilter);
                    int folderDepth = property.depth;
                    int[] expanded = null; // enter all children of folder
                    while (property.NextWithDepthCheck(expanded, folderDepth + 1))
                    {
                        yield return selector(property);
                    }
                }
                else
                {
                    Debug.LogWarning("AssetDatabase.FindAssets: Folder not found: '" + folderPath + "'");
                }
            }
        }

        private static IEnumerator<T> FindEverywhere<T>(SearchFilter searchFilter, Func<HierarchyProperty, T> selector)
        {
            var rootPaths = new List<string>();
            if (searchFilter.searchArea == SearchFilter.SearchArea.AllAssets ||
                searchFilter.searchArea == SearchFilter.SearchArea.InAssetsOnly)
            {
                rootPaths.Add("Assets");
            }
            if (searchFilter.searchArea == SearchFilter.SearchArea.AllAssets ||
                searchFilter.searchArea == SearchFilter.SearchArea.InPackagesOnly)
            {
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages();
                foreach (var package in packages)
                {
                    rootPaths.Add(package.assetPath);
                }
            }
            foreach (var rootPath in rootPaths)
            {
                var property = new HierarchyProperty(rootPath);
                property.SetSearchFilter(searchFilter);
                while (property.Next(null))
                {
                    yield return selector(property);
                }
            }
        }
    }
}
