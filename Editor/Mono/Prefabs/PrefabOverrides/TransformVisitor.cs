// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.SceneManagement
{
    class TransformVisitor
    {
        public void VisitAll(Transform transform, Action<Transform, object> visitorFunc, object userData)
        {
            Assert.IsNotNull(transform, "Please provide a valid transform");
            Assert.IsNotNull(visitorFunc, "Please provide a valid visitorFunc");

            VisitAllRecursively(transform, visitorFunc, userData);
        }

        static void VisitAllRecursively(Transform transform, Action<Transform, object> visitorFunc, object userData)
        {
            visitorFunc(transform, userData);
            for (int i = 0; i < transform.childCount; ++i)
                VisitAllRecursively(transform.GetChild(i), visitorFunc, userData);
        }

        // Let visitorFunc return true for continue visiting, false for early out.
        public void VisitAndAllowEarlyOut(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            Assert.IsNotNull(transform, "Please provide a valid transform");
            Assert.IsNotNull(visitorFunc, "Please provide a valid visitorFunc");

            VisitAndAllowEarlyOutRecursively(transform, visitorFunc, userData);
        }

        static bool VisitAndAllowEarlyOutRecursively(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            if (!visitorFunc(transform, userData))
                return false;

            for (int i = 0; i < transform.childCount; ++i)
            {
                if (!VisitAndAllowEarlyOutRecursively(transform.GetChild(i), visitorFunc, userData))
                    return false;
            }
            return true;
        }

        // Let visitorFunc return true for entering children, false for skipping them.
        public void VisitAndConditionallyEnterChildren(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            Assert.IsNotNull(transform, "Please provide a valid transform");
            Assert.IsNotNull(visitorFunc, "Please provide a valid visitorFunc");

            VisitAndConditionallyEnterChildrenRecursively(transform, visitorFunc, userData);
        }

        static void VisitAndConditionallyEnterChildrenRecursively(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            if (!visitorFunc(transform, userData))
                return;

            for (int i = 0; i < transform.childCount; ++i)
                VisitAndConditionallyEnterChildrenRecursively(transform.GetChild(i), visitorFunc, userData);
        }

        // Only visit transforms under the same PrefabInstance
        public void VisitPrefabInstanceTransforms(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            Assert.IsNotNull(transform, "Please provide a valid transform");
            Assert.IsNotNull(visitorFunc, "Please provide a valid visitorFunc");
            Assert.IsNotNull(PrefabUtility.GetPrefabInstanceHandle(transform), "Please provide a Prefab instance");

            VisitPrefabInstanceTransformsRecursively(transform, visitorFunc, userData);
        }

        static void VisitPrefabInstanceTransformsRecursively(Transform transform, Func<Transform, object, bool> visitorFunc, object userData)
        {
            if (!visitorFunc(transform, userData))
                return;

            var prefabInstanceHandle = PrefabUtility.GetPrefabInstanceHandle(transform);
            for (int i = 0; i < transform.childCount; ++i)
            {
                var child = transform.GetChild(i);
                if (PrefabUtility.GetPrefabInstanceHandle(child) == prefabInstanceHandle)
                    VisitPrefabInstanceTransformsRecursively(child, visitorFunc, userData);
            }
        }
    }
}
