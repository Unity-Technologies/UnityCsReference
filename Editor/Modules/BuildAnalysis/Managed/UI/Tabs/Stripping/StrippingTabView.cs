// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal partial class StrippingTabView : IBuildAnalysisTabView
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/StrippingTab.uxml";
        private const int k_InspectorPaneIndex = 1;
        
        private readonly VisualElement m_Root = new();
        private VisualElement m_NoSelection;
        private VisualElement m_NoStrippingInfo;
        private VisualElement m_StrippingInfoSection;

        private StrippingInspector m_StrippingInspector;
        private TwoPaneSplitView m_InspectorSplit;
        private VisualElement m_InspectorHost;
        private Label m_NumModulesIncludedLabel;
        
        private bool m_HasLaidOut;
        private bool m_InspectorOpen;

        private BuildHeader m_Header;

        public VisualElement Root => m_Root;

        private TreeView m_TreeView;
        private readonly IStrippingInfoProvider m_StrippingInfoProvider;
        private readonly Dictionary<string, StrippingItem> m_ModuleNameToInfo = new();

        private Texture2D m_ModuleIcon;
        private readonly Dictionary<string, Texture> m_IconCache = new();

        public StrippingTabView(IStrippingInfoProvider strippingInfoProvider)
        {
            m_StrippingInfoProvider = strippingInfoProvider;
        }

        public void Initialize()
        {
            Debug.Assert(m_Root.childCount == 0, "StrippingTabView.Initialize() should only be called once.");
            m_Root.style.flexGrow = 1;

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;

            if (template == null)
                throw new Exception("Unable to load UXML path");
            
            template.CloneTree(m_Root);

            m_NoSelection = m_Root.Q<VisualElement>("no-selection");
            m_NoStrippingInfo = m_Root.Q<VisualElement>("no-stripping-info");
            m_NoStrippingInfo.style.display = DisplayStyle.None;
            m_StrippingInfoSection = m_Root.Q<VisualElement>("stripping-scroll");
            
            m_InspectorSplit = m_Root.Q<TwoPaneSplitView>("stripping-inspector-split");
            m_InspectorHost = m_Root.Q<VisualElement>("stripping-inspector-host");
            m_StrippingInspector = new StrippingInspector();
            m_StrippingInspector.RegisterRequestSelectionCallback(ProcessUpdateSelectionRequest);
            m_InspectorHost.Add(m_StrippingInspector);

            m_Header = m_Root.Q<BuildHeader>("build-header");

            m_NumModulesIncludedLabel = m_Root.Q<Label>("included-modules-label");
            
            m_TreeView = m_Root.Q<TreeView>("stripping-tree-view");
            m_TreeView.makeItem = TreeViewMakeItem;
            m_TreeView.bindItem = BindItem;

            m_TreeView.selectionChanged += OnSelectionChanged;
            
            // Defer first CollapseChild call until after first layout — TwoPaneSplitView
            // throws if collapsed before its initial geometry is computed.
            m_InspectorSplit.RegisterCallback<GeometryChangedEvent>(OnFirstGeometry);

            m_ModuleIcon = (Texture2D)EditorGUIUtility.IconContent("Assembly Icon").image;

            Apply(null);
        }
        
        private static VisualElement TreeViewMakeItem()
        {
            var root = new VisualElement();
            root.name = "stripping-item-row";
            root.AddToClassList("stripping-tab-treeview-row");

            var icon = new Image();
            icon.AddToClassList("stripping-tab-treeview-row-icon");
            root.Add(icon);

            var nameLabel = new Label { name = "Name" };
            nameLabel.AddToClassList("stripping-tab-treeview-row-label");
            root.Add(nameLabel);
            
            return root;
        }

        private void BindItem(VisualElement e, int i)
        {
            var item = m_TreeView.GetItemDataForIndex<StrippingItem>(i);
            var label = e.Q<Label>("Name");
            label.text = item.Name;

            var icon = e.Q<Image>();
            icon.image = item.Icon;

            if (item.Type == StrippingItemType.Type)
            {
                label.tooltip = $"{item.Name} was included because it is used by your project.";
                if (item.Reasons.Count > 0)
                    label.tooltip += " Select to view usage in the Inspector.";
            }
            else if(item.Type == StrippingItemType.Module)
            {
                label.tooltip = $"Click to view reasons why {item.Name} was included";
            }
            else
            {
                label.tooltip = string.Empty;
            }
        }

        private void ProcessUpdateSelectionRequest(int id)
        {
            m_TreeView.ScrollToItemById(id);
            m_TreeView.SetSelectionById(id);
            m_TreeView.schedule.Execute(() => m_TreeView.Focus());
        }

        public void Apply(BuildAnalysisView view)
        {
            m_StrippingInspector.Reset();
            m_ModuleNameToInfo.Clear();

            var hasSelection = view?.Entry != null && view.Analysis != null;
            var couldHaveStrippingInfo = view?.Entry is { BuildResult: BuildResult.Succeeded, BuildType: BuildType.Player };
            
            m_Header.style.display = DisplayStyle.None;
            m_StrippingInfoSection.style.display = DisplayStyle.None;
            m_InspectorSplit.style.display = DisplayStyle.None;
            
            m_NoSelection.style.display = !hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoStrippingInfo.style.display = !couldHaveStrippingInfo && hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection || !couldHaveStrippingInfo)
                return;
            
            BindStrippingInfo(view.Entry);
        }

        private void PopulateTreeViewListForStrippingItem(string item, List<TreeViewItemData<StrippingItem>> list, StrippingItemType itemType, HashSet<string> recursionGuard, ref int id)
        {
            if (!recursionGuard.Add(item))
            {
                Debug.LogWarning($"[Build Analysis] Circular dependency detected for {itemType} '{item}'. Omitted from engine modules stripping tree.");
                return;
            }

            var reasons = new List<string>();
            var moduleDependents = new List<string>();
            var treeViewSubItemsData = new List<TreeViewItemData<StrippingItem>>();
            const string moduleSuffix = " Module";
            foreach (var dependent in m_StrippingInfoProvider.GetReasonsForIncluding(item))
            {
                // This API is a bit funky because it merges "reasons" with dependent types and modules
                // We'll make that decision here, but we should adjust the data to make this clearer in the future
                var isSentence = dependent.Contains(" ");
                if(isSentence || dependent.EndsWith(".unity"))
                {
                    const string requiredByModulePrefix = "Required by ";
                    if (dependent.StartsWith(requiredByModulePrefix) && dependent.EndsWith(moduleSuffix))
                    {
                        var suffixPos = dependent.LastIndexOf(moduleSuffix, StringComparison.Ordinal);
                        moduleDependents.Add(dependent[requiredByModulePrefix.Length..suffixPos]);
                    }
                    else
                        reasons.Add(dependent);
                }
                else
                {
                    PopulateTreeViewListForStrippingItem(dependent, treeViewSubItemsData, StrippingItemType.Type, recursionGuard, ref id);
                }
            }

            var moduleId = ++id;

            Texture icon;
            if (itemType == StrippingItemType.Module)
                icon = m_ModuleIcon;
            else
            {
                if (!m_IconCache.TryGetValue(item, out icon))
                {
                    var type = Type.GetType($"UnityEngine.{item},UnityEngine");
                    if (type != null)
                        icon = EditorGUIUtility.ObjectContent(null, type).image;

                    m_IconCache[item] = icon;
                }
            }

            var name = item;
            if (itemType == StrippingItemType.Module)
                name = item.Replace(moduleSuffix, string.Empty);

            var strippingItem = new StrippingItem()
            {
                Id = moduleId,
                Name = name,
                Icon = icon,
                Reasons = reasons,
                ModuleDependents = moduleDependents,
                Type = itemType,
            };

            if(itemType == StrippingItemType.Module)
                m_ModuleNameToInfo[strippingItem.Name] = strippingItem;
            
            var treeViewItemData = new TreeViewItemData<StrippingItem>(moduleId, strippingItem, treeViewSubItemsData);
            list.Add(treeViewItemData);
            recursionGuard.Remove(item);
        }

        private void BindStrippingInfo(BuildEntry selection)
        {
            m_Header.Bind(selection);
            m_StrippingInfoProvider.Apply(selection);

            // We check if the build _could_ have stripping info in Apply
            // But now we're initialised m_StrippingInfoProvider we'll confirm
            if (!m_StrippingInfoProvider.BuildHasStrippingInfo())
            {
                m_NoStrippingInfo.style.display = DisplayStyle.Flex;
                return;
            }

            m_Header.style.display = DisplayStyle.Flex;
            m_StrippingInfoSection.style.display = DisplayStyle.Flex;
            m_InspectorSplit.style.display = DisplayStyle.Flex;

            var includedModules = new List<string>(m_StrippingInfoProvider.GetIncludedModules());
            var items = new List<TreeViewItemData<StrippingItem>>(includedModules.Count);
            var id = 0;

            var recursionGuard = new HashSet<string>();
            foreach(var module in includedModules)
            {
                PopulateTreeViewListForStrippingItem(module, items, StrippingItemType.Module, recursionGuard, ref id);
            }
            
            m_NumModulesIncludedLabel.text = includedModules.Count == 1 ? "1 module included in build" : $"{includedModules.Count} modules included in build";
            m_TreeView.SetRootItems(items);
            m_TreeView.Rebuild();
            m_TreeView.ClearSelection();
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            object selectedItem = null;
            foreach (var item in selectedItems)
            {
                selectedItem = item;
                break;
            }

            if (selectedItem is not StrippingItem moduleData) 
                return;

            m_StrippingInspector.Bind(moduleData, m_ModuleNameToInfo);
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (!isVisible) 
                return;

            m_TreeView.ClearSelection();
            m_StrippingInspector.Reset();
        }

        public void OnInspectorVisibilityChanged(bool isOpen)
        {
            m_InspectorOpen = isOpen;
            if (!m_HasLaidOut)
                return;

            ApplyInspectorVisibility();
        }

        public bool DoesSupportInspector() => true;

        private void OnFirstGeometry(GeometryChangedEvent evt)
        {
            m_InspectorSplit.UnregisterCallback<GeometryChangedEvent>(OnFirstGeometry);
            m_HasLaidOut = true;
            ApplyInspectorVisibility();
        }

        private void ApplyInspectorVisibility()
        {
            if (m_InspectorOpen)
                m_InspectorSplit.UnCollapse();
            else
                m_InspectorSplit.CollapseChild(k_InspectorPaneIndex);
        }
    }
}
