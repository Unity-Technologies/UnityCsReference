// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<UnityEngine.UIElements.VisualElement>;

namespace Unity.UI.Builder
{
    struct PreSaveState
    {
        public List<int> expandedIndices;
        public List<int> selectedIndices;
        public Vector2 scrollOffset;
    }

    internal class ElementHierarchyView : VisualElement
    {
        public const string k_PillName = "unity-builder-tree-class-pill";
        const string k_TreeItemPillClass = "unity-debugger-tree-item-pill";

        public bool hierarchyHasChanged { get; set; }
        public bool hasUnsavedChanges { get; set; }
        public bool hasUssChanges { get; set;  }
        public BuilderExplorer.BuilderElementInfoVisibilityState elementInfoVisibilityState { get; set; }

        VisualTreeAsset m_ClassPillTemplate;

        public IList<TreeViewItem> treeRootItems => m_TreeRootItems;

        public IEnumerable<TreeViewItemData<VisualElement>> treeItems
        {
            get
            {
                foreach (var itemId in m_TreeView.viewController.GetAllItemIds())
                {
                    yield return m_TreeViewController.GetTreeViewItemDataForId(itemId);
                }
            }
        }

        DefaultTreeViewController<VisualElement> m_TreeViewController;


        IList<TreeViewItem> m_TreeRootItems;
        TreeView m_TreeView;
        PreSaveState m_RegisteredState;
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
        bool m_AllowMouseUpRenaming;
        IVisualElementScheduledItem m_RenamingScheduledItem;
        ManipulatorActivationFilter m_RenamingManipulatorFilter;
        List<Label> m_LabelsToResize = new();

        public VisualElement container
        {
            get { return m_Container; }
        }

        internal TreeView treeView => m_TreeView;

        internal string rebuildMarkerName;

        public ElementHierarchyView(
            BuilderPaneWindow paneWindow,
            VisualElement documentRootElement,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderExplorerDragger explorerDragger,
            BuilderElementContextMenu contextMenuManipulator,
            Action<List<VisualElement>> selectElementCallback,
            HighlightOverlayPainter highlightOverlayPainter,
            string profilerMarkerName)
        {
            m_PaneWindow = paneWindow;
            rebuildMarkerName = $"ElementHierarchyView.Rebuild.{profilerMarkerName}";
            m_RebuildMarker = new ProfilerMarker($"ElementHierarchyView.Rebuild.{profilerMarkerName}");
            m_DocumentRootElement = documentRootElement;
            m_Selection = selection;
            m_ClassDragger = classDragger;
            m_ExplorerDragger = explorerDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            this.focusable = true;

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;
            hasUnsavedChanges = true;
            hasUssChanges = false;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            // HACK: ListView/TreeView need to clear their selections when clicking on nothing.
            this.RegisterCallback<MouseDownEvent>(e =>
            {
                var target = e.elementTarget;
                if (target.parent is ScrollView)
                    m_PaneWindow.primarySelection.ClearSelection(null);
            });

            RegisterCallback<GeometryChangedEvent>(e =>
            {
                foreach (var label in m_LabelsToResize)
                {
                    UpdateResizableLabelWidthInSelector(label);
                }
            });

            m_TreeViewHoverOverlay = highlightOverlayPainter;

            m_Container = new VisualElement();
            m_Container.name = "explorer-container";
            m_Container.style.flexGrow = 1;
            m_ClassDragger.builderHierarchyRoot = m_Container;
            m_ExplorerDragger.builderHierarchyRoot = m_Container;
            m_ExplorerDragger.onEndDrag += OnExplorerEndDrag;
            Add(m_Container);

            m_ClassPillTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            // Create TreeView.
            m_TreeRootItems = new List<TreeViewItem>();
            m_TreeView = new TreeView(20, MakeItem, BindItem);
            m_TreeView.SetRootItems(m_TreeRootItems);
            m_TreeView.unbindItem += UnbindItem;
            m_TreeViewController = m_TreeView.viewController as DefaultTreeViewController<VisualElement>;

            m_TreeView.selectionType = SelectionType.Multiple;
            m_TreeView.viewDataKey = "unity-builder-explorer-tree";
            m_TreeView.style.flexGrow = 1;
            m_TreeView.selectedIndicesChanged += OnSelectionChange;
            m_TreeView.selectionNotChanged += OnSameItemSelection;
            m_TreeView.horizontalScrollingEnabled = true;

            m_TreeView.RegisterCallback<MouseDownEvent>(OnLeakedMouseClick);
            m_Container.Add(m_TreeView);

            m_ContextMenuManipulator.RegisterCallbacksOnTarget(m_Container);

            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (selection.selectionCount != 1)
                {
                    return;
                }

                var selectedElement = selection.selection.First();
                var explorerItem = selectedElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;

                if (explorerItem == null)
                {
                    return;
                }

                switch (evt.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (explorerItem.IsRenamingActive())
                            // end renaming and return focus
                            Focus();
                        else if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            explorerItem.ActivateRenameElementMode();
                            evt.StopPropagation();
                        }
                        break;
                    case KeyCode.F2:
                        if (Application.platform != RuntimePlatform.OSXEditor)
                        {
                            explorerItem.ActivateRenameElementMode();
                            evt.StopPropagation();
                        }
                        break;
                    case KeyCode.Escape:
                        if (explorerItem.IsRenamingActive())
                        {
                            if (!explorerItem.IsRenameTextValid())
                            {
                                explorerItem.ResetRenamingField();
                            }

                            Focus();
                        }

                        break;
                }
            }, TrickleDown.TrickleDown);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            var explorerItem = element as BuilderExplorerItem;

            foreach (var label in explorerItem.elidableLabels)
            {
                m_LabelsToResize.Remove(label);
            }

            explorerItem.elidableLabels.Clear();
        }

        private void OnExplorerEndDrag()
        {
            m_AllowMouseUpRenaming = false;
        }

        private void OnSameItemSelection()
        {
            if (m_Selection.isEmpty)
            {
                return;
            }

            var selectedElement = m_Selection.selection.First();
            var explorerItem = selectedElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;

            if (explorerItem != null && !explorerItem.IsRenamingActive() && m_TreeView.IsFocused())
            {
                m_AllowMouseUpRenaming = true;
            }
        }

        public void CopyTreeViewItemStates(VisualElementAsset sourceVEA, VisualElementAsset targetVEA)
        {
            var templateTreeItem = new TreeViewItemData<VisualElement>();
            var unpackedElementTreeItem = new TreeViewItemData<VisualElement>();

            foreach (var item in treeItems)
            {
                if (item.data == null)
                    continue;

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

            if (templateTreeItem.data != null && unpackedElementTreeItem.data != null)
                m_TreeView.CopyExpandedStates(templateTreeItem.id, unpackedElementTreeItem.id);
        }

        void BindItem(VisualElement element, int index)
        {
            var item = m_TreeViewController.GetTreeViewItemDataForIndex(index);
            var explorerItem = element as BuilderExplorerItem;

            explorerItem.SetReorderingZonesEnabled(true);
            explorerItem.Clear();

            // Pre-emptive cleanup.
            var row = explorerItem.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerItemHiddenClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerActiveStyleSheetClassName);
            row.tooltip = string.Empty;

            // Get target element (in the document).
            var documentElement = item.data;
            documentElement.SetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName, explorerItem);
            explorerItem.SetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName, documentElement);
            row.userData = documentElement;

            // If we have a FillItem callback (override), we call it and stop creating the rest of the item.
            var fillItemCallback =
                documentElement.GetProperty(BuilderConstants.ExplorerItemFillItemCallbackVEPropertyName) as Action<VisualElement, TreeViewItem, BuilderSelection>;
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
                var styleSheetAssetName = BuilderAssetUtilities.GetStyleSheetAssetName(styleSheetAsset, hasUnsavedChanges && !isPartOfParentDocument && hasUssChanges);
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
                    Label label;
                    VisualElement pill = null;

                    if (partStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
                    {
                        m_ClassPillTemplate.CloneTree(selectorLabelCont);
                        pill = selectorLabelCont.contentContainer.ElementAt(selectorLabelCont.childCount - 1);
                        label = pill.Q<Label>("class-name-label");
                        pill.name = k_PillName;
                        pill.AddToClassList(k_TreeItemPillClass);
                        pill.SetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName, partStr);
                        pill.userData = documentElement;

                        label.text = partStr;

                        // We want class dragger first because it has priority on the pill label when drag starts.
                        m_ClassDragger.RegisterCallbacksOnTarget(pill);
                        m_ExplorerDragger.RegisterCallbacksOnTarget(pill);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorNameSymbol))
                    {
                        label = new Label(partStr);
                        label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        label.AddToClassList(BuilderConstants.ElementNameClassName);
                        selectorLabelCont.Add(label);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorPseudoStateSymbol))
                    {
                        label = new Label(partStr);
                        label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        label.AddToClassList(BuilderConstants.ElementPseudoStateClassName);
                        selectorLabelCont.Add(label);
                    }
                    else if (partStr == BuilderConstants.SingleSpace)
                    {
                        label = new Label(BuilderConstants.TripleSpace);
                        label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        label.AddToClassList(BuilderConstants.ElementTypeClassName);
                        selectorLabelCont.Add(label);
                    }
                    else
                    {
                        label = new Label(partStr);
                        label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        label.AddToClassList(BuilderConstants.ElementTypeClassName);
                        selectorLabelCont.Add(label);
                    }

                    var shouldElideText = !elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState
                        .FullSelectorText);

                    label.AddToClassList(BuilderConstants.SelectorLabelClassName);

                    if (shouldElideText)
                    {
                        explorerItem.elidableLabels.Add(label);

                        if (selectorParts.Count == 1)
                        {
                            m_LabelsToResize.Add(label);
                        }
                        else
                        {
                            // Label has a max-width
                            label.AddToClassList(BuilderConstants.SelectorLabelMultiplePartsClassName);
                        }

                        label.RegisterCallback<GeometryChangedEvent>(e =>
                        {
                            if (selectorParts.Count == 1)
                            {
                                UpdateResizableLabelWidthInSelector(label);
                            }

                            var fullSelectorText = BuilderSharedStyles.GetSelectorString(documentElement);
                            UpdateTooltips(label, pill, explorerItem, fullSelectorText, partStr);
                        });
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
                var ssLabel = new Label(BuilderAssetUtilities.GetVisualTreeAssetAssetName(uxmlAsset, hasUnsavedChanges && !hasUssChanges));
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
                var uxmlTypeName = documentElement.GetUxmlTypeName();
                var typeLabel = new Label(uxmlTypeName);
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

        private void UpdateTooltips(Label label, VisualElement pill, BuilderExplorerItem explorerItem,
            string fullSelectorText, string selectorPart)
        {
            var tooltipElement = pill ?? label;
            var row = explorerItem.GetFirstAncestorWithClass(BaseTreeView.itemUssClassName);

            tooltipElement.tooltip = label.isElided ? selectorPart : string.Empty;

            if (label.isElided)
            {
                row.tooltip = fullSelectorText;
            }
            else
            {
                row.tooltip = explorerItem.elidableLabels.Any(x => x.isElided) ? fullSelectorText : string.Empty;
            }
        }

        private void UpdateResizableLabelWidthInSelector(Label label)
        {
            var padding = label.resolvedStyle.paddingRight;

            if (label.parent.ClassListContains(k_TreeItemPillClass))
            {
                padding += label.parent.resolvedStyle.paddingRight;
            }

            var size = resolvedStyle.width - label.worldBound.position.x - padding;

            label.style.maxWidth = Mathf.Max(BuilderConstants.ClassNameInPillMinWidth, size);
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
                foreach (var selectedIndex in m_TreeView.selectedIndices)
                {
                    var selectedItem = m_TreeViewController.GetTreeViewItemDataForIndex(selectedIndex);
                    var documentElement = selectedItem.data;
                    HighlightAllRelatedDocumentElements(documentElement);
                }
            }

            var panel = this.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        ProfilerMarker m_RebuildMarker;

        public void RebuildTree(VisualElement rootVisualElement, bool includeParent = true)
        {
            using var marker = m_RebuildMarker.Auto();

            if (!hierarchyHasChanged)
                return;

            // Save focus state.
            bool wasTreeFocused = false;
            if (m_TreeView != null)
                wasTreeFocused = m_TreeView.IsFocused();

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
                m_TreeView.SetRootItems(m_TreeRootItems);
                ApplyRegisteredExpandedIndices();
                m_TreeView.RefreshItems();

                // Restore focus state.
                if (wasTreeFocused)
                    m_TreeView.Focus();
            }

            hierarchyHasChanged = false;
        }

        public void RegisterTreeState()
        {
            if (m_TreeView != null)
            {
                var list = ListPool<int>.Get();
                m_TreeView.viewController.GetExpandedItemIds(list);
                m_RegisteredState.expandedIndices = list.Select(id => m_TreeView.viewController.GetIndexForId(id)).Where(index => index != -1).ToList();
                m_RegisteredState.expandedIndices.Sort();
                m_RegisteredState.selectedIndices = m_TreeView.selectedIndices.ToList();
                m_RegisteredState.scrollOffset = m_TreeView.serializedVirtualizationData.scrollOffset;
                ListPool<int>.Release(list);
            }
        }

        void ApplyRegisteredExpandedIndices()
        {
            if (m_RegisteredState.expandedIndices == null)
                return;

            m_TreeView.CollapseAll();
            foreach (var index in m_RegisteredState.expandedIndices)
            {
                var id = m_TreeView.viewController.GetIdForIndex(index);
                if (id != BaseTreeView.invalidId)
                    m_TreeView.ExpandItem(id);
            }

            m_RegisteredState.expandedIndices.Clear();
            m_RegisteredState.expandedIndices = null;
        }

        public void ApplyRegisteredSelectionInternallyIfNeeded()
        {
            if (m_RegisteredState.selectedIndices == null)
                return;

            m_TreeView.SetSelection(m_RegisteredState.selectedIndices);
            m_TreeView.serializedVirtualizationData.scrollOffset = m_RegisteredState.scrollOffset;
            m_TreeView.RefreshItems();

            m_RegisteredState.selectedIndices.Clear();
            m_RegisteredState.selectedIndices = null;
        }

        public void ExpandRootItems()
        {
            // Auto-expand root items on load.
            if (m_TreeRootItems != null)
                m_TreeView.ExpandRootItems();
        }

        public void ExpandItem(VisualElement element)
        {
            var item = FindElement(m_TreeRootItems, element);

            if (item.data != null)
            {
                m_TreeView.ExpandItem(item.id);
            }
        }

        public void ExpandAllItems()
        {
            // Auto-expand all items on load.
            if (m_TreeRootItems != null)
                m_TreeView.ExpandAll();
        }

        void OnLeakedMouseClick(MouseDownEvent evt)
        {
            if (!(evt.elementTarget is ScrollView))
                return;

            m_TreeView.ClearSelection();
            evt.StopPropagation();
        }

        void OnSelectionChange(IEnumerable<int> itemIndices)
        {
            void ProcessSelectionChange(IEnumerable<int> itemIndices)
            {
                if (m_SelectElementCallback == null)
                    return;

                var enumerable = itemIndices as int[] ?? itemIndices.ToArray();
                if (enumerable.Length == 0)
                {
                    m_SelectElementCallback(null);
                    return;
                }

                using (ListPool<VisualElement>.Get(out var elements))
                {
                    foreach (var index in enumerable)
                    {
                        var item = m_TreeViewController.GetTreeViewItemDataForIndex(index);
                        if (item.data != null)
                            elements.Add(item.data);
                    }

                    m_SelectElementCallback(elements);
                }
            }

            if (focusController is { focusedElement: DimensionStyleField })
            {
                schedule.Execute(() =>
                {
                    ProcessSelectionChange(itemIndices);
                });
            }
            else
                ProcessSelectionChange(itemIndices);
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

                var explorerItem = e.elementTarget;
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

            element.RegisterCallback<ClickEvent>(evt =>
            {
                if (!m_RenamingManipulatorFilter.Matches(evt))
                {
                    return;
                }

                if (evt.clickCount > 1)
                {
                    // Multiple clicks. Cancel rename
                    m_RenamingScheduledItem?.Pause();
                }

                if (evt.clickCount == 1 && m_AllowMouseUpRenaming)
                {
                    m_RenamingScheduledItem = element.schedule.Execute(() =>
                    {
                        if (m_Selection.selectionCount != 1 || !m_TreeView.IsFocused())
                        {
                            return;
                        }

                        var selectedElement = m_Selection.selection.First();

                        if (!selectedElement.HasProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName))
                        {
                            return;
                        }

                        var explorerItem = selectedElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;

                        if (explorerItem == element)
                        {
                            element.ActivateRenameElementMode();
                        }
                    }).StartingIn(500);
                    evt.StopPropagation();
                }

                m_AllowMouseUpRenaming = false;

            }, TrickleDown.TrickleDown);

            return element;
        }

        public TreeViewItem FindElement(IEnumerable<TreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return default;

            foreach (var item in list)
            {
                var treeItem = item;
                if (treeItem.data == element)
                    return treeItem;

                var itemFoundInChildren = new TreeViewItemData<VisualElement>();
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren.data != null)
                    return itemFoundInChildren;
            }

            return default;
        }

        // Used in tests
        internal bool IsRenamingScheduled()
        {
            return m_RenamingScheduledItem != null && m_RenamingScheduledItem.isActive;
        }

        public void ClearSelection()
        {
            m_TreeView?.ClearSelection();
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public int GetSelectedItemId()
        {
            return m_TreeView.GetIdForIndex(m_TreeView.selectedIndex);
        }

        public void SelectItemById(int id)
        {
            m_TreeView.SetSelectionById(id);
        }

        public void SelectElements(IEnumerable<VisualElement> elements)
        {
            m_TreeView.ClearSelection();

            if (elements == null)
                return;

            foreach (var element in elements)
            {
                var item = FindElement(m_TreeRootItems, element);
                if (item.data == null)
                    continue;

                m_TreeView.AddToSelectionById(item.id);
                m_TreeView.ScrollToItemById(item.id);
            }
        }

        IList<TreeViewItem> GetTreeItemsFromVisualTreeIncludingParent(VisualElement parent, ref int nextId)
        {
            if (parent == null)
                return null;

            var items = new List<TreeViewItem>();
            var id = nextId;
            nextId++;

            var item = new TreeViewItem(id, parent);
            items.Add(item);

            var childItems = GetTreeItemsFromVisualTree(parent, ref nextId);
            if (childItems == null)
                return items;

            item.AddChildren(childItems);

            return items;
        }

        IList<TreeViewItem> GetTreeItemsFromVisualTree(VisualElement parent, ref int nextId)
        {
            List<TreeViewItem> items = null;

            if (parent == null)
                return null;

            int count = parent.hierarchy.childCount;
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var element = parent.hierarchy[i];

                if (items == null)
                    items = new List<TreeViewItem>();

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

                var item = new TreeViewItem(id, element);
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
