// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal interface IUxmlAttributeConverter
    {
        public object FromString(string value);
        public string ToString(object value); // One value
    }

    internal static class UxmlAttributeConverter
    {
        private static readonly Dictionary<Type, Type> s_RegisteredConverterTypes = new();
        private static readonly Dictionary<Type, IUxmlAttributeConverter> s_Converters = new();

        static UxmlAttributeConverter()
        {
            var types = TypeCache.GetTypesDerivedFrom<IUxmlAttributeConverter>();
            foreach (var converterType in types)
            {
                var attributes = converterType.Attributes;
                if ((attributes & TypeAttributes.Abstract) != 0 || converterType.IsGenericType)
                    continue;

                Type conversionType = null;
                var t = converterType;
                while (t.BaseType != null && conversionType == null)
                {
                    t = t.BaseType;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(UxmlAttributeConverter<>))
                        conversionType = t.GetGenericArguments()[0];
                }
                Debug.Assert(conversionType != null);
                s_RegisteredConverterTypes[conversionType] = converterType;
            }
        }

        public static bool TryConvertFromString<T>(string value, CreationContext cc, out object result)
        {
            return TryConvertFromString(typeof(T), value, cc, out result);
        }

        public static bool TryConvertFromString(Type type, string value, CreationContext cc, out object result)
        {
            // Special handling for Unity Object
            if (typeof(Object).IsAssignableFrom(type))
            {
                if (type == typeof(VisualTreeAsset))
                {
                    result = cc.visualTreeAsset.ResolveTemplate(value);
                    if (result != null)
                        return true;
                }

                result = cc.visualTreeAsset.GetAsset(value, type);
                return true;
            }
            else if (TryGetConverter(type, out var converter))
            {
                result = converter.FromString(value);
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryConvertToString(object value, VisualTreeAsset visualTreeAsset, out string result)
        {
            return TryConvertToString(value.GetType(), value, visualTreeAsset, out result);
        }

        public static bool TryConvertToString<T>(object value, VisualTreeAsset visualTreeAsset, out string result)
        {
            return TryConvertToString(typeof(T), value, visualTreeAsset, out result);
        }

        static bool TryConvertToString(Type type, object value, VisualTreeAsset visualTreeAsset, out string result)
        {
            Debug.Assert(type.IsInstanceOfType(value));

            // Handle RuntimeType.
            if (typeof(Type).IsAssignableFrom(type))
                type = typeof(Type);

            // Special handling for Unity Object
            if (typeof(Object).IsAssignableFrom(type))
            {
                var obj = value as Object;

                if (type == typeof(VisualTreeAsset))
                {
                    result = AssetDatabase.GetAssetPath(obj);
                    if (visualTreeAsset != null && !string.IsNullOrEmpty(result) && !visualTreeAsset.TemplateExists(obj.name))
                        visualTreeAsset.RegisterTemplate(obj.name, result);
                }

                result = URIHelpers.MakeAssetUri(obj);
                if (visualTreeAsset != null && !string.IsNullOrEmpty(result) && !visualTreeAsset.AssetEntryExists(result, type))
                    visualTreeAsset.RegisterAssetEntry(result, type, obj);

                return true;
            }

            if (TryGetConverter(type, out var converter))
            {
                result = converter.ToString(value);
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryGetConverter<T>(out IUxmlAttributeConverter converter)
        {
            return TryGetConverter(typeof(T), out converter);
        }

        public static bool TryGetConverter(Type type, out IUxmlAttributeConverter converter)
        {
            if (!s_RegisteredConverterTypes.ContainsKey(type))
            {
                if (type.IsEnum)
                {
                    converter = GetOrCreateConverter(typeof(Enum));
                    ((EnumAttributeConverter)converter).type = type;
                    return true;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var genericType = type.GetGenericArguments()[0];
                    if (s_RegisteredConverterTypes.ContainsKey(genericType) || genericType.IsEnum)
                    {
                        var converterType = typeof(ListAttributeConverter<>).MakeGenericType(genericType);
                        s_RegisteredConverterTypes[type] = converterType;
                        converter = GetOrCreateConverter(type);
                        return true;
                    }
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (s_RegisteredConverterTypes.ContainsKey(elementType) || elementType.IsEnum)
                    {
                        var converterType = typeof(ArrayAttributeConverter<>).MakeGenericType(elementType);
                        s_RegisteredConverterTypes[type] = converterType;
                        converter = GetOrCreateConverter(type);
                        return true;
                    }
                }

                converter = null;
                return false;
            }

            converter = GetOrCreateConverter(type);
            return true;
        }

        private static IUxmlAttributeConverter GetOrCreateConverter(Type type)
        {
            if (!s_Converters.TryGetValue(type, out var converter))
            {
                Debug.Assert(s_RegisteredConverterTypes.ContainsKey(type));
                var converterType = s_RegisteredConverterTypes[type];
                converter = Activator.CreateInstance(converterType) as IUxmlAttributeConverter;
                s_Converters[type] = converter;
            }
            return converter;
        }
    }

    /// <summary>
    /// Converts a UxmlAttribute type to and from a string.
    /// </summary>
    /// <remarks>
    /// Fields marked with <see cref="UxmlAttributeAttribute"/> are represented in UXML by a single string attribute,
    /// however, to properly serialize these attributes, you must declare a UxmlAttributeConverter.
    /// This converter converts the string attribute value into the appropriate data type for the marked field.
    ///
    ///
    /// __Note:__ The following types have native support and you can use them without declaring a UxmlAttributeConverter:
    ///
    ///* bool
    ///* char
    ///* string
    ///* short
    ///* int
    ///* long
    ///* Enum
    ///* float
    ///* double
    ///* Type(SA: [[UxmlTypeReferenceAttribute]])
    ///* [[Color]]
    ///* [[Hash128]]
    ///* [[Length]]
    ///* [[Bounds]]
    ///* [[BoundsInt]]
    ///* [[Rect]]
    ///* [[RectInt]]
    ///* [[Vector2]]
    ///* [[Vector2Int]]
    ///* [[Vector3]]
    ///* [[Vector3Int]]
    ///* [[Vector4]]
    /// </remarks>
    /// <example>
    /// The following example creates a custom control that uses a class instance and a list of class instances as its attributes.
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithData.cs"/>
    /// </example>
    /// <example>
    /// To support the class, declare a converter:
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithDataConverter.cs"/>
    /// </example>
    /// <example>
    /// Example UXML:
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithDataConverter.uxml"/>
    /// </example>
    /// <typeparam name="T"></typeparam>
    public abstract class UxmlAttributeConverter<T> : IUxmlAttributeConverter
    {
        /// <summary>
        /// Provides a type converted from a string representation.
        /// </summary>
        /// <remarks>
        /// Returns an instance or value of a type from the string representation.
        /// </remarks>
        /// <param name="value">A string representation of the type.</param>
        /// <returns>Instance of type from the string value.</returns>
        public abstract T FromString(string value);

        /// <summary>
        /// Provides a string representation of a type.
        /// </summary>
        /// <remarks>
        /// Returns a string that represents a type and its values.
        /// </remarks>
        /// <param name="value">The value to be converted into a string.</param>
        /// <returns>A string representation of the type.</returns>
        public virtual string ToString(T value) => Convert.ToString(value, CultureInfo.InvariantCulture);

        string IUxmlAttributeConverter.ToString(object value) => ToString((T)value);
        object IUxmlAttributeConverter.FromString(string value) => FromString(value);
    }

    internal class BoolAttributeConverter : UxmlAttributeConverter<bool>
    {
        public override bool FromString(string value)
        {
            bool.TryParse(value, out var result);
            return result;
        }

        public override string ToString(bool value)
        {
            return value ? "true" : "false";
        }
    }

    internal class ShortAttributeConverter : UxmlAttributeConverter<short>
    {
        public override short FromString(string value)
        {
            short.TryParse(value, out var result);
            return result;
        }
    }

    internal class IntAttributeConverter : UxmlAttributeConverter<int>
    {
        public override int FromString(string value) => UxmlUtility.ParseInt(value);
    }

    internal class LongAttributeConverter : UxmlAttributeConverter<long>
    {
        public override long FromString(string value)
        {
            long.TryParse(value, out var result);
            return result;
        }
    }

    internal class FloatAttributeConverter : UxmlAttributeConverter<float>
    {
        public override float FromString(string value) => UxmlUtility.ParseFloat(value);
    }

    internal class DoubleAttributeConverter : UxmlAttributeConverter<double>
    {
        public override double FromString(string value)
        {
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
            return result;
        }
    }

    internal class EnumAttributeConverter : UxmlAttributeConverter<Enum>
    {
        public Type type;
        public override Enum FromString(string value)
        {
            if (Enum.TryParse(type, value, out var result))
                return (Enum)result;
            return default;
        }
    }

    internal class ColorAttributeConverter : UxmlAttributeConverter<Color>
    {
        public override Color FromString(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var result);
            return result;
        }

        public override string ToString(Color value) => $"#{ColorUtility.ToHtmlStringRGBA(value)}";
    }

    internal class Hash128AttributeConverter : UxmlAttributeConverter<Hash128>
    {
        public override Hash128 FromString(string value) => Hash128.Parse(value);
    }

    internal class TypeAttributeConverter : UxmlAttributeConverter<Type>
    {
        public override Type FromString(string value) => UxmlUtility.ParseType(value);
        public override string ToString(Type value) => UxmlUtility.TypeToString(value);
    }

    internal class StringAttributeConverter : UxmlAttributeConverter<string>
    {
        public override string FromString(string value) => value;
    }

    internal class CharAttributeConverter : UxmlAttributeConverter<char>
    {
        public override char FromString(string value) => value[0];
    }

    internal class LengthAttributeConverter : UxmlAttributeConverter<Length>
    {
        public override Length FromString(string value) => Length.ParseString(value);
    }

    internal class LayerMaskConverter : UxmlAttributeConverter<LayerMask>
    {
        public override LayerMask FromString(string value) => UxmlUtility.ParseInt(value);
        public override string ToString(LayerMask lm) => lm.value.ToString();
    }

    internal class BoundsAttributeConverter : UxmlAttributeConverter<Bounds>
    {
        public override Bounds FromString(string value)
        {
            // Expected format value="cx,cy,cz,sx,sy,sz"
            var items = value.Split(',');
            if (items.Length != 6)
                return default;

            return new Bounds(new Vector3(UxmlUtility.ParseFloat(items[0]), UxmlUtility.ParseFloat(items[1]), UxmlUtility.ParseFloat(items[2])),
                new Vector3(UxmlUtility.ParseFloat(items[3]), UxmlUtility.ParseFloat(items[4]), UxmlUtility.ParseFloat(items[5])));
        }

        public override string ToString(Bounds value) => UxmlUtility.ValueToString(value);
    }

    internal class BoundsIntAttributeConverter : UxmlAttributeConverter<BoundsInt>
    {
        public override BoundsInt FromString(string value)
        {
            // Expected format value="px,py,pz,sx,sy,sz"
            var items = value.Split(',');
            if (items.Length != 6)
                return default;

            return new BoundsInt(UxmlUtility.ParseInt(items[0]), UxmlUtility.ParseInt(items[1]), UxmlUtility.ParseInt(items[2]),
                UxmlUtility.ParseInt(items[3]), UxmlUtility.ParseInt(items[4]), UxmlUtility.ParseInt(items[5]));
        }

        public override string ToString(BoundsInt value) => UxmlUtility.ValueToString(value);
    }

    internal class RectAttributeConverter : UxmlAttributeConverter<Rect>
    {
        public override Rect FromString(string value)
        {
            // Expected format value="x,y,w,h"
            var items = value.Split(',');
            if (items.Length != 4)
                return default;

            return new Rect(UxmlUtility.ParseFloat(items[0]), UxmlUtility.ParseFloat(items[1]), UxmlUtility.ParseFloat(items[2]), UxmlUtility.ParseFloat(items[3]));
        }

        public override string ToString(Rect value) => UxmlUtility.ValueToString(value);
    }

    internal class RectIntAttributeConverter : UxmlAttributeConverter<RectInt>
    {
        public override RectInt FromString(string value)
        {
            // Expected format value="x,y,w,h"
            var items = value.Split(',');
            if (items.Length != 4)
                return default;

            return new RectInt(UxmlUtility.ParseInt(items[0]), UxmlUtility.ParseInt(items[1]), UxmlUtility.ParseInt(items[2]), UxmlUtility.ParseInt(items[3]));
        }

        public override string ToString(RectInt value) => UxmlUtility.ValueToString(value);
    }

    internal class Vector2AttributeConverter : UxmlAttributeConverter<Vector2>
    {
        public override Vector2 FromString(string value)
        {
            // Expected format value="x,y"
            var items = value.Split(',');
            if (items.Length != 2)
                return default;

            return new Vector2(UxmlUtility.ParseFloat(items[0]), UxmlUtility.ParseFloat(items[1]));
        }

        public override string ToString(Vector2 value) => UxmlUtility.ValueToString(value);
    }

    internal class Vector2IntAttributeConverter : UxmlAttributeConverter<Vector2Int>
    {
        public override Vector2Int FromString(string value)
        {
            // Expected format value="x,y"
            var items = value.Split(',');
            if (items.Length != 2)
                return default;

            return new Vector2Int(UxmlUtility.ParseInt(items[0]), UxmlUtility.ParseInt(items[1]));
        }

        public override string ToString(Vector2Int value) => UxmlUtility.ValueToString(value);
    }

    internal class Vector3AttributeConverter : UxmlAttributeConverter<Vector3>
    {
        public override Vector3 FromString(string value)
        {
            // Expected format value="x,y,z"
            var items = value.Split(',');
            if (items.Length != 3)
                return default;

            return new Vector3(UxmlUtility.ParseFloat(items[0]), UxmlUtility.ParseFloat(items[1]), UxmlUtility.ParseFloat(items[2]));
        }

        public override string ToString(Vector3 value) => UxmlUtility.ValueToString(value);
    }

    internal class Vector3IntAttributeConverter : UxmlAttributeConverter<Vector3Int>
    {
        public override Vector3Int FromString(string value)
        {
            // Expected format value="x,y,z"
            var items = value.Split(',');
            if (items.Length != 3)
                return default;

            return new Vector3Int(UxmlUtility.ParseInt(items[0]), UxmlUtility.ParseInt(items[1]), UxmlUtility.ParseInt(items[2]));
        }

        public override string ToString(Vector3Int value) => UxmlUtility.ValueToString(value);
    }

    internal class Vector4AttributeConverter : UxmlAttributeConverter<Vector4>
    {
        public override Vector4 FromString(string value)
        {
            // Expected format value="x,y,z,w"
            var items = value.Split(',');
            if (items.Length != 4)
                return default;

            return new Vector4(UxmlUtility.ParseFloat(items[0]), UxmlUtility.ParseFloat(items[1]), UxmlUtility.ParseFloat(items[2]), UxmlUtility.ParseFloat(items[3]));
        }

        public override string ToString(Vector4 value) => UxmlUtility.ValueToString(value);
    }

    internal class ListAttributeConverter<T> : UxmlAttributeConverter<List<T>>
    {
        public override List<T> FromString(string value)
        {
            if (value == null)
                return null;

            var items = value.Split(',');
            if (items.Length <= 0 || !UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return null;

            var result = new List<T>();
            foreach (var item in items)
            {
                var s = item.Trim();
                if (string.IsNullOrEmpty(s))
                    continue;

                result.Add((T)converter.FromString(s));
            }

            return result;
        }

        public override string ToString(List<T> value)
        {
            if (!UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return string.Empty;

            var sb = GenericPool<StringBuilder>.Get().Clear();
            for (int i = 0; i < value.Count; i++)
            {
                var s = converter.ToString(value[i]);
                sb.Append(s);
                if (i + 1 != value.Count)
                    sb.Append(",");
            }

            var result = sb.ToString();
            GenericPool<StringBuilder>.Release(sb.Clear());
            return result;
        }
    }

    internal class ArrayAttributeConverter<T> : UxmlAttributeConverter<T[]>
    {
        private static ListAttributeConverter<T> s_ListAttributeConverter;

        public override T[] FromString(string value)
        {
            s_ListAttributeConverter ??= new ListAttributeConverter<T>();
            var result = s_ListAttributeConverter.FromString(value);
            return result.ToArray();
        }

        public override string ToString(T[] value)
        {
            if (!UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return string.Empty;

            var sb = GenericPool<StringBuilder>.Get().Clear();
            for (int i = 0; i < value.Length; i++)
            {
                var s = converter.ToString(value[i]);
                sb.Append(s);
                if (i + 1 != value.Length)
                    sb.Append(",");
            }

            var result = sb.ToString();
            GenericPool<StringBuilder>.Release(sb.Clear());
            return result;
        }
    }
}
