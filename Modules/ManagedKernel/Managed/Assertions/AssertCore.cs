// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.Assertions
{
    // The part of UnityEngine.Assertions.Assert that never names UnityEngine.Object, for code below the Core module.
    [DebuggerStepThrough]
    [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.EntitiesModule")]
    internal static class AssertCore
    {
        const string UNITY_ASSERTIONS = "UNITY_ASSERTIONS";

        // Set by the Core module on code load, so failures honor Assert.raiseExceptions
        [NoAutoStaticsCleanup]
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static Action<string, string> s_Fail;

        static void Fail(string message, string userMessage)
        {
            if (s_Fail != null)
            {
                s_Fail(message, userMessage);
                return;
            }

            throw new AssertionException(message, userMessage);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsTrue(bool condition)
        {
            if (!condition)
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
            if (condition)
                IsFalse(condition, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
                Fail(AssertionMessageUtil.BooleanFailureMessage(false), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNull<T>(T value) where T : class
        {
            IsNull(value, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNull<T>(T value, string message) where T : class
        {
            if (value != null)
                Fail(AssertionMessageUtil.NullFailureMessage(value, true), message);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNotNull<T>(T value) where T : class
        {
            IsNotNull(value, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNotNull<T>(T value, string message) where T : class
        {
            if (value == null)
                Fail(AssertionMessageUtil.NullFailureMessage(value, false), message);
        }

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
            if (!comparer.Equals(actual, expected))
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
            if (comparer.Equals(actual, expected))
                Fail(AssertionMessageUtil.GetEqualityMessage(actual, expected, false), message);
        }
    }
}
