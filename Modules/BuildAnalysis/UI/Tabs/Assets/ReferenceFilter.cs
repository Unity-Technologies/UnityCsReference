// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// The name and extension filter shared by the inspector's reference lists and the Referenced
    /// By tree's direct parents: one extension choice and a case-insensitive name substring.
    /// Items are addressed by index; the owner keeps the rows and asks which indices pass.
    /// </summary>
    internal sealed class ReferenceFilter
    {
        public const string k_AllExtensions = "All";
        private const int k_NoExtension = -1;

        private readonly List<string> m_Choices = new List<string> { k_AllExtensions };
        private string[] m_ExtensionPool = Array.Empty<string>();
        private int[] m_ExtensionIds = Array.Empty<int>();
        private int m_ViewFilterId = k_NoExtension;
        private string m_SearchText = string.Empty;

        /// <summary>"All" followed by the items' distinct extensions, lowercase and sorted.</summary>
        public IReadOnlyList<string> Choices => m_Choices;
        public string SearchText => m_SearchText;
        public string ViewValue => m_ViewFilterId == k_NoExtension ? k_AllExtensions : m_ExtensionPool[m_ViewFilterId];

        /// <summary>Records the items' extensions from their paths and clears both filters.</summary>
        public void SetItems(IReadOnlyList<string> paths)
        {
            var pool = new List<string>();
            var ids = new int[paths.Count];
            var byExtension = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < paths.Count; i++)
            {
                var extension = string.IsNullOrEmpty(paths[i]) ? string.Empty : Path.GetExtension(paths[i]).ToLowerInvariant();
                if (extension.Length == 0)
                {
                    ids[i] = k_NoExtension;
                    continue;
                }
                if (!byExtension.TryGetValue(extension, out var id))
                {
                    id = pool.Count;
                    pool.Add(extension);
                    byExtension.Add(extension, id);
                }
                ids[i] = id;
            }

            // The pool is sorted for the dropdown; the ids were handed out in insertion order.
            var sorted = pool.ToArray();
            Array.Sort(sorted, StringComparer.Ordinal);
            var remap = new int[pool.Count];
            for (var sortedId = 0; sortedId < sorted.Length; sortedId++)
                remap[byExtension[sorted[sortedId]]] = sortedId;
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] != k_NoExtension)
                    ids[i] = remap[ids[i]];
            }

            m_ExtensionPool = sorted;
            m_ExtensionIds = ids;
            m_Choices.Clear();
            m_Choices.Add(k_AllExtensions);
            m_Choices.AddRange(sorted);
            Reset();
        }

        public void Reset()
        {
            m_ViewFilterId = k_NoExtension;
            m_SearchText = string.Empty;
        }

        /// <summary>Returns true when the filter changed.</summary>
        public bool SetSearchText(string value)
        {
            var text = value ?? string.Empty;
            if (string.Equals(text, m_SearchText, StringComparison.Ordinal))
                return false;
            m_SearchText = text;
            return true;
        }

        /// <summary>Takes "All" or an extension from <see cref="Choices"/>; returns true when the filter changed.</summary>
        public bool SetView(string value)
        {
            var id = string.IsNullOrEmpty(value) || string.Equals(value, k_AllExtensions, StringComparison.Ordinal)
                ? k_NoExtension
                : Array.IndexOf(m_ExtensionPool, value.ToLowerInvariant());
            if (id == m_ViewFilterId)
                return false;
            m_ViewFilterId = id;
            return true;
        }

        public bool Matches(int index, string name)
        {
            if (m_ViewFilterId != k_NoExtension && ((uint)index >= (uint)m_ExtensionIds.Length || m_ExtensionIds[index] != m_ViewFilterId))
                return false;
            return m_SearchText.Length == 0 || (name != null && name.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    /// <summary>
    /// The toolbar the inspector's lists share, as the 5 October design has it: the search field
    /// takes the row, and a filter icon at its right opens the extension choices as a menu. The icon
    /// fills in while an extension is chosen, which is the only sign the list is narrowed.
    /// </summary>
    internal static class ReferenceListToolbar
    {
        public const int k_SearchDebounceMs = 200;
        public const string k_FilterClass = "inspector__ref-filter";
        public const string k_FilterActiveClass = "inspector__ref-filter--active";
        const string k_FilterTooltip = "Filter by file extension";

        public static VisualElement Make(out ToolbarSearchField search, out ToolbarMenu filter)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("data-list__toolbar");
            toolbar.AddToClassList("inspector__ref-toolbar");

            search = new ToolbarSearchField { name = "search-field", tooltip = "Search asset name" };
            search.AddToClassList("data-list__toolbar-search");
            toolbar.Add(search);

            // A ToolbarMenu is not focusable on its own; the dropdown it replaces was, so Tab still
            // reaches the filter and Enter opens it.
            var menu = new ToolbarMenu { name = "view-menu", tooltip = k_FilterTooltip, focusable = true };
            menu.AddToClassList(k_FilterClass);
            menu.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                menu.ShowMenu();
                evt.StopPropagation();
            });
            toolbar.Add(menu);
            filter = menu;
            return toolbar;
        }

        /// <summary>
        /// Rebuilds the menu from the filter's choices. Which one is checked is read from the filter
        /// when the menu opens, so a change of view only needs <see cref="ShowFilterState"/>.
        /// </summary>
        public static void SyncFilter(ToolbarMenu menu, ReferenceFilter filter, Action<string> choose)
        {
            menu.menu.ClearItems();
            foreach (var choice in filter.Choices)
            {
                menu.menu.AppendAction(choice, _ => choose(choice),
                    _ => choice == filter.ViewValue ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            ShowFilterState(menu, filter);
        }

        /// <summary>The filled icon, and the extension in the tooltip, while the chosen extension is not All.</summary>
        public static void ShowFilterState(ToolbarMenu menu, ReferenceFilter filter)
        {
            var active = filter.ViewValue != ReferenceFilter.k_AllExtensions;
            menu.EnableInClassList(k_FilterActiveClass, active);
            menu.tooltip = active ? k_FilterTooltip + " (" + filter.ViewValue + ")" : k_FilterTooltip;
        }
    }
}
