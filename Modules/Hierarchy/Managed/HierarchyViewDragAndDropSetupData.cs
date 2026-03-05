// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Contains the data needed to start a drag and drop operation.
    /// </summary>
    public readonly ref struct HierarchyViewDragAndDropSetupData
    {
        readonly Dictionary<string, object> m_GenericData;

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> instances involved in the drag and drop operation.
        /// </summary>
        public ReadOnlySpan<HierarchyNode> Nodes { get; }

        /// <summary>
        /// Gets the <see cref="UnityEngine.Object"/> references that are dragged. <see cref="HierarchyNodeTypeHandler"/> instances can populate this list.
        /// </summary>
        public List<EntityId> EntityIds { get; }

        /// <summary>
        /// Gets the paths of assets involved in the drag and drop operation. <see cref="HierarchyNodeTypeHandler"/> instances can populate this list.
        /// </summary>
        public List<string> Paths { get; }

        /// <summary>
        /// Gets the <see cref="HierarchyView"/> where the drag and drop operation occurs.
        /// </summary>
        public HierarchyView View { get; }

        /// <summary>
        /// Sets generic data for the drag and drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="value">The data to store.</param>
        public void SetGenericData(string key, object value)
        {
            m_GenericData[key] = value;
        }

        internal HierarchyViewDragAndDropSetupData(ReadOnlySpan<HierarchyNode> nodes, List<EntityId> entityIds, List<string> paths, HierarchyView view, Dictionary<string, object> genericData)
        {
            Nodes = nodes;
            EntityIds = entityIds;
            Paths = paths;
            View = view;
            m_GenericData = genericData;
        }
    }
}
