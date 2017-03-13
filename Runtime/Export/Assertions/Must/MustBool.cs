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
        public static void MustBeTrue(this bool value)
        {
            Assert.IsTrue(value);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeTrue(this bool value, string message)
        {
            Assert.IsTrue(value, message);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeFalse(this bool value)
        {
            Assert.IsFalse(value);
        }

        [Conditional(Assert.UNITY_ASSERTIONS)]
        [Obsolete("Must extensions are deprecated. Use UnityEngine.Assertions.Assert instead")]
        public static void MustBeFalse(this bool value, string message)
        {
            Assert.IsFalse(value, message);
        }
    }
}
