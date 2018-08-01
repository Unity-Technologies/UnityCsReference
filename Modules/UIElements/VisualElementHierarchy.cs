// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_CAPTURE_DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public partial class VisualElement : IEnumerable<VisualElement>
    {
        public Hierarchy shadow
        {
            get; private set;
        }

        public enum ClippingOptions
        {
            ClipContents, // default value, content of this element and its children will be clipped
            NoClipping, // no clipping
            ClipAndCacheContents // Renders contents to an cache texture
        }

        // Suppress "use of obsolete enum" warning
        #pragma warning disable 0618
        private ClippingOptions m_ClippingOptions = ClippingOptions.NoClipping;
        #pragma warning restore 0618

        public ClippingOptions clippingOptions
        {
            get { return m_ClippingOptions; }
            set
            {
                if (m_ClippingOptions != value)
                {
                    m_ClippingOptions = value;
                    IncrementVersion(VersionChangeType.Repaint);
                }
            }
        }

        internal bool ShouldClip()
        {
            return style.overflow != Overflow.Visible || clippingOptions != ClippingOptions.NoClipping;
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

        static readonly VisualElement[] s_EmptyList = new VisualElement[0];
        private List<VisualElement> m_Children;

        // each element has a ref to the root panel for internal bookkeeping
        // this will be null until a visual tree is added to a panel
        internal BaseVisualElementPanel elementPanel { get; private set; }

        public IPanel panel { get { return elementPanel; } }

        // Logical container where child elements are added.
        // usually same as this element, but can be overridden by more complex types
        // see ScrollView.contentContainer for an example
        public virtual VisualElement contentContainer
        {
            get { return this; }
        }

        // IVisualElementHierarchy container
        public void Add(VisualElement child)
        {
            if (contentContainer == this)
            {
                shadow.Add(child);
            }
            else
            {
                contentContainer?.Add(child);
            }

            child.m_LogicalParent = this;
        }

        public void Insert(int index, VisualElement element)
        {
            if (contentContainer == this)
            {
                shadow.Insert(index, element);
            }
            else
            {
                contentContainer?.Insert(index, element);
            }

            element.m_LogicalParent = this;
        }

        public void Remove(VisualElement element)
        {
            if (contentContainer == this)
            {
                shadow.Remove(element);
            }
            else
            {
                contentContainer?.Remove(element);
            }
        }

        public void RemoveAt(int index)
        {
            if (contentContainer == this)
            {
                shadow.RemoveAt(index);
            }
            else
            {
                contentContainer?.RemoveAt(index);
            }
        }

        public void Clear()
        {
            if (contentContainer == this)
            {
                shadow.Clear();
            }
            else
            {
                contentContainer?.Clear();
            }
        }

        public VisualElement ElementAt(int index)
        {
            if (contentContainer == this)
            {
                return shadow.ElementAt(index);
            }

            return contentContainer?.ElementAt(index);
        }

        public VisualElement this[int key]
        {
            get { return ElementAt(key); }
        }

        public int childCount
        {
            get
            {
                if (contentContainer == this)
                {
                    return shadow.childCount;
                }
                return contentContainer?.childCount ?? 0;
            }
        }

        public int IndexOf(VisualElement element)
        {
            if (contentContainer == this)
            {
                return shadow.IndexOf(element);
            }
            return contentContainer?.IndexOf(element) ?? -1;
        }

        public IEnumerable<VisualElement> Children()
        {
            if (contentContainer == this)
            {
                return shadow.Children();
            }
            return contentContainer?.Children() ?? s_EmptyList;
        }

        public void Sort(Comparison<VisualElement> comp)
        {
            if (contentContainer == this)
            {
                shadow.Sort(comp);
            }
            else
            {
                contentContainer?.Sort(comp);
            }
        }

        public void BringToFront()
        {
            if (shadow.parent == null)
                return;

            shadow.parent.shadow.BringToFront(this);
        }

        public void SendToBack()
        {
            if (shadow.parent == null)
                return;

            shadow.parent.shadow.SendToBack(this);
        }

        public void PlaceBehind(VisualElement sibling)
        {
            if (shadow.parent == null || sibling.shadow.parent != shadow.parent)
            {
                throw new ArgumentException("VisualElements are not siblings");
            }

            shadow.parent.shadow.PlaceBehind(this, sibling);
        }

        public void PlaceInFront(VisualElement sibling)
        {
            if (shadow.parent == null || sibling.shadow.parent != shadow.parent)
            {
                throw new ArgumentException("VisualElements are not siblings");
            }

            shadow.parent.shadow.PlaceInFront(this, sibling);
        }

        public struct Hierarchy
        {
            private readonly VisualElement m_Owner;

            public VisualElement parent
            {
                get { return m_Owner.m_PhysicalParent; }
            }

            internal Hierarchy(VisualElement element)
            {
                m_Owner = element;
            }

            public void Add(VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot add null child");

                Insert(childCount, child);
            }

            public void Insert(int index, VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot insert null child");

                if (index > childCount)
                    throw new IndexOutOfRangeException("Index out of range: " + index);

                if (child == m_Owner)
                    throw new ArgumentException("Cannot insert element as its own child");

                child.RemoveFromHierarchy();

                child.shadow.SetParent(m_Owner);
                if (m_Owner.m_Children == null)
                {
                    //TODO: Trigger a release on finalizer or something, this means we'll need to make the pool thread-safe as well
                    m_Owner.m_Children = VisualElementListPool.Get();
                }

                if (m_Owner.yogaNode.IsMeasureDefined)
                {
                    m_Owner.yogaNode.SetMeasureFunction(null);
                }

                PutChildAtIndex(child, index);

                child.SetEnabledFromHierarchy(m_Owner.enabledInHierarchy);

                child.IncrementVersion(VersionChangeType.Hierarchy);
                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            public void Remove(VisualElement child)
            {
                if (child == null)
                    throw new ArgumentException("Cannot remove null child");

                if (child.shadow.parent != m_Owner)
                    throw new ArgumentException("This visualElement is not my child");

                if (m_Owner.m_Children != null)
                {
                    int index = m_Owner.m_Children.IndexOf(child);
                    RemoveAt(index);
                }
            }

            public void RemoveAt(int index)
            {
                if (index < 0 || index >= childCount)
                    throw new IndexOutOfRangeException("Index out of range: " + index);

                var child = m_Owner.m_Children[index];
                RemoveChildAtIndex(index);

                child.shadow.SetParent(null);

                if (childCount == 0)
                {
                    ReleaseChildList();
                    if (m_Owner.requireMeasureFunction)
                    {
                        m_Owner.yogaNode.SetMeasureFunction(m_Owner.Measure);
                    }
                }

                // Child is detached from the panel, notify using the panel directly.
                m_Owner.elementPanel?.OnVersionChanged(child, VersionChangeType.Hierarchy);
                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            public void Clear()
            {
                if (childCount > 0)
                {
                    foreach (VisualElement e in m_Owner.m_Children)
                    {
                        e.shadow.SetParent(null);
                        e.m_LogicalParent = null;
                        m_Owner.elementPanel?.OnVersionChanged(e, VersionChangeType.Hierarchy);
                    }

                    ReleaseChildList();
                    m_Owner.yogaNode.Clear();
                    if (m_Owner.requireMeasureFunction)
                    {
                        m_Owner.yogaNode.SetMeasureFunction(m_Owner.Measure);
                    }

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
                RemoveChildAtIndex(currentIndex);
                PutChildAtIndex(child, nextIndex);

                m_Owner.IncrementVersion(VersionChangeType.Hierarchy);
            }

            public int childCount
            {
                get
                {
                    return m_Owner.m_Children != null ? m_Owner.m_Children.Count : 0;
                }
            }

            public VisualElement this[int key] { get { return ElementAt(key); } }

            public int IndexOf(VisualElement element)
            {
                if (m_Owner.m_Children != null)
                {
                    return m_Owner.m_Children.IndexOf(element);
                }
                return -1;
            }

            public VisualElement ElementAt(int index)
            {
                if (m_Owner.m_Children != null)
                {
                    return m_Owner.m_Children[index];
                }

                throw new IndexOutOfRangeException("Index out of range: " + index);
            }

            public IEnumerable<VisualElement> Children()
            {
                if (m_Owner.m_Children != null)
                {
                    return m_Owner.m_Children;
                }
                return s_EmptyList;
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
                var children = m_Owner.m_Children;
                if (children != null)
                {
                    m_Owner.m_Children = null;
                    VisualElementListPool.Release(children);
                }
            }
        }

        /// <summary>
        /// Will remove this element from its hierarchy
        /// </summary>
        public void RemoveFromHierarchy()
        {
            if (shadow.parent != null)
            {
                shadow.parent.shadow.Remove(this);
            }
        }

        public T GetFirstOfType<T>() where T : class
        {
            T casted = this as T;
            if (casted != null)
                return casted;
            return GetFirstAncestorOfType<T>();
        }

        public T GetFirstAncestorOfType<T>() where T : class
        {
            VisualElement ancestor = shadow.parent;
            while (ancestor != null)
            {
                T castedAncestor = ancestor as T;
                if (castedAncestor != null)
                {
                    return castedAncestor;
                }
                ancestor = ancestor.shadow.parent;
            }
            return null;
        }

        public bool Contains(VisualElement child)
        {
            while (child != null)
            {
                if (child.shadow.parent == this)
                {
                    return true;
                }

                child = child.shadow.parent;
            }

            return false;
        }

        private void GatherAllChildren(List<VisualElement> elements)
        {
            if (m_Children != null && m_Children.Count > 0)
            {
                int startIndex = elements.Count;
                elements.AddRange(m_Children);

                while (startIndex < elements.Count)
                {
                    var current = elements[startIndex];

                    if (current.m_Children != null && current.m_Children.Count > 0)
                    {
                        elements.AddRange(current.m_Children);
                    }

                    ++startIndex;
                }
            }
        }

        public VisualElement FindCommonAncestor(VisualElement other)
        {
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
                thisSide = thisSide.shadow.parent;
            }

            VisualElement otherSide = other;
            int otherDepth = 0;
            while (otherSide != null)
            {
                otherDepth++;
                otherSide = otherSide.shadow.parent;
            }

            //we reset
            thisSide = this;
            otherSide = other;

            // we then walk up until both sides are at the same depth
            while (thisDepth > otherDepth)
            {
                thisDepth--;
                thisSide = thisSide.shadow.parent;
            }

            while (otherDepth > thisDepth)
            {
                otherDepth--;
                otherSide = otherSide.shadow.parent;
            }

            // Now both are at the same depth, We then walk up the tree we hit the same element
            while (thisSide != otherSide)
            {
                thisSide = thisSide.shadow.parent;
                otherSide = otherSide.shadow.parent;
            }

            return thisSide;
        }

        public IEnumerator<VisualElement> GetEnumerator()
        {
            if (contentContainer == this)
            {
                return shadow.Children().GetEnumerator();
            }

            IEnumerator<VisualElement> result;
            if (contentContainer != null)
            {
                result = contentContainer.GetEnumerator();
            }
            else
            {
                result = (IEnumerator<VisualElement>)s_EmptyList.GetEnumerator();
            }
            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (contentContainer == this)
            {
                return ((IEnumerable)shadow.Children()).GetEnumerator();
            }

            IEnumerator result;
            if (contentContainer != null)
            {
                result = ((IEnumerable)contentContainer).GetEnumerator();
            }
            else
            {
                result = s_EmptyList.GetEnumerator();
            }

            return result;
        }
    }
}
