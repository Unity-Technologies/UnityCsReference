// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using UnityEngine.TestTools;

namespace Unity.Timeline.Foundation.Model
{
    readonly struct Marker : IEquatable<Marker>, IComparable<Marker>
    {
        public static readonly Marker Invalid = new Marker(UniqueID.Invalid);

        public readonly UniqueID id;
        public readonly DiscreteTime time;
        public readonly IItemContent content;

        public Marker(UniqueID id, DiscreteTime time = default, IItemContent content = default)
        {
            this.id = id;
            this.time = time;
            this.content = content;
        }

        public static Marker Generate(DiscreteTime time = default, IItemContent content = default)
        {
            return new Marker(UniqueID.Generate(), time, content);
        }

        public bool Equals(Marker other)
        {
            return id.Equals(other.id);
        }

        public override bool Equals(object obj)
        {
            return obj is Marker other && Equals(other);
        }

        public static bool operator ==(Marker left, Marker right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Marker left, Marker right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public int CompareTo(Marker other)
        {
            return time.CompareTo(other.time);
        }

        [ExcludeFromCoverage]
        public override string ToString()
        {
            return $"Marker {id} time: {time}";
        }
    }

    static class MarkerExtensions
    {
        public static Marker WithId(in this Marker marker, UniqueID id)
        {
            return new Marker(id, marker.time, marker.content);
        }

        public static Marker WithTime(in this Marker marker, double time)
        {
            return marker.WithTime(new DiscreteTime(time));
        }

        public static Marker WithTime(in this Marker marker, DiscreteTime time)
        {
            return new Marker(marker.id, time, marker.content);
        }

        public static Marker WithContent(in this Marker marker, IItemContent content)
        {
            return new Marker(marker.id, marker.time, content);
        }
    }
}
