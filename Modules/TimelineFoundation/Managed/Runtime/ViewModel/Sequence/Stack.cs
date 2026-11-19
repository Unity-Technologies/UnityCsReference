// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class Stack
    {
        List<Track> m_ChildrenList;

        public abstract UniqueID ID { get; }

        public IReadOnlyList<Track> children => m_ChildrenList;

        internal void SetChildren_Internal(List<Track> children)
        {
            m_ChildrenList = children;
        }
    }

    static class StackUtilities
    {
        /// <summary>
        /// Gets a list of child tracks and recursively adds their children as well.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns>Flattened list of child tracks</returns>
        public static IReadOnlyList<Track> GetFlattenedChildren(this Stack parent)
        {
            List<Track> list = new List<Track>();
            foreach (var child in parent.children)
            {
                list.Add(child);
                list.AddRange(child.GetFlattenedChildren());
            }

            return list;
        }
    }
}
