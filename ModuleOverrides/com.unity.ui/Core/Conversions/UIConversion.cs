// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Allows to register global UI conversion delegates between data sources and UI controls.
    /// </summary>
    internal static class UIConversion
    {
        static readonly ConversionRegistry s_GlobalUIConverters = ConversionRegistry.Create();
        static readonly ConversionRegistry s_PrimitiveConverters = ConversionRegistry.Create();

        static UIConversion()
        {
            RegisterPrimitivesConverter();
        }

        /// <summary>
        /// Registers a global UI conversion delegate that will be used when converting data between a data source and a
        /// UI control. This delegate will be used both when converting data from and to UI.
        /// </summary>
        /// <param name="converter">The delegate that handles the conversion.</param>
        /// <typeparam name="TSource">The type of the input data.</typeparam>
        /// <typeparam name="TDestination">The type of the output data.</typeparam>
        public static void Register<TSource, TDestination>(TypeConverter<TSource, TDestination> converter)
        {
            s_GlobalUIConverters.Register(typeof(TSource), typeof(TDestination), converter);
        }

        /// <summary>
        /// Registers a global UI conversion delegate that will be used when converting data between a data source and a
        /// UI control. This will also register the delegate as a global conversion in <see cref="Unity.Properties.TypeConversion"/>
        /// </summary>
        /// <param name="converter">The delegate that handles the conversion.</param>
        /// <typeparam name="TSource">The type of the input data.</typeparam>
        /// <typeparam name="TDestination">The type of the output data.</typeparam>
        internal static void RegisterGlobal<TSource, TDestination>(TypeConverter<TSource, TDestination> converter)
        {
            s_GlobalUIConverters.Register(typeof(TSource), typeof(TDestination), converter);
            TypeConversion.Register(converter);
        }

        /// <summary>
        /// Tries to convert the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">When this method returns, contains the converted destination value if the conversion succeeded; otherwise, default.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        ///<returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        internal static bool TryConvert<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            // Registered converters always has the highest priority.
            if (s_GlobalUIConverters.TryGetConverter(typeof(TSource), typeof(TDestination), out var converter))
            {
                destination = ((TypeConverter<TSource, TDestination>) converter)(ref source);
                return true;
            }

            // If we are dealing with the same value type, do an unsafe cast, but not try to reinterpret it. This will
            // avoid boxing allocations.
            if (typeof(TSource).IsValueType && typeof(TDestination).IsValueType)
            {
                if (typeof(TSource) == typeof(TDestination))
                {
                    destination = UnsafeUtility.As<TSource, TDestination>(ref source);
                    return true;
                }

                // Conversions between primitive types.
                if (s_PrimitiveConverters.TryGetConverter(typeof(TSource), typeof(TDestination), out converter))
                {
                    destination = ((TypeConverter<TSource, TDestination>) converter)(ref source);
                    return true;
                }

                destination = default;
                return false;
            }

            if (source is TDestination d)
            {
                destination = d;
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

            destination = default;
            return false;
        }

        ///  <summary>
        ///  Tries to convert the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>
        ///  using only the provided conversion delegates.
        ///  </summary>
        ///  <param name="registry">The registry containing the conversion delegates.</param>
        ///  <param name="source">The source value to convert.</param>
        ///  <param name="destination">When this method returns, contains the converted destination value if the conversion succeeded; otherwise, default.</param>
        ///  <typeparam name="TSource">The source type to convert from.</typeparam>
        ///  <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        internal static bool TryConvert<TSource, TDestination>(ConversionRegistry registry, ref TSource source,
            out TDestination destination)
        {
            if (registry.TryGetConverter(typeof(TSource), typeof(TDestination), out var converter) &&
                converter is TypeConverter<TSource, TDestination> typedConverter)
            {
                destination = typedConverter(ref source);
                return true;
            }

            destination = default;
            return false;
        }

        internal static bool TrySetValue<TContainer, TValue>(ConversionRegistry registry, ref TContainer container,
            PropertyPath path, TValue value, out VisitReturnCode returnCode)
        {
            if (path.IsEmpty)
            {
                returnCode = VisitReturnCode.InvalidPath;
                return false;
            }

            var visitor = SetValueVisitor<TValue>.Pool.Get();
            visitor.ConversionRegistry = registry;
            visitor.Path = path;
            visitor.Value = value;
            try
            {
                if (!PropertyContainer.TryAccept(visitor, ref container, out returnCode))
                    return false;

                returnCode = visitor.ReturnCode;
            }
            finally
            {
                SetValueVisitor<TValue>.Pool.Release(visitor);
            }

            return returnCode == VisitReturnCode.Ok;
        }

        static void RegisterPrimitivesConverter()
        {
            RegisterInt8Converters();
            RegisterInt16Converters();
            RegisterInt32Converters();
            RegisterInt64Converters();
            RegisterUInt8Converters();
            RegisterUInt16Converters();
            RegisterUInt32Converters();
            RegisterUInt64Converters();
            RegisterFloatConverters();
            RegisterDoubleConverters();
            RegisterBooleanConverters();
            RegisterCharConverters();
        }

        static void RegisterInt8Converters()
        {
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(bool), (TypeConverter<sbyte, bool>) ((ref sbyte v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(char), (TypeConverter<sbyte, char>) ((ref sbyte v) => (char) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(short), (TypeConverter<sbyte, short>) ((ref sbyte v) => (short) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(int), (TypeConverter<sbyte, int>) ((ref sbyte v) => (int) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(long), (TypeConverter<sbyte, long>) ((ref sbyte v) => (long) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(byte), (TypeConverter<sbyte, byte>) ((ref sbyte v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(ushort), (TypeConverter<sbyte, ushort>) ((ref sbyte v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(uint), (TypeConverter<sbyte, uint>) ((ref sbyte v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(ulong), (TypeConverter<sbyte, ulong>) ((ref sbyte v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(float), (TypeConverter<sbyte, float>) ((ref sbyte v) => (float) v));
            s_PrimitiveConverters.Register(typeof(sbyte), typeof(double), (TypeConverter<sbyte, double>) ((ref sbyte v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(sbyte), (TypeConverter<string, sbyte>) ((ref string v) =>
            {
                if (sbyte.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out sbyte result)
                    ? result
                    : default;
            }));
        }

        static void RegisterInt16Converters()
        {
            s_PrimitiveConverters.Register(typeof(short), typeof(bool), (TypeConverter<short, bool>) ((ref short v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(short), typeof(sbyte), (TypeConverter<short, sbyte>) ((ref short v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(char), (TypeConverter<short, char>) ((ref short v) => (char) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(int), (TypeConverter<short, int>) ((ref short v) => (int) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(long), (TypeConverter<short, long>) ((ref short v) => (long) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(byte), (TypeConverter<short, byte>) ((ref short v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(ushort), (TypeConverter<short, ushort>) ((ref short v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(uint), (TypeConverter<short, uint>) ((ref short v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(ulong), (TypeConverter<short, ulong>) ((ref short v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(float), (TypeConverter<short, float>) ((ref short v) => (float) v));
            s_PrimitiveConverters.Register(typeof(short), typeof(double), (TypeConverter<short, double>) ((ref short v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(short), (TypeConverter<string, short>) ((ref string v) =>
            {
                if (short.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out short result)
                    ? result
                    : default;
            }));
        }

        static void RegisterInt32Converters()
        {
            s_PrimitiveConverters.Register(typeof(int), typeof(bool), (TypeConverter<int, bool>) ((ref int v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(int), typeof(sbyte), (TypeConverter<int, sbyte>) ((ref int v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(char), (TypeConverter<int, char>) ((ref int v) => (char) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(short), (TypeConverter<int, short>) ((ref int v) => (short) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(long), (TypeConverter<int, long>) ((ref int v) => (long) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(byte), (TypeConverter<int, byte>) ((ref int v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(ushort), (TypeConverter<int, ushort>) ((ref int v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(uint), (TypeConverter<int, uint>) ((ref int v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(ulong), (TypeConverter<int, ulong>) ((ref int v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(float), (TypeConverter<int, float>) ((ref int v) => (float) v));
            s_PrimitiveConverters.Register(typeof(int), typeof(double), (TypeConverter<int, double>) ((ref int v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(int), (TypeConverter<string, int>) ((ref string v) =>
            {
                if (int.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out int result)
                    ? result
                    : default;
            }));
        }

        static void RegisterInt64Converters()
        {
            s_PrimitiveConverters.Register(typeof(long), typeof(bool), (TypeConverter<long, bool>) ((ref long v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(long), typeof(sbyte), (TypeConverter<long, sbyte>) ((ref long v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(char), (TypeConverter<long, char>) ((ref long v) => (char) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(short), (TypeConverter<long, short>) ((ref long v) => (short) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(int), (TypeConverter<long, int>) ((ref long v) => (int) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(byte), (TypeConverter<long, byte>) ((ref long v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(ushort), (TypeConverter<long, ushort>) ((ref long v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(uint), (TypeConverter<long, uint>) ((ref long v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(ulong), (TypeConverter<long, ulong>) ((ref long v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(float), (TypeConverter<long, float>) ((ref long v) => (float) v));
            s_PrimitiveConverters.Register(typeof(long), typeof(double), (TypeConverter<long, double>) ((ref long v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(long), (TypeConverter<string, long>) ((ref string v) =>
            {
                if (long.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out long result)
                    ? result
                    : default;
            }));
        }

        static void RegisterUInt8Converters()
        {
            s_PrimitiveConverters.Register(typeof(byte), typeof(bool), (TypeConverter<byte, bool>) ((ref byte v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(byte), typeof(sbyte), (TypeConverter<byte, sbyte>) ((ref byte v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(char), (TypeConverter<byte, char>) ((ref byte v) => (char) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(short), (TypeConverter<byte, short>) ((ref byte v) => (short) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(int), (TypeConverter<byte, int>) ((ref byte v) => (int) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(long), (TypeConverter<byte, long>) ((ref byte v) => (long) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(ushort), (TypeConverter<byte, ushort>) ((ref byte v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(uint), (TypeConverter<byte, uint>) ((ref byte v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(ulong), (TypeConverter<byte, ulong>) ((ref byte v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(float), (TypeConverter<byte, float>) ((ref byte v) => (float) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(double), (TypeConverter<byte, double>) ((ref byte v) => (double) v));
            s_PrimitiveConverters.Register(typeof(byte), typeof(object), (TypeConverter<byte, object>) ((ref byte v) => (object) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(byte), (TypeConverter<string, byte>) ((ref string v) =>
            {
                if (byte.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out byte result)
                    ? result
                    : default;
            }));
        }

        static void RegisterUInt16Converters()
        {
            s_PrimitiveConverters.Register(typeof(ushort), typeof(bool), (TypeConverter<ushort, bool>) ((ref ushort v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(sbyte), (TypeConverter<ushort, sbyte>) ((ref ushort v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(char), (TypeConverter<ushort, char>) ((ref ushort v) => (char) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(short), (TypeConverter<ushort, short>) ((ref ushort v) => (short) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(int), (TypeConverter<ushort, int>) ((ref ushort v) => (int) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(long), (TypeConverter<ushort, long>) ((ref ushort v) => (long) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(byte), (TypeConverter<ushort, byte>) ((ref ushort v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(uint), (TypeConverter<ushort, uint>) ((ref ushort v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(ulong), (TypeConverter<ushort, ulong>) ((ref ushort v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(float), (TypeConverter<ushort, float>) ((ref ushort v) => (float) v));
            s_PrimitiveConverters.Register(typeof(ushort), typeof(double), (TypeConverter<ushort, double>) ((ref ushort v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(ushort), (TypeConverter<string, ushort>) ((ref string v) =>
            {
                if (ushort.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out ushort result)
                    ? result
                    : default;
            }));
        }

        static void RegisterUInt32Converters()
        {
            s_PrimitiveConverters.Register(typeof(uint), typeof(bool), (TypeConverter<uint, bool>) ((ref uint v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(uint), typeof(sbyte), (TypeConverter<uint, sbyte>) ((ref uint v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(char), (TypeConverter<uint, char>) ((ref uint v) => (char) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(short), (TypeConverter<uint, short>) ((ref uint v) => (short) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(int), (TypeConverter<uint, int>) ((ref uint v) => (int) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(long), (TypeConverter<uint, long>) ((ref uint v) => (long) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(byte), (TypeConverter<uint, byte>) ((ref uint v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(ushort), (TypeConverter<uint, ushort>) ((ref uint v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(ulong), (TypeConverter<uint, ulong>) ((ref uint v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(float), (TypeConverter<uint, float>) ((ref uint v) => (float) v));
            s_PrimitiveConverters.Register(typeof(uint), typeof(double), (TypeConverter<uint, double>) ((ref uint v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(uint), (TypeConverter<string, uint>) ((ref string v) =>
            {
                if (uint.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out uint result)
                    ? result
                    : default;
            }));
        }

        static void RegisterUInt64Converters()
        {
            s_PrimitiveConverters.Register(typeof(ulong), typeof(bool), (TypeConverter<ulong, bool>) ((ref ulong v) => v > 0U));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(sbyte), (TypeConverter<ulong, sbyte>) ((ref ulong v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(char), (TypeConverter<ulong, char>) ((ref ulong v) => (char) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(short), (TypeConverter<ulong, short>) ((ref ulong v) => (short) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(int), (TypeConverter<ulong, int>) ((ref ulong v) => (int) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(long), (TypeConverter<ulong, long>) ((ref ulong v) => (long) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(byte), (TypeConverter<ulong, byte>) ((ref ulong v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(ushort), (TypeConverter<ulong, ushort>) ((ref ulong v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(uint), (TypeConverter<ulong, uint>) ((ref ulong v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(float), (TypeConverter<ulong, float>) ((ref ulong v) => (float) v));
            s_PrimitiveConverters.Register(typeof(ulong), typeof(double), (TypeConverter<ulong, double>) ((ref ulong v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(ulong), (TypeConverter<string, ulong>) ((ref string v) =>
            {
                if (ulong.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out ulong result)
                    ? result
                    : default;
            }));
        }

        static void RegisterFloatConverters()
        {
            s_PrimitiveConverters.Register(typeof(float), typeof(bool), (TypeConverter<float, bool>) ((ref float v) => (double) v != 0.0));
            s_PrimitiveConverters.Register(typeof(float), typeof(sbyte), (TypeConverter<float, sbyte>) ((ref float v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(char), (TypeConverter<float, char>) ((ref float v) => (char) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(short), (TypeConverter<float, short>) ((ref float v) => (short) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(int), (TypeConverter<float, int>) ((ref float v) => (int) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(long), (TypeConverter<float, long>) ((ref float v) => (long) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(byte), (TypeConverter<float, byte>) ((ref float v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(ushort), (TypeConverter<float, ushort>) ((ref float v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(uint), (TypeConverter<float, uint>) ((ref float v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(ulong), (TypeConverter<float, ulong>) ((ref float v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(float), typeof(double), (TypeConverter<float, double>) ((ref float v) => (double) v));

            s_PrimitiveConverters.Register(typeof(float), typeof(string), (TypeConverter<float, string>) ((ref float v) => v.ToString(CultureInfo.InvariantCulture)));
            s_PrimitiveConverters.Register(typeof(string), typeof(float), (TypeConverter<string, float>) ((ref string v) =>
            {
                if (float.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out float result)
                    ? result
                    : default;
            }));
        }

        static void RegisterDoubleConverters()
        {
            s_PrimitiveConverters.Register(typeof(double), typeof(bool), (TypeConverter<double, bool>) ((ref double v) => v != 0.0));
            s_PrimitiveConverters.Register(typeof(double), typeof(sbyte), (TypeConverter<double, sbyte>) ((ref double v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(char), (TypeConverter<double, char>) ((ref double v) => (char) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(short), (TypeConverter<double, short>) ((ref double v) => (short) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(int), (TypeConverter<double, int>) ((ref double v) => (int) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(long), (TypeConverter<double, long>) ((ref double v) => (long) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(byte), (TypeConverter<double, byte>) ((ref double v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(ushort), (TypeConverter<double, ushort>) ((ref double v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(uint), (TypeConverter<double, uint>) ((ref double v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(ulong), (TypeConverter<double, ulong>) ((ref double v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(double), typeof(float), (TypeConverter<double, float>) ((ref double v) => (float) v));

            s_PrimitiveConverters.Register(typeof(double), typeof(string), (TypeConverter<double, string>) ((ref double v) => v.ToString(CultureInfo.InvariantCulture)));
            s_PrimitiveConverters.Register(typeof(string), typeof(double), (TypeConverter<string, double>) ((ref string v) =>
            {
                double.TryParse(v, out var r);
                return r;
            }));
        }

        static void RegisterBooleanConverters()
        {
            s_PrimitiveConverters.Register(typeof(bool), typeof(char), (TypeConverter<bool, char>) ((ref bool v) => v ? (char) 1 : (char) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(sbyte), (TypeConverter<bool, sbyte>) ((ref bool v) => v ? (sbyte) 1 : (sbyte) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(short), (TypeConverter<bool, short>) ((ref bool v) => v ? (short) 1 : (short) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(int), (TypeConverter<bool, int>) ((ref bool v) => v ? 1 : 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(long), (TypeConverter<bool, long>) ((ref bool v) => v ? (long) 1 : (long) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(byte), (TypeConverter<bool, byte>) ((ref bool v) => v ? (byte) 1 : (byte) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(ushort), (TypeConverter<bool, ushort>) ((ref bool v) => v ? (ushort) 1 : (ushort) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(uint), (TypeConverter<bool, uint>) ((ref bool v) => v ? (uint) 1 : (uint) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(ulong), (TypeConverter<bool, ulong>) ((ref bool v) => v ? (ulong) 1 : (ulong) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(float), (TypeConverter<bool, float>) ((ref bool v) => v ? (float) 1 : (float) 0));
            s_PrimitiveConverters.Register(typeof(bool), typeof(double), (TypeConverter<bool, double>) ((ref bool v) => v ? (double) 1 : (double) 0));

            s_PrimitiveConverters.Register(typeof(string), typeof(bool), (TypeConverter<string, bool>) ((ref string v) =>
            {
                if (bool.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out bool result)
                    ? result
                    : default;
            }));
        }

        static void RegisterCharConverters()
        {
            s_PrimitiveConverters.Register(typeof(char), typeof(bool), (TypeConverter<char, bool>) ((ref char v) => v != (char) 0));
            s_PrimitiveConverters.Register(typeof(char), typeof(sbyte), (TypeConverter<char, sbyte>) ((ref char v) => (sbyte) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(short), (TypeConverter<char, short>) ((ref char v) => (short) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(int), (TypeConverter<char, int>) ((ref char v) => (int) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(long), (TypeConverter<char, long>) ((ref char v) => (long) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(byte), (TypeConverter<char, byte>) ((ref char v) => (byte) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(ushort), (TypeConverter<char, ushort>) ((ref char v) => (ushort) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(uint), (TypeConverter<char, uint>) ((ref char v) => (uint) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(ulong), (TypeConverter<char, ulong>) ((ref char v) => (ulong) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(float), (TypeConverter<char, float>) ((ref char v) => (float) v));
            s_PrimitiveConverters.Register(typeof(char), typeof(double), (TypeConverter<char, double>) ((ref char v) => (double) v));

            s_PrimitiveConverters.Register(typeof(string), typeof(char), (TypeConverter<string, char>) ((ref string v) => !string.IsNullOrEmpty(v) ? v[0] : '\0'));
        }
    }
}
