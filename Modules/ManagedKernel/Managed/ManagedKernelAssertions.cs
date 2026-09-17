// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;

namespace ManagedKernel.Assertions
{
    [DebuggerStepThrough]
    internal static class Assert
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsTrue(bool condition)
        {
            if (!condition)
                Fail("Assertion failed: Expected true but was false", null);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
                Fail("Assertion failed: Expected true but was false", message);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsFalse(bool condition)
        {
            if (condition)
                Fail("Assertion failed: Expected false but was true", null);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
                Fail("Assertion failed: Expected false but was true", message);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual(int expected, int actual)
        {
            if (expected != actual)
                Fail("Assertion failed: Expected values to be equal", null);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual(int expected, int actual, string message)
        {
            if (expected != actual)
                Fail("Assertion failed: Expected values to be equal", message);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual<T>(T expected, T actual)
        {
            AreEqual(expected, actual, null);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            AreEqual(expected, actual, message, EqualityComparer<T>.Default);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual<T>(T expected, T actual, string message, IEqualityComparer<T> comparer)
        {
            if (!comparer.Equals(actual, expected))
                Fail($"Assertion failed: Expected <{expected}> but was <{actual}>", message);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreNotEqual<T>(T expected, T actual)
        {
            AreNotEqual(expected, actual, null);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreNotEqual<T>(T expected, T actual, string message)
        {
            AreNotEqual(expected, actual, message, EqualityComparer<T>.Default);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreNotEqual<T>(T expected, T actual, string message, IEqualityComparer<T> comparer)
        {
            if (comparer.Equals(actual, expected))
                Fail($"Assertion failed: Expected not equal to <{expected}> but was <{actual}>", message);
        }

        [BurstDiscard]
        static void Fail(string assertionMessage, string userMessage)
        {
            var message = assertionMessage;
            if (userMessage != null)
                message = userMessage + "\n" + assertionMessage;

            throw new InvalidOperationException(message);
        }
    }
}
