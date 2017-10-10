// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_CAPTURE_DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.CSSLayout;
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

        private ClippingOptions m_ClippingOptions;
        public ClippingOptions clippingOptions
        {
            get { return m_ClippingOptions; }
            set
            {
                if (m_ClippingOptions != value)
                {
                    m_ClippingOptions = value;
                    Dirty(ChangeType.Repaint);
                }
            }
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
                contentContainer.Add(child);
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
                contentContainer.Insert(index, element);
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
                contentContainer.Remove(element);
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
                contentContainer.RemoveAt(index);
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
                contentContainer.Clear();
            }
        }

        public VisualElement ElementAt(int index)
        {
            if (contentContainer == this)
            {
                return shadow.ElementAt(index);
            }

            return contentContainer.ElementAt(index);
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
                return contentContainer.childCount;
            }
        }

        public IEnumerable<VisualElement> Children()
        {
            if (contentContainer == this)
            {
                return shadow.Children();
            }
            return contentContainer.Children();
        }

        public void Sort(Comparison<VisualElement> comp)
        {
            if (contentContainer == this)
            {
                shadow.Sort(comp);
            }
            else
            {
                contentContainer.Sort(comp);
            }
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
                    m_Owner.m_Children = new List<VisualElement>();
                }

                if (m_Owner.cssNode.IsMeasureDefined)
                {
                    m_Owner.cssNode.SetMeasureFunction(null);
                }

                if (index >= m_Owner.m_Children.Count)
                {
                    m_Owner.m_Children.Add(child);
                    m_Owner.cssNode.Insert(m_Owner.cssNode.Count, child.cssNode);
                }
                else
                {
                    m_Owner.m_Children.Insert(index, child);
                    m_Owner.cssNode.Insert(index, child.cssNode);
                }

                child.SetEnabledFromHierarchy(m_Owner.enabledInHierarchy);

                // child styles are dependent on topology
                child.Dirty(ChangeType.Styles);
                m_Owner.Dirty(ChangeType.Layout);

                // persistent data key may have changed or needs initialization
                if (!string.IsNullOrEmpty(child.persistenceKey))
                    child.Dirty(ChangeType.PersistentData);
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
                child.shadow.SetParent(null);
                m_Owner.m_Children.RemoveAt(index);
                m_Owner.cssNode.RemoveAt(index);

                if (childCount == 0)
                {
                    m_Owner.cssNode.SetMeasureFunction(m_Owner.Measure);
                }

                m_Owner.Dirty(ChangeType.Layout);
            }

            public void Clear()
            {
                if (childCount > 0)
                {
                    foreach (VisualElement e in m_Owner.m_Children)
                    {
                        e.shadow.SetParent(null);
                        e.m_LogicalParent = null;
                    }
                    m_Owner.m_Children.Clear();
                    m_Owner.cssNode.Clear();
                    m_Owner.Dirty(ChangeType.Layout);
                }
            }

            public int childCount
            {
                get
                {
                    return m_Owner.m_Children != null ? m_Owner.m_Children.Count : 0;
                }
            }

            public VisualElement this[int key] { get { return ElementAt(key); } }

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
                    m_Owner.ChangePanel(m_Owner.m_PhysicalParent.elementPanel);
                    m_Owner.PropagateChangesToParents();
                }
                else
                {
                    m_Owner.ChangePanel(null);
                }
            }

            public void Sort(Comparison<VisualElement> comp)
            {
                m_Owner.m_Children.Sort(comp);

                m_Owner.cssNode.Clear();
                for (int i = 0; i < m_Owner.m_Children.Count; i++)
                {
                    m_Owner.cssNode.Insert(i, m_Owner.m_Children[i].cssNode);
                }
                m_Owner.Dirty(ChangeType.Layout);
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

        public IEnumerator<VisualElement> GetEnumerator()
        {
            if (contentContainer == this)
            {
                return shadow.Children().GetEnumerator();
            }
            return contentContainer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (contentContainer == this)
            {
                return ((IEnumerable)shadow.Children()).GetEnumerator();
            }
            return ((IEnumerable)contentContainer).GetEnumerator();
        }
    }
}
