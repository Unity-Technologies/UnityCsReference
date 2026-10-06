// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static partial class ToolbarFavoritesProvider
    {
        internal const string type = "toolbar-favorites";
        private const string displayName = "Favorites";

        // Mirrors MenuProvider's own cache of this same native call, instead of paying that cost again on every FetchItems call.
        [AutoStaticsCleanupOnCodeReload]
        static List<string> s_MenuItemPaths;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            Menu.menuChanged -= InvalidateMenuItemPaths;
            Menu.menuChanged += InvalidateMenuItemPaths;

            return new SearchProvider(type, displayName)
            {
                isExplicitProvider = true, // stays out of the global Search Everything window
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.DefaultGroup,
                fetchItems = FetchItems,
                fetchLabel = FetchLabel,
                fetchThumbnail = FetchThumbnail,
            };
        }

        internal static void InvalidateMenuItemPaths() => s_MenuItemPaths = null;

        internal static List<string> GetMenuItemPaths()
        {
            if (s_MenuItemPaths == null)
            {
                s_MenuItemPaths = new List<string>();
                Utils.GetMenuItemDefaultShortcuts(s_MenuItemPaths, new List<string>());
            }
            return s_MenuItemPaths;
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var query = context.searchQuery ?? string.Empty;

            foreach (var entry in MainToolbar.GetSortedAvailableOverlaysCached())
            {
                // Favoriting stores the id actually shown in the tree, which is the prefixed element id.
                if (!SearchSettings.searchItemFavorites.Contains(MainToolbarElementProvider.GetElementItemId(entry.attrib.path)))
                    continue;
                if (query.Length > 0 && entry.attrib.path.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                yield return provider.CreateItem(context, MainToolbarElementProvider.GetElementItemId(entry.attrib.path), 0, null, null, null, entry.overlay);
            }

            foreach (var path in GetMenuItemPaths())
            {
                if (!SearchSettings.searchItemFavorites.Contains(path))
                    continue;
                // A favorited leaf path can later become a submenu container, which PinMenuItem already no-ops on.
                if (!MainToolbar.IsExecutableMenuItem(path))
                    continue;
                if (query.Length > 0 && path.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                yield return provider.CreateItem(context, path);
            }
        }

        private static string FetchLabel(SearchItem item, SearchContext context)
        {
            if (item.label != null)
                return item.label;

            var name = Utils.GetFileName(item.id);

            // Menu-item favorites carry no overlay; their active state hangs off whether they're pinned and shown.
            var active = item.data is Overlay overlay ? overlay.displayed : MainToolbarElementProvider.IsMenuItemActive(item.id);
            item.label = MainToolbarElementProvider.FormatElementLabel(name, active);
            return item.label;
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            if (item.data is not Overlay)
                return Icons.shortcut;

            return MainToolbarElementProvider.GetElementIcon();
        }
    }
}
