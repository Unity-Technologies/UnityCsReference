// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

internal abstract class ToggleTreeViewItem : TreeViewItem
{
    public abstract bool nodeState { get; set; }
}

internal class ToggleTreeView<T> : TreeView where T : ToggleTreeViewItem, new()
{
    static class Styles
    {
        public static readonly GUIContent toggleAll = EditorGUIUtility.TrTextContent("Toggle All");
        public static readonly GUIContent expandAll = EditorGUIUtility.TrTextContent("Expand All");
        public static readonly GUIContent collapseAll = EditorGUIUtility.TrTextContent("Collapse All");
        public static readonly GUIContent toggle = EditorGUIUtility.TrTextContent("", "Maintain Alt/Option key to enable or disable all children");
        public static readonly GUIContent filterSelected = new GUIContent(EditorGUIUtility.FindTexture("FilterSelectedOnly"), "Filter selected only");
    }

    static string s_Regex = "(?:(.*) |^)(s:)(false|true)(?: (.*)|$)";

    Func<T> m_RebuildRoot;
    List<TreeViewItem> m_DefaultRows;

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

        Reload();
    }

    bool hasNodes => rootItem != null && rootItem.hasChildren;

    public float totalHeightIncludingSearchBarAndBottomBar => totalHeight + 18f + EditorGUI.kSingleLineHeight;

    protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        // Reuse cached list (for capacity)
        if (m_DefaultRows == null)
            m_DefaultRows = new List<TreeViewItem>(100);
        m_DefaultRows.Clear();

        if (hasSearch)
            SearchFullTree(m_DefaultRows);
        else
            AddExpandedRows(root, m_DefaultRows);
        return m_DefaultRows;
    }

    void SearchFullTree(List<TreeViewItem> rows)
    {
        if (rows == null)
            throw new ArgumentException("Invalid list: cannot be null", nameof(rows));

        var search = searchString;
        bool searchEnabledState = false;
        bool searchedEnabledState = false;
        var match = Regex.Match(search, s_Regex);
        if (match.Success)
        {
            search = match.Groups[1].Value + match.Groups[4].Value;
            searchEnabledState = true;
            searchedEnabledState = match.Groups[3].Value == "true";
        }

        var stack = new Stack<TreeViewItem>();
        stack.Push(rootItem);
        while (stack.Count > 0)
        {
            TreeViewItem current = stack.Pop();
            if (current.children != null)
            {
                foreach (var child in current.children)
                {
                    if (child != null)
                    {
                        if (!searchEnabledState || ((ToggleTreeViewItem)child).nodeState == searchedEnabledState)
                            if (DoesItemMatchSearch(child, search))
                                rows.Add(child);

                        stack.Push(child);
                    }
                }
            }
        }

        rows.Sort((x, y) => EditorUtility.NaturalCompare(x.displayName, y.displayName));
    }

    protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
    {
        return string.IsNullOrEmpty(search) || base.DoesItemMatchSearch(item, search);
    }

    public override void OnGUI(Rect rect)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            DrawSearchField();
            DrawTreeViewGUI();
            BottomGUI();
        }
    }

    void DrawTreeViewGUI()
    {
        base.OnGUI(GUILayoutUtility.GetRect(0, totalHeight));
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
    }

    void DrawSearchField()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.MaxHeight(EditorGUI.kWindowToolbarHeight)))
        {
            EditorGUI.BeginChangeCheck();
            var search = EditorGUILayout.ToolbarSearchField(searchString);
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

            using (new EditorGUI.DisabledScope(Regex.IsMatch(searchString, s_Regex)))
            {
                if (GUILayout.Button(Styles.filterSelected, EditorStyles.miniButton, GUILayout.MaxWidth(24f)))
                    searchString = "s:true " + searchString;
            }
        }
    }

    protected virtual void BottomGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        using (new EditorGUI.DisabledScope(!hasNodes))
        {
            if (GUILayout.Button(Styles.toggleAll, EditorStyles.miniButton))
                ToggleAll();

            var enabledGUI = GUI.enabled;
            GUI.enabled = hasNodes;
            if (GUILayout.Button(Styles.collapseAll, EditorStyles.miniButton))
                CollapseAll();

            if (GUILayout.Button(Styles.expandAll, EditorStyles.miniButton))
                ExpandAll();

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
            isActive = GUI.Toggle(cellRect, isActive, Styles.toggle);
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
