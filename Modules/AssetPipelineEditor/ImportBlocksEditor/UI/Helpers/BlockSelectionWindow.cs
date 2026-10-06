// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;
using UnityEditor.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal class BlockSelectionWindow : EditorWindow
    {
        Type m_RequiredInterface;
        Action<Type> m_OnBlockSelected;

        ToolbarSearchField m_SearchField;
        TreeView m_TreeView;
        List<TreeViewItemData<BlockItemData>> m_AllTreeItems;
        Label m_DetailPaneHeading;
        Label m_DetailTitle;
        Label m_DetailDescriptionHeading;
        Label m_DetailDescription;
        Label m_DetailInterfacesHeading;
        Label m_DetailInterfaces;
        ScrollView m_DetailScrollView;
        VisualElement m_MainContainer;
        VisualElement m_LeftPane;
        VisualElement m_RightPane;
        TwoPaneSplitView m_SplitView;
        Button m_ToggleButton;
        bool m_RightPaneVisible = true;
        float m_SinglePaneWidth = 250f;
        Rect m_ButtonRect;

        const string k_DetailsPaneVisiblePrefKey = "ImportBlocks.BlockSelectionWindow.DetailsPaneVisible";
        const string k_SelectedBlockTypePrefKey = "ImportBlocks.BlockSelectionWindow.SelectedBlockType";
        const string k_ExpandedCategoriesPrefKey = "ImportBlocks.BlockSelectionWindow.ExpandedCategories";
        [NoAutoStaticsCleanup] // Stateless dispatcher; its internal cache clears itself on reload (BlockDropdownItemCache.s_CachedRoots).
        static readonly IBlockDropdownItemCache s_DropdownItemCache = new BlockDropdownItemCache();

        public static void Show(Rect buttonRect, Type requiredInterface, Action<Type> onBlockSelected)
        {
            var window = CreateInstance<BlockSelectionWindow>();
            window.m_RequiredInterface = requiredInterface;
            window.m_OnBlockSelected = onBlockSelected;
            window.m_ButtonRect = buttonRect;

            bool detailsVisible = EditorPrefs.GetBool(k_DetailsPaneVisiblePrefKey, true);

            // With the details pane hidden the window matches the triggering button's width; fall back to 250
            // when there's no real button rect (e.g. the mouse-anchored insert path).
            window.m_SinglePaneWidth = buttonRect.width > 1f ? buttonRect.width : 250f;
            var windowSize = detailsVisible ? new Vector2(500, 400) : new Vector2(window.m_SinglePaneWidth, 400);

            window.ShowCenteredUnderButton(windowSize);
        }

        // Positions the window centered horizontally on the anchor button, keeping the button's vertical extent
        // so ShowAsDropDown drops below the button and flips above it when there's no room. The ideal center is
        // always recomputed from the button rect (never the current, possibly screen-clamped window position),
        // so toggling the details pane re-centers on the button instead of compounding an earlier clamp offset.
        void ShowCenteredUnderButton(Vector2 windowSize)
        {
            var anchorRect = new Rect(
                m_ButtonRect.center.x - (windowSize.x / 2f),
                m_ButtonRect.y,
                m_ButtonRect.width,
                m_ButtonRect.height);

            ShowAsDropDown(anchorRect, windowSize);
        }

        void CreateGUI()
        {
            m_RightPaneVisible = EditorPrefs.GetBool(k_DetailsPaneVisiblePrefKey, true);

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.AddToClassList("block-selection-root");

            var styleSheet = UiHelpers.GetBlockItemStyleSheet();
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            // Structure comes from BlockSelectionWindow.uxml; clone it into root and query the named
            // elements. The panes are re-parented at runtime by UpdateRightPaneVisibility (split view vs
            // left pane only), so they're queried once here and reorganized there.
            UiHelpers.GetBlockSelectionWindowTemplate()?.CloneTree(root);

            m_MainContainer = root.Q<VisualElement>("main-container");
            m_LeftPane = root.Q<VisualElement>("left-pane");
            m_RightPane = root.Q<VisualElement>("right-pane");
            m_ToggleButton = root.Q<Button>("toggle-button");
            m_SearchField = root.Q<ToolbarSearchField>("search-field");
            m_TreeView = root.Q<TreeView>("tree-view");
            m_DetailPaneHeading = root.Q<Label>("detail-pane-heading");
            m_DetailScrollView = root.Q<ScrollView>("detail-scroll");
            m_DetailTitle = root.Q<Label>("detail-title");
            m_DetailDescriptionHeading = root.Q<Label>("detail-description-heading");
            m_DetailDescription = root.Q<Label>("detail-description");
            m_DetailInterfacesHeading = root.Q<Label>("detail-interfaces-heading");
            m_DetailInterfaces = root.Q<Label>("detail-interfaces");

            ConfigureToggleButtonIcon();
            m_ToggleButton.clicked += ToggleRightPane;

            m_SearchField.RegisterValueChangedCallback(OnSearchChanged);

            m_TreeView.selectionType = SelectionType.Single;
            m_TreeView.selectedIndicesChanged += OnSelectionChanged;
            m_TreeView.itemsChosen += OnItemChosen;
            m_TreeView.makeItem = () => new Label();
            m_TreeView.bindItem = (element, index) =>
            {
                var label = element as Label;
                var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(index);
                if (label != null && itemData != null)
                {
                    label.text = itemData.Name;
                    label.style.unityFontStyleAndWeight = itemData.IsCategory ? FontStyle.Bold : FontStyle.Normal;
                }
            };

            ShowEmptyState();

            BuildTreeView();

            UpdateRightPaneVisibility();

            var expandedCategoriesString = EditorPrefs.GetString(k_ExpandedCategoriesPrefKey, "");
            if (!string.IsNullOrEmpty(expandedCategoriesString))
            {
                var parts = expandedCategoriesString.Split(';');
                var expandedCategories = new List<string>();
                foreach (var s in parts)
                {
                    if (!string.IsNullOrEmpty(s))
                        expandedCategories.Add(s);
                }
                RestoreExpandedCategories(expandedCategories);
                EditorPrefs.DeleteKey(k_ExpandedCategoriesPrefKey);
            }

            var savedBlockTypeName = EditorPrefs.GetString(k_SelectedBlockTypePrefKey, "");
            if (!string.IsNullOrEmpty(savedBlockTypeName))
            {
                RestoreSelection(savedBlockTypeName);
                EditorPrefs.DeleteKey(k_SelectedBlockTypePrefKey);
            }

            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);

            m_SearchField.schedule.Execute(() =>
            {
                m_SearchField.Q<TextField>().Q("unity-text-input").Focus();
                SelectFirstBlock();
            });
        }

        List<string> GetExpandedCategories()
        {
            var expandedCategories = new List<string>();
            var allIds = m_TreeView.viewController.GetAllItemIds();
            foreach (var id in allIds)
            {
                var index = m_TreeView.viewController.GetIndexForId(id);
                if (index >= 0 && m_TreeView.viewController.IsExpanded(id))
                {
                    var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(index);
                    if (itemData != null && itemData.IsCategory)
                    {
                        expandedCategories.Add(itemData.Name);
                    }
                }
            }

            return expandedCategories;
        }

        void RestoreExpandedCategories(List<string> categoryNames)
        {
            var allIds = new List<int>(m_TreeView.viewController.GetAllItemIds());
            foreach (var categoryName in categoryNames)
            {
                foreach (var id in allIds)
                {
                    var index = m_TreeView.viewController.GetIndexForId(id);
                    if (index >= 0)
                    {
                        var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(index);
                        if (itemData != null && itemData.IsCategory && itemData.Name == categoryName)
                        {
                            m_TreeView.ExpandItem(id);
                            break;
                        }
                    }
                }
            }
        }

        void RestoreSelection(string blockTypeFullName)
        {
            var allItemIds = new List<int>(m_TreeView.viewController.GetAllItemIds());

            for (int i = 0; i < allItemIds.Count; i++)
            {
                var index = m_TreeView.viewController.GetIndexForId(allItemIds[i]);
                if (index >= 0)
                {
                    var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(index);
                    if (itemData != null && itemData.BlockType != null &&
                        itemData.BlockType.AssemblyQualifiedName == blockTypeFullName)
                    {
                        m_TreeView.selectedIndex = index;
                        m_TreeView.ScrollToItem(index);
                        m_TreeView.Focus();
                        break;
                    }
                }
            }
        }

        void ConfigureToggleButtonIcon()
        {
            // Try a few icon names to find the info icon across editor versions/skins.
            string[] iconNames =
            {
                "console.infoicon.inactive.sml", "console.infoicon.sml", "console.infoicon.inactive",
                "console.infoicon", "_Help", "UnityEditor.InspectorWindow"
            };
            Texture2D icon = null;

            foreach (var iconName in iconNames)
            {
                var content = EditorGUIUtility.IconContent(iconName);
                if (content?.image != null)
                {
                    icon = content.image as Texture2D;
                    break;
                }
            }

            if (icon != null)
            {
                m_ToggleButton.style.backgroundImage = new StyleBackground(icon);
                m_ToggleButton.style.backgroundSize =
                    BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop);
                m_ToggleButton.style.backgroundRepeat =
                    BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop);
                m_ToggleButton.style.backgroundPositionX =
                    BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
                m_ToggleButton.style.backgroundPositionY =
                    BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
                m_ToggleButton.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(12, 12));
            }
        }

        void ToggleRightPane()
        {
            m_RightPaneVisible = !m_RightPaneVisible;
            EditorPrefs.SetBool(k_DetailsPaneVisiblePrefKey, m_RightPaneVisible);

            var expandedCategories = GetExpandedCategories();
            EditorPrefs.SetString(k_ExpandedCategoriesPrefKey, string.Join(";", expandedCategories));

            var selectedIndex = m_TreeView.selectedIndex;
            if (selectedIndex >= 0)
            {
                var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(selectedIndex);
                if (itemData != null && itemData.BlockType != null)
                {
                    EditorPrefs.SetString(k_SelectedBlockTypePrefKey, itemData.BlockType.AssemblyQualifiedName);
                }
            }

            var newWidth = m_RightPaneVisible ? 500f : m_SinglePaneWidth;

            Close();

            var window = CreateInstance<BlockSelectionWindow>();
            window.m_RequiredInterface = m_RequiredInterface;
            window.m_OnBlockSelected = m_OnBlockSelected;
            window.m_SinglePaneWidth = m_SinglePaneWidth;
            window.m_ButtonRect = m_ButtonRect;

            window.ShowCenteredUnderButton(new Vector2(newWidth, 400));
        }

        void UpdateRightPaneVisibility()
        {
            if (m_MainContainer == null || m_LeftPane == null || m_RightPane == null)
                return;

            m_MainContainer.Clear();

            if (m_RightPaneVisible)
            {
                if (m_SplitView == null)
                {
                    m_SplitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
                    m_SplitView.style.flexGrow = 1;
                }

                m_SplitView.Clear();

                m_LeftPane.style.flexGrow = 0;
                m_LeftPane.style.width = 250;

                m_RightPane.style.flexGrow = 0;
                m_RightPane.style.width = 250;

                m_SplitView.Add(m_LeftPane);
                m_SplitView.Add(m_RightPane);
                m_MainContainer.Add(m_SplitView);
            }
            else
            {
                m_LeftPane.style.flexGrow = 1;
                m_LeftPane.style.width = StyleKeyword.Auto;
                m_MainContainer.Add(m_LeftPane);
            }

            if (m_ToggleButton != null)
            {
                m_ToggleButton.EnableInClassList("block-details-toggle--active", m_RightPaneVisible);
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                var selectedIndex = m_TreeView.selectedIndex;
                if (selectedIndex >= 0)
                {
                    var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(selectedIndex);
                    if (itemData != null && !itemData.IsCategory && itemData.BlockType != null)
                    {
                        m_OnBlockSelected?.Invoke(itemData.BlockType);
                        Close();
                        evt.StopPropagation();
                    }
                }
            }
            else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow)
            {
                if (m_TreeView.selectedIndex < 0)
                    SelectFirstBlock();
                else
                    SelectNextBlock(evt.keyCode == KeyCode.DownArrow ? 1 : -1);
                evt.StopPropagation();
            }
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            // Up/Down list navigation is handled once in OnKeyDown. When the tree has focus the collection view
            // would also navigate on this synthesized event, advancing the selection a second time (skipping a
            // row); swallow the vertical directions so only OnKeyDown moves. Left/Right still reach the tree so
            // categories can expand/collapse.
            if (evt.direction == NavigationMoveEvent.Direction.Up ||
                evt.direction == NavigationMoveEvent.Direction.Down)
                evt.StopPropagation();
        }

        void SelectFirstBlock()
        {
            var itemCount = m_TreeView.viewController.GetItemsCount();
            for (int i = 0; i < itemCount; i++)
            {
                var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(i);
                if (itemData != null && !itemData.IsCategory)
                {
                    m_TreeView.selectedIndex = i;
                    m_TreeView.ScrollToItem(i);
                    return;
                }
            }
        }

        void SelectNextBlock(int direction)
        {
            var itemCount = m_TreeView.viewController.GetItemsCount();
            if (itemCount == 0) return;

            var current = m_TreeView.selectedIndex;
            var index = current;

            for (int attempt = 0; attempt < itemCount; attempt++)
            {
                index += direction;
                if (index >= itemCount) index = 0;
                if (index < 0) index = itemCount - 1;

                var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(index);
                if (itemData != null && !itemData.IsCategory)
                {
                    m_TreeView.selectedIndex = index;
                    m_TreeView.ScrollToItem(index);
                    return;
                }
            }
        }

        void BuildTreeView()
        {
            var dropdownRoot = s_DropdownItemCache.GetOrBuildDropdownRoot(m_RequiredInterface);
            var treeViewItems = new List<TreeViewItemData<BlockItemData>>();
            int currentId = 0;

            ConvertDropdownToTreeView(dropdownRoot, treeViewItems, ref currentId);

            m_AllTreeItems = treeViewItems;
            m_TreeView.SetRootItems(treeViewItems);
            m_TreeView.Rebuild();
        }

        static bool HasChildren(AdvancedDropdownItem item) => item.childList.Count > 0;

        void ConvertDropdownToTreeView(AdvancedDropdownItem dropdownItem,
            List<TreeViewItemData<BlockItemData>> treeItems, ref int currentId)
        {
            foreach (var child in dropdownItem.childList)
            {
                if (child is BlockDropdownItem blockItem)
                {
                    var itemData = new BlockItemData
                    {
                        Name = blockItem.name,
                        BlockType = blockItem.BlockType,
                        Description = blockItem.Description,
                        IsCategory = false
                    };

                    treeItems.Add(new TreeViewItemData<BlockItemData>(currentId++, itemData));
                }
                else if (HasChildren(child))
                {
                    var categoryData = new BlockItemData
                    {
                        Name = child.name,
                        IsCategory = true
                    };

                    var childItems = new List<TreeViewItemData<BlockItemData>>();
                    ConvertDropdownToTreeView(child, childItems, ref currentId);

                    treeItems.Add(new TreeViewItemData<BlockItemData>(currentId++, categoryData, childItems));
                }
            }
        }

        void OnSelectionChanged(IEnumerable<int> selectedIndices)
        {
            int selectedIndex = -1;
            foreach (var index in selectedIndices)
            {
                selectedIndex = index;
                break;
            }
            if (selectedIndex < 0)
            {
                ShowEmptyState();
                return;
            }

            var itemData = m_TreeView.GetItemDataForIndex<BlockItemData>(selectedIndex);
            if (itemData == null || itemData.IsCategory)
            {
                ShowEmptyState();
                return;
            }

            ShowBlockDetails(itemData);
        }

        void OnItemChosen(IEnumerable<object> chosenItems)
        {
            BlockItemData itemData = null;
            foreach (var item in chosenItems)
            {
                itemData = item as BlockItemData;
                break;
            }
            if (itemData != null && !itemData.IsCategory && itemData.BlockType != null)
            {
                m_OnBlockSelected?.Invoke(itemData.BlockType);
                Close();
            }
        }

        void ShowEmptyState()
        {
            m_DetailPaneHeading.style.display = DisplayStyle.Flex;
            m_DetailTitle.text = "";
            m_DetailDescriptionHeading.style.display = DisplayStyle.None;
            m_DetailDescription.text = "Select a block to view details";
            m_DetailDescription.style.display = DisplayStyle.Flex;
            m_DetailInterfacesHeading.style.display = DisplayStyle.None;
            m_DetailInterfaces.text = "";
            m_DetailInterfaces.style.display = DisplayStyle.None;
        }

        void ShowBlockDetails(BlockItemData itemData)
        {
            m_DetailPaneHeading.style.display = DisplayStyle.None;
            m_DetailTitle.text = itemData.Name;

            if (!string.IsNullOrEmpty(itemData.Description))
            {
                m_DetailDescriptionHeading.style.display = DisplayStyle.Flex;
                m_DetailDescription.text = itemData.Description;
                m_DetailDescription.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_DetailDescriptionHeading.style.display = DisplayStyle.None;
                m_DetailDescription.style.display = DisplayStyle.None;
            }

            var interfacesText = BuildInterfacesText(itemData.BlockType);
            if (!string.IsNullOrEmpty(interfacesText))
            {
                m_DetailInterfacesHeading.style.display = DisplayStyle.Flex;
                m_DetailInterfaces.text = interfacesText;
                m_DetailInterfaces.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_DetailInterfacesHeading.style.display = DisplayStyle.None;
                m_DetailInterfaces.style.display = DisplayStyle.None;
            }
        }

        string BuildInterfacesText(Type blockType)
        {
            if (blockType == null)
                return null;

            var interfaces = blockType.GetInterfaces();
            var allInterfaces = new List<Type>();
            foreach (var i in interfaces)
            {
                if (i != typeof(IBlock) && typeof(IBlock).IsAssignableFrom(i))
                {
                    allInterfaces.Add(i);
                }
            }

            var leafInterfaces = new List<Type>();
            foreach (var iface in allInterfaces)
            {
                bool isLeaf = true;
                foreach (var other in allInterfaces)
                {
                    if (other != iface && iface.IsAssignableFrom(other))
                    {
                        isLeaf = false;
                        break;
                    }
                }

                if (isLeaf)
                {
                    leafInterfaces.Add(iface);
                }
            }

            var filteredInterfaces = new List<Type>();
            foreach (var iface in leafInterfaces)
            {
                if (m_RequiredInterface.IsAssignableFrom(iface))
                {
                    filteredInterfaces.Add(iface);
                }
            }

            filteredInterfaces.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            if (filteredInterfaces.Count == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var iface in filteredInterfaces)
            {
                var interfaceName = iface.Name;
                if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
                {
                    interfaceName = interfaceName.Substring(1);
                }

                sb.AppendLine($"  • {interfaceName}");
            }

            return sb.ToString().TrimEnd();
        }

        void OnSearchChanged(ChangeEvent<string> evt)
        {
            var query = evt.newValue;
            if (string.IsNullOrEmpty(query))
            {
                m_TreeView.SetRootItems(m_AllTreeItems);
                m_TreeView.Rebuild();
                SelectFirstBlock();
                return;
            }

            var filtered = FilterTreeItems(m_AllTreeItems, query);
            m_TreeView.SetRootItems(filtered);
            m_TreeView.Rebuild();
            m_TreeView.ExpandAll();
            SelectFirstBlock();
            SelectFirstBlock();
        }

        List<TreeViewItemData<BlockItemData>> FilterTreeItems(
            List<TreeViewItemData<BlockItemData>> items, string query)
        {
            var result = new List<TreeViewItemData<BlockItemData>>();
            foreach (var item in items)
            {
                if (item.data.IsCategory)
                {
                    var filteredChildren = FilterTreeItems(
                        new List<TreeViewItemData<BlockItemData>>(item.children), query);
                    if (filteredChildren.Count > 0)
                    {
                        result.Add(new TreeViewItemData<BlockItemData>(
                            item.id, item.data, filteredChildren));
                    }
                }
                else if (item.data.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        class BlockItemData
        {
            public string Name;
            public Type BlockType;
            public string Description;
            public bool IsCategory;
        }
    }
}
