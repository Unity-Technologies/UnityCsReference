using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement
    {
        /// <summary>
        ///  Access to this element physical hierarchy
        ///
        /// </summary>
        public Hierarchy hierarchy
        {
            get; private set;
        }

        [Obsolete("VisualElement.cacheAsBitmap is deprecated and has no effect")]
        public bool cacheAsBitmap { get; set; }

        internal bool ShouldClip()
        {
            return computedStyle.overflow.value != OverflowInternal.Visible;
        }

        // parent in visual tree
        private VisualElement m_PhysicalParent;
        private VisualElement m_LogicalParent;


        public VisualElement parent
        {
            get
            {
                return m_LogicalParent;
            }
        }

        static readonly List<VisualElement> s_EmptyList = new List<VisualElement>();
        private List<VisualElement> m_Children;

        // each element has a ref to the root panel for internal bookkeeping
        // this will be null until a visual tree is added to a panel
        internal BaseVisualElementPanel elementPanel { get; private set; }

        public IPanel panel { get { return elementPanel; } }

        // Logical container where child elements are added.
        // usually same as this element, but can be overridden by more complex types
        // see ScrollView.contentContainer for an example
        /// <summary>
        ///  child elements are added to this element, usually this
        ///
        /// </summary>
        public virtual VisualElement contentContainer
        {
            get { return this; }
        }

        // IVisualElementHierarchy container
        /// <summary>
        /// Add an element to this element's contentContainer
        /// </summary>
        public void Add(VisualElement child)
        {
            if (child == null)
            {
                return;
            }

            var container = contentContainer;

            if (container == null)
            {
                throw new InvalidOperationException("You can't add directly to this VisualElement. Use hierarchy.Add() if you know what you're doing.");
            }
            else if (container == this)
            {
                hierarchy.Add(child);
            }
            else
            {
                container?.Add(child);
            }

            child.m_LogicalParent = this;
        }

        /// <summary>
        /// Insert an element into this element's contentContainer
        /// </summary>
        public void Insert(int index, VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (contentContainer == this)
            {
                hierarchy.Insert(index, element);
            }
            else
            {
                contentContainer?.Insert(index, element);
            }

            element.m_LogicalParent = this;
        }

        /// <summary>
        /// Removes this child from the hierarchy
        /// </summary>
        public void Remove(VisualElement element)
        {
            if (contentContainer == this)
            {
                hierarchy.Remove(element);
            }
            else
            {
                contentContainer?.Remove(element);
            }
        }

        /// <summary>
        /// Remove the child element located at this position from this element's contentContainer
        /// </summary>
        public void RemoveAt(int index)
        {
            if (contentContainer == this)
            {
                hierarchy.RemoveAt(index);
            }
            else
            {
                contentContainer?.RemoveAt(index);
            }
        }

        /// <summary>
        /// Remove all child elements from this element's contentContainer
        /// </summary>
        public void Clear()
        {
            if (contentContainer == this)
            {
                hierarchy.Clear();
            }
            else
            {
                contentContainer?.Clear();
            }
        }

        /// <summary>
        /// Retrieves the child element at position
        /// </summary>
        public VisualElement ElementAt(int index)
        {
            return this[index];
        }

        public VisualElement this[int key]
        {
            get
            {
                if (contentContainer == this)
                {
                    return hierarchy[key];
                }

                return contentContainer ? [key];
            }
        }

        /// <summary>
        ///  Number of child elements in this object's contentContainer
        ///
        /// </summary>
        public int childCount
        {
            get
            {
                if (contentContainer == this)
                {
                    return hierarchy.childCount;
                }
                return contentContainer?.childCount ?? 0;
            }
        }

        /// <summary>
        /// Retrieves the child index of the specified VisualElement.
        /// </summary>
        /// <param name="element">The child to return the index for.</param>
        /// <returns>The index of the child, or -1 if the child is not found.</returns>
        public int IndexOf(VisualElement element)
        {
            if (contentContainer == this)
            {
                return hierarchy.IndexOf(element);
            }
            return contentContainer?.IndexOf(element) ?? -1;
        }

        /// <summary>
        /// Returns the elements from its contentContainer
        /// </summary>
        public IEnumerable<VisualElement> Children()
        {
            if (contentContainer == this)
            {
                return hierarchy.Children();
            }

            return contentContainer?.Children() ?? s_EmptyList;
        }

        /// <summary>
        /// Reorders child elements from this VisualElement contentContainer.
        /// </summary>
        /// <param name="comp">Sorting criteria.</param>
        public void Sort(Comparison<VisualElement> comp)
        {
            if (contentContainer == this)
            {
                hierarchy.Sort(comp);
            }
            else
            {
                contentContainer?.Sort(comp);
            }
        }

        /// <summary>
        /// Brings this element to the end of its parent children list. The element will be visually in front of any overlapping sibling elements.
        /// </summary>
        public void BringToFront()
        {
            if (hierarchy.parent == null)
                return;

            hierarchy.parent.hierarchy.BringToFront(this);
        }

        /// <summary>
        /// Sends this element to the beginning of its parent children list. The element will be visually behind any overlapping sibling elements.
        /// </summary>
        public void SendToBack()
        {
            if (hierarchy.parent == null)
                return;

            hierarchy.parent.hierarchy.SendToBack(this);
        }

        /// <summary>
        /// Places this element right before the sibling element in their parent children list. If the element and the sibling position overlap, the element will be visually behind of its sibling.
        /// </summary>
        /// <param name="sibling">The sibling element.</param>
        /// <remarks>
        /// The elements must be siblings.
        /// </remarks>
        public void PlaceBehind(VisualElement sibling)
        {
            if (sibling == null)
            {
                throw new ArgumentNullException(nameof(sibling));
            }

            if (hierarchy.parent == null || sibling.hierarchy.parent != hierarchy.parent)
            {
                throw new ArgumentException("VisualElements are not siblings");
            }

            hierarchy.parent.hierarchy.PlaceBehind(this, sibling);
        }

        /// <summary>
        /// Places this element right after the sibling element in their parent children list. If the element and the sibling position overlap, the element will be visually in front of its sibling.
        /// </summary>
        /// <param name="sibling">The sibling element.</param>
        /// <remarks>
        /// The elements must be siblings.
        /// </remarks>
        public void PlaceInFront(VisualElement sibling)
        {
            if (sibling == null)
            {
                throw new ArgumentNullException(nameof(sibling));
            }

            if (hierarchy.parent == null || sibling.hierarchy.parent != hierarchy.parent)
            {
                throw new ArgumentException("VisualElements are not siblings");
            }

            hierarchy.parent.hierarchy.PlaceInFront(this, sibling);
        }

        /// <summary>
        /// Hierarchy is a struct allowing access to the hierarchy of visual elements
        /// </summary>
        public struct Hierarchy
        {
            private readonly VisualElement m_Owner;

            /// <summary>
            ///  Access the physical parent of this element in the hierarchy
            ///
            /// </summary>
            public VisualElement parent
            {
                get { return m_Owner.m_PhysicalParent; }
            }

            internal Hierarchy(VisualElement element)
            {
                m_Owner = element;
            }

            /// <summary>
            /// Add an element to this element's contentContainer
            /// </summary>
            public void Add(VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot add null child");

                Insert(childCount, child);
            }

            /// <summary>
            /// Insert an element into this element's contentContainer
            /// </summary>
            public void Insert(int index, VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot insert null child");

                if (index > childCount)
                    throw new ArgumentOutOfRangeException("Index out of range: " + index);

                if (child == m_Owner)
                    throw new ArgumentException("Cannot insert element as its own child");

                child.RemoveFromHierarchy();

                if (ReferenceEquals(m_Owner.m_Children, s_EmptyList))
                {
                    //TODO: Trigger a release on finalizer or something, this means we'll need to make the pool thread-safe as well
                    m_Owner.m_Children = VisualElementListPool.Get();
                }

                if (m_Owner.yogaNode.IsMeasureDefined)
                {
                    m_Owner.RemoveMeasureFunction();
                }

                PutChildAtIndex(child, index);

                int imguiContainerCount = child.imguiContainerDescendantCount + (child.isIMGUIContainer ? 1 : 0);
                if (imguiContainerCount > 0)
                {
                    m_Owner.ChangeIMGUIContainerCount(imguiContainerCount);
                }

                child.hierarchy.SetParent(m_Owner);
                child.PropagateEnabledToChildren(m_Owner.enabledInHierarchy);

                child.InvokeHierarchyChanged(HierarchyChangeType.Add);
                child.IncrementVersion(VersionChangeType.Hierarchy);
                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            /// <summary>
            /// Removes this child from the hierarchy
            /// </summary>
            public void Remove(VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot remove null child");

                if (child.hierarchy.parent != m_Owner)
                    throw new ArgumentException("This visualElement is not my child");

                int index = m_Owner.m_Children.IndexOf(child);
                RemoveAt(index);
            }

            /// <summary>
            /// Remove the child element located at this position from this element's contentContainer
            /// </summary>
            public void RemoveAt(int index)
            {
                if (index < 0 || index >= childCount)
                    throw new ArgumentOutOfRangeException("Index out of range: " + index);

                var child = m_Owner.m_Children[index];
                child.InvokeHierarchyChanged(HierarchyChangeType.Remove);
                RemoveChildAtIndex(index);

                int imguiContainerCount = child.imguiContainerDescendantCount + (child.isIMGUIContainer ? 1 : 0);
                if (imguiContainerCount > 0)
                {
                    m_Owner.ChangeIMGUIContainerCount(-imguiContainerCount);
                }

                child.hierarchy.SetParent(null);

                if (childCount == 0)
                {
                    ReleaseChildList();

                    m_Owner.AssignMeasureFunction();
                }

                // Child is detached from the panel, notify using the panel directly.
                m_Owner.elementPanel?.OnVersionChanged(child, VersionChangeType.Hierarchy);
                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            /// <summary>
            /// Remove all child elements from this element's contentContainer
            /// </summary>
            public void Clear()
            {
                if (childCount > 0)
                {
                    // Copy children to a temporary list because removing child elements from
                    // the panel may trigger modifications (DetachFromPanelEvent callback)
                    // of the same list while we are in the foreach loop.
                    var elements = VisualElementListPool.Copy(m_Owner.m_Children);

                    ReleaseChildList();
                    m_Owner.yogaNode.Clear();

                    m_Owner.AssignMeasureFunction();

                    foreach (VisualElement e in elements)
                    {
                        e.InvokeHierarchyChanged(HierarchyChangeType.Remove);
                        e.hierarchy.SetParent(null);
                        e.m_LogicalParent = null;
                        m_Owner.elementPanel?.OnVersionChanged(e, VersionChangeType.Hierarchy);
                    }

                    if (m_Owner.imguiContainerDescendantCount > 0)
                    {
                        int totalChange = m_Owner.imguiContainerDescendantCount;

                        if (m_Owner.isIMGUIContainer)
                        {
                            totalChange--;
                        }

                        m_Owner.ChangeIMGUIContainerCount(-totalChange);
                    }
                    VisualElementListPool.Release(elements);

                    m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
                }
            }

            internal void BringToFront(VisualElement child)
            {
                if (childCount > 1)
                {
                    int index = m_Owner.m_Children.IndexOf(child);

                    if (index >= 0 && index < childCount - 1)
                    {
                        MoveChildElement(child, index, childCount);
                    }
                }
            }

            internal void SendToBack(VisualElement child)
            {
                if (childCount > 1)
                {
                    int index = m_Owner.m_Children.IndexOf(child);

                    if (index > 0)
                    {
                        MoveChildElement(child, index, 0);
                    }
                }
            }

            internal void PlaceBehind(VisualElement child, VisualElement over)
            {
                if (childCount > 0)
                {
                    int currenIndex = m_Owner.m_Children.IndexOf(child);
                    if (currenIndex < 0)
                        return;

                    int nextIndex = m_Owner.m_Children.IndexOf(over);
                    if (nextIndex > 0 && currenIndex < nextIndex)
                    {
                        nextIndex--;
                    }

                    MoveChildElement(child, currenIndex, nextIndex);
                }
            }

            internal void PlaceInFront(VisualElement child, VisualElement under)
            {
                if (childCount > 0)
                {
                    int currentIndex = m_Owner.m_Children.IndexOf(child);
                    if (currentIndex < 0)
                        return;

                    int nextIndex = m_Owner.m_Children.IndexOf(under);
                    if (currentIndex > nextIndex)
                    {
                        nextIndex++;
                    }

                    MoveChildElement(child, currentIndex, nextIndex);
                }
            }

            private void MoveChildElement(VisualElement child, int currentIndex, int nextIndex)
            {
                child.InvokeHierarchyChanged(HierarchyChangeType.Remove);
                RemoveChildAtIndex(currentIndex);
                PutChildAtIndex(child, nextIndex);
                child.InvokeHierarchyChanged(HierarchyChangeType.Add);

                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            /// <summary>
            ///  Number of child elements in this object's contentContainer
            ///
            /// </summary>
            public int childCount
            {
                get
                {
                    return m_Owner.m_Children.Count;
                }
            }

            public VisualElement this[int key]
            {
                get
                {
                    return m_Owner.m_Children[key];
                }
            }

            /// <summary>
            /// Retrieves the index of the specified VisualElement in the Hierarchy.
            /// </summary>
            /// <param name="element">The element to return the index for.</param>
            /// <returns>The index of the element, or -1 if the element is not found.</returns>
            public int IndexOf(VisualElement element)
            {
                return m_Owner.m_Children.IndexOf(element);
            }

            /// <summary>
            /// Retrieves the child element at position
            /// </summary>
            public VisualElement ElementAt(int index)
            {
                return this[index];
            }

            /// <summary>
            /// Returns the elements from its contentContainer
            /// </summary>
            public IEnumerable<VisualElement> Children()
            {
                return m_Owner.m_Children;
            }

            private void SetParent(VisualElement value)
            {
                m_Owner.m_PhysicalParent = value;
                m_Owner.m_LogicalParent = value;
                if (value != null)
                {
                    m_Owner.SetPanel(m_Owner.m_PhysicalParent.elementPanel);
                }
                else
                {
                    m_Owner.SetPanel(null);
                }
            }

            /// <summary>
            /// Reorders child elements from this VisualElement contentContainer.
            /// </summary>
            /// <param name="comp">Sorting criteria.</param>
            public void Sort(Comparison<VisualElement> comp)
            {
                if (childCount > 0)
                {
                    m_Owner.m_Children.Sort(comp);

                    m_Owner.yogaNode.Clear();
                    for (int i = 0; i < m_Owner.m_Children.Count; i++)
                    {
                        m_Owner.yogaNode.Insert(i, m_Owner.m_Children[i].yogaNode);
                    }
                    m_Owner.InvokeHierarchyChanged(HierarchyChangeType.Move);
                    m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
                }
            }

            // manipulates the children list (without sending events or dirty flags)
            private void PutChildAtIndex(VisualElement child, int index)
            {
                if (index >= childCount)
                {
                    m_Owner.m_Children.Add(child);
                    m_Owner.yogaNode.Insert(m_Owner.yogaNode.Count, child.yogaNode);
                }
                else
                {
                    m_Owner.m_Children.Insert(index, child);
                    m_Owner.yogaNode.Insert(index, child.yogaNode);
                }
            }

            // manipulates the children list (without sending events or dirty flags)
            private void RemoveChildAtIndex(int index)
            {
                m_Owner.m_Children.RemoveAt(index);
                m_Owner.yogaNode.RemoveAt(index);
            }

            private void ReleaseChildList()
            {
                if (!ReferenceEquals(m_Owner.m_Children, s_EmptyList))
                {
                    var children = m_Owner.m_Children;
                    m_Owner.m_Children = s_EmptyList;
                    VisualElementListPool.Release(children);
                }
            }

            public bool Equals(Hierarchy other)
            {
                return other == this;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Hierarchy && Equals((Hierarchy)obj);
            }

            public override int GetHashCode()
            {
                return (m_Owner != null ? m_Owner.GetHashCode() : 0);
            }

            public static bool operator==(Hierarchy x, Hierarchy y)
            {
                return ReferenceEquals(x.m_Owner, y.m_Owner);
            }

            public static bool operator!=(Hierarchy x, Hierarchy y)
            {
                return !(x == y);
            }
        }

        /// <summary>
        /// Removes this element from its parent hierarchy
        /// </summary>
        /// <summary>
        /// Will remove this element from its hierarchy
        /// </summary>
        public void RemoveFromHierarchy()
        {
            if (hierarchy.parent != null)
            {
                hierarchy.parent.hierarchy.Remove(this);
            }
        }

        /// <summary>
        /// Walks up the hierarchy, starting from this element, and returns the first VisualElement of this type
        /// </summary>
        public T GetFirstOfType<T>() where T : class
        {
            T casted = this as T;
            if (casted != null)
                return casted;
            return GetFirstAncestorOfType<T>();
        }

        /// <summary>
        /// Walks up the hierarchy, starting from this element's parent, and returns the first VisualElement of this type
        /// </summary>
        public T GetFirstAncestorOfType<T>() where T : class
        {
            VisualElement ancestor = hierarchy.parent;
            while (ancestor != null)
            {
                T castedAncestor = ancestor as T;
                if (castedAncestor != null)
                {
                    return castedAncestor;
                }
                ancestor = ancestor.hierarchy.parent;
            }
            return null;
        }

        public bool Contains(VisualElement child)
        {
            while (child != null)
            {
                if (child.hierarchy.parent == this)
                {
                    return true;
                }

                child = child.hierarchy.parent;
            }

            return false;
        }

        private void GatherAllChildren(List<VisualElement> elements)
        {
            if (m_Children.Count > 0)
            {
                int startIndex = elements.Count;
                elements.AddRange(m_Children);

                while (startIndex < elements.Count)
                {
                    var current = elements[startIndex];
                    elements.AddRange(current.m_Children);
                    ++startIndex;
                }
            }
        }

        /// <summary>
        /// Finds the lowest commont ancestor between two VisualElements inside the VisualTree hierarchy
        /// </summary>
        public VisualElement FindCommonAncestor(VisualElement other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (panel != other.panel)
            {
                return null;
            }

            // We compute the depth of the 2 elements
            VisualElement thisSide = this;
            int thisDepth = 0;
            while (thisSide != null)
            {
                thisDepth++;
                thisSide = thisSide.hierarchy.parent;
            }

            VisualElement otherSide = other;
            int otherDepth = 0;
            while (otherSide != null)
            {
                otherDepth++;
                otherSide = otherSide.hierarchy.parent;
            }

            //we reset
            thisSide = this;
            otherSide = other;

            // we then walk up until both sides are at the same depth
            while (thisDepth > otherDepth)
            {
                thisDepth--;
                thisSide = thisSide.hierarchy.parent;
            }

            while (otherDepth > thisDepth)
            {
                otherDepth--;
                otherSide = otherSide.hierarchy.parent;
            }

            // Now both are at the same depth, We then walk up the tree we hit the same element
            while (thisSide != otherSide)
            {
                thisSide = thisSide.hierarchy.parent;
                otherSide = otherSide.hierarchy.parent;
            }

            return thisSide;
        }

        internal VisualElement GetRoot()
        {
            if (panel != null)
            {
                return panel.visualTree;
            }

            VisualElement root = this;
            while (root.m_PhysicalParent != null)
            {
                root = root.m_PhysicalParent;
            }

            return root;
        }

        internal VisualElement GetNextElementDepthFirst()
        {
            if (m_Children.Count > 0)
            {
                return m_Children[0];
            }

            var p = m_PhysicalParent;
            var c = this;

            while (p != null)
            {
                int i;
                for (i = 0; i < p.m_Children.Count; i++)
                {
                    if (p.m_Children[i] == c)
                    {
                        break;
                    }
                }

                if (i < p.m_Children.Count - 1)
                {
                    return p.m_Children[i + 1];
                }

                c = p;
                p = p.m_PhysicalParent;
            }

            return null;
        }

        internal VisualElement GetPreviousElementDepthFirst()
        {
            if (m_PhysicalParent != null)
            {
                int i;
                for (i = 0; i < m_PhysicalParent.m_Children.Count; i++)
                {
                    if (m_PhysicalParent.m_Children[i] == this)
                    {
                        break;
                    }
                }

                if (i > 0)
                {
                    var p = m_PhysicalParent.m_Children[i - 1];
                    while (p.m_Children.Count > 0)
                    {
                        p = p.m_Children[p.m_Children.Count - 1];
                    }

                    return p;
                }

                return m_PhysicalParent;
            }

            return null;
        }

        internal VisualElement RetargetElement(VisualElement retargetAgainst)
        {
            if (retargetAgainst == null)
            {
                return this;
            }

            // If retargetAgainst.isCompositeRoot is true, we want to retarget THIS to the tree that holds
            // retargetAgainst, not against the tree rooted by retargetAgainst. In this case we start
            // by setting retargetRoot to retargetAgainst.m_PhysicalParent.
            // However, if retargetAgainst.m_PhysicalParent == null, we are at the top of the main tree,
            // so retargetRoot should be retargetAgainst.
            var retargetRoot = retargetAgainst.m_PhysicalParent ?? retargetAgainst;
            while (retargetRoot.m_PhysicalParent != null && !retargetRoot.isCompositeRoot)
            {
                retargetRoot = retargetRoot.m_PhysicalParent;
            }

            var retargetCandidate = this;
            var p = m_PhysicalParent;
            while (p != null)
            {
                p = p.m_PhysicalParent;

                if (p == retargetRoot)
                {
                    return retargetCandidate;
                }

                if (p != null && p.isCompositeRoot)
                {
                    retargetCandidate = p;
                }
            }

            // THIS is not under retargetRoot
            return this;
        }
    }
}
