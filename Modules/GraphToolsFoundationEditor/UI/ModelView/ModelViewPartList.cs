// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A list of <see cref="ModelViewPart"/>.
    /// </summary>
    class ModelViewPartList
    {
        List<ModelViewPart> m_Parts = new List<ModelViewPart>();

        public IReadOnlyList<ModelViewPart> Parts => m_Parts;

        /// <summary>
        /// Adds a part to this list.
        /// </summary>
        /// <param name="child">The part to add.</param>
        public void AppendPart(ModelViewPart child)
        {
            if (child != null)
                m_Parts.Add(child);
        }

        /// <summary>
        /// Gets the part with <see cref="ModelViewPart.PartName"/> equal to <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The part name to match.</param>
        /// <returns>The part found, or null if no part was found.</returns>
        public ModelViewPart GetPart(string name)
        {
            for (int i = 0; i < m_Parts.Count; i++)
            {
                var part = m_Parts[i];
                if (part.PartName == name)
                    return part;
            }

            return null;
        }

        /// <summary>
        /// Inserts a <see cref="ModelViewPart"/> before the part named <paramref name="beforeChild"/>.
        /// </summary>
        /// <param name="beforeChild">The name of the part before which <paramref name="child"/> should be inserted.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="beforeChild"/>.</exception>
        public void InsertPartBefore(string beforeChild, ModelViewPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == beforeChild)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(beforeChild), beforeChild, "Part not found");
                }
            }
        }

        /// <summary>
        /// Move <paramref name="child"/> before the part named <paramref name="beforeChild"/>.
        /// </summary>
        /// <param name="child">The part that will be moved.</param>
        /// <param name="beforeChild">The name of the part before which <paramref name="child"/> should be moved.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="child"/>.</exception>
        public void MovePartBefore(string child, string beforeChild)
        {
            if (beforeChild != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == beforeChild)
                    {
                        index = i;
                        break;
                    }
                }

                var targetPart = GetPart(child);

                if (index != -1 && targetPart != null)
                {
                    m_Parts.Remove(targetPart);
                    m_Parts.Insert(index, targetPart);
                }
                else
                {
                    if (targetPart == null)
                        throw new ArgumentOutOfRangeException(nameof(child), child, "Part not found");
                    else
                        throw new ArgumentOutOfRangeException(nameof(beforeChild), beforeChild, "Part not found");
                }
            }
        }

        /// <summary>
        /// Inserts a <see cref="ModelViewPart"/> after the part named <paramref name="afterChild"/>.
        /// </summary>
        /// <param name="afterChild">The name of the part after which <paramref name="child"/> should be inserted.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="afterChild"/>.</exception>
        public void InsertPartAfter(string afterChild, ModelViewPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == afterChild)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.Insert(index + 1, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(afterChild), afterChild, "Part not found");
                }
            }
        }

        /// <summary>
        /// Replaces the <see cref="ModelViewPart"/> named <paramref name="componentToReplace"/> by <paramref name="child"/>.
        /// </summary>
        /// <param name="componentToReplace">The name of the part to replace.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="componentToReplace"/>.</exception>
        public void ReplacePart(string componentToReplace, ModelViewPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == componentToReplace)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.RemoveAt(index);
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(componentToReplace), componentToReplace, "Part not found");
                }
            }
        }

        /// <summary>
        /// Removes the <see cref="ModelViewPart"/> named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the part to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="name"/>.</exception>
        public void RemovePart(string name)
        {
            var index = -1;
            for (int i = 0; i < m_Parts.Count; i++)
            {
                var part = m_Parts[i];
                if (part.PartName == name)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                m_Parts.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Part not found");
            }
        }
    }
}
