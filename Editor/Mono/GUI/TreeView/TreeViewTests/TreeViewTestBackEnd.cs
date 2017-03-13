// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor.TreeViewExamples
{
    internal class BackendData
    {
        public class Foo
        {
            public Foo(string name, int depth, int id)
            {
                this.name = name;
                this.depth = depth;  this.id = id;
            }

            public string name { get; set; }
            public int id { get; set; }
            public int depth { get; set; }
            public Foo parent { get; set; }
            public List<Foo> children { get; set; }
            public bool hasChildren { get { return children != null && children.Count > 0; } }
        }

        public Foo root { get { return m_Root; } }

        private Foo m_Root;
        public bool m_RecursiveFindParentsBelow = true;
        public int IDCounter { get; private set; }
        private int m_MaxItems = 10000;
        private const int k_MinChildren = 3;
        private const int k_MaxChildren = 15;
        private const float k_ProbOfLastDescendent = 0.5f;
        private const int k_MaxDepth = 12;

        public void GenerateData(int maxNumItems)
        {
            m_MaxItems = maxNumItems;
            IDCounter = 1;
            m_Root = new Foo("Root", 0, 0);
            for (int i = 0; i < 10; ++i)
                AddChildrenRecursive(m_Root, UnityEngine.Random.Range(k_MinChildren, k_MaxChildren), true);
        }

        public Foo Find(int id)
        {
            return FindRecursive(id, m_Root);
        }

        public Foo FindRecursive(int id, Foo parent)
        {
            if (!parent.hasChildren)
                return null;

            foreach (var child in parent.children)
            {
                if (child.id == id)
                    return child;

                var result = FindRecursive(id, child);
                if (result != null)
                    return result;
            }

            return null;
        }

        public HashSet<int> GetParentsBelow(int id)
        {
            Foo searchFromThis = FindItemRecursive(root, id);
            if (searchFromThis != null)
            {
                if (m_RecursiveFindParentsBelow)
                    return GetParentsBelowRecursive(searchFromThis);

                return GetParentsBelowStackBased(searchFromThis);
            }
            return new HashSet<int>();
        }

        private HashSet<int> GetParentsBelowStackBased(Foo searchFromThis)
        {
            Stack<Foo> stack = new Stack<Foo>();
            stack.Push(searchFromThis);

            HashSet<int> parentsBelow = new HashSet<int>();
            while (stack.Count > 0)
            {
                Foo current = stack.Pop();
                if (current.hasChildren)
                {
                    parentsBelow.Add(current.id);
                    foreach (var foo in current.children)
                    {
                        stack.Push(foo);
                    }
                }
            }

            return parentsBelow;
        }

        private HashSet<int> GetParentsBelowRecursive(Foo searchFromThis)
        {
            HashSet<int> result = new HashSet<int>();
            GetParentsBelowRecursive(searchFromThis, result);
            return result;
        }

        private static void GetParentsBelowRecursive(Foo item, HashSet<int> parentIDs)
        {
            if (!item.hasChildren)
                return;
            parentIDs.Add(item.id);
            foreach (var child in item.children)
                GetParentsBelowRecursive(child, parentIDs);
        }

        public void ReparentSelection(Foo parentItem, int insertionIndex, List<Foo> draggedItems)
        {
            // Invalid reparenting input
            if (parentItem == null)
                return;

            // We are moving items so we adjust the insertion index to accomodate that any items above the insertion index is removed before inserting
            if (insertionIndex > 0)
                insertionIndex -= parentItem.children.GetRange(0, insertionIndex).Count(draggedItems.Contains);

            // Remove draggedItems from their parents
            foreach (var draggedItem in draggedItems)
            {
                draggedItem.parent.children.Remove(draggedItem);    // remove from old parent
                draggedItem.parent = parentItem;                    // set new parent
            }

            if (!parentItem.hasChildren)
                parentItem.children = new List<Foo>();
            var newChildren = new List<Foo>(parentItem.children);

            // If insertionIndex is -1 then item was dropped upon the parent: client have to decide where to place the dragged items. We add as the first.
            if (insertionIndex == -1)
                insertionIndex = 0;

            // Insert dragged items under new parent
            newChildren.InsertRange(insertionIndex, draggedItems);
            parentItem.children = newChildren;
        }

        void AddChildrenRecursive(Foo foo, int numChildren, bool force)
        {
            if (IDCounter > m_MaxItems)
                return;

            if (foo.depth >= k_MaxDepth)
                return;

            if (!force && UnityEngine.Random.value < k_ProbOfLastDescendent)
                return;

            if (foo.children == null)
                foo.children = new List<Foo>(numChildren);
            for (int i = 0; i < numChildren; ++i)
            {
                Foo child = new Foo("Tud" + IDCounter, foo.depth + 1, ++IDCounter);
                child.parent = foo;
                foo.children.Add(child);
            }

            if (IDCounter > m_MaxItems)
                return;

            foreach (var child in foo.children)
            {
                AddChildrenRecursive(child, UnityEngine.Random.Range(k_MinChildren, k_MaxChildren), false);
            }
        }

        public static Foo FindItemRecursive(Foo item, int id)
        {
            if (item == null)
                return null;

            if (item.id == id)
                return item;

            if (item.children == null)
                return null;

            foreach (Foo child in item.children)
            {
                Foo result = FindItemRecursive(child, id);
                if (result != null)
                    return result;
            }
            return null;
        }
    }

    internal class TreeViewColumnHeader
    {
        public float[] columnWidths { get; set; }
        public float minColumnWidth { get; set; }
        public float dragWidth { get; set; }
        public Action<int, Rect> columnRenderer { get; set; }

        public TreeViewColumnHeader()
        {
            minColumnWidth = 10;
            dragWidth = 6f;
        }

        public void OnGUI(Rect rect)
        {
            const float dragAreaWidth = 3f;
            float columnPos = rect.x;
            for (int i = 0; i < columnWidths.Length; ++i)
            {
                Rect columnRect = new Rect(columnPos, rect.y, columnWidths[i], rect.height);
                columnPos += columnWidths[i];
                Rect dragRect = new Rect(columnPos - dragWidth / 2, rect.y, dragAreaWidth, rect.height);
                float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
                if (deltaX != 0f)
                {
                    columnWidths[i] += deltaX;
                    columnWidths[i] = Mathf.Max(columnWidths[i], minColumnWidth);
                }

                if (columnRenderer != null)
                    columnRenderer(i, columnRect);

                if (Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);
            }
        }
    }
} // UnityEditor
