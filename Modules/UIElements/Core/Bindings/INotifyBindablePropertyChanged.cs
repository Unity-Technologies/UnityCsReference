// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides information about the property that has changed.
    /// </summary>
    public readonly struct BindablePropertyChangedEventArgs
    {
        readonly BindingId m_PropertyName;

        /// <summary>
        /// Instantiates a new <see cref="BindablePropertyChangedEventArgs"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        public BindablePropertyChangedEventArgs(in BindingId propertyName) => m_PropertyName = propertyName;

        /// <summary>
        /// Returns the name of the property that has changed.
        /// </summary>
        public BindingId propertyName => m_PropertyName;
    }

    /// <summary>
    /// Defines a component that notifies when a property has changed.
    /// </summary>
    public interface INotifyBindablePropertyChanged
    {
        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
    }
}
