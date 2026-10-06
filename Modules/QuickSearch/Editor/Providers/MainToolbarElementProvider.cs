// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static partial class MainToolbarElementProvider
    {
        internal const string type = "toolbar-elements";
        private const string displayName = "Toolbar Elements";

        // Prefixes ids so a toolbar-element path can never collide with MenuProvider's raw menu path in the picker's shared "All" tab.
        const string k_ElementIdPrefix = type + "\x1f";

        internal static string GetElementItemId(string path) => k_ElementIdPrefix + path;

        internal static string GetElementPath(SearchItem item) =>
            item.id.StartsWith(k_ElementIdPrefix, StringComparison.Ordinal) ? item.id.Substring(k_ElementIdPrefix.Length) : item.id;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                isExplicitProvider = true, // stays out of the global Search Everything window
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.DefaultGroup,
                fetchItems = FetchItems,
                fetchLabel = FetchLabel,
                fetchThumbnail = FetchThumbnail,
                fetchParentDescriptor = FetchParentDescriptor,
            };
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var query = context.searchQuery ?? string.Empty;

            foreach (var entry in MainToolbar.GetSortedAvailableOverlaysCached())
            {
                if (query.Length > 0 && entry.attrib.path.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                yield return provider.CreateItem(context, GetElementItemId(entry.attrib.path), 0, null, null, null, entry.overlay);
            }
        }

        private static string FetchLabel(SearchItem item, SearchContext context)
        {
            if (item.label == null && item.data is Overlay overlay)
            {
                item.label = FormatElementLabel(Utils.GetFileName(GetElementPath(item)), overlay.displayed);
            }
            return item.label;
        }

        // Inactive elements are dimmed to 70% opacity instead of an explicit checkbox glyph.
        internal const float inactiveThumbnailAlpha = 0.7f;
        internal static string FormatElementLabel(string name, bool active) => active ? name : $"<alpha=#B3>{name}</alpha>";

        // Menu-item equivalent of overlay.displayed: active once pinned (which creates the overlay) and shown.
        internal static bool IsMenuItemActive(string menuPath) =>
            MainToolbar.TryGetOverlay($"{MainToolbar.menuItemOverlayIdPrefix}{menuPath}", out var overlay) && overlay.displayed;

        private static SearchItemParentDescriptor FetchParentDescriptor(SearchItem item, SearchContext context)
        {
            var path = GetElementPath(item);
            var lastSeparatorIndex = path.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
                return default;

            return new SearchItemParentDescriptor(path.Substring(0, lastSeparatorIndex), SearchItemParentType.TokenSeparatedId);
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            // Category nodes are synthesized by FetchParentDescriptor and carry no data; no icon for those.
            if (item.data is not Overlay overlay)
                return null;

            item.thumbnailAlpha = overlay.displayed ? 1f : inactiveThumbnailAlpha;
            return GetElementIcon();
        }

        const string k_ElementIconPath = "UIToolkit/Icons/CustomCSharpElement.png";

        // Shared by every element: invoking a per-element factory just to render the picker isn't safe (factories aren't guaranteed side-effect-free).
        internal static Texture2D GetElementIcon() => EditorGUIUtility.LoadIcon(k_ElementIconPath);
    }
}
