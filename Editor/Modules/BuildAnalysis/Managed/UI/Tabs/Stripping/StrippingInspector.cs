// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis;

internal class StrippingInspector : VisualElement
{
    // Share some stylesheets with the asset tab inspector
    private const string k_UssPath = "BuildAnalysis/StyleSheets/AssetInspector.uss";

    private readonly StrippingInspectorHeader m_InspectorHeader;
    private readonly StrippingInspectorBody m_InspectorBody;
    private readonly Label m_CenteredLabel;
    
    internal const string k_NoSelectionText = "Select an item to see more information.";
    internal const string k_NoReasonsText = "No additional information is available for this item.";
    
    public StrippingInspector()
    {
        AddToClassList("inspector");

        styleSheets.Add(EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet);

        m_InspectorHeader = new StrippingInspectorHeader();
        m_InspectorHeader.style.display = DisplayStyle.None;
        
        Add(m_InspectorHeader);
        
        m_CenteredLabel = new Label
        {
            name = "centered-label",
            text = k_NoSelectionText,
        };
        m_CenteredLabel.AddToClassList("stripping-inspector-centered-label");
        Add(m_CenteredLabel);
        
        m_InspectorBody = new StrippingInspectorBody();
        m_InspectorBody.style.display = DisplayStyle.None;
        
        Add(m_InspectorBody);
    }

    public void Bind(StrippingItem strippingItem, IReadOnlyDictionary<string, StrippingItem> strippingItemData)
    {
        m_InspectorHeader.style.display = DisplayStyle.Flex;
        m_InspectorHeader.Bind(strippingItem.Icon, strippingItem.Name); 
        
        m_InspectorBody.style.display = DisplayStyle.None;
        
        if (!strippingItem.HasDataAboutDependents())
        {
            m_CenteredLabel.text = k_NoReasonsText;
            m_CenteredLabel.style.display = DisplayStyle.Flex;
            return;
        }
        
        m_CenteredLabel.style.display = DisplayStyle.None;
        m_InspectorBody.style.display = DisplayStyle.Flex;
        
        m_InspectorBody.Bind(strippingItem, strippingItemData);
    }

    public void Reset()
    {
        m_InspectorBody.style.display = DisplayStyle.None;
        m_InspectorHeader.style.display = DisplayStyle.None;
        m_CenteredLabel.style.display = DisplayStyle.Flex;
        
        m_CenteredLabel.text = k_NoSelectionText;
    }

    public void RegisterRequestSelectionCallback(Action<int> callback)
    {
        m_InspectorBody.RegisterRequestSelectionCallback(callback);
    }
}

internal sealed class StrippingInspectorHeader : VisualElement
{
    private readonly Image m_Icon;
    private readonly Label m_Name;

    public StrippingInspectorHeader()
    {
        AddToClassList("inspector__header");

        m_Icon = new Image { scaleMode = ScaleMode.ScaleToFit };
        m_Icon.AddToClassList("inspector__header-icon");
        Add(m_Icon);

        m_Name = new Label();
        m_Name.AddToClassList("inspector__header-name");
        Add(m_Name);
    }

    public void Bind(Texture icon, string title)
    {
        m_Icon.image = icon;
        m_Name.text = title;
    }
}

internal sealed class StrippingInspectorBody : VisualElement
{
    private readonly TreeView m_ReasonTree;
    private readonly ZebraEmptyBody m_EmptyBody;
    
    private Action<int> m_RequestSelectionCallback;

    public StrippingInspectorBody()
    {
        AddToClassList("inspector__body");

        var label = new Label
        {
            text = "Reasons for inclusion"
        };
        label.AddToClassList("stripping-inspector-reasons-label");
        Add(label);

        m_ReasonTree = new TreeView
        {
            name = "reason-tree-view",
            makeItem = MakeReasonItem,
            bindItem = BindReasonItem,
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            fixedItemHeight = 20,
            selectionType = SelectionType.Single
        };
        m_ReasonTree.itemExpandedChanged += _ => m_EmptyBody?.Refresh(); 
        m_ReasonTree.itemsChosen += OnReasonDoubleClicked;
        
        Add(m_ReasonTree);
        
        m_EmptyBody = new ZebraEmptyBody(m_ReasonTree);
        Add(m_EmptyBody);
    }
    
    private VisualElement MakeReasonItem()
    {
        var rowRoot = new VisualElement();
        rowRoot.AddToClassList("stripping-tab-treeview-row");

        var icon = new Image();
        icon.AddToClassList("stripping-tab-treeview-row-icon");
        
        rowRoot.Add(icon);
        
        var reasonLabel = new Label();
        reasonLabel.name = "Name";
        reasonLabel.AddToClassList("stripping-tab-treeview-row-label");
        
        rowRoot.Add(reasonLabel);
        return rowRoot;
    }

    private void BindReasonItem(VisualElement e, int i)
    {
        var item = m_ReasonTree.GetItemDataForIndex<ReasonItem>(i);
        var label = e.Q<Label>();
        label.text = item.Text;
        label.tooltip = item.Tooltip;

        var icon = e.Q<Image>();
        icon.image = item.Icon;
        
        // TODO: Validate how we want to handle fallback icons
        // Labels are pushed very far to the right if the don't have children or an icon,
        // this helps that but may not be correct if we have items with and without children
        icon.style.display = item.Icon == null ? DisplayStyle.None : DisplayStyle.Flex;
        
        e.SetEnabled(item.Enabled);
    }

    private static void BindModuleDependenciesAsReasons(StrippingItem strippingItem, IReadOnlyDictionary<string, StrippingItem> strippingItemData, HashSet<string> modulesSeen,
        List<TreeViewItemData<ReasonItem>> list, ref int id)
    {
        modulesSeen.Add(strippingItem.Name);
        
        foreach (var moduleDependent in strippingItem.ModuleDependents)
        {
            var tooltip = string.Empty;
            var enabled = true;

            if (modulesSeen.Contains(moduleDependent))
            {
                enabled = false;
                tooltip = $"Dependents of {moduleDependent} are omitted because of a circular dependency.";
            }

            if (!strippingItemData.TryGetValue(moduleDependent, out var dependentItem))
            {
                Debug.LogError($"Unable to get information about engine module {moduleDependent}");
                continue;
            }

            var dependentItems = new List<TreeViewItemData<ReasonItem>>();
            
            if(enabled)
                BindModuleDependenciesAsReasons(dependentItem, strippingItemData, modulesSeen, dependentItems, ref id);
            
            var moduleReasonItem = new ReasonItem
            {
                StrippingItemId = dependentItem.Id,
                Text = $"Required by {dependentItem.Name}",
                Icon = dependentItem.Icon,
                Enabled = enabled,
                Tooltip = tooltip,
            };

            var moduleReasonData = new TreeViewItemData<ReasonItem>(++id, moduleReasonItem, dependentItems);
            list.Add(moduleReasonData);
        }
        
        modulesSeen.Remove(strippingItem.Name);
    }

    public void Bind(StrippingItem strippingItem, IReadOnlyDictionary<string, StrippingItem> strippingItemData)
    {
        var items = new List<TreeViewItemData<ReasonItem>>();

        var id = 0;
        var moduleSeen = new HashSet<string>();
        BindModuleDependenciesAsReasons(strippingItem, strippingItemData, moduleSeen, items, ref id);
        
        foreach (var itemReason in strippingItem.Reasons)
        {
            var reasonItem = new ReasonItem
            {
                Text = itemReason,
                Icon = null,
            };

            var treeData = new TreeViewItemData<ReasonItem>(++id, reasonItem);
            items.Add(treeData);
        }

        m_ReasonTree.SetRootItems(items);
        m_ReasonTree.RefreshItems();
        m_ReasonTree.ClearSelection();
        m_EmptyBody.Refresh();
    }

    private void OnReasonDoubleClicked(IEnumerable<object> items)
    {
        object selectedItem = null;
        foreach (var item in items)
        {
            selectedItem = item;
            break;
        }

        if (selectedItem is not ReasonItem reasonItem) 
            return;
        
        if(reasonItem.StrippingItemId != ReasonItem.KInvalidStrippingItemId)
            m_RequestSelectionCallback?.Invoke(reasonItem.StrippingItemId);
    }

    public void RegisterRequestSelectionCallback(Action<int> callback)
    {
        m_RequestSelectionCallback += callback;
    }
}
