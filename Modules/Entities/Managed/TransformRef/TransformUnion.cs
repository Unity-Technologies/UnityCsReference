// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;


namespace Unity.Entities
{
    /// <summary>
    /// Provides callback registration for native code to update TransformUnion components.
    /// </summary>
    [BurstCompile]
    internal static unsafe class TransformUnionCallbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UpdateTransformUnionsDelegate(IntPtr entityComponentStore, IntPtr entityIds, IntPtr hierarchy, IntPtr indices, int count);
        [NoAutoStaticsCleanup] // TypeManager.Shutdown calls Shutdown.
        private static bool s_Initialized;

        internal static void Initialize()
        {
            if (s_Initialized)
                return;

            var updateFnPtr = BurstCompiler.CompileFunctionPointer<UpdateTransformUnionsDelegate>(UpdateTransformUnionsCallback);
            HierarchyTransformAccess.SetUpdateTransformUnionsCallback(updateFnPtr.Value);
            s_Initialized = true;
        }

        internal static void Shutdown()
        {
            HierarchyTransformAccess.SetUpdateTransformUnionsCallback(default);
            s_Initialized = false;
        }

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(UpdateTransformUnionsDelegate))]
        private static void UpdateTransformUnionsCallback(IntPtr entityComponentStorePtr,
            IntPtr entityIdsPtr, IntPtr hierarchyPtr, IntPtr indicesPtr, int count)
        {
            Entity* entityIds = (Entity*)entityIdsPtr;
            int* indices = (int*)indicesPtr;

            EntityComponentStore* pComponentStore = (EntityComponentStore*)entityComponentStorePtr;
            TypeIndex typeIndex = TypeManager.GetTypeIndex<TransformRef>();
            for (int i = 0; i < count; i++)
            {
                Entity entity = entityIds[i];
                var union = (TransformUnion*)pComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    pComponentStore->GlobalSystemVersion);
                union->_UnsafeTransformHierarchyPointer = (void*)hierarchyPtr;
                union->_IndexInHierarchy = indices[i];
            }
        }

    }

    /// <summary>
    /// Transform data requires storing data in a private native format, and providing a
    /// public interface to manipulate it. Transform data may be stored directly in the
    /// chunk for independent transforms, or in a native TransformHierarchy if the entity
    /// belongs to a hierarchy.
    /// </summary>
    [StructLayout((LayoutKind.Explicit))]
    [NoAlias]
    internal unsafe struct TransformUnion : IComponentData
    {
        // union of

        // Transform belongs in hierarchy
        [FieldOffset(0)] public void* _UnsafeTransformHierarchyPointer;                 // 08 bytes
        [FieldOffset(8)] public int _IndexInHierarchy;                                  // 04 bytes
        [FieldOffset(12)] public byte _IsDirty;                                         // 01 byte
        // ------------------------------

        // or

        // "Loose" Transform has no Parent or Children
        [FieldOffset(0)] public quaternion _Rotation;                                   // 16 bytes
        [FieldOffset(16)] public float3 _Translation;                                   // 12 bytes
        [FieldOffset(28)] public float3 _Scale;                                         // 12 bytes
        //--------------------------------------------

        // TODO DOTS-10285: find somewhere to pack this
        [FieldOffset(40)] public byte _HasHierarchy;                                    // 01 byte

        private HierarchyTransformAccess GetTransformAccess()
        {
            return new HierarchyTransformAccess
            {
                Hierarchy = (IntPtr)_UnsafeTransformHierarchyPointer,
                Index = _IndexInHierarchy
            };
        }

        public float3 Position
        {
            get
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    return uta.localPosition;
                }
                else
                {
                    return _Translation;
                }
            }
            set
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    uta.localPosition = value;
                    _IsDirty = 1;
                }
                else
                {
                    _Translation = value;
                }
            }
        }

        public quaternion Rotation {
            get
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    return uta.localRotation;
                }
                else
                {
                    return _Rotation;
                }
            }
            set
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    uta.localRotation = value;
                    _IsDirty = 1;
                }
                else
                {
                    _Rotation = value;
                }
            }
        }

        public float3 Scale
        {
            get
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    return uta.localScale;
                }
                else
                {
                    return _Scale;
                }
            }
            set
            {
                if (_HasHierarchy != 0)
                {
                    var uta = GetTransformAccess();
                    uta.localScale = value;
                    _IsDirty = 1;
                }
                else
                {
                    _Scale = value;
                }
            }
        }

        public float4x4 ComputeLocalToWorld()
        {
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                return uta.localToWorldMatrix;
            }
            else
            {
                return float4x4.TRS(Position, Rotation, Scale);
            }
        }

        /// <summary>
        /// Sets the local (parent-relative) position, rotation, and scale to the values encoded in a 4x4 local-to-parent matrix.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// AffineTransform.decompose(localToParentMatrix, out float3 position, out quaternion rotation, out float3 scale);
        /// transformUnion.Position = position;
        /// transformUnion.Rotation = rotation;
        /// transformUnion.Scale = scale;
        /// </code>
        /// </remarks>
        /// <param name="localToParentMatrix">A matrix representing the parent-relative affine transformation.</param>
        public void SetLocalTransform(float4x4 localToParentMatrix)
        {
            float3x3 rs = math.float3x3(localToParentMatrix.c0.xyz, localToParentMatrix.c1.xyz, localToParentMatrix.c2.xyz);
            quaternion r = math.rotation(rs);
            float3x3 sm = math.mul(math.float3x3(math.conjugate(r)), rs);
            SetLocalTransform(localToParentMatrix.c3.xyz,
                r,
                math.float3(sm.c0.x, sm.c1.y, sm.c2.z));
        }

        /// <summary>
        /// Sets the local (parent-relative) position, rotation, and scale to the provided values.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// transformUnion.Position = position;
        /// transformUnion.Rotation = rotation;
        /// transformUnion.Scale = scale;
        /// </code>
        /// </remarks>
        /// <param name="position">The new parent-relative position</param>
        /// <param name="rotation">The new parent-relative rotation</param>
        /// <param name="scale">The new parent-relative scale</param>
        public void SetLocalTransform(float3 position, quaternion rotation, float3 scale)
        {
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                uta.SetLocalPositionAndRotation(position, rotation);
                uta.localScale = scale;
                _IsDirty = 1;
            }
            else
            {
                _Translation = position;
                _Rotation = rotation;
                _Scale = scale;
            }
        }

        /// <summary>
        /// Gets the local (parent-relative) position and rotation.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// position = transformUnion.Position;
        /// rotation = transformUnion.Rotation;
        /// </code>
        /// </remarks>
        /// <param name="position">The parent-relative position will be stored here</param>
        /// <param name="rotation">The parent-relative rotation will be stored here</param>
        public void GetLocalPositionAndRotation(out float3 position, out quaternion rotation)
        {
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                uta.GetLocalPositionAndRotation(out position, out rotation);
            }
            else
            {
                position = _Translation;
                rotation = _Rotation;
            }
        }

        /// <summary>
        /// Sets the local (parent-relative) position and rotation to the provided values.
        /// </summary>
        /// <remarks>
        /// This function gives identical results to the following operations, but is more efficient:
        /// <code>
        /// transformUnion.Position = position;
        /// transformUnion.Rotation = rotation;
        /// </code>
        /// </remarks>
        /// <param name="position">The new parent-relative position</param>
        /// <param name="rotation">The new parent-relative rotation</param>
        public void SetLocalPositionAndRotation(float3 position, quaternion rotation)
        {
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                uta.SetLocalPositionAndRotation(position, rotation);
                _IsDirty = 1;
            }
            else
            {
                _Translation = position;
                _Rotation = rotation;
            }
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
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                uta.GetWorldPositionAndRotation(out position, out rotation);
            }
            else
            {
                position = _Translation;
                rotation = _Rotation;
            }
        }

        /// <summary>
        /// Sets the world-space position and rotation to the provided values.
        /// </summary>
        /// <remarks>
        /// This operation is significantly more expensive than the local-space equivalent. Its execution
        /// time is proporitional to the depth of this object in its transform hierarchy. Only use it
        /// when the call site specifically needs world-space transform data.
        /// </remarks>
        /// <seealso cref="SetLocalPositionAndRotation(float3, quaternion)"/>
        /// <param name="position">The new world-space position</param>
        /// <param name="rotation">The new world-space rotation</param>
        public void SetWorldPositionAndRotation(float3 position, quaternion rotation)
        {
            if (_HasHierarchy != 0)
            {
                var uta = GetTransformAccess();
                uta.SetWorldPositionAndRotation(position, rotation);
                _IsDirty = 1;
            }
            else
            {
                _Translation = position;
                _Rotation = rotation;
            }
        }


        public void SetParent(EntityComponentStore* componentStore, TransformUnion* parent,
            Entity parentEntity, Entity childEntity, bool preserveWorldTransform = true)
        {
            HierarchyTransformAccess parentAccess = default;
            if (parent != null)
            {
                if (parent->_HasHierarchy == 0)
                {
                    // TODO DOTS-10288: get a better initial capacity from number of initial deep child count.
                    parentAccess = HierarchyTransformAccess.CreateHierarchy((IntPtr)componentStore,
                        parent->Position, parent->Rotation, parent->Scale, Unsafe.As<Entity, ulong>(ref parentEntity),
                        capacity:4);
                    parent->_HasHierarchy = 1;
                    parent->_UnsafeTransformHierarchyPointer = (void*)parentAccess.Hierarchy;
                    parent->_IndexInHierarchy = parentAccess.Index;
                }
                else
                {
                    parentAccess = parent->GetTransformAccess();
                }
            }

            if (_HasHierarchy == 0)
            {
                if (parent == null)
                {
                    return;
                }
                float3 position = _Translation;
                quaternion rotation = _Rotation;
                float3 scale = _Scale;
                if (preserveWorldTransform)
                {
                    // TODO DOTS-10289: find the most numerically stable way to do this.
                    var ltw = float4x4.TRS(Position, Rotation, Scale);
                    float4x4 parentWtl = parentAccess.worldToLocalMatrix;
                    float4x4 newLocal = math.mul(parentWtl, ltw);

                    position = newLocal.c3.xyz;
                    rotation = new quaternion(math.orthonormalize(new float3x3(newLocal)));
                    scale = new float3(math.length(newLocal.c0.xyz), math.length(newLocal.c1.xyz), math.length(newLocal.c2.xyz));
                }

                var childAccess = HierarchyTransformAccess.SetParent((IntPtr)componentStore,
                    parentAccess, position, rotation, scale, Unsafe.As<Entity, ulong>(ref childEntity));
                _HasHierarchy = 1;
                _IsDirty = 1;
                _UnsafeTransformHierarchyPointer = (void*)childAccess.Hierarchy;
                _IndexInHierarchy = childAccess.Index;
            }
            else
            {
                var childAccess = GetTransformAccess();
                if (preserveWorldTransform)
                {
                    var ltw = ComputeLocalToWorld();
                    float4x4 newLocal = ltw;
                    if (parent != null)
                    {
                        float4x4 parentWtl = parentAccess.worldToLocalMatrix;
                        newLocal = math.mul(parentWtl, ltw);
                    }

                    childAccess.localPosition = newLocal.c3.xyz;
                    childAccess.localRotation = new quaternion(math.orthonormalize(new float3x3(newLocal)));
                    childAccess.localScale = new float3(math.length(newLocal.c0.xyz), math.length(newLocal.c1.xyz), math.length(newLocal.c2.xyz));
                }

                // TODO DOTS-10361: If parent == null and deepChildCount == 1 then destroy
                //  TransformHierarchy and store in chunk

                var newAccess = HierarchyTransformAccess.SetParent((IntPtr)componentStore,
                    parentAccess, childAccess);
                _UnsafeTransformHierarchyPointer = (void*)newAccess.Hierarchy;
                _IndexInHierarchy = newAccess.Index;
                _IsDirty = 1;
            }
        }

        static void Internal_UpdateTransformUnions(IntPtr entityComponentStorePtr,
            IntPtr entityIdsPtr, IntPtr hierarchyPtr, IntPtr indicesPtr, int count)
        {
            Entity* entityIds = (Entity*)entityIdsPtr;
            int* indices = (int*)indicesPtr;

            EntityComponentStore* pComponentStore = (EntityComponentStore*)entityComponentStorePtr;
            TypeIndex typeIndex = TypeManager.GetTypeIndex<TransformRef>();
            for (int i = 0; i < count; i++)
            {
                Entity entity = entityIds[i];
                var union = (TransformUnion*)pComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    pComponentStore->GlobalSystemVersion);
                union->_UnsafeTransformHierarchyPointer = (void*)hierarchyPtr;
                union->_IndexInHierarchy = indices[i];
            }
        }

        internal JobHandle GetHierarchyDependency()
        {
            var uta = GetTransformAccess();
            return uta.GetHierarchyDependency();
        }

        internal void QueueTransformDispatch()
        {
            if (_HasHierarchy != 0 && _IsDirty != 0)
            {
                var uta = GetTransformAccess();
                uta.QueueTransformDispatch();
                _IsDirty = 0;
            }
        }

        /// <summary>
        /// Returns whether this transform is currently part of a hierarchy.
        /// </summary>
        internal bool HasHierarchy => _HasHierarchy != 0;

        /// <summary>
        /// Gets the parent entity of this transform.
        /// </summary>
        /// <returns>The parent Entity, or Entity.Null if this is a root or not in a hierarchy.</returns>
        internal Entity GetParent()
        {
            if (_HasHierarchy == 0)
                return Entity.Null;

            var uta = GetTransformAccess();
            ulong parentEntityRef = uta.GetParentEntityReference();

            if (parentEntityRef == 0)
                return Entity.Null;

            return UnsafeUtility.As<ulong, Entity>(ref parentEntityRef);
        }

        /// <summary>
        /// Gets the number of direct children of this transform.
        /// </summary>
        /// <returns>The child count, or 0 if not in a hierarchy.</returns>
        internal int GetChildCount()
        {
            if (_HasHierarchy == 0)
                return 0;

            var uta = GetTransformAccess();
            return uta.GetChildCount();
        }

        /// <summary>
        /// Gets the child entity at the specified index.
        /// </summary>
        /// <param name="index">The index of the child (0-based).</param>
        /// <returns>The child Entity, or Entity.Null if index is out of bounds or not in a hierarchy.</returns>
        internal Entity GetChild(int index)
        {
            if (_HasHierarchy == 0)
                return Entity.Null;

            var uta = GetTransformAccess();
            int childCount = uta.GetChildCount();
            if (index < 0 || index >= childCount)
                return Entity.Null;

            int childHierarchyIndex = uta.GetChildIndex(index);
            if (childHierarchyIndex < 0)
                return Entity.Null;

            ulong childEntityRef = uta.GetEntityReferenceAtIndex(childHierarchyIndex);
            if (childEntityRef == 0)
                return Entity.Null;

            return UnsafeUtility.As<ulong, Entity>(ref childEntityRef);
        }

        /// <summary>
        /// Gets all child entities in a single native call (more efficient than iterating with GetChild).
        /// </summary>
        /// <param name="allocator">The allocator to use for the returned array.</param>
        /// <returns>A NativeArray containing all direct child entities. Returns an empty array if not in a hierarchy.</returns>
        internal Unity.Collections.NativeArray<Entity> GetChildEntities(Unity.Collections.Allocator allocator)
        {
            if (_HasHierarchy == 0)
                return new Unity.Collections.NativeArray<Entity>(0, allocator);

            var uta = GetTransformAccess();
            int childCount = uta.GetChildCount();
            if (childCount == 0)
                return new Unity.Collections.NativeArray<Entity>(0, allocator);

            var result = new Unity.Collections.NativeArray<Entity>(childCount, allocator, Unity.Collections.NativeArrayOptions.UninitializedMemory);

            // Entity is 8 bytes (Index:int + Version:int), same as ulong
            // We can safely reinterpret the memory
            ulong* entityPtr = (ulong*)result.GetUnsafePtr();
            uta.GetChildEntities(entityPtr, childCount);

            return result;
        }
    }
}
