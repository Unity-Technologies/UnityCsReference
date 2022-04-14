// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    public enum PropertyDatabaseType : byte
    {
        None = 0,
        FixedString,
        String,
        Bool,
        Byte,
        Short,
        UnsignedShort,
        Integer,
        UnsignedInteger,
        Long,
        UnsignedLong,
        Double,
        Float,
        GlobalObjectId,
        Vector4,
        Color,
        Color32,
        InstanceId,
        GameObjectProperty,
        Volatile
    }

    class PropertyDatabaseSerializerAttribute : Attribute
    {
        public Type type;

        public PropertyDatabaseSerializerAttribute(Type type)
        {
            this.type = type;
        }
    }

    class PropertyDatabaseDeserializerAttribute : Attribute
    {
        public PropertyDatabaseType type;

        public PropertyDatabaseDeserializerAttribute(PropertyDatabaseType type)
        {
            this.type = type;
        }
    }

    struct PropertyDatabaseSerializationArgs
    {
        public object value;
        public PropertyStringTableView stringTableView;

        public PropertyDatabaseSerializationArgs(object value, PropertyStringTableView stringTableView)
        {
            this.value = value;
            this.stringTableView = stringTableView;
        }
    }

    struct PropertyDatabaseDeserializationArgs
    {
        public PropertyDatabaseRecordValue value;
        public PropertyStringTableView stringTableView;

        public PropertyDatabaseDeserializationArgs(PropertyDatabaseRecordValue value, PropertyStringTableView stringTableView)
        {
            this.value = value;
            this.stringTableView = stringTableView;
        }
    }

    delegate PropertyDatabaseRecordValue PropertySerializerHandler(PropertyDatabaseSerializationArgs args);

    delegate object PropertyDeserializerHandler(PropertyDatabaseDeserializationArgs args);

    readonly struct PropertyDatabaseSerializer
    {
        public readonly Type type;
        public readonly PropertySerializerHandler handler;

        public PropertyDatabaseSerializer(Type type, PropertySerializerHandler handler)
        {
            this.type = type;
            this.handler = handler;
        }
    }

    readonly struct PropertyDatabaseDeserializer
    {
        public readonly PropertyDatabaseType type;
        public readonly PropertyDeserializerHandler handler;

        public PropertyDatabaseDeserializer(PropertyDatabaseType type, PropertyDeserializerHandler handler)
        {
            this.type = type;
            this.handler = handler;
        }
    }

    static class PropertyDatabaseSerializerManager
    {
        static Dictionary<Type, PropertyDatabaseSerializer> s_Serializers = new Dictionary<Type, PropertyDatabaseSerializer>();
        static Dictionary<PropertyDatabaseType, PropertyDatabaseDeserializer> s_Deserializers = new Dictionary<PropertyDatabaseType, PropertyDatabaseDeserializer>();

        static PropertyDatabaseSerializerManager()
        {
            RefreshSerializers();
        }

        public static void RefreshSerializers()
        {
            s_Serializers.Clear();
            s_Deserializers.Clear();

            var serializers = ReflectionUtils.LoadAllMethodsWithAttribute<PropertyDatabaseSerializerAttribute, PropertyDatabaseSerializer>((info, attribute, handler) =>
            {
                if (handler is PropertySerializerHandler psh)
                    return new PropertyDatabaseSerializer(attribute.type, psh);
                throw new Exception($"Invalid {nameof(PropertyDatabaseSerializerAttribute)} handler.");
            }, MethodSignature.FromDelegate<PropertySerializerHandler>());

            var deserializers = ReflectionUtils.LoadAllMethodsWithAttribute<PropertyDatabaseDeserializerAttribute, PropertyDatabaseDeserializer>((info, attribute, handler) =>
            {
                if (handler is PropertyDeserializerHandler psh)
                    return new PropertyDatabaseDeserializer(attribute.type, psh);
                throw new Exception($"Invalid {nameof(PropertyDatabaseDeserializerAttribute)} handler.");
            }, MethodSignature.FromDelegate<PropertyDeserializerHandler>());

            foreach (var propertySerializer in serializers)
            {
                if (s_Serializers.ContainsKey(propertySerializer.type))
                    continue;
                s_Serializers.Add(propertySerializer.type, propertySerializer);
            }

            foreach (var propertyDeserializer in deserializers)
            {
                if (s_Deserializers.ContainsKey(propertyDeserializer.type))
                    continue;
                s_Deserializers.Add(propertyDeserializer.type, propertyDeserializer);
            }
        }

        public static bool SerializerExists(Type type)
        {
            return s_Serializers.ContainsKey(type);
        }

        public static bool DeserializerExists(PropertyDatabaseType type)
        {
            return s_Deserializers.ContainsKey(type);
        }

        public static PropertyDatabaseRecordValue Serialize(object value, PropertyStringTableView stringTableView)
        {
            var type = value.GetType();
            if (!s_Serializers.ContainsKey(type))
                return PropertyDatabaseRecordValue.invalid;

            var args = new PropertyDatabaseSerializationArgs(value, stringTableView);
            return s_Serializers[type].handler(args);
        }

        public static object Deserialize(PropertyDatabaseRecordValue value, PropertyStringTableView stringTableView)
        {
            var type = (PropertyDatabaseType)value.propertyType;
            if (!s_Deserializers.ContainsKey(type))
                return PropertyDatabaseRecordValue.invalid;

            var args = new PropertyDatabaseDeserializationArgs(value, stringTableView);
            return s_Deserializers[type].handler(args);
        }
    }
}
