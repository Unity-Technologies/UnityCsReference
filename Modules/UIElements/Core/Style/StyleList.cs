// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Style value that can be either a list or a <see cref="StyleKeyword"/>.
    /// </summary>
    /// <typeparam name="T">List type.</typeparam>
    public struct StyleList<T> : IStyleValue<List<T>>, IEquatable<StyleList<T>>
    {
        /// <summary>
        /// The style value.
        /// </summary>
        public List<T> value
        {
            get { return m_Keyword == StyleKeyword.Undefined ? m_Value : default; }
            set
            {
                m_Value = value;
                m_Keyword = StyleKeyword.Undefined;
            }
        }

        /// <summary>
        /// The style keyword.
        /// </summary>
        public StyleKeyword keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; }
        }

        /// <summary>
        /// Creates from either a list or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleList(List<T> v)
            : this(v, StyleKeyword.Undefined)
        {}

        /// <summary>
        /// Creates from either a list or a <see cref="StyleKeyword"/>.
        /// </summary>
        public StyleList(StyleKeyword keyword)
            : this(default, keyword)
        {}

        internal StyleList(List<T> v, StyleKeyword keyword)
        {
            m_Keyword = keyword;
            m_Value = v;
        }

        private StyleKeyword m_Keyword;
        private List<T> m_Value;

        /// <undoc/>
        public static bool operator==(StyleList<T> lhs, StyleList<T> rhs)
        {
            if (lhs.m_Keyword != rhs.m_Keyword)
                return false;

            var list1 = lhs.m_Value;
            var list2 = rhs.m_Value;
            if (ReferenceEquals(list1, list2))
                return true;

            if (list1 == null || list2 == null)
                return false;

            return list1.Count == list2.Count && list1.SequenceEqual(list2);
        }

        /// <undoc/>
        public static bool operator!=(StyleList<T> lhs, StyleList<T> rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator StyleList<T>(StyleKeyword keyword)
        {
            return new StyleList<T>(keyword);
        }

        /// <undoc/>
        public static implicit operator StyleList<T>(List<T> v)
        {
            return new StyleList<T>(v);
        }

        /// <undoc/>
        public bool Equals(StyleList<T> other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is StyleList<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                if (m_Value != null && m_Value.Count > 0)
                {
                    hashCode = EqualityComparer<T>.Default.GetHashCode(m_Value[0]);
                    for (int i = 1; i < m_Value.Count; i++)
                    {
                        hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(m_Value[i]);
                    }
                }
                hashCode = (hashCode * 397) ^ (int)m_Keyword;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return this.DebugString();
        }
    }
}
