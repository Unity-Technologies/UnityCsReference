// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.Search
{
    public interface IPropertyDatabaseRecordValue
    {
        PropertyDatabaseType type { get; }
    }

    public interface IPropertyDatabaseRecord
    {
        PropertyDatabaseRecordKey key { get; }
        IPropertyDatabaseRecordValue value { get; }
        bool validRecord { get; }
    }

    class IPropertyDatabaseRecordComparer : IComparer<IPropertyDatabaseRecord>
    {
        public int Compare(IPropertyDatabaseRecord x, IPropertyDatabaseRecord y)
        {
            return x.key.CompareTo(y.key);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PropertyDatabaseRecordKey : IComparable<PropertyDatabaseRecordKey>, IEquatable<PropertyDatabaseRecordKey>
    {
        public readonly ulong documentKey;
        public readonly Hash128 propertyKey;

        public static int size
        {
            get
            {
                unsafe
                {
                    return sizeof(ulong) + sizeof(Hash128);
                }
            }
        }

        public PropertyDatabaseRecordKey(ulong documentKey, Hash128 propertyKey)
        {
            this.documentKey = documentKey;
            this.propertyKey = propertyKey;
        }

        public int CompareTo(PropertyDatabaseRecordKey other)
        {
            var documentCompare = documentKey.CompareTo(other.documentKey);
            if (documentCompare != 0)
                return documentCompare;
            return propertyKey.CompareTo(other.propertyKey);
        }

        public static bool operator<(PropertyDatabaseRecordKey lhs, PropertyDatabaseRecordKey rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator>(PropertyDatabaseRecordKey lhs, PropertyDatabaseRecordKey rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public bool Equals(PropertyDatabaseRecordKey other)
        {
            return CompareTo(other) == 0;
        }

        public static bool operator==(PropertyDatabaseRecordKey lhs, PropertyDatabaseRecordKey rhs)
        {
            return lhs.CompareTo(rhs) == 0;
        }

        public static bool operator!=(PropertyDatabaseRecordKey lhs, PropertyDatabaseRecordKey rhs)
        {
            return lhs.CompareTo(rhs) != 0;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyDatabaseRecordKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (documentKey.GetHashCode() * 397) ^ propertyKey.GetHashCode();
            }
        }

        internal void ToBinary(BinaryWriter bw)
        {
            bw.Write(documentKey);
            bw.Write(propertyKey.u64_0);
            bw.Write(propertyKey.u64_1);
        }

        internal static PropertyDatabaseRecordKey FromBinary(BinaryReader br)
        {
            var documentKey = br.ReadUInt64();
            var u640 = br.ReadUInt64();
            var u641 = br.ReadUInt64();
            return new PropertyDatabaseRecordKey(documentKey, new Hash128(u640, u641));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly unsafe struct PropertyDatabaseRecordValue : IPropertyDatabaseRecordValue
    {
        public readonly byte propertyType;
        readonly uint m_u32_0;
        readonly uint m_u32_1;
        readonly uint m_u32_2;
        readonly uint m_u32_3;
        readonly uint m_u32_4;
        readonly uint m_u32_5;
        readonly uint m_u32_6;
        readonly uint m_u32_7;

        public PropertyDatabaseType type => (PropertyDatabaseType)propertyType;

        public static PropertyDatabaseRecordValue invalid => new PropertyDatabaseRecordValue();
        public static int maxSize => sizeof(uint) * 8;
        public static int size => sizeof(byte) + maxSize;

        public bool valid => propertyType != 0
        || m_u32_0 != 0
        || m_u32_1 != 0
        || m_u32_2 != 0
        || m_u32_3 != 0
        || m_u32_4 != 0
        || m_u32_5 != 0
        || m_u32_6 != 0
        || m_u32_7 != 0;

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index > 31)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var integerIndex = index / 4;
                var integerOffset = index % 4;

                uint target = 0;
                switch (integerIndex)
                {
                    case 0:
                        target = m_u32_0;
                        break;
                    case 1:
                        target = m_u32_1;
                        break;
                    case 2:
                        target = m_u32_2;
                        break;
                    case 3:
                        target = m_u32_3;
                        break;
                    case 4:
                        target = m_u32_4;
                        break;
                    case 5:
                        target = m_u32_5;
                        break;
                    case 6:
                        target = m_u32_6;
                        break;
                    case 7:
                        target = m_u32_7;
                        break;
                }

                var bytePtr = (byte*)&target;
                return *(bytePtr + integerOffset);
            }
        }

        public uint uint32_0 => m_u32_0;
        public uint uint32_1 => m_u32_1;
        public uint uint32_2 => m_u32_2;
        public uint uint32_3 => m_u32_3;
        public uint uint32_4 => m_u32_4;
        public uint uint32_5 => m_u32_5;
        public uint uint32_6 => m_u32_6;
        public uint uint32_7 => m_u32_7;

        public int int32_0
        {
            get
            {
                fixed(uint* ptr = &m_u32_0) return *(int*)ptr;
            }
        }
        public int int32_1
        {
            get
            {
                fixed(uint* ptr = &m_u32_1) return *(int*)ptr;
            }
        }
        public int int32_2
        {
            get
            {
                fixed(uint* ptr = &m_u32_2) return *(int*)ptr;
            }
        }
        public int int32_3
        {
            get
            {
                fixed(uint* ptr = &m_u32_3) return *(int*)ptr;
            }
        }
        public int int32_4
        {
            get
            {
                fixed(uint* ptr = &m_u32_4) return *(int*)ptr;
            }
        }
        public int int32_5
        {
            get
            {
                fixed(uint* ptr = &m_u32_5) return *(int*)ptr;
            }
        }
        public int int32_6
        {
            get
            {
                fixed(uint* ptr = &m_u32_6) return *(int*)ptr;
            }
        }
        public int int32_7
        {
            get
            {
                fixed(uint* ptr = &m_u32_7) return *(int*)ptr;
            }
        }

        public float float32_0
        {
            get
            {
                fixed(uint* ptr = &m_u32_0) return *(float*)ptr;
            }
        }
        public float float32_1
        {
            get
            {
                fixed(uint* ptr = &m_u32_1) return *(float*)ptr;
            }
        }
        public float float32_2
        {
            get
            {
                fixed(uint* ptr = &m_u32_2) return *(float*)ptr;
            }
        }
        public float float32_3
        {
            get
            {
                fixed(uint* ptr = &m_u32_3) return *(float*)ptr;
            }
        }
        public float float32_4
        {
            get
            {
                fixed(uint* ptr = &m_u32_4) return *(float*)ptr;
            }
        }
        public float float32_5
        {
            get
            {
                fixed(uint* ptr = &m_u32_5) return *(float*)ptr;
            }
        }
        public float float32_6
        {
            get
            {
                fixed(uint* ptr = &m_u32_6) return *(float*)ptr;
            }
        }
        public float float32_7
        {
            get
            {
                fixed(uint* ptr = &m_u32_7) return *(float*)ptr;
            }
        }

        public double float64_0
        {
            get
            {
                fixed(uint* ptr = &m_u32_0) return *(double*)ptr;
            }
        }
        public double float64_1
        {
            get
            {
                fixed(uint* ptr = &m_u32_2) return *(double*)ptr;
            }
        }
        public double float64_2
        {
            get
            {
                fixed(uint* ptr = &m_u32_4) return *(double*)ptr;
            }
        }
        public double float64_3
        {
            get
            {
                fixed(uint* ptr = &m_u32_6) return *(double*)ptr;
            }
        }

        public long int64_0
        {
            get
            {
                fixed(uint* ptr = &m_u32_0) return *(long*)ptr;
            }
        }
        public long int64_1
        {
            get
            {
                fixed(uint* ptr = &m_u32_2) return *(long*)ptr;
            }
        }
        public long int64_2
        {
            get
            {
                fixed(uint* ptr = &m_u32_4) return *(long*)ptr;
            }
        }
        public long int64_3
        {
            get
            {
                fixed(uint* ptr = &m_u32_6) return *(long*)ptr;
            }
        }

        public ulong uint64_0
        {
            get
            {
                fixed(uint* ptr = &m_u32_0) return *(ulong*)ptr;
            }
        }
        public ulong uint64_1
        {
            get
            {
                fixed(uint* ptr = &m_u32_2) return *(ulong*)ptr;
            }
        }
        public ulong uint64_2
        {
            get
            {
                fixed(uint* ptr = &m_u32_4) return *(ulong*)ptr;
            }
        }
        public ulong uint64_3
        {
            get
            {
                fixed(uint* ptr = &m_u32_6) return *(ulong*)ptr;
            }
        }

        public bool boolean => m_u32_0 > 0;

        public PropertyDatabaseRecordValue(byte propertyType, uint value)
            : this(propertyType, value, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2)
            : this(propertyType, value1, value2, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3)
            : this(propertyType, value1, value2, value3, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3, uint value4)
            : this(propertyType, value1, value2, value3, value4, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3, uint value4, uint value5)
            : this(propertyType, value1, value2, value3, value4, value5, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3, uint value4, uint value5, uint value6)
            : this(propertyType, value1, value2, value3, value4, value5, value6, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3, uint value4, uint value5, uint value6, uint value7)
            : this(propertyType, value1, value2, value3, value4, value5, value6, value7, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, uint value1, uint value2, uint value3, uint value4, uint value5, uint value6, uint value7, uint value8)
        {
            this.propertyType = propertyType;
            m_u32_0 = value1;
            m_u32_1 = value2;
            m_u32_2 = value3;
            m_u32_3 = value4;
            m_u32_4 = value5;
            m_u32_5 = value6;
            m_u32_6 = value7;
            m_u32_7 = value8;
        }

        public PropertyDatabaseRecordValue(byte propertyType, int value1)
            : this(propertyType, value1, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2)
            : this(propertyType, value1, value2, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3)
            : this(propertyType, value1, value2, value3, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3, int value4)
            : this(propertyType, value1, value2, value3, value4, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3, int value4, int value5)
            : this(propertyType, value1, value2, value3, value4, value5, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3, int value4, int value5, int value6)
            : this(propertyType, value1, value2, value3, value4, value5, value6, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3, int value4, int value5, int value6, int value7)
            : this(propertyType, value1, value2, value3, value4, value5, value6, value7, 0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8)
        {
            this.propertyType = propertyType;

            var ptr0 = (uint*)&value1;
            var ptr1 = (uint*)&value2;
            var ptr2 = (uint*)&value3;
            var ptr3 = (uint*)&value4;
            var ptr4 = (uint*)&value5;
            var ptr5 = (uint*)&value6;
            var ptr6 = (uint*)&value7;
            var ptr7 = (uint*)&value8;

            m_u32_0 = *ptr0;
            m_u32_1 = *ptr1;
            m_u32_2 = *ptr2;
            m_u32_3 = *ptr3;
            m_u32_4 = *ptr4;
            m_u32_5 = *ptr5;
            m_u32_6 = *ptr6;
            m_u32_7 = *ptr7;
        }

        public PropertyDatabaseRecordValue(byte propertyType, float value1)
            : this(propertyType, value1, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2)
            : this(propertyType, value1, value2, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3)
            : this(propertyType, value1, value2, value3, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3, float value4)
            : this(propertyType, value1, value2, value3, value4, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3, float value4, float value5)
            : this(propertyType, value1, value2, value3, value4, value5, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3, float value4, float value5, float value6)
            : this(propertyType, value1, value2, value3, value4, value5, value6, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3, float value4, float value5, float value6, float value7)
            : this(propertyType, value1, value2, value3, value4, value5, value6, value7, 0f)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, float value1, float value2, float value3, float value4, float value5, float value6, float value7, float value8)
        {
            this.propertyType = propertyType;

            var ptr0 = (uint*)&value1;
            var ptr1 = (uint*)&value2;
            var ptr2 = (uint*)&value3;
            var ptr3 = (uint*)&value4;
            var ptr4 = (uint*)&value5;
            var ptr5 = (uint*)&value6;
            var ptr6 = (uint*)&value7;
            var ptr7 = (uint*)&value8;

            m_u32_0 = *ptr0;
            m_u32_1 = *ptr1;
            m_u32_2 = *ptr2;
            m_u32_3 = *ptr3;
            m_u32_4 = *ptr4;
            m_u32_5 = *ptr5;
            m_u32_6 = *ptr6;
            m_u32_7 = *ptr7;
        }

        public PropertyDatabaseRecordValue(byte propertyType, double value1)
            : this(propertyType, value1, 0.0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, double value1, double value2)
            : this(propertyType, value1, value2, 0.0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, double value1, double value2, double value3)
            : this(propertyType, value1, value2, value3, 0.0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, double value1, double value2, double value3, double value4)
        {
            this.propertyType = propertyType;

            var ptr0 = (uint*)&value1;
            var ptr1 = (uint*)&value2;
            var ptr2 = (uint*)&value3;
            var ptr3 = (uint*)&value4;

            m_u32_0 = *ptr0;
            m_u32_1 = *(ptr0 + 1);
            m_u32_2 = *ptr1;
            m_u32_3 = *(ptr1 + 1);
            m_u32_4 = *ptr2;
            m_u32_5 = *(ptr2 + 1);
            m_u32_6 = *ptr3;
            m_u32_7 = *(ptr3 + 1);
        }

        public PropertyDatabaseRecordValue(byte propertyType, byte value1)
            : this(propertyType, value1, (byte)0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, byte value2)
            : this(propertyType, value1, value2, (byte)0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, byte value2, byte value3)
            : this(propertyType, value1, value2, value3, (byte)0)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, byte value2, byte value3, byte value4)
        {
            this.propertyType = propertyType;
            m_u32_0 = value1 | (uint)value2 << 8 | (uint)value3 << 16 | (uint)value4 << 24;
            m_u32_1 = 0;
            m_u32_2 = 0;
            m_u32_3 = 0;
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 0;
        }

        public PropertyDatabaseRecordValue(byte propertyType, byte[] values)
        {
            if (values.Length > 32)
                throw new ArgumentException("Array should be of size 32 or less.", nameof(values));

            this.propertyType = propertyType;
            m_u32_0 = 0;
            m_u32_1 = 0;
            m_u32_2 = 0;
            m_u32_3 = 0;
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 0;

            var byteIndex = 0;
            foreach (var value in values)
            {
                var integerIndex = byteIndex / 4;
                var integerOffset = byteIndex % 4;
                ++byteIndex;

                switch (integerIndex)
                {
                    case 0:
                        m_u32_0 |= (uint)value << (integerOffset * 8);
                        break;
                    case 1:
                        m_u32_1 |= (uint)value << (integerOffset * 8);
                        break;
                    case 2:
                        m_u32_2 |= (uint)value << (integerOffset * 8);
                        break;
                    case 3:
                        m_u32_3 |= (uint)value << (integerOffset * 8);
                        break;
                    case 4:
                        m_u32_4 |= (uint)value << (integerOffset * 8);
                        break;
                    case 5:
                        m_u32_5 |= (uint)value << (integerOffset * 8);
                        break;
                    case 6:
                        m_u32_6 |= (uint)value << (integerOffset * 8);
                        break;
                    case 7:
                        m_u32_7 |= (uint)value << (integerOffset * 8);
                        break;
                }
            }
        }

        public PropertyDatabaseRecordValue(byte propertyType, bool value)
        {
            this.propertyType = propertyType;
            m_u32_0 = value ? 1U : 0U;
            m_u32_1 = 0;
            m_u32_2 = 0;
            m_u32_3 = 0;
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 1;
        }

        public PropertyDatabaseRecordValue(byte propertyType, long value1)
            : this(propertyType, value1, 0L)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, long value1, long value2)
            : this(propertyType, value1, value2, 0L)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, long value1, long value2, long value3)
            : this(propertyType, value1, value2, value3, 0L)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, long value1, long value2, long value3, long value4)
        {
            this.propertyType = propertyType;

            var ptr0 = (uint*)&value1;
            var ptr1 = (uint*)&value2;
            var ptr2 = (uint*)&value3;
            var ptr3 = (uint*)&value4;

            m_u32_0 = *ptr0;
            m_u32_1 = *(ptr0 + 1);
            m_u32_2 = *ptr1;
            m_u32_3 = *(ptr1 + 1);
            m_u32_4 = *ptr2;
            m_u32_5 = *(ptr2 + 1);
            m_u32_6 = *ptr3;
            m_u32_7 = *(ptr3 + 1);
        }

        public PropertyDatabaseRecordValue(byte propertyType, ulong value1)
            : this(propertyType, value1, 0UL)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, ulong value1, ulong value2)
            : this(propertyType, value1, value2, 0UL)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, ulong value1, ulong value2, ulong value3)
            : this(propertyType, value1, value2, value3, 0UL)
        {}

        public PropertyDatabaseRecordValue(byte propertyType, ulong value1, ulong value2, ulong value3, ulong value4)
        {
            this.propertyType = propertyType;

            var ptr0 = (uint*)&value1;
            var ptr1 = (uint*)&value2;
            var ptr2 = (uint*)&value3;
            var ptr3 = (uint*)&value4;

            m_u32_0 = *ptr0;
            m_u32_1 = *(ptr0 + 1);
            m_u32_2 = *ptr1;
            m_u32_3 = *(ptr1 + 1);
            m_u32_4 = *ptr2;
            m_u32_5 = *(ptr2 + 1);
            m_u32_6 = *ptr3;
            m_u32_7 = *(ptr3 + 1);
        }

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, float value2)
        {
            this.propertyType = propertyType;

            m_u32_0 = value1;
            m_u32_1 = *(uint*)&value2;
            m_u32_2 = 0;
            m_u32_3 = 0;
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 0;
        }

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, long value2)
        {
            this.propertyType = propertyType;

            var ptr2 = (uint*)&value2;

            m_u32_0 = value1;
            m_u32_1 = 0;
            m_u32_2 = *ptr2;
            m_u32_3 = *(ptr2 + 1);
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 0;
        }

        public PropertyDatabaseRecordValue(byte propertyType, byte value1, int value2)
        {
            this.propertyType = propertyType;

            m_u32_0 = value1;
            m_u32_1 = *(uint*)&value2;
            m_u32_2 = 0;
            m_u32_3 = 0;
            m_u32_4 = 0;
            m_u32_5 = 0;
            m_u32_6 = 0;
            m_u32_7 = 0;
        }

        public void ToBinary(BinaryWriter bw)
        {
            bw.Write(propertyType);
            bw.Write(m_u32_0);
            bw.Write(m_u32_1);
            bw.Write(m_u32_2);
            bw.Write(m_u32_3);
            bw.Write(m_u32_4);
            bw.Write(m_u32_5);
            bw.Write(m_u32_6);
            bw.Write(m_u32_7);
        }

        public static PropertyDatabaseRecordValue FromBinary(BinaryReader br)
        {
            var propertyType = br.ReadByte();
            var u320 = br.ReadUInt32();
            var u321 = br.ReadUInt32();
            var u322 = br.ReadUInt32();
            var u323 = br.ReadUInt32();
            var u324 = br.ReadUInt32();
            var u325 = br.ReadUInt32();
            var u326 = br.ReadUInt32();
            var u327 = br.ReadUInt32();
            return new PropertyDatabaseRecordValue(propertyType, u320, u321, u322, u323, u324, u325, u326, u327);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct PropertyDatabaseRecord : IPropertyDatabaseRecord, IComparable<PropertyDatabaseRecord>, IComparable<PropertyDatabaseRecordKey>
    {
        public readonly PropertyDatabaseRecordKey recordKey;
        public readonly byte valid;
        public readonly PropertyDatabaseRecordValue recordValue;

        public PropertyDatabaseRecordKey key => recordKey;
        public IPropertyDatabaseRecordValue value => recordValue;
        public bool validRecord => valid == 1;

        public static int size => PropertyDatabaseRecordKey.size + sizeof(byte) + PropertyDatabaseRecordValue.size;

        public static PropertyDatabaseRecord invalid => new PropertyDatabaseRecord();

        public PropertyDatabaseRecord(ulong documentKey, Hash128 propertyKey, in PropertyDatabaseRecordValue recordValue)
            : this(new PropertyDatabaseRecordKey(documentKey, propertyKey), recordValue)
        {}

        public PropertyDatabaseRecord(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue recordValue)
        {
            this.recordKey = recordKey;
            this.recordValue = recordValue;
            valid = 1;
        }

        public PropertyDatabaseRecord(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue recordValue, bool valid)
        {
            this.recordKey = recordKey;
            this.recordValue = recordValue;
            this.valid = valid ? (byte)1 : (byte)0;
        }

        public PropertyDatabaseRecord(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue recordValue, byte valid)
        {
            this.recordKey = recordKey;
            this.recordValue = recordValue;
            this.valid = valid;
        }

        public int CompareTo(PropertyDatabaseRecord other)
        {
            return recordKey.CompareTo(other.recordKey);
        }

        public int CompareTo(PropertyDatabaseRecordKey other)
        {
            return recordKey.CompareTo(other);
        }

        public bool IsValid()
        {
            return valid == 1;
        }

        public void ToBinary(BinaryWriter bw)
        {
            recordKey.ToBinary(bw);
            bw.Write(valid);
            recordValue.ToBinary(bw);
        }

        public static PropertyDatabaseRecord FromBinary(BinaryReader br)
        {
            var recordKey = PropertyDatabaseRecordKey.FromBinary(br);
            var valid = br.ReadByte();
            var recordValue = PropertyDatabaseRecordValue.FromBinary(br);
            return new PropertyDatabaseRecord(recordKey, recordValue, valid);
        }
    }
}
