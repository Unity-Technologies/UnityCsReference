// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The filter parameter declaration for a <see cref="FilterFunctionDefinition"/>.
    /// </summary>
    [Serializable]
    internal struct FilterParameterDeclaration
    {
        [SerializeField]
        private string m_Name;

        /// <summary>
        /// The parameter name, used for display in the UI Builder.
        /// </summary>
        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        [SerializeField]
        private FilterParameter m_InterpolationDefaultValue;

        /// <summary>
        /// Default value when interpolating between two filters with missing declarations.
        /// </summary>
        /// <remarks>
        /// Example: Transition from source filter "blur(2px)" to destination "blur(2px) invert(80%)" would
        /// insert a default "invert(0%)" filter in the source declaration.
        /// </remarks>  
        public FilterParameter interpolationDefaultValue {
            get => m_InterpolationDefaultValue;
            set => m_InterpolationDefaultValue = value;
        }

        internal FilterParameter defaultValue;
    }

    /// <summary>
    /// Represents a filter function definition that holds the parameters and effects of a filter.
    /// </summary>
    [Serializable]
    internal sealed class FilterFunctionDefinition : ScriptableObject
    {
        [SerializeField]
        private string m_FilterName;

        /// <summary>The name of the filter function used for display.</summary>
        public string filterName {
            get => m_FilterName;
            set => m_FilterName = value;
        }

        [SerializeField]
        private FilterParameterDeclaration[] m_Parameters;

        /// <summary>The description of the function parameters.</summary>
        /// <remarks>A maximum of 4 parameters is supported. Extra parameters will be ignored.</remarks>
        public FilterParameterDeclaration[] parameters {
            get => m_Parameters;
            set => m_Parameters = value;
        }

        [SerializeField]
        private PostProcessingPass[] m_Passes;

        /// <summary>The post-processing effects applied by the filter function.</summary>
        public PostProcessingPass[] passes {
            get => m_Passes;
            set => m_Passes = value;
        }
    }

    /// <summary>
    /// Represents a binding of a parameter index to a post-processing material property.
    /// </summary>
    [Serializable]
    internal struct ParameterBinding
    {
        [SerializeField]
        private int m_Index;

        /// <summary>The index of the parameter in the filter function.</summary>
        public int index {
            get => m_Index;
            set => m_Index = value;
        }

        [SerializeField]
        private string m_Name;

        /// <summary>The name of the material property.</summary>
        public string name {
            get => m_Name;
            set => m_Name = value;
        }
    }

    /// <summary>
    /// Represents a post-processing effect that can be applied to a visual element.
    /// This is used as part of a <see cref="FilterFunctionDefinition"/>.
    /// </summary>
    [Serializable]
    internal struct PostProcessingPass
    {
        [SerializeField]
        private Material m_Material;

        /// <summary>The material to use for the effect.</summary>
        public Material material {
            get => m_Material;
            set => m_Material = value;
        }

        [SerializeField]
        private int m_PassIndex;

        /// <summary>The pass index to use in the material.</summary>
        public int passIndex {
            get => m_PassIndex;
            set => m_PassIndex = value;
        }

        [SerializeField]
        private ParameterBinding[] m_ParameterBindings;

        /// <summary>The parameter bindings for the effect.</summary>
        public ParameterBinding[] parameterBindings {
            get => m_ParameterBindings;
            set => m_ParameterBindings = value;
        }

        [SerializeField]
        private PostProcessingMargins m_ReadMargins;

        /// <summary>The extra margins, in points, required for the effect to read from the source texture.</summary>
        /// <remarks>If the <see cref="computeRequiredReadMarginsCallback"/> callback is defined, this value is ignored.</remarks>
        internal PostProcessingMargins readMargins {
            get => m_ReadMargins;
            set => m_ReadMargins = value;
        }

        [SerializeField]
        private PostProcessingMargins m_WriteMargins;

        /// <summary>The extra margins, in points, required for the effect to write to destination texture.</summary>
        /// <remarks>If the <see cref="computeRequiredWriteMarginsCallback"/> callback is defined, this value is ignored.</remarks>
        public PostProcessingMargins writeMargins {
            get => m_WriteMargins;
            set => m_WriteMargins = value;
        }

        /// <summary>The optional callback to prepare the material property block for the effect.</summary>
        /// <param name="mpb">The property block to fill from the callback.</param>
        /// <param name="func">The input filter function value.</param>
        public delegate void PrepareMaterialPropertyBlockDelegate(MaterialPropertyBlock mpb, FilterFunction func);
        public PrepareMaterialPropertyBlockDelegate prepareMaterialPropertyBlockCallback { get; set; }

        /// <summary>The optional callback to compute the required read and write margins for the effect.</summary>
        /// <param name="func">The filter function value.</param>
        /// <returns>The required margins for that effect for that <see cref="FilterFunction"/>.</returns>
        public delegate PostProcessingMargins ComputeRequiredMarginsDelegate(FilterFunction func);
        public ComputeRequiredMarginsDelegate computeRequiredReadMarginsCallback { get; set; }
        public ComputeRequiredMarginsDelegate computeRequiredWriteMarginsCallback { get; set; }
    }

    /// <summary>
    /// The post-processing margins required by a <see cref="FilterFunction"/>.
    /// </summary>
    [Serializable]
    internal struct PostProcessingMargins
    {
        [SerializeField]
        private float m_Left;

        /// <summary>The left margin value.</summary>
        public float left {
            get => m_Left;
            set => m_Left = value;
        }

        [SerializeField]
        private float m_Top;

        /// <summary>The top margin value.</summary>
        public float top {
            get => m_Top;
            set => m_Top = value;
        }

        [SerializeField]
        private float m_Right;

        /// <summary>The right margin value.</summary>
        public float right {
            get => m_Right;
            set => m_Right = value;
        }

        [SerializeField]
        private float m_Bottom;

        /// <summary>The bottom margin value.</summary>
        public float bottom {
            get => m_Bottom;
            set => m_Bottom = value;
        }
    }
}
