// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// <remarks>
    /// The UI Library is either the UI Library window or a view embedded in a window that claims a local
    /// <see cref="AuthoringContext"/>, such as the UI Builder's <see cref="LibraryView"/>. Adding an item targets the
    /// context that owns the view: the window's document for an embedded view, the UI Stage or the scene otherwise.
    /// Dragging an item works the same from any of them, as the drop target decides where the item goes.
    /// </remarks>
    internal static partial class UIElementsProvider
    {
        class ProviderConfig
        {
            public string Id;
            public string Name;
            public Func<LibraryTypeKey, bool, bool> Filter { get; }

            public ProviderConfig(string id, string name, Func<LibraryTypeKey, bool, bool> filter)
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

        // The editor controls are engine controls too, for a document that is an editor extension.
        [NoAutoStaticsCleanup] // immutable provider configs, safe to persist
        static readonly ProviderConfig[] s_ProviderConfigs =
        {
            new(k_EngineProviderId, "Engine", (typeKey, includeEditorOnly) =>
                typeKey.id.StartsWith("UnityEngine") || (includeEditorOnly && typeKey.id.StartsWith("UnityEditor"))),
            new(k_CustomProviderId, "Custom", (typeKey, _) => !typeKey.id.StartsWith("UnityEngine") && !typeKey.id.StartsWith("UnityEditor")),
        };

        // Cache for sorted and filtered library types per category
        [AutoStaticsCleanupOnCodeReload] // readonly: cleanup calls Clear()
        static readonly Dictionary<string, List<LibraryTypeKey>> s_CachedTypesByCategory = new();
        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Cache recomputed on demand when the hash no longer matches")]
        static List<LibraryTypeKey> s_SortedTypes;
        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Cache key; reset on code reload so the cache is recomputed")]
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
        [NoAutoStaticsCleanup] // entries are released with their search context
        static readonly ConditionalWeakTable<SearchContext, AuthoringContext> s_PickerTargetContexts = new();
        [AutoStaticsCleanupOnCodeReload] // the scheduled delayCall dies with the domain
        static bool s_RefreshScheduled;
        const string k_VisibilityButtonClassName = "search-groupbar__visibility-button";
        const string k_NativeVisibilityButtonName = "SearchVisibilityOptions";
        const string k_ControlsVisibilityButtonName = "UILibraryControlsVisibility";

        [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 5)]
        internal static void OpenUIElementsPicker()
        {
            var state = new SearchViewState(CreateLibraryContext())
            {
                excludeClearItem = true,
                group = k_EngineProviderId,
                windowTitle = new GUIContent(k_WindowTitle),
                flags = SearchViewFlags.DisableSavedSearchQuery | SearchViewFlags.DisableBuilderModeToggle | SearchViewFlags.OpenInBuilderMode,
                resultViewDescriptorList = new SearchResultViewDescriptorList([SearchTreeView.GetDescriptor()])
            };

            SearchService.ShowWindow(state);
        }

        /// <summary>
        /// The state of a UI Library view embedded in another window: the content of the UI Library window, with
        /// a plain search field and no side panels, to fit a pane.
        /// </summary>
        internal static SearchViewState CreateEmbeddedLibraryViewState()
        {
            var flags = SearchViewFlags.DisableSavedSearchQuery | SearchViewFlags.DisableBuilderModeToggle |
                SearchViewFlags.DisableInspectorPreview | SearchViewFlags.OpenInTextMode;
            return new SearchViewState(CreateLibraryContext(), flags)
            {
                excludeClearItem = true,
                group = k_EngineProviderId,
                resultViewDescriptorList = new SearchResultViewDescriptorList([SearchTreeView.GetDescriptor()])
            };
        }

        static SearchContext CreateLibraryContext()
        {
            var providers = new List<SearchProvider>
            {
                SearchService.GetProvider(k_EngineProviderId),
                SearchService.GetProvider(k_CustomProviderId),
                SearchService.GetProvider(k_UxmlProviderId)
            };

            var searchContext = SearchService.CreateContext(providers, string.Empty);
            searchContext.useExplicitProvidersAsNormalProviders = true;
            return searchContext;
        }

        /// <summary>
        /// The context the items of <paramref name="view"/> are added to: the one that owns the view when it is
        /// embedded in a window, <see cref="AuthoringContext.Global"/> for the UI Library window.
        /// </summary>
        internal static AuthoringContext GetTargetContext(ISearchView view)
        {
            return view is VisualElement element ? AuthoringContext.For(element) : AuthoringContext.Global;
        }

        /// <summary>
        /// Whether <paramref name="item"/> can be added to the document of the local <paramref name="context"/>.
        /// A document cannot instantiate itself, and a document that is only previewed takes nothing.
        /// </summary>
        internal static bool CanAddToLocalDocument(AuthoringContext context, LibraryItem item)
        {
            return item != null && context is { IsDisposed: false } &&
                context.Session is { IsDisposed: false, IsDocumentReadOnly: false } session &&
                session.EditedVisualTreeAsset is { } document &&
                (!item.isAsset || item.visualTreeAsset != document);
        }

        /// <summary>
        /// The context the items of the search <paramref name="context"/> are added to: the one a picker was opened
        /// for, or the one that owns the view of the search otherwise.
        /// </summary>
        internal static AuthoringContext GetTargetContext(SearchContext context)
        {
            if (context != null && s_PickerTargetContexts.TryGetValue(context, out var pickerTargetContext))
                return pickerTargetContext;

            return GetTargetContext(context?.searchView);
        }

        static bool IncludesEditorOnlyControls(SearchContext context)
        {
            var targetContext = GetTargetContext(context);
            return !targetContext.IsDisposed && EditorExtensionAuthoring.IsEnabled(targetContext.EditedVisualTreeAsset);
        }

        static void RegisterLibraryView(ISearchView view)
        {
            if (view == null || s_OpenLibraryViews.Contains(view))
                return;

            if (s_OpenLibraryViews.Count == 0)
                EditorApplication.projectChanged += OnProjectChanged;

            s_OpenLibraryViews.Add(view);

            // An embedded view installs the dropdown in its own group bar.
            if (view is EditorWindow window)
                InstallControlsVisibilityDropdown(window.rootVisualElement, view);
        }

        static bool IsAlive(ISearchView view)
        {
            return view switch
            {
                EditorWindow window => window,
                VisualElement element => element.panel != null,
                _ => false,
            };
        }

        // The native Search visibility dropdown is hardcoded and offers no hook for custom options, so we hide it
        // and inject our own dropdown with the library's controls-visibility toggles in the same groupbar spot.
        internal static void InstallControlsVisibilityDropdown(VisualElement root, ISearchView view)
        {
            root.schedule.Execute(() =>
            {
                var nativeButton = root.Q(name: k_NativeVisibilityButtonName);
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

        internal static void InvalidateAndRefresh()
        {
            s_CachedTypesByCategory.Clear();
            s_SortedTypes = null;
            s_CachedTypesHash = 0;

            for (var i = s_OpenLibraryViews.Count - 1; i >= 0; i--)
            {
                if (IsAlive(s_OpenLibraryViews[i]))
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
#pragma warning disable UAL0018 // FetchElementThumbnail reaches the FolderIcon lazy cache, which re-loads itself on next access after reload
                fetchThumbnail = FetchElementThumbnail,
#pragma warning restore UAL0018
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
            if (item.data is LibraryItem libItem)
                libItem.StartDrag();
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
            try
            {
                return CreatePreviewVisualTreeAsset(libItem);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create VisualTreeAsset for {libItem.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an unsaved VisualTreeAsset that holds the control of <paramref name="libItem"/> alone, configured
        /// the way adding it from the library configures it, or null when the item is not a control. Throws what the
        /// configuration throws; the caller destroys the asset.
        /// </summary>
        internal static VisualTreeAsset CreatePreviewVisualTreeAsset(LibraryItem libItem)
        {
            var elementType = libItem.libraryType.type;
            if (elementType == null || !typeof(VisualElement).IsAssignableFrom(elementType))
                return null;

            // Create VTA using ScriptableObject
            var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            vta.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            vta.name = $"Preview_{libItem.name}";

            try
            {
                // Add the element to the VTA using internal API
                var fullTypeName = elementType.FullName;
                var vea = vta.AddElementOfType(null, fullTypeName);

                var description = UxmlSerializedDataRegistry.GetDescription(fullTypeName);
                if (description != null)
                {
                    vea.serializedData = description.CreateDefaultSerializedData();
                }

                ElementConfiguratorRegistry.Configure(elementType, libItem.libraryType.variantName, vta, vea);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(vta);
                throw;
            }

            return vta;
        }

        /// <summary>
        /// Common method to fetch controls filtered by a predicate.
        /// </summary>
        static IEnumerable<SearchItem> FetchControlsByFilter(SearchContext context, SearchProvider provider, string idPrefix, Func<LibraryTypeKey, bool, bool> filter)
        {
            long score = 0;
            var filteredTypes = GetCachedFilteredTypes(idPrefix, IncludesEditorOnlyControls(context), filter);

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
            foreach (var libItem in LibraryContent.EnumerateProjectTemplates())
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
                if (IsAlive(s_OpenLibraryViews[i]))
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
        static List<LibraryTypeKey> GetCachedFilteredTypes(string providerId, bool includeEditorOnly, Func<LibraryTypeKey, bool, bool> filter)
        {
            var categoryId = includeEditorOnly ? providerId + "/editor" : providerId;

            var libraryTypes = LibraryContent.GetAllLibraryTypes();
            var currentHash = HashCode.Combine(libraryTypes.GetHashCode(), LibraryContent.ShowInternalControls, LibraryContent.ShowPackageControls);

            // Invalidate all caches if library content changed
            if (s_CachedTypesHash != currentHash)
            {
                s_CachedTypesByCategory.Clear();
                s_CachedTypesHash = currentHash;

                s_SortedTypes = new List<LibraryTypeKey>(libraryTypes.Count);
                // The curated editor controls count as internal, but an editor extension lists them regardless.
                foreach (var typeKey in libraryTypes.Keys)
                {
                    if (LibraryContent.IsVisibleInLibrary(typeKey.type) || LibraryOrdering.GetCategory(typeKey.type) is { IsEditorOnly: true })
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
                if (filter(typeKey, includeEditorOnly))
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

            var context = GetTargetContext(item.context);
            if (!context.IsGlobal)
            {
                if (CanAddToLocalDocument(context, libItem))
                    MenuUtility.AddFromLibrary(context, libItem);
                return;
            }

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

            var context = GetTargetContext(item.context);
            if (!context.IsGlobal)
            {
                if (CanAddToLocalDocument(context, libItem))
                    MenuUtility.AddAsLastChild(context, libItem);
                return;
            }

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
