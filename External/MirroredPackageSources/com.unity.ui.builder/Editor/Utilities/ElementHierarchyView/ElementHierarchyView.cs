using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ElementHierarchyView : VisualElement
    {
        public const string k_PillName = "unity-builder-tree-class-pill";

        public bool hierarchyHasChanged { get; set; }
        public bool hasUnsavedChanges { get; set; }
        public BuilderExplorer.BuilderElementInfoVisibilityState elementInfoVisibilityState { get; set; }

        VisualTreeAsset m_ClassPillTemplate;

        public IList<ITreeViewItem> treeRootItems
        {
            get
            {
                return m_TreeRootItems;
            }
            set {}
        }

        public IEnumerable<ITreeViewItem> treeItems
        {
            get
            {
                return m_TreeView.items;
            }
        }

        IList<ITreeViewItem> m_TreeRootItems;

        TreeView m_TreeView;
        HighlightOverlayPainter m_TreeViewHoverOverlay;

        VisualElement m_Container;

        Action<List<VisualElement>> m_SelectElementCallback;

        List<VisualElement> m_SearchResultsHightlights;
        IPanel m_CurrentPanelDebug;

        BuilderPaneWindow m_PaneWindow;
        VisualElement m_DocumentRootElement;
        BuilderSelection m_Selection;
        BuilderClassDragger m_ClassDragger;
        BuilderExplorerDragger m_ExplorerDragger;
        BuilderElementContextMenu m_ContextMenuManipulator;

        public VisualElement container
        {
            get { return m_Container; }
        }

        public ElementHierarchyView(
            BuilderPaneWindow paneWindow,
            VisualElement documentRootElement,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderExplorerDragger explorerDragger,
            BuilderElementContextMenu contextMenuManipulator,
            Action<List<VisualElement>> selectElementCallback,
            HighlightOverlayPainter highlightOverlayPainter)
        {
            m_PaneWindow = paneWindow;
            m_DocumentRootElement = documentRootElement;
            m_Selection = selection;
            m_ClassDragger = classDragger;
            m_ExplorerDragger = explorerDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            this.focusable = true;

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;
            hasUnsavedChanges = false;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            // HACK: ListView/TreeView need to clear their selections when clicking on nothing.
            this.RegisterCallback<MouseDownEvent>(e =>
            {
                var leafTarget = e.leafTarget as VisualElement;
                if (leafTarget.parent is ScrollView)
                    m_PaneWindow.primarySelection.ClearSelection(null);
            });

            m_TreeViewHoverOverlay = highlightOverlayPainter;

            m_Container = new VisualElement();
            m_Container.name = "explorer-container";
            m_Container.style.flexGrow = 1;
            m_ClassDragger.builderHierarchyRoot = m_Container;
            m_ExplorerDragger.builderHierarchyRoot = m_Container;
            Add(m_Container);

            m_ClassPillTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            // Create TreeView.
            m_TreeRootItems = new List<ITreeViewItem>();
            m_TreeView = new TreeView(m_TreeRootItems, 20, MakeItem, FillItem);
            m_TreeView.selectionType = SelectionType.Multiple;
            m_TreeView.viewDataKey = "unity-builder-explorer-tree";
            m_TreeView.style.flexGrow = 1;
            m_TreeView.onSelectionChange += OnSelectionChange;

            m_TreeView.RegisterCallback<MouseDownEvent>(OnLeakedMouseClick);
            m_Container.Add(m_TreeView);

            m_ContextMenuManipulator.RegisterCallbacksOnTarget(m_Container);
        }

        public void CopyTreeViewItemStates(VisualElementAsset sourceVEA, VisualElementAsset targetVEA)
        {
            ITreeViewItem templateTreeItem = null;
            ITreeViewItem unpackedElementTreeItem = null;

            foreach (TreeViewItem<VisualElement> item in treeItems)
            {
                if (item == null || item.data == null)
                {
                    continue;
                }

                var elementAsset = item.data.GetVisualElementAsset();

                if (elementAsset == sourceVEA)
                {
                    templateTreeItem = item;
                }
                else if (elementAsset == targetVEA)
                {
                    unpackedElementTreeItem = item;
                }
            }

            if (templateTreeItem != null && unpackedElementTreeItem != null)
            {
                m_TreeView.CopyExpandedStates(templateTreeItem, unpackedElementTreeItem);
            }
        }

        void FillItem(VisualElement element, ITreeViewItem item)
        {
            var explorerItem = element as BuilderExplorerItem;
            explorerItem.Clear();

            // Pre-emptive cleanup.
            var row = explorerItem.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerItemHiddenClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerActiveStyleSheetClassName);

            // Get target element (in the document).
            var documentElement = (item as TreeViewItem<VisualElement>).data;
            documentElement.SetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName, explorerItem);
            explorerItem.SetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName, documentElement);
            row.userData = documentElement;

            // If we have a FillItem callback (override), we call it and stop creating the rest of the item.
            var fillItemCallback =
                documentElement.GetProperty(BuilderConstants.ExplorerItemFillItemCallbackVEPropertyName) as Action<VisualElement, ITreeViewItem, BuilderSelection>;
            if (fillItemCallback != null)
            {
                fillItemCallback(explorerItem, item, m_Selection);
                return;
            }

            // Create main label container.
            var labelCont = new VisualElement();
            labelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
            explorerItem.Add(labelCont);

            if (BuilderSharedStyles.IsStyleSheetElement(documentElement))
            {
                var owningUxmlPath = documentElement.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string;
                var isPartOfParentDocument = !string.IsNullOrEmpty(owningUxmlPath);

                var styleSheetAsset = documentElement.GetStyleSheet();
                var styleSheetFileName = AssetDatabase.GetAssetPath(styleSheetAsset);
                var styleSheetAssetName = BuilderAssetUtilities.GetStyleSheetAssetName(styleSheetAsset, hasUnsavedChanges && !isPartOfParentDocument);
                var ssLabel = new Label(styleSheetAssetName);
                ssLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                // Register drag-and-drop events for reparenting.
                m_ExplorerDragger.RegisterCallbacksOnTarget(explorerItem);

                // Allow reparenting.
                explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

                var assetIsActiveStyleSheet = styleSheetAsset == m_PaneWindow.document.activeStyleSheet;
                if (assetIsActiveStyleSheet)
                    row.AddToClassList(BuilderConstants.ExplorerActiveStyleSheetClassName);

                if (isPartOfParentDocument)
                    row.AddToClassList(BuilderConstants.ExplorerItemHiddenClassName);

                // Show name of UXML file that USS file 'belongs' to.
                if (!string.IsNullOrEmpty(owningUxmlPath))
                {
                    var pathStr = Path.GetFileName(owningUxmlPath);
                    var label = new Label(BuilderConstants.TripleSpace + pathStr);
                    label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                    label.AddToClassList(BuilderConstants.ElementTypeClassName);
                    label.AddToClassList("unity-builder-explorer-tree-item-template-path"); // Just make it look a bit shaded.
                    labelCont.Add(label);
                }

                return;
            }
            else if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                var selectorParts = BuilderSharedStyles.GetSelectorParts(documentElement);

                var selectorLabelCont = new VisualElement();
                selectorLabelCont.AddToClassList(BuilderConstants.ExplorerItemSelectorLabelContClassName);
                labelCont.Add(selectorLabelCont);

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                // Register drag-and-drop events for reparenting.
                m_ExplorerDragger.RegisterCallbacksOnTarget(explorerItem);

                foreach (var partStr in selectorParts)
                {
                    if (partStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
                    {
                        m_ClassPillTemplate.CloneTree(selectorLabelCont);
                        var pill = selectorLabelCont.contentContainer.ElementAt(selectorLabelCont.childCount - 1);
                        var pillLabel = pill.Q<Label>("class-name-label");
                        pill.name = k_PillName;
                        pill.AddToClassList("unity-debugger-tree-item-pill");
                        pill.SetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName, partStr);
                        pill.userData = documentElement;

                        // Add ellipsis if the class name is too long.
                        var partStrShortened = BuilderNameUtilities.CapStringLengthAndAddEllipsis(partStr, BuilderConstants.ClassNameInPillMaxLength);

                        if (partStrShortened != partStr)
                        {
                            pillLabel.tooltip = partStr;
                        }

                        pillLabel.text = partStrShortened;

                        // We want class dragger first because it has priority on the pill label when drag starts.
                        m_ClassDragger.RegisterCallbacksOnTarget(pill);
                        m_ExplorerDragger.RegisterCallbacksOnTarget(pill);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorNameSymbol))
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementNameClassName);
                        selectorLabelCont.Add(selectorPartLabel);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorPseudoStateSymbol))
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementPseudoStateClassName);
                        selectorLabelCont.Add(selectorPartLabel);
                    }
                    else if (partStr == BuilderConstants.SingleSpace)
                    {
                        var selectorPartLabel = new Label(BuilderConstants.TripleSpace);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                        selectorLabelCont.Add(selectorPartLabel);
                    }
                    else
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                        selectorLabelCont.Add(selectorPartLabel);
                    }
                }

                // Textfield to rename element in hierarchy.
                var renameField = explorerItem.CreateRenamingTextField(documentElement, null, m_Selection);
                labelCont.Add(renameField);

                // Allow reparenting.
                explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

                // Check if selector element is inside current open StyleSheets
                if (documentElement.IsParentSelector())
                    row.AddToClassList(BuilderConstants.ExplorerItemHiddenClassName);

                return;
            }

            if (BuilderSharedStyles.IsDocumentElement(documentElement))
            {
                var uxmlAsset = documentElement.GetVisualTreeAsset();
                var ssLabel = new Label(BuilderAssetUtilities.GetVisualTreeAssetAssetName(uxmlAsset, hasUnsavedChanges));
                ssLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);

                // Allow reparenting.
                explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                return;
            }

            // Check if element is inside current document.
            if (!documentElement.IsPartOfActiveVisualTreeAsset(m_PaneWindow.document))
                row.AddToClassList(BuilderConstants.ExplorerItemHiddenClassName);

            // Register drag-and-drop events for reparenting.
            m_ExplorerDragger.RegisterCallbacksOnTarget(explorerItem);

            // Allow reparenting.
            explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

            // Element type label.
            if (string.IsNullOrEmpty(documentElement.name) ||
                elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.TypeName))
            {
                var typeLabel = new Label(documentElement.typeName);
                typeLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                typeLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                labelCont.Add(typeLabel);
            }

            // Element name label.
            var nameLabel = new Label();
            nameLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
            nameLabel.AddToClassList("unity-debugger-tree-item-name-label");
            nameLabel.AddToClassList(BuilderConstants.ExplorerItemNameLabelClassName);
            nameLabel.AddToClassList(BuilderConstants.ElementNameClassName);
            if (!string.IsNullOrEmpty(documentElement.name))
                nameLabel.text = BuilderConstants.UssSelectorNameSymbol + documentElement.name;
            labelCont.Add(nameLabel);

            // Textfield to rename element in hierarchy.
            var renameTextfield = explorerItem.CreateRenamingTextField(documentElement, nameLabel, m_Selection);
            labelCont.Add(renameTextfield);

            // Add class list.
            if (documentElement.classList.Count > 0 && elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.ClassList))
            {
                foreach (var ussClass in documentElement.GetClasses())
                {
                    var classLabelCont = new VisualElement();
                    classLabelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
                    explorerItem.Add(classLabelCont);

                    var classLabel = new Label(BuilderConstants.UssSelectorClassNameSymbol + ussClass);
                    classLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                    classLabel.AddToClassList(BuilderConstants.ElementClassNameClassName);
                    classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");
                    classLabelCont.Add(classLabel);
                }
            }

            // Add stylesheets.
            if (elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.StyleSheets))
            {
                var vea = documentElement.GetVisualElementAsset();
                if (vea != null)
                {
                    foreach (var ussPath in vea.GetStyleSheetPaths())
                    {
                        if (string.IsNullOrEmpty(ussPath))
                            continue;

                        var classLabelCont = new VisualElement();
                        classLabelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
                        explorerItem.Add(classLabelCont);

                        var classLabel = new Label(Path.GetFileName(ussPath));
                        classLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        classLabel.AddToClassList(BuilderConstants.ElementAttachedStyleSheetClassName);
                        classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");
                        classLabelCont.Add(classLabel);
                    }
                }
                else
                {
                    for (int i = 0; i < documentElement.styleSheets.count; ++i)
                    {
                        var attachedStyleSheet = documentElement.styleSheets[i];
                        if (attachedStyleSheet == null)
                            continue;

                        var classLabelCont = new VisualElement();
                        classLabelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
                        explorerItem.Add(classLabelCont);

                        var classLabel = new Label(attachedStyleSheet.name + BuilderConstants.UssExtension);
                        classLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        classLabel.AddToClassList(BuilderConstants.ElementAttachedStyleSheetClassName);
                        classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");
                        classLabelCont.Add(classLabel);
                    }
                }
            }

            // Show name of uxml file if this element is a TemplateContainer.
            var path = documentElement.GetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName) as string;
            Texture2D itemIcon;
            if (documentElement is TemplateContainer && !string.IsNullOrEmpty(path))
            {
                var pathStr = Path.GetFileName(path);
                var label = new Label(pathStr);
                label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                label.AddToClassList(BuilderConstants.ElementTypeClassName);
                label.AddToClassList("unity-builder-explorer-tree-item-template-path"); // Just make it look a bit shaded.
                labelCont.Add(label);
                itemIcon = BuilderLibraryContent.GetUXMLAssetIcon(path);
            }
            else
            {
                itemIcon = BuilderLibraryContent.GetTypeLibraryIcon(documentElement.GetType());
            }

            // Element icon.
            var icon = new VisualElement();
            icon.AddToClassList(BuilderConstants.ExplorerItemIconClassName);
            var styleBackgroundImage = icon.style.backgroundImage;
            styleBackgroundImage.value = new Background { texture = itemIcon };
            icon.style.backgroundImage = styleBackgroundImage;
            labelCont.Insert(0, icon);

            // Register right-click events for context menu actions.
            m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);
        }

        void HighlightItemInTargetWindow(VisualElement documentElement)
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.AddOverlay(documentElement);
            var panel = documentElement.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void ClearHighlightOverlay()
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.ClearOverlay();
        }

        public void ResetHighlightOverlays()
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.ClearOverlay();

            if (m_TreeView != null)
            {
                foreach (TreeViewItem<VisualElement> selectedItem in m_TreeView.selectedItems)
                {
                    var documentElement = selectedItem.data;
                    HighlightAllRelatedDocumentElements(documentElement);
                }
            }

            var panel = this.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void RebuildTree(VisualElement rootVisualElement, bool includeParent = true)
        {
            if (!hierarchyHasChanged)
                return;

            // Save focus state.
            bool wasTreeFocused = false;
            if (m_TreeView != null)
                wasTreeFocused = m_TreeView.Q<ListView>().IsFocused();

            m_CurrentPanelDebug = rootVisualElement.panel;

            int nextId = 1;
            if (includeParent)
                m_TreeRootItems = GetTreeItemsFromVisualTreeIncludingParent(rootVisualElement, ref nextId);
            else
                m_TreeRootItems = GetTreeItemsFromVisualTree(rootVisualElement, ref nextId);

            // Clear selection which would otherwise persist via view data persistence.
            if (m_TreeView != null)
            {
                m_TreeView.ClearSelection();
                m_TreeView.rootItems = m_TreeRootItems;

                // Restore focus state.
                if (wasTreeFocused)
                    m_TreeView.Q<ListView>()?.Focus();
            }

            hierarchyHasChanged = false;
        }

        public void ExpandRootItems()
        {
            // Auto-expand root items on load.
            if (m_TreeRootItems != null)
                foreach (var item in m_TreeView.rootItems)
                    m_TreeView.ExpandItem(item.id);
        }

        public void ExpandItem(VisualElement element)
        {
            var item = FindElement(m_TreeRootItems, element);

            if (item != null)
            {
                m_TreeView.ExpandItem(item.id);
            }
        }

        public void ExpandAllItems()
        {
            // Auto-expand all items on load.
            if (m_TreeRootItems != null)
                foreach (var item in TreeView.GetAllItems(m_TreeView.rootItems))
                    m_TreeView.ExpandItem(item.id);
        }

        void OnLeakedMouseClick(MouseDownEvent evt)
        {
            if (!(evt.target is ScrollView))
                return;

            m_TreeView.ClearSelection();
            evt.StopPropagation();
        }

        void OnSelectionChange(IEnumerable<ITreeViewItem> items)
        {
            if (m_SelectElementCallback == null)
                return;

            if (items.Count() == 0)
            {
                m_SelectElementCallback(null);
                return;
            }

            var elements = new List<VisualElement>();
            foreach (TreeViewItem<VisualElement> item in items)
                if (item != null)
                    elements.Add(item.data);

            m_SelectElementCallback(elements);
        }

        void HighlightAllElementsMatchingSelectorElement(VisualElement selectorElement)
        {
            var selector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            if (selector == null)
                return;

            var selectorStr = StyleSheetToUss.ToUssSelector(selector);
            var matchingElements = BuilderSharedStyles.GetMatchingElementsForSelector(m_DocumentRootElement, selectorStr);
            if (matchingElements == null)
                return;

            foreach (var element in matchingElements)
                HighlightItemInTargetWindow(element);
        }

        void HighlightAllRelatedDocumentElements(VisualElement documentElement)
        {
            if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                HighlightAllElementsMatchingSelectorElement(documentElement);
            }
            else
            {
                HighlightItemInTargetWindow(documentElement);
            }
        }

        VisualElement MakeItem()
        {
            var element = new BuilderExplorerItem();
            element.name = "unity-treeview-item-content";
            element.RegisterCallback<MouseEnterEvent>((e) =>
            {
                ClearHighlightOverlay();

                var explorerItem = e.target as VisualElement;
                var documentElement = explorerItem?.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
                HighlightAllRelatedDocumentElements(documentElement);
            });
            element.RegisterCallback<MouseLeaveEvent>((e) =>
            {
                ClearHighlightOverlay();
            });

            element.RegisterCustomBuilderStyleChangeEvent(elementStyle =>
            {
                var documentElement = element.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
                var isValidTarget = documentElement != null;
                if (!isValidTarget)
                    return;

                var icon = element.Q(null, BuilderConstants.ExplorerItemIconClassName);
                if (icon == null)
                    return;

                var path = documentElement.GetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName) as string;
                var libraryIcon = BuilderLibraryContent.GetTypeLibraryIcon(documentElement.GetType());
                if (documentElement is TemplateContainer && !string.IsNullOrEmpty(path))
                {
                    libraryIcon = BuilderLibraryContent.GetUXMLAssetIcon(path);
                }
                else if (elementStyle == BuilderElementStyle.Highlighted && !EditorGUIUtility.isProSkin)
                {
                    libraryIcon = BuilderLibraryContent.GetTypeDarkSkinLibraryIcon(documentElement.GetType());
                }

                var styleBackgroundImage = icon.style.backgroundImage;
                styleBackgroundImage.value = new Background { texture = libraryIcon };
                icon.style.backgroundImage = styleBackgroundImage;
            });

            return element;
        }

        TreeViewItem<VisualElement> FindElement(IEnumerable<ITreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return null;

            foreach (var item in list)
            {
                var treeItem = item as TreeViewItem<VisualElement>;
                if (treeItem.data == element)
                    return treeItem;

                TreeViewItem<VisualElement> itemFoundInChildren = null;
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren != null)
                    return itemFoundInChildren;
            }

            return null;
        }

        public void ClearSelection()
        {
            if (m_TreeView == null)
                return;

            m_TreeView.ClearSelection();
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public void SelectElements(IEnumerable<VisualElement> elements)
        {
            m_TreeView.ClearSelection();

            if (elements == null)
                return;

            foreach (var element in elements)
            {
                var item = FindElement(m_TreeRootItems, element);
                if (item == null)
                    continue;

                m_TreeView.AddToSelection(item.id);
                m_TreeView.ScrollToItem(item.id);
            }
        }

        IList<ITreeViewItem> GetTreeItemsFromVisualTreeIncludingParent(VisualElement parent, ref int nextId)
        {
            if (parent == null)
                return null;

            var items = new List<ITreeViewItem>();
            var id = nextId;
            nextId++;

            var item = new TreeViewItem<VisualElement>(id, parent);
            items.Add(item);

            var childItems = GetTreeItemsFromVisualTree(parent, ref nextId);
            if (childItems == null)
                return items;

            item.AddChildren(childItems);

            return items;
        }

        IList<ITreeViewItem> GetTreeItemsFromVisualTree(VisualElement parent, ref int nextId)
        {
            List<ITreeViewItem> items = null;

            if (parent == null)
                return null;

            int count = parent.hierarchy.childCount;
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var element = parent.hierarchy[i];

                if (element.name == BuilderConstants.SpecialVisualElementInitialMinSizeName)
                    continue;

                if (items == null)
                    items = new List<ITreeViewItem>();

                var id = 0;
                var linkedAsset = element.GetVisualElementAsset();
                if (linkedAsset != null)
                {
                    id = linkedAsset.id;
                }
                else
                {
                    id = nextId;
                    nextId++;
                }

                var item = new TreeViewItem<VisualElement>(id, element);
                items.Add(item);

                var childItems = GetTreeItemsFromVisualTree(element, ref nextId);
                if (childItems == null)
                    continue;

                item.AddChildren(childItems);
            }

            return items;
        }
    }
}
