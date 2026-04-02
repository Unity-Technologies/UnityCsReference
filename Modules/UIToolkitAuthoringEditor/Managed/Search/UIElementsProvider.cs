// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

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
        const string k_MenuPath = "Window/UI Toolkit/UI Library";
        const string k_WindowTitle = "UI Library";

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesChanged;
            EditorApplication.delayCall += () => OnUIStagesChanged(UIToolkitAuthoringSettings.EnableUIStages);
        }

        static void OnUIStagesChanged(bool enabled)
        {
            if (enabled)
            {
                Menu.AddMenuItem(k_MenuPath, "", false, k_MenuPriority, OpenUIElementsPicker, null);
                return;
            }

            Menu.RemoveMenuItem(k_MenuPath);
        }

        internal static void OpenUIElementsPicker()
        {
            var providers = new List<SearchProvider>
            {
                SearchService.GetProvider(k_EngineProviderId),
                SearchService.GetProvider(k_CustomProviderId)
            };

            var searchContext = SearchService.CreateContext(providers, string.Empty);
            searchContext.useExplicitProvidersAsNormalProviders = true;

            var state = new SearchViewState(searchContext)
            {
                excludeClearItem = true,
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

        static SearchProvider CreateProvider(ProviderConfig config)
        {
            return new SearchProvider(config.Id, config.Name, config.CreateFetchFunction())
            {
                fetchLabel = FetchElementLabel,
                fetchThumbnail = FetchElementThumbnail,
                startDrag = StartElementDrag,
                toObject = ToObject,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Preview,
                actions = [CreateAddElementAction(config.Id)],
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
            }
            return item.thumbnail;
        }

        static void StartElementDrag(SearchItem item, SearchContext context)
        {
            if (item.data is LibraryItem libItem)
            {
                // Store the library item for drag-and-drop
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("LibraryItem", libItem);
                DragAndDrop.StartDrag(libItem.name);
            }
        }

        static UnityEngine.Object ToObject(SearchItem item, Type type)
        {
            if (item.data is LibraryItem libItem && libItem.libraryType.type != null)
            {
                // Create a VisualTreeAsset containing just this element
                var vta = CreateVisualTreeAssetFromElement(libItem);
                if (vta != null)
                {
                    return vta;
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

                s_SortedTypes = new List<LibraryTypeKey>(libraryTypes.Keys);
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

            if (StageUtility.GetCurrentStage() is not VisualElementEditingStage stage)
                return;

            var selectedElement = (Selection.activeObject as VisualElementSelection)?.Element;
            var parentVea = selectedElement?.visualElementAsset ?? stage.EditedVisualTreeAsset.visualTree;

            if (selectedElement != null)
            {
                var editFlags = stage.Context.GetElementEditFlags(selectedElement);
                if (editFlags != VisualElementEditFlags.FullyEditable)
                {
                    parentVea = stage.EditedVisualTreeAsset.visualTree;
                }
            }

            var elementType = libItem.libraryType.type;
            var command = new AddElementCommand(elementType, stage.EditedVisualTreeAsset, parentVea);
            command.Execute();

            stage.RequestRefresh();
        }
    }
}
