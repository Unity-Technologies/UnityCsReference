// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine;

namespace Unity.Hierarchy
{
    public static partial class HierarchyExtensions
    {
        #region Marked as obsolete warning in 6.6
        [Obsolete("GetNode is obsolete, use Hierarchy.GetNodeFromEntityId instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static HierarchyNode GetNode(this Hierarchy hierarchy, EntityId entityId) => hierarchy.GetNodeFromEntityId(entityId);

        [Obsolete("GetNodes is obsolete, use Hierarchy.GetNodesFromEntityIds instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetNodes(this Hierarchy hierarchy, ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes) => hierarchy.GetNodesFromEntityIds(entityIds, outNodes);

        [Obsolete("GetEntityId is obsolete, use Hierarchy.GetEntityIdFromNode instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static EntityId GetEntityId(this Hierarchy hierarchy, in HierarchyNode node) => hierarchy.GetEntityIdFromNode(in node);

        [Obsolete("GetEntityIds is obsolete, use Hierarchy.GetEntityIdsFromNodes instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetEntityIds(this Hierarchy hierarchy, ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds) => hierarchy.GetEntityIdsFromNodes(nodes, outEntityIds);
        #endregion
    }
}
