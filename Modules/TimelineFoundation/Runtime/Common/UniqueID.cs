// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Common
{
    using System.Runtime.InteropServices;

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct UniqueID : IEquatable<UniqueID>
    {
        public static UniqueID Invalid = new(EntityId.None);

        public readonly ulong A;
        public readonly ulong B;
        public readonly ulong C;
        public readonly ulong D;

        public UniqueID(EntityId A)
            : this(EntityId.ToULong(A)) { }

        public UniqueID(EntityId A, EntityId B, ulong C = default, ulong D = default)
            : this(EntityId.ToULong(A), EntityId.ToULong(B), C, D) { }

        public UniqueID(ulong A, ulong B = default, ulong C = default, ulong D = default)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct GuidConverter
        {
            [FieldOffset(0)]
            readonly Guid guid;
            [FieldOffset(0)]
            public readonly int A;
            [FieldOffset(4)]
            public readonly int B;
            [FieldOffset(8)]
            public readonly int C;
            [FieldOffset(12)]
            public readonly int D;

            public GuidConverter(Guid guid)
            {
                A = default;
                B = default;
                C = default;
                D = default;
                this.guid = guid;
            }
        }
        public static UniqueID Generate()
        {
            var converter = new GuidConverter(Guid.NewGuid());
            return new UniqueID((ulong)converter.A, (ulong)converter.B, (ulong)converter.C, (ulong)converter.D);
        }

        public static UniqueID Create(ulong uniqueId)
        {
            return new UniqueID(uniqueId);
        }

        public bool Equals(UniqueID other)
        {
            return A == other.A && B == other.B && C == other.C && D == other.D;
        }

        public override bool Equals(object obj)
        {
            return obj is UniqueID other && Equals(other);
        }

        public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode() ^ D.GetHashCode();

        public static bool operator ==(UniqueID left, UniqueID right) => left.Equals(right);

        public static bool operator !=(UniqueID left, UniqueID right) => !(left == right);

        public override string ToString() => $"UniqueID: {A}-{B}-{C}-{D}";
    }
}
