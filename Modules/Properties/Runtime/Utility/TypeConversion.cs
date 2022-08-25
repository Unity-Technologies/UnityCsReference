// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Properties
{
    readonly struct ConversionRegistry : IEqualityComparer<ConversionRegistry>
    {
        class ConverterKeyComparer : IEqualityComparer<ConverterKey>
        {
            public bool Equals(ConverterKey x, ConverterKey y)
            {
                return x.SourceType == y.SourceType && x.DestinationType == y.DestinationType;
            }

            public int GetHashCode(ConverterKey obj)
            {
                return ((obj.SourceType != null ? obj.SourceType.GetHashCode() : 0) * 397) ^ (obj.DestinationType != null ? obj.DestinationType.GetHashCode() : 0);
            }
        }

        static readonly ConverterKeyComparer Comparer = new ConverterKeyComparer();

        readonly struct ConverterKey
        {
            public readonly Type SourceType;
            public readonly Type DestinationType;

            public ConverterKey(Type source, Type destination)
            {
                SourceType = source;
                DestinationType = destination;
            }
        }

        readonly Dictionary<ConverterKey, Delegate> m_Converters;

        ConversionRegistry(Dictionary<ConverterKey, Delegate> storage)
        {
            m_Converters = storage;
        }

        public int ConverterCount => m_Converters?.Count ?? 0;

        public static ConversionRegistry Create()
        {
            return new ConversionRegistry(new Dictionary<ConverterKey, Delegate>(Comparer));
        }

        public void Register(Type source, Type destination, Delegate converter)
        {
            m_Converters[new ConverterKey(source, destination)] = converter ?? throw new ArgumentException(nameof(converter));
        }

        public void Unregister(Type source, Type destination)
        {
            m_Converters.Remove(new ConverterKey(source, destination));
        }

        public Delegate GetConverter(Type source, Type destination)
        {
            var key = new ConverterKey(source, destination);
            return m_Converters.TryGetValue(key, out var converter)
                ? converter
                : null;
        }

        public bool TryGetConverter(Type source, Type destination, out Delegate converter)
        {
            converter = GetConverter(source, destination);
            return null != converter;
        }

        public void GetAllTypesConvertingToType(Type type, List<Type> result)
        {
            foreach (var key in m_Converters.Keys)
            {
                if (key.DestinationType == type)
                    result.Add(key.SourceType);
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
        {
            if (s_GlobalConverters.TryGetConverter(typeof(TSource), typeof(TDestination), out var converter))
            {
                destination = ((TypeConverter<TSource, TDestination>) converter)(ref source);
                return true;
            }

            if (typeof(TSource).IsValueType && typeof(TSource) == typeof(TDestination))
            {
                destination = UnsafeUtility.As<TSource, TDestination>(ref source);
                return true;
            }

            if (TypeTraits<TDestination>.IsNullable)
            {
                // Both types are nullable types, but the underlying types don't match. In some cases, this is supported in C# (int? => float?),
                // but we don't support this case.
                if (TypeTraits<TSource>.IsNullable && Nullable.GetUnderlyingType(typeof(TDestination)) != Nullable.GetUnderlyingType(typeof(TSource)))
                {
                    destination = default;
                    return false;
                }

                var underlyingType = Nullable.GetUnderlyingType(typeof(TDestination));
                if (underlyingType.IsEnum)
                {
                    var enumUnderlyingType = Enum.GetUnderlyingType(underlyingType);
                    var value = System.Convert.ChangeType(source, enumUnderlyingType);
                    destination = (TDestination) Enum.ToObject(underlyingType, value);
                    return true;
                }

                if (source == null)
                {
                    destination = default;
                    return true;
                }

                destination = (TDestination) System.Convert.ChangeType(source, underlyingType);
                return true;
            }

            // Conversion from T? => T.
            if (TypeTraits<TSource>.IsNullable && typeof(TDestination) == Nullable.GetUnderlyingType(typeof(TSource)))
            {
                // This conversion would result in an InvalidOperationException.
                // i.e. int v = (int)(default(int?));
                if (null == source)
                {
                    destination = default;
                    return false;
                }
                destination = (TDestination) (object) source;
                return true;
            }

            if (TypeTraits<TDestination>.IsUnityObject)
            {
                if (TryConvertToUnityEngineObject(source, out destination))
                {
                    return true;
                }
            }

            if (TypeTraits<TDestination>.IsEnum)
            {
                if (typeof(TSource) == typeof(string))
                {
                    try
                    {
                        destination = (TDestination) Enum.Parse(typeof(TDestination), (string) (object) source);
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

            // Could be boxing :(
            if (source is TDestination assignable)
            {
                destination = assignable;
                return true;
            }

            if (typeof(TDestination).IsAssignableFrom(typeof(TSource)))
            {
                destination = (TDestination) (object) source;
                return true;
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
                var unityObject = ((TypeConverter<TSource, UnityEngine.Object>) converter)(ref source);
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
                // signed integral types
                RegisterInt8Converters();
                RegisterInt16Converters();
                RegisterInt32Converters();
                RegisterInt64Converters();

                // unsigned integral types
                RegisterUInt8Converters();
                RegisterUInt16Converters();
                RegisterUInt32Converters();
                RegisterUInt64Converters();

                // floating point types
                RegisterFloat32Converters();
                RegisterFloat64Converters();

                // .net types
                RegisterBooleanConverters();
                RegisterCharConverters();
                RegisterStringConverters();
                RegisterObjectConverters();

                // support System.Guid by default
                s_GlobalConverters.Register(typeof(string), typeof(Guid), (TypeConverter<string, Guid>)((ref string g) => new Guid(g)));
            }

            static void RegisterInt8Converters()
            {
                s_GlobalConverters.Register(typeof(sbyte), typeof(char), (TypeConverter<sbyte, char>)((ref sbyte v) => (char) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(bool), (TypeConverter<sbyte, bool>)((ref sbyte v) => v != 0));
                s_GlobalConverters.Register(typeof(sbyte), typeof(short), (TypeConverter<sbyte, short>)((ref sbyte v) => (short) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(int), (TypeConverter<sbyte, int>)((ref sbyte v) => (int) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(long), (TypeConverter<sbyte, long>)((ref sbyte v) => (long) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(byte), (TypeConverter<sbyte, byte>)((ref sbyte v) => (byte) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(ushort), (TypeConverter<sbyte, ushort>)((ref sbyte v) => (ushort) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(uint), (TypeConverter<sbyte, uint>)((ref sbyte v) => (uint) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(ulong), (TypeConverter<sbyte, ulong>)((ref sbyte v) => (ulong) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(float), (TypeConverter<sbyte, float>)((ref sbyte v) => (float) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(double), (TypeConverter<sbyte, double>)((ref sbyte v) => (double) v));
                s_GlobalConverters.Register(typeof(sbyte), typeof(object), (TypeConverter<sbyte, object>)((ref sbyte v) => (object) v));
            }

            static void RegisterInt16Converters()
            {
                s_GlobalConverters.Register(typeof(short), typeof(sbyte), (TypeConverter<short, sbyte>)((ref short v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(short), typeof(char), (TypeConverter<short, char>)((ref short v) => (char) v));
                s_GlobalConverters.Register(typeof(short), typeof(bool), (TypeConverter<short, bool>)((ref short v) => v != 0));
                s_GlobalConverters.Register(typeof(short), typeof(int), (TypeConverter<short, int>)((ref short v) => (int) v));
                s_GlobalConverters.Register(typeof(short), typeof(long), (TypeConverter<short, long>)((ref short v) => (long) v));
                s_GlobalConverters.Register(typeof(short), typeof(byte), (TypeConverter<short, byte>)((ref short v) => (byte) v));
                s_GlobalConverters.Register(typeof(short), typeof(ushort), (TypeConverter<short, ushort>)((ref short v) => (ushort) v));
                s_GlobalConverters.Register(typeof(short), typeof(uint), (TypeConverter<short, uint>)((ref short v) => (uint) v));
                s_GlobalConverters.Register(typeof(short), typeof(ulong), (TypeConverter<short, ulong>)((ref short v) => (ulong) v));
                s_GlobalConverters.Register(typeof(short), typeof(float), (TypeConverter<short, float>)((ref short v) => (float) v));
                s_GlobalConverters.Register(typeof(short), typeof(double), (TypeConverter<short, double>)((ref short v) => (double) v));
                s_GlobalConverters.Register(typeof(short), typeof(object), (TypeConverter<short, object>)((ref short v) => (object) v));
            }

            static void RegisterInt32Converters()
            {
                s_GlobalConverters.Register(typeof(int), typeof(sbyte), (TypeConverter<int, sbyte>)((ref int v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(int), typeof(char), (TypeConverter<int, char>)((ref int v) => (char) v));
                s_GlobalConverters.Register(typeof(int), typeof(bool), (TypeConverter<int, bool>)((ref int v) => v != 0));
                s_GlobalConverters.Register(typeof(int), typeof(short), (TypeConverter<int, short>)((ref int v) => (short) v));
                s_GlobalConverters.Register(typeof(int), typeof(long), (TypeConverter<int, long>)((ref int v) => (long) v));
                s_GlobalConverters.Register(typeof(int), typeof(byte), (TypeConverter<int, byte>)((ref int v) => (byte) v));
                s_GlobalConverters.Register(typeof(int), typeof(ushort), (TypeConverter<int, ushort>)((ref int v) => (ushort) v));
                s_GlobalConverters.Register(typeof(int), typeof(uint), (TypeConverter<int, uint>)((ref int v) => (uint) v));
                s_GlobalConverters.Register(typeof(int), typeof(ulong), (TypeConverter<int, ulong>)((ref int v) => (ulong) v));
                s_GlobalConverters.Register(typeof(int), typeof(float), (TypeConverter<int, float>)((ref int v) => (float) v));
                s_GlobalConverters.Register(typeof(int), typeof(double), (TypeConverter<int, double>)((ref int v) => (double) v));
                s_GlobalConverters.Register(typeof(int), typeof(object), (TypeConverter<int, object>)((ref int v) => (object) v));
            }

            static void RegisterInt64Converters()
            {
                s_GlobalConverters.Register(typeof(long), typeof(sbyte), (TypeConverter<long, sbyte>)((ref long v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(long), typeof(char), (TypeConverter<long, char>)((ref long v) => (char) v));
                s_GlobalConverters.Register(typeof(long), typeof(bool), (TypeConverter<long, bool>)((ref long v) => v != 0));
                s_GlobalConverters.Register(typeof(long), typeof(short), (TypeConverter<long, short>)((ref long v) => (short) v));
                s_GlobalConverters.Register(typeof(long), typeof(int), (TypeConverter<long, int>)((ref long v) => (int) v));
                s_GlobalConverters.Register(typeof(long), typeof(byte), (TypeConverter<long, byte>)((ref long v) => (byte) v));
                s_GlobalConverters.Register(typeof(long), typeof(ushort), (TypeConverter<long, ushort>)((ref long v) => (ushort) v));
                s_GlobalConverters.Register(typeof(long), typeof(uint), (TypeConverter<long, uint>)((ref long v) => (uint) v));
                s_GlobalConverters.Register(typeof(long), typeof(ulong), (TypeConverter<long, ulong>)((ref long v) => (ulong) v));
                s_GlobalConverters.Register(typeof(long), typeof(float), (TypeConverter<long, float>)((ref long v) => (float) v));
                s_GlobalConverters.Register(typeof(long), typeof(double), (TypeConverter<long, double>)((ref long v) => (double) v));
                s_GlobalConverters.Register(typeof(long), typeof(object), (TypeConverter<long, object>)((ref long v) => (object) v));
            }

            static void RegisterUInt8Converters()
            {
                s_GlobalConverters.Register(typeof(byte), typeof(sbyte), (TypeConverter<byte, sbyte>)((ref byte v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(byte), typeof(char), (TypeConverter<byte, char>)((ref byte v) => (char) v));
                s_GlobalConverters.Register(typeof(byte), typeof(bool), (TypeConverter<byte, bool>)((ref byte v) => v != 0));
                s_GlobalConverters.Register(typeof(byte), typeof(short), (TypeConverter<byte, short>)((ref byte v) => (short) v));
                s_GlobalConverters.Register(typeof(byte), typeof(int), (TypeConverter<byte, int>)((ref byte v) => (int) v));
                s_GlobalConverters.Register(typeof(byte), typeof(long), (TypeConverter<byte, long>)((ref byte v) => (long) v));
                s_GlobalConverters.Register(typeof(byte), typeof(ushort), (TypeConverter<byte, ushort>)((ref byte v) => (ushort) v));
                s_GlobalConverters.Register(typeof(byte), typeof(uint), (TypeConverter<byte, uint>)((ref byte v) => (uint) v));
                s_GlobalConverters.Register(typeof(byte), typeof(ulong), (TypeConverter<byte, ulong>)((ref byte v) => (ulong) v));
                s_GlobalConverters.Register(typeof(byte), typeof(float), (TypeConverter<byte, float>)((ref byte v) => (float) v));
                s_GlobalConverters.Register(typeof(byte), typeof(double), (TypeConverter<byte, double>)((ref byte v) => (double) v));
                s_GlobalConverters.Register(typeof(byte), typeof(object), (TypeConverter<byte, object>)((ref byte v) => (object) v));
            }

            static void RegisterUInt16Converters()
            {
                s_GlobalConverters.Register(typeof(ushort), typeof(sbyte), (TypeConverter<ushort, sbyte>)((ref ushort v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(char), (TypeConverter<ushort, char>)((ref ushort v) => (char) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(bool), (TypeConverter<ushort, bool>)((ref ushort v) => v != 0));
                s_GlobalConverters.Register(typeof(ushort), typeof(short), (TypeConverter<ushort, short>)((ref ushort v) => (short) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(int), (TypeConverter<ushort, int>)((ref ushort v) => (int) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(long), (TypeConverter<ushort, long>)((ref ushort v) => (long) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(byte), (TypeConverter<ushort, byte>)((ref ushort v) => (byte) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(uint), (TypeConverter<ushort, uint>)((ref ushort v) => (uint) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(ulong), (TypeConverter<ushort, ulong>)((ref ushort v) => (ulong) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(float), (TypeConverter<ushort, float>)((ref ushort v) => (float) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(double), (TypeConverter<ushort, double>)((ref ushort v) => (double) v));
                s_GlobalConverters.Register(typeof(ushort), typeof(object), (TypeConverter<ushort, object>)((ref ushort v) => (object) v));
            }

            static void RegisterUInt32Converters()
            {
                s_GlobalConverters.Register(typeof(uint), typeof(sbyte), (TypeConverter<uint, sbyte>)((ref uint v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(uint), typeof(char), (TypeConverter<uint, char>)((ref uint v) => (char) v));
                s_GlobalConverters.Register(typeof(uint), typeof(bool), (TypeConverter<uint, bool>)((ref uint v) => v != 0));
                s_GlobalConverters.Register(typeof(uint), typeof(short), (TypeConverter<uint, short>)((ref uint v) => (short) v));
                s_GlobalConverters.Register(typeof(uint), typeof(int), (TypeConverter<uint, int>)((ref uint v) => (int) v));
                s_GlobalConverters.Register(typeof(uint), typeof(long), (TypeConverter<uint, long>)((ref uint v) => (long) v));
                s_GlobalConverters.Register(typeof(uint), typeof(byte), (TypeConverter<uint, byte>)((ref uint v) => (byte) v));
                s_GlobalConverters.Register(typeof(uint), typeof(ushort), (TypeConverter<uint, ushort>)((ref uint v) => (ushort) v));
                s_GlobalConverters.Register(typeof(uint), typeof(ulong), (TypeConverter<uint, ulong>)((ref uint v) => (ulong) v));
                s_GlobalConverters.Register(typeof(uint), typeof(float), (TypeConverter<uint, float>)((ref uint v) => (float) v));
                s_GlobalConverters.Register(typeof(uint), typeof(double), (TypeConverter<uint, double>)((ref uint v) => (double) v));
                s_GlobalConverters.Register(typeof(uint), typeof(object), (TypeConverter<uint, object>)((ref uint v) => (object) v));
            }

            static void RegisterUInt64Converters()
            {
                s_GlobalConverters.Register(typeof(ulong), typeof(sbyte), (TypeConverter<ulong, sbyte>)((ref ulong v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(char), (TypeConverter<ulong, char>)((ref ulong v) => (char) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(bool), (TypeConverter<ulong, bool>)((ref ulong v) => v != 0));
                s_GlobalConverters.Register(typeof(ulong), typeof(short), (TypeConverter<ulong, short>)((ref ulong v) => (short) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(int), (TypeConverter<ulong, int>)((ref ulong v) => (int) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(long), (TypeConverter<ulong, long>)((ref ulong v) => (long) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(byte), (TypeConverter<ulong, byte>)((ref ulong v) => (byte) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(ushort), (TypeConverter<ulong, ushort>)((ref ulong v) => (ushort) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(uint), (TypeConverter<ulong, uint>)((ref ulong v) => (uint) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(float), (TypeConverter<ulong, float>)((ref ulong v) => (float) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(double), (TypeConverter<ulong, double>)((ref ulong v) => (double) v));
                s_GlobalConverters.Register(typeof(ulong), typeof(object), (TypeConverter<ulong, object>)((ref ulong v) => (object) v));
            }

            static void RegisterFloat32Converters()
            {
                s_GlobalConverters.Register(typeof(float), typeof(sbyte), (TypeConverter<float, sbyte>)((ref float v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(float), typeof(char), (TypeConverter<float, char>)((ref float v) => (char) v));
                s_GlobalConverters.Register(typeof(float), typeof(bool), (TypeConverter<float, bool>)((ref float v) => Math.Abs(v) > float.Epsilon));
                s_GlobalConverters.Register(typeof(float), typeof(short), (TypeConverter<float, short>)((ref float v) => (short) v));
                s_GlobalConverters.Register(typeof(float), typeof(int), (TypeConverter<float, int>)((ref float v) => (int) v));
                s_GlobalConverters.Register(typeof(float), typeof(long), (TypeConverter<float, long>)((ref float v) => (long) v));
                s_GlobalConverters.Register(typeof(float), typeof(byte), (TypeConverter<float, byte>)((ref float v) => (byte) v));
                s_GlobalConverters.Register(typeof(float), typeof(ushort), (TypeConverter<float, ushort>)((ref float v) => (ushort) v));
                s_GlobalConverters.Register(typeof(float), typeof(uint), (TypeConverter<float, uint>)((ref float v) => (uint) v));
                s_GlobalConverters.Register(typeof(float), typeof(ulong), (TypeConverter<float, ulong>)((ref float v) => (ulong) v));
                s_GlobalConverters.Register(typeof(float), typeof(double), (TypeConverter<float, double>)((ref float v) => (double) v));
                s_GlobalConverters.Register(typeof(float), typeof(object), (TypeConverter<float, object>)((ref float v) => (object) v));
            }

            static void RegisterFloat64Converters()
            {
                s_GlobalConverters.Register(typeof(double), typeof(sbyte), (TypeConverter<double, sbyte>)((ref double v) => (sbyte) v));
                s_GlobalConverters.Register(typeof(double), typeof(char), (TypeConverter<double, char>)((ref double v) => (char) v));
                s_GlobalConverters.Register(typeof(double), typeof(bool), (TypeConverter<double, bool>)((ref double v) => Math.Abs(v) > double.Epsilon));
                s_GlobalConverters.Register(typeof(double), typeof(short), (TypeConverter<double, short>)((ref double v) => (short) v));
                s_GlobalConverters.Register(typeof(double), typeof(int), (TypeConverter<double, int>)((ref double v) => (int) v));
                s_GlobalConverters.Register(typeof(double), typeof(long), (TypeConverter<double, long>)((ref double v) => (long) v));
                s_GlobalConverters.Register(typeof(double), typeof(byte), (TypeConverter<double, byte>)((ref double v) => (byte) v));
                s_GlobalConverters.Register(typeof(double), typeof(ushort), (TypeConverter<double, ushort>)((ref double v) => (ushort) v));
                s_GlobalConverters.Register(typeof(double), typeof(uint), (TypeConverter<double, uint>)((ref double v) => (uint) v));
                s_GlobalConverters.Register(typeof(double), typeof(ulong), (TypeConverter<double, ulong>)((ref double v) => (ulong) v));
                s_GlobalConverters.Register(typeof(double), typeof(float), (TypeConverter<double, float>)((ref double v) => (float) v));
                s_GlobalConverters.Register(typeof(double), typeof(object), (TypeConverter<double, object>)((ref double v) => (object) v));
            }

            static void RegisterBooleanConverters()
            {
                s_GlobalConverters.Register(typeof(bool), typeof(char), (TypeConverter<bool, char>)((ref bool v) => v ? (char) 1 : (char) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(sbyte), (TypeConverter<bool, sbyte>)((ref bool v) => v ? (sbyte) 1 : (sbyte) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(short), (TypeConverter<bool, short>)((ref bool v) => v ? (short) 1 : (short) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(int), (TypeConverter<bool, int>)((ref bool v) => v ? 1 : 0));
                s_GlobalConverters.Register(typeof(bool), typeof(long), (TypeConverter<bool, long>)((ref bool v) => v ? (long) 1 : (long) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(byte), (TypeConverter<bool, byte>)((ref bool v) => v ? (byte) 1 : (byte) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(ushort), (TypeConverter<bool, ushort>)((ref bool v) => v ? (ushort) 1 : (ushort) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(uint), (TypeConverter<bool, uint>)((ref bool v) => v ? (uint) 1 : (uint) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(ulong), (TypeConverter<bool, ulong>)((ref bool v) => v ? (ulong) 1 : (ulong) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(float), (TypeConverter<bool, float>)((ref bool v) => v ? (float) 1 : (float) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(double), (TypeConverter<bool, double>)((ref bool v) => v ? (double) 1 : (double) 0));
                s_GlobalConverters.Register(typeof(bool), typeof(object), (TypeConverter<bool, object>)((ref bool v) => (object)v));
            }

            static void RegisterCharConverters()
            {
                s_GlobalConverters.Register(typeof(string), typeof(char), (TypeConverter<string, char>) ((ref string v) =>
                {
                    if (v.Length != 1)
                    {
                        throw new Exception("Not a valid char");
                    }

                    return v[0];
                }));
                s_GlobalConverters.Register(typeof(char), typeof(bool), (TypeConverter<char, bool>)((ref char v) => v != (char) 0));
                s_GlobalConverters.Register(typeof(char), typeof(sbyte), (TypeConverter<char, sbyte>)((ref char v) => (sbyte)v));
                s_GlobalConverters.Register(typeof(char), typeof(short), (TypeConverter<char, short>)((ref char v) => (short)v));
                s_GlobalConverters.Register(typeof(char), typeof(int), (TypeConverter<char, int>)((ref char v) => (int)v));
                s_GlobalConverters.Register(typeof(char), typeof(long), (TypeConverter<char, long>)((ref char v) => (long)v));
                s_GlobalConverters.Register(typeof(char), typeof(byte), (TypeConverter<char, byte>)((ref char v) => (byte)v));
                s_GlobalConverters.Register(typeof(char), typeof(ushort), (TypeConverter<char, ushort>)((ref char v) => (ushort)v));
                s_GlobalConverters.Register(typeof(char), typeof(uint), (TypeConverter<char, uint>)((ref char v) => (uint)v));
                s_GlobalConverters.Register(typeof(char), typeof(ulong), (TypeConverter<char, ulong>)((ref char v) => (ulong)v));
                s_GlobalConverters.Register(typeof(char), typeof(float), (TypeConverter<char, float>)((ref char v) => (float)v));
                s_GlobalConverters.Register(typeof(char), typeof(double), (TypeConverter<char, double>)((ref char v) => (double)v));
                s_GlobalConverters.Register(typeof(char), typeof(object), (TypeConverter<char, object>)((ref char v) => (object)v));
                s_GlobalConverters.Register(typeof(char), typeof(string), (TypeConverter<char, string>)((ref char v) => v.ToString()));
            }

            static void RegisterStringConverters()
            {
                s_GlobalConverters.Register(typeof(string), typeof(char), (TypeConverter<string, char>) ((ref string v) => !string.IsNullOrEmpty(v) ? v[0] : '\0'));
                s_GlobalConverters.Register(typeof(char), typeof(string), (TypeConverter<char, string>)((ref char v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(bool), (TypeConverter<string, bool>)((ref string v) =>
                {
                    if (bool.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, bool>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(bool), typeof(string), (TypeConverter<bool, string>)((ref bool v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(sbyte), (TypeConverter<string, sbyte>) ((ref string v) =>
                {
                    if (sbyte.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, sbyte>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(sbyte), typeof(string), (TypeConverter<sbyte, string>)((ref sbyte v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(short), (TypeConverter<string, short>) ((ref string v) =>
                {
                    if (short.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, short>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(short), typeof(string), (TypeConverter<short, string>)((ref short v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(int), (TypeConverter<string, int>) ((ref string v) =>
                {
                    if (int.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, int>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(int), typeof(string), (TypeConverter<int, string>)((ref int v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(long), (TypeConverter<string, long>)((ref string v) =>
                {
                    if (long.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, long>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(long), typeof(string), (TypeConverter<long, string>)((ref long v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(byte), (TypeConverter<string, byte>) ((ref string v) =>
                {
                    if (byte.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, byte>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(byte), typeof(string), (TypeConverter<byte, string>) ((ref byte v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(ushort), (TypeConverter<string, ushort>) ((ref string v) =>
                {
                    if (ushort.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, ushort>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(ushort), typeof(string), (TypeConverter<ushort, string>)((ref ushort v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(uint), (TypeConverter<string, uint>) ((ref string v) =>
                {
                    if (uint.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, uint>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(uint), typeof(string), (TypeConverter<uint, string>)((ref uint v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(ulong), (TypeConverter<string, ulong>)((ref string v) =>
                {
                    if (ulong.TryParse(v, out var r))
                    {
                        return r;
                    }

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, ulong>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(ulong), typeof(string), (TypeConverter<ulong, string>)((ref ulong v) => v.ToString()));
                s_GlobalConverters.Register(typeof(string), typeof(float), (TypeConverter<string, float>)((ref string v) =>
                {
                    if (float.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, float>(ref fromDouble)
                        : default;
                }));
                s_GlobalConverters.Register(typeof(float), typeof(string), (TypeConverter<float, string>)((ref float v) => v.ToString(CultureInfo.InvariantCulture)));
                s_GlobalConverters.Register(typeof(string), typeof(double), (TypeConverter<string, double>)((ref string v) =>
                {
                    double.TryParse(v, out var r);
                    return r;
                }));
                s_GlobalConverters.Register(typeof(double), typeof(string), (TypeConverter<double, string>)((ref double v) => v.ToString(CultureInfo.InvariantCulture)));
            }

            static void RegisterObjectConverters()
            {
                s_GlobalConverters.Register(typeof(object), typeof(char), (TypeConverter<object, char>)((ref object v) => v is char value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(bool), (TypeConverter<object, bool>)((ref object v) => v is bool value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(sbyte), (TypeConverter<object, sbyte>)((ref object v) => v is sbyte value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(short), (TypeConverter<object, short>)((ref object v) => v is short value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(int), (TypeConverter<object, int>)((ref object v) => v is int value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(long), (TypeConverter<object, long>)((ref object v) => v is long value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(byte), (TypeConverter<object, byte>)((ref object v) => v is byte value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(ushort), (TypeConverter<object, ushort>)((ref object v) => v is ushort value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(uint), (TypeConverter<object, uint>)((ref object v) => v is uint value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(ulong), (TypeConverter<object, ulong>)((ref object v) => v is ulong value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(float), (TypeConverter<object, float>)((ref object v) => v is float value ? value : default));
                s_GlobalConverters.Register(typeof(object), typeof(double), (TypeConverter<object, double>)((ref object v) => v is double value ? value : default));
            }
        }
    }
}
