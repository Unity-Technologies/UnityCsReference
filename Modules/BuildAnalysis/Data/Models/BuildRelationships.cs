// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Which of the listed builds ran nested inside which, read from <see cref="BuildHistory.TryGetParentBuild"/>
    /// once per build-list refresh.
    /// </summary>
    internal sealed class BuildRelationships
    {
        private readonly Dictionary<GUID, GUID> m_ParentOf;
        private readonly Dictionary<GUID, List<GUID>> m_ChildrenOf;

        /// <summary>No build is nested in any other.</summary>
        public BuildRelationships()
            : this(Array.Empty<(GUID Parent, GUID Child)>())
        {
        }

        // Children keep the links' order; the build list relies on it.
        public BuildRelationships(IReadOnlyList<(GUID Parent, GUID Child)> links)
        {
            if (links == null)
                throw new ArgumentNullException(nameof(links));

            m_ParentOf = new Dictionary<GUID, GUID>(links.Count);
            m_ChildrenOf = new Dictionary<GUID, List<GUID>>();

            for (var i = 0; i < links.Count; i++)
            {
                var (parent, child) = links[i];
                m_ParentOf[child] = parent;

                if (!m_ChildrenOf.TryGetValue(parent, out var children))
                {
                    children = new List<GUID>();
                    m_ChildrenOf[parent] = children;
                }
                children.Add(child);
            }
        }

        public bool HasAny => m_ParentOf.Count > 0;

        public bool TryGetParent(GUID child, out GUID parent)
        {
            return m_ParentOf.TryGetValue(child, out parent);
        }

        /// <summary>Direct children only, most-recent-first. Never null.</summary>
        public IReadOnlyList<GUID> GetChildren(GUID parent)
        {
            return m_ChildrenOf.TryGetValue(parent, out var children) ? children : Array.Empty<GUID>();
        }
    }
}
