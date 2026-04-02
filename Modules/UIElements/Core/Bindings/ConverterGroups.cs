// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        internal struct Unsafe
        {
            public static void LazyRegisterGlobal(Type source, Type destination, Func<Delegate> converterDelegate)
            {
                s_GlobalConverters.registry.LazyRegister(source, destination, converterDelegate);
                TypeConversion.Unsafe.LazyRegister(source, destination, converterDelegate);
            }
        }

        private static readonly ConverterGroup s_GlobalConverters = new ConverterGroup("__global_converters");

        internal static ConverterGroup globalConverters => s_GlobalConverters;

        private static readonly Dictionary<string, ConverterGroup> s_BindingConverterGroups = new();

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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
                if (TypeConversion.PrimitivesConverters.TryConvertPrimitiveOrString(ref source, out destination))
                {
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

            // Special case where the source is null and of type object
            if (!typeof(TSource).IsValueType
                && source == null
                && typeof(TSource) == typeof(object))
            {
                destination = default;
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
    }
}
