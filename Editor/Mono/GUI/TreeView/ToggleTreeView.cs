// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal abstract class ToggleTreeViewItem : TreeViewItem
{
    public abstract bool nodeState { get; set; }
}

internal class ToggleTreeView<T> : TreeView where T : ToggleTreeViewItem, new()
{
    static class Style
    {
        public static GUIContent toggleAll = EditorGUIUtility.TrTextContent("Toggle All");
        public static GUIContent expandAll = EditorGUIUtility.TrTextContent("Expand All");
        public static GUIContent collapseAll = EditorGUIUtility.TrTextContent("Collapse All");
        public static GUIContent toggle = EditorGUIUtility.TrTextContent("", "Maintain Alt/Option key to enable or disable all children");
    }

    SearchField m_SearchField;
    Func<T> m_RebuildRoot;

    public enum Column
    {
        Enabled,
        Name,
    }

    public ToggleTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, Func<T> rebuildRoot)
        : base(state, multiColumnHeader)
    {
        m_RebuildRoot = rebuildRoot;
        columnIndexForTreeFoldouts = 1;
        useScrollView = false;
        multiColumnHeader.canSort = false;
        multiColumnHeader.height = 18f;
        showBorder = true;
        showAlternatingRowBackgrounds = true;

        foldoutOverride = DoFoldoutButtonOverride;

        m_SearchField = new SearchField();
        m_SearchField.downOrUpArrowKeyPressed += SetFocusAndEnsureSelectedItem;

        Reload();
    }

    bool hasNodes => rootItem != null && rootItem.hasChildren;

    public float totalHeightIncludingSearchBarAndBottomBar => totalHeight + 18f + EditorGUI.kSingleLineHeight;

    public override void OnGUI(Rect rect)
    {
        var searchFieldRect = rect;
        searchFieldRect.yMax = searchFieldRect.yMin + 20f;
        EditorGUI.BeginChangeCheck();
        var search = m_SearchField.OnGUI(searchFieldRect, searchString);
        if (EditorGUI.EndChangeCheck())
        {
            bool wasSearching = hasSearch;
            searchString = search;
            if (wasSearching && !hasSearch)
            {
                foreach (var item in GetSelection())
                {
                    FrameItem(item);
                }
            }
        }

        var baseGUIRect = rect;
        baseGUIRect.yMin = searchFieldRect.yMax;
        baseGUIRect.yMax = baseGUIRect.yMin + totalHeight;
        base.OnGUI(EditorGUI.IndentedRect(baseGUIRect));
        if (HasFocus() && Event.current.type == EventType.KeyDown
            && (Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            Event.current.Use();
            var nodes = GetSelection();
            if (nodes.Count > 0)
            {
                var firstNode = (T)FindItem(nodes[0], rootItem);
                bool value = !firstNode.nodeState;
                firstNode.nodeState = value;
                for (var i = 1; i < nodes.Count; i++)
                {
                    var node = (T)FindItem(nodes[i], rootItem);
                    node.nodeState = value;
                }
            }
        }

        var bottomRect = rect;
        bottomRect.yMin = bottomRect.yMax - EditorGUI.kSingleLineHeight + 3f;
        BottomGUI(EditorGUI.IndentedRect(bottomRect));
    }

    protected virtual void BottomGUI(Rect rect)
    {
        using (new EditorGUI.DisabledScope(!hasNodes))
        {
            var buttonRect = rect;
            var buttonSize = EditorStyles.miniButton.CalcSize(Style.toggleAll);
            buttonRect.yMin = buttonRect.yMax - buttonSize.y;
            buttonRect.xMax = buttonRect.xMin + buttonSize.x;
            buttonRect.y += 2f;
            if (GUI.Button(buttonRect, Style.toggleAll, EditorStyles.miniButton))
            {
                ToggleAll();
            }

            // lets make sure expand and collapse buttons are always enabled when there is any node.
            var enabledGUI = GUI.enabled;
            GUI.enabled = hasNodes;
            buttonSize = EditorStyles.miniButton.CalcSize(Style.collapseAll);
            buttonRect.xMax = rect.xMax;
            buttonRect.xMin = buttonRect.xMax - buttonSize.x;
            if (GUI.Button(buttonRect, Style.collapseAll, EditorStyles.miniButton))
            {
                CollapseAll();
            }

            buttonSize = EditorStyles.miniButton.CalcSize(Style.expandAll);
            buttonRect.xMax = buttonRect.xMin - 2f;
            buttonRect.xMin = buttonRect.xMax - buttonSize.x;
            if (GUI.Button(buttonRect, Style.expandAll, EditorStyles.miniButton))
            {
                ExpandAll();
            }

            GUI.enabled = enabledGUI;
        }
    }

    protected virtual void ToggleAll()
    {
        if (!hasNodes) return;
        bool value = !((T)rootItem.children[0]).nodeState;
        PropagateValue((T)rootItem, value);
    }

    protected override TreeViewItem BuildRoot()
    {
        return m_RebuildRoot == null ? new T() { depth = -1, id = 0, displayName = "", children = new List<TreeViewItem>() } : m_RebuildRoot();
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        if (!hasNodes)
        {
            base.RowGUI(args);
            return;
        }

        for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            CellGUI(args.GetCellRect(i), (Column)args.GetColumn(i), (T)args.item, ref args);
        }
    }

    void CellGUI(Rect cellRect, Column column, T node, ref RowGUIArgs args)
    {
        CenterRectUsingSingleLineHeight(ref cellRect);
        switch (column)
        {
            case Column.Enabled:
                EnabledGUI(cellRect, node, ref args);
                break;
            case Column.Name:
                NameGUI(cellRect, node, ref args);
                break;
        }
    }

    protected virtual void EnabledGUI(Rect cellRect, T node, ref RowGUIArgs args)
    {
        using (var change = new EditorGUI.ChangeCheckScope())
        {
            bool isActive = node.nodeState;

            // center the toggle button in the cell
            cellRect.xMin = cellRect.xMin + cellRect.width / 2f - 8f;
            isActive = GUI.Toggle(cellRect, isActive, Style.toggle);
            if (change.changed)
            {
                var selection = GetSelection();
                if (selection.Contains(node.id))
                {
                    var propagate = selection.Count == 1 && Event.current.alt;
                    foreach (var i in selection)
                    {
                        var item = (T)FindItem(i, rootItem);
                        item.nodeState = isActive;
                        if (propagate)
                        {
                            PropagateValue(item, isActive);
                        }
                    }
                }
                else
                {
                    node.nodeState = isActive;
                    if (Event.current.alt)
                    {
                        PropagateValue(node, isActive);
                    }
                }
            }
        }
    }

    protected static void PropagateValue(T node, bool value)
    {
        if (node.children == null)
            return;
        foreach (var treeViewItem in node.children)
        {
            var child = (T)treeViewItem;
            child.nodeState = value;
            PropagateValue(child, value);
        }
    }

    private static bool DoFoldoutButtonOverride(Rect foldoutRect, bool expandedState, GUIStyle foldoutStyle)
    {
        // We need to make sure hierarchyMode is false before drawing the Foldout
        // or the foldout will be offsetted if the TreeView is inside an inspector.
        var hierarchy = EditorGUIUtility.hierarchyMode;
        EditorGUIUtility.hierarchyMode = false;
        // We need to make sure the indentLevel is 0 before drawing the Foldout
        // because we want it to ignore current indent level as it was already calculated by the TreeView.
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        // Using EditorGUI.Foldout to keep the foldout active even when the GUI is disabled.
        var newExpandedValue = EditorGUI.Foldout(foldoutRect, expandedState, GUIContent.none, foldoutStyle);
        // Restore previous values after the Foldout has been draw.
        EditorGUI.indentLevel = indent;
        EditorGUIUtility.hierarchyMode = hierarchy;
        return newExpandedValue;
    }

    protected virtual void NameGUI(Rect position, T node, ref RowGUIArgs args)
    {
        args.rowRect = position;
        base.RowGUI(args);
    }
}
