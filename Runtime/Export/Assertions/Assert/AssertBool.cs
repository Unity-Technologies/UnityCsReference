// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Assertions
{
    public static partial class Assert
    {
        [Conditional(UNITY_ASSERTIONS)]
        public static void IsTrue(bool condition)
        {
            IsTrue(condition, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
                Fail(AssertionMessageUtil.BooleanFailureMessage(true), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsFalse(bool condition)
        {
            IsFalse(condition, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
                Fail(AssertionMessageUtil.BooleanFailureMessage(false), message);
        }
    }
}
