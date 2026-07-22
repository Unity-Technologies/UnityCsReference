// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Search provider for UI Toolkit elements - provides a picker window for browsing and searching UI controls.
    /// </summary>
    internal static class UIElementsProvider
    {
        class ProviderConfig
        {
            public string Id;
            public string Name;
            public Func<LibraryTypeKey, bool> Filter { get; }

            public ProviderConfig(string id, string name, Func<LibraryTypeKey, bool> filter)
            {
                Id = id;
                Name = name;
                Filter = filter;
            }

            // Create the fetch function for this provider
            public Func<SearchContext, SearchProvider, IEnumerable<SearchItem>> CreateFetchFunction()
            {
                return (context, provider) => FetchControlsByFilter(context, provider, Id, Filter);
            }
        }

        static readonly ProviderConfig[] s_ProviderConfigs =
        {
            new(k_EngineProviderId, "Engine", typeKey => typeKey.id.StartsWith("UnityEngine")),
            new(k_CustomProviderId, "Custom", typeKey => !typeKey.id.StartsWith("UnityEngine") && !typeKey.id.StartsWith("UnityEditor")),
        };

        // Cache for sorted and filtered library types per category
        static readonly Dictionary<string, List<LibraryTypeKey>> s_CachedTypesByCategory = new();
        static List<LibraryTypeKey> s_SortedTypes;
        static int s_CachedTypesHash;
        const int k_MenuPriority = 3030;
        const string k_CustomProviderId = "uicustom";
        const string k_EngineProviderId = "uiengine";
        const string k_UxmlProviderId = "uiuxml";
        const string k_MenuPath = "Window/UI Toolkit/UI Library";
        const string k_WindowTitle = "UI Library";

        static Texture2D s_FolderIcon;
        static Texture2D FolderIcon => s_FolderIcon != null ? s_FolderIcon : s_FolderIcon = EditorGUIUtility.FindTexture("Folder Icon");

        [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 5)]
        internal static void OpenUIElementsPicker()
        {
            var providers = new List<SearchProvider>
            {
                SearchService.GetProvider(k_EngineProviderId),
                SearchService.GetProvider(k_CustomProviderId),
                SearchService.GetProvider(k_UxmlProviderId)
            };

            var searchContext = SearchService.CreateContext(providers, string.Empty);
            searchContext.useExplicitProvidersAsNormalProviders = true;

            var state = new SearchViewState(searchContext)
            {
                excludeClearItem = true,
                group = k_EngineProviderId,
                windowTitle = new GUIContent(k_WindowTitle),
                flags = SearchViewFlags.DisableSavedSearchQuery | SearchViewFlags.DisableBuilderModeToggle | SearchViewFlags.OpenInBuilderMode,
                resultViewDescriptorList = new SearchResultViewDescriptorList([SearchTreeView.GetDescriptor()])
            };

            SearchService.ShowWindow(state);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateEngineControlsProvider()
        {
            return CreateProvider(s_ProviderConfigs[0]);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateCustomControlsProvider()
        {
            return CreateProvider(s_ProviderConfigs[1]);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProjectUxmlProvider()
        {
            return BuildProvider(k_UxmlProviderId, "UXMLs", FetchProjectUxmlItems);
        }

        static SearchProvider CreateProvider(ProviderConfig config)
        {
            return BuildProvider(config.Id, config.Name, config.CreateFetchFunction());
        }

        static SearchProvider BuildProvider(string id, string name, Func<SearchContext, SearchProvider, IEnumerable<SearchItem>> fetch)
        {
            return new SearchProvider(id, name, fetch)
            {
                fetchLabel = FetchElementLabel,
                fetchThumbnail = FetchElementThumbnail,
                startDrag = StartElementDrag,
                toObject = ToObject,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Preview,
                actions = [CreateAddElementAction(id), CreateAddChildElementAction(id)],
                isExplicitProvider = true,
                fetchParentDescriptor = FetchParentDescriptor,
                fetchParentsTokenSeparatedIds = FetchParentsTokenSeparatedIds
            };
        }

        static string FetchElementLabel(SearchItem item, SearchContext context)
        {
            if (item.data is LibraryItem libItem)
                return libItem.name;
            return item.label;
        }

        static Texture2D FetchElementThumbnail(SearchItem item, SearchContext context)
        {
            if (item.data is LibraryItem libItem)
            {
                if (libItem.largeIcon.texture != null)
                    return libItem.largeIcon.texture;
                if (libItem.icon.texture != null)
                    return libItem.icon.texture;
                return item.thumbnail;
            }

            return FolderIcon;
        }

        static void StartElementDrag(SearchItem item, SearchContext context)
        {
            if (item.data is not LibraryItem libItem)
                return;

            DragAndDrop.PrepareStartDrag();

            if (libItem.isAsset)
            {
                DragAndDrop.objectReferences = [libItem.visualTreeAsset];
                DragAndDrop.paths = [libItem.assetPath];
            }
            else
            {
                DragAndDrop.SetGenericData(LibraryItem.DragDataKey, libItem);
            }

            DragAndDrop.StartDrag(libItem.name);
        }

        static UnityEngine.Object ToObject(SearchItem item, Type type)
        {
            if (item.data is LibraryItem libItem)
            {
                if (libItem.isAsset)
                    return libItem.visualTreeAsset;

                if (libItem.libraryType.type != null)
                {
                    // Create a VisualTreeAsset containing just this element
                    var vta = CreateVisualTreeAssetFromElement(libItem);
                    if (vta != null)
                    {
                        return vta;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a VisualTreeAsset containing a single element of the specified type for preview.
        /// </summary>
        static VisualTreeAsset CreateVisualTreeAssetFromElement(LibraryItem libItem)
        {
            var elementType = libItem.libraryType.type;
            if (elementType == null || !typeof(VisualElement).IsAssignableFrom(elementType))
                return null;

            try
            {
                // Create VTA using ScriptableObject
                var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
                vta.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                vta.name = $"Preview_{libItem.name}";

                // Add the element to the VTA using internal API
                var fullTypeName = elementType.FullName;
                var vea = vta.AddElementOfType(null, fullTypeName);

                var description = UxmlSerializedDataRegistry.GetDescription(fullTypeName);
                if (description != null)
                {
                    vea.serializedData = description.CreateDefaultSerializedData();
                }

                return vta;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create VisualTreeAsset for {libItem.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Common method to fetch controls filtered by a predicate.
        /// </summary>
        static IEnumerable<SearchItem> FetchControlsByFilter(SearchContext context, SearchProvider provider, string idPrefix, Func<LibraryTypeKey, bool> filter)
        {
            long score = 0;
            var filteredTypes = GetCachedFilteredTypes(idPrefix, filter);

            foreach (var typeKey in filteredTypes)
            {
                if (!string.IsNullOrEmpty(context.searchQuery))
                {
                    var searchText = $"{typeKey.name} {typeKey.type?.Name}";
                    if (searchText.IndexOf(context.searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                var item = LibraryContent.GetLibraryItemByLibraryKey(typeKey);
                if (item == null)
                    continue;

                var searchItem = provider.CreateItem(
                    context,
                    id: $"{idPrefix}/{typeKey.name}/{typeKey.type?.FullName}",
                    score: ~(int)score,
                    label: typeKey.name,
                    description: null, // TODO: Check types [tooltip] or [description] attribute
                    thumbnail: item.icon.texture,
                    data: item
                );
                yield return searchItem;
                score++;
            }
        }

        /// <summary>
        /// Fetches the user's project UXML documents as search items for the UXML provider.
        /// </summary>
        static IEnumerable<SearchItem> FetchProjectUxmlItems(SearchContext context, SearchProvider provider)
        {
            var includePackages = (context.options & SearchFlags.Packages) != 0;
            long score = 0;
            foreach (var libItem in EnumerateProjectUxmlItems(includePackages))
            {
                if (!string.IsNullOrEmpty(context.searchQuery)
                    && libItem.name.IndexOf(context.searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    yield return null;
                    continue;
                }

                var searchItem = provider.CreateItem(
                    context,
                    id: $"{k_UxmlProviderId}/{libItem.assetPath}",
                    score: ~(int)score,
                    label: libItem.name,
                    description: null,
                    thumbnail: libItem.icon.texture,
                    data: libItem
                );
                yield return searchItem;
                score++;
            }
        }

        /// <summary>
        /// Lazily enumerates the project's UXML documents as <see cref="LibraryItem"/>s, excluding read-only assets.
        /// Package documents are included only when the search's "Show Package Files" option is enabled.
        /// </summary>
        static IEnumerable<LibraryItem> EnumerateProjectUxmlItems(bool includePackages)
        {
            var searchFilter = new SearchFilter
            {
                classNames = [nameof(VisualTreeAsset)],
                searchArea = includePackages ? SearchFilter.SearchArea.AllAssets : SearchFilter.SearchArea.InAssetsOnly
            };

            var guids = AssetDatabase.FindAssets(searchFilter);
            var paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            Array.Sort(paths, StringComparer.Ordinal);

            foreach (var assetPath in paths)
            {
                if (string.IsNullOrEmpty(assetPath) || !IsWritable(assetPath))
                    continue;

                var name = Path.GetFileName(assetPath);
                var folder = Path.GetDirectoryName(assetPath);
                folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.Replace('\\', '/');

                yield return new LibraryItem(name, assetPath, folder);
            }
        }

        static bool IsWritable(string assetPath)
        {
            var packageInfo = PackageInfo.FindForAssetPath(assetPath);
            return packageInfo == null || packageInfo.source == PackageSource.Embedded || packageInfo.source == PackageSource.Local;
        }

        /// <summary>
        /// Gets cached filtered and sorted types for a category. Cache is invalidated when library content changes.
        /// </summary>
        static List<LibraryTypeKey> GetCachedFilteredTypes(string categoryId, Func<LibraryTypeKey, bool> filter)
        {
            var libraryTypes = LibraryContent.GetAllLibraryTypes();
            var currentHash = libraryTypes.GetHashCode();

            // Invalidate all caches if library content changed
            if (s_CachedTypesHash != currentHash)
            {
                s_CachedTypesByCategory.Clear();
                s_CachedTypesHash = currentHash;

                s_SortedTypes = new List<LibraryTypeKey>(libraryTypes.Count);
                foreach (var typeKey in libraryTypes.Keys)
                {
                    if (LibraryContent.IsVisibleInLibrary(typeKey.type))
                        s_SortedTypes.Add(typeKey);
                }
                s_SortedTypes.Sort((a, b) => string.Compare(b.name, a.name, StringComparison.Ordinal));
            }

            // Return cached result
            if (s_CachedTypesByCategory.TryGetValue(categoryId, out var cached))
                return cached;

            var filtered = new List<LibraryTypeKey>();
            foreach (var typeKey in s_SortedTypes)
            {
                if (filter(typeKey))
                    filtered.Add(typeKey);
            }

            s_CachedTypesByCategory[categoryId] = filtered;
            return filtered;
        }

        static string GetParentNamespace(Type type)
        {
            var fullName = type?.Namespace;
            return string.IsNullOrEmpty(fullName) ? null : fullName;
        }

        static SearchItemParentDescriptor FetchParentDescriptor(SearchItem searchItem, SearchContext context)
        {
            if (searchItem.data is not LibraryItem libItem)
                return default;

            // Use libraryPath if available, otherwise fall back to namespace
            var parentId = !string.IsNullOrEmpty(libItem.libraryPath) ? libItem.libraryPath : libItem.libraryType.type?.Namespace;

            return new SearchItemParentDescriptor(parentId, SearchItemParentType.TokenSeparatedId);
        }

        static void FetchParentsTokenSeparatedIds(SearchItem searchItem, SearchContext context, List<StringView> idsSubstrings)
        {
            var descriptor = searchItem.GetParentDescriptor(context);
            if (string.IsNullOrEmpty(descriptor.Id))
                return;

            var separator = descriptor.Id.Contains('/') ? '/' : '.';
            descriptor.Id.GetStringView().Split(stackalloc char[1] { separator }, StringSplitOptions.RemoveEmptyEntries, idsSubstrings);
        }

        static SearchAction CreateAddElementAction(string providerId)
        {
            var action = new SearchAction(
                providerId,
                "add-to-visual-tree-asset" ,
                new GUIContent("Add Element"),
                AddElementToVisualTreeAsset
            );

            // Keep the window open after adding an element
            action.closeWindowAfterExecution = false;
            return action;
        }

        static void AddElementToVisualTreeAsset(SearchItem item)
        {
            if (item.data is not LibraryItem libItem)
                return;

            if (libItem.isAsset)
            {
                var template = libItem.visualTreeAsset;
                if (template != null)
                    MenuUtility.AddTemplateAsSibling(template);
                return;
            }

            var elementType = libItem.libraryType.type;
            if (elementType == null)
                return;

            MenuUtility.AddElementAsSibling(elementType, libItem.libraryType.variantName);
        }

        static SearchAction CreateAddChildElementAction(string providerId)
        {
            var action = new SearchAction(
                providerId,
                "add-child-to-visual-tree-asset" ,
                new GUIContent("Add Child Element"),
                AddChildElementToVisualTreeAsset
            );

            // Keep the window open after adding an element
            action.closeWindowAfterExecution = false;
            return action;
        }

        static void AddChildElementToVisualTreeAsset(SearchItem item)
        {
            if (item.data is not LibraryItem libItem)
                return;

            if (libItem.isAsset)
            {
                var template = libItem.visualTreeAsset;
                if (template != null)
                    MenuUtility.AddTemplateAsLastChild(template);
                return;
            }

            var elementType = libItem.libraryType.type;
            if (elementType == null)
                return;

            MenuUtility.AddElementAsLastChild(elementType, libItem.libraryType.variantName);
        }
    }
}
