using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeView : BuilderLibraryView
    {
        const string k_TreeItemRootName = "root";
        const string k_TreeItemLabelName = "item-name-label";
        const string k_TreeItemEditorOnlyLabelName = "item-editor-only-label";
        const string k_TreeViewName = "library-tree-view";
        const string k_OpenButtonName = "item-open-button";

        const string k_TreeViewClassName = "unity-builder-library__tree-view";
        const string k_TreeViewItemWithButtonClassName = "unity-builder-library__tree-item-with-edit-button";

        readonly TreeView m_TreeView;
        readonly VisualTreeAsset m_TreeViewItemTemplate;

        public override VisualElement primaryFocusable => m_TreeView;

        public BuilderLibraryTreeView(IList<TreeViewItem> items)
        {
            m_TreeViewItemTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUIPath + "/BuilderLibraryTreeViewItem.uxml");

            style.flexGrow = 1;
            m_TreeView = new TreeView { name = k_TreeViewName };
            m_TreeView.AddToClassList(k_TreeViewClassName);
            Add(m_TreeView);

            m_TreeView.viewDataKey = "samples-tree";
            m_TreeView.fixedItemHeight = 20;
            m_TreeView.SetRootItems(items);
            m_TreeView.makeItem = MakeItem;
            m_TreeView.bindItem = BindItem;
            m_TreeView.itemsChosen += OnItemsChosen;
            m_TreeView.Rebuild();

            m_TreeView.ExpandRootItems();
        }

        void OnContextualMenuPopulateEvent(ContextualMenuPopulateEvent evt)
        {
            var libraryItem = GetLibraryTreeItem((VisualElement)evt.target);

            evt.menu.AppendAction(
                "Add",
                action => { AddItemToTheDocument(libraryItem); },
                action =>
                {
                    if (libraryItem.makeVisualElementCallback == null)
                        return DropdownMenuAction.Status.Disabled;

                    if (libraryItem.sourceAsset == m_PaneWindow.document.visualTreeAsset)
                        return DropdownMenuAction.Status.Disabled;

                    return DropdownMenuAction.Status.Normal;
                });

            if (libraryItem.sourceAsset != null)
            {
                evt.menu.AppendAction(
                    "Open In UIBuilder",
                    action => { m_PaneWindow.LoadDocument(libraryItem.sourceAsset); },
                    action =>
                    {
                        if (libraryItem.sourceAsset == m_PaneWindow.document.visualTreeAsset)
                            return DropdownMenuAction.Status.Disabled;

                        return DropdownMenuAction.Status.Normal;
                    });

                evt.menu.AppendAction(
                    "Open with IDE",
                    action => { AssetDatabase.OpenAsset(libraryItem.sourceAsset, BuilderConstants.OpenInIDELineNumber); },
                    action => DropdownMenuAction.Status.Normal);
            }
        }

        internal static CustomStyleProperty<int> s_DummyProperty = new CustomStyleProperty<int>("--my-dummy");

        VisualElement MakeItem()
        {
            var root = m_TreeViewItemTemplate.CloneTree().Q(k_TreeItemRootName);
            RegisterControlContainer(root);
            root.AddManipulator(new ContextualMenuManipulator(OnContextualMenuPopulateEvent));
            if (!EditorGUIUtility.isProSkin)
            {
                root.RegisterCustomBuilderStyleChangeEvent(builderElementStyle =>
                {
                    var libraryTreeItem = GetLibraryTreeItem(root);
                    if (libraryTreeItem.icon == null)
                        return;

                    var libraryTreeItemIcon = libraryTreeItem.icon;
                    if (builderElementStyle == BuilderElementStyle.Highlighted)
                        libraryTreeItemIcon = libraryTreeItem.darkSkinIcon;

                    AssignTreeItemIcon(root, libraryTreeItemIcon);
                });
            }

            // Open button.
            var openButton = root.Q<Button>(k_OpenButtonName);
            openButton.AddToClassList(BuilderConstants.HiddenStyleClassName);
            openButton.clickable.clickedWithEventInfo += OnOpenButtonClick;

            return root;
        }

        void OnOpenButtonClick(EventBase evt)
        {
            var button = evt.target as Button;
            var item = button.userData as BuilderLibraryTreeItem;

            if (item?.sourceAsset == null)
                return;

            HidePreview();
            m_PaneWindow.LoadDocument(item.sourceAsset);
        }

        void BindItem(VisualElement element, int index)
        {
            var item = (m_TreeView.viewController as DefaultTreeViewController<BuilderLibraryTreeItem>).GetTreeViewItemDataForIndex(index);
            var builderItem = item.data;

            // Pre-emptive cleanup.
            var row = element.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.SetEnabled(true);

            var listOfOpenDocuments = m_PaneWindow.document.openUXMLFiles;
            bool isCurrentDocumentOpen = listOfOpenDocuments.Any(doc => doc.visualTreeAsset == builderItem.sourceAsset);
            row.EnableInClassList(BuilderConstants.LibraryCurrentlyOpenFileItemClassName, isCurrentDocumentOpen);
            element.SetEnabled(!isCurrentDocumentOpen);

            // Header
            if (builderItem.isHeader)
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);

            var editorOnlyLabel = element.Q<Label>(k_TreeItemEditorOnlyLabelName);
            editorOnlyLabel.text = BuilderConstants.EditorOnlyTag;
            editorOnlyLabel.style.display = builderItem.isEditorOnly ? DisplayStyle.Flex : DisplayStyle.None;

            // Set Icon
            AssignTreeItemIcon(element, builderItem.icon);

            // Set label.
            var label = element.Q<Label>(k_TreeItemLabelName);
            Assert.IsNotNull(label);
            label.text = builderItem.name;

            // Set open button visibility.
            var openButton = element.Q<Button>(k_OpenButtonName);
            openButton.userData = item.data;
            var enableTreeViewItemWithButton = builderItem.sourceAsset != null && builderItem.sourceAsset != m_PaneWindow.document.visualTreeAsset;
            element.EnableInClassList(k_TreeViewItemWithButtonClassName, enableTreeViewItemWithButton);

            LinkToTreeViewItem(element, builderItem);
        }

        void OnItemsChosen(IEnumerable<object> selectedItems)
        {
            var selectedItem = selectedItems.FirstOrDefault();

            var item = selectedItem as BuilderLibraryTreeItem;
            AddItemToTheDocument(item);
        }

        public override void Refresh() => m_TreeView.Rebuild();

        void AssignTreeItemIcon(VisualElement itemRoot, Texture2D icon)
        {
            var iconElement = itemRoot.ElementAt(0);
            if (icon == null)
            {
                iconElement.style.display = DisplayStyle.None;
            }
            else
            {
                iconElement.style.display = DisplayStyle.Flex;
                var styleBackgroundImage = iconElement.style.backgroundImage;
                styleBackgroundImage.value = new Background { texture = icon };
                iconElement.style.backgroundImage = styleBackgroundImage;
            }
        }
    }
}
