// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Jobs;

namespace UnityEngine
{
    internal struct TransformHierarchy
    {
        [FreeFunction("TransformHierarchyBindings::SetUpdateTransformUnionsCallback", HasExplicitThis = false)]
        internal static extern void SetUpdateTransformUnionsCallback(IntPtr callback);

        [FreeFunction("TransformHierarchyBindings::CreateNewHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess CreateHierarchy(IntPtr entityComponentStore,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 entity, uint capacity);

        [FreeFunction("TransformHierarchyBindings::SetParent_Internal_WithoutHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess SetParent_Internal_WithoutHierarchy(IntPtr entityComponentStore,
            UnsafeTransformAccess unsafeTransformAccess,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 childEntity);

        [FreeFunction("TransformHierarchyBindings::SetParent_Internal_WithHierarchy", HasExplicitThis = false)]
        internal static extern UnsafeTransformAccess SetParent_Internal_WithHierarchy(IntPtr entityComponentStore,
            UnsafeTransformAccess parentTransformAccess,
            UnsafeTransformAccess childTransformAccess);

        internal static UnsafeTransformAccess SetParent(IntPtr entityComponentStore,
            UnsafeTransformAccess unsafeTransformAccess,
            Vector3 worldPosition, Quaternion rotation, Vector3 scale, UInt64 childEntity)
        {
            return SetParent_Internal_WithoutHierarchy(entityComponentStore,
                unsafeTransformAccess,
                worldPosition, rotation, scale, childEntity);
        }

        internal static UnsafeTransformAccess SetParent(IntPtr entityComponentStore,
            UnsafeTransformAccess parentTransformAccess,
            UnsafeTransformAccess childTransformAccess)
        {
            return SetParent_Internal_WithHierarchy(entityComponentStore, parentTransformAccess, childTransformAccess);
        }

        // Hierarchy traversal functions for TransformRef
        // These are marked IsThreadSafe=true to allow calling from jobs. Thread safety is ensured by:
        // 1. These functions only read from the hierarchy arrays (parentIndices, childIndices, mainThreadOnlyEntityReferences)
        // 2. Structural changes (SetParent, DetachChildren) complete all pending jobs before modifying the hierarchy
        // 3. The AtomicSafetyHandle on TransformTypeHandle prevents concurrent read/write access to the component data
        // Note: these are _not_ safe to use with hierarchies containing GameObjects off the main thread yet (DOTS-10269).
        [FreeFunction("TransformHierarchyBindings::GetParentIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetParentIndex(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetParentEntityReference", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern ulong GetParentEntityReference(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetChildCount", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetChildCount(UnsafeTransformAccess access);

        [FreeFunction("TransformHierarchyBindings::GetChildIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern int GetChildIndex(UnsafeTransformAccess access, int childPosition);

        [FreeFunction("TransformHierarchyBindings::GetEntityReferenceAtIndex", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern ulong GetEntityReferenceAtIndex(UnsafeTransformAccess access, int index);

        // Batch function to get all child entities at once (more efficient than iterating)
        [FreeFunction("TransformHierarchyBindings::GetChildEntities", HasExplicitThis = false, IsThreadSafe = true)]
        internal static extern unsafe int GetChildEntities(UnsafeTransformAccess access, ulong* outChildEntities, int maxCount);
    }
}
