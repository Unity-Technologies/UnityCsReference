// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Static lists used to avoid allocations when using simple change hint lists.
    /// </summary>
    [UnityRestricted]
    internal class ChangeHintList
    {
        /// <summary>
        /// Change hint list for unspecified changes.
        /// </summary>
        public static readonly ChangeHintList Unspecified = new(ChangeHint.Unspecified);

        /// <summary>
        /// Change hint list for layout changes.
        /// </summary>
        public static readonly ChangeHintList Layout = new(ChangeHint.Layout);

        /// <summary>
        /// Change hint list for style changes.
        /// </summary>
        public static readonly ChangeHintList Style = new(ChangeHint.Style);

        /// <summary>
        /// Change hint list for data changes.
        /// </summary>
        public static readonly ChangeHintList Data = new(ChangeHint.Data);

        /// <summary>
        /// Change hint list for graph topology changes.
        /// </summary>
        public static readonly ChangeHintList GraphTopology = new(ChangeHint.GraphTopology);

        /// <summary>
        /// Change hint list for grouping changes.
        /// </summary>
        public static readonly ChangeHintList Grouping = new(ChangeHint.Grouping);

        /// <summary>
        /// Change hint list for UI hint changes.
        /// </summary>
        public static readonly ChangeHintList UIHints = new(ChangeHint.UIHints);

        /// <summary>
        /// Change hint list for redraw changes.
        /// </summary>
        public static readonly ChangeHintList NeedsRedraw = new(ChangeHint.NeedsRedraw);

        readonly List<ChangeHint> m_ChangeHints;

        ChangeHintList(ChangeHint changeHint)
        {
            m_ChangeHints = new List<ChangeHint>(1) { changeHint };
        }

        ChangeHintList(ChangeHintList changeHintList)
        {
            m_ChangeHints = new List<ChangeHint>(changeHintList.m_ChangeHints);
        }

        // For tests only.
        internal ChangeHintList(params ChangeHint[] changeHintList)
        {
            m_ChangeHints = new List<ChangeHint>(changeHintList);
        }

        public IReadOnlyList<ChangeHint> Hints => m_ChangeHints;

        int Count => m_ChangeHints.Count;

        ChangeHint this[int index] => m_ChangeHints[index];

        static bool Equals(ChangeHintList a, ChangeHintList b)
        {
            if (ReferenceEquals(a, b))
                return true;

            return a.m_ChangeHints != null && b.m_ChangeHints != null && a.m_ChangeHints.Equals(b.m_ChangeHints);
        }

        static bool IsSharedList(ChangeHintList list)
        {
            return
                ReferenceEquals(list, Unspecified) ||
                ReferenceEquals(list, Layout) ||
                ReferenceEquals(list, Style) ||
                ReferenceEquals(list, Data) ||
                ReferenceEquals(list, GraphTopology) ||
                ReferenceEquals(list, Grouping) ||
                ReferenceEquals(list, UIHints) ||
                ReferenceEquals(list, NeedsRedraw);
        }

        /// <summary>
        /// Merges two lists of <see cref="ChangeHint"/>s.
        /// </summary>
        /// <param name="dest">The first list of <see cref="ChangeHint"/>s. This list may be modified and returned.</param>
        /// <param name="source">The list of <see cref="ChangeHint"/>s to merge with <paramref name="dest"/>. This list will not be modified but could be returned if it is a shared list.</param>
        /// <returns>The merged list of <see cref="ChangeHint"/>s.</returns>
        public static ChangeHintList AddRange(ChangeHintList dest, ChangeHintList source)
        {
            if (source == null || source.Count == 0)
                return dest;

            if (dest == null)
            {
                if (IsSharedList(source))
                    return source;

                dest = new ChangeHintList(source);
                return dest;
            }

            if (Equals(dest, Unspecified) || Equals(source, Unspecified))
                return Unspecified;

            if (Equals(dest, source))
                return dest;

            var returnValue = IsSharedList(dest) ? new ChangeHintList(dest) : dest;
            var writableList = returnValue.m_ChangeHints;
            writableList.Capacity = Math.Max(writableList.Capacity, writableList.Count + source.Count);

            for (var i = 0; i < source.Count; i++)
            {
                if (!writableList.Contains(source[i]))
                    writableList.Add(source[i]);
            }
            return returnValue;
        }

        /// <summary>
        /// Adds a ChangeHint to a <see cref="ChangeHintList"/>.
        /// </summary>
        /// <param name="list">The list of <see cref="ChangeHint"/>s.</param>
        /// <param name="changeHint">The <see cref="ChangeHint"/> to add.</param>
        /// <returns>The new list of <see cref="ChangeHint"/>s.</returns>
        public static ChangeHintList Add(ChangeHintList list, ChangeHint changeHint)
        {
            if (list == null)
                return ToSharedList(changeHint);

            if (changeHint == ChangeHint.Unspecified)
                return Unspecified;

            if (list.m_ChangeHints.Contains(changeHint))
                return list;

            var returnValue = IsSharedList(list) ? new ChangeHintList(list) : list;
            var writableList = returnValue.m_ChangeHints;
            writableList.Add(changeHint);
            return returnValue;
        }

        /// <summary>
        /// Converts a <see cref="ChangeHint"/> to a list of <see cref="ChangeHint"/>s.
        /// </summary>
        /// <param name="changeHint">The <see cref="ChangeHint"/> to convert.</param>
        /// <returns>A list of <see cref="ChangeHint"/>s that contains only <paramref name="changeHint"/>.</returns>
        public static ChangeHintList ToSharedList(ChangeHint changeHint)
        {
            if (changeHint == null) return null;

            if (changeHint == ChangeHint.Unspecified) return Unspecified;
            if (changeHint == ChangeHint.Layout) return Layout;
            if (changeHint == ChangeHint.Style) return Style;
            if (changeHint == ChangeHint.Data) return Data;
            if (changeHint == ChangeHint.GraphTopology) return GraphTopology;
            if (changeHint == ChangeHint.Grouping) return Grouping;
            if (changeHint == ChangeHint.UIHints) return UIHints;
            if (changeHint == ChangeHint.NeedsRedraw) return NeedsRedraw;

            return new ChangeHintList(changeHint);
        }

        internal bool Contains(ChangeHint needle)
        {
            for (var i = 0; i < m_ChangeHints.Count; i++)
            {
                var hint = m_ChangeHints[i];
                if (hint == needle)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="needle">The hint to find.</param>
        /// <returns>True if this object contains <paramref name="needle"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public bool HasChange(ChangeHint needle)
        {
            for (var i = 0; i < m_ChangeHints.Count; i++)
            {
                var hint = m_ChangeHints[i];
                if (hint == needle || hint == ChangeHint.Unspecified)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="needle1">The hint to find.</param>
        /// <param name="needle2">The hint to find.</param>
        /// <returns>True if this object contains <paramref name="needle1"/> or <paramref name="needle2"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public bool HasAnyChange(ChangeHint needle1, ChangeHint needle2)
        {
            for (var i = 0; i < m_ChangeHints.Count; i++)
            {
                var hint = m_ChangeHints[i];
                if (hint == needle1 || hint == needle2 || hint == ChangeHint.Unspecified)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="needles">The hints to find.</param>
        /// <returns>True if this object contains at least one of the hint in <paramref name="needles"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public bool HasAnyChange(IReadOnlyList<ChangeHint> needles)
        {
            for (var i = 0; i < m_ChangeHints.Count; i++)
            {
                var hint = m_ChangeHints[i];
                if (hint == ChangeHint.Unspecified || needles.Contains(hint))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the current changeList contains all the change hints in the provided list.
        /// </summary>
        /// <param name="needles">The other changelist to compare.</param>
        /// <returns>True if all hints in <paramref name="needles"/> are contained in this object or this object is Unspecified. False otherwise.</returns>
        /// <remarks>Returns false if <paramref name="needles"/> contains <see cref="ChangeHint.Unspecified"/>, unless this contains <see cref="ChangeHint.Unspecified"/> itself.</remarks>
        public bool IsSupersetOf(ChangeHintList needles)
        {
            if (Contains(ChangeHint.Unspecified))
                return true;
            for (var i = 0; i < needles.m_ChangeHints.Count; i++)
            {
                var hint = needles.m_ChangeHints[i];
                if (hint == ChangeHint.Unspecified)
                    return false;
                if (!Contains(hint))
                    return false;
            }

            return true;
        }
    }
}
