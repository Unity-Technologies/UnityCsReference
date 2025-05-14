// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This structure manipulates the set of <see cref="StyleSheet"/> objects attached to the owner <see cref="VisualElement"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="VisualElementStyleSheetSet"/> instances can't be created directly.
    /// Use the <see cref="VisualElement.styleSheets"/> property accessor to work with the style sheets of an element.
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
            Insert(count, styleSheet);
        }

        /// <summary>
        /// Adds a style sheet for the owner element at a specified index
        /// </summary>
        public void Insert(int index, StyleSheet styleSheet)
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

            m_Element.styleSheetList.Insert(index, styleSheet);
            m_Element.IncrementVersion(VersionChangeType.StyleSheet);

            m_Element.elementPanel?.liveReloadSystem.StartStyleSheetAssetTracking(styleSheet);
        }

        /// <summary>
        /// Removes all style sheets for the owner element.
        /// </summary>
        public void Clear()
        {
            if (m_Element.styleSheetList == null)
                return;

            if (m_Element.elementPanel != null)
            {
                var liveReloadSystem = m_Element.elementPanel.liveReloadSystem;
                foreach (var styleSheet in m_Element.styleSheetList)
                {
                    liveReloadSystem.StopStyleSheetAssetTracking(styleSheet);
                }
            }

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

                m_Element.elementPanel?.liveReloadSystem.StopStyleSheetAssetTracking(styleSheet);

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

                if (m_Element.elementPanel != null)
                {
                    var liveReloadSystem = m_Element.elementPanel.liveReloadSystem;
                    liveReloadSystem.StopStyleSheetAssetTracking(old);
                    liveReloadSystem.StartStyleSheetAssetTracking(@new);
                }
            }
        }

        /// <summary>
        /// Looks for the specified <see cref="StyleSheet"/>
        /// </summary>
        /// <returns>Returns true if the style sheet is attached to the owner element, false otherwise.</returns>
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

        /// <summary>
        /// Reads the value at the specified index in the list of StyleSheet objects attached of the element
        /// </summary>
        /// <param name="index">The index of the StyleSheet</param>
        public StyleSheet this[int index]
        {
            get
            {
                if (m_Element.styleSheetList == null)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return m_Element.styleSheetList[index];
            }
        }

        /// <summary>
        /// Compares instances of the VisualElementStyleSheetSet struct for equality.
        /// </summary>
        /// <param name="other">The structure to compare with.</param>
        /// <returns>Returns true if the two instances refer to the same element, false otherwise.</returns>
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

        /// <summary>
        /// Compares instances of the VisualElementStyleSheetSet struct for equality.
        /// </summary>
        /// <param name="left">The left operand of the comparison</param>
        /// <param name="right">The right operand of the comparison</param>
        /// <returns>True if the two instances refer to the same element, false otherwise.</returns>
        public static bool operator==(VisualElementStyleSheetSet left, VisualElementStyleSheetSet right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares instances of the VisualElementStyleSheetSet struct for inequality.
        /// </summary>
        /// <param name="left">The left operand of the comparison</param>
        /// <param name="right">The right operand of the comparison</param>
        /// <returns>Returns false if the two instances refer to the same element, true otherwise.</returns>
        public static bool operator!=(VisualElementStyleSheetSet left, VisualElementStyleSheetSet right)
        {
            return !left.Equals(right);
        }
    }
}
