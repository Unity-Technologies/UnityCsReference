// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Search not yet converted
using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed partial class MainToolbarPicker : SearchPickerWindow
    {
        // An item's path never changes while the picker is open, so sorting on it keeps every row in place.
        internal sealed class StableIdComparer : ISearchListComparer
        {
            public int Compare(SearchItem x, SearchItem y)
            {
                var rankCompare = CompareElementSortRank(x, y);
                if (rankCompare != 0)
                    return rankCompare;

                // Ordinal tie-break for everything with no rank (e.g. Menus/Favorites), which falls back to alphabetical-by-id.
                var comparison = string.Compare(x?.id, y?.id, StringComparison.OrdinalIgnoreCase);
                return comparison != 0 ? comparison : string.CompareOrdinal(x?.id, y?.id);
            }

            // Orders toolbar-element leaves and category headers by MainToolbar's own sorted overlay list, not alphabetically.
            static int CompareElementSortRank(SearchItem x, SearchItem y)
            {
                if (!HasElementRankProvenance(x) || !HasElementRankProvenance(y))
                    return 0;
                if (!MainToolbar.TryGetElementSortRank(GetRankPath(x), out var rankX) ||
                    !MainToolbar.TryGetElementSortRank(GetRankPath(y), out var rankY))
                    return 0;
                return rankX.CompareTo(rankY);
            }

            static string GetRankPath(SearchItem item) =>
                item == null ? string.Empty : Providers.MainToolbarElementProvider.GetElementPath(item);

            // MainToolbar's rank table only knows Toolbar Elements' own paths, so a Menu Items node sharing a bare name must not be ranked by it.
            static bool HasElementRankProvenance(SearchItem item) =>
                item != null && (item.data is Overlay ||
                    HierarchySearchItemHandler.IsBuiltinParentSearchItemForProvider(item, Providers.MainToolbarElementProvider.type));
        }

        [OnCodeLoaded]
        static void RegisterMainToolbarPicker()
        {
            MainToolbarWindow.pickerRequested += OpenPicker;
        }

        internal static void OpenPicker(MainToolbar.PickerMode mode, string filter = "") => Open(GetInitialGroup(mode), filter);

        static string GetInitialGroup(MainToolbar.PickerMode mode) => mode switch
        {
            MainToolbar.PickerMode.ToolbarElements => Providers.MainToolbarElementProvider.type,
            MainToolbar.PickerMode.MenuItems => Providers.MenuProvider.type,
            _ => GroupedSearchList.allGroupId
        };

        static void Open(string initialGroup, string filter)
        {
            // Ensures this session starts from a fresh snapshot; see GetSortedAvailableOverlaysCached.
            MainToolbar.InvalidatePickerOverlayCache();

            var context = SearchService.CreateContext(new[]
            {
                Providers.MainToolbarElementProvider.type,
                Providers.MenuProvider.type,
                Providers.ToolbarFavoritesProvider.type
            }, filter ?? string.Empty);
            // MainToolbarElementProvider and ToolbarFavoritesProvider are isExplicitProvider; treat them as normal providers in this context.
            context.useExplicitProvidersAsNormalProviders = true;
            // MenuProvider must list every item on an empty query here, even with 3 active providers.
            context.userData = Providers.MenuProvider.ProviderMode.Finder;
            var state = SearchViewState.CreatePickerState(L10n.Tr("Main Toolbar", null), context, null);
            state.excludeClearItem = true;
            // Matches ShowPicker's trailing-space normalization, or a pre-filled query fuses the next typed character onto it.
            if (!state.queryBuilderEnabled && !string.IsNullOrEmpty(context.searchText))
            {
                if (!state.text.EndsWith(' '))
                    state.text += ' ';
                if (!state.initialQuery.EndsWith(' '))
                    state.initialQuery += ' ';
            }
            // Captured before assignment below; the closure only runs once ShowAuxWindow triggers the tree view's lazy creation.
            MainToolbarPicker picker = null;
            state.resultViewDescriptorList = new SearchResultViewDescriptorList(new[] { SearchTreeView.GetDescriptor(treeView => picker.WireRowToggle(treeView)) });
            state.group = initialGroup;

            // Open() stands in for ShowPicker; without this, providers that consult a runtime context see null.
            InjectDefaultRuntimeContext(context);

            picker = Create<MainToolbarPicker>(state);
            // Replaces the default label/favorite-based ordering; see StableIdComparer.
            picker.searchView?.SetSearchItemComparer(new StableIdComparer());
            picker.selectCallback = picker.OnItemSelected;
            picker.rootVisualElement.RegisterCallback<PointerMoveEvent>(picker.OnPointerMove);
            picker.titleContent.text = L10n.Tr("Main Toolbar", null);
            // Favoriting only updates the row's star icon by default; refresh so Favorites stays live.
            picker.m_OffFavoriteChanged = Dispatcher.On(SearchEvent.ItemFavoriteStateChanged, picker.OnFavoriteChanged);
            // The result view can be recreated later (e.g. results going empty and back); this fires on every such swap.
            picker.m_OffDisplayModeChanged = Dispatcher.On(SearchEvent.DisplayModeChanged, picker.OnDisplayModeChanged);
            picker.ShowAuxWindow();
            // As in ShowPicker: the window position can only be restored one frame after ShowAuxWindow.
            picker.RestoreWindowPosition(state);
            picker.Focus();
        }

        Overlay m_LastTrackedOverlay;
        string m_LastHoveredItemId;
        Action m_OffFavoriteChanged;
        Action m_OffDisplayModeChanged;
        // The result view can be swapped out (e.g. filtering), so this rebinds opportunistically instead of subscribing once at Open().
        SearchTreeView m_ContextMenuTreeView;

        void OnFavoriteChanged(ISearchEvent evt)
        {
            // A refetch resorts every group's tree, not just Favorites, so only pay that cost if Favorites is the visible tab.
            if (currentGroup == Providers.ToolbarFavoritesProvider.type)
            {
                Refresh(RefreshFlags.ItemsChanged);
                return;
            }

            // Otherwise, refetch just Favorites directly into its group (cheap, unlike MenuProvider's full menu-tree walk) and poke the tab bar.
            if (results is GroupedSearchList groupedResults &&
                groupedResults.GetGroupById(Providers.ToolbarFavoritesProvider.type) is IGroup favoritesGroup &&
                SearchService.GetProvider(Providers.ToolbarFavoritesProvider.type) is SearchProvider provider)
            {
                favoritesGroup.Clear();
                if (provider.fetchItems(context, new List<SearchItem>(), provider) is IEnumerable<SearchItem> freshItems)
                {
                    foreach (var item in freshItems)
                        favoritesGroup.Add(item);
                }

                searchView.RefreshContent(RefreshFlags.GroupChanged, updateView: false);
            }
        }

        // Handles the picker opening via shortcut with the cursor already over a row, which OnPointerMove alone would miss.
        void OnDisplayModeChanged(ISearchEvent evt)
        {
            if (resultView is SearchTreeView treeView)
                EnsureContextMenuHooked(treeView);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            SearchItem item = null;
            if (resultView is SearchTreeView treeView)
            {
                // Belt and braces behind OnDisplayModeChanged; idempotent, so it costs an early return.
                EnsureContextMenuHooked(treeView);
                treeView.TryGetSearchItemAtPosition(evt.position, out item);
            }

            if (item?.id == m_LastHoveredItemId)
                return;

            m_LastHoveredItemId = item?.id;
            m_LastTrackedOverlay?.SetHighlightEnabled(false);
            m_LastTrackedOverlay = ResolveOverlay(item);
            m_LastTrackedOverlay?.SetHighlightEnabled(true);
        }

        void EnsureContextMenuHooked(SearchTreeView treeView)
        {
            if (treeView == m_ContextMenuTreeView)
                return;

            if (m_ContextMenuTreeView != null)
            {
                m_ContextMenuTreeView.PopulateFolderContextMenu -= OnPopulateFolderContextMenu;
                m_ContextMenuTreeView.PopulateItemsContextMenu -= OnPopulateItemContextMenu;
            }
            treeView.PopulateFolderContextMenu += OnPopulateFolderContextMenu;
            treeView.PopulateItemsContextMenu += OnPopulateItemContextMenu;
            m_ContextMenuTreeView = treeView;
        }

        // Called synchronously during the tree view's construction (see Open()); a row can bind before any later event fires.
        void WireRowToggle(SearchTreeView treeView)
        {
            treeView.SearchItemHandler.RowToggleStateProvider = GetRowToggleState;
            treeView.SearchItemHandler.RowToggleClicked = ToggleDisplayed;
        }

        // No toggle on synthesized folder/category rows - same rows the context menu already skips.
        static bool? GetRowToggleState(SearchItem item) =>
            IsCategoryFolder(item) ? null : ResolveOverlay(item)?.displayed ?? false;

        // Mirrors the Show All / Hide All utility functions from Toolbar.PopulateMenuWithOverlays.
        void OnPopulateFolderContextMenu(SearchItem item, DropdownMenu menu)
        {
            if (!IsToolbarElementsFolder(item))
                return;

            menu.AppendAction(L10n.Tr("Show All", null), _ =>
            {
                MainToolbar.ShowAll(item.id);
                InvalidateFolderLabels(item.id);
            });
            menu.AppendAction(L10n.Tr("Hide All", null), _ =>
            {
                MainToolbar.HideAll(item.id);
                InvalidateFolderLabels(item.id);
            });
        }

        // Replaces the whole menu rather than appending: selectHandler already intercepts every provider action.
        void OnPopulateItemContextMenu(SearchItem item, DropdownMenu menu)
        {
            menu.ClearItems();

            // An unpinned menu item has no overlay to resolve, but that's equivalent to being hidden.
            var displayed = ResolveOverlay(item)?.displayed ?? false;
            menu.AppendAction(displayed ? L10n.Tr("Hide", null) : L10n.Tr("Show", null), _ => ToggleDisplayed(item));

            menu.AppendSeparator();

            if (SearchSettings.searchItemFavorites.Contains(item.id))
                menu.AppendAction(L10n.Tr("Remove from Favorites", null), _ => SearchSettings.RemoveItemFavorite(item));
            else
                menu.AppendAction(L10n.Tr("Add to Favorites", null), _ => SearchSettings.AddItemFavorite(item));
        }

        // Shared by the context menu's Show/Hide action and the row toggle button - same behavior either way.
        void ToggleDisplayed(SearchItem item)
        {
            if (item.data is Overlay elementOverlay)
            {
                elementOverlay.displayed = !elementOverlay.displayed;
                item.label = null; // fetchLabel caches this; clear it so it recomputes.
                InvalidateElementLabel(elementOverlay);
                return;
            }

            // A menu item only exists as an overlay while pinned, so Hide unpins outright rather than leaving it stranded hidden-but-pinned.
            if (ResolveOverlay(item) is Overlay menuOverlay)
            {
                if (menuOverlay.displayed)
                    MainToolbar.UnpinMenuItem(item.id);
                else
                    menuOverlay.displayed = true; // Pinned but hidden by some other means; just re-show it.
            }
            else
            {
                MainToolbar.PinMenuItem(item.id);
            }
            Refresh(RefreshFlags.ItemsChanged); // Pinning/unpinning changes which items exist.
        }

        void InvalidateFolderLabels(string folderPath) =>
            InvalidateOverlayLabels(overlay => overlay.id.StartsWith(folderPath, StringComparison.Ordinal));

        void InvalidateElementLabel(Overlay overlay) =>
            InvalidateOverlayLabels(o => o == overlay);

        // One overlay can back multiple SearchItems (e.g. one per tab); invalidate every match without RefreshFlags.ItemsChanged's full resort.
        void InvalidateOverlayLabels(Func<Overlay, bool> matches)
        {
            // Walks provider groups directly, not the de-duplicating "all" group, so pinned menu items (resolved by id, not item.data) are included too.
            if (results is GroupedSearchList groupedResults)
            {
                foreach (var group in groupedResults.EnumerateGroups())
                {
                    foreach (var resultItem in group.items)
                    {
                        if (ResolveOverlay(resultItem) is Overlay overlay && matches(overlay))
                            resultItem.label = null; // fetchLabel caches this; clear it so it recomputes.
                    }
                }
            }
            else
            {
                foreach (var resultItem in results)
                {
                    if (ResolveOverlay(resultItem) is Overlay overlay && matches(overlay))
                        resultItem.label = null;
                }
            }

            if (resultView is SearchTreeView treeView)
                treeView.SearchItemHandler.UpdateSearchItemFavoriteState(string.Empty);
        }

        // Menu items carry no data; resolve an already-pinned one by id instead.
        internal static Overlay ResolveOverlay(SearchItem item)
        {
            if (item?.data is Overlay overlay)
                return overlay;
            if (item != null && MainToolbar.TryGetOverlay($"{MainToolbar.menuItemOverlayIdPrefix}{item.id}", out var pinnedOverlay))
                return pinnedOverlay;
            return null;
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            // SearchViewState.context is [NonSerialized]: a window restored from a saved layout rebuilds it without userData.
            if (context != null)
                context.userData = Providers.MenuProvider.ProviderMode.Finder;

            // Open()'s wiring (selectCallback, hooks, comparer) is lost on reload, so a restored window would misbehave; close instead.
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        internal override void OnDisable()
        {
            MainToolbar.InvalidatePickerOverlayCache();
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            m_OffFavoriteChanged?.Invoke();
            m_OffDisplayModeChanged?.Invoke();
            m_LastTrackedOverlay?.SetHighlightEnabled(false);
            if (m_ContextMenuTreeView != null)
            {
                m_ContextMenuTreeView.PopulateFolderContextMenu -= OnPopulateFolderContextMenu;
                m_ContextMenuTreeView.PopulateItemsContextMenu -= OnPopulateItemContextMenu;
            }

            // base.OnDisable() re-invokes selectCallback as a final commit pick, which would replay the last real pick here; make that a no-op.
            selectCallback = null;

            base.OnDisable();
        }

        void OnItemSelected(SearchItem item, bool canceled)
        {
            if (!canceled)
            {
                if (ToggleIfFolder(item))
                {
                    InvalidateFolderLabels(item.id);
                }
                else if (ToggleIfElement(item))
                {
                    if (item.data is Overlay overlay)
                        InvalidateElementLabel(overlay);
                }
                else if (!IsCategoryFolder(item))
                {
                    HandleMenuItemPick(item);
                    Refresh(RefreshFlags.ItemsChanged); // Pinning/unpinning changes which items exist.
                }
            }

            // SearchView.ExecuteAction nulls selectHandler right after invoking it; re-arm next tick.
            EditorApplication.delayCall += () =>
            {
                if (this)
                    selectCallback = OnItemSelected;
            };
        }

        public override bool CanCloseWindowOnAction()
        {
            // Base SearchPickerWindow always returns true; this picker stays open for repeated picks.
            return false;
        }

        public override void ExecuteSelection()
        {
            // Keyboard-Enter path; omits the base's CloseSearchWindow() call for the same reason.
            if (selectCallback == null || selection.Count == 0)
                return;
            selectCallback(selection.First(), false);
        }

        // Double-click (or Enter) on a Toolbar Elements folder node toggles all its elements at once.
        internal static bool ToggleIfFolder(SearchItem item)
        {
            if (!IsToolbarElementsFolder(item))
                return false;

            MainToolbar.ToggleAll(item.id);
            return true;
        }

        internal static bool IsToolbarElementsFolder(SearchItem item) =>
            HierarchySearchItemHandler.IsBuiltinParentSearchItemForProvider(item, Providers.MainToolbarElementProvider.type);

        // Any synthesized category node from any provider, unlike ToggleIfFolder (Toolbar Elements only) - a pick here must not fall through to HandleMenuItemPick.
        internal static bool IsCategoryFolder(SearchItem item) =>
            HierarchySearchItemHandler.IsBuiltinParentSearchItem(item);

        internal static bool ToggleIfElement(SearchItem item)
        {
            // Checked by data type, not provider id, so this also works for Favorites-tab items.
            if (item.data is not Overlay overlay)
                return false;

            overlay.displayed = !overlay.displayed;
            item.label = null; // fetchLabel caches this; clear it so it recomputes.
            return true;
        }

        internal static void HandleMenuItemPick(SearchItem item)
        {
            if (MainToolbar.IsMenuItemPinned(item.id)
                && MainToolbar.TryGetOverlay($"{MainToolbar.menuItemOverlayIdPrefix}{item.id}", out var overlay))
                overlay.displayed = !overlay.displayed;
            else
                MainToolbar.PinMenuItem(item.id);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
