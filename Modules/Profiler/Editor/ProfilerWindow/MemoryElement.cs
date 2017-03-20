// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class ObjectInfo
    {
        public int    instanceId;
        public long   memorySize;
        public int    reason;
        public List<ObjectInfo> referencedBy;
        public string name;
        public string className;
    }

    [System.Serializable]
    internal class MemoryElement
    {
        public List<MemoryElement> children = null;
        public MemoryElement parent = null;
        public ObjectInfo memoryInfo;

        public long totalMemory;
        public int totalChildCount;
        public string name;
        public bool expanded;
        public string description;

        public MemoryElement()
        {
            children = new List<MemoryElement>();
        }

        public MemoryElement(string n)
        {
            expanded = false;
            name = n;
            children = new List<MemoryElement>();
            description = "";
        }

        public MemoryElement(ObjectInfo memInfo, bool finalize)
        {
            expanded = false;
            memoryInfo = memInfo;
            name = memoryInfo.name;
            totalMemory = memInfo != null ? memInfo.memorySize : 0;
            totalChildCount = 1;
            if (finalize)
                children = new List<MemoryElement>();
        }

        public MemoryElement(string n, List<MemoryElement> groups)
        {
            name = n;
            expanded = false;
            description = "";
            totalMemory = 0;
            totalChildCount = 0;
            children = new List<MemoryElement>();
            foreach (MemoryElement group in groups)
            {
                AddChild(group);
            }
        }

        public void ExpandChildren()
        {
            if (children != null)
                return;
            children = new List<MemoryElement>();
            for (int i = 0; i < ReferenceCount(); i++)
                AddChild(new MemoryElement(memoryInfo.referencedBy[i], false));
        }

        public int AccumulatedChildCount()
        {
            return totalChildCount;
        }

        public int ChildCount()
        {
            if (children != null)
                return children.Count;
            return ReferenceCount();
        }

        public int ReferenceCount()
        {
            return memoryInfo != null && memoryInfo.referencedBy != null ? memoryInfo.referencedBy.Count : 0;
        }

        public void AddChild(MemoryElement node)
        {
            if (node == this)
            {
                throw new System.Exception("Should not AddChild to itself");
            }
            children.Add(node);
            node.parent = this;
            totalMemory += node.totalMemory;
            totalChildCount += node.totalChildCount;
        }

        public int GetChildIndexInList()
        {
            for (int i = 0; i < parent.children.Count; i++)
            {
                if (parent.children[i] == this)
                    return i;
            }
            return parent.children.Count;
        }

        public MemoryElement GetPrevNode()
        {
            int siblingindex = GetChildIndexInList() - 1;
            if (siblingindex >= 0)
            {
                MemoryElement prev = parent.children[siblingindex];
                while (prev.expanded)
                {
                    prev = prev.children[prev.children.Count - 1];
                }
                return prev;
            }
            else
                return parent;
        }

        public MemoryElement GetNextNode()
        {
            if (expanded && children.Count > 0)
                return children[0];

            int nextsiblingindex = GetChildIndexInList() + 1;
            if (nextsiblingindex  < parent.children.Count)
                return parent.children[nextsiblingindex];
            MemoryElement p = parent;
            while (p.parent != null)
            {
                int parentsiblingindex = p.GetChildIndexInList() + 1;
                if (parentsiblingindex < p.parent.children.Count)
                    return p.parent.children[parentsiblingindex];
                p = p.parent;
            }
            return null;
        }

        public MemoryElement GetRoot()
        {
            if (parent != null)
                return parent.GetRoot();
            return this;
        }

        public MemoryElement FirstChild()
        {
            return children[0];
        }

        public MemoryElement LastChild()
        {
            if (!expanded)
                return this;
            return children[children.Count - 1].LastChild();
        }
    }

    [System.Serializable]
    class MemoryElementSelection
    {
        private MemoryElement m_Selected = null;

        public void SetSelection(MemoryElement node)
        {
            m_Selected = node;

            MemoryElement parent = node.parent;
            while (parent != null)
            {
                parent.expanded = true;
                parent = parent.parent;
            }
        }

        public void ClearSelection()
        {
            m_Selected = null;
        }

        public bool isSelected(MemoryElement node)
        {
            return (m_Selected == node);
        }

        public MemoryElement Selected
        {
            get { return m_Selected; }
        }

        public void MoveUp()
        {
            if (m_Selected == null)
                return;
            if (m_Selected.parent == null)
                return;
            MemoryElement prev = m_Selected.GetPrevNode();
            if (prev.parent != null)
                SetSelection(prev);
            else
                SetSelection(prev.FirstChild());
        }

        public void MoveDown()
        {
            if (m_Selected == null)
                return;
            if (m_Selected.parent == null)
                return;
            MemoryElement next = m_Selected.GetNextNode();
            if (next != null)
                SetSelection(next);
        }

        public void MoveFirst()
        {
            if (m_Selected == null)
                return;
            if (m_Selected.parent == null)
                return;
            SetSelection(m_Selected.GetRoot().FirstChild());
        }

        public void MoveLast()
        {
            if (m_Selected == null)
                return;
            if (m_Selected.parent == null)
                return;
            SetSelection(m_Selected.GetRoot().LastChild());
        }

        public void MoveParent()
        {
            if (m_Selected == null)
                return;
            if (m_Selected.parent == null)
                return;
            if (m_Selected.parent.parent == null)
                return;
            SetSelection(m_Selected.parent);
        }
    }
}
