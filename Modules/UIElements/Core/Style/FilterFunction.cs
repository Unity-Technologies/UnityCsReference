// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Layout; // For FixedBuffer4<T>
using System.Data.Common;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The filter function type for a <see cref="FilterFunction"/> .
    /// </summary>
    [Serializable]
    internal enum FilterFunctionType
    {
        /// <undoc/>
        None,

        /// <summary>A custom filter function to be used with a <see cref="FilterFunctionDefinition"/>.</summary>
        Custom,

        /// <summary>A built-in tint filter function that expects a single color value (tint).</summary>
        Tint,

        /// <summary>A built-in opacity filter function that expects a single float value between 0.0f and 1.0f (opacity).</summary>
        Opacity,

        /// <summary>A built-in invert filter function that expects a single float value between 0.0f and 1.0f (invert percentage).</summary>
        Invert,

        /// <summary>A built-in grayscale filter function that expects a single float value between 0.0f and 1.0f (grayscale percentage).</summary>
        Grayscale,

        /// <summary>A built-in sepia filter function that expects a single float value between 0.0f and 1.0f (sepia percentage).</summary>
        Sepia,

        /// <summary>A built-in blur filter function that expects a single float value (sigma).</summary>
        Blur,

        /// <undoc/>
        Count
    }

    /// <summary>
    /// The type of a filter parameter.
    /// </summary>
    [Serializable]
    internal enum FilterParameterType
    {
        /// <summary>A float value.</summary>
        Float,

        /// <summary>A color value.</summary>
        Color
    }

    /// <summary>
    /// Represents a filter parameter for a <see cref="FilterFunctionDefinition"/>.
    /// </summary>
    [Serializable]
    internal struct FilterParameter : IEquatable<FilterParameter>
    {
        [SerializeField]
        private FilterParameterType m_Type;

        /// <summary>The type of the filter parameter.</summary>
        public FilterParameterType type {
            get => m_Type;
            set => m_Type = value;
        }

        [SerializeField]
        private float m_FloatValue;

        /// <summary>The float value of the filter parameter.</summary>
        public float floatValue {
            get => m_FloatValue;
            set => m_FloatValue = value;
        }

        [SerializeField]
        private Color m_ColorValue;

        /// <summary>The color value of the filter parameter.</summary>
        public Color colorValue {
            get => m_ColorValue;
            set => m_ColorValue = value;
        }

        /// <summary>
        /// Creates a float filter parameter.
        /// </summary>
        /// <param name="value">The float value.</param>
        public FilterParameter(float value)
        {
            m_Type = FilterParameterType.Float;
            m_FloatValue = value;
            m_ColorValue = Color.clear;
        }

        /// <summary>
        /// Creates a color filter paramter.
        /// </summary>
        /// <param name="value">The color value.</param>
        public FilterParameter(Color value)
        {
            m_Type = FilterParameterType.Color;
            m_ColorValue = value;
            m_FloatValue = 0.0f;
        }

        /// <undoc/>
        public static bool operator==(FilterParameter a, FilterParameter b)
        {
            if (a.type != b.type)
                return false;
            if (a.type == FilterParameterType.Float)
                return a.floatValue == b.floatValue;
            return a.colorValue == b.colorValue;
        }

        /// <undoc/>
        public static bool operator!=(FilterParameter a, FilterParameter b)
        {
            return !(a == b);
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is FilterParameter parameter && this == parameter;
        }

        /// <undoc/>
        public bool Equals(FilterParameter other)
        {
            return this == other;
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            return type == FilterParameterType.Float ? floatValue.GetHashCode() : colorValue.GetHashCode();
        }

        /// <undoc/>
        public override string ToString()
        {
            return type == FilterParameterType.Float ? floatValue.ToString(CultureInfo.InvariantCulture) : colorValue.ToString();
        }
    }

    /// <summary>
    /// Represents a filter function that holds the definition and parameters of a filter.
    /// </summary>
    [Serializable]
    internal partial struct FilterFunction : IEquatable<FilterFunction>
    {
        [SerializeField]
        private FilterFunctionType m_Type;

        /// <summary>
        /// The type of the filter function.
        /// </summary>
        /// <remarks>
        /// When using a <see cref="FilterFunctionType.Custom"/> type, a <see cref="FilterFunction.customDefinition"/> must be provided.
        /// </remarks>
        public FilterFunctionType type
        {
            get => m_Type;
            set => m_Type = value;
        }

        [SerializeField]
        private FixedBuffer4<FilterParameter> m_Parameters;
        internal FixedBuffer4<FilterParameter> parameters
        {
            get => m_Parameters;
            set => m_Parameters = value;
        }

        [SerializeField]
        int m_ParameterCount = 0;

        /// <summary>The number of parameters in the filter function.</summary>
        public int parameterCount => m_ParameterCount;

        [SerializeField]
        private FilterFunctionDefinition m_CustomDefinition;

        /// <summary>
        /// The custom filter function definition, when the filter function type is <see cref="FilterFunctionType.Custom"/>.
        /// </summary>
        public FilterFunctionDefinition customDefinition
        {
            get => m_CustomDefinition;
            set => m_CustomDefinition = value;
        }

        /// <summary>
        /// Adds a parameter to the filter function.
        /// </summary>
        /// <param name="p">The parameter to add.</param>
        public void AddParameter(FilterParameter p)
        {
            int index = m_ParameterCount;
            System.Diagnostics.Debug.Assert(index >= 0);

            if (index >= FixedBuffer4<FilterParameter>.Length)
                throw new ArgumentOutOfRangeException($"FilterFunction.AddParameter only support up to {FixedBuffer4<FilterParameter>.Length} parameters");

            m_Parameters[index] = p;
            ++m_ParameterCount;
        }

        /// <summary>
        /// Sets a parameter to the filter function at the provided index.
        /// </summary>
        /// <param name="index">The parameter index.</param>
        /// <param name="p">The parameter to set.</param>
        public void SetParameter(int index, FilterParameter p)
        {
            if (index < 0 || index >= m_ParameterCount)
                throw new ArgumentOutOfRangeException($"FilterFunction.SetParameter index out of range");

            m_Parameters[index] = p;
        }

        /// <summary>
        /// Gets the parameter at the specified index.
        /// </summary>
        /// <param name="index">The parameter index.</param>
        /// <returns>The filter parameter at the provided index.</returns>
        public FilterParameter GetParameter(int index)
        {
            if (index < 0 || index >= parameterCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Parameters[index];
        }

        /// <summary>
        /// Clears all parameters from the filter function.
        /// </summary>
        public void ClearParameters()
        {
            m_ParameterCount = 0;
        }

        /// <summary>
        /// Initializes a new filter function with the specified <see cref="FilterFunctionType"/>.
        /// </summary>
        public FilterFunction(FilterFunctionType type)
        {
            m_Type = type;
            m_CustomDefinition = null;
            m_Parameters = new FixedBuffer4<FilterParameter>();
        }

        /// <summary>
        /// Initializes a new custom filter function with the specified <see cref="FilterFunctionDefinition"/>.
        /// </summary>
        public FilterFunction(FilterFunctionDefinition filterDef)
        {
            if (filterDef == null)
                throw new ArgumentNullException(nameof(filterDef));

            m_Type = FilterFunctionType.Custom;
            m_CustomDefinition = filterDef;
            m_Parameters = new FixedBuffer4<FilterParameter>();
        }

        internal FilterFunction(FilterFunctionType type, FixedBuffer4<FilterParameter> parameters, int paramCount)
        {
            m_Type = type;
            m_CustomDefinition = null;
            m_Parameters = parameters;
            m_ParameterCount = paramCount;

            var def = GetDefinition();
            if (def != null)
            {
                int expectedParamCount = GetDefinition().parameters.Length;
                if (expectedParamCount != paramCount)
                    Debug.LogError($"Invalid parameter count provided with filter of type {type}: provided {paramCount} but expected {expectedParamCount}");
            }
        }

        internal FilterFunction(FilterFunctionDefinition customDefinition, FixedBuffer4<FilterParameter> parameters, int paramCount)
        {
            m_Type = FilterFunctionType.Custom;
            m_CustomDefinition = customDefinition;
            m_Parameters = parameters;
            m_ParameterCount = paramCount;

            if (customDefinition != null)
            {
                int expectedParamCount = customDefinition.parameters?.Length ?? 0;
                if (expectedParamCount != paramCount)
                    Debug.LogError($"Invalid parameter count provided with custom filter function definition {customDefinition}: provided {paramCount} but expected {expectedParamCount}");
            }
        }

        internal FilterFunctionDefinition GetDefinition()
        {
            if (m_Type == FilterFunctionType.Custom)
                return m_CustomDefinition;

            return FilterFunctionDefinitionUtils.GetBuiltinDefinition(m_Type);
        }

        /// <undoc/>
        public static bool operator==(FilterFunction lhs, FilterFunction rhs)
        {
            if (lhs.m_CustomDefinition != rhs.m_CustomDefinition)
                return false;

            for (int i = 0; i < FixedBuffer4<FilterParameter>.Length; ++i)
            {
                if (lhs.m_Parameters[i] != rhs.m_Parameters[i])
                    return false;
            }

            return true;
        }

        /// <undoc/>
        public static bool operator!=(FilterFunction lhs, FilterFunction rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(FilterFunction other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is FilterFunction other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Parameters.GetHashCode() * 397) ^ m_CustomDefinition.GetHashCode();
            }
        }

        /// <undoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var def = GetDefinition();
            if (!string.IsNullOrEmpty(def?.filterName))
                sb.Append(def.filterName);
            else if (type == FilterFunctionType.Custom)
                sb.Append("custom");
            else if (type == FilterFunctionType.None)
                sb.Append("none");

            sb.Append("(");
            for (int i = 0; i < parameterCount; i++)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(parameters[i].ToString());
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}
