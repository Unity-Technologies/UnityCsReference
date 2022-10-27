// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base implementation for constants.
    /// </summary>
    /// <typeparam name="T">The type of the value of the constant.</typeparam>
    [Serializable]
    abstract class Constant<T> : Constant
    {
        [SerializeField]
        protected T m_Value;

        /// <summary>
        /// The constant value.
        /// </summary>
        public virtual T Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <inheritdoc />
        public override object ObjectValue
        {
            get => Value;
            set => Value = FromObject(value);
        }

        /// <inheritdoc />
        public override object DefaultValue => default(T);

        /// <inheritdoc />
        public override Type Type => typeof(T);

        /// <summary>
        /// Converts an object to a value of the type {T}.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The object cast to type {T}.</returns>
        protected virtual T FromObject(object value) => (T)value;
    }
}
