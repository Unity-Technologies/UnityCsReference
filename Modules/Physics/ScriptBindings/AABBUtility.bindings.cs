// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/AABBUtility.h")]
    internal class AABBUtility
    {
        [StaticAccessor("", StaticAccessorType.DoubleColon)]
        extern public static bool CalculateLocalAABBFromGameObject(GameObject go, ref Bounds bounds);

        [StaticAccessor("", StaticAccessorType.DoubleColon)]
        extern public static bool CalculateCombinedAABBFromHierarchy(GameObject go, ref Bounds bounds, bool includeRoot = false);
    }
}
