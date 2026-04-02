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
    }
}
