// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Assertions.Must
{
    [DebuggerStepThrough]
    [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
    public static partial class MustExtensions
    {
        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeEqual<T>(this T actual, T expected)
        {
            Assert.AreEqual(actual, expected);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeEqual<T>(this T actual, T expected, string message)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeEqual<T>(this T actual, T expected)
        {
            Assert.AreNotEqual(actual, expected);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeEqual<T>(this T actual, T expected, string message)
        {
            Assert.AreNotEqual(expected, actual, message);
        }
    }
}
