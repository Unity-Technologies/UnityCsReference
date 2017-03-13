// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Assertions.Comparers
{
    public class FloatComparer : IEqualityComparer<float>
    {
        readonly float m_Error;
        readonly bool m_Relative;
        public static readonly FloatComparer s_ComparerWithDefaultTolerance = new FloatComparer(kEpsilon);

        public const float kEpsilon = 0.00001f;

        public FloatComparer()
            : this(kEpsilon, false)
        {
        }

        public FloatComparer(bool relative)
            : this(kEpsilon, relative)
        {
        }

        public FloatComparer(float error)
            : this(error, false)
        {
        }

        public FloatComparer(float error, bool relative)
        {
            m_Error = error;
            m_Relative = relative;
        }

        public bool Equals(float a, float b)
        {
            return m_Relative ? AreEqualRelative(a, b, m_Error) : AreEqual(a, b, m_Error);
        }

        public int GetHashCode(float obj)
        {
            return base.GetHashCode();
        }

        public static bool AreEqual(Single expected, Single actual, Single error)
        {
            return Math.Abs(actual - expected) <= error;
        }

        public static bool AreEqualRelative(Single expected, Single actual, Single error)
        {
            if (expected == actual) return true;

            var absExpected = Math.Abs(expected);
            var absActual = Math.Abs(actual);
            var relativeError = Math.Abs((actual - expected) / (absExpected > absActual ? absExpected : absActual));

            return relativeError <= error;
        }
    }
}
