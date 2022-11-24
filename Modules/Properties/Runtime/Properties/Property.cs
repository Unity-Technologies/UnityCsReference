// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    /// <summary>
    /// Base interface for working with properties.
    /// </summary>
    /// <remarks>
    /// This is used to pass or store properties without knowing the underlying container or value type.
    /// * <seealso cref="IProperty{TContainer}"/>
    /// * <seealso cref="Property{TContainer,TValue}"/>
    /// </remarks>
    public interface IProperty
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the property is read-only or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Returns the declared value type of the property.
        /// </summary>
        /// <returns>The declared value type.</returns>
        Type DeclaredValueType();

        /// <summary>
        /// Returns true if the property has any attributes of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to check for.</typeparam>
        /// <returns><see langword="true"/> if the property has the given attribute type; otherwise, <see langword="false"/>.</returns>
        bool HasAttribute<TAttribute>()
            where TAttribute : Attribute;

        /// <summary>
        /// Returns the first attribute of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to get.</typeparam>
        /// <returns>The attribute of the given type for this property.</returns>
        TAttribute GetAttribute<TAttribute>()
            where TAttribute : Attribute;

        /// <summary>
        /// Returns all attribute of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to get.</typeparam>
        /// <returns>An <see cref="IEnumerable{TAttribute}"/> for all attributes of the given type.</returns>
        IEnumerable<TAttribute> GetAttributes<TAttribute>()
            where TAttribute : Attribute;

        /// <summary>
        /// Returns all attribute for this property.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{Attribute}"/> for all attributes.</returns>
        IEnumerable<Attribute> GetAttributes();
    }

    /// <summary>
    /// Base interface for working with properties.
    /// </summary>
    /// <remarks>
    /// This is used to pass or store properties without knowing the underlying value type.
    /// * <seealso cref="Property{TContainer,TValue}"/>
    /// </remarks>
    /// <typeparam name="TContainer">The container type this property operates on.</typeparam>
    public interface IProperty<TContainer> : IProperty, IPropertyAccept<TContainer>
    {
        /// <summary>
        /// Returns the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <returns>The property value of the given container.</returns>
        object GetValue(ref TContainer container);

        /// <summary>
        /// Sets the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be set.</param>
        /// <param name="value">The new property value.</param>
        /// <returns><see langword="true"/> if the value was set; otherwise, <see langword="false"/>.</returns>
        void SetValue(ref TContainer container, object value);
    }

    /// <summary>
    /// Base class for implementing properties. This is an abstract class.
    /// </summary>
    /// <remarks>
    /// A <see cref="IProperty"/> is used as an accessor to the underlying data of a container.
    /// </remarks>
    /// <typeparam name="TContainer">The container type this property operates on.</typeparam>
    /// <typeparam name="TValue">The value type for this property.</typeparam>
    public abstract class Property<TContainer, TValue> : IProperty<TContainer>, IAttributes
    {
        List<Attribute> m_Attributes;

        /// <summary>
        /// Collection of attributes for this <see cref="Property{TContainer,TValue}"/>.
        /// </summary>
        List<Attribute> IAttributes.Attributes
        {
            get => m_Attributes;
            set => m_Attributes = value;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the property is read-only or not.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Returns the declared value type of the property.
        /// </summary>
        /// <returns>The declared value type.</returns>
        public Type DeclaredValueType() => typeof(TValue);

        /// <summary>
        /// Call this method to invoke <see cref="IPropertyVisitor.Visit{TContainer,TValue}"/> with the strongly typed container and value.
        /// </summary>
        /// <param name="visitor">The visitor being run.</param>
        /// <param name="container">The container being visited.</param>
        public void Accept(IPropertyVisitor visitor, ref TContainer container) => visitor.Visit(this, ref container);

        /// <summary>
        /// Returns the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <returns>The property value of the given container.</returns>
        object IProperty<TContainer>.GetValue(ref TContainer container) => GetValue(ref container);

        /// <summary>
        /// Sets the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be set.</param>
        /// <param name="value">The new property value.</param>
        /// <returns><see langword="true"/> if the value was set; otherwise, <see langword="false"/>.</returns>
        void IProperty<TContainer>.SetValue(ref TContainer container, object value) => SetValue(ref container, TypeConversion.Convert<object, TValue>(ref value));

        /// <summary>
        /// Returns the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be returned.</param>
        /// <returns>The property value of the given container.</returns>
        public abstract TValue GetValue(ref TContainer container);

        /// <summary>
        /// Sets the property value of a specified container.
        /// </summary>
        /// <param name="container">The container whose property value will be set.</param>
        /// <param name="value">The new property value.</param>
        public abstract void SetValue(ref TContainer container, TValue value);

        /// <summary>
        /// Adds an attribute to the property.
        /// </summary>
        /// <param name="attribute">The attribute to add.</param>
        protected void AddAttribute(Attribute attribute) => ((IAttributes) this).AddAttribute(attribute);

        /// <summary>
        /// Adds a set of attributes to the property.
        /// </summary>
        /// <param name="attributes">The attributes to add.</param>
        protected void AddAttributes(IEnumerable<Attribute> attributes) => ((IAttributes) this).AddAttributes(attributes);

        /// <inheritdoc/>
        void IAttributes.AddAttribute(Attribute attribute)
        {
            if (null == attribute || attribute.GetType() == typeof(CreatePropertyAttribute)) return;
            if (null == m_Attributes) m_Attributes = new List<Attribute>();
            m_Attributes.Add(attribute);
        }

        /// <summary>
        /// Adds a set of attributes to the property.
        /// </summary>
        /// <param name="attributes">The attributes to add.</param>
        void IAttributes.AddAttributes(IEnumerable<Attribute> attributes)
        {
            if (null == m_Attributes) m_Attributes = new List<Attribute>();

            foreach (var attribute in attributes)
            {
                if (null == attribute)
                    continue;

                m_Attributes.Add(attribute);
            }
        }

        /// <summary>
        /// Returns true if the property has any attributes of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to check for.</typeparam>
        /// <returns><see langword="true"/> if the property has the given attribute type; otherwise, <see langword="false"/>.</returns>
        public bool HasAttribute<TAttribute>() where TAttribute : Attribute
        {
            for (var i = 0; i < m_Attributes?.Count; i++)
            {
                if (m_Attributes[i] is TAttribute)
                {
                    return true;
                }
            }

            return default;
        }

        /// <summary>
        /// Returns the first attribute of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to get.</typeparam>
        /// <returns>The attribute of the given type for this property.</returns>
        public TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
        {
            for (var i = 0; i < m_Attributes?.Count; i++)
            {
                if (m_Attributes[i] is TAttribute typed)
                {
                    return typed;
                }
            }

            return default;
        }

        /// <summary>
        /// Returns all attribute of the given type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to get.</typeparam>
        /// <returns>An <see cref="IEnumerable{TAttribute}"/> for all attributes of the given type.</returns>
        public IEnumerable<TAttribute> GetAttributes<TAttribute>() where TAttribute : Attribute
        {
            for (var i = 0; i < m_Attributes?.Count; i++)
            {
                if (m_Attributes[i] is TAttribute typed)
                {
                    yield return typed;
                }
            }
        }

        /// <summary>
        /// Returns all attribute for this property.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{Attribute}"/> for all attributes.</returns>
        public IEnumerable<Attribute> GetAttributes()
        {
            for (var i = 0; i < m_Attributes?.Count; i++)
            {
                yield return m_Attributes[i];
            }
        }

        /// <inheritdoc/>
        AttributesScope IAttributes.CreateAttributesScope(IAttributes attributes) => new AttributesScope(this, attributes?.Attributes);
    }
}
