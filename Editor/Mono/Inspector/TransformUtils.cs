// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public static bool GetConstrainProportions(Transform transform)
        {
            return GetConstrainProportions(new []{transform});
        }

        public static bool GetConstrainProportions(Transform[] transforms)
        {
            return Selection.DoAllGOsHaveConstrainProportionsEnabled(transforms);
        }

        public static void SetConstrainProportions(Transform transform, bool enabled)
        {
            SetConstrainProportions(new[] { transform }, enabled);
        }

        public static void SetConstrainProportions(Transform[] transforms, bool enabled)
        {
            foreach (var t in transforms)
            {
                if (t == null)
                    throw new ArgumentNullException("transform", "One or more transforms are null");
            }

            ConstrainProportionsTransformScale.SetConstrainProportions(transforms, enabled);
        }
    }
}
