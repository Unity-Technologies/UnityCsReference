// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Container holding the data needed to start a drag and drop operation.
    /// </summary>
    /// <remarks>Do not keep a reference to this class or any of its data past the scope of <see cref="IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData)"/>.</remarks>
    [VisibleToOtherModules]
    internal readonly ref struct HierarchyViewDragAndDropSetupData
    {
        readonly Dictionary<string, object> m_GenericData;

        /// <summary>
        /// The <see cref="HierarchyNode"/>s that are being dragged. These nodes need to be converted into the proper
        /// drag and drop data by the <see cref="IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData)"/> method.
        /// </summary>
        public ReadOnlySpan<HierarchyNode> Nodes { get; }

        /// <summary>
        /// <see cref="UnityEngine.Object"/> references being dragged. <see cref="HierarchyNodeTypeHandler"/>s can populate this member.
        /// </summary>
        public List<EntityId> EntityIds { get; }

        /// <summary>
        /// Paths of assets being dragged. <see cref="HierarchyNodeTypeHandler"/>s can populate this member.
        /// </summary>
        public List<string> Paths { get; }

        /// <summary>
        /// The <see cref="HierarchyView"/> where the drag and drop operation is happening.
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
