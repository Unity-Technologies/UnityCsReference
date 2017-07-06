// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public static class TransformUtils
    {
        public static Vector3 GetInspectorRotation(Transform t)
        {
            return t.GetLocalEulerAngles(t.rotationOrder);
        }

        public static void SetInspectorRotation(Transform t, Vector3 r)
        {
            t.SetLocalEulerAngles(r, t.rotationOrder);
        }
    }
}
