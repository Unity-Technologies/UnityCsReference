// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class AssetDatabase
    {
        public static string[] FindAssets(string filter)
        {
            return FindAssets(filter, null);
        }

        public static GUID[] FindAssetGUIDs(string filter)
        {
            return FindAssetGUIDs(filter, null);
        }
        private static SearchFilter CreateSearchFilter(string filter, string[] searchInFolders)
        {
            var searchFilter = new SearchFilter { searchArea = SearchFilter.SearchArea.AllAssets };
            SearchUtility.ParseSearchString(filter, searchFilter);
            if (searchInFolders != null && searchInFolders.Length > 0)
            {
                searchFilter.folders = searchInFolders;
                searchFilter.searchArea = SearchFilter.SearchArea.SelectedFolders;
            }

            return searchFilter;
        }
        public static string[] FindAssets(string filter, string[] searchInFolders)
        {
            var searchFilter = CreateSearchFilter(filter, searchInFolders);
            return FindAssets(searchFilter);
        }
        public static GUID[] FindAssetGUIDs(string filter, string[] searchInFolders)
        {
            var searchFilter = CreateSearchFilter(filter, searchInFolders);
            return FindAssetGUIDs(searchFilter);
        }
        internal static string[] FindAssets(SearchFilter searchFilter)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return FindAllAssets(searchFilter).Select(property => property.guid).Distinct().ToArray();
#pragma warning restore UA2001
        }
        internal static GUID[] FindAssetGUIDs(SearchFilter searchFilter)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return FindAllAssets(searchFilter).Select(property => property.assetGUID).Distinct().ToArray();
#pragma warning restore UA2001
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.ShaderFoundryModule")]
        internal static IEnumerable<HierarchyIterator> FindAllAssets(SearchFilter searchFilter)
        {
            var enumerator = EnumerateAllAssets(searchFilter);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        internal static IEnumerator<HierarchyIterator> EnumerateAllAssets(SearchFilter searchFilter)
        {
            if (searchFilter.folders != null && searchFilter.folders.Length > 0 && searchFilter.searchArea == SearchFilter.SearchArea.SelectedFolders)
                return FindInFolders(searchFilter, p => p);

            return FindEverywhere(searchFilter, p => p);
        }

        private static IEnumerator<T> FindInFolders<T>(SearchFilter searchFilter, Func<HierarchyIterator,  T> selector)
        {
            var folders = new List<string>();
            folders.AddRange(searchFilter.folders);
            if (folders.Remove(PackageManager.Folders.GetPackagesPath()))
            {
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(searchFilter.skipHidden);
                foreach (var package in packages)
                {
                    if (!folders.Contains(package.assetPath))
                        folders.Add(package.assetPath);
                }
            }

            HierarchyIterator propertyWithFilter = null;
            foreach (var folderPath in folders)
            {
                var sanitizedFolderPath = folderPath.ConvertSeparatorsToUnity().TrimTrailingSlashes();
                var folderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(sanitizedFolderPath);
                var rootPath = "Assets";

                // Find the right rootPath if folderPath is part of a package
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(sanitizedFolderPath);
                if (packageInfo != null)
                {
                    rootPath = packageInfo.assetPath;
                    if (searchFilter.skipHidden && !PackageManagerUtilityInternal.IsPathInVisiblePackage(rootPath))
                        continue;
                }

                // Set empty filter to ensure we search all assets to find folder
                var property = new HierarchyIterator(rootPath);
                property.SetSearchFilter(new SearchFilter());
                if (property.Find(folderInstanceID, null))
                {
                    // Set filter after we found the folder
                    if (propertyWithFilter != null)
                        property.CopySearchFilterFrom(propertyWithFilter);
                    else
                    {
                        property.SetSearchFilter(searchFilter);
                        propertyWithFilter = property;
                    }

                    int folderDepth = property.depth;
                    EntityId[] expanded = null; // enter all children of folder
                    while (property.NextWithDepthCheck(expanded, folderDepth + 1))
                    {
                        yield return selector(property);
                    }
                }
                else
                {
                    Debug.LogWarning("AssetDatabase.FindAssets: Folder not found: '" + sanitizedFolderPath + "'");
                }
            }
        }

        private static IEnumerator<T> FindEverywhere<T>(SearchFilter searchFilter, Func<HierarchyIterator, T> selector)
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
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(searchFilter.skipHidden);
                foreach (var package in packages)
                {
                    rootPaths.Add(package.assetPath);
                }
            }

            HierarchyIterator lastProperty = null;
            foreach (var rootPath in rootPaths)
            {
                var property = new HierarchyIterator(rootPath);
                if (lastProperty != null)
                    property.CopySearchFilterFrom(lastProperty);
                else
                    property.SetSearchFilter(searchFilter);
                lastProperty = property;
                while (property.Next(null))
                {
                    yield return selector(property);
                }
            }
        }
    }
}
