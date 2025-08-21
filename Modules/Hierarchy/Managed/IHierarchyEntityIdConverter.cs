// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Interface that <see cref="HierarchyNodeTypeHandler"/> should implement if the type can be mapped to <see cref="EntityId"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IHierarchyEntityIdConverter
    {
        /// <summary>
        /// Get the hierarchy node corresponding to the given entity id.
        /// </summary>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The hierarchy node.</returns>
        protected internal HierarchyNode GetNode(EntityId entityId);

        /// <summary>
        /// Get the hierarchy nodes corresponding to the given entity ids.
        /// </summary>
        /// <remarks>
        /// The implementation should only fill the nodes that it knows how to convert, and leave the rest of the nodes unchanged.
        /// </remarks>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        protected internal void GetNodes(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes);

        /// <summary>
        /// Get the entity id corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The entity id.</returns>
        protected internal EntityId GetEntityId(in HierarchyNode node);

        /// <summary>
        /// Get the entity ids corresponding to the given hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// The implementation should only fill the entity ids that it knows how to convert, and leave the rest of the entity ids unchanged.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="outEntityIds">The entity ids.</param>
        protected internal void GetEntityIds(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds);
    }

    /// <summary>
    /// Extension methods on <see cref="Hierarchy"/> for <see cref="IHierarchyEntityIdConverter"/>.
    /// </summary>
    internal static partial class HierarchyExtensions
    {
        /// <summary>
        /// Get the hierarchy node corresponding to the given entity id.
        /// </summary>
        /// <remarks>
        /// This method will loop through all <see cref="IHierarchyEntityIdConverter"/> handlers and return the first valid hierarchy node.
        /// It is more efficient to call <see cref="IHierarchyEntityIdConverter.GetNodes"/> when multiple entity ids need to be converted.
        /// </remarks>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The hierarchy node.</returns>
        public static HierarchyNode GetNode(this Hierarchy hierarchy, EntityId entityId)
        {
            if (entityId == EntityId.None)
                return HierarchyNode.Null;

            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEntityIdConverter converter)
                {
                    var node = converter.GetNode(entityId);
                    if (node != HierarchyNode.Null)
                        return node;
                }
            }
            return HierarchyNode.Null;
        }

        /// <summary>
        /// Get the hierarchy nodes corresponding to the given entity ids.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        public static void GetNodes(this Hierarchy hierarchy, ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes)
        {
            if (outNodes.Length != entityIds.Length)
                throw new ArgumentException($"{nameof(entityIds)} and {nameof(outNodes)} must have the same length.");

            outNodes.Clear();
            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEntityIdConverter converter)
                    converter.GetNodes(entityIds, outNodes);
            }
        }

        /// <summary>
        /// Get the entity id corresponding to the given hierarchy node.
        /// </summary>
        /// <remarks>
        /// This method will loop through all <see cref="IHierarchyEntityIdConverter"/> handlers and return the first valid entity id.
        /// It is more efficient to call <see cref="IHierarchyEntityIdConverter.GetEntityIds"/> when multiple nodes need to be converted.
        /// </remarks>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns></returns>
        public static EntityId GetEntityId(this Hierarchy hierarchy, in HierarchyNode node)
        {
            if (node == HierarchyNode.Null)
                return EntityId.None;

            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEntityIdConverter converter)
                {
                    var entityId = converter.GetEntityId(in node);
                    if (entityId != EntityId.None)
                        return entityId;
                }
            }
            return EntityId.None;
        }

        /// <summary>
        /// Get the entity ids corresponding to the given hierarchy nodes.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="outEntityIds">The EntityIds.</param>
        public static void GetEntityIds(this Hierarchy hierarchy, ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds)
        {
            if (outEntityIds.Length != nodes.Length)
                throw new ArgumentException($"{nameof(nodes)} and {nameof(outEntityIds)} must have the same length.");

            outEntityIds.Clear();
            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEntityIdConverter converter)
                    converter.GetEntityIds(nodes, outEntityIds);
            }
        }
    }
}
