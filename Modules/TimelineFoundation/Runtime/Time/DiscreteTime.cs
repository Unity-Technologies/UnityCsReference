// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Time
{
    /// <summary>
    /// Extension methods for the DiscreteTime dataType
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class DiscreteTimeExtensions // need a better name. math cannot be split across assemblies.
    {
        /// <summary> Returns half of a time value. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Half(this DiscreteTime time)
        {
            return DiscreteTime.FromTicks(time.Value / 2);
        }

        /// <summary> Returns two DiscreteTime objects with half the duration of the parameter.
        /// If the parameter's value is odd then b will be 1 tick longer. </summary>
        public static (DiscreteTime a, DiscreteTime b) Split(this DiscreteTime time)
        {
            DiscreteTime a = time / 2;
            return (a, time - a); // expects: a.Value + b.Value == time.Value
        }

        /// <summary> Return the time restricted inside the range. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Clamp(this DiscreteTime time, TimeRange range)
        {
            return time.Clamp(range.start, range.end);
        }

        /// <summary> Returns the next representable value of <paramref name="time"/></summary>
        /// <exception cref="OverflowException">Throws if the operation overflows the bounds of the DiscreteTime type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime NextValue(this DiscreteTime time)
        {
            checked
            {
                return DiscreteTime.FromTicks(time.Value + 1);
            }
        }

        /// <summary> Returns the previous representable value of <paramref name="time"/></summary>
        /// <exception cref="OverflowException">Throws if the operation overflows the bounds of the DiscreteTime type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime PreviousValue(this DiscreteTime time)
        {
            checked
            {
                return DiscreteTime.FromTicks(time.Value - 1);
            }
        }
    }
}
