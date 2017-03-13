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
        public static void MustBeApproximatelyEqual(this float actual, float expected)
        {
            Assert.AreApproximatelyEqual(actual, expected);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeApproximatelyEqual(this float actual, float expected, string message)
        {
            Assert.AreApproximatelyEqual(actual, expected, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeApproximatelyEqual(this float actual, float expected, float tolerance)
        {
            Assert.AreApproximatelyEqual(actual, expected, tolerance);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeApproximatelyEqual(this float actual, float expected, float tolerance, string message)
        {
            Assert.AreApproximatelyEqual(expected, actual, tolerance, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeApproximatelyEqual(this float actual, float expected)
        {
            Assert.AreNotApproximatelyEqual(expected, actual);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeApproximatelyEqual(this float actual, float expected, string message)
        {
            Assert.AreNotApproximatelyEqual(expected, actual, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeApproximatelyEqual(this float actual, float expected, float tolerance)
        {
            Assert.AreNotApproximatelyEqual(expected, actual, tolerance);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustNotBeApproximatelyEqual(this float actual, float expected, float tolerance, string message)
        {
            Assert.AreNotApproximatelyEqual(expected, actual, tolerance, message);
        }
    }
}
