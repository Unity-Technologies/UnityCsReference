// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
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
    ///* char
    ///* string
    ///* short
    ///* int
    ///* long
    ///* uint
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

    internal class UnsignedIntAttributeConverter : UxmlAttributeConverter<uint>
    {
        public override uint FromString(string value)
        {
            uint.TryParse(value, out var result);
            return result;
        }
    }

    internal class UnsignedLongAttributeConverter : UxmlAttributeConverter<ulong>
    {
        public override ulong FromString(string value)
        {
            ulong.TryParse(value, out var result);
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
            return cc.visualTreeAsset.GetAsset(value, typeof(T));
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
            if (items.Length <= 0 || !UxmlAttributeConverter.TryGetConverter<T>(out var converter))
                return null;

            var result = new List<T>();
            foreach (var item in items)
            {
                var s = item.Trim();
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
                var encoded = UxmlUtility.EncodeListItem(s);
                sb.Append(encoded);
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
                var encoded = UxmlUtility.EncodeListItem(s);
                sb.Append(encoded);
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
}
