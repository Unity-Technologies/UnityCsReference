// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace Unity.Properties
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    readonly record struct ConverterKey
    {
        public readonly Type SourceType;
        public readonly Type DestinationType;

        public ConverterKey(Type source, Type destination)
        {
            SourceType = source;
            DestinationType = destination;
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    sealed class ConverterKeyComparer : IEqualityComparer<ConverterKey>
    {
        public static readonly ConverterKeyComparer Instance = new ();

        public bool Equals(ConverterKey x, ConverterKey y)
        {
            return x.SourceType == y.SourceType && x.DestinationType == y.DestinationType;
        }

        public int GetHashCode(ConverterKey obj)
        {
            unchecked
            {
                var hash = obj.SourceType?.TypeHandle.Value.GetHashCode() ?? 0;
                hash = hash * -1521134295 + obj.DestinationType?.TypeHandle.Value.GetHashCode() ?? 0;
                return hash;
            }
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    readonly struct ConversionRegistry : IEqualityComparer<ConversionRegistry>
    {
        readonly Dictionary<ConverterKey, Delegate> m_Converters;
        readonly Dictionary<ConverterKey, Func<Delegate>> m_LazyConverters;

        ConversionRegistry(Dictionary<ConverterKey, Delegate> storage)
        {
            m_Converters = storage;
            m_LazyConverters = new Dictionary<ConverterKey, Func<Delegate>>(256, ConverterKeyComparer.Instance);
        }

        public int ConverterCount => m_Converters?.Count ?? 0;

        public static ConversionRegistry Create()
        {
            return new ConversionRegistry(new Dictionary<ConverterKey, Delegate>(256, ConverterKeyComparer.Instance));
        }

        public void Register(Type source, Type destination, Delegate converter)
        {
            m_Converters[new ConverterKey(source, destination)] = converter ?? throw new ArgumentException(nameof(converter));
        }

        public void LazyRegister(Type source, Type destination, Func<Delegate> converter)
        {
            m_LazyConverters[new ConverterKey(source, destination)] = converter ?? throw new ArgumentException(nameof(converter));
        }

        public void Unregister(Type source, Type destination)
        {
            m_Converters.Remove(new ConverterKey(source, destination));
        }

        public void Apply(ConversionRegistry registry)
        {
            foreach (var c in registry.m_Converters)
            {
                Register(c.Key.SourceType, c.Key.DestinationType, c.Value);
            }
        }

        public Delegate GetConverter(Type source, Type destination)
        {
            var key = new ConverterKey(source, destination);
            if (!m_Converters.TryGetValue(key, out var converter) && m_LazyConverters.TryGetValue(key, out var func))
            {
                converter = func();
                if (converter != null)
                {
                    Register(source, destination, converter);
                    m_LazyConverters.Remove(key);
                }
            }

            return converter;
        }

        public bool TryGetConverter(Type source, Type destination, out Delegate converter)
        {
            converter = GetConverter(source, destination);
            return null != converter;
        }

        public void GetAllTypesConvertingToType(Type type, List<Type> result)
        {
            if (m_Converters == null)
                return;

            foreach (var key in m_Converters.Keys)
            {
                if (key.DestinationType == type)
                    result.Add(key.SourceType);
            }
        }

        public void GetAllTypesConvertingFromType(Type type, List<Type> result)
        {
            if (m_Converters == null)
                return;

            foreach (var key in m_Converters.Keys)
            {
                if (key.SourceType == type)
                    result.Add(key.DestinationType);
            }
        }

        public void GetAllConversions(List<(Type, Type)> result)
        {
            if (m_Converters == null)
                return;

            foreach (var key in m_Converters.Keys)
            {
                result.Add((key.SourceType, key.DestinationType));
            }
        }

        public bool Equals(ConversionRegistry x, ConversionRegistry y)
        {
            return x.m_Converters == y.m_Converters;
        }

        public int GetHashCode(ConversionRegistry obj)
        {
            return (obj.m_Converters != null ? obj.m_Converters.GetHashCode() : 0);
        }
    }

    /// <summary>
    /// Represents the method that will handle converting an object of type <typeparamref name="TSource"/> to an object of type <typeparamref name="TDestination"/>.
    /// </summary>
    /// <param name="value">The source value to be converted.</param>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
    public delegate TDestination TypeConverter<TSource, out TDestination>(ref TSource value);

    /// <summary>
    /// Helper class to handle type conversion during properties API calls.
    /// </summary>
    public static class TypeConversion
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal struct Unsafe
        {
            public static void Register(Type source, Type destination, Delegate converter)
            {
                s_GlobalConverters.Register(source, destination, converter);
            }

            public static void LazyRegister(Type source, Type destination, Func<Delegate> getConverterDelegate)
            {
                s_GlobalConverters.LazyRegister(source, destination, getConverterDelegate);
            }
        }

        static readonly ConversionRegistry s_GlobalConverters = ConversionRegistry.Create();

        static TypeConversion()
        {
            PrimitiveConverters.Register();
        }

        /// <summary>
        /// Registers a new converter from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="converter">Conversion delegate</param>
        /// <typeparam name="TSource">Type of the source object.</typeparam>
        /// <typeparam name="TDestination">Type of the destination object.</typeparam>
        public static void Register<TSource, TDestination>(TypeConverter<TSource, TDestination> converter)
        {
            s_GlobalConverters.Register(typeof(TSource), typeof(TDestination), converter);
        }

        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>The value converted to the <typeparamref name="TDestination"/> type.</returns>
        /// <exception cref="InvalidOperationException">No converter is registered for the given types.</exception>
        public static TDestination Convert<TSource, TDestination>(ref TSource value)
        {
            if (!TryConvert<TSource, TDestination>(ref value, out var destination))
            {
                throw new InvalidOperationException($"TypeConversion no converter has been registered for SrcType=[{typeof(TSource)}] to DstType=[{typeof(TDestination)}]");
            }

            return destination;
        }

        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">When this method returns, contains the converted destination value if the conversion succeeded; otherwise, default.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        ///<returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert<TSource, TDestination>(ref TSource source, out TDestination destination)
            => TryConvert(in s_GlobalConverters, ref source, out destination);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool TryConvert<TSource, TDestination>(in ConversionRegistry registry, ref TSource source, out TDestination destination)
        {
            if (registry.TryGetConverter(typeof(TSource), typeof(TDestination), out var converter))
            {
                var typedConverter = (TypeConverter<TSource, TDestination>)converter;
                destination = typedConverter(ref source);
                return true;
            }

            if (typeof(TSource) == typeof(TDestination))
            {
                destination = UnsafeUtility.As<TSource, TDestination>(ref source);
                return true;
            }

            if (PrimitivesConverters.TryConvertPrimitiveOrString(ref source, out destination))
                return true;

            if (TryConvertNullable(ref source, out destination))
                return true;

            if (TryConvertEnum(ref source, out destination))
                return true;

            if (TryConvertToUnityEngineObject(source, out destination))
                return true;

            if (typeof(TSource).IsValueType && typeof(TDestination).IsValueType)
            {
                destination = default;
                return false;
            }

            // Could be boxing :(
            if (source is TDestination assignable)
            {
                destination = assignable;
                return true;
            }

            if (typeof(TDestination).IsAssignableFrom(typeof(TSource)))
            {
                destination = (TDestination)(object)source;
                return true;
            }

            // T -> string conversions should be supported by default.
            if (typeof(TDestination) == typeof(string))
            {
                destination = (TDestination) (object) source?.ToString();
                return true;
            }

            // T -> object conversions should be supported by default.
            if (typeof(TDestination) == typeof(object))
            {
                // ReSharper disable once PossibleInvalidCastException
                destination = (TDestination) (object) source;
                return true;
            }

            // Special case where the source is null and of type object
            if (typeof(TSource) == typeof(object) && source == null)
            {
                destination = default;
                return true;
            }

            destination = default;
            return false;
        }

        static bool TryConvertNullable<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            var destinationUnderlyingType = Nullable.GetUnderlyingType(typeof(TDestination));
            var sourceUnderlyingType = Nullable.GetUnderlyingType(typeof(TSource));
            if (destinationUnderlyingType != null)
            {
                // We don't support the case where both types are nullable types when their underlying types don't match,
                // even if, in some cases, it is supported in C# (int? => float?).
                if (sourceUnderlyingType != null && destinationUnderlyingType != sourceUnderlyingType)
                {
                    destination = default;
                    return false;
                }

                if (source == null)
                {
                    destination = default;
                    return true;
                }

                try
                {
                    if (destinationUnderlyingType.IsEnum)
                    {
                        var enumUnderlyingType = Enum.GetUnderlyingType(destinationUnderlyingType);
                        var value = System.Convert.ChangeType(source, enumUnderlyingType);
                        destination = (TDestination)Enum.ToObject(destinationUnderlyingType, value);
                        return true;
                    }

                    destination = (TDestination)System.Convert.ChangeType(source, destinationUnderlyingType);
                    return true;
                } catch (Exception)
                {
                    destination = default;
                    return false;
                }
            }

            // Conversion from T? => T.
            if (sourceUnderlyingType != null && typeof(TDestination) == sourceUnderlyingType &&
                source != null) // This conversion would result in an InvalidOperationException. i.e. int v = (int)(default(int?));
            {
                destination = (TDestination)(object)source;
                return true;
            }

            destination = default;
            return false;
        }

        static bool TryConvertEnum<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            if (typeof(TDestination).IsEnum)
            {
                if (typeof(TSource) == typeof(string))
                {
                    try
                    {
                        destination = (TDestination)Enum.Parse(typeof(TDestination), (string)(object)source);
                    }
                    catch (ArgumentException)
                    {
                        destination = default;
                        return false;
                    }

                    return true;
                }

                if (IsNumericType(typeof(TSource)))
                {
                    destination = UnsafeUtility.As<TSource, TDestination>(ref source);
                    return true;
                }
            }

            destination = default;
            return false;
        }

        static bool TryConvertToUnityEngineObject<TSource, TDestination>(TSource source, out TDestination destination)
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(TDestination)))
            {
                destination = default;
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TSource)) ||
                source is UnityEngine.Object)
            {
                if (null == source)
                {
                    destination = default;
                    return true;
                }

                if (typeof(TDestination) == typeof(UnityEngine.Object))
                {
                    destination = (TDestination)(object)source;
                    return true;
                }
            }

            if (s_GlobalConverters.TryGetConverter(typeof(TSource), typeof(UnityEngine.Object), out var converter))
            {
                var unityObject = ((TypeConverter<TSource, UnityEngine.Object>)converter)(ref source);
                destination = (TDestination)(object)unityObject;
                return unityObject;
            }

            destination = default;
            return false;
        }

        static bool IsNumericType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        static class PrimitiveConverters
        {
            public static void Register()
            {
                // support System.Guid by default
                s_GlobalConverters.Register(typeof(string), typeof(Guid), (TypeConverter<string, Guid>)((ref string g) => new Guid(g)));
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static class PrimitivesConverters
        {
            public static bool TryConvertPrimitiveOrString<TSource, TDestination>(ref TSource source, out TDestination destination)
            {
                var stringType = typeof(string);
                var tSource = typeof(TSource);

                if (!tSource.IsPrimitive && tSource != stringType)
                {
                    destination = default;
                    return false;
                }

                var tDestination = typeof(TDestination);
                if (!tDestination.IsPrimitive && tDestination != stringType)
                {
                    destination = default;
                    return false;
                }

                var tSourceTypeCode = Type.GetTypeCode(tSource);

                switch (tSourceTypeCode)
                {
                    case TypeCode.Boolean:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, bool>(ref source));
                        return true;
                    case TypeCode.Byte:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, byte>(ref source));
                        return true;
                    case TypeCode.Char:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, char>(ref source));
                        return true;
                    case TypeCode.Double:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, double>(ref source));
                        return true;
                    case TypeCode.Int16:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, short>(ref source));
                        return true;
                    case TypeCode.Int32:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, int>(ref source));
                    return true;
                    case TypeCode.Int64:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, long>(ref source));
                        return true;
                    case TypeCode.SByte:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, sbyte>(ref source));
                        return true;
                    case TypeCode.Single:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, float>(ref source));
                        return true;
                    case TypeCode.String:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, string>(ref source));
                        return true;
                    case TypeCode.UInt16:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, ushort>(ref source));
                        return true;
                    case TypeCode.UInt32:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, uint>(ref source));
                        return true;
                    case TypeCode.UInt64:
                        destination = DoConvert<TDestination>(ref UnsafeUtility.As<TSource, ulong>(ref source));
                        return true;
                    default:
                        destination = default;
                        return false;
                }
            }

            private static TDestination DoConvert<TDestination>(ref bool source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    return UnsafeUtility.As<bool, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = source ? (byte)1 : (byte)0;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = source ? (char)1 : (char)0;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = source ? 1.0 : 0.0;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = source ? (short)1 : (short)0;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = source ? 1 : 0;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = source ? 1L : 0L;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = source ? (sbyte)1 : (sbyte)0;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = source ? (float)1 : (float)0;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source ? "true" : "false";
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = source ? (ushort)1 : (ushort)0;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = source ? 1U : 0U;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = source ? 1UL : 0UL;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref byte source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    return UnsafeUtility.As<byte, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref char source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    return UnsafeUtility.As<char, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref double source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0.0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    return UnsafeUtility.As<double, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref short source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    return UnsafeUtility.As<short, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref int source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref long source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref sbyte source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref float source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref string source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    if (bool.TryParse(source, out var v))
                        return UnsafeUtility.As<bool, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out bool result)
                        ? UnsafeUtility.As<bool, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    if (byte.TryParse(source, out var v))
                        return UnsafeUtility.As<byte, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out byte result)
                        ? UnsafeUtility.As<byte, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = !string.IsNullOrEmpty(source) ? source[0] : '\0';
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    return double.TryParse(source, out var v)
                        ? UnsafeUtility.As<double, TDestination>(ref v)
                        : default;
                }

                if (typeof(TDestination) == typeof(short))
                {
                    if (short.TryParse(source, out var v))
                        return UnsafeUtility.As<short, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out short result)
                        ? UnsafeUtility.As<short, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(int))
                {
                    if (int.TryParse(source, out var v))
                        return UnsafeUtility.As<int, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out int result)
                        ? UnsafeUtility.As<int, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(long))
                {
                    if (long.TryParse(source, out var v))
                        return UnsafeUtility.As<long, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out long result)
                        ? UnsafeUtility.As<long, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    if (sbyte.TryParse(source, out var v))
                        return UnsafeUtility.As<sbyte, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out sbyte result)
                        ? UnsafeUtility.As<sbyte, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(float))
                {
                    if (float.TryParse(source, out var v))
                        return UnsafeUtility.As<float, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out float result)
                        ? UnsafeUtility.As<float, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(string))
                {
                    return UnsafeUtility.As<string, TDestination>(ref source);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    if (ushort.TryParse(source, out var v))
                        return UnsafeUtility.As<ushort, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out ushort result)
                        ? UnsafeUtility.As<ushort, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    if (uint.TryParse(source, out var v))
                        return UnsafeUtility.As<uint, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out uint result)
                        ? UnsafeUtility.As<uint, TDestination>(ref result)
                        : default;
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    if (ulong.TryParse(source, out var v))
                        return UnsafeUtility.As<ulong, TDestination>(ref v);

                    return double.TryParse(source, out var fromDouble) &&
                           TryConvertPrimitiveOrString(ref fromDouble, out ulong result)
                        ? UnsafeUtility.As<ulong, TDestination>(ref result)
                        : default;
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref ushort source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref uint source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = (ulong)source;
                    return UnsafeUtility.As<ulong, TDestination>(ref v);
                }

                throw new ArgumentOutOfRangeException();
            }

            private static TDestination DoConvert<TDestination>(ref ulong source)
            {
                // Use type checks that JIT can optimize into direct branches
                if (typeof(TDestination) == typeof(bool))
                {
                    var v = source != 0;
                    return UnsafeUtility.As<bool, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(byte))
                {
                    var v = (byte)source;
                    return UnsafeUtility.As<byte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(char))
                {
                    var v = (char)source;
                    return UnsafeUtility.As<char, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(double))
                {
                    var v = (double)source;
                    return UnsafeUtility.As<double, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(short))
                {
                    var v = (short)source;
                    return UnsafeUtility.As<short, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(int))
                {
                    var v = (int)source;
                    return UnsafeUtility.As<int, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(long))
                {
                    var v = (long)source;
                    return UnsafeUtility.As<long, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(sbyte))
                {
                    var v = (sbyte)source;
                    return UnsafeUtility.As<sbyte, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(float))
                {
                    var v = (float)source;
                    return UnsafeUtility.As<float, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(string))
                {
                    var v = source.ToString(CultureInfo.InvariantCulture);
                    return UnsafeUtility.As<string, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ushort))
                {
                    var v = (ushort)source;
                    return UnsafeUtility.As<ushort, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(uint))
                {
                    var v = (uint)source;
                    return UnsafeUtility.As<uint, TDestination>(ref v);
                }

                if (typeof(TDestination) == typeof(ulong))
                {
                    var v = source;
                    return UnsafeUtility.As<ulong, TDestination>(ref source);
                }

                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
