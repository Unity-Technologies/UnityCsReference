// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    /// <summary>
    /// A transform inside a native TransformHierarchy: the Entities module's counterpart of the Core module's
    /// UnsafeTransformAccess, typed with Unity.Mathematics. It calls the same native functions through
    /// EntitiesTransformBindings, which take values as pointers to float3, quaternion and float4x4.
    /// </summary>
    [NativeHeader("Modules/Entities/Public/HierarchyTransformAccess.bindings.h")]
    internal unsafe struct HierarchyTransformAccess
    {
        public IntPtr Hierarchy;
        public int Index;

        bool IsValid => Hierarchy != IntPtr.Zero;

        void CheckHierarchyValid()
        {
            if (!IsValid)
                throw new NullReferenceException("The TransformAccess is not valid and points to an invalid hierarchy");
        }

        public float4x4 localToWorldMatrix
        {
            get
            {
                CheckHierarchyValid();
                float4x4 m;
                GetLocalToWorldMatrix(Hierarchy, Index, &m);
                return m;
            }
        }

        public float4x4 worldToLocalMatrix
        {
            get
            {
                CheckHierarchyValid();
                float4x4 m;
                GetWorldToLocalMatrix(Hierarchy, Index, &m);
                return m;
            }
        }

        public float3 localPosition
        {
            get
            {
                CheckHierarchyValid();
                float3 p;
                GetLocalPosition(Hierarchy, Index, &p);
                return p;
            }
            set
            {
                CheckHierarchyValid();
                SetLocalPosition(Hierarchy, Index, &value);
            }
        }

        public quaternion localRotation
        {
            get
            {
                CheckHierarchyValid();
                quaternion r;
                GetLocalRotation(Hierarchy, Index, &r);
                return r;
            }
            set
            {
                CheckHierarchyValid();
                SetLocalRotation(Hierarchy, Index, &value);
            }
        }

        public float3 localScale
        {
            get
            {
                CheckHierarchyValid();
                float3 s;
                GetLocalScale(Hierarchy, Index, &s);
                return s;
            }
            set
            {
                CheckHierarchyValid();
                SetLocalScale(Hierarchy, Index, &value);
            }
        }

        public void SetWorldPositionAndRotation(float3 position, quaternion rotation)
        {
            CheckHierarchyValid();
            SetWorldPositionAndRotation(Hierarchy, Index, &position, &rotation);
        }

        public void SetLocalPositionAndRotation(float3 position, quaternion rotation)
        {
            CheckHierarchyValid();
            SetLocalPositionAndRotation(Hierarchy, Index, &position, &rotation);
        }

        public void GetWorldPositionAndRotation(out float3 position, out quaternion rotation)
        {
            CheckHierarchyValid();
            float3 p;
            quaternion r;
            GetWorldPositionAndRotation(Hierarchy, Index, &p, &r);
            position = p;
            rotation = r;
        }

        public void GetLocalPositionAndRotation(out float3 position, out quaternion rotation)
        {
            CheckHierarchyValid();
            float3 p;
            quaternion r;
            GetLocalPositionAndRotation(Hierarchy, Index, &p, &r);
            position = p;
            rotation = r;
        }

        public JobHandle GetHierarchyDependency()
        {
            CheckHierarchyValid();
            return GetHierarchyDependency(Hierarchy, Index);
        }

        public void QueueTransformDispatch()
        {
            CheckHierarchyValid();
            QueueTransformDispatch(Hierarchy, Index);
        }

        public ulong GetParentEntityReference() => IsValid ? GetParentEntityReference(Hierarchy, Index) : 0;

        public int GetChildCount() => IsValid ? GetChildCount(Hierarchy, Index) : 0;

        public int GetChildIndex(int childPosition) => IsValid ? GetChildIndex(Hierarchy, Index, childPosition) : -1;

        public ulong GetEntityReferenceAtIndex(int hierarchyIndex) => IsValid ? GetEntityReferenceAtIndex(Hierarchy, Index, hierarchyIndex) : 0;

        public int GetChildEntities(ulong* outChildEntities, int maxCount) => IsValid ? GetChildEntities(Hierarchy, Index, outChildEntities, maxCount) : 0;

        public static HierarchyTransformAccess CreateHierarchy(IntPtr entityComponentStore, float3 worldPosition, quaternion rotation, float3 scale, ulong entity, uint capacity)
        {
            var result = default(HierarchyTransformAccess);
            CreateHierarchy(entityComponentStore, &worldPosition, &rotation, &scale, entity, capacity, &result.Hierarchy, &result.Index);
            return result;
        }

        public static HierarchyTransformAccess SetParent(IntPtr entityComponentStore, HierarchyTransformAccess parent,
            float3 worldPosition, quaternion rotation, float3 scale, ulong childEntity)
        {
            var result = default(HierarchyTransformAccess);
            SetParentWithoutHierarchy(entityComponentStore, parent.Hierarchy, parent.Index,
                &worldPosition, &rotation, &scale, childEntity, &result.Hierarchy, &result.Index);
            return result;
        }

        public static HierarchyTransformAccess SetParent(IntPtr entityComponentStore, HierarchyTransformAccess parent, HierarchyTransformAccess child)
        {
            var result = default(HierarchyTransformAccess);
            SetParentWithHierarchy(entityComponentStore, parent.Hierarchy, parent.Index, child.Hierarchy, child.Index,
                &result.Hierarchy, &result.Index);
            return result;
        }

        [FreeFunction("EntitiesTransformBindings::GetLocalToWorldMatrix", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetLocalToWorldMatrix(IntPtr hierarchy, int index, void* matrix);

        [FreeFunction("EntitiesTransformBindings::GetWorldToLocalMatrix", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetWorldToLocalMatrix(IntPtr hierarchy, int index, void* matrix);

        [FreeFunction("EntitiesTransformBindings::GetLocalPosition", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetLocalPosition(IntPtr hierarchy, int index, void* position);

        [FreeFunction("EntitiesTransformBindings::SetLocalPosition", IsThreadSafe = true, ThrowsException = true)]
        static extern void SetLocalPosition(IntPtr hierarchy, int index, void* position);

        [FreeFunction("EntitiesTransformBindings::GetLocalRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetLocalRotation(IntPtr hierarchy, int index, void* rotation);

        [FreeFunction("EntitiesTransformBindings::SetLocalRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void SetLocalRotation(IntPtr hierarchy, int index, void* rotation);

        [FreeFunction("EntitiesTransformBindings::GetLocalScale", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetLocalScale(IntPtr hierarchy, int index, void* scale);

        [FreeFunction("EntitiesTransformBindings::SetLocalScale", IsThreadSafe = true, ThrowsException = true)]
        static extern void SetLocalScale(IntPtr hierarchy, int index, void* scale);

        [FreeFunction("EntitiesTransformBindings::SetWorldPositionAndRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void SetWorldPositionAndRotation(IntPtr hierarchy, int index, void* position, void* rotation);

        [FreeFunction("EntitiesTransformBindings::SetLocalPositionAndRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void SetLocalPositionAndRotation(IntPtr hierarchy, int index, void* position, void* rotation);

        [FreeFunction("EntitiesTransformBindings::GetWorldPositionAndRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetWorldPositionAndRotation(IntPtr hierarchy, int index, void* position, void* rotation);

        [FreeFunction("EntitiesTransformBindings::GetLocalPositionAndRotation", IsThreadSafe = true, ThrowsException = true)]
        static extern void GetLocalPositionAndRotation(IntPtr hierarchy, int index, void* position, void* rotation);

        [FreeFunction("EntitiesTransformBindings::GetHierarchyDependency", IsThreadSafe = true)]
        static extern JobHandle GetHierarchyDependency(IntPtr hierarchy, int index);

        [FreeFunction("EntitiesTransformBindings::QueueTransformDispatch", IsThreadSafe = false, ThrowsException = true)]
        static extern void QueueTransformDispatch(IntPtr hierarchy, int index);

        [FreeFunction("EntitiesTransformBindings::GetParentEntityReference", IsThreadSafe = true)]
        static extern ulong GetParentEntityReference(IntPtr hierarchy, int index);

        [FreeFunction("EntitiesTransformBindings::GetChildCount", IsThreadSafe = true)]
        static extern int GetChildCount(IntPtr hierarchy, int index);

        [FreeFunction("EntitiesTransformBindings::GetChildIndex", IsThreadSafe = true)]
        static extern int GetChildIndex(IntPtr hierarchy, int index, int childPosition);

        [FreeFunction("EntitiesTransformBindings::GetEntityReferenceAtIndex", IsThreadSafe = true)]
        static extern ulong GetEntityReferenceAtIndex(IntPtr hierarchy, int index, int hierarchyIndex);

        [FreeFunction("EntitiesTransformBindings::GetChildEntities", IsThreadSafe = true)]
        static extern int GetChildEntities(IntPtr hierarchy, int index, ulong* outChildEntities, int maxCount);

        [FreeFunction("EntitiesTransformBindings::CreateHierarchy")]
        static extern void CreateHierarchy(IntPtr entityComponentStore, void* worldPosition, void* rotation, void* scale,
            ulong entity, uint capacity, IntPtr* outHierarchy, int* outIndex);

        [FreeFunction("EntitiesTransformBindings::SetParentWithoutHierarchy")]
        static extern void SetParentWithoutHierarchy(IntPtr entityComponentStore, IntPtr parentHierarchy, int parentIndex,
            void* worldPosition, void* rotation, void* scale, ulong childEntity, IntPtr* outHierarchy, int* outIndex);

        [FreeFunction("EntitiesTransformBindings::SetParentWithHierarchy")]
        static extern void SetParentWithHierarchy(IntPtr entityComponentStore, IntPtr parentHierarchy, int parentIndex,
            IntPtr childHierarchy, int childIndex, IntPtr* outHierarchy, int* outIndex);

        [FreeFunction("EntitiesTransformBindings::SetUpdateTransformUnionsCallback")]
        internal static extern void SetUpdateTransformUnionsCallback(IntPtr callback);

        [FreeFunction("EntitiesTransformBindings::SetAssignTransformHierarchyToEntityCallback")]
        internal static extern void SetAssignTransformHierarchyToEntityCallback(IntPtr callback);
    }
}
