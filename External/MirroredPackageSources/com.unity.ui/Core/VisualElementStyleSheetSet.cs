using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public struct VisualElementStyleSheetSet : IEquatable<VisualElementStyleSheetSet>
    {
        private readonly VisualElement m_Element;

        internal VisualElementStyleSheetSet(VisualElement element)
        {
            m_Element = element;
        }

        public void Add(StyleSheet styleSheet)
        {
            if (styleSheet == null)
                throw new ArgumentNullException(nameof(styleSheet));

            if (m_Element.styleSheetList == null)
            {
                m_Element.styleSheetList = new List<StyleSheet>();
            }
            else if (m_Element.styleSheetList.Contains(styleSheet))
            {
                return;
            }

            m_Element.styleSheetList.Add(styleSheet);
            m_Element.IncrementVersion(VersionChangeType.StyleSheet);
        }

        public void Clear()
        {
            if (m_Element.styleSheetList == null)
                return;

            m_Element.styleSheetList = null;
            m_Element.IncrementVersion(VersionChangeType.StyleSheet);
        }

        public bool Remove(StyleSheet styleSheet)
        {
            if (styleSheet == null)
                throw new ArgumentNullException(nameof(styleSheet));

            if (m_Element.styleSheetList != null && m_Element.styleSheetList.Remove(styleSheet))
            {
                if (m_Element.styleSheetList.Count == 0)
                {
                    m_Element.styleSheetList = null;
                }
                m_Element.IncrementVersion(VersionChangeType.StyleSheet);
                return true;
            }
            return false;
        }

        internal void Swap(StyleSheet old, StyleSheet @new)
        {
            if (old == null)
                throw new ArgumentNullException(nameof(old));

            if (@new == null)
                throw new ArgumentNullException(nameof(@new));

            if (m_Element.styleSheetList == null)
            {
                return;
            }

            int index = m_Element.styleSheetList.IndexOf(old);
            if (index >= 0)
            {
                m_Element.IncrementVersion(VersionChangeType.StyleSheet);
                m_Element.styleSheetList[index] = @new;
            }
        }

        public bool Contains(StyleSheet styleSheet)
        {
            if (styleSheet == null)
                throw new ArgumentNullException(nameof(styleSheet));

            if (m_Element.styleSheetList != null)
            {
                return m_Element.styleSheetList.Contains(styleSheet);
            }
            return false;
        }

        public int count
        {
            get
            {
                if (m_Element.styleSheetList == null)
                    return 0;

                return m_Element.styleSheetList.Count;
            }
        }

        public StyleSheet this[int index]
        {
            get
            {
                if (m_Element.styleSheetList == null)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return m_Element.styleSheetList[index];
            }
        }

        public bool Equals(VisualElementStyleSheetSet other)
        {
            return Equals(m_Element, other.m_Element);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VisualElementStyleSheetSet && Equals((VisualElementStyleSheetSet)obj);
        }

        public override int GetHashCode()
        {
            return (m_Element != null ? m_Element.GetHashCode() : 0);
        }

        public static bool operator==(VisualElementStyleSheetSet left, VisualElementStyleSheetSet right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(VisualElementStyleSheetSet left, VisualElementStyleSheetSet right)
        {
            return !left.Equals(right);
        }
    }
}
