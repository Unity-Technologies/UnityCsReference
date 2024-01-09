// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides a set of static methods to register and use converter groups and registers a set of global converters.
    /// <seealso cref="ConverterGroup"/>.
    /// <seealso cref="DataBinding"/>.
    /// </summary>
    public static class ConverterGroups
    {
        private static readonly ConverterGroup s_GlobalConverters = new ConverterGroup("__global_converters");
        private static readonly ConverterGroup s_PrimitivesConverters = new ConverterGroup("__primitives_converters");

        internal static ConverterGroup globalConverters => s_GlobalConverters;
        internal static ConverterGroup primitivesConverters => s_PrimitivesConverters;

        private static readonly Dictionary<string, ConverterGroup> s_BindingConverterGroups = new();

        static ConverterGroups()
        {
            RegisterPrimitivesConverter();
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
            RegisterColorConverters();
        }

        /// <summary>
        /// Registers a global UI conversion delegate that will be used when converting data between a data source and a
        /// UI control. This delegate will be used both when converting data from and to UI.
        /// </summary>
        /// <param name="converter">The delegate that handles the conversion.</param>
        /// <typeparam name="TSource">The type of the input data.</typeparam>
        /// <typeparam name="TDestination">The type of the output data.</typeparam>
        public static void RegisterGlobalConverter<TSource, TDestination>(TypeConverter<TSource, TDestination> converter)
        {
            s_GlobalConverters.registry.Register(typeof(TSource), typeof(TDestination), converter);
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
            s_GlobalConverters.registry.Register(typeof(TSource), typeof(TDestination), converter);
            TypeConversion.Register(converter);
        }

        /// <summary>
        /// Registers a conversion to a converter group. A new group will be created if none exist with that ID.
        /// </summary>
        /// <remarks>
        /// You can apply a converter group in a <see cref="DataBinding"/> UXML with the <c>source-to-ui-converters</c> or
        /// <c>ui-to-source-converters</c> attributes.
        /// </remarks>
        /// <param name="groupId">The converter group ID.</param>
        /// <param name="converter">The type converter to add.</param>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        internal static void AddConverterToGroup<TSource, TDestination>(string groupId, TypeConverter<TSource, TDestination> converter)
        {
            if (!s_BindingConverterGroups.TryGetValue(groupId, out var converterGroup))
            {
                converterGroup = new ConverterGroup(groupId);
                s_BindingConverterGroups.Add(groupId, converterGroup);
            }

            converterGroup.AddConverter(converter);
        }

        /// <summary>
        /// Registers a converter group by ID. Converter groups can be applied on a binding using local converters.
        /// </summary>
        /// <remarks>
        /// You can apply a converter group in a <see cref="DataBinding"/> UXML with the <c>source-to-ui-converters</c> or
        /// <c>ui-to-source-converters</c> attributes.
        /// </remarks>
        /// <param name="converterGroup">The converter group to register.</param>
        public static void RegisterConverterGroup(ConverterGroup converterGroup)
        {
            if (string.IsNullOrWhiteSpace(converterGroup.id))
            {
                Debug.LogWarning("[UI Toolkit] Cannot register a converter group with a 'null' or empty id.");
                return;
            }

            if (s_BindingConverterGroups.ContainsKey(converterGroup.id))
                Debug.LogWarning($"[UI Toolkit] Replacing converter group with id: {converterGroup.id}");

            s_BindingConverterGroups[converterGroup.id] = converterGroup;
        }

        /// <summary>
        /// Retrieves a converter group by ID.
        /// </summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="converterGroup">The converter group.</param>
        public static bool TryGetConverterGroup(string groupId, out ConverterGroup converterGroup)
        {
            return s_BindingConverterGroups.TryGetValue(groupId, out converterGroup);
        }

        /// <summary>
        /// Returns all existing converter groups.
        /// </summary>
        /// <param name="result">The resulting list of converter groups.</param>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void GetAllConverterGroups(List<ConverterGroup> result)
        {
            foreach (var group in s_BindingConverterGroups.Values)
            {
                result.Add(group);
            }
        }

        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> using the registered global converters.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">The converted destination value if the conversion succeeded, and the default value otherwise.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns><see langword="true"/> if the conversion succeeded, and <see langword="false"/> otherwise.</returns>
        public static bool TryConvert<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            // Registered converters always has the highest priority.
            if (s_GlobalConverters.registry.TryGetConverter(typeof(TSource), destinationType, out var converter))
            {
                destination = ((TypeConverter<TSource, TDestination>) converter)(ref source);
                return true;
            }

            // If we are dealing with the same value type, do an unsafe cast, but not try to reinterpret it. This will
            // avoid boxing allocations.
            if (sourceType.IsValueType && destinationType.IsValueType)
            {
                if (sourceType == destinationType)
                {
                    destination = UnsafeUtility.As<TSource, TDestination>(ref source);
                    return true;
                }

                // Conversions between primitive types.
                if (s_PrimitivesConverters.registry.TryGetConverter(sourceType, destinationType, out converter))
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

            if (destinationType.IsAssignableFrom(sourceType) && null == source)
            {
                destination = default;
                return true;
            }

            // T -> string conversions should be supported by default.
            if (destinationType == typeof(string))
            {
                destination = (TDestination) (object) source?.ToString();
                return true;
            }

            // T -> object conversions should be supported by default.
            if (destinationType == typeof(object))
            {
                // ReSharper disable once PossibleInvalidCastException
                destination = (TDestination) (object) source;
                return true;
            }

            destination = default;
            return false;
        }

        /// <summary>
        /// Sets the value of a property at the given path to the given value, using the global converters.
        /// </summary>
        /// <remarks>
        /// This method isn't thread-safe.
        /// </remarks>
        /// <param name="container">The container whose property needs to be set.</param>
        /// <param name="path">The path of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <param name="returnCode">The return code of the conversion.</param>
        /// <typeparam name="TContainer">The container type to set the value on.</typeparam>
        /// <typeparam name="TValue">The value type to set.</typeparam>
        /// <returns><see langword="true"/> if the value was set correctly, and <see langword="false"/> otherwise.</returns>
        public static bool TrySetValueGlobal<TContainer, TValue>(ref TContainer container,
            in PropertyPath path, TValue value, out VisitReturnCode returnCode)
        {
            if (path.IsEmpty)
            {
                returnCode = VisitReturnCode.InvalidPath;
                return false;
            }

            var visitor = SetValueVisitor<TValue>.Pool.Get();
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

        private static void RegisterInt8Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(bool), (TypeConverter<sbyte, bool>) ((ref sbyte v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(char), (TypeConverter<sbyte, char>) ((ref sbyte v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(short), (TypeConverter<sbyte, short>) ((ref sbyte v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(int), (TypeConverter<sbyte, int>) ((ref sbyte v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(long), (TypeConverter<sbyte, long>) ((ref sbyte v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(byte), (TypeConverter<sbyte, byte>) ((ref sbyte v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(ushort), (TypeConverter<sbyte, ushort>) ((ref sbyte v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(uint), (TypeConverter<sbyte, uint>) ((ref sbyte v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(ulong), (TypeConverter<sbyte, ulong>) ((ref sbyte v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(float), (TypeConverter<sbyte, float>) ((ref sbyte v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(sbyte), typeof(double), (TypeConverter<sbyte, double>) ((ref sbyte v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(sbyte), (TypeConverter<string, sbyte>) ((ref string v) =>
            {
                if (sbyte.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out sbyte result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterInt16Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(bool), (TypeConverter<short, bool>) ((ref short v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(sbyte), (TypeConverter<short, sbyte>) ((ref short v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(char), (TypeConverter<short, char>) ((ref short v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(int), (TypeConverter<short, int>) ((ref short v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(long), (TypeConverter<short, long>) ((ref short v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(byte), (TypeConverter<short, byte>) ((ref short v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(ushort), (TypeConverter<short, ushort>) ((ref short v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(uint), (TypeConverter<short, uint>) ((ref short v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(ulong), (TypeConverter<short, ulong>) ((ref short v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(float), (TypeConverter<short, float>) ((ref short v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(short), typeof(double), (TypeConverter<short, double>) ((ref short v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(short), (TypeConverter<string, short>) ((ref string v) =>
            {
                if (short.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out short result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterInt32Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(bool), (TypeConverter<int, bool>) ((ref int v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(sbyte), (TypeConverter<int, sbyte>) ((ref int v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(char), (TypeConverter<int, char>) ((ref int v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(short), (TypeConverter<int, short>) ((ref int v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(long), (TypeConverter<int, long>) ((ref int v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(byte), (TypeConverter<int, byte>) ((ref int v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(ushort), (TypeConverter<int, ushort>) ((ref int v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(uint), (TypeConverter<int, uint>) ((ref int v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(ulong), (TypeConverter<int, ulong>) ((ref int v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(float), (TypeConverter<int, float>) ((ref int v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(int), typeof(double), (TypeConverter<int, double>) ((ref int v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(int), (TypeConverter<string, int>) ((ref string v) =>
            {
                if (int.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out int result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterInt64Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(bool), (TypeConverter<long, bool>) ((ref long v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(sbyte), (TypeConverter<long, sbyte>) ((ref long v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(char), (TypeConverter<long, char>) ((ref long v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(short), (TypeConverter<long, short>) ((ref long v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(int), (TypeConverter<long, int>) ((ref long v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(byte), (TypeConverter<long, byte>) ((ref long v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(ushort), (TypeConverter<long, ushort>) ((ref long v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(uint), (TypeConverter<long, uint>) ((ref long v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(ulong), (TypeConverter<long, ulong>) ((ref long v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(float), (TypeConverter<long, float>) ((ref long v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(long), typeof(double), (TypeConverter<long, double>) ((ref long v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(long), (TypeConverter<string, long>) ((ref string v) =>
            {
                if (long.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out long result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterUInt8Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(bool), (TypeConverter<byte, bool>) ((ref byte v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(sbyte), (TypeConverter<byte, sbyte>) ((ref byte v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(char), (TypeConverter<byte, char>) ((ref byte v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(short), (TypeConverter<byte, short>) ((ref byte v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(int), (TypeConverter<byte, int>) ((ref byte v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(long), (TypeConverter<byte, long>) ((ref byte v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(ushort), (TypeConverter<byte, ushort>) ((ref byte v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(uint), (TypeConverter<byte, uint>) ((ref byte v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(ulong), (TypeConverter<byte, ulong>) ((ref byte v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(float), (TypeConverter<byte, float>) ((ref byte v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(double), (TypeConverter<byte, double>) ((ref byte v) => (double) v));
            s_PrimitivesConverters.registry.Register(typeof(byte), typeof(object), (TypeConverter<byte, object>) ((ref byte v) => (object) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(byte), (TypeConverter<string, byte>) ((ref string v) =>
            {
                if (byte.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out byte result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterUInt16Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(bool), (TypeConverter<ushort, bool>) ((ref ushort v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(sbyte), (TypeConverter<ushort, sbyte>) ((ref ushort v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(char), (TypeConverter<ushort, char>) ((ref ushort v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(short), (TypeConverter<ushort, short>) ((ref ushort v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(int), (TypeConverter<ushort, int>) ((ref ushort v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(long), (TypeConverter<ushort, long>) ((ref ushort v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(byte), (TypeConverter<ushort, byte>) ((ref ushort v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(uint), (TypeConverter<ushort, uint>) ((ref ushort v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(ulong), (TypeConverter<ushort, ulong>) ((ref ushort v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(float), (TypeConverter<ushort, float>) ((ref ushort v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(ushort), typeof(double), (TypeConverter<ushort, double>) ((ref ushort v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(ushort), (TypeConverter<string, ushort>) ((ref string v) =>
            {
                if (ushort.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out ushort result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterUInt32Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(bool), (TypeConverter<uint, bool>) ((ref uint v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(sbyte), (TypeConverter<uint, sbyte>) ((ref uint v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(char), (TypeConverter<uint, char>) ((ref uint v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(short), (TypeConverter<uint, short>) ((ref uint v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(int), (TypeConverter<uint, int>) ((ref uint v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(long), (TypeConverter<uint, long>) ((ref uint v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(byte), (TypeConverter<uint, byte>) ((ref uint v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(ushort), (TypeConverter<uint, ushort>) ((ref uint v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(ulong), (TypeConverter<uint, ulong>) ((ref uint v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(float), (TypeConverter<uint, float>) ((ref uint v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(uint), typeof(double), (TypeConverter<uint, double>) ((ref uint v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(uint), (TypeConverter<string, uint>) ((ref string v) =>
            {
                if (uint.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out uint result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterUInt64Converters()
        {
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(bool), (TypeConverter<ulong, bool>) ((ref ulong v) => v > 0U));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(sbyte), (TypeConverter<ulong, sbyte>) ((ref ulong v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(char), (TypeConverter<ulong, char>) ((ref ulong v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(short), (TypeConverter<ulong, short>) ((ref ulong v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(int), (TypeConverter<ulong, int>) ((ref ulong v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(long), (TypeConverter<ulong, long>) ((ref ulong v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(byte), (TypeConverter<ulong, byte>) ((ref ulong v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(ushort), (TypeConverter<ulong, ushort>) ((ref ulong v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(uint), (TypeConverter<ulong, uint>) ((ref ulong v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(float), (TypeConverter<ulong, float>) ((ref ulong v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(ulong), typeof(double), (TypeConverter<ulong, double>) ((ref ulong v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(ulong), (TypeConverter<string, ulong>) ((ref string v) =>
            {
                if (ulong.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out ulong result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterFloatConverters()
        {
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(bool), (TypeConverter<float, bool>) ((ref float v) => (double) v != 0.0));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(sbyte), (TypeConverter<float, sbyte>) ((ref float v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(char), (TypeConverter<float, char>) ((ref float v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(short), (TypeConverter<float, short>) ((ref float v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(int), (TypeConverter<float, int>) ((ref float v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(long), (TypeConverter<float, long>) ((ref float v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(byte), (TypeConverter<float, byte>) ((ref float v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(ushort), (TypeConverter<float, ushort>) ((ref float v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(uint), (TypeConverter<float, uint>) ((ref float v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(ulong), (TypeConverter<float, ulong>) ((ref float v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(float), typeof(double), (TypeConverter<float, double>) ((ref float v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(float), typeof(string), (TypeConverter<float, string>) ((ref float v) => v.ToString(CultureInfo.InvariantCulture)));
            s_PrimitivesConverters.registry.Register(typeof(string), typeof(float), (TypeConverter<string, float>) ((ref string v) =>
            {
                if (float.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out float result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterDoubleConverters()
        {
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(bool), (TypeConverter<double, bool>) ((ref double v) => v != 0.0));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(sbyte), (TypeConverter<double, sbyte>) ((ref double v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(char), (TypeConverter<double, char>) ((ref double v) => (char) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(short), (TypeConverter<double, short>) ((ref double v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(int), (TypeConverter<double, int>) ((ref double v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(long), (TypeConverter<double, long>) ((ref double v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(byte), (TypeConverter<double, byte>) ((ref double v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(ushort), (TypeConverter<double, ushort>) ((ref double v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(uint), (TypeConverter<double, uint>) ((ref double v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(ulong), (TypeConverter<double, ulong>) ((ref double v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(double), typeof(float), (TypeConverter<double, float>) ((ref double v) => (float) v));

            s_PrimitivesConverters.registry.Register(typeof(double), typeof(string), (TypeConverter<double, string>) ((ref double v) => v.ToString(CultureInfo.InvariantCulture)));
            s_PrimitivesConverters.registry.Register(typeof(string), typeof(double), (TypeConverter<string, double>) ((ref string v) =>
            {
                double.TryParse(v, out var r);
                return r;
            }));
        }

        private static void RegisterBooleanConverters()
        {
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(char), (TypeConverter<bool, char>) ((ref bool v) => v ? (char) 1 : (char) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(sbyte), (TypeConverter<bool, sbyte>) ((ref bool v) => v ? (sbyte) 1 : (sbyte) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(short), (TypeConverter<bool, short>) ((ref bool v) => v ? (short) 1 : (short) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(int), (TypeConverter<bool, int>) ((ref bool v) => v ? 1 : 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(long), (TypeConverter<bool, long>) ((ref bool v) => v ? (long) 1 : (long) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(byte), (TypeConverter<bool, byte>) ((ref bool v) => v ? (byte) 1 : (byte) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(ushort), (TypeConverter<bool, ushort>) ((ref bool v) => v ? (ushort) 1 : (ushort) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(uint), (TypeConverter<bool, uint>) ((ref bool v) => v ? (uint) 1 : (uint) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(ulong), (TypeConverter<bool, ulong>) ((ref bool v) => v ? (ulong) 1 : (ulong) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(float), (TypeConverter<bool, float>) ((ref bool v) => v ? (float) 1 : (float) 0));
            s_PrimitivesConverters.registry.Register(typeof(bool), typeof(double), (TypeConverter<bool, double>) ((ref bool v) => v ? (double) 1 : (double) 0));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(bool), (TypeConverter<string, bool>) ((ref string v) =>
            {
                if (bool.TryParse(v, out var r))
                    return r;

                return double.TryParse(v, out var fromDouble) && TryConvert(ref fromDouble, out bool result)
                    ? result
                    : default;
            }));
        }

        private static void RegisterCharConverters()
        {
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(bool), (TypeConverter<char, bool>) ((ref char v) => v != (char) 0));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(sbyte), (TypeConverter<char, sbyte>) ((ref char v) => (sbyte) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(short), (TypeConverter<char, short>) ((ref char v) => (short) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(int), (TypeConverter<char, int>) ((ref char v) => (int) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(long), (TypeConverter<char, long>) ((ref char v) => (long) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(byte), (TypeConverter<char, byte>) ((ref char v) => (byte) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(ushort), (TypeConverter<char, ushort>) ((ref char v) => (ushort) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(uint), (TypeConverter<char, uint>) ((ref char v) => (uint) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(ulong), (TypeConverter<char, ulong>) ((ref char v) => (ulong) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(float), (TypeConverter<char, float>) ((ref char v) => (float) v));
            s_PrimitivesConverters.registry.Register(typeof(char), typeof(double), (TypeConverter<char, double>) ((ref char v) => (double) v));

            s_PrimitivesConverters.registry.Register(typeof(string), typeof(char), (TypeConverter<string, char>) ((ref string v) => !string.IsNullOrEmpty(v) ? v[0] : '\0'));
        }

        private static void RegisterColorConverters()
        {
            s_PrimitivesConverters.registry.Register(typeof(Color), typeof(Color32), (TypeConverter<Color, Color32>) ((ref Color v) => v));
            s_PrimitivesConverters.registry.Register(typeof(Color32), typeof(Color), (TypeConverter<Color32, Color>) ((ref Color32 v) => v));
        }
    }
}
