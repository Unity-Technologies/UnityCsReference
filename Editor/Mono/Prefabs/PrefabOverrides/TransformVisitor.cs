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
    }
}
