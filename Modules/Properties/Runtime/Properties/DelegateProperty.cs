// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    /// <summary>
    /// Represents the method that will handle getting a <typeparamref name="TValue"/> type for a specified <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The container which holds the data.</param>
    /// <typeparam name="TContainer">The strongly typed container.</typeparam>
    /// <typeparam name="TValue">The strongly typed value to get.</typeparam>
    public delegate TValue PropertyGetter<TContainer, out TValue>(ref TContainer container);

    /// <summary>
    /// Represents the method that will handle setting a specified <paramref name="value"/> for a specified <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The container on which to set the data.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="TContainer">The strongly typed container.</typeparam>
    /// <typeparam name="TValue">The strongly typed value to set.</typeparam>
    public delegate void PropertySetter<TContainer, in TValue>(ref TContainer container, TValue value);

    /// <summary>
    /// Represents a value property.
    /// </summary>
    /// <remarks>
    /// A <see cref="DelegateProperty{TContainer,TValue}"/> is the default way to construct properties.
    /// </remarks>
    /// <typeparam name="TContainer">The container type this property will access data on.</typeparam>
    /// <typeparam name="TValue">The value type for this property.</typeparam>
    public class DelegateProperty<TContainer, TValue> : Property<TContainer, TValue>
    {
        readonly PropertyGetter<TContainer, TValue> m_Getter;
        readonly PropertySetter<TContainer, TValue> m_Setter;

        /// <inheritdoc/>
        public override string Name { get; }

        /// <inheritdoc/>
        public override bool IsReadOnly => null == m_Setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateProperty{TContainer,TValue}"/> class.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="getter">The delegate to use when accessing the property value.</param>
        /// <param name="setter">The delegate to use when setting the property value.</param>
        public DelegateProperty(
            string name,
            PropertyGetter<TContainer, TValue> getter,
            PropertySetter<TContainer, TValue> setter = null)
        {
            Name = name;
            m_Getter = getter ?? throw new ArgumentException(nameof(getter));
            m_Setter = setter;
        }

        /// <inheritdoc/>
        public override TValue GetValue(ref TContainer container)
        {
            return m_Getter(ref container);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The property is read-only.</exception>
        public override void SetValue(ref TContainer container, TValue value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Property is ReadOnly.");
            }

            m_Setter(ref container, value);
        }
    }
}
