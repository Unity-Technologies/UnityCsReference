// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A type to hold information about a conversion registry used locally on bindings.
    /// </summary>
    /// <remarks>
    /// You can apply converter groups on a <see cref="DataBinding"/> in UXML with the <c>source-to-ui-converters</c> or
    /// <c>ui-to-source-converters</c> attributes or in C# script with the <see cref="DataBinding.ApplyConverterGroupToSource"/> or
    /// <see cref="DataBinding.ApplyConverterGroupToUI"/> methods.
    /// </remarks>
    public class ConverterGroup
    {
        /// <summary>
        /// The group id.
        /// </summary>
        /// <remarks>
        /// Converter groups can be queried through <see cref="ConverterGroups.TryGetConverterGroup"/>.
        /// </remarks>
        public string id { get; }

        /// <summary>
        /// Optional and alternative name for a converter group ID, to be displayed to users to assist while authoring.
        /// </summary>
        public string displayName { get; }

        /// <summary>
        /// Optional description for a converter group ID that may include information about what this group contains
        /// or is used for, to be displayed to users to assist while authoring.
        /// </summary>
        public string description { get; }

        internal TypeConverterRegistry registry { get; }

        /// <summary>
        /// Creates a ConverterGroup.
        /// </summary>
        /// <param name="id">The group id.</param>
        /// <param name="displayName">The group display name.</param>
        /// <param name="description">The group description.</param>
        public ConverterGroup(string id, string displayName = null, string description = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            registry = TypeConverterRegistry.Create();
        }

        /// <summary>
        /// Adds a converter to the group.
        /// </summary>
        /// <param name="converter">The converter to add.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        public void AddConverter<TSource, TDestination>(TypeConverter<TSource, TDestination> converter)
        {
            registry.Register(typeof(TSource), typeof(TDestination), converter);
        }

        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> using only the converter group.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">The converted destination value if the conversion succeeded, and the default value otherwise.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns><see langword="true"/> if the conversion succeeded, and <see langword="false"/> otherwise.</returns>
        public bool TryConvert<TSource, TDestination>(ref TSource source,
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

        /// <summary>
        /// Sets the value of a property at the given path to the given value, using this converter group or the global ones.
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
        public bool TrySetValue<TContainer, TValue>(ref TContainer container,
            PropertyPath path, TValue value, out VisitReturnCode returnCode)
        {
            if (path.IsEmpty)
            {
                returnCode = VisitReturnCode.InvalidPath;
                return false;
            }

            var visitor = SetValueVisitor<TValue>.Pool.Get();
            visitor.group = this;
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

    readonly struct TypeConverterRegistry : IEqualityComparer<TypeConverterRegistry>
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

        static readonly ConverterKeyComparer k_Comparer = new ();

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

        TypeConverterRegistry(Dictionary<ConverterKey, Delegate> storage)
        {
            m_Converters = storage;
        }

        public int ConverterCount => m_Converters?.Count ?? 0;

        public static TypeConverterRegistry Create()
        {
            return new TypeConverterRegistry(new Dictionary<ConverterKey, Delegate>(k_Comparer));
        }

        public void Register(Type source, Type destination, Delegate converter)
        {
            m_Converters[new ConverterKey(source, destination)] = converter ?? throw new ArgumentException(nameof(converter));
        }

        public void Unregister(Type source, Type destination)
        {
            m_Converters.Remove(new ConverterKey(source, destination));
        }

        internal void Apply(TypeConverterRegistry registry)
        {
            foreach (var c in registry.m_Converters)
            {
                Register(c.Key.SourceType, c.Key.DestinationType, c.Value);
            }
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

        public bool Equals(TypeConverterRegistry x, TypeConverterRegistry y)
        {
            return x.m_Converters == y.m_Converters;
        }

        public int GetHashCode(TypeConverterRegistry obj)
        {
            return (obj.m_Converters != null ? obj.m_Converters.GetHashCode() : 0);
        }
    }
}
