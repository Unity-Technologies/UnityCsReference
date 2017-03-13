// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;


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
            SearchFilter searchFilter = new SearchFilter();
            SearchUtility.ParseSearchString(filter, searchFilter);
            if (searchInFolders != null)
                searchFilter.folders = searchInFolders;

            return FindAssets(searchFilter);
        }

        private static string[] FindAssets(SearchFilter searchFilter)
        {
            if (searchFilter.folders != null && searchFilter.folders.Length > 0)
                return SearchInFolders(searchFilter);
            return SearchAllAssets(searchFilter);
        }

        private static string[] SearchAllAssets(SearchFilter searchFilter)
        {
            var property = new HierarchyProperty(HierarchyType.Assets);
            property.SetSearchFilter(searchFilter);
            property.Reset();
            var guids = new List<string>();
            while (property.Next(null))
            {
                guids.Add(property.guid);
            }
            return guids.ToArray();
        }

        private static string[] SearchInFolders(SearchFilter searchFilter)
        {
            var property = new HierarchyProperty(HierarchyType.Assets);
            var guids = new List<string>();
            foreach (string folderPath in searchFilter.folders)
            {
                // Set empty filter to ensure we search all assets to find folder
                property.SetSearchFilter(new SearchFilter());
                int folderInstanceID = GetMainAssetInstanceID(folderPath);
                if (property.Find(folderInstanceID, null))
                {
                    // Set filter after we found the folder
                    property.SetSearchFilter(searchFilter);
                    int folderDepth = property.depth;
                    int[] expanded = null; // enter all children of folder
                    while (property.NextWithDepthCheck(expanded, folderDepth + 1))
                    {
                        guids.Add(property.guid);
                    }
                }
                else
                {
                    Debug.LogWarning("AssetDatabase.FindAssets: Folder not found: '" + folderPath + "'");
                }
            }
            return guids.ToArray();
        }
    }
}
