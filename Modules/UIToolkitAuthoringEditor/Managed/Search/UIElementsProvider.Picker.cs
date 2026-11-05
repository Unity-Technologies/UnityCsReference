// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Search;

namespace Unity.UIToolkit.Editor
{
    internal static partial class UIElementsProvider
    {
        /// <summary>
        /// Opens a search picker over the UI Library and reports the picked entry.
        /// </summary>
        /// <param name="includeDocuments">Whether UXML documents can be picked, to be added as template instances.</param>
        internal static void ShowLibraryPicker(string title, bool includeDocuments, Action<LibraryItem> onPicked)
        {
            var providers = new List<SearchProvider>
            {
                SearchService.GetProvider(k_EngineProviderId),
                SearchService.GetProvider(k_CustomProviderId)
            };
            if (includeDocuments)
                providers.Add(SearchService.GetProvider(k_UxmlProviderId));

            var context = SearchService.CreateContext(providers, string.Empty);
            context.useExplicitProvidersAsNormalProviders = true;

            var origin = EditorWindow.focusedWindow;
            ISearchView picker = null;
            var state = new SearchViewState(context)
            {
                title = title,
                excludeClearItem = true,
                selectHandler = (item, cancelled) =>
                {
                    if (cancelled || item?.data is not LibraryItem libraryItem)
                        return;

                    onPicked(libraryItem);
                    EditorApplication.delayCall += () => ClosePicker(picker, origin);
                },
                flags = SearchViewFlags.DisableSavedSearchQuery |
                        SearchViewFlags.DisableBuilderModeToggle |
                        SearchViewFlags.DisableInspectorPreview
            };

            picker = SearchService.ShowPicker(state);
        }

        // The library's default action keeps the search window open, and the picker inherits that on Enter.
        static void ClosePicker(ISearchView picker, EditorWindow origin)
        {
            if (picker is not EditorWindow window || !window)
                return;

            var restoreFocus = EditorWindow.focusedWindow == window;
            window.Close();
            if (restoreFocus && origin)
                origin.Focus();
        }
    }
}
