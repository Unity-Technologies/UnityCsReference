// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Search provider for UI Toolkit elements - provides a picker window for browsing and searching UI controls.
    /// </summary>
    internal static partial class UIElementsProvider
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

        [NoAutoStaticsCleanup] // immutable provider configs, safe to persist
        static readonly ProviderConfig[] s_ProviderConfigs =
        {
            new(k_EngineProviderId, "Engine", typeKey => typeKey.id.StartsWith("UnityEngine")),
            new(k_CustomProviderId, "Custom", typeKey => !typeKey.id.StartsWith("UnityEngine") && !typeKey.id.StartsWith("UnityEditor")),
        };

        // Cache for sorted and filtered library types per category
        [AutoStaticsCleanupOnCodeReload] // readonly: cleanup calls Clear()
        static readonly Dictionary<string, List<LibraryTypeKey>> s_CachedTypesByCategory = new();
        [AutoStaticsCleanupOnCodeReload]
        static List<LibraryTypeKey> s_SortedTypes;
        [AutoStaticsCleanupOnCodeReload]
        static int s_CachedTypesHash;
        const string k_CustomProviderId = "uicustom";
        const string k_EngineProviderId = "uiengine";
        const string k_UxmlProviderId = "uiuxml";
        const string k_MenuPath = "Window/UI Toolkit/UI Library";
        const string k_WindowTitle = "UI Library";
        const string k_EngineNamespaceRoot = "UnityEngine";

        [AutoStaticsCleanupOnCodeReload]
        static Texture2D s_FolderIcon;
        static Texture2D FolderIcon => s_FolderIcon != null ? s_FolderIcon : s_FolderIcon = EditorGUIUtility.FindTexture("Folder Icon");
        [NoAutoStaticsCleanup] // dead views are pruned on refresh, safe to persist
        static readonly List<ISearchView> s_OpenLibraryViews = new();
        [AutoStaticsCleanupOnCodeReload] // the scheduled delayCall dies with the domain
        static bool s_RefreshScheduled;
        const string k_VisibilityButtonClassName = "search-groupbar__visibility-button";
        const string k_NativeVisibilityButtonName = "SearchVisibilityOptions";
        const string k_ControlsVisibilityButtonName = "UILibraryControlsVisibility";

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

        static void RegisterLibraryView(ISearchView view)
        {
            if (view == null || s_OpenLibraryViews.Contains(view))
                return;

            if (s_OpenLibraryViews.Count == 0)
                EditorApplication.projectChanged += OnProjectChanged;

            s_OpenLibraryViews.Add(view);
            InstallControlsVisibilityDropdown(view);
        }

        // The native Search visibility dropdown is hardcoded and offers no hook for custom options, so we hide it
        // and inject our own dropdown with the library's controls-visibility toggles in the same groupbar spot.
        static void InstallControlsVisibilityDropdown(ISearchView view)
        {
            if (view is not EditorWindow window)
                return;

            window.rootVisualElement.schedule.Execute(() =>
            {
                var nativeButton = window.rootVisualElement.Q(name: k_NativeVisibilityButtonName);
                if (nativeButton == null)
                    return;

                nativeButton.style.display = DisplayStyle.None;

                var parent = nativeButton.parent;
                if (parent == null || parent.Q(name: k_ControlsVisibilityButtonName) != null)
                    return;

                var button = new Button(() => ShowControlsVisibilityMenu(view))
                {
                    name = k_ControlsVisibilityButtonName,
                    tooltip = "Filter the controls shown in the library"
                };
                button.AddToClassList("search-groupbar__button");
                button.AddToClassList(k_VisibilityButtonClassName);
                parent.Insert(parent.IndexOf(nativeButton), button);
            });
        }

        static void ShowControlsVisibilityMenu(ISearchView view)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Show Internal Controls"), LibraryContent.ShowInternalControls, () =>
            {
                LibraryContent.ShowInternalControls = !LibraryContent.ShowInternalControls;
                InvalidateAndRefresh();
            });
            menu.AddItem(new GUIContent("Show Package Controls"), LibraryContent.ShowPackageControls, () =>
            {
                LibraryContent.ShowPackageControls = !LibraryContent.ShowPackageControls;
                InvalidateAndRefresh();
            });
            menu.ShowAsContext();
        }

        static void InvalidateAndRefresh()
        {
            s_CachedTypesByCategory.Clear();
            s_SortedTypes = null;
            s_CachedTypesHash = 0;

            for (var i = s_OpenLibraryViews.Count - 1; i >= 0; i--)
            {
                if (s_OpenLibraryViews[i] is EditorWindow window && window)
                    s_OpenLibraryViews[i].Refresh(RefreshFlags.StructureChanged);
                else
                    s_OpenLibraryViews.RemoveAt(i);
            }
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
            var provider = BuildProvider(k_UxmlProviderId, "UXMLs", FetchProjectUxmlItems);
            provider.showDetailsOptions |= ShowDetailsOptions.DefaultGroup;
            provider.priority = 200;
            return provider;
        }

        static SearchProvider CreateProvider(ProviderConfig config)
        {
            return BuildProvider(config.Id, config.Name, config.CreateFetchFunction());
        }

        static SearchProvider BuildProvider(string id, string name, Func<SearchContext, SearchProvider, IEnumerable<SearchItem>> fetch)
        {
            return new SearchProvider(id, name, (context, provider) =>
            {
                RegisterLibraryView(context.searchView);
                return fetch(context, provider);
            })
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
            long score = 0;
            foreach (var libItem in EnumerateProjectUxmlItems())
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
        /// Lazily enumerates the project's UXML documents under <c>Assets/</c> as <see cref="LibraryItem"/>s.
        /// </summary>
        static IEnumerable<LibraryItem> EnumerateProjectUxmlItems()
        {
            var searchFilter = new SearchFilter
            {
                classNames = [nameof(VisualTreeAsset)],
                searchArea = SearchFilter.SearchArea.InAssetsOnly
            };

            var guids = AssetDatabase.FindAssets(searchFilter);
            var paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            Array.Sort(paths, StringComparer.Ordinal);

            foreach (var assetPath in paths)
            {
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                var name = Path.GetFileName(assetPath);
                var folder = Path.GetDirectoryName(assetPath);
                folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.Replace('\\', '/');

                yield return new LibraryItem(name, assetPath, folder);
            }
        }

        static void OnProjectChanged()
        {
            if (s_RefreshScheduled)
                return;

            s_RefreshScheduled = true;
            EditorApplication.delayCall += RefreshOpenLibraryViewsDebounced;
        }

        static void RefreshOpenLibraryViewsDebounced()
        {
            s_RefreshScheduled = false;
            for (var i = s_OpenLibraryViews.Count - 1; i >= 0; i--)
            {
                if (s_OpenLibraryViews[i] is EditorWindow window && window)
                    s_OpenLibraryViews[i].Refresh(RefreshFlags.StructureChanged);
                else
                    s_OpenLibraryViews.RemoveAt(i);
            }

            if (s_OpenLibraryViews.Count == 0)
                EditorApplication.projectChanged -= OnProjectChanged;
        }

        /// <summary>
        /// Gets cached filtered and sorted types for a category. Cache is invalidated when library content changes.
        /// </summary>
        static List<LibraryTypeKey> GetCachedFilteredTypes(string categoryId, Func<LibraryTypeKey, bool> filter)
        {
            var libraryTypes = LibraryContent.GetAllLibraryTypes();
            var currentHash = HashCode.Combine(libraryTypes.GetHashCode(), LibraryContent.ShowInternalControls, LibraryContent.ShowPackageControls);

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
                s_SortedTypes.Sort(static (a, b) =>
                {
                    var byCuratedOrder = LibraryOrdering.GetOrder(b.type).CompareTo(LibraryOrdering.GetOrder(a.type));
                    return byCuratedOrder != 0 ? byCuratedOrder : string.Compare(b.name, a.name, StringComparison.Ordinal);
                });
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

            // We wanted to fold "UnityEngine" out when in the Engine tab but be present when in the All tab.
            if (ShouldFoldEngineNamespaceRoot(context.searchView?.currentGroup, idsSubstrings))
                idsSubstrings.RemoveAt(0);
        }

        internal static bool ShouldFoldEngineNamespaceRoot(string currentGroup, List<StringView> idsSubstrings)
        {
            return idsSubstrings.Count > 1
                && currentGroup == k_EngineProviderId
                && idsSubstrings[0].Equals(k_EngineNamespaceRoot, StringComparison.Ordinal);
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
