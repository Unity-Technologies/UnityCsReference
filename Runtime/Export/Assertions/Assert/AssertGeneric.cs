// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Assertions
{
    public static partial class Assert
    {
        [Conditional(UNITY_ASSERTIONS)]
        public static void AreEqual<T>(T expected, T actual)
        {
            AreEqual(expected, actual, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            AreEqual(expected, actual, message, EqualityComparer<T>.Default);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreEqual<T>(T expected, T actual, string message, IEqualityComparer<T> comparer)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                AreEqual(expected as UnityEngine.Object, actual as UnityEngine.Object, message);
                return;
            }
            if (!comparer.Equals(actual, expected))
                Fail(AssertionMessageUtil.GetEqualityMessage(actual, expected, true), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreEqual(UnityEngine.Object expected, UnityEngine.Object actual, string message)
        {
            if (actual != expected)
                Fail(AssertionMessageUtil.GetEqualityMessage(actual, expected, true), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotEqual<T>(T expected, T actual)
        {
            AreNotEqual(expected, actual, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotEqual<T>(T expected, T actual, string message)
        {
            AreNotEqual(expected, actual, message, EqualityComparer<T>.Default);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotEqual<T>(T expected, T actual, string message, IEqualityComparer<T> comparer)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                AreNotEqual(expected as UnityEngine.Object, actual as UnityEngine.Object, message);
                return;
            }
            if (comparer.Equals(actual, expected))
                Fail(AssertionMessageUtil.GetEqualityMessage(actual, expected, false), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotEqual(UnityEngine.Object expected, UnityEngine.Object actual, string message)
        {
            if (actual == expected)
                Fail(AssertionMessageUtil.GetEqualityMessage(actual, expected, false), message);
        }
    }
}
