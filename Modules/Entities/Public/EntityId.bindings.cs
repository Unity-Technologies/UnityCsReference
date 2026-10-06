// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
#pragma warning disable 612, 618
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    [UsedByNativeCode]
    [Serializable]
    [NativeClass("EntityId")]
    [NativeHeader("Runtime/BaseClasses/BaseObject.h")]
    [NativeHeader("ManagedKernel/BaseClasses/EntityId.bindings.h")]
    [NativeHeader("Modules/Entities/Public/EntityIdStore.h")]
    public struct EntityId : IEquatable<EntityId>, IComparable<EntityId>, IFormattable
    {
        [SerializeField]
        ulong m_rawData;

        public static EntityId None => new EntityId { m_rawData = 0 };
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public bool Equals(EntityId other)
        {
            return m_rawData == other.m_rawData;
        }
        public int CompareTo(EntityId other) => m_rawData.CompareTo(other.m_rawData);
        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public static bool operator <(EntityId left, EntityId right)  => left.m_rawData < right.m_rawData;
        public static bool operator >(EntityId left, EntityId right)  => left.m_rawData > right.m_rawData;
        public static bool operator <=(EntityId left, EntityId right) => left.m_rawData <= right.m_rawData;
        public static bool operator >=(EntityId left, EntityId right) => left.m_rawData >= right.m_rawData;

        public override int GetHashCode()
        {
            // Mirrors native EntityId::CalculateHash (Fibonacci hash, 2^64 / phi multiplier,
            // with a high-to-low fold so callers that mask off only the low bits of the
            // hash still see the full avalanche from Version bits).
            unchecked // No-op under the assembly's default /checked- build; documents that the multiply is meant to wrap.
            {
                const ulong kKnuth64 = 0x9E3779B97F4A7C15UL;
                uint hash = (uint)((m_rawData * kKnuth64) >> 32);
                return (int)(hash ^ (hash >> 16));
            }
        }

        public bool IsValid()
        {
            return this != EntityId.None;
        }

        // Bit layout matches native EntityId (see Modules/NativeKernel/Include/NativeKernel/BaseClasses/EntityId.h):
        //   [Version:24 | TypeId:12 | Index:28]
        //   Index:   bits  0–27 (mask 0x0FFFFFFF)
        //   TypeId:  bits 28–39
        //   Version: bits 40–63
        internal uint Index   => (uint)(m_rawData & 0x0FFFFFFFUL);
        internal uint Version => (uint)((m_rawData >> 40) & 0xFFFFFFUL);


        [Obsolete("EntityId will not be representable by an int in the future. This equals will be removed in a future version.", true)]
        public bool Equals(int other) => throw new NotImplementedException();

        [Obsolete("EntityId will not be representable by an int in the future. This casting operator will be removed in a future version.", true)]
        public static implicit operator int(EntityId entityId) => throw new NotImplementedException();

        [Obsolete("EntityId will not be representable by an int in the future. This casting operator will be removed in a future version.", true)]
        public static implicit operator EntityId(int intValue) => throw new NotImplementedException();

        public override string ToString() => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(CultureInfo.InvariantCulture)}:{(int)(m_rawData >> 32)}";
        public string ToString(string format) => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(format, CultureInfo.InvariantCulture)}:{(int)(m_rawData >> 32)}";
        public string ToString(string format, IFormatProvider formatProvider) => $"{((int)(m_rawData & 0xFFFFFFFF)).ToString(format, formatProvider)}:{((int)(m_rawData >> 32)).ToString(format, formatProvider)}";


        // Single-id allocation for editor and tooling callers, which take one id at a time
        // and so have no use for the store's batched path.
        internal static unsafe EntityId AllocateEntityId()
        {
            EntityId id;
            EntityIdStoreBindings.AllocateForManaged(&id, 1);
            return id;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static EntityId Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
                return EntityId.None;

            // Same as native StringToEntityId: full raw UInt64, no reinterpretation (see EntityID.cpp).
            if (!ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongResult))
            {
                var colonIndex = input.IndexOf(':');
                if (colonIndex == -1 || colonIndex == 0 || colonIndex == input.Length - 1)
                    return EntityId.None;
                var indexStr = input.Substring(0, colonIndex);
                var versionStr = input.Substring(colonIndex + 1);
                if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityIndex))
                    return EntityId.None;
                if (!int.TryParse(versionStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityVersion))
                    return EntityId.None;
                ulongResult = ((ulong)entityVersion << 32) | (uint)entityIndex;
            }

            return EntityId.FromULong(ulongResult);
        }

        [Obsolete("Please use EntityId.ToULong(EntityId) instead.", false)]
        public ulong GetRawData() => m_rawData;

        public static EntityId FromULong(ulong input) => new EntityId { m_rawData = input };
        public static ulong ToULong(EntityId entityId) => entityId.m_rawData;
    }
#pragma warning restore 612, 618
}
