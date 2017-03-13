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
        public static void IsNull<T>(T value) where T : class
        {
            IsNull(value, null);
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNull<T>(T value, string message) where T : class
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                IsNull(value as UnityEngine.Object, message);
            }
            else if (value != null)
            {
                Fail(AssertionMessageUtil.NullFailureMessage(value, true), message);
            }
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNull(UnityEngine.Object value, string message)
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
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                IsNotNull(value as UnityEngine.Object, message);
            }
            else if (value == null)
            {
                Fail(AssertionMessageUtil.NullFailureMessage(value, false), message);
            }
        }

        [Conditional(UNITY_ASSERTIONS)]
        public static void IsNotNull(UnityEngine.Object value, string message)
        {
            if (value == null)
                Fail(AssertionMessageUtil.NullFailureMessage(value, false), message);
        }
    }
}
