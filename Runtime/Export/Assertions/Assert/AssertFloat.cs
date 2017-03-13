// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

namespace UnityEngine.Assertions
{
    public static partial class Assert
    {
        [Conditional(UNITY_ASSERTIONS)]
        public static void AreApproximatelyEqual(float expected, float actual)
        {
            AreEqual(expected, actual, null, FloatComparer.s_ComparerWithDefaultTolerance);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreApproximatelyEqual(float expected, float actual, string message)
        {
            AreEqual(expected, actual, message, FloatComparer.s_ComparerWithDefaultTolerance);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance)
        {
            AreApproximatelyEqual(expected, actual, tolerance, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance, string message)
        {
            AreEqual(expected, actual, message, new FloatComparer(tolerance));
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotApproximatelyEqual(float expected, float actual)
        {
            AreNotEqual(expected, actual, null, FloatComparer.s_ComparerWithDefaultTolerance);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotApproximatelyEqual(float expected, float actual, string message)
        {
            AreNotEqual(expected, actual, message, FloatComparer.s_ComparerWithDefaultTolerance);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotApproximatelyEqual(float expected, float actual, float tolerance)
        {
            AreNotApproximatelyEqual(expected, actual, tolerance, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void AreNotApproximatelyEqual(float expected, float actual, float tolerance, string message)
        {
            AreNotEqual(expected, actual, message, new FloatComparer(tolerance));
        }
    }
}
