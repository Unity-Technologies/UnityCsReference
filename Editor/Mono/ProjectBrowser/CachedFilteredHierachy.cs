// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine.Assertions;

namespace UnityEditor
{
    internal partial class FilteredHierarchy
    {
        public class FilterResult
        {
            public EntityId entityId;
            public string name;
            public bool hasChildren;
            public int colorCode;
            public bool isMainRepresentation;
            public bool hasFullPreviewImage;
            public IconDrawStyle iconDrawStyle;
            public bool isFolder;
            public HierarchyType type;
            public GUID assetGUID;
            public string guid{ get { return assetGUID.ToString(); } }
            public Texture2D icon
            {
                get
                {
                    if (m_Icon == null)
                    {
                        if (type == HierarchyType.Assets)
                        {
                            // Note: Do not set m_Icon as GetCachedIcon uses its own cache that is cleared on reaching a max limit.
                            // This is because when having e.g very large projects (1000s of textures with unique icons) we do not want all icons loaded
                            // at the same time so don't keep a reference in m_Icon here
                            string path = entityId == EntityId.None ? null : AssetDatabase.GetAssetPath(entityId);

                            if (path != null)
                                // Finding icon based on only file extension fails in several ways, and a different approach have to be found.
                                // Using InternalEditorUtility.FindIconForFile first in revision f25945218bb6 / 29b23dbe4b5c introduced several regressions.
                                //  - Doesn't support custom user-assigned icons for monoscripts.
                                //  - Doesn't support open/closed folder icon destinction.
                                //  - The change only affected Two-Column mode in Project View and not One-Column mode, adding inconsistency.
                                //  - Doesn't support showing different icons for different prefab types, such as prefab variants.
                                // Support for specific file types based on file extensiom have to be supported inside AssetDatabase.GetCachedIcon
                                // itself to work correctly and universally. for e.g. uxml files from within GetCachedIcon without relying on FindIconForFile.
                                return AssetDatabase.GetCachedIcon(path) as Texture2D;

                            path = assetGUID.Empty() ? null : AssetDatabase.GUIDToAssetPath(assetGUID);
                            if (path != null)
                                return UnityEditorInternal.InternalEditorUtility.FindIconForFile(path);
                        }
                        else if (type == HierarchyType.GameObjects)
                        {
                            // GameObject thumbnail can be set to m_Icon since its an actual icon which means we have a limited set of them
                            Object go = EditorUtility.EntityIdToObject(entityId);
                            m_Icon = AssetPreview.GetMiniThumbnail(go);
                        }
                        else
                        {
                            Assert.IsTrue(false, "Unhandled HierarchyType");
                        }
                    }
                    return m_Icon;
                }
                set
                {
                    m_Icon = value;
                }
            }
            private Texture2D m_Icon;
        }

        SearchFilter m_SearchFilter = new SearchFilter();
        FilterResult[] m_Results = System.Array.Empty<FilterResult>();     // When filtering of folder we have all sub assets here
        FilterResult[] m_VisibleItems = System.Array.Empty<FilterResult>(); // Subset of m_Results used for showing/hiding sub assets

        SearchService.SceneSearchSessionHandler m_SearchSessionHandler = new SearchService.SceneSearchSessionHandler();
        SearchService.SearchSessionOptions m_SearchSessionOptions;

        HierarchyType m_HierarchyType;

        public const int maxSearchAddCount = 12000;

        public FilteredHierarchy(HierarchyType type)
        {
            m_HierarchyType = type;
            m_SearchSessionOptions = SearchService.SearchSessionOptions.Default;
        }

        public FilteredHierarchy(HierarchyType type, SearchService.SearchSessionOptions searchSessionOptions)
        {
            m_HierarchyType = type;
            m_SearchSessionOptions = searchSessionOptions;
        }

        public HierarchyType hierarchyType
        {
            get { return m_HierarchyType; }
        }

        public FilterResult[] results
        {
            get
            {
                if (m_VisibleItems.Length > 0)
                    return m_VisibleItems;
                return m_Results;
            }
        }
        public SearchFilter searchFilter
        {
            get { return m_SearchFilter; }
            set
            {
                if (m_SearchFilter.SetNewFilter(value))
                    ResultsChanged();
            }
        }

        public bool foldersFirst { get; set; }

        public void SetResults(EntityId[] instanceIDs)
        {
            var entityIdSet = new HashSet<EntityId>(instanceIDs);
            if (m_HierarchyType ==  HierarchyType.Assets)
            {
                var idsUnderEachRoot = new Dictionary<string, int>();
                for (int i = 0; i < instanceIDs.Length; ++i)
                {
                    var rootPath = "Assets";

                    var path = AssetDatabase.GetAssetPath((EntityId)instanceIDs[i]);
                    var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                    // Find the right rootPath if folderPath is part of a package
                    if (packageInfo != null)
                        rootPath = packageInfo.assetPath;

                    if (!idsUnderEachRoot.ContainsKey(rootPath))
                        idsUnderEachRoot.Add(rootPath, 0);
                    ++idsUnderEachRoot[rootPath];
                }

                SetAssetsResults(entityIdSet, idsUnderEachRoot);
            }
            else
            {
                var property = new HierarchyIterator(m_HierarchyType, false);
                property.Reset();

                System.Array.Resize(ref m_Results, instanceIDs.Length);
                for (int i = 0; i < instanceIDs.Length; ++i)
                {
                    if (property.Find(instanceIDs[i], null))
                        CopyPropertyData(ref m_Results[i], property);
                }
            }
        }

        internal void SetResults(EntityId[] entityIds, string[] rootPaths)
        {
            var entityIdSet = new HashSet<EntityId>(entityIds);
            if (m_HierarchyType == HierarchyType.Assets)
            {
                var idsUnderEachRoot = new Dictionary<string, int>();
                foreach (var rootPath in rootPaths)
                {
                    if (!idsUnderEachRoot.ContainsKey(rootPath))
                        idsUnderEachRoot.Add(rootPath, 0);
                    ++idsUnderEachRoot[rootPath];
                }
                SetAssetsResults(entityIdSet, idsUnderEachRoot);
            }
            else
            {
                var property = new HierarchyIterator(m_HierarchyType, false);
                property.Reset();

                System.Array.Resize(ref m_Results, entityIds.Length);
                for (int i = 0; i < entityIds.Length; ++i)
                {
                    if (property.Find(entityIds[i], null))
                        CopyPropertyData(ref m_Results[i], property);
                }
            }
        }

        void SetAssetsResults(HashSet<EntityId> entityIdsSet, Dictionary<string, int> idsUnderEachRoot)
        {
            System.Array.Resize(ref m_Results, entityIdsSet.Count);
            var currentResultIndex = 0;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var rootPaths = idsUnderEachRoot.Keys.ToArray();
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var idCounts = idsUnderEachRoot.Values.ToArray();
#pragma warning restore UA2001
            for (var i = 0; i < rootPaths.Length; ++i)
            {
                var rootPath = rootPaths[i];
                var nbIds = idCounts[i];
                var property = new HierarchyIterator(rootPath, false);
                var propertiesFound = 0;
                while (property.Next(null) && propertiesFound < nbIds)
                {
                    var entityId = property.GetEntityIdIfImported();
                    if (entityIdsSet.Contains(entityId))
                    {
                        ++propertiesFound;
                        CopyPropertyData(ref m_Results[currentResultIndex], property);
                        ++currentResultIndex;
                    }
                }
            }
        }

        void CopyPropertyData(ref FilterResult result, HierarchyIterator property)
        {
            if (result == null)
                result = new FilterResult();

            result.entityId = property.GetEntityIdIfImported();
            result.name = property.name;
            result.hasChildren = property.hasChildren;
            result.colorCode = property.colorCode;
            result.isMainRepresentation = property.isMainRepresentation;
            result.hasFullPreviewImage = property.hasFullPreviewImage;
            result.iconDrawStyle = property.iconDrawStyle;
            result.isFolder = property.isFolder;
            result.type = hierarchyType;

            // If this is not the main representation, cache the icon, as we don't have an API to access it later.
            // Otherwise, don't - as this may cause Textures to load unintendedly (e.g if we have 3000 search results we do not want to load icons before needed when rendering)
            if (!property.isMainRepresentation)
                result.icon = property.icon;
            else if (property.isFolder && !property.hasChildren)
                result.icon = EditorGUIUtility.FindTexture(EditorResources.emptyFolderIconName);
            else
                result.icon = null;

            if (m_HierarchyType == HierarchyType.Assets)
            {
                result.assetGUID = property.assetGUID;
            }

        }

        void SearchAllAssets(SearchFilter.SearchArea area)
        {
            if (m_HierarchyType == HierarchyType.Assets)
            {
                List<FilterResult> list = new List<FilterResult>();
                list.AddRange(m_Results);

                var maxAddCount = maxSearchAddCount;
                m_SearchFilter.searchArea = area;
                var enumerator = AssetDatabase.EnumerateAllAssets(m_SearchFilter);
                while (enumerator.MoveNext() && --maxAddCount >= 0)
                {
                    var result = new FilterResult();
                    CopyPropertyData(ref result, enumerator.Current);
                    list.Add(result);
                }

                m_Results = list.ToArray();
            }
            else if (m_HierarchyType == HierarchyType.GameObjects)
            {
                var property = new HierarchyIterator(m_HierarchyType, false);
                m_SearchSessionHandler.BeginSession(() =>
                {
                    return new SearchService.SceneSearchContext
                    {
                        searchFilter = m_SearchFilter,
                        rootIterator = property,
                        requiredTypeNames = m_SearchFilter.classNames,
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        requiredTypes = searchFilter.classNames.Select(name => TypeCache.GetTypesDerivedFrom<Object>().FirstOrDefault(t => name == t.FullName || name == t.Name))
#pragma warning restore UA2001
                    };
                }, m_SearchSessionOptions);

                var searchQuery = m_SearchFilter.originalText;
                m_SearchSessionHandler.BeginSearch(searchQuery);

                if (m_SearchFilter.sceneHandles != null &&
                    m_SearchFilter.sceneHandles.Length > 0)
                {
                    property.SetCustomScenes(m_SearchFilter.sceneHandles);
                }

                var newResults = new List<FilterResult>();
                while (property.Next(null))
                {
                    if (!m_SearchSessionHandler.Filter(searchQuery, property))
                        continue;
                    FilterResult newResult = new FilterResult();
                    CopyPropertyData(ref newResult, property);
                    newResults.Add(newResult);
                }
                int elements = newResults.Count;
                elements = Mathf.Min(elements, maxSearchAddCount);

                int i = m_Results.Length;
                System.Array.Resize(ref m_Results, m_Results.Length + elements);
                for (var j = 0; j < elements && i < m_Results.Length; ++j, ++i)
                {
                    m_Results[i] = newResults[j];
                }

                m_SearchSessionHandler.EndSearch();
            }
        }

        void SearchInFolders()
        {
            List<FilterResult> list = new List<FilterResult>();
            List<string> baseFolders = new List<string>();
            baseFolders.AddRange(ProjectWindowUtil.GetBaseFolders(m_SearchFilter.folders));
            if (baseFolders.Remove(PackageManager.Folders.GetPackagesPath()))
            {
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(m_SearchFilter.skipHidden);
                foreach (var package in packages)
                {
                    if (!baseFolders.Contains(package.assetPath))
                        baseFolders.Add(package.assetPath);
                }
            }

            m_SearchFilter.searchArea = SearchFilter.SearchArea.SelectedFolders;
            foreach (string folderPath in baseFolders)
            {
                // Ensure we do not have a filter when finding folder
                var property = new HierarchyIterator(folderPath);
                property.SetSearchFilter(m_SearchFilter);

                // Set filter after we found the folder
                int folderDepth = property.depth;
                EntityId[] expanded = null; // enter all children of folder
                while (property.NextWithDepthCheck(expanded, folderDepth + 1))
                {
                    FilterResult result = new FilterResult();
                    CopyPropertyData(ref result, property);
                    list.Add(result);
                }
            }
            m_Results = list.ToArray();
        }

        void FolderBrowsing()
        {
            // We are not concerned with assets being added multiple times as we only show the contents
            // of each selected folder. This is an issue when searching recursively into child folders.
            List<FilterResult> list = new List<FilterResult>();
            HierarchyIterator property;
            foreach (string folderPath in m_SearchFilter.folders)
            {
                if (folderPath == PackageManager.Folders.GetPackagesPath())
                {
                    var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(m_SearchFilter.skipHidden);
                    foreach (var package in packages)
                    {
                        var packageFolderInstanceId = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(package.assetPath);
                        property = new HierarchyIterator(package.assetPath);
                        if (property.Find(packageFolderInstanceId, null))
                        {
                            FilterResult result = new FilterResult();
                            CopyPropertyData(ref result, property);
                            result.name = !string.IsNullOrEmpty(package.displayName) ? package.displayName : package.name;
                            list.Add(result);
                        }
                    }
                    continue;
                }

                if (m_SearchFilter.skipHidden && !PackageManagerUtilityInternal.IsPathInVisiblePackage(folderPath))
                    continue;

                EntityId folderEntityId = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(folderPath);
                property = new HierarchyIterator(folderPath);
                property.SetSearchFilter(m_SearchFilter);

                int folderDepth = property.depth;
                EntityId[] expanded = { folderEntityId };
                Dictionary<string , List<FilterResult>> subAssets = new Dictionary<string, List<FilterResult>>();
                List<FilterResult> parentAssets = new List<FilterResult>();
                while (property.Next(expanded))
                {
                    if (property.depth <= folderDepth)
                        break; // current property is outside folder

                    FilterResult result = new FilterResult();
                    CopyPropertyData(ref result, property);
                    //list.Add(result);

                    // Fetch sub assets by expanding the main asset (ignore folders)
                    if (property.hasChildren && !property.isFolder)
                    {
                        parentAssets.Add(result);
                        List<FilterResult>  subAssetList = new List<FilterResult>();
                        subAssets.Add(result.guid, subAssetList);
                        System.Array.Resize(ref expanded, expanded.Length + 1);
                        expanded[expanded.Length - 1] = property.entityId;
                    }
                    else
                    {
                        List<FilterResult> subAssetList;
                        if (subAssets.TryGetValue(result.guid, out subAssetList))
                        {
                            subAssetList.Add(result);
                            subAssets[result.guid] = subAssetList;
                        }
                        else
                        {
                            parentAssets.Add(result);
                        }
                    }
                }
                parentAssets.Sort((result1, result2) => EditorUtility.NaturalCompare(result1.name, result2.name));
                foreach (FilterResult result in parentAssets)
                {
                    list.Add(result);
                    List<FilterResult> subAssetList;
                    if (subAssets.TryGetValue(result.guid, out subAssetList))
                    {
                        subAssetList.Sort((result1, result2) => EditorUtility.NaturalCompare(result1.name, result2.name));
                        foreach (FilterResult subasset in subAssetList)
                        {
                            list.Add(subasset);
                        }
                    }
                }
            }
            m_Results = list.ToArray();
        }

        void AddResults()
        {
            switch (m_SearchFilter.GetState())
            {
                case SearchFilter.State.FolderBrowsing:          FolderBrowsing();  break;
                case SearchFilter.State.SearchingInAllAssets:    SearchAllAssets(SearchFilter.SearchArea.AllAssets); break;
                case SearchFilter.State.SearchingInAssetsOnly:   SearchAllAssets(SearchFilter.SearchArea.InAssetsOnly); break;
                case SearchFilter.State.SearchingInPackagesOnly: SearchAllAssets(SearchFilter.SearchArea.InPackagesOnly); break;
                case SearchFilter.State.SearchingInFolders:      SearchInFolders(); break;
                case SearchFilter.State.EmptySearchFilter: /*do nothing*/ break;
                default: Debug.LogError("Unhandled enum!"); break;
            }
        }

        public void ResultsChanged()
        {
            m_Results = System.Array.Empty<FilterResult>();
            if (m_SearchFilter.GetState() != SearchFilter.State.EmptySearchFilter)
            {
                AddResults();

                // When filtering on folder we use the order we get from BaseHiearchyProperty.cpp (to keep indented children under parent) otherwise we sort
                if (m_SearchFilter.IsSearching())
                {
                    System.Array.Sort(m_Results, (result1, result2) => EditorUtility.NaturalCompare(result1.name, result2.name));
                }

                if (foldersFirst)
                {
                    for (int nonFolderPos = 0; nonFolderPos < m_Results.Length; ++nonFolderPos)
                    {
                        if (m_Results[nonFolderPos].isFolder)
                            continue;

                        for (int folderPos = nonFolderPos + 1; folderPos < m_Results.Length; ++folderPos)
                        {
                            if (!m_Results[folderPos].isFolder)
                                continue;

                            FilterResult folder = m_Results[folderPos];
                            int length = folderPos - nonFolderPos;
                            System.Array.Copy(m_Results, nonFolderPos, m_Results, nonFolderPos + 1, length);
                            m_Results[nonFolderPos] = folder;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Reset visible flags if filter string is empty (see BaseHiearchyProperty::SetSearchFilter)
                if (m_HierarchyType == HierarchyType.GameObjects)
                {
                    var gameObjects = new HierarchyIterator(HierarchyType.GameObjects, false);
                    gameObjects.SetSearchFilter(m_SearchFilter);
                    m_SearchSessionHandler.EndSession();
                }
            }
        }

        public void RefreshVisibleItems(List<EntityId> expandedInstanceIDs)
        {
            bool isSearching = m_SearchFilter.IsSearching();
            List<FilterResult> visibleItems = new List<FilterResult>();
            for (int i = 0; i < m_Results.Length; ++i)
            {
                visibleItems.Add(m_Results[i]);
                if (m_Results[i].isMainRepresentation && m_Results[i].hasChildren && !m_Results[i].isFolder)
                {
                    bool isParentExpanded = expandedInstanceIDs.IndexOf(m_Results[i].entityId) >= 0;
                    bool addSubItems = isParentExpanded || isSearching;
                    int numSubItems = AddSubItemsOfMainRepresentation(i, addSubItems ? visibleItems : null);
                    i += numSubItems;
                }
            }

            m_VisibleItems = visibleItems.ToArray();
        }

        public List<EntityId> GetSubAssetInstanceIDs(EntityId mainAssetEntityId)
        {
            for (int i = 0; i < m_Results.Length; ++i)
            {
                if (m_Results[i].entityId == mainAssetEntityId)
                {
                    List<EntityId> subAssetEntityIds = new List<EntityId>();
                    int index = i + 1; // Start after the main representation
                    while (index < m_Results.Length && !m_Results[index].isMainRepresentation)
                    {
                        subAssetEntityIds.Add(m_Results[index].entityId);
                        index++;
                    }
                    return subAssetEntityIds;
                }
            }
            Debug.LogError("Not main rep " + mainAssetEntityId);
            return new List<EntityId>();
        }

        public int AddSubItemsOfMainRepresentation(int mainRepresentionIndex, List<FilterResult> visibleItems)
        {
            int count = 0;
            int index = mainRepresentionIndex + 1; // Start after the main representation
            while (index < m_Results.Length && !m_Results[index].isMainRepresentation)
            {
                if (visibleItems != null)
                    visibleItems.Add(m_Results[index]);
                index++;
                count++;
            }
            return count;
        }
    }

    internal class FilteredHierarchyIterator : IHierarchyIterator
    {
        FilteredHierarchy m_Hierarchy;
        int m_Position = -1;

        public static IHierarchyIterator CreateHierarchyIteratorForFilter(FilteredHierarchy filteredHierarchy)
        {
            if (filteredHierarchy.searchFilter.GetState() != SearchFilter.State.EmptySearchFilter)
                return new FilteredHierarchyIterator(filteredHierarchy);
            else
                return new HierarchyIterator(filteredHierarchy.hierarchyType, false);
        }

        public FilteredHierarchyIterator(FilteredHierarchy filter)
        {
            m_Hierarchy = filter;
        }

        public void Reset()
        {
            m_Position = -1;
        }

        public EntityId entityId
        {
            get
            {
                var id = m_Hierarchy.results[m_Position].entityId;
                if (id == EntityId.None)
                    m_Hierarchy.results[m_Position].entityId = AssetDatabase.GetMainAssetEntityId(guid);
                return m_Hierarchy.results[m_Position].entityId;
            }
        }

        public Object pptrValue
        {
            get { return EditorUtility.EntityIdToObject(entityId); }
        }

        public string name
        {
            get { return m_Hierarchy.results[m_Position].name; }
        }

        public bool hasChildren
        {
            get { return m_Hierarchy.results[m_Position].hasChildren; }
        }

        public bool isMainRepresentation
        {
            get { return m_Hierarchy.results[m_Position].isMainRepresentation; }
        }

        public bool hasFullPreviewImage
        {
            get { return m_Hierarchy.results[m_Position].hasFullPreviewImage; }
        }

        public IconDrawStyle iconDrawStyle
        {
            get { return m_Hierarchy.results[m_Position].iconDrawStyle; }
        }

        public bool isFolder
        {
            get { return m_Hierarchy.results[m_Position].isFolder; }
        }

        public GUID[] dynamicDependencies
        {
            get { return System.Array.Empty<GUID>(); }
        }

        public int depth
        {
            get { return 0; }
        }

        public int row
        {
            get { return m_Position; }
        }

        public int colorCode
        {
            get { return m_Hierarchy.results[m_Position].colorCode; }
        }

        public bool IsExpanded(EntityId[] expanded)
        {
            return false;
        }

        public string guid
        {
            get { return assetGUID.ToString(); }
        }

        public GUID assetGUID
        {
            get { return m_Hierarchy.results[m_Position].assetGUID; }
        }

        public bool isValid
        {
            get { return m_Hierarchy.results != null && m_Position < m_Hierarchy.results.Length && m_Position >= 0; }
        }

        public Texture2D icon
        {
            get { return m_Hierarchy.results[m_Position].icon; }
        }

        public bool Next(EntityId[] expanded)
        {
            m_Position++;
            return m_Position < m_Hierarchy.results.Length;
        }

        public bool NextWithDepthCheck(EntityId[] expanded, int minDepth)
        {
            // Depth check does not make sense for filtered properties as tree info is lost
            return Next(expanded);
        }

        public bool Previous(EntityId[] expanded)
        {
            m_Position--;
            return m_Position >= 0;
        }

        public bool Parent()
        {
            return false;
        }

        public EntityId[] ancestors
        {
            get
            {
                return System.Array.Empty<EntityId>();
            }
        }

        public bool Find(EntityId _EntityId, EntityId[] expanded)
        {
            Reset();
            while (Next(expanded))
            {
                if (entityId == _EntityId)
                    return true;
            }
            return false;
        }

        public EntityId[] FindAllAncestors(EntityId[] entityIds)
        {
            return System.Array.Empty<EntityId>();
        }

        public bool Skip(int count, EntityId[] expanded)
        {
            m_Position += count;
            return m_Position < m_Hierarchy.results.Length;
        }

        public int CountRemaining(EntityId[] expanded)
        {
            return m_Hierarchy.results.Length - m_Position - 1;
        }

        public EntityId GetEntityIdIfImported()
        {
            return m_Hierarchy.results[m_Position].entityId;
        }
    }
}
