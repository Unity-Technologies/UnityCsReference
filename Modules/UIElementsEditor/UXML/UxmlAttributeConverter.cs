// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.StyleSheets;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Cursor = UnityEngine.UIElements.Cursor;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal interface IUxmlAttributeConverter
    {
        public object FromString(string value, CreationContext cc);
        public string ToString(object value, VisualTreeAsset visualTreeAsset);
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
                if ((attributes & TypeAttributes.Abstract) != 0)
                    continue;

                Type conversionType = null;
                var t = converterType;
                while (t.BaseType != null && conversionType == null)
                {
                    t = t.BaseType;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(UxmlAttributeConverter<>))
                    {
                        conversionType = t.GetGenericArguments()[0];

                        if (conversionType.IsGenericType && conversionType.ContainsGenericParameters)
                            conversionType = conversionType.GetGenericTypeDefinition();
                    }
                }

                // Some converers such as List, Array and Unity Object are invoked manually.
                if (conversionType != null)
                    s_RegisteredConverterTypes[conversionType] = converterType;
            }
        }

        public static bool TryConvertFromString<T>(string value, CreationContext cc, out object result)
        {
            return TryConvertFromString(typeof(T), value, cc, out result);
        }

        public static bool TryConvertFromString(Type type, string value, CreationContext cc, out object result)
        {
            if (TryGetConverter(type, out var converter))
            {
                result = converter.FromString(value, cc);
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

            if (TryGetConverter(type, out var converter))
            {
                result = converter.ToString(value, visualTreeAsset);
                return true;
            }

            result = default;
            return false;
        }

        public static bool TryGetConverter<T>(out IUxmlAttributeConverter converter)
        {
            return TryGetConverter(typeof(T), out converter);
        }

        public static bool TryGetConverterType(Type type, out Type converterType)
        {
            if (s_RegisteredConverterTypes.TryGetValue(type, out converterType))
                return true;

            if (type.IsEnum)
            {
                converterType = typeof(EnumAttributeConverter<>).MakeGenericType(type);
                s_RegisteredConverterTypes[type] = converterType;
                return true;
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>))
                {
                    var genericTypeArgument0 = type.GetGenericArguments()[0];
                    if (HasConverter(genericTypeArgument0))
                    {
                        converterType = typeof(ListAttributeConverter<>).MakeGenericType(genericTypeArgument0);
                        s_RegisteredConverterTypes[type] = converterType;
                        return true;
                    }
                }
                else if (s_RegisteredConverterTypes.TryGetValue(genericTypeDefinition, out var genericConverterType))
                {
                    // Check for a generic converter type
                    converterType = genericConverterType.MakeGenericType(type.GetGenericArguments());
                    s_RegisteredConverterTypes[type] = converterType;
                    return true;
                }
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (HasConverter(elementType))
                {
                    converterType = typeof(ArrayAttributeConverter<>).MakeGenericType(elementType);
                    s_RegisteredConverterTypes[type] = converterType;
                    return true;
                }
            }

            // Unity object reference
            if (typeof(Object).IsAssignableFrom(type))
            {
                converterType = typeof(UnityObjectConverter<>).MakeGenericType(type);
                s_RegisteredConverterTypes[type] = converterType;
                return true;
            }

            return false;
        }

        public static bool HasConverter(Type type) => TryGetConverterType(type, out _);

        public static bool TryGetConverter(Type type, out IUxmlAttributeConverter converter)
        {
            if (TryGetConverterType(type, out var converterType))
            {
                converter = GetOrCreateConverter(type);
                return true;
            }

            converter = null;
            return false;
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
    /// Additionally, the data type must be serializable by the Unity serializer and adhere to
    /// the [[wiki:script-serialization-rules|serialization rules]].
    ///
    ///
    /// __Note:__ The following types have native support and you can use them without declaring a UxmlAttributeConverter:
    ///
    ///* bool
    ///* byte
    ///* sbyte
    ///* char
    ///* string
    ///* short
    ///* ushort
    ///* int
    ///* uint
    ///* long
    ///* ulong
    ///* Enum
    ///* float
    ///* double
    ///* Type([[UxmlTypeReferenceAttribute]])
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
    ///* [[Gradient]]
    ///* [[AnimationCurve]]
    ///* [[ToggleButtonGroupState]]
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
    /// <example>
    /// You can also use generic attribute converters. However, you must declare the attribute with the generic type. To use a type derived from a generic type, declare a new converter specifically for it.
    /// The following example uses a generic attribute converter and a custom property drawer to create a generic serialized dictionary:
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_SerializableDictionary.cs"/>
    /// </example>
    /// <example>
    /// Generic attribute converter:
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_SerializableDictionaryConverter.cs"/>
    /// </example>
    /// <example>
    /// Custom property drawer:
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_SerializableDictionaryPropertyDrawer.cs"/>
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

        string IUxmlAttributeConverter.ToString(object value, VisualTreeAsset visualTreeAsset) => ToString((T)value);
        object IUxmlAttributeConverter.FromString(string value, CreationContext cc) => FromString(value);
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

    internal class ByteAttributeConverter : UxmlAttributeConverter<byte>
    {
        public override byte FromString(string value) => UxmlUtility.ParseByte(value);
    }

    internal class SByteAttributeConverter : UxmlAttributeConverter<sbyte>
    {
        public override sbyte FromString(string value) => UxmlUtility.ParseSByte(value);
    }

    internal class ShortAttributeConverter : UxmlAttributeConverter<short>
    {
        public override short FromString(string value) => UxmlUtility.ParseShort(value);
    }

    internal class UShortAttributeConverter : UxmlAttributeConverter<ushort>
    {
        public override ushort FromString(string value) => UxmlUtility.ParseUShort(value);
    }

    internal class IntAttributeConverter : UxmlAttributeConverter<int>
    {
        public override int FromString(string value) => UxmlUtility.ParseInt(value);
    }

    internal class LongAttributeConverter : UxmlAttributeConverter<long>
    {
        public override long FromString(string value) => UxmlUtility.ParseLong(value);
    }

    internal class UnsignedIntAttributeConverter : UxmlAttributeConverter<uint>
    {
        public override uint FromString(string value) => UxmlUtility.ParseUint(value);
    }

    internal class UnsignedLongAttributeConverter : UxmlAttributeConverter<ulong>
    {
        public override ulong FromString(string value) => UxmlUtility.ParseULong(value);
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

    internal class EnumAttributeConverter<T> : UxmlAttributeConverter<Enum>
    {
        public override Enum FromString(string value)
        {
            if (Enum.TryParse(typeof(T), value, true, out var result))
                return (Enum)result;
            return default;
        }
    }

    internal class ColorAttributeConverter : UxmlAttributeConverter<Color>
    {
        static readonly Color k_DefaultColor = Color.clear;
        public override Color FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return k_DefaultColor;

            if (ColorUtility.TryParseHtmlString(value, out var res))
                return res;

            if (StyleSheetColor.TryGetColor(value, out res))
                return res;

            // Use AsSpan and slice directly without allocating a new string
            var spanValue = value.AsSpan();

            // Find the indices of the parentheses
            var startIndex = spanValue.IndexOf('(') + 1;
            var endIndex = spanValue.IndexOf(')');

            if (startIndex <= 0 || endIndex <= 0 || endIndex <= startIndex)
                return k_DefaultColor;

            // Slice the span directly between the parentheses and trim
            var spanStr = spanValue.Slice(startIndex, endIndex - startIndex).Trim();

            Span<float> floatValues = stackalloc float[4]; // Allocating on the stack for up to 4 floats
            var valueCount = UxmlUtility.SplitValues(spanStr, floatValues, ',');

            // Using rgb with 4 values is invalid and using rgba with 3 values is invalid
            if ((spanValue.IndexOf("rgba(", StringComparison.OrdinalIgnoreCase) >= 0 && valueCount != 4) ||
                (spanValue.IndexOf("rgb(", StringComparison.OrdinalIgnoreCase) >= 0 && valueCount != 3))
                return k_DefaultColor;

            // If alpha is outside of the range [0, 1], clamp it
            if (valueCount == 4)
                floatValues[3] = Mathf.Clamp(floatValues[3], 0, 1);

            return new Color(
                floatValues[0]/255f,
                floatValues[1]/255f,
                floatValues[2]/255f,
                valueCount == 3 ? 1 : floatValues[3]
            );
        }

        public override string ToString(Color value)
        {
            return StyleSheetToUss.ToUssString(value);
        }
    }

    internal class Hash128AttributeConverter : UxmlAttributeConverter<Hash128>
    {
        public override Hash128 FromString(string value) => Hash128.Parse(value);
    }

    internal class GUIDAttributeConverter : UxmlAttributeConverter<GUID>
    {
        public override GUID FromString(string value) => new GUID(value);
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

    internal class RenderingLayerMaskConverter : UxmlAttributeConverter<RenderingLayerMask>
    {
        public override RenderingLayerMask FromString(string value) => UxmlUtility.ParseUint(value);
        public override string ToString(RenderingLayerMask lm) => lm.value.ToString();
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

    internal class UnityObjectConverter<T> : IUxmlAttributeConverter where T : Object
    {
        public object FromString(string value, CreationContext cc)
        {
            // Optional support for referencing by template alias
            if (typeof(T) == typeof(VisualTreeAsset))
            {
                var vta = cc.visualTreeAsset.ResolveTemplate(value);
                if (vta != null)
                    return vta;
            }

            var asset = cc.visualTreeAsset.GetAsset(value, typeof(T));

            // This lets us handle assets that are "Missing (Type)" in the inspector.
            if (asset?.GetEntityId() == null)
            {
                // When dealing with asset overriddes the asset may not be in the direct visualTreeAsset so we fallback to Asset Database. (UUM-91641)
                var relativePath = AssetDatabase.GetAssetPath(cc.visualTreeAsset);
                var result = URIHelpers.ValidateAssetURL(relativePath, value);

                if (result.resolvedQueryAsset is T resolvedAsset)
                    asset = resolvedAsset;
                else if (result.result == URIValidationResult.OK)
                    asset = AssetDatabase.LoadAssetAtPath<T>(result.resolvedProjectRelativePath);
            }

            return asset;
        }

        public string ToString(object value, VisualTreeAsset visualTreeAsset)
        {
            var obj = value as Object;
            var result = URIHelpers.MakeAssetUri(obj);
            if (visualTreeAsset != null && !string.IsNullOrEmpty(result) && !visualTreeAsset.AssetEntryExists(result, typeof(T)))
                visualTreeAsset.RegisterAssetEntry(result, typeof(T), obj);
            return result;
        }
    }

    internal class ListAttributeConverter<T> : IUxmlAttributeConverter
    {
        public object FromString(string value, CreationContext cc)
        {
            if (value == null)
                return null;

            var items = value.Split(',');
            if (typeof(T) == typeof(Color) && value.Contains("("))
            {
                // Using the lookahead assertion to split the string by commas that are not inside parentheses
                items = Regex.Split(value, @"(?<=\)),");
            }

            if (items.Length <= 0 || !UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return null;

            var result = new List<T>();
            for (var i = 0; i < items.Length; i++)
            {
                var s = items[i].Trim();
                if (string.IsNullOrEmpty(s))
                    continue;

                var decoded = UxmlUtility.DecodeListItem(s);
                result.Add((T)converter.FromString(decoded, cc));
            }

            return result;
        }

        public string ToString(object value, VisualTreeAsset visualTreeAsset)
        {
            var list = value as IList<T>;
            if (!UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return string.Empty;

            var sb = StringBuilderPool.Get();
            for (int i = 0; i < list.Count; i++)
            {
                var s = converter.ToString(list[i], visualTreeAsset);
                if (typeof(T) == typeof(Color) && s.Contains("("))
                {
                    sb.Append(s);
                }
                else
                {
                    var encoded = UxmlUtility.EncodeListItem(s);
                    sb.Append(encoded);
                }

                if (i + 1 != list.Count)
                    sb.Append(",");
            }

            var result = sb.ToString();
            StringBuilderPool.Release(sb);
            return result;
        }
    }

    internal class ArrayAttributeConverter<T> : IUxmlAttributeConverter
    {
        private static ListAttributeConverter<T> s_ListAttributeConverter;

        public object FromString(string value, CreationContext cc)
        {
            s_ListAttributeConverter ??= new ListAttributeConverter<T>();
            var result = (List<T>)s_ListAttributeConverter.FromString(value, cc);
            return result.ToArray();
        }

        public string ToString(object value, VisualTreeAsset visualTreeAsset)
        {
            var array = value as T[];
            if (!UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return string.Empty;

            var sb = StringBuilderPool.Get();
            for (int i = 0; i < array.Length; i++)
            {
                var s = converter.ToString(array[i], visualTreeAsset);
                if (typeof(T) == typeof(Color) && s.Contains("("))
                {
                    sb.Append(s);
                }
                else
                {
                    var encoded = UxmlUtility.EncodeListItem(s);
                    sb.Append(encoded);
                }

                if (i + 1 != array.Length)
                    sb.Append(",");
            }

            var result = sb.ToString();
            StringBuilderPool.Release(sb);
            return result;
        }
    }

    internal class AnimationCurveAttributeConverter : UxmlAttributeConverter<AnimationCurve>
    {
        public override AnimationCurve FromString(string value)
        {
            if (value == null)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Cannot parse a null string to an AnimationCurve.");

            var animationItems = value.Split('+');
            if (animationItems.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] AnimationCurve does not have the correct format.");

            var keyFrameItems = animationItems[0].Split(';');
            if (keyFrameItems.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] AnimationCurve does not have the correct format.");

            using var pool = ListPool<Keyframe>.Get(out var keyFrames);
            foreach (var item in keyFrameItems)
            {
                var valueList = item.Trim().Substring(1, item.Length - 2); // Remove brackets
                var keyItems = valueList.Split(',');
                if (keyItems.Length < 8)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] AnimationCurve's KeyFrame does not have enough values.");

                var success = float.TryParse(keyItems[0], out var keyTime);
                success &= float.TryParse(keyItems[1], out var keyValue);
                success &= float.TryParse(keyItems[2], out var inTangent);
                success &= float.TryParse(keyItems[3], out var outTangent);
                success &= float.TryParse(keyItems[4], out var inWeight);
                success &= float.TryParse(keyItems[5], out var outWeight);
                success &= Enum.TryParse<AnimationUtility.TangentMode>(keyItems[6], out var leftTangent);
                success &= Enum.TryParse<AnimationUtility.TangentMode>(keyItems[7], out var rightTangent);
                success &= bool.TryParse(keyItems[8], out var broken);
                success &= Enum.TryParse<WeightedMode>(keyItems[9], out var gradientMode);

                if (!success)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] AnimationCurve's KeyFrame does not have the correct format.");

                var keyFrame = new Keyframe(keyTime, keyValue, inTangent, outTangent, inWeight, outWeight) { weightedMode = gradientMode };
                AnimationUtility.SetKeyLeftTangentMode(ref keyFrame, leftTangent);
                AnimationUtility.SetKeyRightTangentMode(ref keyFrame, rightTangent);
                AnimationUtility.SetKeyBroken(ref keyFrame, broken);
                keyFrames.Add(keyFrame);
            }

            var wrapModesGroup = animationItems[1];
            var wrapModeArray = wrapModesGroup.Substring(1, wrapModesGroup.Length - 2); // Remove parenthesis
            var wrapModes = wrapModeArray.Split(',');
            var parsed = Enum.TryParse<WrapMode>(wrapModes[0], out var preWrapMode);
            parsed &= Enum.TryParse<WrapMode>(wrapModes[1], out var postWrapMode);

            if (!parsed)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] AnimationCurve's wrap modes do not have the correct format.");

            return new AnimationCurve(keyFrames.ToArray()) { preWrapMode = preWrapMode, postWrapMode = postWrapMode };
        }

        public override string ToString(AnimationCurve value)
        {
            var sb = StringBuilderPool.Get();
            for (var i = 0; i < value.keys.Length; i++)
            {
                var key = value.keys[i];
                sb.Append($"{{{key.time.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{key.value.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{key.inTangent.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{key.outTangent.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{key.inWeight.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{key.outWeight.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{AnimationUtility.GetKeyLeftTangentMode(key)},");
                sb.Append($"{AnimationUtility.GetKeyRightTangentMode(key)},");
                sb.Append($"{AnimationUtility.GetKeyBroken(key)},");
                sb.Append($"{key.weightedMode}}}");

                if (i + 1 < value.keys.Length)
                {
                    sb.Append(";");
                }
            }

            sb.Append($"+({value.preWrapMode},{value.postWrapMode})");

            var result = sb.ToString();
            StringBuilderPool.Release(sb);
            return result;
        }
    }

    internal class GradientAttributeConverter : UxmlAttributeConverter<Gradient>
    {
        public override Gradient FromString(string value)
        {
            if (value == null)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Cannot parse a null string to a Gradient.");

            var index = value.IndexOf(":");
            if (index == -1)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient mode does not have the correct format.");

            var mode = value[..index];
            var keys = value.Remove(0, index + 1);
            var parsedMode = Enum.TryParse<GradientMode>(mode, out var gradientMode);
            if (!parsedMode)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient mode cannot be parsed correctly.");

            var gradientItems = keys.Split('+');
            if (gradientItems.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient's keys do not have the correct format.");

            using var poolColor = ListPool<GradientColorKey>.Get(out var gradientColorKeys);
            var colorKeyItems = gradientItems[0].Split(';');
            foreach (var item in colorKeyItems)
            {
                var valueList = item.Trim().Substring(1, item.Length - 2); // Remove brackets
                var colorKeys = valueList.Split(',');
                if (colorKeys.Length <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient's color key does not have enough values.");

                var success = float.TryParse(colorKeys[0], out var time);
                success &= ColorUtility.TryParseHtmlString(colorKeys[1], out var color);

                if (!success)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient's color key could not be parsed correctly.");

                gradientColorKeys.Add(new GradientColorKey(color, time));
            }

            using var poolAlpha = ListPool<GradientAlphaKey>.Get(out var gradientAlphaKeys);
            var alphaKeyItems = gradientItems[1].Split(';');
            foreach (var item in alphaKeyItems)
            {
                var valueList = item.Trim().Substring(1, item.Length - 2); // Remove brackets
                var alphaKeys = valueList.Split(',');
                if (alphaKeys.Length <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient's alpha key does not have enough values.");

                var success = float.TryParse(alphaKeys[0], out var time);
                success &= float.TryParse(alphaKeys[1], out var alpha);

                if (!success)
                    throw new ArgumentOutOfRangeException(nameof(value), "[UxmlAttributeConverter] Gradient's alpha key could not be parsed correctly.");

                gradientAlphaKeys.Add(new GradientAlphaKey(alpha, time));
            }

            var gradient = new Gradient { mode = gradientMode};
            gradient.SetKeys(gradientColorKeys.ToArray(), gradientAlphaKeys.ToArray());
            return gradient;
        }

        public override string ToString(Gradient value)
        {
            var sb = StringBuilderPool.Get();
            sb.Append($"{value.mode}:");

            for (var i = 0; i < value.colorKeys.Length; i++)
            {
                var colorKey = value.colorKeys[i];
                var s = $"{{{colorKey.time.ToString(CultureInfo.InvariantCulture)},#{ColorUtility.ToHtmlStringRGBA(colorKey.color)}}}";
                sb.Append(s);

                if (i + 1 != value.colorKeys.Length)
                    sb.Append(";");
            }

            sb.Append("+");

            for (var i = 0; i < value.alphaKeys.Length; i++)
            {
                var colorKey = value.alphaKeys[i];
                var s = $"{{{colorKey.time.ToString(CultureInfo.InvariantCulture)},{colorKey.alpha.ToString(CultureInfo.InvariantCulture)}}}";
                sb.Append(s);

                if (i + 1 != value.alphaKeys.Length)
                    sb.Append(";");
            }

            var result = sb.ToString();
            StringBuilderPool.Release(sb);
            return result;
        }
    }

    internal class ToggleButtonGroupStateAttributeConverter : UxmlAttributeConverter<ToggleButtonGroupState>
    {
        public override ToggleButtonGroupState FromString(string value)
        {
            ulong state = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '0')
                    continue;

                state |= 1UL << (value.Length - 1 - i);
            }

            return new ToggleButtonGroupState(state, value.Length);
        }

        public override string ToString(ToggleButtonGroupState value)
        {
            return value.ToString();
        }
    }

    internal class AngleAttributeConverter : UxmlAttributeConverter<Angle>
    {
        public override Angle FromString(string value) => UxmlUtility.ParseAngle(value);

        public override string ToString(Angle value) => value.ToString();
    }

    internal class BackgroundAttributeConverter : UxmlAttributeConverter<Background>
    {
        public override Background FromString(string value)
        {
            if (value.AsSpan().Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
                return new Background();

            var assetPath = Path.GetFileNameWithoutExtension(value);
            var response = URIHelpers.ValidateAssetURL(assetPath, value);
            if (response.result == URIValidationResult.OK && response.resolvedQueryAsset !=null)
            {
                return Background.FromObject(response.resolvedQueryAsset);
            }
            return new Background();
        }

        public override string ToString(Background value)
        {
            var image = value.GetSelectedImage();
            if (image != null)
                return URIHelpers.MakeAssetUri(image);
            return "none";
        }
    }

    internal class BackgroundPositionAttributeConverter : UxmlAttributeConverter<BackgroundPosition>
    {
        public override BackgroundPosition FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BackgroundPosition.Initial();

            var span = value.AsSpan().Trim();

            var firstSpace = span.IndexOf(' ');
            if (firstSpace == -1)
            {
                // Only one part
                var part = span;
                return char.IsLetter(part[0])
                    ? new BackgroundPosition(Enum.TryParse<BackgroundPositionKeyword>(part.ToString(), true, out var k) ? k : BackgroundPositionKeyword.Center)
                    : new BackgroundPosition(BackgroundPositionKeyword.Center, Length.ParseString(part.ToString()));
            }

            var part1 = span.Slice(0, firstSpace).Trim();
            var part2 = span.Slice(firstSpace + 1).Trim();

            // Check if there's a third part
            if (part2.IndexOf(' ') != -1)
                return BackgroundPosition.Initial();

            if (char.IsLetter(part1[0]))
            {
                if (Enum.TryParse<BackgroundPositionKeyword>(part1.ToString(), true, out var keyword))
                    return new BackgroundPosition(keyword, Length.ParseString(part2.ToString()));
                else
                    return BackgroundPosition.Initial();
            }

            // If first part is not a keyword, treat as Length
            return new BackgroundPosition(BackgroundPositionKeyword.Center, Length.ParseString(part1.ToString()));
        }

        public override string ToString(BackgroundPosition value)
        {
            // Following the syntax: "[ [ left | center | right | top | bottom | <length-percentage> ]
            // | [ left | center | right | <length-percentage> ] [ top | center | bottom | <length-percentage> ]
            // | [ center | [ left | right ] <length-percentage>? ] && [ center | [ top | bottom ] <length-percentage>? ] ]"
            var keywordString = value.keyword.ToString().ToLower();
            var offsetString = value.offset.ToString();

            return offsetString != "0" ? $"{keywordString} {offsetString}" : keywordString;
        }
    }

    internal class BackgroundRepeatAttributeConverter : UxmlAttributeConverter<BackgroundRepeat>
    {
        static readonly string s_RepeatXKebabCase = RepeatXY.RepeatX.ToString().ToKebabCase().ToLowerInvariant();
        static readonly string s_RepeatYKebabCase = RepeatXY.RepeatY.ToString().ToKebabCase().ToLowerInvariant();

        public override BackgroundRepeat FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return BackgroundRepeat.Initial();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var backgroundRepeat = BackgroundRepeat.Initial();

            if (parts.Length == 1)
            {
                if (Enum.TryParse<RepeatXY>(parts[0].ToCamelCase(), true, out var repeatXY))
                {
                    if (repeatXY == RepeatXY.RepeatX)
                    {
                        backgroundRepeat.x = Repeat.Repeat;
                        backgroundRepeat.y = Repeat.NoRepeat;
                    }
                    else if (repeatXY == RepeatXY.RepeatY)
                    {
                        backgroundRepeat.x = Repeat.NoRepeat;
                        backgroundRepeat.y = Repeat.Repeat;
                    }
                }
                else
                {
                    Enum.TryParse(parts[0], true, out backgroundRepeat.x);
                    backgroundRepeat.y = backgroundRepeat.x;
                }
            }
            else
            {
                if (Enum.TryParse<Repeat>(parts[0], true, out var x) && Enum.TryParse<Repeat>(parts[1], true, out var y))
                {
                    backgroundRepeat.x = x;
                    backgroundRepeat.y = y;
                }
                else
                {
                    return BackgroundRepeat.Initial();
                }
            }

            return backgroundRepeat;
        }

        public override string ToString(BackgroundRepeat value)
        {
            // Following the syntax: "repeat-x | repeat-y | [ repeat | space | round | no-repeat ]{1,2}"
            // Check for repeat-x
            if (value.x == Repeat.Repeat && value.y == Repeat.NoRepeat)
                return s_RepeatXKebabCase;

            // Check for repeat-y
            if (value.y == Repeat.Repeat && value.x == Repeat.NoRepeat)
                return s_RepeatYKebabCase;

            // Check for single value
            if (value.x == value.y)
                return value.x.ToString().ToKebabCase().ToLowerInvariant();

            return $"{value.x.ToString().ToKebabCase().ToLowerInvariant()} {value.y.ToString().ToKebabCase().ToLowerInvariant()}";
        }
    }

    internal class BackgroundSizeAttributeConverter : UxmlAttributeConverter<BackgroundSize>
    {
        public override BackgroundSize FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return BackgroundSize.Initial();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var backgroundSize = BackgroundSize.Initial();

            if (parts.Length == 1)
            {
                if (char.IsLetter(parts[0][0]))
                {
                    if (parts[0].AsSpan().Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        backgroundSize.x = Length.Auto();
                        backgroundSize.y = Length.Auto();
                    }
                    else if (Enum.TryParse<BackgroundSizeType>(parts[0], true, out var sizeType))
                    {
                        backgroundSize.sizeType = sizeType;
                    }
                    else
                    {
                        // Invalid value
                        return backgroundSize;
                    }
                }
                else
                {
                    backgroundSize.x = Length.ParseString(parts[0]);
                    backgroundSize.y = Length.Auto();
                }
            }
            else if (parts.Length == 2)
            {
                backgroundSize.x = parts[0].AsSpan().Trim().Equals("auto", StringComparison.OrdinalIgnoreCase) ? Length.Auto() : Length.ParseString(parts[0]);
                backgroundSize.y = parts[1].AsSpan().Trim().Equals("auto", StringComparison.OrdinalIgnoreCase) ? Length.Auto() : Length.ParseString(parts[1]);
            }

            return backgroundSize;
        }

        public override string ToString(BackgroundSize value)
        {
           // Following the syntax: "[ <length-percentage> | auto ]{1,2} | cover | contain"
           if (value.sizeType != BackgroundSizeType.Length)
               return value.sizeType.ToString().ToLowerInvariant();

           return value.y == Length.Auto() ? value.x.ToString() : $"{value.x} {value.y}";
        }
    }

    internal class CursorAttributeConverter : UxmlAttributeConverter<Cursor>
    {
        public override Cursor FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Cursor();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var cursor = new Cursor();

            switch (parts.Length)
            {
                case 3:
                {
                    var x = UxmlUtility.ParseFloat(parts[1]);
                    var y = UxmlUtility.ParseFloat(parts[2]);

                    cursor.hotspot = new Vector2(x, y);
                    goto case 1; // Go parse the texture
                }
                case 1:
                {
                    if (Enum.TryParse<MouseCursor>(parts[0], true, out var cursorId))
                    {
                        cursor.defaultCursorId = (int)cursorId;
                        return cursor;
                    }

                    var assetPath = Path.GetFileNameWithoutExtension(parts[0]);
                    var response = URIHelpers.ValidateAssetURL(assetPath, parts[0]);
                    if (response.result == URIValidationResult.OK && response.resolvedQueryAsset is Texture2D tex)
                    {
                        cursor.texture = tex;
                        return cursor;
                    }

                    break;
                }
            }

            return new Cursor();
        }

        public override string ToString(Cursor value)
        {
            // Following the syntax of: "[ [ <resource> | <url> ] [ <integer> <integer> ]? ] | [ arrow | text | resize-vertical | resize-horizontal | link | slide-arrow | resize-up-right | resize-up-left | move-arrow | rotate-arrow | scale-arrow | arrow-plus | arrow-minus | pan | orbit | zoom | fps | split-resize-up-down | split-resize-left-right | not-allowed ]"
            if (value.defaultCursorId != 0)
            {
                return ((MouseCursor)value.defaultCursorId).ToString();
            }

            var path = URIHelpers.MakeAssetUri(value.texture);

            return value.hotspot != Vector2.one ? $"{path} {value.hotspot.x} {value.hotspot.y}" : path;
        }
    }

    internal class FontAttributeConverter : UxmlAttributeConverter<Font>
    {
        public override Font FromString(string value)
        {
            if (string.IsNullOrEmpty(value) || value.AsSpan().Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
                return default;

            var assetPath = Path.GetFileNameWithoutExtension(value);
            var response = URIHelpers.ValidateAssetURL(assetPath, value);
            if (response.result == URIValidationResult.OK && response.resolvedQueryAsset is Font font)
            {
                return font;
            }

            return null;
        }

        public override string ToString(Font value)
        {
            return value != null ? URIHelpers.MakeAssetUri(value) : "none";
        }
    }

    internal class FontDefinitionAttributeConverter : UxmlAttributeConverter<FontDefinition>
    {
        public override FontDefinition FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;

            var assetPath = Path.GetFileNameWithoutExtension(value);
            var response = URIHelpers.ValidateAssetURL(assetPath, value);
            if (response.result == URIValidationResult.OK)
            {
                switch (response.resolvedQueryAsset)
                {
                    case Font font:
                        return new FontDefinition { font = font };
                    case FontAsset fontAsset:
                        return new FontDefinition { fontAsset = fontAsset };
                }
            }

            return new FontDefinition();
        }

        public override string ToString(FontDefinition value)
        {
            if (value.font != null)
                return URIHelpers.MakeAssetUri(value.font);
            if (value.fontAsset != null)
                return URIHelpers.MakeAssetUri(value.fontAsset);

            return string.Empty;
        }
    }

    internal class MaterialDefinitionAttributeConverter : UxmlAttributeConverter<MaterialDefinition>
    {
        const string k_EmptyState = "none";

        public override MaterialDefinition FromString(string value)
        {
            if (value.AsSpan().Trim().Equals(k_EmptyState, StringComparison.OrdinalIgnoreCase))
            {
                return new MaterialDefinition();
            }

            var assetPath = Path.GetFileNameWithoutExtension(value);
            var response = URIHelpers.ValidateAssetURL(assetPath, value);
            if (response.result == URIValidationResult.OK && response.resolvedQueryAsset != null)
            {
                return MaterialDefinition.FromObject(response.resolvedQueryAsset);
            }

            return new MaterialDefinition();
        }

        public override string ToString(MaterialDefinition value)
        {
            var material = value.material;
            return material != null ? URIHelpers.MakeAssetUri(material) : k_EmptyState;
        }
    }

    internal class RotateAttributeConverter : UxmlAttributeConverter<Rotate>
    {
        public override Rotate FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Rotate.Initial();
            }

            var spanStr = value.AsSpan().Trim();
            if (spanStr.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Rotate.None();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            switch (parts.Length)
            {
                case 1:
                {
                    // Only angle with unit
                    var angle = Angle.TryParseString(parts[0], out var result) ? result : default;
                    return new Rotate(angle);
                }
                case 2:
                {
                    // Axis and angle
                    var axis = ParseAxis(parts[0]);
                    var angle = Angle.TryParseString(parts[1], out var result) ? result : default;
                    return new Rotate(angle, axis);
                }
                case 4:
                {
                    // Vector and angle
                    var axis = new Vector3(UxmlUtility.ParseFloat(parts[0]), UxmlUtility.ParseFloat(parts[1]), UxmlUtility.ParseFloat(parts[2]));
                    var angle = Angle.TryParseString(parts[3], out var result) ? result : default;
                    return new Rotate(angle, axis);
                }
                default:
                    return Rotate.Initial();
            }

            static Vector3 ParseAxis(string axisString)
            {
                if (axisString.Equals("x", StringComparison.OrdinalIgnoreCase))
                    return Vector3.right;
                if (axisString.Equals("y", StringComparison.OrdinalIgnoreCase))
                    return Vector3.up;
                return Vector3.forward;
            }
        }

        public override string ToString(Rotate value)
        {
            // Following the syntax: "none | [ x | y | z | <number>{3} ] && <angle> | <angle>"
            if (value.IsNone())
                return "none";

            // Reduce the expression to just the angle if possible
            if (value.axis == Vector3.forward)
            {
                return value.angle.ToString();
            }

            // Reduce the expression if possible
            if (value.axis == Vector3.right)
            {
                return $"x {value.angle.ToString()}";
            }

            if (value.axis == Vector3.up)
            {
                return $"y {value.angle.ToString()}";
            }

            return FormattableString.Invariant($"{value.axis.x} {value.axis.y} {value.axis.z} {value.angle}");
        }
    }

    internal class ScaleAttributeConverter : UxmlAttributeConverter<Scale>
    {
        public override Scale FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Scale.Initial();
            }

            var spanStr = value.AsSpan().Trim();
            if (spanStr.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Scale.None();
            }

            Span<float> values = stackalloc float[3]; // Allocating on the stack for up to 3 floats
            var valueCount = UxmlUtility.SplitValues(spanStr, values, ' ');

            switch (valueCount)
            {
                case 1:
                    return new Scale(new Vector2(values[0], values[0])); // Uniform scale
                case 2:
                    return new Scale(new Vector2(values[0], values[1])); // Two-axis scale
                case 3:
                    return new Scale(new Vector3(values[0], values[1], values[2])); // Full 3-axis scale
                default:
                    return Scale.Initial();
            }
        }

        public override string ToString(Scale value)
        {
            // Check for "none" case
            if (value.IsNone())
                return "none";

            // Check for uniform scale
            if (Mathf.Approximately(value.value.x, value.value.y) && Mathf.Approximately(value.value.y, value.value.z))
            {
                return value.value.x.ToString(CultureInfo.InvariantCulture);
            }

            // Check for Two-axis scale
            if (Mathf.Approximately(value.value.z, 1))
            {
                return $"{value.value.x.ToString(CultureInfo.InvariantCulture)} {value.value.y.ToString(CultureInfo.InvariantCulture)}";
            }

            // Return full three-component scale
            return FormattableString.Invariant($"{value.value.x} {value.value.y} {value.value.z}");
        }
    }

    internal class TextShadowAttributeConverter : UxmlAttributeConverter<TextShadow>
    {
        private readonly ColorAttributeConverter k_ColorConverter = new();

        public override TextShadow FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new TextShadow();
            }

            var textShadow = new TextShadow();

            // Split string into parts (numbers and color)
            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // If the third part is a Length, then the format is offset.x offset.y blur radius color
            var colorPart = "";
            int indexForColorPart;

            if (parts.Length < 3)
            {
                return textShadow;
            }

            if (parts[2].Equals("0px", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("0") || Length.ParseString(parts[2]) != default)
            {
                var offsetX = Length.ParseString(parts[0]).pixelValue;
                var offsetY = Length.ParseString(parts[1]).pixelValue;
                textShadow.offset = new Vector2(offsetX, offsetY);
                textShadow.blurRadius = Length.ParseString(parts[2]).pixelValue;
                indexForColorPart = 3;
            }
            else
            {
                var offset = Length.ParseString(parts[0]).pixelValue;
                textShadow.offset = new Vector2(offset, offset);
                textShadow.blurRadius = Length.ParseString(parts[1]).pixelValue;
                indexForColorPart = 2;
            }


            for (var i = indexForColorPart; i < parts.Length; i++)
            {
                colorPart += parts[i];
            }

            if (!string.IsNullOrEmpty(colorPart))
            {
                textShadow.color = k_ColorConverter.FromString(colorPart);
            }

            return textShadow;
        }


        public override string ToString(TextShadow value)
        {
            // Following the syntax of: "<length>{2,3} && <color>?"
            return value.offset.x == value.offset.y
                ? $"{value.offset.x}px {value.blurRadius}px {k_ColorConverter.ToString(value.color)}"
                : $"{value.offset.x}px {value.offset.y}px {value.blurRadius}px {k_ColorConverter.ToString(value.color)}";
        }
    }

    internal class TimeValueAttributeConverter : UxmlAttributeConverter<TimeValue>
    {
        public override TimeValue FromString(string value) => TimeValue.TryParseString(value, out var result) ? result : default;

        public override string ToString(TimeValue value) => value.ToString();
    }

    internal class TransformOriginAttributeConverter : UxmlAttributeConverter<TransformOrigin>
    {
        public override TransformOrigin FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return TransformOrigin.Initial();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Length x = Length.Percent(50) , y = Length.Percent(50);
            float z = 0;

            switch (parts.Length)
            {
                case 1: //Single parameter, Could be x-offset(length or %) or offset-keyword (left, right, top, bottom, or center)
                {
                    var val = ReadTransformOriginEnum(parts[0], out _, out var isHorizontal);
                    if (isHorizontal)     //We apply on X by default, except if we have top or bottom
                        x = val;
                    else
                        y = val;
                    break;
                }
                case 3: // xyz
                    z = UxmlUtility.ParseFloat(parts[2]);

                    goto case 2; //Go parse the first arguments
                case 2: //two parameter ( x, y ) whether they are value or keyword. The order can be inverted if using keywords (ie top, left)
                {
                    var len1 = ReadTransformOriginEnum(parts[0], out var isVertical1, out var isHorizontal1);
                    var len2 = ReadTransformOriginEnum(parts[1], out var isVertical2, out var isHorizontal2);

                    if (!isHorizontal1 || !isVertical2)    //Argument probably are "swapped" if one of both cant be assigned "in order" to x, y
                    {
                        if (isHorizontal2 && isVertical1)
                        {
                            x = len2;
                            y = len1;
                        }
                    }
                    else
                    {
                        x = len1;
                        y = len2;
                    }

                    break;
                }
            }


            return new TransformOrigin(x, y, z);
        }

        static private Length ReadTransformOriginEnum(string value, out bool isVertical, out bool isHorizontal)
        {
            var spanStr = value.AsSpan().Trim();
            if (spanStr.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                isVertical = true; isHorizontal = true;
                return Length.Auto();
            }

            if (spanStr.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                isVertical = true; isHorizontal = true;
                return Length.None();
            }

            if (char.IsLetter(value[0]) && Enum.TryParse<TransformOriginOffset>(value.Replace(",", ""), true, out var enumValue))
            {
                switch (enumValue)
                {
                    case TransformOriginOffset.Left:
                        isVertical = false; isHorizontal = true;
                        return Length.Percent(0);
                    case TransformOriginOffset.Top:
                        isVertical = true; isHorizontal = false;
                        return Length.Percent(0);
                    case TransformOriginOffset.Center:
                        isVertical = true; isHorizontal = true;
                        return Length.Percent(50);
                    case TransformOriginOffset.Right:
                        isVertical = false; isHorizontal = true;
                        return Length.Percent(100);
                    case TransformOriginOffset.Bottom:
                        isVertical = true; isHorizontal = false;
                        return Length.Percent(100);
                }
            }
            else
            {
                isVertical = true; isHorizontal = true;
                return Length.ParseString(value, Length.Percent(50));
            }

            isVertical = false; isHorizontal = false; // should not matter because there would be no ambiguity
            return Length.Auto();
        }

        public override string ToString(TransformOrigin value)
        {
            var sb = StringBuilderPool.Get();
            var hasY = false;

            string result;

            // Handle y-axis reduction (top, bottom, center, or specific value)
            if (value.y.value == 0)
            {
                sb.Append("top");
                hasY = true;
            }
            else if (value.y == new Length(100, LengthUnit.Percent))
            {
                sb.Append("bottom");
                hasY = true;
            }

            // Handle x-axis reduction (left, right, center, or specific value)
            if (value.x.value == 0)
            {
                if (hasY) sb.Append(' ');
                sb.Append("left");
            }
            else if (value.x == new Length(100, LengthUnit.Percent))
            {
                if (hasY) sb.Append(' ');
                sb.Append("right");
            }
            else if (value.x == new Length(50, LengthUnit.Percent) && !hasY)
            {
                sb.Append("center");
                if (value.y == new Length(50, LengthUnit.Percent))
                {
                    result = sb.ToString();
                    StringBuilderPool.Release(sb);
                    return result;
                }
            }
            else if (value.x != new Length(50, LengthUnit.Percent))
            {
                if (hasY) sb.Append(' ');
                sb.Append(value.x.ToString());
            }

            // If we still haven't written y
            if (!hasY && value.y != new Length(50, LengthUnit.Percent))
            {
                sb.Append(' ');
                sb.Append(value.y.ToString());
            }

            // Handle z-value (optional)
            if (value.z != 0)
            {
                sb.Append($" {value.z}");
            }

            result = sb.ToString();
            StringBuilderPool.Release(sb);
            return result;
        }
    }

    internal class TranslateAttributeConverter : UxmlAttributeConverter<Translate>
    {
        public override Translate FromString(string value)
        {
            var spanStr = value.AsSpan().Trim();
            if (string.IsNullOrEmpty(value) || spanStr.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Translate.None();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Length x = 0, y = 0;
            float z = 0;

            switch (parts.Length)
            {
                case 1: // If only one argument, the translation is along both axes
                    x = y = Length.ParseString(parts[0]);
                    break;
                case 2: //X Y value
                        x = Length.ParseString(parts[0]);
                        y = Length.ParseString(parts[1]);
                    break;
                case 3: //X Y Z value
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out z);
                    goto case 2; //Parse the first 2 parameters
            }
            return new Translate(x, y, z);
        }

        public override string ToString(Translate value)
        {
            // Check for "none" case
            if (value.IsNone())
                return "none";

            // Check for uniform scale
            if (Length.Approximately(value.x, value.y))
            {
                return value.x.ToString();
            }

            // Check for Two-axis scale
            if (Mathf.Approximately(value.z, 0))
            {
                return $"{value.x.ToString()} {value.y.ToString()}";
            }

            // Return full three-component scale
            return $"{value.x.ToString()} {value.y.ToString()} {value.z.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    internal class RatioAttributeConverter : UxmlAttributeConverter<Ratio>
    {
        public override Ratio FromString(string value)
        {
            if (value.StartsWith("auto", StringComparison.OrdinalIgnoreCase))
            {
                return Ratio.Auto();
            }

            if (value.Contains("/") || value.Contains(":"))
            {
                var parts = value.Split('/', ':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double h))
                    {
                        if (h <= 0 || w <= 0 || double.IsNaN(h) || double.IsNaN(w))
                        {
                            return Ratio.Auto();
                        }

                        return new Ratio((float)(w / h));
                    }
                }
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                if (val <= 0 || double.IsNaN(val))
                {
                    return Ratio.Auto();
                }

                return new Ratio((float)val);
            }

            return Ratio.Auto();
        }

        public override string ToString(Ratio value)
        {
            if (value.IsAuto())
            {
                return string.Empty;
            }
            return value.ToString();
        }
    }
}
