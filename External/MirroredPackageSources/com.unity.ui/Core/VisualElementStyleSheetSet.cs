using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This structure manipulates the set of <see cref="StyleSheet"/> objects attached to the owner <see cref="VisualElement"/>.
    /// </summary>
    /// <remarks>
    ///
    ///                 <see cref="VisualElementStyleSheetSet"/> instances cannot be created directly.
    ///                 Use the <see cref="VisualElement.styleSheets"/> property accessor to work with the style sheets of an element.
    ///
    /// </remarks>
    public struct VisualElementStyleSheetSet : IEquatable<VisualElementStyleSheetSet>
    {
        private readonly VisualElement m_Element;

        internal VisualElementStyleSheetSet(VisualElement element)
        {
            m_Element = element;
        }

        /// <summary>
        /// Adds a style sheet for the owner element.
        /// </summary>
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

        /// <summary>
        /// Removes all style sheets for the owner element.
        /// </summary>
        public void Clear()
        {
            if (m_Element.styleSheetList == null)
                return;

            m_Element.styleSheetList = null;
            m_Element.IncrementVersion(VersionChangeType.StyleSheet);
        }

        /// <summary>
        /// Removes a style sheet for the owner element.
        /// </summary>
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

        /// <summary>
        /// Looks for the specified <see cref="StyleSheet"/>
        /// </summary>
        /// <returns>True if the style sheet is attached to the owner element, false otherwise.</returns>
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

        /// <summary>
        /// Number of style sheets attached to the owner element.
        ///
        /// </summary>
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
