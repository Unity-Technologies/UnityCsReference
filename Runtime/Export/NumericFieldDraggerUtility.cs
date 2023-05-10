// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine
{
    [MovedFrom("UnityEditor")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class NumericFieldDraggerUtility
    {
        public static float Acceleration(bool shiftPressed, bool altPressed)
        {
            return (shiftPressed ? 4 : 1) * (altPressed ? .25f : 1);
        }

        static bool s_UseYSign = false;

        public static float NiceDelta(Vector2 deviceDelta, float acceleration)
        {
            deviceDelta.y = -deviceDelta.y;

            if (Mathf.Abs(Mathf.Abs(deviceDelta.x) - Mathf.Abs(deviceDelta.y)) / Mathf.Max(Mathf.Abs(deviceDelta.x), Mathf.Abs(deviceDelta.y)) > .1f)
            {
                if (Mathf.Abs(deviceDelta.x) > Mathf.Abs(deviceDelta.y))
                    s_UseYSign = false;
                else
                    s_UseYSign = true;
            }

            if (s_UseYSign)
                return Mathf.Sign(deviceDelta.y) * deviceDelta.magnitude * acceleration;
            else
                return Mathf.Sign(deviceDelta.x) * deviceDelta.magnitude * acceleration;
        }

        const float kDragSensitivity = .03f;

        public static double CalculateFloatDragSensitivity(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0.0;
            }
            return Math.Max(1, Math.Pow(Math.Abs(value), 0.5)) * kDragSensitivity;
        }

        public static double CalculateFloatDragSensitivity(double value, double minValue, double maxValue)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0.0;
            }

            double range = Math.Abs(maxValue - minValue);
            return range / 100.0f * kDragSensitivity;
        }

        public static long CalculateIntDragSensitivity(long value)
        {
            return (long)CalculateIntDragSensitivity((double)value);
        }

        public static ulong CalculateIntDragSensitivity(ulong value)
        {
            return (ulong)CalculateIntDragSensitivity((double)value);
        }

        public static double CalculateIntDragSensitivity(double value)
        {
            return Math.Max(1, Math.Pow(Math.Abs(value), 0.5) * kDragSensitivity);
        }

        public static long CalculateIntDragSensitivity(long value, long minValue, long maxValue)
        {
            long range = Math.Abs(maxValue - minValue);
            return Math.Max(1, (long)(kDragSensitivity * range / 100));
        }
    }
}
