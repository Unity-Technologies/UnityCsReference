// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        internal readonly struct Transition : IEquatable<Transition>
        {
            public static readonly Transition Invalid = new Transition(UniqueID.Invalid, TimeRange.Empty, null);

            readonly UniqueID m_Id;
            readonly TimeRange m_Range;
            readonly IItemContent m_Content;

            public UniqueID id => m_Id;
            public TimeRange range => m_Range;
            public DiscreteTime duration => m_Range.duration;

            internal enum Location { Left, Right }

            internal DiscreteTime GetOverlap_Internal(Location location)
            {
                switch (location)
                {
                    case Location.Left:
                        return duration.Split().a;
                    case Location.Right:
                        return duration.Split().b;
                }

                throw new IndexOutOfRangeException();
            }
            public IItemContent content => m_Content;

            public Transition(UniqueID id, TimeRange range, IItemContent content)
            {
                m_Id = id;
                m_Range = range;
                m_Content = content;
            }

            public bool isValid => m_Id != UniqueID.Invalid;

            public bool Equals(Transition other)
            {
                return m_Id == other.m_Id;
            }

            public override bool Equals(object obj)
            {
                return obj is Transition other && Equals(other);
            }

            public override int GetHashCode()
            {
                return m_Id.GetHashCode();
            }

            public static bool operator ==(Transition left, Transition right)
            {
                return left.m_Id == right.m_Id;
            }

            public static bool operator !=(Transition left, Transition right)
            {
                return !(left == right);
            }
        }
    }
}
