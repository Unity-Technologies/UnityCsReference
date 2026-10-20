// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    public unsafe partial struct EntityManager
    {
        static void SetParentTransformRefInternal(EntityDataAccess* eda, Entity childEntity, Entity parentEntity, bool preserveWorldTransform)
        {
            var childRef = eda->GetTransformRef(childEntity);
            var parentRef = parentEntity == Entity.Null ? default(TransformRef) : eda->GetTransformRef(parentEntity);
            childRef.SetParent(eda->EntityComponentStore, parentRef, parentEntity, childEntity, preserveWorldTransform);
        }

        /// <summary>
        /// Establish a new link between a child and parent entity.
        /// </summary>
        /// <remarks>
        /// This function must not be used from an ExclusiveEntityTransaction context. Manipulating transform hierarchies
        /// from worker threads is not safe.
        /// </remarks>
        /// <param name="child">The entity whose parent should be changed.</param>
        /// <param name="newParent">The new parent entity for <paramref name="child"/>. If this value is <see cref="Entity.Null"/>,
        /// <paramref name="child"/> will have no parent and will become the root of a new hierarchy.</param>
        /// <param name="preserveWorldTransform">
        /// If true, the world-space transform of <paramref name="child"/> will be preserved as closely as possible by
        /// setting its <see cref="Unity.Transforms.LocalTransform"/> component to match its current world-space transform. Slight
        /// differences are still possible due to floating-point rounding.
        ///
        /// If false, <paramref name="child"/>'s <see cref="Unity.Transforms.LocalTransform"/> components will not be modified. However, the existing
        /// value will now be relative to world-space instead to its previous parent; this may cause a significant
        /// instantaneous change in its world-space transform.
        /// </param>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="child"/> does not exist.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="newParent"/> is not <see cref="Entity.Null"/> and does not exist.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if <paramref name="preserveWorldTransform"/> is true,
        /// but <paramref name="child"/> or <paramref name="newParent"/> (or their ancestors) do not have the required
        /// <see cref="Unity.Transforms.LocalTransform"/> component.</exception>
        public void SetParent(Entity child, Entity newParent, bool preserveWorldTransform = true)
        {
            var access = GetCheckedEntityDataAccess();
            access->AssertMainThread();
            var changes = access->BeginStructuralChanges();
            SetParentTransformRefInternal(access, child, newParent, preserveWorldTransform);

            if (newParent != Entity.Null)
            {
                // Both newParent and child now belong to a hierarchy, so we add a HasHierarchy
                // tag component so that we can query transforms that belong to a hierarchy
                // separately. This, also guarantees that all transforms within a chunk either
                // have a hierarchy, or do not, which helps with branch prediction in TransformUnion.
                // TODO DOTS-10361: We should remove TransformHierarchies and the HasHierarchy
                // flag from an Entity if its parent and children are removed, but not if it is a
                // GameObject.
                var hasHierarchyType = ComponentType.ReadWrite<HasHierarchy>();
                access->AddComponentDuringStructuralChange(child, hasHierarchyType);
                access->AddComponentDuringStructuralChange(newParent, hasHierarchyType);
            }

            access->EndStructuralChanges(ref changes);
        }

        // TODO DOTS-10284 Keeping this around for speed-of-light testing without all the migration overhead (LocalTransform, Parent, Child updates)
        internal void SetParentTransformRef(Entity childEntity, Entity parentEntity, bool preserveWorldTransform = true)
        {
            var access = GetCheckedEntityDataAccess();
            access->AssertMainThread();
            SetParentTransformRefInternal(access, childEntity, parentEntity, preserveWorldTransform);
        }

        /// <summary>
        /// Break the parent/child links between the target entity and all of its children. Each child entity becomes
        /// the root of a new hierarchy.
        /// </summary>
        /// <remarks>
        /// This is effectively the same as calling SetParent(child, Entity.Null, preserveWorldTransform) on all
        /// of the target entity's children, but is potentially more efficient.
        ///
        /// This function must not be used from an ExclusiveEntityTransaction context. Manipulating transform hierarchies
        /// from worker threads is not safe.
        /// </remarks>
        /// <param name="parent">The entity whose children should be detached.</param>
        /// <param name="preserveWorldTransform">
        /// If true, the world-space transform of all child entities will be preserved as closely as possible to match its
        /// current world-space transform. Slight differences are still possible due to floating-point rounding.
        ///
        /// If false, the childrens' transforms will not be modified. However, the existing
        /// values will now be relative to world-space instead of <paramref name="parent"/>; this may cause a significant instantaneous
        /// change in their world-space transforms.
        /// </param>
        /// <exception cref="ArgumentException">Thrown if the target <paramref name="parent"/> entity does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="preserveWorldTransform"/> is true,
        /// but <paramref name="parent"/>, its children, or its ancestors do not have the required
        /// <see cref="Unity.Transforms.LocalTransform"/> component.</exception>
        public void DetachChildren(Entity parent, bool preserveWorldTransform = true)
        {
            var access = GetCheckedEntityDataAccess();
            access->AssertMainThread();
            var changes = access->BeginStructuralChanges();
            access->EntityComponentStore->AssertEntitiesExist(&parent, 1);
            var parentRef = access->GetTransformRef(parent);

            // Get all children in a single native call (more efficient)
            using (var children = parentRef.GetChildEntities(Allocator.Temp))
            {
                // Iterate in reverse order to avoid invalidating indices
                for (var i = children.Length - 1; i >= 0; i--)
                {
                    var child = children[i];
                    if (child != Entity.Null)
                    {
                        // TODO: We should batch up this work so we don't have to call into Native code repeatedly
                        SetParentTransformRefInternal(access, child, Entity.Null, preserveWorldTransform);
                    }
                }
            }

            access->EndStructuralChanges(ref changes);
        }
    }
}
