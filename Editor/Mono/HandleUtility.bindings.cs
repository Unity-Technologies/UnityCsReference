// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/HandleUtility.bindings.h")]
    [StaticAccessor("HandleUtilityBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class HandleUtility
    {
        // Calculate distance between a point and a bezier
        public static extern float DistancePointBezier(Vector3 point, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent);

        internal static extern GameObject[] Internal_PickRectObjects(Camera cam, Rect rect, bool selectPrefabRoots);

        internal static extern bool Internal_FindNearestVertex(Camera cam, Vector2 screenPoint, Transform[] objectsToSearch, Transform[] ignoreObjects, out Vector3 vertex);
        internal static extern GameObject Internal_PickClosestGO(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);


        private static extern void Internal_SetHandleWireTextureIndex(int textureIndex, int samplerIndex);

        internal static extern float CalcRayPlaceOffset(Transform[] objects, Vector3 normal);

        private static extern void Internal_Repaint();

        // Register a callback for GfxDevice cleanup event. It checks if the callback wasn't already registered in native code.
        private static extern void RegisterGfxDeviceCleanupIfNeeded();

        // Test a mesh for intersection against a ray
        internal static extern bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit);
    }
}
