// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Search
{
    static class DefaultPropertyDatabaseSerializers
    {
        public static int fixedStringLengthByteSize => 1;

        [PropertyDatabaseSerializer(typeof(int))]
        internal static PropertyDatabaseRecordValue IntegerSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Integer, (int)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Integer)]
        internal static object IntegerDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.int32_0;
        }

        [PropertyDatabaseSerializer(typeof(uint))]
        internal static PropertyDatabaseRecordValue UnsignedIntegerSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.UnsignedInteger, (uint)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.UnsignedInteger)]
        internal static object UnsignedIntegerDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.uint32_0;
        }

        [PropertyDatabaseSerializer(typeof(byte))]
        internal static PropertyDatabaseRecordValue ByteSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Byte, (byte)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Byte)]
        internal static object ByteDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value[0];
        }

        [PropertyDatabaseSerializer(typeof(short))]
        internal static PropertyDatabaseRecordValue ShortSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Short, (short)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Short)]
        internal static object ShortDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return (short)args.value.int32_0;
        }

        [PropertyDatabaseSerializer(typeof(ushort))]
        internal static PropertyDatabaseRecordValue UnsignedShortSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.UnsignedShort, (ushort)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.UnsignedShort)]
        internal static object UnsignedShortDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return (ushort)args.value.int32_0;
        }

        [PropertyDatabaseSerializer(typeof(long))]
        internal static PropertyDatabaseRecordValue LongSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Long, (long)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Long)]
        internal static object LongDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.int64_0;
        }

        [PropertyDatabaseSerializer(typeof(ulong))]
        internal static PropertyDatabaseRecordValue UnsignedLongSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.UnsignedLong, (ulong)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.UnsignedLong)]
        internal static object UnsignedLongDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.uint64_0;
        }

        [PropertyDatabaseSerializer(typeof(bool))]
        internal static PropertyDatabaseRecordValue BooleanSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Bool, (bool)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Bool)]
        internal static object BooleanDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.boolean;
        }

        [PropertyDatabaseSerializer(typeof(double))]
        internal static PropertyDatabaseRecordValue DoubleSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Double, (double)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Double)]
        internal static object DoubleDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.float64_0;
        }

        [PropertyDatabaseSerializer(typeof(float))]
        internal static PropertyDatabaseRecordValue FloatSerializer(PropertyDatabaseSerializationArgs args)
        {
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Float, (float)args.value);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Float)]
        internal static object FloatDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return args.value.float32_0;
        }

        [PropertyDatabaseSerializer(typeof(string))]
        internal static PropertyDatabaseRecordValue StringSerializer(PropertyDatabaseSerializationArgs args)
        {
            var stringValue = (string)args.value;
            var byteSize = PropertyStringTable.encoding.GetByteCount(stringValue);
            if (byteSize + fixedStringLengthByteSize <= PropertyDatabaseRecordValue.maxSize)
            {
                var bytes = new byte[byteSize + fixedStringLengthByteSize];
                SetFixedStringLength(bytes, byteSize);
                PropertyStringTable.encoding.GetBytes(stringValue, 0, stringValue.Length, bytes, fixedStringLengthByteSize);
                return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.FixedString, bytes);
            }

            var symbol = args.stringTableView.ToSymbol(stringValue);
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.String, symbol);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.FixedString)]
        internal static object FixedStringDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            var byteSize = GetFixedStringLength(args.value);
            var bytes = new byte[byteSize];
            for (var i = 0; i < byteSize; ++i)
            {
                bytes[i] = args.value[i + fixedStringLengthByteSize];
            }
            return PropertyStringTable.encoding.GetString(bytes);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.String)]
        internal static object StringDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            var symbol = args.value.int32_0;
            return args.stringTableView.GetString(symbol);
        }

        [PropertyDatabaseSerializer(typeof(Vector4))]
        internal static PropertyDatabaseRecordValue Vector4Serializer(PropertyDatabaseSerializationArgs args)
        {
            var v = (Vector4)args.value;
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Vector4, v.x, v.y, v.z, v.w);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Vector4)]
        internal static object Vector4Deserializer(PropertyDatabaseDeserializationArgs args)
        {
            return new Vector4(args.value.float32_0, args.value.float32_1, args.value.float32_2, args.value.float32_3);
        }

        [PropertyDatabaseSerializer(typeof(GlobalObjectId))]
        internal static PropertyDatabaseRecordValue GlobalObjectIdSerializer(PropertyDatabaseSerializationArgs args)
        {
            var goid = (GlobalObjectId)args.value;
            var str = goid.ToString();
            var symbol = args.stringTableView.ToSymbol(str);
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.GlobalObjectId, symbol);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.GlobalObjectId)]
        internal static object GlobalObjectIdDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            var symbol = args.value.int32_0;
            var str = args.stringTableView.GetString(symbol);
            if (GlobalObjectId.TryParse(str, out var goid))
                return goid;
            return null;
        }

        [PropertyDatabaseSerializer(typeof(Color))]
        internal static PropertyDatabaseRecordValue ColorSerializer(PropertyDatabaseSerializationArgs args)
        {
            var color = (Color)args.value;
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Color, color.r, color.g, color.b, color.a);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Color)]
        internal static object ColorDeserializer(PropertyDatabaseDeserializationArgs args)
        {
            return new Color(args.value.float32_0, args.value.float32_1, args.value.float32_2, args.value.float32_3);
        }

        [PropertyDatabaseSerializer(typeof(Color32))]
        internal static PropertyDatabaseRecordValue Color32Serializer(PropertyDatabaseSerializationArgs args)
        {
            var color = (Color32)args.value;
            return new PropertyDatabaseRecordValue((byte)PropertyDatabaseType.Color32, color.r, color.g, color.b, color.a);
        }

        [PropertyDatabaseDeserializer(PropertyDatabaseType.Color32)]
        internal static object Color32Deserializer(PropertyDatabaseDeserializationArgs args)
        {
            return new Color32(args.value[0], args.value[1], args.value[2], args.value[3]);
        }

        static void SetFixedStringLength(byte[] bytes, int byteSize)
        {
            bytes[0] = (byte)byteSize;
        }

        static byte GetFixedStringLength(PropertyDatabaseRecordValue value)
        {
            return value[0];
        }
    }
}
