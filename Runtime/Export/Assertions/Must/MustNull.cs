// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.Assertions.Must
{
    public static partial class MustExtensions
    {
        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeNull<T>(this T expected) where T : class
        {
            Assert.IsNull(expected);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeNull<T>(this T expected, string message) where T : class
        {
            Assert.IsNull(expected, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeNull<T>(this T expected) where T : class
        {
            Assert.IsNotNull(expected);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeNull<T>(this T expected, string message) where T : class
        {
            Assert.IsNotNull(expected, message);
        }
    }
}
