// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Entities
{
    /// <summary>
    /// Tag component present on entities that are part of a transform hierarchy
    /// (i.e., entities that have or have ever had a parent or children).
    /// </summary>
    /// <remarks>
    /// Added automatically when <see cref="EntityManager.SetParent"/> places an entity into a hierarchy.
    /// GameObject entities always have it, because every GameObject Transform belongs to a native
    /// TransformHierarchy.
    /// </remarks>
    internal struct HasHierarchy : IComponentData { }


    /// <summary>
    /// A reference to the transform of an entity, which can be part of a transform hierarchy.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer] // Needed for safety handles to be patched if TransformRef is used in a job struct
    public unsafe readonly struct TransformRef : IQueryTypeParameter
    {
        readonly internal TransformUnion* m_TransformUnion;

        readonly internal AtomicSafetyHandle m_Safety0;
        // We need to track hierarchy safety correctly as well to handle GameObjects (DOTS-10269).
        readonly internal AtomicSafetyHandle m_HierarchySafety;
        readonly internal int m_SafetyReadOnlyCount;
        readonly internal int m_SafetyReadWriteCount;
        readonly internal byte m_IsReadOnly;

        /// <summary>
        /// Whether this reference points to a transform.
        /// </summary>
        public bool IsValid => m_TransformUnion != null;


        internal TransformRef(TransformUnion* transformUnion, bool isReadOnly,
            AtomicSafetyHandle safety, AtomicSafetyHandle hierarchySafety)
        {
            m_TransformUnion = transformUnion;
            m_IsReadOnly = (byte)(isReadOnly ? 1 : 0);
            m_Safety0 = safety;
            m_HierarchySafety = hierarchySafety;
            m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(m_HierarchySafety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(m_HierarchySafety);
        }

        /// <summary>
        /// The position relative to the parent, or to the world if the transform has no parent.
        /// </summary>
        public float3 LocalPosition
        {
            get
            {
                CheckReadAccess();
                return m_TransformUnion->Position;
            }
            set
            {
                CheckWriteAccess();
                m_TransformUnion->Position = value;
            }
        }

        /// <summary>
        /// The rotation relative to the parent, or to the world if the transform has no parent.
        /// </summary>
        public quaternion LocalRotation
        {
            get
            {
                CheckReadAccess();
                return m_TransformUnion->Rotation;
            }
            set
            {
                CheckWriteAccess();
                m_TransformUnion->Rotation = value;
            }
        }

        /// <summary>
        /// The scale relative to the parent, or to the world if the transform has no parent.
        /// </summary>
        public float3 LocalScale
        {
            get
            {
                CheckReadAccess();
                return m_TransformUnion->Scale;
            }
            set
            {
                CheckWriteAccess();
                m_TransformUnion->Scale = value;
            }
        }

        /// <summary>
        /// Computes the matrix that transforms from this transform's local space to world space.
        /// </summary>
        /// <returns>The local-to-world matrix.</returns>
        public float4x4 ComputeLocalToWorld()
        {
            CheckReadAccess();
            // TODO DOTS-10269: CheckHierarchyReadAccess();
            return m_TransformUnion->ComputeLocalToWorld();
        }

        /// <summary>
        /// Sets the local (parent-relative) position, rotation, and scale to the values encoded in a 4x4 local-to-parent matrix.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// AffineTransform.decompose(localToParentMatrix, out float3 position, out quaternion rotation, out float3 scale);
        /// transformRef.LocalPosition = position;
        /// transformRef.LocalRotation = rotation;
        /// transformRef.LocalScale = scale;
        /// </code>
        /// </remarks>
        /// <param name="localToParentMatrix">A matrix representing the parent-relative affine transformation.</param>
        public void SetLocalTransform(float4x4 localToParentMatrix)
        {
            CheckWriteAccess();
            m_TransformUnion->SetLocalTransform(localToParentMatrix);
        }

        /// <summary>
        /// Sets the local (parent-relative) position, rotation, and scale to the provided values.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// transformRef.LocalPosition = position;
        /// transformRef.LocalRotation = rotation;
        /// transformRef.LocalScale = scale;
        /// </code>
        /// </remarks>
        /// <param name="position">The new parent-relative position</param>
        /// <param name="rotation">The new parent-relative rotation</param>
        /// <param name="scale">The new parent-relative scale</param>
        public void SetLocalTransform(float3 position, quaternion rotation, float3 scale)
        {
            CheckWriteAccess();
            m_TransformUnion->SetLocalTransform(position, rotation, scale);
        }

        /// <summary>
        /// Gets the local (parent-relative) position and rotation.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// position = transformRef.LocalPosition;
        /// rotation = transformRef.LocalRotation;
        /// </code>
        /// </remarks>
        /// <param name="position">The parent-relative position will be stored here</param>
        /// <param name="rotation">The parent-relative rotation will be stored here</param>
        public void GetLocalPositionAndRotation(out float3 position, out quaternion rotation)
        {
            CheckReadAccess();
            m_TransformUnion->GetLocalPositionAndRotation(out position, out rotation);
        }

        /// <summary>
        /// Sets the local (parent-relative) position and rotation to the provided values.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// transformRef.LocalPosition = position;
        /// transformRef.LocalRotation = rotation;
        /// </code>
        /// </remarks>
        /// <seealso cref="SetLocalTransform(float3, quaternion, float3)"/>
        /// <seealso cref="SetLocalTransform(float4x4)"/>
        /// <param name="position">The new parent-relative position</param>
        /// <param name="rotation">The new parent-relative rotation</param>
        public void SetLocalPositionAndRotation(float3 position, quaternion rotation)
        {
            CheckWriteAccess();
            m_TransformUnion->SetLocalPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Gets the world-space position and rotation.
        /// </summary>
        /// <remarks>
        /// This operation is significantly more expensive than the local-space equivalent. Its execution
        /// time is proporitional to the depth of this object in its transform hierarchy. Only use it
        /// when the call site specifically needs world-space transform data for objects that may be in
        /// a hierarchy.
        ///
        /// The world-space scale can only be computed approximately in the general case; see <see cref="GetWorldLossyScale"/>.
        /// </remarks>
        /// <seealso cref="GetLocalPositionAndRotation(float3, quaternion)"/>
        /// <param name="position">The world-space position will be stored here</param>
        /// <param name="rotation">The world-space rotation will be stored here</param>
        public void GetWorldPositionAndRotation(out float3 position, out quaternion rotation)
        {
            CheckReadAccess();
            m_TransformUnion->GetWorldPositionAndRotation(out position, out rotation);
        }

        /// <summary>
        /// Sets the world-space position and rotation to the provided values.
        /// </summary>
        /// <remarks>
        /// This operation is significantly more expensive than the local-space equivalent. Its execution
        /// time is proporitional to the depth of this object in its transform hierarchy. Only use it
        /// when the call site specifically needs world-space transform data for objects that may be in
        /// a hierarchy.
        /// </remarks>
        /// <seealso cref="SetLocalPositionAndRotation(float3, quaternion)"/>
        /// <param name="position">The new world-space position</param>
        /// <param name="rotation">The new world-space rotation</param>
        public void SetWorldPositionAndRotation(float3 position, quaternion rotation)
        {
            CheckWriteAccess();
            m_TransformUnion->SetWorldPositionAndRotation(position, rotation);
        }

        internal void SetParent(EntityComponentStore* componentStore, TransformRef newParent, Entity parentEntity, Entity childEntity, bool preserveWorldTransform = true)
        {
            CheckWriteAccess();
            if (parentEntity != Entity.Null)
                newParent.CheckWriteAccess();
            m_TransformUnion->SetParent(componentStore, newParent.m_TransformUnion, parentEntity, childEntity, preserveWorldTransform);
        }


        internal void QueueTransformDispatch()
        {
            m_TransformUnion->QueueTransformDispatch();
        }

        /// <summary>
        /// Returns whether this transform is currently part of a hierarchy.
        /// </summary>
        /// <remarks>
        /// A transform that was never parented or has no children will not be part of a hierarchy.
        /// </remarks>
        public bool HasHierarchy
        {
            get
            {
                CheckReadAccess();
                return m_TransformUnion->HasHierarchy;
            }
        }

        /// <summary>
        /// Gets the parent entity of this transform.
        /// </summary>
        /// <remarks>
        /// Returns Entity.Null if this transform is a root transform or is not currently part of a hierarchy.
        /// </remarks>
        /// <returns>The parent Entity, or Entity.Null if no parent exists.</returns>
        public Entity GetParent()
        {
            CheckReadAccess();
            return m_TransformUnion->GetParent();
        }

        /// <summary>
        /// Gets the number of direct children of this transform.
        /// </summary>
        /// <remarks>
        /// Returns 0 if this transform is not currently part of a hierarchy.
        /// </remarks>
        /// <returns>The number of direct children.</returns>
        public int GetChildCount()
        {
            CheckReadAccess();
            return m_TransformUnion->GetChildCount();
        }

        /// <summary>
        /// Gets the child entity at the specified index.
        /// </summary>
        /// <remarks>
        /// Returns Entity.Null if the index is out of bounds or this transform is not part of a hierarchy.
        /// </remarks>
        /// <param name="index">The zero-based index of the child.</param>
        /// <returns>The child Entity, or Entity.Null if invalid.</returns>
        public Entity GetChild(int index)
        {
            CheckReadAccess();
            return m_TransformUnion->GetChild(index);
        }

        /// <summary>
        /// Gets all child entities.
        /// </summary>
        /// <remarks>
        /// This fetches all children in a single call.
        ///
        /// The caller is responsible for disposing the returned NativeArray.
        ///
        /// Usage:
        /// <code>
        /// using (var children = transformRef.GetChildEntities(Allocator.Temp))
        /// {
        ///     foreach (Entity child in children)
        ///     {
        ///         // Process child
        ///     }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="allocator">The allocator to use for the returned array.</param>
        /// <returns>A NativeArray containing all direct child entities.</returns>
        public Unity.Collections.NativeArray<Entity> GetChildEntities(Unity.Collections.Allocator allocator)
        {
            CheckReadAccess();
            return m_TransformUnion->GetChildEntities(allocator);
        }
    }
}
